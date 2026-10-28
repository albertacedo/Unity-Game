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
            ContentDatabase.Reload();
            run = new RunState { Seed = seed };
            run.Map = MapGenerator.Generate(seed);
            run.Deck = CardLibrary.StarterDeck();
            run.Hp = run.MaxHp;
            run.Current = run.Map.Start;
            RevealVisibleNodes();
            rng = new System.Random(seed + 7919);
            combat = null;
            pendingNode = null;
            ShowMap();
        }

        public void DebugToggleFogOfWar()
        {
            if (run == null) return;
            run.FogOfWar = !run.FogOfWar;
            RevealVisibleNodes();
            RefreshMapForDebug();
        }

        public void DebugCycleRevealDistance()
        {
            if (run == null) return;
            run.RevealDistance = run.RevealDistance >= 5 ? 1 : run.RevealDistance + 1;
            RevealVisibleNodes();
            RefreshMapForDebug();
        }

        public void DebugToggleBacktracking()
        {
            if (run == null) return;
            run.AllowBacktracking = !run.AllowBacktracking;
            RefreshMapForDebug();
        }

        void RefreshMapForDebug()
        {
            if (screen == ScreenKind.Map) ShowMap();
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
            // Visited rooms are safe traversal nodes. They never replay their content.
            if (node.Visited)
            {
                run.Current = node;
                RevealVisibleNodes();
                ShowMap();
                return;
            }

            switch (node.Type)
            {
                case NodeType.Start:
                    break;
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
            List<EnemyData> enemies = node.Type == NodeType.Boss
                ? BossEncounter()
                : EnemyLibrary.RandomEncounter(rng);

            enemies.RemoveAll(enemy => enemy == null);
            if (enemies.Count == 0)
            {
                Debug.LogError("Content: no enemy for a " + node.Type +
                               " room. Check Assets/Resources/Content/enemies.csv.");
                Advance(node);
                return;
            }

            combat = new CombatSession(run, enemies);

            Clear();
            screen = ScreenKind.Combat;
            var screenRoot = UIFactory.MakeRect(root, "CombatScreen");
            UIFactory.Stretch(screenRoot);
            screenRoot.gameObject.AddComponent<CombatScreen>().Init(combat, () => OnCombatFinished(node));
        }

        List<EnemyData> BossEncounter()
        {
            EnemyData boss = EnemyLibrary.Boss(rng);
            return boss == null ? new List<EnemyData>() : new List<EnemyData> { boss };
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
            node.TypeRevealed = true;
            RevealVisibleNodes();
            combat = null;
            pendingNode = null;
            ShowMap();
        }

        void RevealVisibleNodes()
        {
            if (run == null || run.Current == null) return;

            // TypeRevealed is the current accumulated visibility: every visited room is a
            // source for the reveal radius. Visited rooms keep their identity because their
            // content has already been experienced.
            foreach (MapNode mapNode in run.Map.Nodes())
            {
                if (!mapNode.Visited && mapNode.Type != NodeType.Boss)
                    mapNode.TypeRevealed = false;
            }

            if (!run.FogOfWar) return;

            var queue = new Queue<MapNode>();
            var distances = new Dictionary<MapNode, int>();

            foreach (MapNode mapNode in run.Map.Nodes())
            {
                if (!mapNode.Visited) continue;
                queue.Enqueue(mapNode);
                distances[mapNode] = 0;
            }

            while (queue.Count > 0)
            {
                MapNode node = queue.Dequeue();
                int distance = distances[node];
                node.TypeRevealed = true;
                if (distance >= run.RevealDistance) continue;

                foreach (MapNode neighbour in node.Next)
                {
                    if (distances.ContainsKey(neighbour)) continue;
                    distances[neighbour] = distance + 1;
                    queue.Enqueue(neighbour);
                }
            }
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

        public void DebugKillTarget()
        {
            if (combat != null && !combat.IsOver) combat.DebugKillTarget();
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
            if (run.Current != null)
            {
                foreach (MapNode node in run.Current.Next)
                    if (!node.Visited) options.Add(node);
            }
            if (options.Count == 0) return;

            OnNodeSelected(options[rng.Next(options.Count)]);
        }

        /// <summary>Ends the run on purpose: dead end on the map, or a manual give-up.</summary>
        public void AbandonRun()
        {
            if (run == null) return;
            ShowEnd(false);
        }

        public void DebugRestart(int seed)
        {
            StartNewRun(seed);
        }
    }
}
