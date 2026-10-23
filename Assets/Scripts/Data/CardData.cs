using System;
using UnityEngine;

namespace Prototype.Data
{
    public enum CardKind { Attack, Skill, Power }

    public enum EffectKind { Damage, Block, Draw, GainEnergy, Vulnerable, DamageAll }

    [Serializable]
    public class CardEffect
    {
        public EffectKind Kind;
        public int Value;

        public CardEffect() { }

        public CardEffect(EffectKind kind, int value)
        {
            Kind = kind;
            Value = value;
        }
    }

    /// <summary>
    /// A playable card. Content lives in code for now (see CardLibrary); this type is
    /// already a ScriptableObject so it can be moved to .asset files without changing
    /// any gameplay code.
    /// </summary>
    [CreateAssetMenu(menuName = "Prototype/Card", fileName = "NewCard")]
    public class CardData : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea(2, 4)] public string Text;
        public int Cost = 1;
        public CardKind Kind = CardKind.Attack;
        public CardEffect[] Effects = new CardEffect[0];

        public Color Tint
        {
            get
            {
                switch (Kind)
                {
                    case CardKind.Attack: return new Color(0.52f, 0.17f, 0.17f);
                    case CardKind.Skill: return new Color(0.15f, 0.30f, 0.50f);
                    default: return new Color(0.32f, 0.21f, 0.46f);
                }
            }
        }
    }
}
