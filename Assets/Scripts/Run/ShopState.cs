using System;
using System.Collections.Generic;
using Prototype.Data;

namespace Prototype
{
    /// <summary>One card on the counter: what it is, what it costs and whether it sold.</summary>
    public class ShopOffer
    {
        public CardData Card;
        public int Price;
        public bool Sold;
    }

    /// <summary>
    /// Rules of a shop room: what is on sale, what it costs and what buying does.
    /// Plain C#, no UGUI: <see cref="Prototype.UI.ShopScreen"/> only renders this.
    /// </summary>
    public class ShopState
    {
        public const int OfferCount = 4;
        public const int HealPrice = 20;
        public const int HealAmount = 15;

        public readonly List<ShopOffer> Cards = new List<ShopOffer>();
        public bool HealSold;

        /// <summary>
        /// Fills the counter with a seeded, duplicate-free sample of the pool, so the same
        /// run and room always see the same stock.
        /// </summary>
        public static ShopState Create(IList<CardData> pool, int seed, int roomKey, int offers = OfferCount)
        {
            var shop = new ShopState();
            if (pool == null || pool.Count == 0) return shop;

            var cards = new List<CardData>();
            foreach (CardData card in pool)
                if (card != null) cards.Add(card);

            var rng = new Random(unchecked(seed * 7919 + roomKey * 37 + 11));
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CardData swap = cards[i];
                cards[i] = cards[j];
                cards[j] = swap;
            }

            int count = Math.Min(offers, cards.Count);
            for (int i = 0; i < count; i++)
                shop.Cards.Add(new ShopOffer { Card = cards[i], Price = PriceOf(cards[i]) });

            return shop;
        }

        /// <summary>Cheap for 0-cost cards, a real decision for 2-cost ones.</summary>
        public static int PriceOf(CardData card)
        {
            if (card == null) return 0;
            return 30 + Math.Max(0, card.Cost) * 15;
        }

        public bool CanBuy(ShopOffer offer, RunState run)
        {
            return offer != null && !offer.Sold && run != null && run.Gold >= offer.Price;
        }

        /// <summary>Takes the gold, adds the card to the run deck and marks the offer sold.</summary>
        public bool Buy(ShopOffer offer, RunState run)
        {
            if (!CanBuy(offer, run)) return false;

            run.Gold -= offer.Price;
            run.Deck.Add(offer.Card);
            offer.Sold = true;
            return true;
        }

        public bool CanHeal(RunState run)
        {
            return !HealSold && run != null && run.Gold >= HealPrice && run.Hp < run.MaxHp;
        }

        /// <summary>One bandage per visit, clamped at max HP.</summary>
        public bool Heal(RunState run)
        {
            if (!CanHeal(run)) return false;

            run.Gold -= HealPrice;
            run.Hp = Math.Min(run.MaxHp, run.Hp + HealAmount);
            HealSold = true;
            return true;
        }
    }
}
