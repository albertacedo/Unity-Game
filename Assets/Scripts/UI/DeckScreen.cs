using System;
using System.Collections.Generic;
using Prototype.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Prototype.UI
{
    /// <summary>
    /// Scrollable overlay with every card in the run deck. It is added on top of the
    /// current screen, so it works from the map, from combat and from rewards.
    /// </summary>
    public class DeckScreen : MonoBehaviour
    {
        RunState run;
        Action onClose;
        RectTransform detail;

        public void Init(RunState runState, Action close)
        {
            run = runState;
            onClose = close;
            Build();
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (detail != null) CloseDetail();
            else Close();
        }

        void Build()
        {
            var dim = UIFactory.MakePanel(transform, "Dim", new Color(0f, 0f, 0f, 0.86f));

            var title = UIFactory.MakeLabel(dim, "Title",
                "YOUR DECK  ·  " + run.Deck.Count + " CARDS", 34f, UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(900f, 48f));

            var close = UIFactory.MakeButton(dim, "Close", "CLOSE",
                new Color(0.30f, 0.26f, 0.38f), 20f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -24f), new Vector2(170f, 54f));
            close.onClick.AddListener(Close);

            var hint = UIFactory.MakeLabel(dim, "Hint",
                "Click a card to enlarge it  ·  ESC to close", 18f, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 14f), new Vector2(900f, 28f));

            BuildGrid(dim);
        }

        void BuildGrid(RectTransform parent)
        {
            var viewport = UIFactory.MakeRect(parent, "Viewport");
            UIFactory.Place(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -6f), new Vector2(1180f, 690f));

            var backdrop = viewport.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.35f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = UIFactory.MakeRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 205f);
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(10, 10, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45f;

            List<CardData> sorted = new List<CardData>(run.Deck);
            sorted.Sort(CompareCards);

            foreach (CardData card in sorted)
            {
                CardData captured = card;
                UIFactory.MakeCard(content, captured, () => ShowDetail(captured));
            }
        }

        static int CompareCards(CardData a, CardData b)
        {
            int byKind = ((int)a.Kind).CompareTo((int)b.Kind);
            if (byKind != 0) return byKind;

            int byTitle = string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
            if (byTitle != 0) return byTitle;

            return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        }

        void ShowDetail(CardData card)
        {
            if (detail != null) return;

            var overlay = UIFactory.MakePanel(transform, "CardDetail",
                new Color(0f, 0f, 0f, 0.92f));
            detail = overlay;

            var backdrop = overlay.gameObject.AddComponent<Button>();
            backdrop.transition = Selectable.Transition.None;
            backdrop.onClick.AddListener(CloseDetail);

            var big = UIFactory.MakeCard(overlay, card, () => { });
            ((RectTransform)big.transform).localScale = new Vector3(2f, 2f, 1f);

            var kind = UIFactory.MakeLabel(overlay, "Kind", KindLine(card), 22f, UIFactory.Muted);
            UIFactory.Place(kind.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -260f), new Vector2(700f, 34f));

            var hint = UIFactory.MakeLabel(overlay, "Hint",
                "Click anywhere to close", 17f, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -300f), new Vector2(700f, 28f));
        }

        static string KindLine(CardData card)
        {
            string kind;
            switch (card.Kind)
            {
                case CardKind.Attack: kind = "ATTACK"; break;
                case CardKind.Skill: kind = "SKILL"; break;
                default: kind = "POWER"; break;
            }

            return kind + "  ·  COST " + card.Cost;
        }

        void CloseDetail()
        {
            if (detail == null) return;
            Destroy(detail.gameObject);
            detail = null;
        }

        void Close()
        {
            if (onClose != null) onClose();
            Destroy(gameObject);
        }
    }
}
