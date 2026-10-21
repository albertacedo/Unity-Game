using System.Collections.Generic;
using Prototype.Data;
using Prototype.Mapgen;

namespace Prototype
{
    /// <summary>Everything that survives between rooms during a run.</summary>
    public class RunState
    {
        public int MaxHp = 72;
        public int Hp = 72;
        public int Gold = 99;
        public int Seed;
        public RunMap Map;
        public MapNode Current;
        public List<CardData> Deck = new List<CardData>();
    }
}
