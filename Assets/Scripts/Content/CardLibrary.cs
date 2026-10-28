using System.Collections.Generic;
using Prototype.Data;

namespace Prototype.Content
{
    /// <summary>
    /// Thin facade over <see cref="ContentDatabase"/>: the rest of the game asks for
    /// "the starter deck" or "a random encounter" and never touches CSV details.
    /// </summary>
    public static class CardLibrary
    {
        public static List<CardData> StarterDeck()
        {
            var deck = new List<CardData>();
            foreach (CardData card in ContentDatabase.Cards)
            {
                if (!card.IsStarter) continue;
                for (int copy = 0; copy < card.Copies; copy++) deck.Add(card);
            }
            return deck;
        }

        public static List<CardData> RewardPool()
        {
            var pool = new List<CardData>();
            foreach (CardData card in ContentDatabase.Cards)
                if (card.IsReward) pool.Add(card);
            return pool;
        }
    }

    public static class EnemyLibrary
    {
        /// <summary>
        /// Normal room: 55% of the time one regular enemy, otherwise a pack of two
        /// weaker ones. Both kinds are picked from the CSV by role.
        /// </summary>
        public static List<EnemyData> RandomEncounter(System.Random rng)
        {
            List<EnemyData> solo = ByRole(EnemyRole.Normal);
            List<EnemyData> packs = ByRole(EnemyRole.Pack);

            if (rng.Next(100) < 55)
            {
                if (solo.Count > 0) return new List<EnemyData> { solo[rng.Next(solo.Count)] };
                if (packs.Count > 0) return new List<EnemyData> { packs[rng.Next(packs.Count)] };
                return new List<EnemyData>();
            }

            if (packs.Count == 0)
            {
                if (solo.Count == 0) return new List<EnemyData>();
                return new List<EnemyData> { solo[rng.Next(solo.Count)], solo[rng.Next(solo.Count)] };
            }

            return new List<EnemyData> { packs[rng.Next(packs.Count)], packs[rng.Next(packs.Count)] };
        }

        public static EnemyData Boss(System.Random rng)
        {
            List<EnemyData> bosses = ByRole(EnemyRole.Boss);
            return bosses.Count == 0 ? null : bosses[rng.Next(bosses.Count)];
        }

        static List<EnemyData> ByRole(EnemyRole role)
        {
            var list = new List<EnemyData>();
            foreach (EnemyData enemy in ContentDatabase.Enemies)
                if (enemy.Role == role) list.Add(enemy);
            return list;
        }
    }
}
