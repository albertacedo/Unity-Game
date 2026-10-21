using System;
using Prototype.Combat;
using Prototype.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    public class CombatScreen : MonoBehaviour
    {
        CombatSession session;
        Action onFinished;

        TextMeshProUGUI hpLabel;
        TextMeshProUGUI blockLabel;
        TextMeshProUGUI energyLabel;
        TextMeshProUGUI enemyNameLabel;
        TextMeshProUGUI enemyHpLabel;
        TextMeshProUGUI intentLabel;
        TextMeshProUGUI pilesLabel;
        TextMeshProUGUI logLabel;

        RectTransform hpFill;
        RectTransform enemyHpFill;
        RectTransform handRoot;
        Button endTurnButton;

        bool resultShown;

        /// <summary>Exposed so tools and tests can inspect or drive the fight.</summary>
        public CombatSession Session { get { return session; } }

        public void Init(CombatSession combat, Action finished)
        {
            session = combat;
            onFinished = finished;
            Build();
            session.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        void Build()
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);

            // --- Player panel -------------------------------------------------
            var playerPanel = UIFactory.MakePanel(background, "PlayerPanel", UIFactory.PanelColor);
            UIFactory.Place(playerPanel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -24f), new Vector2(460f, 126f));

            var you = UIFactory.MakeLabel(playerPanel, "You", "YOU", 17f, UIFactory.Muted,
                TextAlignmentOptions.Left);
            UIFactory.Place(you.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -8f), new Vector2(300f, 24f));

            hpLabel = UIFactory.MakeLabel(playerPanel, "Hp", "", 23f, UIFactory.Ink,
                TextAlignmentOptions.Left);
            UIFactory.Place(hpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -32f), new Vector2(300f, 32f));

            RectTransform hpBar = UIFactory.MakeBar(playerPanel, "HpBar", UIFactory.Danger, out hpFill);
            UIFactory.Place(hpBar, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 14f), new Vector2(428f, 18f));

            blockLabel = UIFactory.MakeLabel(playerPanel, "Block", "", 20f,
                new Color(0.55f, 0.70f, 0.90f), TextAlignmentOptions.Right);
            UIFactory.Place(blockLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -12f), new Vector2(200f, 28f));

            energyLabel = UIFactory.MakeLabel(playerPanel, "Energy", "", 20f, UIFactory.Gold,
                TextAlignmentOptions.Right);
            UIFactory.Place(energyLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -44f), new Vector2(200f, 28f));

            // --- Enemy panel --------------------------------------------------
            var enemyPanel = UIFactory.MakePanel(background, "EnemyPanel", UIFactory.PanelColor);
            UIFactory.Place(enemyPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(580f, 236f));

            enemyNameLabel = UIFactory.MakeLabel(enemyPanel, "Name", "", 32f, UIFactory.Gold);
            UIFactory.Place(enemyNameLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(520f, 44f));

            enemyHpLabel = UIFactory.MakeLabel(enemyPanel, "Hp", "", 20f, UIFactory.Ink);
            UIFactory.Place(enemyHpLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(520f, 28f));

            RectTransform enemyBar = UIFactory.MakeBar(enemyPanel, "HpBar", UIFactory.Danger, out enemyHpFill);
            UIFactory.Place(enemyBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -96f), new Vector2(500f, 20f));

            intentLabel = UIFactory.MakeLabel(enemyPanel, "Intent", "", 24f, UIFactory.Danger);
            UIFactory.Place(intentLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(520f, 40f));

            // --- Log and piles ------------------------------------------------
            logLabel = UIFactory.MakeLabel(background, "Log", "", 22f, UIFactory.Muted);
            UIFactory.Place(logLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f), new Vector2(1000f, 40f));

            pilesLabel = UIFactory.MakeLabel(background, "Piles", "", 19f, UIFactory.Muted,
                TextAlignmentOptions.Left);
            UIFactory.Place(pilesLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(36f, 36f), new Vector2(400f, 28f));

            // --- Hand ---------------------------------------------------------
            handRoot = UIFactory.MakeRect(background, "Hand");
            UIFactory.Place(handRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 104f), new Vector2(1300f, 220f));

            var layout = handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            endTurnButton = UIFactory.MakeButton(background, "EndTurn", "END TURN",
                new Color(0.30f, 0.26f, 0.38f), 24f);
            UIFactory.Place((RectTransform)endTurnButton.transform, new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(220f, 72f));
            endTurnButton.onClick.AddListener(OnEndTurn);
        }

        void OnEndTurn()
        {
            session.EndTurn();
        }

        void Refresh()
        {
            hpLabel.text = "HP  " + session.Run.Hp + " / " + session.Run.MaxHp;
            UIFactory.SetBarValue(hpFill, session.Run.Hp / (float)session.Run.MaxHp);

            blockLabel.text = session.PlayerBlock > 0 ? "Block " + session.PlayerBlock : "";
            energyLabel.text = "Energy  " + session.Energy + " / " + CombatSession.EnergyPerTurn;

            enemyNameLabel.text = session.Enemy.Title;
            enemyHpLabel.text = session.EnemyHp + " / " + session.EnemyMaxHp +
                                (session.EnemyBlock > 0 ? "    (Block " + session.EnemyBlock + ")" : "");
            UIFactory.SetBarValue(enemyHpFill,
                session.EnemyMaxHp == 0 ? 0f : session.EnemyHp / (float)session.EnemyMaxHp);

            EnemyIntent intent = session.CurrentIntent;
            intentLabel.text = intent == null
                ? ""
                : (intent.Kind == IntentKind.Attack ? "ATTACK  " + intent.Value : "DEFEND  " + intent.Value);

            pilesLabel.text = "Draw " + session.DrawPile.Count + "   ·   Discard " + session.DiscardPile.Count;
            logLabel.text = session.LastEvent;

            RebuildHand();

            endTurnButton.interactable = !session.IsOver;

            if (session.IsOver && !resultShown) ShowResult();
        }

        void RebuildHand()
        {
            for (int i = handRoot.childCount - 1; i >= 0; i--)
                Destroy(handRoot.GetChild(i).gameObject);

            foreach (CardData card in session.Hand)
            {
                CardData captured = card;
                bool playable = session.CanPlay(captured);
                Action onClick = null;
                if (playable) onClick = () => session.PlayCard(captured);

                Button button = UIFactory.MakeCard(handRoot, captured, onClick);
                button.interactable = playable;
            }
        }

        void ShowResult()
        {
            resultShown = true;

            var overlay = UIFactory.MakePanel(transform, "Result", new Color(0f, 0f, 0f, 0.78f));

            var title = UIFactory.MakeLabel(overlay, "Title",
                session.PlayerWon ? "VICTORY" : "DEFEAT", 58f,
                session.PlayerWon ? UIFactory.Gold : UIFactory.Danger);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), new Vector2(900f, 90f));

            var subtitle = UIFactory.MakeLabel(overlay, "Subtitle",
                session.PlayerWon ? "The enemy falls. The path continues." : "You have fallen.",
                24f, UIFactory.Ink);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(900f, 50f));

            var button = UIFactory.MakeButton(overlay, "Continue", "CONTINUE",
                new Color(0.30f, 0.26f, 0.38f), 24f);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(280f, 70f));
            button.onClick.AddListener(() =>
            {
                if (onFinished != null) onFinished();
            });
        }
    }
}
