namespace Prototype.Mapgen
{
    /// <summary>
    /// Where a room sits on screen for its ring and angle. The generator and the map screen
    /// both read the layout from here: if they disagreed, rooms would be linked by one
    /// geometry and drawn with another, and the roads would cross the whole map.
    /// The map is an ellipse, wider than it is tall, to use a 16:9 screen.
    /// </summary>
    public static class MapLayout
    {
        public const float FirstRingRadius = 88f;
        public const float RingSpacing = 60f;
        public const float Wide = 1.45f;

        public static float RadiusOf(int ring)
        {
            return ring == 0 ? 0f : FirstRingRadius + (ring - 1) * RingSpacing;
        }

        /// <summary>Angle 0 points up and grows clockwise; x grows right, y grows up.</summary>
        public static void Project(int ring, float angle, out float x, out float y)
        {
            float radius = RadiusOf(ring);
            float radians = (90f - angle) * (float)System.Math.PI / 180f;
            x = (float)System.Math.Cos(radians) * radius * Wide;
            y = (float)System.Math.Sin(radians) * radius;
        }

        public static float SqrDistance(MapNode a, MapNode b)
        {
            Project(a.Ring, a.Angle, out float ax, out float ay);
            Project(b.Ring, b.Angle, out float bx, out float by);
            float dx = ax - bx;
            float dy = ay - by;
            return dx * dx + dy * dy;
        }
    }
}
