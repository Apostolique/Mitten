using System;
using Microsoft.Xna.Framework;

namespace GameProject {
    /// <summary>
    /// One renderable primitive in camera-relative pixels: a capsule when A != B, a
    /// disc when A == B. Sorted by Id to preserve paint order.
    /// </summary>
    public readonly struct Drawable(int id, Vector2 a, Vector2 b, float radius, Color color) {
        public readonly int Id = id;
        public readonly Vector2 A = a;
        public readonly Vector2 B = b;
        public readonly float Radius = radius;
        public readonly Color Color = color;
    }

    /// <summary>
    /// One drawn line segment. Coordinates are local to the frame it is anchored to
    /// (<see cref="Node"/>); a pen stroke is many of these grouped as one undo unit.
    /// </summary>
    public class Line {
        public Line(int id, Vector2D a, Vector2D b, double radius, Color c) {
            Id = id;
            A = a;
            B = b;
            Radius = radius;
            Color = c;
            AABB = ComputeAABB();
        }

        public int Id { get; set; }
        public int Leaf { get; set; }
        public Frame Node { get; set; } = null!;
        public Vector2D A { get; set; }
        public Vector2D B { get; set; }
        public double Radius { get; set; }
        public Color Color { get; set; }

        public RectangleD AABB { get; set; }

        private RectangleD ComputeAABB() {
            double left = Math.Min(A.X, B.X) - Radius;
            double top = Math.Min(A.Y, B.Y) - Radius;
            double right = Math.Max(A.X, B.X) + Radius;
            double bottom = Math.Max(A.Y, B.Y) + Radius;

            return new RectangleD(left, top, right - left, bottom - top);
        }
    }
}
