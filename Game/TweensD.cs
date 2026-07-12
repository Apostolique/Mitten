using Apos.Tweens;

namespace GameProject {
    public delegate double InterpolatorD(double x);

    /// <summary>
    /// Double precision easings. The camera pan and zoom tweens need these: easing a
    /// float t over a large pan delta wobbles by around a pixel at high zoom.
    /// </summary>
    public static class EasingD {
        public static double Linear(double x) => x;

        public static double QuintOut(double x) {
            double x2 = --x * x;
            return 1.0 + x * x2 * x2;
        }
    }

    public class DoubleTween : ITween<double> {
        public DoubleTween(double a, double b, long duration, InterpolatorD interpolator) {
            A = a;
            B = b;
            StartTime = TweenHelper.TotalMS;
            Duration = duration;
            Interpolator = interpolator;
        }

        public double A { get; set; }
        public double B { get; set; }
        public long StartTime { get; set; }
        public long Duration { get; set; }
        public InterpolatorD Interpolator { get; set; }

        public double Value => ValueAt(TweenHelper.TotalMS - StartTime);
        public double ValueAt(long ms) {
            // Duration is checked first so zero-duration tweens return B on the
            // tick they are set: gesture code reads the value right back, and a
            // stale A there turns the drag feedback loop into an oscillator.
            if (ms >= Duration) return B;
            else if (ms <= 0) return A;

            return A + (B - A) * Interpolator(ms / (double)Duration);
        }
    }

    public class Vector2DTween : ITween<Vector2D> {
        public Vector2DTween(Vector2D a, Vector2D b, long duration, InterpolatorD interpolator) {
            A = a;
            B = b;
            StartTime = TweenHelper.TotalMS;
            Duration = duration;
            Interpolator = interpolator;
        }

        public Vector2D A { get; set; }
        public Vector2D B { get; set; }
        public long StartTime { get; set; }
        public long Duration { get; set; }
        public InterpolatorD Interpolator { get; set; }

        public Vector2D Value => ValueAt(TweenHelper.TotalMS - StartTime);
        public Vector2D ValueAt(long ms) {
            // Duration first, same as DoubleTween: zero-duration must return B.
            if (ms >= Duration) return B;
            else if (ms <= 0) return A;

            return A + (B - A) * Interpolator(ms / (double)Duration);
        }
    }
}
