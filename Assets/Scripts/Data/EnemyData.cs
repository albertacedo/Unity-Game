using System;
using UnityEngine;

namespace Prototype.Data
{
    public enum IntentKind { Attack, Block }

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

    [CreateAssetMenu(menuName = "Prototype/Enemy", fileName = "NewEnemy")]
    public class EnemyData : ScriptableObject
    {
        public string Id;
        public string Title;
        public int MaxHp = 40;
        public EnemyIntent[] Intents = new EnemyIntent[0];
    }
}
