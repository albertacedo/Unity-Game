using System;
using System.Collections.Generic;
using Prototype.Data;

namespace Prototype
{
    /// <summary>
    /// What an event option did, in the order it happened, ready to be shown to the
    /// player. <see cref="Died"/> is set when the option took the last hit point.
    /// </summary>
    public class EventResolution
    {
        public readonly List<string> Lines = new List<string>();
        public bool Died;
    }

    /// <summary>
    /// Rules of an event room: pick one, say whether an option can be paid, apply it.
    /// Plain C# on purpose: no UGUI and no UnityEngine, so the numbers stay testable
    /// and the screen only renders what this class decided.
    /// </summary>
    public static class EventResolver
    {
        /// <summary>
        /// Weighted pick, deterministic per run and room, skipping events already seen
        /// this run. Falls back to the whole pool when everything has been seen.
        /// </summary>
        public static EventData Pick(IList<EventData> pool, int seed, int roomKey, ICollection<string> seen)
        {
            if (pool == null || pool.Count == 0) return null;

            var candidates = new List<EventData>();
            foreach (EventData entry in pool)
            {
                if (entry == null) continue;
                if (seen != null && entry.Id != null && seen.Contains(entry.Id)) continue;
                candidates.Add(entry);
            }

            if (candidates.Count == 0)
            {
                foreach (EventData entry in pool)
                    if (entry != null) candidates.Add(entry);
            }
            if (candidates.Count == 0) return null;

            int total = 0;
            foreach (EventData entry in candidates) total += Math.Max(1, entry.Weight);

            int roll = new Random(unchecked(seed * 397 + roomKey * 31 + 1543)).Next(total);
            foreach (EventData entry in candidates)
            {
                int weight = Math.Max(1, entry.Weight);
                if (roll < weight) return entry;
                roll -= weight;
            }

            return candidates[candidates.Count - 1];
        }

        /// <summary>Gold the option takes away before it resolves.</summary>
        public static int GoldCost(EventOption option)
        {
            if (option == null || option.Effects == null) return 0;

            int cost = 0;
            foreach (EventEffect effect in option.Effects)
            {
                if (effect == null || effect.Kind != EventEffectKind.LoseGold) continue;
                cost += Math.Max(0, effect.Value);
            }
            return cost;
        }

        /// <summary>An option that costs more gold than the player has cannot be chosen.</summary>
        public static bool CanAfford(EventOption option, RunState run)
        {
            if (run == null) return true;
            return run.Gold >= GoldCost(option);
        }

        /// <summary>Short summary for the option button, e.g. "-15 gold, +5 max HP".</summary>
        public static string Describe(EventOption option, IList<CardData> cards)
        {
            if (option == null || option.Effects == null) return "no effect";

            var parts = new List<string>();
            foreach (EventEffect effect in option.Effects)
            {
                string part = DescribeEffect(effect, cards);
                if (!string.IsNullOrEmpty(part)) parts.Add(part);
            }

            return parts.Count == 0 ? "no effect" : string.Join(", ", parts.ToArray());
        }

        static string DescribeEffect(EventEffect effect, IList<CardData> cards)
        {
            if (effect == null) return null;

            switch (effect.Kind)
            {
                case EventEffectKind.Nothing: return null;
                case EventEffectKind.Heal: return Signed(effect.Value, "HP");
                case EventEffectKind.LoseHp: return "-" + effect.Value + " HP";
                case EventEffectKind.GainGold: return Signed(effect.Value, "gold");
                case EventEffectKind.LoseGold: return "-" + effect.Value + " gold";
                case EventEffectKind.MaxHp: return Signed(effect.Value, "max HP");
                case EventEffectKind.AddCard:
                    CardData card = FindCard(cards, effect.Target);
                    return "+" + (card != null ? card.Title : effect.Target);
                default: return null;
            }
        }

        static string Signed(int value, string unit)
        {
            return (value > 0 ? "+" : "") + value + " " + unit;
        }

        /// <summary>
        /// Applies every effect of the chosen option in order and records one readable
        /// line per effect. Clamps: healing stops at max HP, gold stops at zero, and the
        /// last hit point ends the run instead of going negative.
        /// </summary>
        public static EventResolution Apply(EventOption option, RunState run, IList<CardData> cards)
        {
            var result = new EventResolution();
            if (option == null || run == null || option.Effects == null) return result;

            foreach (EventEffect effect in option.Effects)
            {
                if (effect == null) continue;

                switch (effect.Kind)
                {
                    case EventEffectKind.Nothing:
                        break;

                    case EventEffectKind.Heal:
                    {
                        int healed = Math.Min(Math.Max(0, effect.Value), Math.Max(0, run.MaxHp - run.Hp));
                        run.Hp += healed;
                        result.Lines.Add(healed > 0 ? "+" + healed + " HP" : "Already at full HP");
                        break;
                    }

                    case EventEffectKind.LoseHp:
                    {
                        int lost = Math.Min(Math.Max(0, effect.Value), run.Hp);
                        run.Hp -= lost;
                        result.Lines.Add("-" + lost + " HP");
                        if (run.Hp <= 0) result.Died = true;
                        break;
                    }

                    case EventEffectKind.GainGold:
                    {
                        int gained = Math.Max(0, effect.Value);
                        run.Gold += gained;
                        result.Lines.Add("+" + gained + " gold");
                        break;
                    }

                    case EventEffectKind.LoseGold:
                    {
                        int paid = Math.Min(Math.Max(0, effect.Value), run.Gold);
                        run.Gold -= paid;
                        result.Lines.Add("-" + paid + " gold");
                        break;
                    }

                    case EventEffectKind.MaxHp:
                        ApplyMaxHp(run, effect.Value, result);
                        break;

                    case EventEffectKind.AddCard:
                    {
                        CardData card = FindCard(cards, effect.Target);
                        if (card == null) break;

                        run.Deck.Add(card);
                        result.Lines.Add("Added " + card.Title + " to your deck");
                        break;
                    }
                }
            }

            if (result.Lines.Count == 0) result.Lines.Add("Nothing happens.");
            return result;
        }

        static void ApplyMaxHp(RunState run, int delta, EventResolution result)
        {
            // Raising the ceiling also patches the wound; lowering it drags the current
            // HP down with it, never above the new maximum.
            run.MaxHp = Math.Max(1, run.MaxHp + delta);
            if (delta > 0) run.Hp += delta;
            run.Hp = Math.Min(run.Hp, run.MaxHp);
            result.Lines.Add(Signed(delta, "max HP"));
        }

        static CardData FindCard(IList<CardData> cards, string id)
        {
            if (cards == null || string.IsNullOrEmpty(id)) return null;

            foreach (CardData card in cards)
                if (card != null && string.Equals(card.Id, id, StringComparison.Ordinal)) return card;

            return null;
        }
    }
}
