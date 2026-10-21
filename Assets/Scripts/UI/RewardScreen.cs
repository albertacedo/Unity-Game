using System;
using System.Collections.Generic;
using Prototype.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    public class RewardScreen : MonoBehaviour
    {
        public void Init(List<CardData> choices, Action<CardData> onPick, Action onSkip)
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);

            var title = UIFactory.MakeLabel(background, "Title", "CHOOSE A CARD", 42f, UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -110f), new Vector2(900f, 60f));

            var subtitle = UIFactory.MakeLabel(background, "Subtitle",
                "The card is added to your deck for the rest of the run.", 21f, UIFactory.Muted);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -160f), new Vector2(900f, 36f));

            var row = UIFactory.MakeRect(background, "Choices");
            UIFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(900f, 240f));

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 46f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (CardData card in choices)
            {
                CardData captured = card;
                UIFactory.MakeCard(row, captured, () =>
                {
                    if (onPick != null) onPick(captured);
                });
            }

            var skip = UIFactory.MakeButton(background, "Skip", "SKIP", new Color(0.20f, 0.19f, 0.24f), 22f);
            UIFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 90f), new Vector2(240f, 64f));
            skip.onClick.AddListener(() =>
            {
                if (onSkip != null) onSkip();
            });
        }
    }
}
