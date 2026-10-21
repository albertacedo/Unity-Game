using System;
using Prototype.Mapgen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    public class MapScreen : MonoBehaviour
    {
        const float ColumnSpacing = 152f;
        const float FloorSpacing = 112f;

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

            var title = UIFactory.MakeLabel(background, "Title", "ACT I  ·  THE SPIRE", 32f,
                UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -32f), new Vector2(900f, 46f));

            var stats = UIFactory.MakeLabel(background, "Stats", StatsText(), 21f,
                UIFactory.Ink, TextAlignmentOptions.Left);
            UIFactory.Place(stats.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -28f), new Vector2(520f, 96f));

            var hint = UIFactory.MakeLabel(background, "Hint", HintText(), 19f,
                UIFactory.Muted, TextAlignmentOptions.Right);
            UIFactory.Place(hint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-38f, -28f), new Vector2(620f, 60f));

            var area = UIFactory.MakeRect(background, "MapArea");
            UIFactory.Place(area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -26f), new Vector2(1400f, 730f));

            foreach (var floor in run.Map.Floors)
            {
                foreach (var node in floor)
                {
                    foreach (var next in node.Next)
                    {
                        UIFactory.MakeLine(area, PositionOf(node), PositionOf(next),
                            new Color(0.33f, 0.31f, 0.38f), 4f);
                    }
                }
            }

            foreach (var floor in run.Map.Floors)
            {
                foreach (var node in floor)
                {
                    CreateNode(area, node);
                }
            }
        }

        Vector2 PositionOf(MapNode node)
        {
            float x = (node.Column - (run.Map.Width - 1) * 0.5f) * ColumnSpacing;
            float y = (node.Floor - (run.Map.FloorCount - 1) * 0.5f) * FloorSpacing;
            return new Vector2(x, y);
        }

        string StatsText()
        {
            return "HP  " + run.Hp + " / " + run.MaxHp +
                   "\nGold  " + run.Gold +
                   "\nDeck  " + run.Deck.Count + " cards";
        }

        string HintText()
        {
            return run.Current == null
                ? "Choose your first room."
                : "Choose a connected room.  Floor " + (run.Current.Floor + 1) + " / " + run.Map.FloorCount;
        }

        bool IsAvailable(MapNode node)
        {
            if (node.Visited) return false;
            if (run.Current == null) return node.Floor == 0;
            return run.Current.Next.Contains(node);
        }

        void CreateNode(RectTransform area, MapNode node)
        {
            bool available = IsAvailable(node);
            Color color = ColorFor(node.Type);
            if (!available)
                color = new Color(color.r * 0.34f, color.g * 0.34f, color.b * 0.34f);

            var button = UIFactory.MakeButton(area, "Node_" + node.Floor + "_" + node.Column,
                LabelFor(node.Type), color, 18f);
            var rt = (RectTransform)button.transform;
            UIFactory.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                PositionOf(node), new Vector2(104f, 62f));

            button.interactable = available;
            if (available)
            {
                MapNode captured = node;
                button.onClick.AddListener(() => onSelect(captured));
            }
        }

        static string LabelFor(NodeType type)
        {
            switch (type)
            {
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
                case NodeType.Combat: return new Color(0.60f, 0.25f, 0.24f);
                case NodeType.Elite: return new Color(0.72f, 0.20f, 0.42f);
                case NodeType.Rest: return new Color(0.24f, 0.52f, 0.34f);
                case NodeType.Shop: return new Color(0.66f, 0.54f, 0.22f);
                case NodeType.Event: return new Color(0.24f, 0.42f, 0.66f);
                default: return new Color(0.42f, 0.24f, 0.58f);
            }
        }
    }
}
