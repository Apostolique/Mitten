using System;
using Apos.Input;
using Apos.Shapes;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class ColorPicker(GraphicsDevice g, ContentManager c) {
        public Color[][] Colors = null!;

        public Color UpdateInput() {
            (int x, int y) = MouseToColor();

            return Colors[x][y];
        }
        public void Draw(FontSystem fs, bool isBackground, Color bgColor) {
            var width = InputHelper.WindowWidth / (float)Colors.Length;
            _sb.Begin();
            _sb.FillRectangle(new Vector2(0, 0), new Vector2(InputHelper.WindowWidth, InputHelper.WindowHeight), isBackground ? bgColor : TWColor.Black);
            for (int i = 0; i < Colors.Length; i++) {
                var height = InputHelper.WindowHeight / (float)Colors[i].Length;
                for (int j = 0; j < Colors[i].Length; j++) {
                    _sb.FillRectangle(new Vector2(i * width + width * 0.08f, j * height), new Vector2(width * 0.84f, height), Colors[i][j]);
                }
            }
            (int x, int y) = MouseToColor();
            var selectHeight = InputHelper.WindowHeight / (float)Colors[x].Length;
            _sb.BorderRectangle(new Vector2(x * width, y * selectHeight), new Vector2(width, selectHeight), TWColor.Black, 6f);
            _sb.BorderRectangle(new Vector2(x * width + 2f, y * selectHeight + 2f), new Vector2(width - 4f, selectHeight - 4f), TWColor.White, 2f);
            _sb.End();

            var font = fs.GetFont(20f);
            _s.Begin();
            for (int i = 0; i < Colors.Length; i++) {
                var height = InputHelper.WindowHeight / (float)Colors[i].Length;
                for (int j = 0; j < Colors[i].Length; j++) {
                    var c = j < MathF.Ceiling(Colors[i].Length / 2f) ? TWColor.Black : TWColor.White;
                    _s.DrawString(font, $"{j}", new Vector2(i * width + width * 0.08f, j * height), c * 0.4f);
                }
            }
            _s.End();
        }

        private (int, int) MouseToColor() {
            var width = InputHelper.WindowWidth / Colors.Length;
            int x = MathHelper.Clamp(InputHelper.NewMouse.X / width, 0, Colors.Length - 1);
            var height = InputHelper.WindowHeight / Colors[x].Length;
            int y = MathHelper.Clamp(InputHelper.NewMouse.Y / height, 0, Colors[x].Length - 1);

            return (x, y);
        }

        SpriteBatch _s = new(g);
        ShapeBatch _sb = new(g, c);
    }
}
