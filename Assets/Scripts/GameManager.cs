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

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;

            EnsureCamera();
            EnsureEventSystem();

            Canvas canvas = UIFactory.MakeCanvas();
            root = UIFactory.MakeRect(canvas.transform, "Screens");
            UIFactory.Stretch(root);

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
            ShowMap();
        }

        void Clear()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        void ShowMap()
        {
            Clear();
            var screen = UIFactory.MakeRect(root, "MapScreen");
            UIFactory.Stretch(screen);
            screen.gameObject.AddComponent<MapScreen>().Init(run, OnNodeSelected);
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
            var screen = UIFactory.MakeRect(root, "CombatScreen");
            UIFactory.Stretch(screen);
            screen.gameObject.AddComponent<CombatScreen>().Init(combat, () => OnCombatFinished(node));
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
            var screen = UIFactory.MakeRect(root, "RewardScreen");
            UIFactory.Stretch(screen);
            screen.gameObject.AddComponent<RewardScreen>().Init(
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
            SimpleScreens.Placeholder(root, node.Type, () => Advance(node));
        }

        void ShowEnd(bool victory)
        {
            Clear();
            SimpleScreens.End(root, victory, () => StartNewRun(rng.Next()));
        }

        void Advance(MapNode node)
        {
            run.Current = node;
            node.Visited = true;
            combat = null;
            ShowMap();
        }
    }
}
