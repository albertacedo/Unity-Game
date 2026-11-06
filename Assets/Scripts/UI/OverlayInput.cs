namespace Prototype.UI
{
    /// <summary>
    /// Overlays drawn on top of the map register here while they are open. The deck does it
    /// today; the shop and the events will do the same. <see cref="MapScreen"/> ignores the
    /// wheel and the drag as long as <see cref="Count"/> is above zero, so scrolling a list
    /// never moves the map behind it.
    /// </summary>
    public static class OverlayInput
    {
        /// <summary>How many input-blocking overlays are open right now.</summary>
        public static int Count { get; private set; }

        /// <summary>Registers an overlay that has just opened.</summary>
        public static void Push()
        {
            Count++;
        }

        /// <summary>Unregisters an overlay that has just closed. It must be paired with <see cref="Push"/>.</summary>
        public static void Pop()
        {
            if (Count > 0) Count--;
        }
    }
}
