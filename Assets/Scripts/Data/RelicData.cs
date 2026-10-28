using System;

namespace Prototype.Data
{
    public enum RelicRarity { Common, Uncommon, Rare, Boss, Event }

    /// <summary>
    /// A run relic definition. The catalog is authored now so relics can be written
    /// before the relic system exists; nothing in gameplay consumes it yet.
    /// </summary>
    [Serializable]
    public class RelicData
    {
        public string Id;
        public string Title;
        public string Description;
        public RelicRarity Rarity = RelicRarity.Common;
    }
}
