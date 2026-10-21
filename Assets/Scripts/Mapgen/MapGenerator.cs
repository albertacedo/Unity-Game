using System;
using System.Collections.Generic;

namespace Prototype.Mapgen
{
    public enum NodeType { Combat, Elite, Rest, Shop, Event, Boss }

    public class MapNode
    {
        public int Floor;
        public int Column;
        public NodeType Type;
        public bool Visited;
        public readonly List<MapNode> Next = new List<MapNode>();
        public readonly List<MapNode> Previous = new List<MapNode>();
    }

    public class RunMap
    {
        public int FloorCount;
        public int Width;
        public List<MapNode>[] Floors;
    }

    /// <summary>
    /// Slay-the-Spire style map: several paths are walked from the bottom row to the
    /// top, nodes are merged by (floor, column) and edges are rejected when they would
    /// cross an existing edge. Same seed always produces the same map.
    /// </summary>
    public static class MapGenerator
    {
        public static RunMap Generate(int seed, int floorCount = 6, int width = 7, int paths = 4)
        {
            var rng = new Random(seed);
            var map = new RunMap
            {
                FloorCount = floorCount,
                Width = width,
                Floors = new List<MapNode>[floorCount]
            };
            for (int i = 0; i < floorCount; i++)
                map.Floors[i] = new List<MapNode>();

            int minCol = 1;
            int maxCol = width - 2;
            int lastRegular = floorCount - 2;

            MapNode GetOrCreate(int floor, int column)
            {
                foreach (MapNode node in map.Floors[floor])
                    if (node.Column == column) return node;

                var created = new MapNode { Floor = floor, Column = column };
                map.Floors[floor].Add(created);
                return created;
            }

            for (int p = 0; p < paths; p++)
            {
                int column = rng.Next(minCol, maxCol + 1);
                GetOrCreate(0, column);

                for (int floor = 0; floor < lastRegular; floor++)
                {
                    var options = new List<int> { column };
                    if (column > minCol) options.Add(column - 1);
                    if (column < maxCol) options.Add(column + 1);
                    Shuffle(options, rng);

                    int chosen = column;
                    for (int i = 0; i < options.Count; i++)
                    {
                        if (!WouldCross(map, floor, column, options[i]))
                        {
                            chosen = options[i];
                            break;
                        }
                    }

                    MapNode from = GetOrCreate(floor, column);
                    MapNode to = GetOrCreate(floor + 1, chosen);
                    if (!from.Next.Contains(to))
                    {
                        from.Next.Add(to);
                        to.Previous.Add(from);
                    }
                    column = chosen;
                }
            }

            MapNode boss = GetOrCreate(floorCount - 1, width / 2);
            boss.Type = NodeType.Boss;
            foreach (MapNode node in map.Floors[lastRegular])
            {
                if (!node.Next.Contains(boss))
                {
                    node.Next.Add(boss);
                    boss.Previous.Add(node);
                }
            }

            for (int floor = 0; floor < floorCount - 1; floor++)
                foreach (MapNode node in map.Floors[floor])
                    node.Type = floor == 0 ? NodeType.Combat : PickType(rng);

            for (int floor = 0; floor < floorCount; floor++)
                map.Floors[floor].Sort((a, b) => a.Column.CompareTo(b.Column));

            return map;
        }

        static bool WouldCross(RunMap map, int floor, int fromColumn, int toColumn)
        {
            foreach (MapNode source in map.Floors[floor])
            {
                if (source.Column == fromColumn) continue;
                foreach (MapNode target in source.Next)
                {
                    if ((fromColumn - source.Column) * (toColumn - target.Column) < 0)
                        return true;
                }
            }
            return false;
        }

        static NodeType PickType(Random rng)
        {
            double roll = rng.NextDouble();
            if (roll < 0.62) return NodeType.Combat;
            if (roll < 0.80) return NodeType.Rest;
            if (roll < 0.90) return NodeType.Shop;
            return NodeType.Event;
        }

        static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
