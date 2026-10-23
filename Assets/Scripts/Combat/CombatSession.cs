using System;
using System.Collections.Generic;
using Prototype.Data;
using UnityEngine;

namespace Prototype.Combat
{
    /// <summary>
    /// Turn based card combat: 3 energy, draw 5, block resets every turn,
    /// Vulnerable multiplies incoming damage by 1.5. A fight can hold several
    /// enemies; cards hit the selected target unless they say "all enemies".
    /// </summary>
    public class CombatSession
    {
        public const int HandSize = 5;
        public const int EnergyPerTurn = 3;
        public const int MaxHandSize = 10;

        public readonly RunState Run;
        public readonly List<EnemyInstance> Enemies = new List<EnemyInstance>();

        /// <summary>Enemy that single target cards hit. Null once every enemy is dead.</summary>
        public EnemyInstance Target { get; private set; }

        public int Energy;
        public int PlayerBlock;
        public int PlayerVulnerable;

        public readonly List<CardData> DrawPile = new List<CardData>();
        public readonly List<CardData> Hand = new List<CardData>();
        public readonly List<CardData> DiscardPile = new List<CardData>();

        public bool IsOver;
        public bool PlayerWon;
        public string LastEvent = "";

        public event Action Changed;

        /// <summary>
        /// Raised every time damage is resolved, even if it is fully blocked.
        /// The enemy is null when the player received the hit.
        /// </summary>
        public event Action<int, EnemyInstance> DamageDealt;

        readonly System.Random rng;

        public CombatSession(RunState run, IList<EnemyData> enemies)
        {
            Run = run;
            rng = new System.Random(run.Seed * 397 + run.Hp);

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData data = enemies[i];
                Enemies.Add(new EnemyInstance(data, data.MaxHp, UniqueTitle(enemies, i)));
            }

            Target = FirstAlive();
            DrawPile.AddRange(run.Deck);
            Shuffle(DrawPile);
            PlayerBlock = 0;
            StartPlayerTurn();
        }

        /// <summary>Changes the card target. Dead enemies and no-ops are ignored.</summary>
        public void SelectTarget(EnemyInstance enemy)
        {
            if (enemy == null || !enemy.IsAlive || enemy == Target) return;
            Target = enemy;
            NotifyChanged();
        }

        public EnemyInstance FirstAlive()
        {
            foreach (EnemyInstance enemy in Enemies)
                if (enemy.IsAlive) return enemy;
            return null;
        }

        public bool AllEnemiesDead { get { return FirstAlive() == null; } }

        public bool CanPlay(CardData card)
        {
            return !IsOver && card != null && card.Cost <= Energy;
        }

        public void PlayCard(CardData card)
        {
            if (!CanPlay(card)) return;

            if (card.Kind == CardKind.Attack && (Target == null || !Target.IsAlive))
                Target = FirstAlive();

            Energy -= card.Cost;
            Hand.Remove(card);
            DiscardPile.Add(card);

            LastEvent = "";
            ApplyEffects(card);
            CheckCombatEnd();

            Changed?.Invoke();
        }

        public void EndTurn()
        {
            if (IsOver) return;

            LastEvent = "";
            EnemyTurn();

            if (Run.Hp <= 0)
            {
                Run.Hp = 0;
                IsOver = true;
                PlayerWon = false;
                Changed?.Invoke();
                return;
            }

            foreach (EnemyInstance enemy in Enemies)
            {
                if (!enemy.IsAlive) continue;
                enemy.AdvanceIntent();
                if (enemy.Vulnerable > 0) enemy.Vulnerable--;
            }

            StartPlayerTurn();
        }

        /// <summary>Forces the view to rebuild, e.g. after a debug action changed state.</summary>
        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>Debug helper: grants energy without playing a card.</summary>
        public void AddEnergy(int amount)
        {
            if (amount == 0 || IsOver) return;
            Energy = Mathf.Max(0, Energy + amount);
            NotifyChanged();
        }

