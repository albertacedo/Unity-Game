using System;
using System.Collections.Generic;

namespace Prototype.Mapgen
{
    public enum NodeType { Start, Combat, Elite, Rest, Shop, Event, Boss }

    public class MapNode
    {
        public int Ring;
        public int Index;
        /// <summary>Degrees around the centre. 0 points up and grows clockwise.</summary>
        public float Angle;
        public NodeType Type;
        public bool Visited;
        /// <summary>Every connected room, inwards and outwards. Walking is free both ways.</summary>
        public readonly List<MapNode> Next = new List<MapNode>();
    }

    public class RunMap
    {
        public int RingCount;
        public List<MapNode>[] Rings;
        public MapNode Start;
        public readonly List<MapNode> Bosses = new List<MapNode>();

        public IEnumerable<MapNode> Nodes()
        {
            foreach (List<MapNode> ring in Rings)
                foreach (MapNode node in ring)
                    yield return node;
        }

        public int VisitedCount()
        {
            int visited = 0;
            foreach (MapNode node in Nodes())
                if (node.Visited) visited++;
            return visited;
        }
    }

    /// <summary>
    /// Radial map: one start room in the middle, rings of rooms growing outwards and
    /// several bosses on the outer ring. Consecutive rings are linked through nearby
    /// angles and every edge is walkable in both directions, so the player explores
    /// freely as long as rooms are unvisited. Same seed always produces the same map.
    /// </summary>
    public static class MapGenerator
    {
        public const int DefaultRingCount = 7;

        const int FirstRingRooms = 5;
        const int MaxRoomsPerRing = 15;
        const int MinBosses = 3;
        const int MaxBosses = 6;

        public static RunMap Generate(int seed, int ringCount = DefaultRingCount)
        {
            if (ringCount < 3) ringCount = 3;

            var rng = new Random(seed);
            var map = new RunMap { RingCount = ringCount, Rings = new List<MapNode>[ringCount] };
            for (int ring = 0; ring < ringCount; ring++)
                map.Rings[ring] = new List<MapNode>();

            var start = new MapNode { Ring = 0, Index = 0, Angle = 0f, Type = NodeType.Start, Visited = true };
            map.Rings[0].Add(start);
            map.Start = start;

            int bossRing = ringCount - 1;
            for (int ring = 1; ring < bossRing; ring++)
            {
                int count = Math.Min(FirstRingRooms + 2 * (ring - 1), MaxRoomsPerRing);
                BuildRing(map, ring, count, rng);

                foreach (MapNode node in map.Rings[ring])
                    node.Type = ring == 1 ? NodeType.Combat : PickType(rng);
            }

            BuildBossRing(map, rng);

            // The centre opens onto every first-ring room, which gives the run a choice
            // of direction from the very first click.
            foreach (MapNode node in map.Rings[1])
                Connect(start, node);

            for (int ring = 1; ring < bossRing - 1; ring++)
                LinkRings(map.Rings[ring], map.Rings[ring + 1], rng);

            LinkBosses(map.Rings[bossRing - 1], map.Rings[bossRing], rng);

            return map;
        }

        /// <summary>
        /// Each outer room walks to the boss closest in angle. Distance would also be fair,
        /// but on a wide ellipse a room can end up "closer" to the opposite boss, and those
        /// roads would cross the whole map.
        /// </summary>
        static void LinkBosses(List<MapNode> rooms, List<MapNode> bosses, Random rng)
        {
            if (bosses.Count == 0) return;

            var reached = new HashSet<MapNode>();
            foreach (MapNode room in rooms)
            {
                MapNode best = null;
                float bestDelta = float.MaxValue;

                foreach (MapNode boss in bosses)
                {
                    float delta = AngleDelta(room.Angle, boss.Angle);
                    if (delta >= bestDelta) continue;

                    bestDelta = delta;
                    best = boss;
                }

                if (best == null) continue;
                Connect(room, best);
                reached.Add(best);
            }

            // A boss nobody walks to would be a decoration, not an exit.
            foreach (MapNode boss in bosses)
            {
                if (reached.Contains(boss)) continue;

                MapNode best = null;
                float bestDelta = float.MaxValue;
                foreach (MapNode room in rooms)
                {
                    float delta = AngleDelta(room.Angle, boss.Angle);
                    if (delta >= bestDelta) continue;

                    bestDelta = delta;
                    best = room;
                }

                if (best != null) Connect(best, boss);
            }
        }

