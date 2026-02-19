using NUnit.Framework;
using Unity.Mathematics;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// Pure unit tests for HitboxCollisionUtils math functions.
    /// No World or System needed - just collision geometry.
    /// </summary>
    [TestFixture]
    public class HitboxCollisionTests
    {
        // --- CircleVsCircle ---

        [Test]
        public void CircleVsCircle_Overlap_ReturnsTrue()
        {
            // Two circles at same position
            var result = HitboxCollisionUtils.CircleVsCircle(
                float2.zero, 0.5f,
                float2.zero, 0.5f);
            Assert.IsTrue(result, "Overlapping circles should collide");
        }

        [Test]
        public void CircleVsCircle_Touching_ReturnsTrue()
        {
            // Two circles just touching (dist == r1 + r2)
            var result = HitboxCollisionUtils.CircleVsCircle(
                new float2(0f, 0f), 0.5f,
                new float2(1f, 0f), 0.5f);
            Assert.IsTrue(result, "Touching circles should collide");
        }

        [Test]
        public void CircleVsCircle_Miss_ReturnsFalse()
        {
            // Two circles far apart
            var result = HitboxCollisionUtils.CircleVsCircle(
                new float2(0f, 0f), 0.5f,
                new float2(5f, 0f), 0.5f);
            Assert.IsFalse(result, "Distant circles should not collide");
        }

        // --- CircleVsOval ---

        [Test]
        public void CircleVsOval_Overlap_ReturnsTrue()
        {
            // Circle at center of oval
            var result = HitboxCollisionUtils.CircleVsOval(
                float2.zero, 0.1f,
                float2.zero, new float2(1f, 0.5f), 0f);
            Assert.IsTrue(result, "Circle inside oval should collide");
        }

        [Test]
        public void CircleVsOval_NearEdge_ReturnsTrue()
        {
            // Circle near the wide side of oval (within reach)
            var result = HitboxCollisionUtils.CircleVsOval(
                new float2(0.9f, 0f), 0.2f,
                float2.zero, new float2(1f, 0.5f), 0f);
            Assert.IsTrue(result, "Circle near oval edge should collide");
        }

        [Test]
        public void CircleVsOval_Miss_ReturnsFalse()
        {
            // Circle far above narrow side of oval
            var result = HitboxCollisionUtils.CircleVsOval(
                new float2(0f, 3f), 0.1f,
                float2.zero, new float2(1f, 0.5f), 0f);
            Assert.IsFalse(result, "Distant circle should not collide with oval");
        }

        // --- CircleVsRect ---

        [Test]
        public void CircleVsRect_Overlap_ReturnsTrue()
        {
            // Circle at center of rect
            var result = HitboxCollisionUtils.CircleVsRect(
                float2.zero, 0.1f,
                float2.zero, new float2(1f, 0.5f), 0f);
            Assert.IsTrue(result, "Circle inside rect should collide");
        }

        [Test]
        public void CircleVsRect_NearEdge_ReturnsTrue()
        {
            // Circle just touching the right edge
            var result = HitboxCollisionUtils.CircleVsRect(
                new float2(1.05f, 0f), 0.1f,
                float2.zero, new float2(1f, 0.5f), 0f);
            Assert.IsTrue(result, "Circle touching rect edge should collide");
        }

        [Test]
        public void CircleVsRect_Miss_ReturnsFalse()
        {
            // Circle far from rect
            var result = HitboxCollisionUtils.CircleVsRect(
                new float2(5f, 5f), 0.1f,
                float2.zero, new float2(1f, 0.5f), 0f);
            Assert.IsFalse(result, "Distant circle should not collide with rect");
        }

        [Test]
        public void CircleVsRect_Rotated_Overlap()
        {
            // Rect rotated 90 degrees. Original half-size (2, 0.5).
            // After rotation, the wide axis is now vertical.
            // A circle at (0, 1.5) should be inside the rotated rect.
            var result = HitboxCollisionUtils.CircleVsRect(
                new float2(0f, 1.5f), 0.1f,
                float2.zero, new float2(2f, 0.5f), math.PI / 2f);
            Assert.IsTrue(result, "Circle should collide with rotated rect");
        }

        [Test]
        public void CircleVsRect_Rotated_Miss()
        {
            // Rect rotated 90 degrees with half-size (2, 0.5).
            // After rotation, the thin axis is now horizontal.
            // A circle at (1.5, 0) should miss (thin side is only 0.5 half-width, now horizontal).
            var result = HitboxCollisionUtils.CircleVsRect(
                new float2(1.5f, 0f), 0.1f,
                float2.zero, new float2(2f, 0.5f), math.PI / 2f);
            Assert.IsFalse(result, "Circle should miss rotated rect on narrow side");
        }

        // --- CircleVsLine ---

        [Test]
        public void CircleVsLine_Overlap_ReturnsTrue()
        {
            // Circle at center of a horizontal line
            var result = HitboxCollisionUtils.CircleVsLine(
                float2.zero, 0.1f,
                float2.zero, new float2(2f, 0.1f), 0f);
            Assert.IsTrue(result, "Circle on line should collide");
        }

        [Test]
        public void CircleVsLine_NearEndpoint_ReturnsTrue()
        {
            // Circle near end of horizontal line (halfLength=2, so line goes from -2 to +2)
            var result = HitboxCollisionUtils.CircleVsLine(
                new float2(1.9f, 0f), 0.2f,
                float2.zero, new float2(2f, 0.1f), 0f);
            Assert.IsTrue(result, "Circle near line endpoint should collide");
        }

        [Test]
        public void CircleVsLine_Miss_ReturnsFalse()
        {
            // Circle far above a horizontal line
            var result = HitboxCollisionUtils.CircleVsLine(
                new float2(0f, 5f), 0.1f,
                float2.zero, new float2(2f, 0.1f), 0f);
            Assert.IsFalse(result, "Distant circle should not collide with line");
        }

        [Test]
        public void CircleVsLine_PastEndpoint_Miss()
        {
            // Circle past the end of the line segment
            var result = HitboxCollisionUtils.CircleVsLine(
                new float2(5f, 0f), 0.1f,
                float2.zero, new float2(2f, 0.1f), 0f);
            Assert.IsFalse(result, "Circle past line endpoint should not collide");
        }

        // --- TestHitbox dispatcher ---

        [Test]
        public void TestHitbox_Circle_Dispatches()
        {
            var hitbox = new BulletHitbox
            {
                Type = HitboxType.Circle,
                Size = new float2(0.5f, 0f),
                Offset = float2.zero
            };

            var result = HitboxCollisionUtils.TestHitbox(
                float2.zero, 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsTrue(result, "TestHitbox should dispatch Circle type correctly");
        }

        [Test]
        public void TestHitbox_Oval_Dispatches()
        {
            var hitbox = new BulletHitbox
            {
                Type = HitboxType.Oval,
                Size = new float2(1f, 0.5f),
                Offset = float2.zero
            };

            var result = HitboxCollisionUtils.TestHitbox(
                float2.zero, 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsTrue(result, "TestHitbox should dispatch Oval type correctly");
        }

        [Test]
        public void TestHitbox_Rect_Dispatches()
        {
            var hitbox = new BulletHitbox
            {
                Type = HitboxType.Rect,
                Size = new float2(1f, 0.5f),
                Offset = float2.zero
            };

            var result = HitboxCollisionUtils.TestHitbox(
                float2.zero, 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsTrue(result, "TestHitbox should dispatch Rect type correctly");
        }

        [Test]
        public void TestHitbox_Line_Dispatches()
        {
            var hitbox = new BulletHitbox
            {
                Type = HitboxType.Line,
                Size = new float2(2f, 0.1f),
                Offset = float2.zero
            };

            var result = HitboxCollisionUtils.TestHitbox(
                float2.zero, 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsTrue(result, "TestHitbox should dispatch Line type correctly");
        }

        [Test]
        public void TestHitbox_None_AlwaysFalse()
        {
            var hitbox = new BulletHitbox
            {
                Type = HitboxType.None,
                Size = new float2(100f, 100f),
                Offset = float2.zero
            };

            var result = HitboxCollisionUtils.TestHitbox(
                float2.zero, 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsFalse(result, "HitboxType.None should never collide");
        }

        [Test]
        public void TestHitbox_Offset_AppliedCorrectly()
        {
            // Circle hitbox at bulletPos=(0,0) with offset=(5,0), radius=0.5
            // Player at (5,0) should collide (effective hitbox center is at (5,0))
            var hitbox = new BulletHitbox
            {
                Type = HitboxType.Circle,
                Size = new float2(0.5f, 0f),
                Offset = new float2(5f, 0f)
            };

            var result = HitboxCollisionUtils.TestHitbox(
                new float2(5f, 0f), 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsTrue(result, "Offset should shift hitbox center");

            // Player at (0,0) should NOT collide (hitbox center is at (5,0))
            var resultMiss = HitboxCollisionUtils.TestHitbox(
                float2.zero, 0.1f,
                float2.zero, hitbox, 0f);
            Assert.IsFalse(resultMiss, "Player at origin should miss offset hitbox");
        }
    }
}
