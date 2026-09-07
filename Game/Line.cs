using System;
using Microsoft.Xna.Framework;

namespace GameProject {
    /// <summary>
    /// One renderable primitive in camera-relative pixels: a capsule when A != B, a
    /// disc when A == B. Sorted by Id to preserve paint order.
    /// </summary>
    public readonly struct Drawable {
        /// <summary>One of a single width.</summary>
        public Drawable(int id, Vector2 a, Vector2 b, float radius, Color color)
            : this(id, a, b, radius, radius, color) { }
        /// <summary>One that tapers, radiusA wide at A and radius wide at B.</summary>
        public Drawable(int id, Vector2 a, Vector2 b, float radiusA, float radius, Color color) {
            Id = id;
            A = a;
            B = b;
            RadiusA = radiusA;
            Radius = radius;
            Color = color;
        }

        public readonly int Id;
        public readonly Vector2 A;
        public readonly Vector2 B;
        public readonly float RadiusA;
        public readonly float Radius;
        public readonly Color Color;
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
            RadiusA = radius;
            Color = c;
            AABB = ComputeAABB();
        }

        public int Id { get; set; }
        // The id of the first segment in the pen stroke this segment belongs to.
        // Survives partial erasure: fragments of a stroke still select as one.
        public int StrokeId { get; set; }
        public int Leaf { get; set; }
        public Frame Node { get; set; } = null!;
        public Vector2D A { get; set; }
        public Vector2D B { get; set; }
        /// <summary>The half width at <see cref="B"/>. A segment only carries its own
        /// end's width, so the width it starts at is <see cref="RadiusA"/>.</summary>
        public double Radius { get; set; }
        /// <summary>
        /// The half width at <see cref="A"/>, which is the radius of the segment before
        /// this one in the same stroke. A segment that starts a stroke, or whose
        /// predecessor was erased, recolored or anchored to another frame, opens at its
        /// own. Derived from the neighbours rather than saved: GameRoot's AttachLine and
        /// DetachLine keep it current.
        /// </summary>
        public double RadiusA { get; set; }
        public Color Color { get; set; }

        public RectangleD AABB { get; set; }

        public void RecomputeAABB() {
            AABB = ComputeAABB();
        }

        private RectangleD ComputeAABB() {
            double left = Math.Min(A.X, B.X) - Radius;
            double top = Math.Min(A.Y, B.Y) - Radius;
            double right = Math.Max(A.X, B.X) + Radius;
            double bottom = Math.Max(A.Y, B.Y) + Radius;

            return new RectangleD(left, top, right - left, bottom - top);
        }
    }
}
