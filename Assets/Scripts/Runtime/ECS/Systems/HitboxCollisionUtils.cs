using Unity.Mathematics;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Static utility class with Burst-compatible collision math.
    /// Tests player circle vs various bullet hitbox shapes.
    /// All functions are pure static with no managed types.
    /// </summary>
    public static class HitboxCollisionUtils
    {
        /// <summary>
        /// Circle vs Circle: distSq &lt;= (rA + rB)^2
        /// </summary>
        public static bool CircleVsCircle(float2 posA, float radiusA, float2 posB, float radiusB)
        {
            var radiusSum = radiusA + radiusB;
            return math.distancesq(posA, posB) <= radiusSum * radiusSum;
        }

        /// <summary>
        /// Circle vs Oval: transform circle into oval's local space, scale axes
        /// to normalize the ellipse into a unit circle, then test.
        /// ovalSize = (halfWidth, halfHeight), ovalAngle = rotation in radians.
        /// </summary>
        public static bool CircleVsOval(
            float2 circlePos, float circleR,
            float2 ovalPos, float2 ovalSize, float ovalAngle)
        {
            // Rotate circle position into oval's local space
            var delta = circlePos - ovalPos;
            var cos = math.cos(-ovalAngle);
            var sin = math.sin(-ovalAngle);
            var local = new float2(
                delta.x * cos - delta.y * sin,
                delta.x * sin + delta.y * cos);

            // Scale to make oval into circle
            var scale = new float2(1f / ovalSize.x, 1f / ovalSize.y);
            var scaledPos = local * scale;
            var scaledRadius = circleR * math.max(scale.x, scale.y); // conservative

            return math.lengthsq(scaledPos) <= (1f + scaledRadius) * (1f + scaledRadius);
        }

        /// <summary>
        /// Circle vs Rect: transform to rect local space, clamp to rect, check distance.
        /// rectSize = (halfWidth, halfHeight), rectAngle = rotation in radians.
        /// </summary>
        public static bool CircleVsRect(
            float2 circlePos, float circleR,
            float2 rectPos, float2 rectSize, float rectAngle)
        {
            // Rotate circle into rect's local space
            var delta = circlePos - rectPos;
            var cos = math.cos(-rectAngle);
            var sin = math.sin(-rectAngle);
            var local = new float2(
                delta.x * cos - delta.y * sin,
                delta.x * sin + delta.y * cos);

            // Find closest point on rect to circle center
            var closest = math.clamp(local, -rectSize, rectSize);
            var diff = local - closest;

            return math.lengthsq(diff) <= circleR * circleR;
        }

        /// <summary>
        /// Circle vs Line: point-to-segment distance.
        /// lineSize = (halfLength, halfThickness).
        /// The line extends along lineAngle direction from -halfLength to +halfLength,
        /// with halfThickness acting as the line's "radius".
        /// </summary>
        public static bool CircleVsLine(
            float2 circlePos, float circleR,
            float2 linePos, float2 lineSize, float lineAngle)
        {
            // Line as segment from -halfLength to +halfLength along lineAngle
            var dir = new float2(math.cos(lineAngle), math.sin(lineAngle));
            var start = linePos - dir * lineSize.x;
            var end = linePos + dir * lineSize.x;

            // Point to segment distance
            var seg = end - start;
            var segLenSq = math.lengthsq(seg);
            float t = 0f;
            if (segLenSq > 0f)
                t = math.saturate(math.dot(circlePos - start, seg) / segLenSq);
            var closest = start + seg * t;

            var combinedR = circleR + lineSize.y; // thickness acts as line "radius"
            return math.distancesq(circlePos, closest) <= combinedR * combinedR;
        }

        /// <summary>
        /// Dispatcher: test player circle vs bullet hitbox.
        /// Player is always circle; bullet uses BulletHitbox shape.
        /// bulletAngle is the bullet's current facing direction (from BulletMotion.Angle).
        /// </summary>
        public static bool TestHitbox(
            float2 playerPos, float playerR,
            float2 bulletPos, BulletHitbox hitbox, float bulletAngle)
        {
            var hbPos = bulletPos + hitbox.Offset;

            switch (hitbox.Type)
            {
                case HitboxType.Circle:
                    return CircleVsCircle(playerPos, playerR, hbPos, hitbox.Size.x);
                case HitboxType.Oval:
                    return CircleVsOval(playerPos, playerR, hbPos, hitbox.Size, bulletAngle);
                case HitboxType.Rect:
                    return CircleVsRect(playerPos, playerR, hbPos, hitbox.Size, bulletAngle);
                case HitboxType.Line:
                    return CircleVsLine(playerPos, playerR, hbPos, hitbox.Size, bulletAngle);
                case HitboxType.None:
                default:
                    return false;
            }
        }
    }
}
