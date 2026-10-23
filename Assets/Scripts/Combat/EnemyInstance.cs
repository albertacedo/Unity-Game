using Prototype.Data;

namespace Prototype.Combat
{
    /// <summary>
    /// One enemy inside a fight. <see cref="EnemyData"/> is the shared definition;
    /// this holds what changes while the fight runs (HP, block, Vulnerable, intent).
    /// </summary>
    public class EnemyInstance
    {
        public readonly EnemyData Data;
        public readonly string Title;
        public readonly int MaxHp;

        public int Hp;
        public int Block;
        public int Vulnerable;
        public int IntentIndex;

        public EnemyInstance(EnemyData data, int maxHp, string title)
        {
            Data = data;
            Title = title;
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public bool IsAlive { get { return Hp > 0; } }

        /// <summary>What this enemy will do on its next turn, or null if it has no intents.</summary>
        public EnemyIntent Intent
        {
            get
            {
                if (Data.Intents == null || Data.Intents.Length == 0) return null;
                return Data.Intents[IntentIndex % Data.Intents.Length];
            }
        }

        public void AdvanceIntent()
        {
            if (Data.Intents == null || Data.Intents.Length == 0) return;
            IntentIndex = (IntentIndex + 1) % Data.Intents.Length;
        }
    }
}
