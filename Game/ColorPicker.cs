using System;
using Apos.Input;
using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class ColorPicker(GraphicsDevice g) {
        public Color[][] Colors = null!;

        // `view` is the screen in CSS pixels, which is the unit the mouse arrives in. The
        // viewport counts device pixels and the two differ in a browser on a high density
        // screen, so reading the viewport here would put the swatches under the wrong column.
        public Color UpdateInput(Vector2 view) {
            (int x, int y) = MouseToColor(view);

            return Colors[x][y];
        }
        public void Draw(ShapeFont font, bool isBackground, Color bgColor, Vector2 view) {
            var width = view.X / Colors.Length;
            _sb.Begin(GameRoot.UiMatrix);
            _sb.FillRectangle(new Vector2(0, 0), view, isBackground ? bgColor : TWColor.Black);
            for (int i = 0; i < Colors.Length; i++) {
                var height = view.Y / Colors[i].Length;
                for (int j = 0; j < Colors[i].Length; j++) {
                    _sb.FillRectangle(new Vector2(i * width + width * 0.08f, j * height), new Vector2(width * 0.84f, height), Colors[i][j]);
                }
            }
            (int x, int y) = MouseToColor(view);
            var selectHeight = view.Y / Colors[x].Length;
            _sb.BorderRectangle(new Vector2(x * width, y * selectHeight), new Vector2(width, selectHeight), TWColor.Black, 6f);
            _sb.BorderRectangle(new Vector2(x * width + 2f, y * selectHeight + 2f), new Vector2(width - 4f, selectHeight - 4f), TWColor.White, 2f);
            _sb.End();

            // A second pass rather than more calls in the one above, so the labels land on
            // top of the swatches instead of wherever the batch happens to order them.
            _sb.Begin(GameRoot.UiMatrix);
            for (int i = 0; i < Colors.Length; i++) {
                var height = view.Y / Colors[i].Length;
                for (int j = 0; j < Colors[i].Length; j++) {
                    var c = j < MathF.Ceiling(Colors[i].Length / 2f) ? TWColor.Black : TWColor.White;
                    _sb.DrawString(font, $"{j}", new Vector2(i * width + width * 0.08f, j * height), 20f, c.SetAlpha(0.4f));
                }
            }
            _sb.End();
        }

        private (int, int) MouseToColor(Vector2 view) {
            var width = (int)view.X / Colors.Length;
            int x = MathHelper.Clamp((int)Pointer.Position.X / width, 0, Colors.Length - 1);
            var height = (int)view.Y / Colors[x].Length;
            int y = MathHelper.Clamp((int)Pointer.Position.Y / height, 0, Colors[x].Length - 1);

            return (x, y);
        }

        ShapeBatch _sb = new(g);
    }
}