        /// <summary>
        /// Bosses stand behind the outer rooms, one per sector, so their roads stay short
        /// instead of cutting across the whole map.
        /// </summary>
        static void BuildBossRing(RunMap map, Random rng)
        {
            int bossRing = map.RingCount - 1;
            int count = rng.Next(MinBosses, MaxBosses + 1);
            float sector = 360f / count;

            var available = new List<MapNode>(map.Rings[bossRing - 1]);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                float target = Normalize(i * sector + sector * 0.5f);
                MapNode anchor = null;
                float best = float.MaxValue;

                foreach (MapNode candidate in available)
                {
                    float distance = AngleDelta(candidate.Angle, target);
                    if (distance >= best) continue;

                    best = distance;
                    anchor = candidate;
                }

                available.Remove(anchor);

                float jitter = ((float)rng.NextDouble() - 0.5f) * sector * 0.35f;
                var boss = new MapNode
                {
                    Ring = bossRing,
                    Angle = Normalize(anchor.Angle + jitter),
                    Type = NodeType.Boss
                };

                map.Rings[bossRing].Add(boss);
                map.Bosses.Add(boss);
            }

            map.Rings[bossRing].Sort((a, b) => a.Angle.CompareTo(b.Angle));
            for (int i = 0; i < map.Rings[bossRing].Count; i++)
                map.Rings[bossRing][i].Index = i;
        }

        static void BuildRing(RunMap map, int ring, int count, Random rng)
        {
            float step = 360f / count;
            float offset = (float)rng.NextDouble() * 360f;
            if (ring % 2 == 0) offset += step * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float jitter = ((float)rng.NextDouble() - 0.5f) * step * 0.5f;
                float angle = Normalize(offset + i * step + jitter);
                map.Rings[ring].Add(new MapNode { Ring = ring, Angle = angle });
            }

            map.Rings[ring].Sort((a, b) => a.Angle.CompareTo(b.Angle));
            for (int i = 0; i < map.Rings[ring].Count; i++)
                map.Rings[ring][i].Index = i;
        }

        /// <summary>Links rooms of two consecutive rings through their closest neighbours.</summary>
        static void LinkRings(List<MapNode> inner, List<MapNode> outer, Random rng)
        {
            var linked = new HashSet<MapNode>();

            foreach (MapNode from in inner)
            {
                List<MapNode> nearest = Nearest(outer, from, 2);
                if (nearest.Count == 0) continue;

                Connect(from, nearest[0]);
                linked.Add(nearest[0]);

                if (nearest.Count > 1 && rng.NextDouble() < 0.5)
                {
                    Connect(from, nearest[1]);
                    linked.Add(nearest[1]);
                }
            }

            // Every room needs a way in, otherwise it would be unreachable from the centre.
            foreach (MapNode to in outer)
            {
                if (linked.Contains(to)) continue;

                List<MapNode> nearest = Nearest(inner, to, 1);
                if (nearest.Count > 0) Connect(nearest[0], to);
            }
        }

        static List<MapNode> Nearest(List<MapNode> candidates, MapNode origin, int count)
        {
            var sorted = new List<MapNode>(candidates);
            sorted.Sort((a, b) => MapLayout.SqrDistance(a, origin).CompareTo(MapLayout.SqrDistance(b, origin)));

            if (sorted.Count > count) sorted.RemoveRange(count, sorted.Count - count);
            return sorted;
        }

        static void Connect(MapNode from, MapNode to)
        {
            if (from.Next.Contains(to)) return;

            from.Next.Add(to);
            to.Next.Add(from);
        }

        static float AngleDelta(float a, float b)
        {
            float delta = Normalize(a - b);
            return delta > 180f ? 360f - delta : delta;
        }

        static float Normalize(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }

        static NodeType PickType(Random rng)
        {
            double roll = rng.NextDouble();
            if (roll < 0.62) return NodeType.Combat;
            if (roll < 0.80) return NodeType.Rest;
            if (roll < 0.90) return NodeType.Shop;
            return NodeType.Event;
        }
    }
}
