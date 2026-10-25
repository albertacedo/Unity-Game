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

        /// <summary>Segments used to draw a road that follows a ring.</summary>
        public const int ArcSteps = 16;

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

        /// <summary>
        /// A point on the arc that runs clockwise from one room to the next, used for roads
        /// between rooms of the same ring. Following the ring keeps those roads away from the
        /// radial ones, which only touch a ring at the rooms themselves.
        /// </summary>
        public static void ProjectArc(int ring, float fromAngle, float toAngle, float t,
            out float x, out float y)
        {
            Project(ring, fromAngle + Sweep(fromAngle, toAngle) * t, out x, out y);
        }

        public static float Sweep(float fromAngle, float toAngle)
        {
            float sweep = (toAngle - fromAngle) % 360f;
            return sweep < 0f ? sweep + 360f : sweep;
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
