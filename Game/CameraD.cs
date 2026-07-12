using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    /// <summary>
    /// Double precision 2D camera built for floating origin rendering: world positions
    /// stay in doubles and get converted to small camera-relative floats with
    /// <see cref="WorldToView"/> right before rendering. The <see cref="View"/> matrix
    /// only applies rotation and the viewport center translation, so the GPU never sees
    /// large coordinates. Replaces Apos.Camera with the same view semantics
    /// (rotation and uniform scale commute).
    /// </summary>
    public class CameraD {
        public CameraD(GraphicsDevice graphicsDevice) {
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>World position of the camera (center of the screen).</summary>
        public Vector2D XY { get; set; } = Vector2D.Zero;
        /// <summary>Screen pixels per world unit.</summary>
        public double Scale { get; set; } = 1.0;
        public float Rotation { get; set; } = 0f;

        public Vector2 Origin {
            get {
                var vp = _graphicsDevice.Viewport;
                return new Vector2(vp.Width / 2f, vp.Height / 2f);
            }
        }

        /// <summary>
        /// View matrix for camera-relative vertices produced by <see cref="WorldToView"/>.
        /// Translation and scale already happened in doubles on the CPU.
        /// </summary>
        public Matrix View {
            get {
                var origin = Origin;
                return
                    Matrix.CreateRotationZ(Rotation) *
                    Matrix.CreateTranslation(origin.X, origin.Y, 0f);
            }
        }

        /// <summary>Converts a world position to camera-relative screen pixels (pre-rotation).</summary>
        public Vector2 WorldToView(Vector2D xy) {
            return (Vector2)((xy - XY) * Scale);
        }
        /// <summary>Converts a world radius or length to screen pixels.</summary>
        public float WorldToViewScale(double length) {
            return (float)(length * Scale);
        }

        /// <summary>World units per screen pixel.</summary>
        public double ScreenToWorldScale() => 1.0 / Scale;

        public Vector2D ScreenToWorld(float x, float y) => ScreenToFrame(x, y, XY, Scale);
        public Vector2D ScreenToWorld(Vector2 xy) => ScreenToFrame(xy.X, xy.Y, XY, Scale);

        /// <summary>World space bounding rectangle of the screen.</summary>
        public RectangleD ViewRect => ViewRectIn(XY, Scale);

        /// <summary>
        /// Bounding rectangle of the screen in an arbitrary frame, given the camera's
        /// position in that frame and the frame's pixels per unit.
        /// </summary>
        public RectangleD ViewRectIn(Vector2D camXY, double pxPerUnit) {
            var vp = _graphicsDevice.Viewport;
            Vector2D a = ScreenToFrame(0f, 0f, camXY, pxPerUnit);
            Vector2D b = ScreenToFrame(vp.Width, 0f, camXY, pxPerUnit);
            Vector2D c = ScreenToFrame(0f, vp.Height, camXY, pxPerUnit);
            Vector2D d = ScreenToFrame(vp.Width, vp.Height, camXY, pxPerUnit);

            double left = Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X));
            double right = Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X));
            double top = Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
            double bottom = Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));

            return RectangleD.FromBounds(left, top, right, bottom);
        }

        private Vector2D ScreenToFrame(float x, float y, Vector2D camXY, double pxPerUnit) {
            var origin = Origin;
            double dx = x - origin.X;
            double dy = y - origin.Y;
            double cos = Math.Cos(-Rotation);
            double sin = Math.Sin(-Rotation);
            double rx = dx * cos - dy * sin;
            double ry = dx * sin + dy * cos;
            return new Vector2D(camXY.X + rx / pxPerUnit, camXY.Y + ry / pxPerUnit);
        }

        private readonly GraphicsDevice _graphicsDevice;
    }
}
