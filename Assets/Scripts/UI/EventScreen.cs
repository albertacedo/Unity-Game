using System;
using System.Collections.Generic;
using Prototype.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    /// <summary>
    /// Event room. Shows the text and one button per option, then the outcome of the
    /// chosen one. It never touches the run: <see cref="GameManager"/> applies the
    /// option through <see cref="EventResolver"/> and asks for the result.
    /// </summary>
    public class EventScreen : MonoBehaviour
    {
        const float CardWidth = 820f;
        const float CardHeight = 560f;
        const float OptionStep = 88f;

        string eventTitle = "EVENT";

        public void Init(EventData data, RunState run, IList<CardData> cards,
            Action<EventOption> onChoose, Action onLeave)
        {
            if (data != null && !string.IsNullOrEmpty(data.Title)) eventTitle = data.Title;
            Clear();

            RectTransform card = BuildCard();
            AddTitle(card);

            var text = UIFactory.MakeLabel(card, "Text", data != null ? data.Text : "", 22f, UIFactory.Ink);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -104f), new Vector2(CardWidth - 120f, 96f));

            EventOption[] options = data != null && data.Options != null ? data.Options : new EventOption[0];
            if (options.Length == 0)
            {
                AddContinue(card, onLeave);
                return;
            }

            // First option on top: the eye reads the list in the authored order.
            for (int i = 0; i < options.Length; i++)
            {
                EventOption option = options[i];
                bool affordable = EventResolver.CanAfford(option, run);

                string head = affordable
                    ? option.Label
                    : "<color=#8A8496>" + option.Label + "</color>";
                string tail = EventResolver.Describe(option, cards);
                if (!affordable) tail += "  -  need " + EventResolver.GoldCost(option) + " gold";
                string caption = head + "\n<size=15><color=#A8A2B4>" + tail + "</color></size>";

                var button = UIFactory.MakeButton(card, "Option" + i, caption,
                    new Color(0.20f, 0.19f, 0.24f), 20f);
                float y = 26f + (options.Length - 1 - i) * OptionStep;
                UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(680f, 78f));
                button.interactable = affordable;

                EventOption chosen = option;
                button.onClick.AddListener(() =>
                {
                    if (onChoose != null) onChoose(chosen);
                });
            }
        }

        /// <summary>Second half of the room: what the choice did, and a way out.</summary>
        public void ShowResult(IReadOnlyList<string> lines, RunState run, Action onContinue)
        {
            Clear();

            RectTransform card = BuildCard();
            AddTitle(card);

            var result = UIFactory.MakeLabel(card, "Result",
                lines != null && lines.Count > 0 ? string.Join("\n", lines) : "Nothing happens.",
                26f, UIFactory.Ink);
            UIFactory.Place(result.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -180f), new Vector2(CardWidth - 140f, 180f));

            var totals = UIFactory.MakeLabel(card, "Totals",
                "HP " + run.Hp + " / " + run.MaxHp + "   -   " + run.Gold + " gold", 20f, UIFactory.Muted);
            UIFactory.Place(totals.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 122f), new Vector2(CardWidth - 140f, 32f));

            AddContinue(card, onContinue);
        }

        RectTransform BuildCard()
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);
            var card = UIFactory.MakePanel(background, "Card", UIFactory.PanelColor);
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(CardWidth, CardHeight));
            return card;
        }

        void AddTitle(RectTransform card)
        {
            var title = UIFactory.MakeLabel(card, "Title", eventTitle.ToUpperInvariant(), 36f, UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -38f), new Vector2(CardWidth - 80f, 50f));
        }

        void AddContinue(RectTransform card, Action onContinue)
        {
            var button = UIFactory.MakeButton(card, "Continue", "CONTINUE",
                new Color(0.30f, 0.26f, 0.38f), 22f);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(280f, 64f));
            button.onClick.AddListener(() =>
            {
                if (onContinue != null) onContinue();
            });
        }

        void Clear()
        {
            // SetActive first: Destroy is deferred, and a disabled old card must not
            // paint over the new one for a frame.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
