using System;
using Apos.Input;
using Apos.Shapes;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class ColorPicker {
        public ColorPicker(GraphicsDevice g, ContentManager c) {
            _s = new SpriteBatch(g);
            _sb = new ShapeBatch(g, c);
            _colors = new Color[][] {
                new Color[] { TWColor.White, TWColor.Black },
                new Color[] { TWColor.Slate050, TWColor.Slate100, TWColor.Slate200, TWColor.Slate300, TWColor.Slate400, TWColor.Slate500, TWColor.Slate600, TWColor.Slate700, TWColor.Slate800, TWColor.Slate900, TWColor.Slate950 },
                new Color[] { TWColor.Gray050, TWColor.Gray100, TWColor.Gray200, TWColor.Gray300, TWColor.Gray400, TWColor.Gray500, TWColor.Gray600, TWColor.Gray700, TWColor.Gray800, TWColor.Gray900, TWColor.Gray950 },
                new Color[] { TWColor.Zinc050, TWColor.Zinc100, TWColor.Zinc200, TWColor.Zinc300, TWColor.Zinc400, TWColor.Zinc500, TWColor.Zinc600, TWColor.Zinc700, TWColor.Zinc800, TWColor.Zinc900, TWColor.Zinc950 },
                new Color[] { TWColor.Neutral050, TWColor.Neutral100, TWColor.Neutral200, TWColor.Neutral300, TWColor.Neutral400, TWColor.Neutral500, TWColor.Neutral600, TWColor.Neutral700, TWColor.Neutral800, TWColor.Neutral900, TWColor.Neutral950 },
                new Color[] { TWColor.Stone050, TWColor.Stone100, TWColor.Stone200, TWColor.Stone300, TWColor.Stone400, TWColor.Stone500, TWColor.Stone600, TWColor.Stone700, TWColor.Stone800, TWColor.Stone900, TWColor.Stone950 },
                new Color[] { TWColor.Red050, TWColor.Red100, TWColor.Red200, TWColor.Red300, TWColor.Red400, TWColor.Red500, TWColor.Red600, TWColor.Red700, TWColor.Red800, TWColor.Red900, TWColor.Red950 },
                new Color[] { TWColor.Orange050, TWColor.Orange100, TWColor.Orange200, TWColor.Orange300, TWColor.Orange400, TWColor.Orange500, TWColor.Orange600, TWColor.Orange700, TWColor.Orange800, TWColor.Orange900, TWColor.Orange950 },
                new Color[] { TWColor.Amber050, TWColor.Amber100, TWColor.Amber200, TWColor.Amber300, TWColor.Amber400, TWColor.Amber500, TWColor.Amber600, TWColor.Amber700, TWColor.Amber800, TWColor.Amber900, TWColor.Amber950 },
                new Color[] { TWColor.Yellow050, TWColor.Yellow100, TWColor.Yellow200, TWColor.Yellow300, TWColor.Yellow400, TWColor.Yellow500, TWColor.Yellow600, TWColor.Yellow700, TWColor.Yellow800, TWColor.Yellow900, TWColor.Yellow950 },
                new Color[] { TWColor.Lime050, TWColor.Lime100, TWColor.Lime200, TWColor.Lime300, TWColor.Lime400, TWColor.Lime500, TWColor.Lime600, TWColor.Lime700, TWColor.Lime800, TWColor.Lime900, TWColor.Lime950 },
                new Color[] { TWColor.Green050, TWColor.Green100, TWColor.Green200, TWColor.Green300, TWColor.Green400, TWColor.Green500, TWColor.Green600, TWColor.Green700, TWColor.Green800, TWColor.Green900, TWColor.Green950 },
                new Color[] { TWColor.Emerald050, TWColor.Emerald100, TWColor.Emerald200, TWColor.Emerald300, TWColor.Emerald400, TWColor.Emerald500, TWColor.Emerald600, TWColor.Emerald700, TWColor.Emerald800, TWColor.Emerald900, TWColor.Emerald950 },
                new Color[] { TWColor.Teal050, TWColor.Teal100, TWColor.Teal200, TWColor.Teal300, TWColor.Teal400, TWColor.Teal500, TWColor.Teal600, TWColor.Teal700, TWColor.Teal800, TWColor.Teal900, TWColor.Teal950 },
                new Color[] { TWColor.Cyan050, TWColor.Cyan100, TWColor.Cyan200, TWColor.Cyan300, TWColor.Cyan400, TWColor.Cyan500, TWColor.Cyan600, TWColor.Cyan700, TWColor.Cyan800, TWColor.Cyan900, TWColor.Cyan950 },
                new Color[] { TWColor.Sky050, TWColor.Sky100, TWColor.Sky200, TWColor.Sky300, TWColor.Sky400, TWColor.Sky500, TWColor.Sky600, TWColor.Sky700, TWColor.Sky800, TWColor.Sky900, TWColor.Sky950 },
                new Color[] { TWColor.Blue050, TWColor.Blue100, TWColor.Blue200, TWColor.Blue300, TWColor.Blue400, TWColor.Blue500, TWColor.Blue600, TWColor.Blue700, TWColor.Blue800, TWColor.Blue900, TWColor.Blue950 },
                new Color[] { TWColor.Indigo050, TWColor.Indigo100, TWColor.Indigo200, TWColor.Indigo300, TWColor.Indigo400, TWColor.Indigo500, TWColor.Indigo600, TWColor.Indigo700, TWColor.Indigo800, TWColor.Indigo900, TWColor.Indigo950 },
                new Color[] { TWColor.Violet050, TWColor.Violet100, TWColor.Violet200, TWColor.Violet300, TWColor.Violet400, TWColor.Violet500, TWColor.Violet600, TWColor.Violet700, TWColor.Violet800, TWColor.Violet900, TWColor.Violet950 },
                new Color[] { TWColor.Purple050, TWColor.Purple100, TWColor.Purple200, TWColor.Purple300, TWColor.Purple400, TWColor.Purple500, TWColor.Purple600, TWColor.Purple700, TWColor.Purple800, TWColor.Purple900, TWColor.Purple950 },
                new Color[] { TWColor.Fuchsia050, TWColor.Fuchsia100, TWColor.Fuchsia200, TWColor.Fuchsia300, TWColor.Fuchsia400, TWColor.Fuchsia500, TWColor.Fuchsia600, TWColor.Fuchsia700, TWColor.Fuchsia800, TWColor.Fuchsia900, TWColor.Fuchsia950 },
                new Color[] { TWColor.Pink050, TWColor.Pink100, TWColor.Pink200, TWColor.Pink300, TWColor.Pink400, TWColor.Pink500, TWColor.Pink600, TWColor.Pink700, TWColor.Pink800, TWColor.Pink900, TWColor.Pink950 },
                new Color[] { TWColor.Rose050, TWColor.Rose100, TWColor.Rose200, TWColor.Rose300, TWColor.Rose400, TWColor.Rose500, TWColor.Rose600, TWColor.Rose700, TWColor.Rose800, TWColor.Rose900, TWColor.Rose950 },
            };
        }

        public Color UpdateInput() {
            (int x, int y) = MouseToColor();

            return _colors[x][y];
        }
        public void Draw(FontSystem fs, bool isBackground, Color bgColor) {
            var width = InputHelper.WindowWidth / (float)_colors.Length;
            _sb.Begin();
            _sb.FillRectangle(new Vector2(0, 0), new Vector2(InputHelper.WindowWidth, InputHelper.WindowHeight), isBackground ? bgColor : TWColor.Black);
            for (int i = 0; i < _colors.Length; i++) {
                var height = InputHelper.WindowHeight / (float)_colors[i].Length;
                for (int j = 0; j < _colors[i].Length; j++) {
                    _sb.FillRectangle(new Vector2(i * width + width * 0.08f, j * height), new Vector2(width * 0.84f, height), _colors[i][j]);
                }
            }
            (int x, int y) = MouseToColor();
            var selectHeight = InputHelper.WindowHeight / (float)_colors[x].Length;
            _sb.BorderRectangle(new Vector2(x * width, y * selectHeight), new Vector2(width, selectHeight), TWColor.Black, 6f);
            _sb.BorderRectangle(new Vector2(x * width + 2f, y * selectHeight + 2f), new Vector2(width - 4f, selectHeight - 4f), TWColor.White, 2f);
            _sb.End();

            var font = fs.GetFont(20f);
            _s.Begin();
            for (int i = 0; i < _colors.Length; i++) {
                var height = InputHelper.WindowHeight / (float)_colors[i].Length;
                for (int j = 0; j < _colors[i].Length; j++) {
                    var c = j < MathF.Ceiling(_colors[i].Length / 2f) ? TWColor.Black : TWColor.White;
                    _s.DrawString(font, $"{j}", new Vector2(i * width + width * 0.08f, j * height), c * 0.4f);
                }
            }
            _s.End();
        }

        private (int, int) MouseToColor() {
            var width = InputHelper.WindowWidth / _colors.Length;
            int x = MathHelper.Clamp(InputHelper.NewMouse.X / width, 0, _colors.Length - 1);
            var height = InputHelper.WindowHeight / _colors[x].Length;
            int y = MathHelper.Clamp(InputHelper.NewMouse.Y / height, 0, _colors[x].Length - 1);

            return (x, y);
        }

        SpriteBatch _s;
        ShapeBatch _sb;
        Color[][] _colors;
    }
}
