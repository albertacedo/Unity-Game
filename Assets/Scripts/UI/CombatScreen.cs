using System;
using System.Collections;
using System.Collections.Generic;
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
        TextMeshProUGUI pilesLabel;
        TextMeshProUGUI logLabel;
        TextMeshProUGUI targetLabel;

        RectTransform hpFill;
        RectTransform handRoot;
        RectTransform playerPanel;
        Image playerFlash;
        Button endTurnButton;

        readonly List<EnemyWidget> enemyWidgets = new List<EnemyWidget>();

        bool resultShown;

        /// <summary>Exposed so tools and tests can inspect or drive the fight.</summary>
        public CombatSession Session { get { return session; } }

        public void Init(CombatSession combat, Action finished)
        {
            session = combat;
            onFinished = finished;
            Build();
            session.Changed += Refresh;
            session.DamageDealt += OnDamageDealt;
            Refresh();
        }

        void OnDestroy()
        {
            if (session != null)
            {
                session.Changed -= Refresh;
                session.DamageDealt -= OnDamageDealt;
            }
        }

        void Build()
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);

            // --- Player panel -------------------------------------------------
            playerPanel = UIFactory.MakePanel(background, "PlayerPanel", UIFactory.PanelColor);
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

            playerFlash = MakeFlash(playerPanel, "Flash");

            // --- Enemies ------------------------------------------------------
            RectTransform enemyRow = UIFactory.MakeRect(background, "Enemies");
            UIFactory.Place(enemyRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -158f), new Vector2(1160f, 214f));

            var layout = enemyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float width = session.Enemies.Count <= 1 ? 380f
                : session.Enemies.Count == 2 ? 330f
                : 260f;

            foreach (EnemyInstance enemy in session.Enemies)
            {
                enemyWidgets.Add(new EnemyWidget(enemyRow, enemy, width, session.SelectTarget));
            }

            targetLabel = UIFactory.MakeLabel(background, "TargetHint", "", 18f, UIFactory.Gold);
            UIFactory.Place(targetLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -396f), new Vector2(1100f, 26f));

            // --- Log and piles ------------------------------------------------
            logLabel = UIFactory.MakeLabel(background, "Log", "", 21f, UIFactory.Muted);
            UIFactory.Place(logLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), new Vector2(1000f, 104f));

            pilesLabel = UIFactory.MakeLabel(background, "Piles", "", 19f, UIFactory.Muted,
                TextAlignmentOptions.Left);
            UIFactory.Place(pilesLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(36f, 36f), new Vector2(400f, 28f));

            // --- Hand ---------------------------------------------------------
            handRoot = UIFactory.MakeRect(background, "Hand");
            UIFactory.Place(handRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 104f), new Vector2(1300f, 220f));

            var handLayout = handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = 14f;
            handLayout.childAlignment = TextAnchor.MiddleCenter;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;

            endTurnButton = UIFactory.MakeButton(background, "EndTurn", "END TURN",
                new Color(0.30f, 0.26f, 0.38f), 24f);
            UIFactory.Place((RectTransform)endTurnButton.transform, new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(220f, 72f));
            endTurnButton.onClick.AddListener(OnEndTurn);

            var deckButton = UIFactory.MakeButton(background, "Deck", "VIEW DECK",
                new Color(0.20f, 0.19f, 0.24f), 19f);
            UIFactory.Place((RectTransform)deckButton.transform, new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-40f, 240f), new Vector2(220f, 52f));
            deckButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ShowDeck();
            });
        }

        void OnEndTurn()
        {
            session.EndTurn();
        }

        static Image MakeFlash(RectTransform parent, string name)
        {
            var rt = UIFactory.MakePanel(parent, name, new Color(1f, 1f, 1f, 0f));
            var image = rt.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        void OnDamageDealt(int amount, EnemyInstance enemy)
        {
            if (enemy == null)
            {
                if (amount > 0) SpawnPopup(playerPanel, "-" + amount, UIFactory.Danger);
                if (playerFlash != null) StartCoroutine(FadeFlash(playerFlash, new Color(0.85f, 0.15f, 0.15f, 0.45f)));
                return;
            }

            EnemyWidget widget = FindWidget(enemy);
            if (widget == null) return;

            if (amount > 0) SpawnPopup(widget.Panel, "-" + amount, UIFactory.Gold);
            if (widget.Flash != null) StartCoroutine(FadeFlash(widget.Flash, new Color(1f, 1f, 1f, 0.30f)));
        }

        EnemyWidget FindWidget(EnemyInstance enemy)
        {
            foreach (EnemyWidget widget in enemyWidgets)
                if (widget.Enemy == enemy) return widget;
            return null;
        }

        void SpawnPopup(RectTransform parent, string text, Color color)
        {
            if (parent == null) return;

            var label = UIFactory.MakeLabel(parent, "Popup", text, 40f, color);
            label.fontStyle = FontStyles.Bold;
            UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(UnityEngine.Random.Range(-40f, 40f), 30f), new Vector2(240f, 64f));
            StartCoroutine(AnimatePopup(label));
        }

        IEnumerator AnimatePopup(TextMeshProUGUI label)
        {
            const float duration = 0.9f;
            float t = 0f;
            Vector2 start = label.rectTransform.anchoredPosition;
            Color color = label.color;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                label.rectTransform.anchoredPosition = start + new Vector2(0f, k * 90f);
                color.a = 1f - k;
                label.color = color;
                yield return null;
            }

            Destroy(label.gameObject);
        }

        IEnumerator FadeFlash(Image image, Color color)
        {
            const float duration = 0.35f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                Color current = color;
                current.a = color.a * (1f - Mathf.Clamp01(t / duration));
                image.color = current;
                yield return null;
            }

            Color clear = color;
            clear.a = 0f;
            image.color = clear;
        }

        void Refresh()
        {
            hpLabel.text = "HP  " + session.Run.Hp + " / " + session.Run.MaxHp;
            UIFactory.SetBarValue(hpFill, session.Run.Hp / (float)session.Run.MaxHp);

            blockLabel.text = session.PlayerBlock > 0 ? "Block " + session.PlayerBlock : "";
            energyLabel.text = "Energy  " + session.Energy + " / " + CombatSession.EnergyPerTurn;

            foreach (EnemyWidget widget in enemyWidgets)
                widget.Refresh(session.Target);

            targetLabel.text = session.Target == null
                ? ""
                : "Click an enemy to target it   ·   Target: " + session.Target.Title;

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

            var overlay = UIFactory.MakePanel(transform, "Result", new Color(0f, 0f, 0f, 0.95f));

            var title = UIFactory.MakeLabel(overlay, "Title",
                session.PlayerWon ? "VICTORY" : "DEFEAT", 58f,
                session.PlayerWon ? UIFactory.Gold : UIFactory.Danger);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), new Vector2(900f, 90f));

            var subtitle = UIFactory.MakeLabel(overlay, "Subtitle",
                session.PlayerWon
                    ? (session.Enemies.Count > 1
                        ? "The enemies fall. The path continues."
                        : "The enemy falls. The path continues.")
                    : "You have fallen.",
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

        /// <summary>One clickable enemy card in the fight. Clicking it makes it the target.</summary>
        sealed class EnemyWidget
        {
            public readonly EnemyInstance Enemy;
            public readonly RectTransform Panel;
            public readonly Image Flash;

            readonly Image background;
            readonly Outline outline;
            readonly CanvasGroup group;
            readonly Button button;
            readonly TextMeshProUGUI nameLabel;
            readonly TextMeshProUGUI hpLabel;
            readonly TextMeshProUGUI intentLabel;
            readonly RectTransform hpFill;

            readonly Color idleColor = UIFactory.PanelColor;
            readonly Color selectedColor = new Color(0.21f, 0.18f, 0.26f);

            public EnemyWidget(RectTransform parent, EnemyInstance enemy, float width,
                Action<EnemyInstance> onClick)
            {
                Enemy = enemy;

                Panel = UIFactory.MakeRect(parent, "Enemy_" + enemy.Title);
                Panel.sizeDelta = new Vector2(width, 210f);

                background = Panel.gameObject.AddComponent<Image>();
                background.color = idleColor;

                button = Panel.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.onClick.AddListener(() => onClick(Enemy));

                outline = Panel.gameObject.AddComponent<Outline>();
                outline.effectColor = UIFactory.Gold;
                outline.effectDistance = new Vector2(3f, 3f);
                outline.useGraphicAlpha = false;
                outline.enabled = false;

                nameLabel = UIFactory.MakeLabel(Panel, "Name", enemy.Title, 24f, UIFactory.Gold);
                UIFactory.Place(nameLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -10f), new Vector2(width - 24f, 34f));

                hpLabel = UIFactory.MakeLabel(Panel, "Hp", "", 18f, UIFactory.Ink);
                UIFactory.Place(hpLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -46f), new Vector2(width - 24f, 26f));

                RectTransform bar = UIFactory.MakeBar(Panel, "HpBar", UIFactory.Danger, out hpFill);
                UIFactory.Place(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -76f), new Vector2(width - 36f, 18f));

                intentLabel = UIFactory.MakeLabel(Panel, "Intent", "", 22f, UIFactory.Danger);
                UIFactory.Place(intentLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 16f), new Vector2(width - 24f, 36f));

                group = Panel.gameObject.AddComponent<CanvasGroup>();

                Flash = MakeFlash(Panel, "Flash");
            }

            public void Refresh(EnemyInstance target)
            {
                bool alive = Enemy.IsAlive;
                bool selected = alive && Enemy == target;

                background.color = selected ? selectedColor : idleColor;
                outline.enabled = selected;
                group.alpha = alive ? 1f : 0.45f;
                button.interactable = alive;

                nameLabel.text = Enemy.Title;
                nameLabel.color = alive ? UIFactory.Gold : UIFactory.Muted;

                hpLabel.text = alive
                    ? Enemy.Hp + " / " + Enemy.MaxHp +
                      (Enemy.Block > 0 ? "   ·   Block " + Enemy.Block : "")
                    : "DEFEATED";
                UIFactory.SetBarValue(hpFill, Enemy.MaxHp == 0 ? 0f : Enemy.Hp / (float)Enemy.MaxHp);

                EnemyIntent intent = Enemy.Intent;
                intentLabel.text = !alive || intent == null
                    ? ""
                    : intent.Kind == IntentKind.Attack
                        ? "ATTACK  " + intent.Value
                        : "DEFEND  " + intent.Value;
                intentLabel.color = intent == null || intent.Kind == IntentKind.Attack
                    ? UIFactory.Danger
                    : new Color(0.55f, 0.70f, 0.90f);
            }
        }
    }
}
