using Prototype.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    /// <summary>Small helpers so every screen can be built from code.</summary>
    public static class UIFactory
    {
        public static readonly Color Background = new Color(0.07f, 0.065f, 0.085f);
        public static readonly Color PanelColor = new Color(0.13f, 0.12f, 0.16f);
        public static readonly Color Ink = new Color(0.93f, 0.91f, 0.87f);
        public static readonly Color Muted = new Color(0.62f, 0.60f, 0.66f);
        public static readonly Color Gold = new Color(0.86f, 0.72f, 0.36f);
        public static readonly Color Danger = new Color(0.78f, 0.30f, 0.28f);
        public static readonly Color Success = new Color(0.32f, 0.66f, 0.42f);

        public static Canvas MakeCanvas()
        {
            var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100f, 100f);
            return rt;
        }

        public static RectTransform MakePanel(Transform parent, string name, Color color)
        {
            var rt = MakeRect(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            Stretch(rt);
            return rt;
        }

        public static TextMeshProUGUI MakeLabel(Transform parent, string name, string content,
            float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = MakeRect(parent, name);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }

        public static Button MakeButton(Transform parent, string name, string caption, Color background,
            float fontSize = 24f)
        {
            var rt = MakeRect(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = background;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = MakeLabel(rt, "Label", caption, fontSize, Ink);
            Stretch(label.rectTransform, 6f, 4f, 6f, 4f);
            return button;
        }

        public static Image MakeLine(Transform parent, Vector2 from, Vector2 to, Color color, float thickness)
        {
            var rt = MakeRect(parent, "Link");
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            Vector2 direction = to - from;
            rt.sizeDelta = new Vector2(direction.magnitude, thickness);
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return image;
        }

        public static RectTransform MakeBar(Transform parent, string name, Color fillColor, out RectTransform fill)
        {
            var background = MakeRect(parent, name);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.05f, 0.05f, 0.07f);
            backgroundImage.raycastTarget = false;

            fill = MakeRect(background, "Fill");
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);

            return background;
        }

        public static void SetBarValue(RectTransform fill, float value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
        }

        public static Button MakeCard(Transform parent, CardData card, System.Action onClick)
        {
            var rt = MakeRect(parent, "Card_" + card.Id);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = card.Tint;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            rt.sizeDelta = new Vector2(150f, 205f);

            var costBox = MakeRect(rt, "Cost");
            var costImage = costBox.gameObject.AddComponent<Image>();
            costImage.color = new Color(0.06f, 0.06f, 0.08f);
            costImage.raycastTarget = false;
            Place(costBox, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(34f, 34f));
            var costLabel = MakeLabel(costBox, "Value", card.Cost.ToString(), 22f, Gold);
            Stretch(costLabel.rectTransform);

            var title = MakeLabel(rt, "Title", card.Title, 19f, Ink, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(10f, -46f), new Vector2(130f, 46f));

            var body = MakeLabel(rt, "Body", card.Text, 15f, Ink, TextAlignmentOptions.Center);
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -16f), new Vector2(134f, 100f));

            if (onClick != null) button.onClick.AddListener(() => onClick());

            return button;
        }

        public static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }
    }
}
