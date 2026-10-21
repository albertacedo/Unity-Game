using System.Collections.Generic;
using Prototype.Data;
using UnityEngine;

namespace Prototype.Content
{
    /// <summary>
    /// Content lives here as plain code so the prototype can be tuned in one place.
    /// Once the card list grows, these become .asset files and this class goes away.
    /// </summary>
    public static class CardLibrary
    {
        static CardData Make(string id, string title, string text, int cost, CardKind kind,
            params CardEffect[] effects)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.name = id;
            card.Id = id;
            card.Title = title;
            card.Text = text;
            card.Cost = cost;
            card.Kind = kind;
            card.Effects = effects;
            return card;
        }

        public static List<CardData> StarterDeck()
        {
            return new List<CardData>
            {
                Strike(), Strike(), Strike(), Strike(),
                Defend(), Defend(), Defend(), Defend(),
                Bash()
            };
        }

        public static List<CardData> RewardPool()
        {
            return new List<CardData>
            {
                Cleave(), PommelStrike(), IronWave(), ShrugItOff(),
                Thunderclap(), HeavyBlade(), BattleTrance()
            };
        }

        static CardData Strike()
        {
            return Make("strike", "Strike", "Deal 6 damage.", 1, CardKind.Attack,
                new CardEffect(EffectKind.Damage, 6));
        }

        static CardData Defend()
        {
            return Make("defend", "Defend", "Gain 5 Block.", 1, CardKind.Skill,
                new CardEffect(EffectKind.Block, 5));
        }

        static CardData Bash()
        {
            return Make("bash", "Bash", "Deal 8 damage.\nApply 2 Vulnerable.", 2, CardKind.Attack,
                new CardEffect(EffectKind.Damage, 8),
                new CardEffect(EffectKind.Vulnerable, 2));
        }

        static CardData Cleave()
        {
            return Make("cleave", "Cleave", "Deal 8 damage.", 1, CardKind.Attack,
                new CardEffect(EffectKind.Damage, 8));
        }

        static CardData PommelStrike()
        {
            return Make("pommel_strike", "Pommel Strike", "Deal 9 damage.\nDraw 1 card.", 1, CardKind.Attack,
                new CardEffect(EffectKind.Damage, 9),
                new CardEffect(EffectKind.Draw, 1));
        }

        static CardData IronWave()
        {
            return Make("iron_wave", "Iron Wave", "Gain 5 Block.\nDeal 5 damage.", 1, CardKind.Attack,
                new CardEffect(EffectKind.Block, 5),
                new CardEffect(EffectKind.Damage, 5));
        }

        static CardData ShrugItOff()
        {
            return Make("shrug_it_off", "Shrug It Off", "Gain 8 Block.\nDraw 1 card.", 1, CardKind.Skill,
                new CardEffect(EffectKind.Block, 8),
                new CardEffect(EffectKind.Draw, 1));
        }

        static CardData Thunderclap()
        {
            return Make("thunderclap", "Thunderclap", "Deal 4 damage.\nApply 1 Vulnerable.", 1, CardKind.Attack,
                new CardEffect(EffectKind.Damage, 4),
                new CardEffect(EffectKind.Vulnerable, 1));
        }

        static CardData HeavyBlade()
        {
            return Make("heavy_blade", "Heavy Blade", "Deal 14 damage.", 2, CardKind.Attack,
                new CardEffect(EffectKind.Damage, 14));
        }

        static CardData BattleTrance()
        {
            return Make("battle_trance", "Battle Trance", "Draw 3 cards.", 0, CardKind.Skill,
                new CardEffect(EffectKind.Draw, 3));
        }
    }

    public static class EnemyLibrary
    {
        static EnemyData Make(string id, string title, int maxHp, params EnemyIntent[] intents)
        {
            EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.name = id;
            enemy.Id = id;
            enemy.Title = title;
            enemy.MaxHp = maxHp;
            enemy.Intents = intents;
            return enemy;
        }

        public static EnemyData RandomEncounter(System.Random rng)
        {
            var pool = new List<EnemyData>
            {
                JawWorm(),
                Cultist()
            };
            return pool[rng.Next(pool.Count)];
        }

        public static EnemyData Boss()
        {
            return Make("the_guardian", "The Guardian", 90,
                new EnemyIntent(IntentKind.Attack, 14),
                new EnemyIntent(IntentKind.Block, 10),
                new EnemyIntent(IntentKind.Attack, 18));
        }

        static EnemyData JawWorm()
        {
            return Make("jaw_worm", "Jaw Worm", 44,
                new EnemyIntent(IntentKind.Attack, 11),
                new EnemyIntent(IntentKind.Block, 6),
                new EnemyIntent(IntentKind.Attack, 7));
        }

        static EnemyData Cultist()
        {
            return Make("cultist", "Cultist", 50,
                new EnemyIntent(IntentKind.Attack, 6),
                new EnemyIntent(IntentKind.Attack, 9));
        }
    }
}