        /// <summary>Debug helper: ends the fight as a victory without resolving damage.</summary>
        public void ForceVictory()
        {
            if (IsOver) return;

            foreach (EnemyInstance enemy in Enemies)
            {
                enemy.Hp = 0;
                enemy.Block = 0;
            }

            Target = null;
            IsOver = true;
            PlayerWon = true;
            LastEvent = "Victory (debug).";
            NotifyChanged();
        }

        /// <summary>Debug helper: destroys the selected enemy so the next one can be tested.</summary>
        public void DebugKillTarget()
        {
            if (IsOver || Target == null) return;

            EnemyInstance victim = Target;
            victim.Hp = 0;
            victim.Block = 0;
            victim.Vulnerable = 0;
            Log(victim.Title + " is destroyed (debug).");

            DamageDealt?.Invoke(0, victim);
            Target = FirstAlive();
            CheckCombatEnd();
            NotifyChanged();
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
                        DealDamageToEnemy(Target, effect.Value);
                        break;
                    case EffectKind.DamageAll:
                        foreach (EnemyInstance enemy in Enemies)
                            DealDamageToEnemy(enemy, effect.Value);
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
                        if (Target != null) Target.Vulnerable += effect.Value;
                        break;
                }
            }
        }

        void DealDamageToEnemy(EnemyInstance enemy, int amount)
        {
            if (enemy == null || !enemy.IsAlive) return;

            int damage = enemy.Vulnerable > 0 ? amount * 3 / 2 : amount;
            int blocked = Mathf.Min(enemy.Block, damage);
            enemy.Block -= blocked;
            int dealt = damage - blocked;
            enemy.Hp = Mathf.Max(0, enemy.Hp - dealt);

            Log("You hit " + enemy.Title + " for " + dealt + ".");
            if (!enemy.IsAlive) Log(enemy.Title + " falls.");

            DamageDealt?.Invoke(dealt, enemy);

            if (!enemy.IsAlive && Target == enemy) Target = FirstAlive();
        }

        void EnemyTurn()
        {
            foreach (EnemyInstance enemy in Enemies)
            {
                if (!enemy.IsAlive) continue;

                enemy.Block = 0;

                EnemyIntent intent = enemy.Intent;
                if (intent == null)
                {
                    Log(enemy.Title + " hesitates.");
                    continue;
                }

                if (intent.Kind == IntentKind.Block)
                {
                    enemy.Block += intent.Value;
                    Log(enemy.Title + " gains " + intent.Value + " block.");
                    continue;
                }

                int damage = PlayerVulnerable > 0 ? intent.Value * 3 / 2 : intent.Value;
                int blocked = Mathf.Min(PlayerBlock, damage);
                PlayerBlock -= blocked;
                int dealt = damage - blocked;

                Run.Hp = Mathf.Max(0, Run.Hp - dealt);
                Log(enemy.Title + " hits you for " + dealt + ".");
                DamageDealt?.Invoke(dealt, null);

                if (Run.Hp <= 0) return;
            }
        }

        void CheckCombatEnd()
        {
            if (IsOver) return;

            if (AllEnemiesDead)
            {
                Target = null;
                IsOver = true;
                PlayerWon = true;
                Log("Victory.");
                return;
            }

            if (Run.Hp <= 0)
            {
                Run.Hp = 0;
                IsOver = true;
                PlayerWon = false;
            }
        }

        void Log(string line)
        {
            LastEvent = string.IsNullOrEmpty(LastEvent) ? line : LastEvent + "\n" + line;
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

        /// <summary>Adds "A"/"B" suffixes when the same enemy appears twice in one fight.</summary>
        static string UniqueTitle(IList<EnemyData> enemies, int index)
        {
            EnemyData data = enemies[index];
            int count = 0;
            int rank = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Title != data.Title) continue;
                if (i < index) rank++;
                count++;
            }

            return count > 1 ? data.Title + " " + (char)('A' + rank) : data.Title;
        }
    }
}
