using System;
using Microsoft.Xna.Framework;

namespace GameProject {
    /// <summary>
    /// Double precision Vector2. World coordinates use this so that the canvas stays
    /// precise far away from the origin. Only camera-relative values get converted
    /// back to float right before rendering.
    /// </summary>
    public struct Vector2D : IEquatable<Vector2D> {
        public Vector2D(double x, double y) {
            X = x;
            Y = y;
        }
        public Vector2D(double value) {
            X = value;
            Y = value;
        }

        public double X;
        public double Y;

        public static Vector2D Zero => new(0.0, 0.0);
        public static Vector2D One => new(1.0, 1.0);

        public readonly double Length() => Math.Sqrt(X * X + Y * Y);
        public readonly double LengthSquared() => X * X + Y * Y;

        public static double Distance(Vector2D a, Vector2D b) => (a - b).Length();
        public static double Dot(Vector2D a, Vector2D b) => a.X * b.X + a.Y * b.Y;
        public static Vector2D Min(Vector2D a, Vector2D b) => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        public static Vector2D Max(Vector2D a, Vector2D b) => new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

        public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator -(Vector2D a) => new(-a.X, -a.Y);
        public static Vector2D operator *(Vector2D a, double s) => new(a.X * s, a.Y * s);
        public static Vector2D operator *(double s, Vector2D a) => new(a.X * s, a.Y * s);
        public static Vector2D operator /(Vector2D a, double s) => new(a.X / s, a.Y / s);
        public static bool operator ==(Vector2D a, Vector2D b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector2D a, Vector2D b) => !(a == b);

        public static implicit operator Vector2D(Vector2 v) => new(v.X, v.Y);
        public static explicit operator Vector2(Vector2D v) => new((float)v.X, (float)v.Y);

        public readonly bool Equals(Vector2D other) => this == other;
        public override readonly bool Equals(object? obj) => obj is Vector2D other && this == other;
        public override readonly int GetHashCode() => HashCode.Combine(X, Y);
        public override readonly string ToString() => $"{{X:{X} Y:{Y}}}";
    }

    public static class MathD {
        /// <summary>
        /// Distance between the closest points of two segments (Ericson, Real-Time
        /// Collision Detection §5.1.9). Handles degenerate (point) segments. Two
        /// capsules touch when this is at most the sum of their radii.
        /// </summary>
        public static double SegmentSegmentDistance(Vector2D p1, Vector2D q1, Vector2D p2, Vector2D q2) {
            Vector2D d1 = q1 - p1;
            Vector2D d2 = q2 - p2;
            Vector2D r = p1 - p2;
            double a = Vector2D.Dot(d1, d1);
            double e = Vector2D.Dot(d2, d2);
            double f = Vector2D.Dot(d2, r);
            double s, t;
            if (a <= double.Epsilon && e <= double.Epsilon) {
                s = 0.0;
                t = 0.0;
            } else if (a <= double.Epsilon) {
                s = 0.0;
                t = Math.Clamp(f / e, 0.0, 1.0);
            } else {
                double c = Vector2D.Dot(d1, r);
                if (e <= double.Epsilon) {
                    t = 0.0;
                    s = Math.Clamp(-c / a, 0.0, 1.0);
                } else {
                    double b = Vector2D.Dot(d1, d2);
                    double denom = a * e - b * b;
                    s = denom > 0.0 ? Math.Clamp((b * f - c * e) / denom, 0.0, 1.0) : 0.0;
                    t = b * s + f;
                    if (t < 0.0) {
                        t = 0.0;
                        s = Math.Clamp(-c / a, 0.0, 1.0);
                    } else if (t > e) {
                        t = 1.0;
                        s = Math.Clamp((b - c) / a, 0.0, 1.0);
                    } else {
                        t /= e;
                    }
                }
            }
            return Vector2D.Distance(p1 + d1 * s, p2 + d2 * t);
        }

        /// <summary>
        /// Distance from a segment to an axis aligned rectangle: zero when they touch
        /// or the segment is inside, otherwise the gap to the nearest edge. A capsule
        /// touches the rect when this is at most its radius.
        /// </summary>
        public static double SegmentRectDistance(Vector2D a, Vector2D b, RectangleD rect) {
            if (rect.Contains(a) || rect.Contains(b)) return 0.0;
            Vector2D tl = new(rect.Left, rect.Top);
            Vector2D tr = new(rect.Right, rect.Top);
            Vector2D br = new(rect.Right, rect.Bottom);
            Vector2D bl = new(rect.Left, rect.Bottom);
            double d = SegmentSegmentDistance(a, b, tl, tr);
            d = Math.Min(d, SegmentSegmentDistance(a, b, tr, br));
            d = Math.Min(d, SegmentSegmentDistance(a, b, br, bl));
            return Math.Min(d, SegmentSegmentDistance(a, b, bl, tl));
        }
    }

    /// <summary>
    /// Double precision axis aligned rectangle used for world space bounds.
    /// </summary>
    public struct RectangleD : IEquatable<RectangleD> {
        public RectangleD(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X;
        public double Y;
        public double Width;
        public double Height;

        public readonly double Left => X;
        public readonly double Top => Y;
        public readonly double Right => X + Width;
        public readonly double Bottom => Y + Height;
        public readonly Vector2D Center => new(X + Width / 2.0, Y + Height / 2.0);

        public static RectangleD FromBounds(double left, double top, double right, double bottom) {
            return new RectangleD(left, top, right - left, bottom - top);
        }

        public static RectangleD Union(RectangleD a, RectangleD b) {
            double left = Math.Min(a.Left, b.Left);
            double top = Math.Min(a.Top, b.Top);
            double right = Math.Max(a.Right, b.Right);
            double bottom = Math.Max(a.Bottom, b.Bottom);
            return FromBounds(left, top, right, bottom);
        }

        public readonly bool Intersects(RectangleD other) {
            return Left <= other.Right && other.Left <= Right && Top <= other.Bottom && other.Top <= Bottom;
        }
        public readonly bool Contains(Vector2D v) {
            return Left <= v.X && v.X <= Right && Top <= v.Y && v.Y <= Bottom;
        }
        public readonly bool Contains(RectangleD other) {
            return Left <= other.Left && other.Right <= Right && Top <= other.Top && other.Bottom <= Bottom;
        }

        public static bool operator ==(RectangleD a, RectangleD b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        public static bool operator !=(RectangleD a, RectangleD b) => !(a == b);

        public readonly bool Equals(RectangleD other) => this == other;
        public override readonly bool Equals(object? obj) => obj is RectangleD other && this == other;
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
        public override readonly string ToString() => $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";
    }
}
