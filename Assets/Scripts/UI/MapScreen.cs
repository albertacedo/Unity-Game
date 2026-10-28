using System;
using System.Collections.Generic;
using Prototype.Mapgen;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Prototype.UI
{
    public class MapScreen : MonoBehaviour
    {
        static readonly Vector2 NodeSize = new Vector2(70f, 30f);
        static readonly Color RoadColor = new Color(0.29f, 0.27f, 0.34f);
        static readonly Color SameRingRoadColor = new Color(0.24f, 0.23f, 0.30f);

        const float MinScale = 0.6f;
        const float MaxScale = 2.2f;
        const float ZoomStep = 1.12f;

        RunState run;
        Action<MapNode> onSelect;
        RectTransform viewport;
        RectTransform content;
        TextMeshProUGUI zoomLabel;
        float viewScale = 1f;

        public void Init(RunState runState, Action<MapNode> onSelectNode)
        {
            run = runState;
            onSelect = onSelectNode;
            Build();
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || content == null) return;

            // The deck overlay scrolls with the wheel; the map behind it must stay still.
            if (DeckScreen.OpenCount > 0) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                ZoomAtCursor(mouse.position.ReadValue(), scroll > 0f ? ZoomStep : 1f / ZoomStep);

            if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
                Pan(mouse.delta.ReadValue());
        }

        void Build()
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);

            viewport = UIFactory.MakeRect(background, "MapViewport");
            UIFactory.Place(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(1460f, 800f));
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UIFactory.MakeRect(viewport, "MapContent");
            UIFactory.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, viewport.sizeDelta);

            DrawRoads();

            foreach (MapNode node in run.Map.Nodes())
                CreateNode(node);

            // Built after the map so the map never draws over the header while zoomed.
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
                new Vector2(-38f, -26f), new Vector2(740f, 110f));

            CreateViewControls(background);

            if (Options().Count == 0) CreateDeadEnd(background);
        }

        /// <summary>Zoom and reset buttons, so the map can be read without a mouse wheel.</summary>
        void CreateViewControls(RectTransform background)
        {
            var deck = UIFactory.MakeButton(background, "Deck",
                "VIEW DECK  " + run.Deck.Count, new Color(0.30f, 0.26f, 0.38f), 20f);
            UIFactory.Place((RectTransform)deck.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-38f, 26f), new Vector2(240f, 50f));
            deck.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ShowDeck();
            });

            var reset = UIFactory.MakeButton(background, "ResetView", "RESET VIEW",
                new Color(0.22f, 0.21f, 0.27f), 18f);
            UIFactory.Place((RectTransform)reset.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(38f, 26f), new Vector2(200f, 50f));
            reset.onClick.AddListener(ResetView);

            var zoomOut = UIFactory.MakeButton(background, "ZoomOut", "−",
                new Color(0.22f, 0.21f, 0.27f), 26f);
            UIFactory.Place((RectTransform)zoomOut.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(266f, 26f), new Vector2(56f, 50f));
            zoomOut.onClick.AddListener(() => ZoomAt(Vector2.zero, 1f / ZoomStep));

            zoomLabel = UIFactory.MakeLabel(background, "ZoomLabel", "100%", 18f, UIFactory.Muted);
            UIFactory.Place(zoomLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(314f, 26f), new Vector2(80f, 50f));

            var zoomIn = UIFactory.MakeButton(background, "ZoomIn", "+",
                new Color(0.22f, 0.21f, 0.27f), 26f);
            UIFactory.Place((RectTransform)zoomIn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(402f, 26f), new Vector2(56f, 50f));
            zoomIn.onClick.AddListener(() => ZoomAt(Vector2.zero, ZoomStep));
        }

        void DrawRoads()
        {
            foreach (MapNode node in run.Map.Nodes())
            {
                foreach (MapNode next in node.Next)
                {
                    if (next.Ring < node.Ring) continue;          // each road is stored on both ends

                    if (next.Ring == node.Ring)
                    {
                        if (!IsForwardNeighbour(node, next)) continue;
                        UIFactory.MakeLine(content, PositionOf(node), PositionOf(next), SameRingRoadColor, 2.5f);
                        continue;
                    }

                    UIFactory.MakeLine(content, PositionOf(node), PositionOf(next), RoadColor, 3f);
                }
            }
        }

        /// <summary>Same-ring roads only exist between consecutive rooms, so one of the two is "forward".</summary>
        bool IsForwardNeighbour(MapNode node, MapNode next)
        {
            List<MapNode> ring = run.Map.Rings[node.Ring];
            return next.Index == node.Index + 1 || (node.Index == ring.Count - 1 && next.Index == 0);
        }

        Vector2 PositionOf(MapNode node)
        {
            MapLayout.Project(node.Ring, node.Angle, out float x, out float y);
            return new Vector2(x, y);
        }

        void ZoomAtCursor(Vector2 screenPosition, float factor)
        {
            if (content == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPosition,
                    CanvasCamera(), out Vector2 point))
                return;

            ZoomAt(point, factor);
        }

        /// <summary>Scales the map around a point given in viewport coordinates, so it stays put.</summary>
        void ZoomAt(Vector2 point, float factor)
        {
            if (content == null) return;

            float newScale = Mathf.Clamp(viewScale * factor, MinScale, MaxScale);
            if (Mathf.Approximately(newScale, viewScale)) return;

            Vector2 offset = content.anchoredPosition;
            content.anchoredPosition = point - (point - offset) * (newScale / viewScale);

            viewScale = newScale;
            content.localScale = new Vector3(viewScale, viewScale, 1f);
            UpdateZoomLabel();
            ClampView();
        }

        void UpdateZoomLabel()
        {
            if (zoomLabel != null) zoomLabel.text = Mathf.RoundToInt(viewScale * 100f) + "%";
        }

        void Pan(Vector2 delta)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            float scaleFactor = canvas == null ? 1f : Mathf.Max(0.01f, canvas.scaleFactor);

            content.anchoredPosition += delta / scaleFactor;
            ClampView();
        }

        /// <summary>Keeps the map from being dragged out of reach.</summary>
        void ClampView()
        {
            Vector2 size = viewport.rect.size;
            Vector2 limit = Vector2.Max(Vector2.zero, size * viewScale - size) * 0.5f + new Vector2(80f, 80f);

            Vector2 position = content.anchoredPosition;
            position.x = Mathf.Clamp(position.x, -limit.x, limit.x);
            position.y = Mathf.Clamp(position.y, -limit.y, limit.y);
            content.anchoredPosition = position;
        }

        void ResetView()
        {
            viewScale = 1f;
            UpdateZoomLabel();
            if (content == null) return;

            content.localScale = Vector3.one;
            content.anchoredPosition = Vector2.zero;
        }

        Camera CanvasCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

            return canvas.worldCamera;
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
                    "\nUnknown rooms show only their position; bosses are always revealed." +
                    "\nBacktracking: " + (run.AllowBacktracking ? "allowed" : "blocked") + "." +
                   "\nScroll or + / − to zoom  ·  right-drag to move the map" +
                   "\nSomewhere on the outer ring: " + run.Map.Bosses.Count + " bosses.";
        }

        int CountNodes()
        {
            int count = 0;
            foreach (MapNode ignored in run.Map.Nodes()) count++;
            return count;
        }

        /// <summary>Rooms reachable in one map move, including visited rooms for backtracking.</summary>
        List<MapNode> Options()
        {
            var options = new List<MapNode>();
            if (run.Current == null) return options;

            foreach (MapNode node in run.Current.Next)
                if (run.AllowBacktracking || !node.Visited)
                    options.Add(node);

            return options;
        }

        bool IsAvailable(MapNode node)
        {
            if (run.Current == null || node == run.Current) return false;
            if (!run.AllowBacktracking && node.Visited) return false;
            return run.Current.Next.Contains(node);
        }

        void CreateNode(MapNode node)
        {
            bool available = IsAvailable(node);
            bool current = node == run.Current;
            bool known = !run.FogOfWar || node.TypeRevealed || node.Type == NodeType.Boss;
            Color color = known ? ColorFor(node.Type) : UnknownColor;
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

            var button = UIFactory.MakeButton(content, "Node_" + node.Ring + "_" + node.Index,
                LabelFor(node, known), color, 12f);
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

        static readonly Color UnknownColor = new Color(0.27f, 0.27f, 0.32f);

        static string LabelFor(MapNode node, bool known)
        {
            if (!known) return "?";

            NodeType type = node.Type;
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
