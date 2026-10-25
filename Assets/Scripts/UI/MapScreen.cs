using System;
using System.Collections.Generic;
using Prototype.Mapgen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    public class MapScreen : MonoBehaviour
    {
        static readonly Vector2 NodeSize = new Vector2(70f, 30f);

        RunState run;
        Action<MapNode> onSelect;

        public void Init(RunState runState, Action<MapNode> onSelectNode)
        {
            run = runState;
            onSelect = onSelectNode;
            Build();
        }

        void Build()
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);

            var title = UIFactory.MakeLabel(background, "Title", "ACT I  ·  THE HOLLOW", 32f,
                UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(900f, 46f));

            var stats = UIFactory.MakeLabel(background, "Stats", StatsText(), 20f,
                UIFactory.Ink, TextAlignmentOptions.Left);
            UIFactory.Place(stats.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -26f), new Vector2(460f, 110f));

            var hint = UIFactory.MakeLabel(background, "Hint", HintText(), 18f,
                UIFactory.Muted, TextAlignmentOptions.Right);
            UIFactory.Place(hint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-38f, -26f), new Vector2(700f, 96f));

            var deck = UIFactory.MakeButton(background, "Deck",
                "VIEW DECK  " + run.Deck.Count, new Color(0.30f, 0.26f, 0.38f), 20f);
            UIFactory.Place((RectTransform)deck.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-38f, 26f), new Vector2(240f, 50f));
            deck.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ShowDeck();
            });

            var area = UIFactory.MakeRect(background, "MapArea");
            UIFactory.Place(area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(1460f, 800f));

            foreach (MapNode node in run.Map.Nodes())
            {
                foreach (MapNode next in node.Next)
                {
                    if (next.Ring <= node.Ring) continue;   // each edge is stored on both ends
                    UIFactory.MakeLine(area, PositionOf(node), PositionOf(next),
                        new Color(0.29f, 0.27f, 0.34f), 3f);
                }
            }

            foreach (MapNode node in run.Map.Nodes())
                CreateNode(area, node);

            if (Options().Count == 0) CreateDeadEnd(background);
        }

        Vector2 PositionOf(MapNode node)
        {
            MapLayout.Project(node.Ring, node.Angle, out float x, out float y);
            return new Vector2(x, y);
        }

        string StatsText()
        {
            return "HP  " + run.Hp + " / " + run.MaxHp +
                   "\nGold  " + run.Gold +
                   "\nDeck  " + run.Deck.Count + " cards" +
                   "\nRooms  " + run.Map.VisitedCount() + " / " + CountNodes();
        }

        string HintText()
        {
            string ring = run.Current == null ? "the centre"
                : "ring " + run.Current.Ring + " of " + (run.Map.RingCount - 1);

            return "Click any connected room.  ·  You are in " + ring +
                   "\nRooms you leave cannot be entered again." +
                   "\nSomewhere on the outer ring: " + run.Map.Bosses.Count + " bosses.";
        }

        int CountNodes()
        {
            int count = 0;
            foreach (MapNode ignored in run.Map.Nodes()) count++;
            return count;
        }

        /// <summary>Unvisited rooms connected to the room the player is standing in.</summary>
        List<MapNode> Options()
        {
            var options = new List<MapNode>();
            if (run.Current == null) return options;

            foreach (MapNode node in run.Current.Next)
                if (!node.Visited) options.Add(node);

            return options;
        }

        bool IsAvailable(MapNode node)
        {
            if (node.Visited || run.Current == null) return false;
            return run.Current.Next.Contains(node);
        }

        void CreateNode(RectTransform area, MapNode node)
        {
            bool available = IsAvailable(node);
            bool current = node == run.Current;
            Color color = ColorFor(node.Type);
            Color textColor = UIFactory.Ink;

            if (node.Visited)
            {
                color = new Color(0.15f, 0.14f, 0.18f);
                textColor = new Color(0.46f, 0.44f, 0.51f);
            }
            else if (!available)
            {
                color = Dim(color, 0.45f);
                textColor = UIFactory.Muted;
            }

            var button = UIFactory.MakeButton(area, "Node_" + node.Ring + "_" + node.Index,
                LabelFor(node.Type), color, 12f);
            var rt = (RectTransform)button.transform;
            UIFactory.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                PositionOf(node), NodeSize);

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = textColor;

            if (current)
            {
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = UIFactory.Gold;
                outline.effectDistance = new Vector2(3f, 3f);
            }

            button.interactable = available;
            if (available)
            {
                MapNode captured = node;
                button.onClick.AddListener(() => onSelect(captured));
            }
        }

        void CreateDeadEnd(RectTransform background)
        {
            var text = UIFactory.MakeLabel(background, "DeadEnd",
                "No unvisited room is connected to this one. The run ends here.", 20f,
                UIFactory.Danger);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 96f), new Vector2(760f, 30f));

            var giveUp = UIFactory.MakeButton(background, "GiveUp", "END RUN",
                new Color(0.42f, 0.22f, 0.24f), 20f);
            UIFactory.Place((RectTransform)giveUp.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(220f, 50f));
            giveUp.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.AbandonRun();
            });
        }

        static string LabelFor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Start: return "START";
                case NodeType.Combat: return "Fight";
                case NodeType.Elite: return "Elite";
                case NodeType.Rest: return "Rest";
                case NodeType.Shop: return "Shop";
                case NodeType.Event: return "Event";
                default: return "BOSS";
            }
        }

        static Color ColorFor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Start: return new Color(0.32f, 0.38f, 0.48f);
                case NodeType.Combat: return new Color(0.60f, 0.25f, 0.24f);
                case NodeType.Elite: return new Color(0.72f, 0.20f, 0.42f);
                case NodeType.Rest: return new Color(0.24f, 0.52f, 0.34f);
                case NodeType.Shop: return new Color(0.66f, 0.54f, 0.22f);
                case NodeType.Event: return new Color(0.24f, 0.42f, 0.66f);
                default: return new Color(0.48f, 0.26f, 0.64f);
            }
        }

        static Color Dim(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
        }
    }
}
