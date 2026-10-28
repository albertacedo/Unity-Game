using System;
using System.Text;

namespace Prototype.Data
{
    public enum CardKind { Attack, Skill, Power }

    /// <summary>Which starting pools a card belongs to.</summary>
    public enum CardPool { None, Starter, Reward, Both }

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
    /// A playable card. Instances are built from the CSV tables in Resources/Content
    /// (see ContentCsv); the project has no card .asset files.
    /// </summary>
    [Serializable]
    public class CardData
    {
        public string Id;
        public string Title;
        public string Text;
        public int Cost = 1;
        public CardKind Kind = CardKind.Attack;
        public CardPool Pool = CardPool.Reward;

        /// <summary>How many copies go into the starting deck. Ignored for reward-only cards.</summary>
        public int Copies = 1;

        public CardEffect[] Effects = new CardEffect[0];

        public bool IsStarter { get { return Pool == CardPool.Starter || Pool == CardPool.Both; } }
        public bool IsReward { get { return Pool == CardPool.Reward || Pool == CardPool.Both; } }

        /// <summary>Card text, or a sentence per effect when Text is left empty in the CSV.</summary>
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(Text)) return Text;

                var sb = new StringBuilder();
                if (Effects != null)
                {
                    foreach (CardEffect effect in Effects)
                    {
                        if (effect == null) continue;
                        sb.Append(DescribeEffect(effect.Kind, effect.Value));
                        sb.Append('\n');
                    }
                }

                return sb.Length == 0 ? "" : sb.ToString(0, sb.Length - 1);
            }
        }

        public static string DescribeEffect(EffectKind kind, int value)
        {
            switch (kind)
            {
                case EffectKind.Damage: return "Deal " + value + " damage.";
                case EffectKind.DamageAll: return "Deal " + value + " damage to ALL enemies.";
                case EffectKind.Block: return "Gain " + value + " Block.";
                case EffectKind.Draw: return "Draw " + value + (value == 1 ? " card." : " cards.");
                case EffectKind.GainEnergy: return "Gain " + value + (value == 1 ? " energy." : " energy.");
                case EffectKind.Vulnerable: return "Apply " + value + " Vulnerable.";
                default: return kind + " " + value + ".";
            }
        }
    }
}
