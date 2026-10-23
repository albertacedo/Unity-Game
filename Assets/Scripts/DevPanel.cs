using System;
using Prototype.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Prototype
{
    /// <summary>
    /// Playtest overlay toggled with F1. Buttons jump the run into the state you want
    /// to inspect instead of replaying fights.
    /// </summary>
    public class DevPanel : MonoBehaviour
    {
        const float PanelWidth = 320f;

        RectTransform panel;
        TextMeshProUGUI status;
        Button winButton;
        Button killButton;
        Button energyButton;
        Button skipButton;
        float nextY;

        void Awake()
        {
            Build();
            panel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) Toggle();
        }

        void Toggle()
        {
            bool show = !panel.gameObject.activeSelf;
            panel.gameObject.SetActive(show);
            if (show) Refresh();
        }

        void Build()
        {
            panel = UIFactory.MakePanel(transform, "Panel", new Color(0.09f, 0.08f, 0.11f, 0.97f));
            UIFactory.Place(panel, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 24f), new Vector2(PanelWidth, 476f));

            var title = UIFactory.MakeLabel(panel, "Title", "DEV PANEL  ·  F1", 19f, UIFactory.Gold,
                TextAlignmentOptions.Left);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -10f), new Vector2(PanelWidth - 28f, 26f));

            nextY = -44f;
            Add("HEAL TO FULL", new Color(0.24f, 0.42f, 0.30f), () => GameManager.Instance.DebugHeal());
            Add("ADD RANDOM CARD", new Color(0.30f, 0.26f, 0.38f), () => GameManager.Instance.DebugAddCard());
            winButton = Add("WIN COMBAT", new Color(0.46f, 0.24f, 0.24f), () => GameManager.Instance.DebugWinCombat());
            killButton = Add("KILL TARGET", new Color(0.42f, 0.22f, 0.30f), () => GameManager.Instance.DebugKillTarget());
            energyButton = Add("+1 ENERGY", new Color(0.36f, 0.32f, 0.18f), () => GameManager.Instance.DebugAddEnergy());
            skipButton = Add("SKIP ROOM", new Color(0.24f, 0.32f, 0.46f), () => GameManager.Instance.DebugSkipRoom());
            Add("RESTART (SAME SEED)", new Color(0.20f, 0.19f, 0.24f),
                () => GameManager.Instance.DebugRestart(GameManager.Instance.Run.Seed));
            Add("RESTART (NEW SEED)", new Color(0.20f, 0.19f, 0.24f),
                () => GameManager.Instance.DebugRestart(Environment.TickCount));

            status = UIFactory.MakeLabel(panel, "Status", "", 16f, UIFactory.Muted,
                TextAlignmentOptions.Left);
            status.rectTransform.anchorMin = new Vector2(0f, 0f);
            status.rectTransform.anchorMax = new Vector2(1f, 0f);
            status.rectTransform.pivot = new Vector2(0.5f, 0f);
            status.rectTransform.offsetMin = new Vector2(14f, 10f);
            status.rectTransform.offsetMax = new Vector2(-14f, 64f);
        }

        Button Add(string caption, Color color, Action action)
        {
            var button = UIFactory.MakeButton(panel, caption, caption, color, 17f);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, nextY), new Vector2(PanelWidth - 24f, 36f));
            nextY -= 44f;

            button.onClick.AddListener(() =>
            {
                if (action != null) action();
                Refresh();
            });

            return button;
        }

        void Refresh()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.Run == null) return;

            RunState run = game.Run;
            bool inCombat = game.InCombat;

            winButton.interactable = inCombat;
            killButton.interactable = inCombat;
            energyButton.interactable = inCombat;
            skipButton.interactable = true;

            string line = "Seed " + run.Seed + "   ·   HP " + run.Hp + " / " + run.MaxHp;
            string extra = "\nDeck " + run.Deck.Count + "   ·   Gold " + run.Gold;
            if (inCombat)
            {
                extra += "   ·   Energy " + game.Combat.Energy;
                if (game.Combat.Target != null) extra += "\nTarget " + game.Combat.Target.Title;
            }

            status.text = line + extra;
        }
    }
}
