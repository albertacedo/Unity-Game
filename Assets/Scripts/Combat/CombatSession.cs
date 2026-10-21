using System;
using System.Collections.Generic;
using Prototype.Data;
using UnityEngine;

namespace Prototype.Combat
{
    /// <summary>
    /// Turn based card combat: 3 energy, draw 5, block resets every turn,
    /// Vulnerable multiplies incoming damage by 1.5.
    /// </summary>
    public class CombatSession
    {
        public const int HandSize = 5;
        public const int EnergyPerTurn = 3;
        public const int MaxHandSize = 10;

        public readonly RunState Run;
        public readonly EnemyData Enemy;

        public int EnemyMaxHp;
        public int EnemyHp;
        public int EnemyBlock;
        public int EnemyVulnerable;

        public int Energy;
        public int PlayerBlock;
        public int PlayerVulnerable;

        public int IntentIndex;

        public readonly List<CardData> DrawPile = new List<CardData>();
        public readonly List<CardData> Hand = new List<CardData>();
        public readonly List<CardData> DiscardPile = new List<CardData>();

        public bool IsOver;
        public bool PlayerWon;
        public string LastEvent = "";

        public event Action Changed;

        readonly System.Random rng;

        public CombatSession(RunState run, EnemyData enemy)
        {
            Run = run;
            Enemy = enemy;
            EnemyMaxHp = enemy.MaxHp;
            EnemyHp = enemy.MaxHp;
            rng = new System.Random(run.Seed * 397 + run.Hp);

            DrawPile.AddRange(run.Deck);
            Shuffle(DrawPile);
            PlayerBlock = 0;
            StartPlayerTurn();
        }

        public EnemyIntent CurrentIntent
        {
            get
            {
                if (Enemy.Intents == null || Enemy.Intents.Length == 0) return null;
                return Enemy.Intents[IntentIndex % Enemy.Intents.Length];
            }
        }

        public bool CanPlay(CardData card)
        {
            return !IsOver && card != null && card.Cost <= Energy;
        }

        public void PlayCard(CardData card)
        {
            if (!CanPlay(card)) return;

            Energy -= card.Cost;
            Hand.Remove(card);
            DiscardPile.Add(card);

            ApplyEffects(card);

            if (EnemyHp <= 0)
            {
                EnemyHp = 0;
                IsOver = true;
                PlayerWon = true;
            }

            Changed?.Invoke();
        }

        public void EndTurn()
        {
            if (IsOver) return;

            EnemyTurn();

            if (Run.Hp <= 0)
            {
                Run.Hp = 0;
                IsOver = true;
                PlayerWon = false;
                Changed?.Invoke();
                return;
            }

            if (Enemy.Intents != null && Enemy.Intents.Length > 0)
                IntentIndex = (IntentIndex + 1) % Enemy.Intents.Length;
            if (EnemyVulnerable > 0) EnemyVulnerable--;

            StartPlayerTurn();
        }

        void StartPlayerTurn()
        {
            PlayerBlock = 0;
            Energy = EnergyPerTurn;
            DiscardPile.AddRange(Hand);
            Hand.Clear();
            DrawCards(HandSize);
            if (PlayerVulnerable > 0) PlayerVulnerable--;
            Changed?.Invoke();
        }

        void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0) return;
                    DrawPile.AddRange(DiscardPile);
                    DiscardPile.Clear();
                    Shuffle(DrawPile);
                }

                CardData card = DrawPile[0];
                DrawPile.RemoveAt(0);

                if (Hand.Count >= MaxHandSize)
                {
                    DiscardPile.Add(card);
                    continue;
                }

                Hand.Add(card);
            }
        }

        void ApplyEffects(CardData card)
        {
            if (card.Effects == null) return;

            foreach (CardEffect effect in card.Effects)
            {
                switch (effect.Kind)
                {
                    case EffectKind.Damage:
                        DealDamageToEnemy(effect.Value);
                        break;
                    case EffectKind.Block:
                        PlayerBlock += effect.Value;
                        break;
                    case EffectKind.Draw:
                        DrawCards(effect.Value);
                        break;
                    case EffectKind.GainEnergy:
                        Energy += effect.Value;
                        break;
                    case EffectKind.Vulnerable:
                        EnemyVulnerable += effect.Value;
                        break;
                }
            }
        }

        void DealDamageToEnemy(int amount)
        {
            int damage = EnemyVulnerable > 0 ? amount * 3 / 2 : amount;
            int blocked = Mathf.Min(EnemyBlock, damage);
            EnemyBlock -= blocked;
            int dealt = damage - blocked;
            EnemyHp = Mathf.Max(0, EnemyHp - dealt);
            LastEvent = "You deal " + dealt + " damage.";
        }

        void EnemyTurn()
        {
            EnemyBlock = 0;

            EnemyIntent intent = CurrentIntent;
            if (intent == null)
            {
                LastEvent = Enemy.Title + " hesitates.";
                return;
            }

            if (intent.Kind == IntentKind.Block)
            {
                EnemyBlock += intent.Value;
                LastEvent = Enemy.Title + " gains " + intent.Value + " block.";
                return;
            }

            int damage = PlayerVulnerable > 0 ? intent.Value * 3 / 2 : intent.Value;
            int blocked = Mathf.Min(PlayerBlock, damage);
            PlayerBlock -= blocked;
            int dealt = damage - blocked;
            Run.Hp = Mathf.Max(0, Run.Hp - dealt);
            LastEvent = Enemy.Title + " hits you for " + dealt + ".";
        }

        void Shuffle(List<CardData> pile)
        {
            for (int i = pile.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CardData tmp = pile[i];
                pile[i] = pile[j];
                pile[j] = tmp;
            }
        }
    }
}
