using System;
using Prototype.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.UI
{
    /// <summary>
    /// Shop room: four cards with their prices and one bandage. Clicks go back to
    /// <see cref="GameManager"/>, which applies them through <see cref="ShopState"/> and
    /// redraws the screen, so the gold and the sold state are always the run's.
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        const float PanelWidth = 940f;
        const float PanelHeight = 600f;
        const float CardWidth = 150f;
        const float CardHeight = 205f;

        public void Init(ShopState shop, RunState run,
            Action<ShopOffer> onBuy, Action onHeal, Action onLeave)
        {
            var background = UIFactory.MakePanel(transform, "Background", UIFactory.Background);
            var panel = UIFactory.MakePanel(background, "Panel", UIFactory.PanelColor);
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            var title = UIFactory.MakeLabel(panel, "Title", "SHOP", 38f, UIFactory.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(400f, 50f));

            var status = UIFactory.MakeLabel(panel, "Status",
                "HP " + run.Hp + " / " + run.MaxHp + "   -   " + run.Gold + " gold", 21f, UIFactory.Muted);
            UIFactory.Place(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -86f), new Vector2(600f, 30f));

            var row = UIFactory.MakeRect(panel, "Offers");
            UIFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 26f), new Vector2(760f, 252f));

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (ShopOffer offer in shop.Cards)
            {
                ShopOffer captured = offer;
                bool affordable = shop.CanBuy(captured, run);

                var column = UIFactory.MakeRect(row, "Offer_" + captured.Card.Id);
                column.sizeDelta = new Vector2(CardWidth, 252f);

                var button = UIFactory.MakeCard(column, captured.Card, () =>
                {
                    if (onBuy != null) onBuy(captured);
                });
                UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), Vector2.zero, new Vector2(CardWidth, CardHeight));
                button.interactable = affordable;

                string price = captured.Sold ? "SOLD" : captured.Price + " gold";
                Color priceColor = captured.Sold ? UIFactory.Muted
                    : affordable ? UIFactory.Gold : UIFactory.Danger;
                var priceLabel = UIFactory.MakeLabel(column, "Price", price, 20f, priceColor);
                UIFactory.Place(priceLabel.rectTransform, new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(CardWidth, 34f));
            }

            string healTail = shop.HealSold ? "sold"
                : run.Hp >= run.MaxHp ? "you are at full HP"
                : run.Gold < ShopState.HealPrice ? "need " + ShopState.HealPrice + " gold"
                : ShopState.HealAmount + " HP for " + ShopState.HealPrice + " gold";
            var heal = UIFactory.MakeButton(panel, "Heal",
                "BANDAGE\n<size=15><color=#A8A2B4>" + healTail + "</color></size>",
                new Color(0.24f, 0.34f, 0.28f), 20f);
            UIFactory.Place((RectTransform)heal.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(320f, 64f));
            heal.interactable = shop.CanHeal(run);
            heal.onClick.AddListener(() =>
            {
                if (onHeal != null) onHeal();
            });

            var leave = UIFactory.MakeButton(panel, "Leave", "LEAVE",
                new Color(0.20f, 0.19f, 0.24f), 22f);
            UIFactory.Place((RectTransform)leave.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(240f, 60f));
            leave.onClick.AddListener(() =>
            {
                if (onLeave != null) onLeave();
            });
        }
    }
}
