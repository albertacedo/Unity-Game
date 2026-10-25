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
    /// 4-7 bosses on the outer ring. Consecutive rings are joined by angular wedges: each
    /// room of the bigger ring hangs from the room (or boss) of the smaller ring that is
    /// closest in angle, so roads never lean past each other and the map stays readable.
    /// Rooms of the same ring are joined by arcs that follow the ring, and the last room
    /// ring is a full ring road, which connects every branch without cutting a road.
    /// Same seed always produces the same map.
    /// </summary>
    public static class MapGenerator
    {
        public const int DefaultRingCount = 7;

        const int FirstRingRooms = 5;
        const int MaxRoomsPerRing = 15;
        const int MinBosses = 4;
        const int MaxBosses = 7;

        /// <summary>Chance that two neighbouring rooms of the same ring are joined.</summary>
        const float LateralChance = 0.5f;

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

            for (int ring = 1; ring < bossRing; ring++)
                LinkRings(map.Rings[ring], map.Rings[ring + 1]);

            // The room ring in front of the bosses is a complete ring road: from any branch
            // you can walk around the edge and pick a different boss.
            for (int ring = 1; ring < bossRing; ring++)
                LinkLateral(map.Rings[ring], rng, ring == bossRing - 1);

            return map;
        }

        /// <summary>
        /// Links two rings. The ring with fewer rooms owns the other one: every room of the
        /// bigger ring is attached to the room of the smaller ring that is closest in angle, so
        /// the map is split into wedges centred on the smaller ring. Two roads from different
        /// wedges never cross, and a road never leans back over a neighbour.
        /// </summary>
        static void LinkRings(List<MapNode> inner, List<MapNode> outer)
        {
            if (inner.Count == 0 || outer.Count == 0) return;

            List<MapNode> owners = inner;
            List<MapNode> members = outer;

            if (inner.Count > outer.Count)
            {
                owners = outer;
                members = inner;
            }

            Dictionary<MapNode, List<MapNode>> wedges = AssignWedges(owners, members);
            RefillEmptyWedges(owners, wedges);

            foreach (MapNode owner in owners)
                foreach (MapNode member in wedges[owner])
                    Connect(owner, member);
        }

        /// <summary>Groups every member under the owner it points at most directly.</summary>
        static Dictionary<MapNode, List<MapNode>> AssignWedges(List<MapNode> owners, List<MapNode> members)
        {
            var wedges = new Dictionary<MapNode, List<MapNode>>();
            foreach (MapNode owner in owners)
                wedges[owner] = new List<MapNode>();

            foreach (MapNode member in members)
                wedges[NearestOwner(owners, member.Angle)].Add(member);

            return wedges;
        }

        static MapNode NearestOwner(List<MapNode> owners, float angle)
        {
            MapNode best = owners[0];
            float bestDelta = AngleDelta(best.Angle, angle);

            for (int i = 1; i < owners.Count; i++)
            {
                float delta = AngleDelta(owners[i].Angle, angle);
                if (delta >= bestDelta) continue;

                best = owners[i];
                bestDelta = delta;
            }

            return best;
        }

        /// <summary>
        /// A wedge can come out empty when the rooms bunch up: borrow the closest room from an
        /// adjacent wedge so every owner has somewhere to go.
        /// </summary>
        static void RefillEmptyWedges(List<MapNode> owners, Dictionary<MapNode, List<MapNode>> wedges)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 0; i < owners.Count; i++)
                {
                    if (wedges[owners[i]].Count > 0) continue;

                    List<MapNode> left = wedges[owners[(i - 1 + owners.Count) % owners.Count]];
                    List<MapNode> right = wedges[owners[(i + 1) % owners.Count]];

                    MapNode borrowed = ClosestWithSpare(left, right, owners[i].Angle);
                    if (borrowed == null) continue;

                    left.Remove(borrowed);
                    right.Remove(borrowed);
                    wedges[owners[i]].Add(borrowed);
                    changed = true;
                }
            }
        }

        /// <summary>Closest room in either list, never taking the last room of a wedge.</summary>
        static MapNode ClosestWithSpare(List<MapNode> left, List<MapNode> right, float angle)
        {
            MapNode best = null;
            float bestDelta = float.MaxValue;

            foreach (List<MapNode> list in new[] { left, right })
            {
                if (list.Count < 2) continue;

                foreach (MapNode node in list)
                {
                    float delta = AngleDelta(node.Angle, angle);
                    if (delta >= bestDelta) continue;

                    best = node;
                    bestDelta = delta;
                }
            }

            return best;
        }

        /// <summary>Joins neighbouring rooms of the same ring, drawn as an arc (no crossing).</summary>
        static void LinkLateral(List<MapNode> ring, Random rng, bool full)
        {
            if (ring.Count < 3) return;

            for (int i = 0; i < ring.Count; i++)
            {
                if (!full && rng.NextDouble() >= LateralChance) continue;

                Connect(ring[i], ring[(i + 1) % ring.Count]);
            }
        }

        /// <summary>
        /// Bosses are gates: each one sits on the outer ring over the middle of the rooms that
        /// lead to it. Staying over its rooms keeps every road inside its own ring gap, which is
        /// what stops two roads from crossing.
        /// </summary>
        static void BuildBossRing(RunMap map, Random rng)
        {
            int bossRing = map.RingCount - 1;
            List<MapNode> rooms = map.Rings[bossRing - 1];
            int count = rng.Next(MinBosses, MaxBosses + 1);
            float sector = 360f / count;
            float offset = (float)rng.NextDouble() * 360f;

            var bosses = new List<MapNode>(count);
            for (int i = 0; i < count; i++)
            {
                float jitter = ((float)rng.NextDouble() - 0.5f) * sector * 0.2f;
                bosses.Add(new MapNode
                {
                    Ring = bossRing,
                    Angle = Normalize(offset + i * sector + jitter),
                    Type = NodeType.Boss
                });
            }

            // Two passes: move each gate over the rooms that chose it, then let them choose again.
            for (int pass = 0; pass < 2; pass++)
            {
                Dictionary<MapNode, List<MapNode>> wedges = AssignWedges(bosses, rooms);
                foreach (MapNode boss in bosses)
                {
                    List<MapNode> wedge = wedges[boss];
                    if (wedge.Count > 0) boss.Angle = CircularCentre(wedge);
                }
            }

            map.Rings[bossRing].AddRange(bosses);
            map.Rings[bossRing].Sort((a, b) => a.Angle.CompareTo(b.Angle));
            for (int i = 0; i < map.Rings[bossRing].Count; i++)
                map.Rings[bossRing][i].Index = i;

            map.Bosses.AddRange(bosses);
        }

        /// <summary>Middle angle of a group of rooms, measured on the unit circle so it survives 0°.</summary>
        static float CircularCentre(List<MapNode> nodes)
        {
            double x = 0, y = 0;
            foreach (MapNode node in nodes)
            {
                double radians = node.Angle * System.Math.PI / 180.0;
                x += System.Math.Cos(radians);
                y += System.Math.Sin(radians);
            }

            return Normalize((float)(System.Math.Atan2(y, x) * 180.0 / System.Math.PI));
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
            if (roll < 0.50) return NodeType.Combat;
            if (roll < 0.65) return NodeType.Rest;
            if (roll < 0.75) return NodeType.Shop;
            return NodeType.Event;
        }
    }
}
