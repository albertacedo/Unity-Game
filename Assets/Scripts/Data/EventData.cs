using System;

namespace Prototype.Data
{
    /// <summary>
    /// Effects an event option can have. Kept apart from <see cref="EffectKind"/> on
    /// purpose: combat effects and out-of-combat effects should not share a vocabulary
    /// until the event screen exists.
    /// </summary>
    public enum EventEffectKind { Nothing, Heal, LoseHp, GainGold, LoseGold, MaxHp, AddCard }

    [Serializable]
    public class EventEffect
    {
        public EventEffectKind Kind = EventEffectKind.Nothing;
        public int Value;

        /// <summary>Card id for <see cref="EventEffectKind.AddCard"/>; empty otherwise.</summary>
        public string Target;

        public EventEffect() { }

        public EventEffect(EventEffectKind kind, int value, string target = null)
        {
            Kind = kind;
            Value = value;
            Target = target;
        }
    }

    [Serializable]
    public class EventOption
    {
        public string Label;
        public EventEffect[] Effects = new EventEffect[0];
    }

    /// <summary>
    /// A map event definition. Authored ahead of the event screen, like
    /// <see cref="RelicData"/>; the map still shows a placeholder for events.
    /// </summary>
    [Serializable]
    public class EventData
    {
        public string Id;
        public string Title;
        public string Text;
        public int Weight = 10;
        public EventOption[] Options = new EventOption[0];
    }
}
