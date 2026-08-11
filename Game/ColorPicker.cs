using System;
using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class ColorPicker(GraphicsDevice g) {
        public Color[][] Colors = null!;

        // The area is given in CSS pixels, which is the unit the mouse and the contacts arrive
        // in. The viewport counts device pixels and the two differ in a browser on a high
        // density screen, so reading the viewport here would put the swatches under the wrong
        // column. It doesn't always start at the origin either: a touch screen keeps the
        // button bar up on top of the picker, and the grid lays itself out beside it.
        public Color ColorAt(Vector2 xy, Vector2 size, Vector2 p) {
            (int family, int shade) = PointToColor(xy, size, p);

            return Colors[family][shade];
        }
        public void Draw(ShapeFont font, bool isBackground, Color bgColor, Vector2 xy, Vector2 size, Vector2 p) {
            _sb.Begin(GameRoot.UiMatrix);
            _sb.FillRectangle(xy, size, isBackground ? bgColor : TWColor.Black);
            for (int i = 0; i < Colors.Length; i++) {
                for (int j = 0; j < Colors[i].Length; j++) {
                    (Vector2 cell, Vector2 cellSize) = Swatch(xy, size, i, j);
                    _sb.FillRectangle(cell, cellSize, Colors[i][j]);
                }
            }
            (int family, int shade) = PointToColor(xy, size, p);
            (Vector2 at, Vector2 atSize) = Cell(xy, size, family, shade);
            _sb.BorderRectangle(at, atSize, TWColor.Black, 6f);
            _sb.BorderRectangle(at + new Vector2(2f), atSize - new Vector2(4f), TWColor.White, 2f);
            _sb.End();

            // A second pass rather than more calls in the one above, so the labels land on
            // top of the swatches instead of wherever the batch happens to order them.
            _sb.Begin(GameRoot.UiMatrix);
            for (int i = 0; i < Colors.Length; i++) {
                for (int j = 0; j < Colors[i].Length; j++) {
                    var c = j < MathF.Ceiling(Colors[i].Length / 2f) ? TWColor.Black : TWColor.White;
                    (Vector2 cell, Vector2 _) = Swatch(xy, size, i, j);
                    _sb.DrawString(font, $"{j}", cell, 20f, c.SetAlpha(0.4f));
                }
            }
            _sb.End();
        }

        /// <summary>
        /// Whether the families run down the area instead of across it. There are 23 of them
        /// against 11 shades, so on a screen taller than it is wide they go down the long way
        /// and each swatch comes out roughly square instead of a 17 px ribbon.
        /// </summary>
        private static bool Transposed(Vector2 size) => size.Y > size.X;

        /// <summary>One swatch's whole cell, gaps included. The selection outline uses this
        /// while the fills leave a sliver between families.</summary>
        private (Vector2 XY, Vector2 Size) Cell(Vector2 xy, Vector2 size, int family, int shade) {
            bool t = Transposed(size);
            float familySize = (t ? size.Y : size.X) / Colors.Length;
            float shadeSize = (t ? size.X : size.Y) / Colors[family].Length;
            float alongFamily = family * familySize;
            float alongShade = shade * shadeSize;

            return t
                ? (xy + new Vector2(alongShade, alongFamily), new Vector2(shadeSize, familySize))
                : (xy + new Vector2(alongFamily, alongShade), new Vector2(familySize, shadeSize));
        }

        /// <summary>The filled part of a cell, inset so the families read as separate strips.</summary>
        private (Vector2 XY, Vector2 Size) Swatch(Vector2 xy, Vector2 size, int family, int shade) {
            (Vector2 cell, Vector2 cellSize) = Cell(xy, size, family, shade);
            if (Transposed(size)) {
                return (cell + new Vector2(0f, cellSize.Y * 0.08f), new Vector2(cellSize.X, cellSize.Y * 0.84f));
            }
            return (cell + new Vector2(cellSize.X * 0.08f, 0f), new Vector2(cellSize.X * 0.84f, cellSize.Y));
        }

        private (int Family, int Shade) PointToColor(Vector2 xy, Vector2 size, Vector2 p) {
            bool t = Transposed(size);
            Vector2 local = p - xy;
            float alongFamily = t ? local.Y : local.X;
            float alongShade = t ? local.X : local.Y;

            // A grid narrower than one pixel per family would divide by zero.
            float familySize = MathF.Max(1f, (t ? size.Y : size.X) / Colors.Length);
            int family = MathHelper.Clamp((int)(alongFamily / familySize), 0, Colors.Length - 1);
            float shadeSize = MathF.Max(1f, (t ? size.X : size.Y) / Colors[family].Length);
            int shade = MathHelper.Clamp((int)(alongShade / shadeSize), 0, Colors[family].Length - 1);

            return (family, shade);
        }

        ShapeBatch _sb = new(g);
    }
}
