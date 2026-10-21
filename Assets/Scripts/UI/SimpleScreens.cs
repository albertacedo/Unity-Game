using System;
using Prototype.Mapgen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    /// <summary>Screens that are too small to deserve their own class.</summary>
    public static class SimpleScreens
    {
        public static void Rest(Transform parent, RunState run, Action onHeal, Action onLeave)
        {
            var background = UIFactory.MakePanel(parent, "RestScreen", UIFactory.Background);
            var card = UIFactory.MakePanel(background, "Card", UIFactory.PanelColor);
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(640f, 420f));

            var title = UIFactory.MakeLabel(card, "Title", "REST SITE", 40f, UIFactory.Success);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(560f, 56f));

            int heal = Mathf.Min(Mathf.RoundToInt(run.MaxHp * 0.3f), run.MaxHp - run.Hp);
            var text = UIFactory.MakeLabel(card, "Text",
                "A quiet campfire.\nYou can rest to recover " + heal + " HP.", 22f, UIFactory.Ink);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(560f, 90f));

            var rest = UIFactory.MakeButton(card, "Rest", "REST  +" + heal + " HP", UIFactory.Success, 24f);
            UIFactory.Place((RectTransform)rest.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 96f), new Vector2(280f, 64f));
            rest.onClick.AddListener(() =>
            {
                if (onHeal != null) onHeal();
            });

            var leave = UIFactory.MakeButton(card, "Leave", "LEAVE", new Color(0.20f, 0.19f, 0.24f), 22f);
            UIFactory.Place((RectTransform)leave.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(280f, 56f));
            leave.onClick.AddListener(() =>
            {
                if (onLeave != null) onLeave();
            });
        }

        public static void Placeholder(Transform parent, NodeType type, Action onLeave)
        {
            var background = UIFactory.MakePanel(parent, "PlaceholderScreen", UIFactory.Background);
            var card = UIFactory.MakePanel(background, "Card", UIFactory.PanelColor);
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620f, 360f));

            var title = UIFactory.MakeLabel(card, "Title", type.ToString().ToUpperInvariant(), 38f, UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(560f, 56f));

            var text = UIFactory.MakeLabel(card, "Text",
                "This room is not implemented yet.\nThe map, the loop and combat are the current prototype.",
                22f, UIFactory.Muted);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(540f, 90f));

            var button = UIFactory.MakeButton(card, "Leave", "CONTINUE",
                new Color(0.30f, 0.26f, 0.38f), 23f);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(280f, 64f));
            button.onClick.AddListener(() =>
            {
                if (onLeave != null) onLeave();
            });
        }

        public static void End(Transform parent, bool victory, Action onRestart)
        {
            var background = UIFactory.MakePanel(parent, "EndScreen", UIFactory.Background);

            var title = UIFactory.MakeLabel(background, "Title",
                victory ? "THE SPIRE FALLS" : "RUN OVER", 56f,
                victory ? UIFactory.Gold : UIFactory.Danger);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), new Vector2(1000f, 80f));

            var text = UIFactory.MakeLabel(background, "Text",
                victory ? "You defeated the boss of Act I." : "Your deck was not strong enough this time.",
                24f, UIFactory.Ink);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(900f, 50f));

            var button = UIFactory.MakeButton(background, "Restart", "NEW RUN",
                new Color(0.30f, 0.26f, 0.38f), 24f);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(280f, 70f));
            button.onClick.AddListener(() =>
            {
                if (onRestart != null) onRestart();
            });
        }
    }
}
