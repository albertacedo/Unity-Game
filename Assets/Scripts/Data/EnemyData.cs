using System;

namespace Prototype.Data
{
    public enum IntentKind { Attack, Block }

    /// <summary>How an enemy is picked when a combat room starts.</summary>
    public enum EnemyRole { Normal, Pack, Boss }

    [Serializable]
    public class EnemyIntent
    {
        public IntentKind Kind = IntentKind.Attack;
        public int Value = 6;

        public EnemyIntent() { }

        public EnemyIntent(IntentKind kind, int value)
        {
            Kind = kind;
            Value = value;
        }

        public string Describe()
        {
            return Kind == IntentKind.Attack ? "Attacking for " + Value : "Defending " + Value;
        }
    }

    /// <summary>
    /// An enemy definition. Built from Resources/Content/enemies.csv; there are no
    /// enemy .asset files.
    /// </summary>
    [Serializable]
    public class EnemyData
    {
        public string Id;
        public string Title;
        public int MaxHp = 40;
        public EnemyRole Role = EnemyRole.Normal;
        public EnemyIntent[] Intents = new EnemyIntent[0];
    }
}
