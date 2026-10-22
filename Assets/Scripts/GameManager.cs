using System.Collections.Generic;
using Prototype.Combat;
using Prototype.Content;
using Prototype.Data;
using Prototype.Mapgen;
using Prototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Prototype
{
    /// <summary>
    /// Entry point. Builds the whole game from code so the scene only needs this one object.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        RectTransform root;
        RunState run;
        CombatSession combat;
        System.Random rng;
        DeckScreen deckScreen;
        MapNode pendingNode;
        ScreenKind screen;

        /// <summary>Which screen currently owns the root, so debug actions know what to rebuild.</summary>
        enum ScreenKind { Map, Combat, Reward, Rest, Placeholder, End }

        /// <summary>Current run. Never null once StartNewRun has run.</summary>
        public RunState Run { get { return run; } }

        /// <summary>Active fight, or null when the run is between rooms.</summary>
        public CombatSession Combat { get { return combat; } }

        /// <summary>True while a fight is still being played (not once it has ended).</summary>
        public bool InCombat { get { return combat != null && !combat.IsOver; } }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;

            EnsureCamera();
            EnsureEventSystem();

            Canvas canvas = UIFactory.MakeCanvas();
            root = UIFactory.MakeRect(canvas.transform, "Screens");
            UIFactory.Stretch(root);

            var dev = UIFactory.MakeRect(canvas.transform, "DevPanel");
            UIFactory.Stretch(dev);
            dev.gameObject.AddComponent<DevPanel>();

            StartNewRun(System.Environment.TickCount);
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;

            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIFactory.Background;
            camera.orthographic = true;
        }

        void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = go.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public void StartNewRun(int seed)
        {
            run = new RunState { Seed = seed };
            run.Map = MapGenerator.Generate(seed);
            run.Deck = CardLibrary.StarterDeck();
            run.Hp = run.MaxHp;
            rng = new System.Random(seed + 7919);
            combat = null;
            pendingNode = null;
            ShowMap();
        }

        void Clear()
        {
            deckScreen = null;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        void ShowMap()
        {
            Clear();
            screen = ScreenKind.Map;
            var screenRoot = UIFactory.MakeRect(root, "MapScreen");
            UIFactory.Stretch(screenRoot);
            screenRoot.gameObject.AddComponent<MapScreen>().Init(run, OnNodeSelected);
        }

        /// <summary>Opens the deck overlay on top of whatever screen is showing.</summary>
        public void ShowDeck()
        {
            if (deckScreen != null || run == null) return;

            var overlay = UIFactory.MakeRect(root, "DeckScreen");
            UIFactory.Stretch(overlay);
            deckScreen = overlay.gameObject.AddComponent<DeckScreen>();
            deckScreen.Init(run, () => deckScreen = null);
        }

        void OnNodeSelected(MapNode node)
        {
            switch (node.Type)
            {
                case NodeType.Combat:
                case NodeType.Elite:
                case NodeType.Boss:
                    StartCombat(node);
                    break;
                case NodeType.Rest:
                    ShowRest(node);
                    break;
                default:
                    ShowPlaceholder(node);
                    break;
            }
        }

        void StartCombat(MapNode node)
        {
            EnemyData enemy = node.Type == NodeType.Boss
                ? EnemyLibrary.Boss()
                : EnemyLibrary.RandomEncounter(rng);

            combat = new CombatSession(run, enemy);

            Clear();
            screen = ScreenKind.Combat;
            var screenRoot = UIFactory.MakeRect(root, "CombatScreen");
            UIFactory.Stretch(screenRoot);
            screenRoot.gameObject.AddComponent<CombatScreen>().Init(combat, () => OnCombatFinished(node));
        }

        void OnCombatFinished(MapNode node)
        {
            if (combat != null && combat.PlayerWon)
            {
                if (node.Type == NodeType.Boss)
                {
                    ShowEnd(true);
                    return;
                }
                ShowReward(node);
                return;
            }

            ShowEnd(false);
        }

        void ShowReward(MapNode node)
        {
            var pool = CardLibrary.RewardPool();
            var choices = new List<CardData>();
            while (choices.Count < 3 && pool.Count > 0)
            {
                int index = rng.Next(pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }

            Clear();
            screen = ScreenKind.Reward;
            pendingNode = node;
            var screenRoot = UIFactory.MakeRect(root, "RewardScreen");
            UIFactory.Stretch(screenRoot);
            screenRoot.gameObject.AddComponent<RewardScreen>().Init(
                choices,
                card =>
                {
                    if (card != null) run.Deck.Add(card);
                    Advance(node);
                },
                () => Advance(node));
        }

        void ShowRest(MapNode node)
        {
            Clear();
            screen = ScreenKind.Rest;
            pendingNode = node;
            SimpleScreens.Rest(root, run,
                () =>
                {
                    int heal = Mathf.RoundToInt(run.MaxHp * 0.3f);
                    run.Hp = Mathf.Min(run.MaxHp, run.Hp + heal);
                    Advance(node);
                },
                () => Advance(node));
        }

        void ShowPlaceholder(MapNode node)
        {
            Clear();
            screen = ScreenKind.Placeholder;
            pendingNode = node;
            SimpleScreens.Placeholder(root, node.Type, () => Advance(node));
        }

        void ShowEnd(bool victory)
        {
            Clear();
            screen = ScreenKind.End;
            pendingNode = null;
            SimpleScreens.End(root, victory, () => StartNewRun(rng.Next()));
        }

        void Advance(MapNode node)
        {
            run.Current = node;
            node.Visited = true;
            combat = null;
            pendingNode = null;
            ShowMap();
        }

        // --- Playtest helpers (used by DevPanel, safe to call at any time) ----

        public void DebugHeal()
        {
            if (run == null) return;
            run.Hp = run.MaxHp;

            if (screen == ScreenKind.Combat && combat != null) combat.NotifyChanged();
            else if (screen == ScreenKind.Map) ShowMap();
        }

        public void DebugAddCard()
        {
            if (run == null) return;

            var pool = CardLibrary.RewardPool();
            run.Deck.Add(pool[rng.Next(pool.Count)]);

            if (screen == ScreenKind.Map) ShowMap();
        }

        public void DebugWinCombat()
        {
            if (combat != null && !combat.IsOver) combat.ForceVictory();
        }

        public void DebugAddEnergy()
        {
            if (combat != null && !combat.IsOver) combat.AddEnergy(1);
        }

        public void DebugSkipRoom()
        {
            if (screen == ScreenKind.Combat)
            {
                if (combat != null && !combat.IsOver) combat.ForceVictory();
                return;
            }

            if (screen == ScreenKind.Reward || screen == ScreenKind.Rest || screen == ScreenKind.Placeholder)
            {
                if (pendingNode != null) Advance(pendingNode);
                return;
            }

            if (screen != ScreenKind.Map) return;

            var options = new List<MapNode>();
            if (run.Current == null) options.AddRange(run.Map.Floors[0]);
            else options.AddRange(run.Current.Next);
            if (options.Count == 0) return;

            OnNodeSelected(options[rng.Next(options.Count)]);
        }

        public void DebugRestart(int seed)
        {
            StartNewRun(seed);
        }
    }
}
