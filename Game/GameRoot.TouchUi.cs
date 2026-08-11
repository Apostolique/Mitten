using Apos.Input;
using Track = Apos.Input.Track;
using Apos.Shapes;
using Apos.Tweens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Extended;
using System;

namespace GameProject {
    /// <summary>
    /// On screen buttons for undo, redo, the eraser, the brush size, the camera slots and the
    /// colors. Each of those is a key or a hover everywhere else, and a touch screen has
    /// neither, so the bar is the only way to reach them with a finger. It appears on the
    /// first contact and goes away again as soon as a mouse moves.
    /// </summary>
    /// <remarks>
    /// Every button does one thing on a tap. Holding one does its second thing, where it has
    /// one: the color button holds for the background, and a camera slot holds to store the
    /// view rather than travel to it.
    /// </remarks>
    public partial class GameRoot {
        enum TouchButton { Undo, Redo, Erase, Size, Camera, Color }
        /// <summary>The strip that opens one row out from the buttons.</summary>
        enum TouchTray { None, Size, Camera }
        enum TouchPick { None, Color, Background }
        enum TouchGrab { None, Button, Slider, Slot }

        static readonly TouchButton[] TouchButtons = [
            TouchButton.Undo, TouchButton.Redo, TouchButton.Erase,
            TouchButton.Size, TouchButton.Camera, TouchButton.Color
        ];

        /// <summary>Camera slots, in the order they sit in the tray. Slot 0 is the one
        /// <see cref="LoadCam"/> fills on every jump, so it goes back where you came from.</summary>
        static readonly string[] SlotKeys = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
        /// <summary>The cell past the last slot, which arms the next tap to write instead of
        /// travel. Holding a slot does the same thing without the arming step.</summary>
        const int SaveCell = 10;
        const int TrayCells = SaveCell + 1;

        /// <summary>
        /// A row of evenly spaced round cells along one screen edge. It centers itself when the
        /// whole row fits and scrolls along its axis when it doesn't, which is what a phone
        /// too narrow for eleven camera slots needs.
        /// </summary>
        private readonly record struct Strip(
            bool Vertical, Vector2 First, Vector2 Step, float Radius, float Slack, float Span) {
            public Vector2 Center(int i) => First + Step * i;
            /// <summary>Unit vector pointing at the screen edge the row hugs.</summary>
            public Vector2 Out => Vertical ? new Vector2(-1f, 0f) : new Vector2(0f, 1f);
            /// <summary>Unit vector along the row, from the first cell to the last.</summary>
            public Vector2 Along => Vertical ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
            /// <summary>How far the row can slide, as a negative number. Zero when it fits.</summary>
            public bool Scrolls => Slack < 0f;
        }

        /// <summary>
        /// Sizes a row to the screen. The pitch is picked so the whole row fits when it can,
        /// and floored at something a fingertip can hit when it can't, which is what puts the
        /// row over the edge and into scrolling.
        /// </summary>
        private static Strip LayoutStrip(Vector2 view, bool vertical, int count, float scroll,
                                         float minPitch, float maxPitch, float maxRadius, float across) {
            float span = vertical ? view.Y : view.X;
            float avail = span - 2f * EdgeGap;
            float pitch = MathF.Max(minPitch,
                MathF.Min(maxPitch, (avail - 2f * (maxRadius + PanelPad)) / (count - 1)));
            float radius = MathF.Min(maxRadius, pitch * 0.44f);
            float slack = MathF.Min(0f, avail - (pitch * (count - 1) + 2f * (radius + PanelPad)));
            float start = slack < 0f
                ? EdgeGap + PanelPad + radius + MathHelper.Clamp(scroll, slack, 0f)
                : (span - pitch * (count - 1)) / 2f;

            Vector2 step = vertical ? new Vector2(0f, pitch) : new Vector2(pitch, 0f);
            Vector2 first = vertical
                ? new Vector2(across, start)
                : new Vector2(start, view.Y - across);
            return new Strip(vertical, first, step, radius, slack, span);
        }

        /// <summary>The button row. Recomputed every frame, since a phone gets turned around
        /// mid drawing: it runs along the bottom in portrait and up the left edge in
        /// landscape, which is the short way across either way.</summary>
        private Strip BarStrip(Vector2 view) {
            bool vertical = view.X > view.Y;
            // The across position is a distance from the edge the row hugs, which is the left
            // one when the bar is vertical and the bottom one when it is not.
            return LayoutStrip(view, vertical, TouchButtons.Length, _barScroll,
                MinPitch, MaxPitch, MaxButtonRadius, EdgeGap + MaxButtonRadius);
        }

        /// <summary>The camera slots, one row further in from the buttons.</summary>
        private Strip SlotStrip(Vector2 view, Strip bar) {
            return LayoutStrip(view, bar.Vertical, TrayCells, _trayScroll,
                MinCellPitch, MaxCellPitch, MaxSlotRadius,
                EdgeGap + MaxButtonRadius + bar.Radius + TrayGap);
        }

        /// <summary>The rounded panel behind a row. It hugs the row when the row fits and runs
        /// the whole edge when the row is scrolling inside it.</summary>
        private static (Vector2 A, Vector2 B) StripRect(Strip s, int count) {
            float pad = s.Radius + PanelPad;
            Vector2 a = s.First - new Vector2(pad);
            Vector2 b = s.Center(count - 1) + new Vector2(pad);
            if (!s.Scrolls) return (a, b);
            if (s.Vertical) {
                a.Y = EdgeGap;
                b.Y = s.Span - EdgeGap;
            } else {
                a.X = EdgeGap;
                b.X = s.Span - EdgeGap;
            }
            return (a, b);
        }

        /// <summary>Which cell a contact landed on, or -1. A row is hit as one band so the gaps
        /// between the cells aren't dead, and never past the panel that clips it.</summary>
        private static int HitStrip(Strip s, Vector2 p, int count) {
            (Vector2 ra, Vector2 rb) = StripRect(s, count);
            if (p.X < ra.X || p.X > rb.X || p.Y < ra.Y || p.Y > rb.Y) return -1;

            Vector2 d = p - s.First;
            float along = Vector2.Dot(d, s.Along);
            float across = MathF.Abs(Vector2.Dot(d, s.Out));
            float pitch = s.Step.Length();
            if (across > s.Radius + Slop) return -1;
            int i = (int)MathF.Round(along / pitch);
            if (i < 0 || i >= count) return -1;
            if (MathF.Abs(along - i * pitch) > pitch / 2f) return -1;
            return i;
        }

        /// <summary>
        /// The size slider's track, along the tray row and inset by enough for the knob to sit
        /// inside the panel at either extreme. It runs left to right along the bottom and
        /// bottom to top up the side, so a bigger brush is always the way that reads as more.
        /// </summary>
        private (Vector2 A, Vector2 B) TouchTrack(Vector2 view, Strip bar) {
            float out_ = EdgeGap + MaxButtonRadius + bar.Radius + TrayGap;
            (Vector2 ra, Vector2 rb) = StripRect(bar, TouchButtons.Length);
            float inset = KnobRadius + PanelPad;
            Vector2 a, b;
            if (bar.Vertical) {
                a = new Vector2(out_, ra.Y + inset);
                b = new Vector2(out_, rb.Y - inset);
                return (b, a);
            }
            a = new Vector2(ra.X + inset, view.Y - out_);
            b = new Vector2(rb.X - inset, view.Y - out_);
            return (a, b);
        }

        /// <summary>The size slider's panel, which follows the button panel's ends.</summary>
        private (Vector2 A, Vector2 B) TrackRect(Vector2 view, Strip bar) {
            (Vector2 a, Vector2 b) = TouchTrack(view, bar);
            float half = KnobRadius + PanelPad;
            Vector2 across = bar.Vertical ? new Vector2(half, 0f) : new Vector2(0f, half);
            Vector2 inset = bar.Along * (KnobRadius + PanelPad);
            (a, b) = (Vector2.Min(a, b) - inset, Vector2.Max(a, b) + inset);
            return (a - across, b + across);
        }

        /// <summary>How much room the UI takes off the edge it sits on, as (left, bottom). The
        /// color picker and the zoom readout lay themselves out inside what's left.</summary>
        private Vector2 TouchUiPad(Vector2 view) {
            if (!_touchUi) return Vector2.Zero;
            Strip bar = BarStrip(view);
            float thickness = EdgeGap + MaxButtonRadius + bar.Radius + PanelPad;
            if (_touchTray == TouchTray.Size) {
                thickness = EdgeGap + MaxButtonRadius + bar.Radius + TrayGap + KnobRadius + PanelPad;
            } else if (_touchTray == TouchTray.Camera) {
                thickness = EdgeGap + MaxButtonRadius + bar.Radius + TrayGap
                    + SlotStrip(view, bar).Radius + PanelPad;
            }
            return bar.Vertical ? new Vector2(thickness, 0f) : new Vector2(0f, thickness);
        }

        private void UpdateTouchUi() {
            // Latched rather than read off Pointer.Source, which goes back to Mouse the frame
            // a finger lifts and would blink the bar out between taps.
            if (InputHelper.NewTouch.Count > 0) {
                _touchUi = true;
                _touchSeenMs = TweenHelper.TotalMS;
            } else if (_touchUi && TweenHelper.TotalMS - _touchSeenMs > MouseGraceMs && MouseTookOver()) {
                // A contact a page doesn't swallow comes back around as a mouse event a moment
                // later, so a mouse moving right after a lift is that echo rather than a hand
                // reaching for a real one.
                _touchUi = false;
                CloseTouchPanels();
            }

            if (!_touchUi) {
                _touchOwned = -1;
                return;
            }

            Vector2 view = ViewSize;
            Strip bar = BarStrip(view);

            // The contact is held by id rather than through a TouchCondition: a condition
            // claims whichever contact is free, while this one has to be the one that landed
            // on a control. Consuming it again every frame is what keeps the pinch and the
            // stroke off it for as long as it's down.
            if (_touchOwned >= 0) {
                if (InputHelper.NewTouch.FindById(_touchOwned, out TouchLocation held)
                    && held.State != TouchLocationState.Released) {
                    Track.TouchCondition.Consume(_touchOwned);
                    HoldTouchUi(view, bar, held.Position);
                    return;
                }
                ReleaseTouchUi(view, bar);
                _touchOwned = -1;
                _touchGrab = TouchGrab.None;
            }

            // A finger landing while another one is already down is the second finger of a
            // gesture rather than a button press.
            if (InputHelper.NewTouch.Count != 1) return;
            if (_isMouseDrawing || _isTabletDrawing || _selGesture != SelGesture.None) return;

            TouchLocation t = InputHelper.NewTouch[0];
            if (t.State != TouchLocationState.Pressed || !Track.TouchCondition.IsUnique(t.Id)) return;

            int hit = HitStrip(bar, t.Position, TouchButtons.Length);
            if (hit >= 0) {
                Claim(t.Id, TouchGrab.Button, t.Position);
                _touchIndex = hit;
                // Undo and redo answer on the way down and keep going while held, since
                // undoing twenty strokes shouldn't be twenty taps. The rest answer on release,
                // which is what leaves room for a hold and for a drag that scrolls.
                TouchButton b = TouchButtons[hit];
                if (b == TouchButton.Undo || b == TouchButton.Redo) {
                    _touchFired = true;
                    _touchRepeatAt = TweenHelper.TotalMS + RepeatDelayMs;
                    ActivateTouchButton(b);
                }
                return;
            }

            if (_touchTray == TouchTray.Size && InRect(TrackRect(view, bar), t.Position)) {
                Claim(t.Id, TouchGrab.Slider, t.Position);
                SetRadiusFromSlider(view, bar, t.Position);
                return;
            }
            if (_touchTray == TouchTray.Camera) {
                Strip slots = SlotStrip(view, bar);
                int cell = HitStrip(slots, t.Position, TrayCells);
                if (cell >= 0) {
                    Claim(t.Id, TouchGrab.Slot, t.Position);
                    _touchIndex = cell;
                    return;
                }
            }
            if (HitTouchPanel(view, bar, t.Position)) {
                // Everything else the panels cover is swallowed too, since a stroke drawn
                // under one is ink the player can't see.
                Claim(t.Id, TouchGrab.None, t.Position);
            }

            void Claim(int id, TouchGrab grab, Vector2 at) {
                _touchOwned = id;
                _touchGrab = grab;
                _touchDownAt = at;
                _touchLastAt = at;
                _touchDownMs = TweenHelper.TotalMS;
                _touchDragging = false;
                _touchFired = false;
                _touchScrollBase = grab == TouchGrab.Slot ? _trayScroll : _barScroll;
                Track.TouchCondition.Consume(id);
            }
        }

        private void HoldTouchUi(Vector2 view, Strip bar, Vector2 p) {
            _touchLastAt = p;
            if (_touchGrab == TouchGrab.Slider) {
                SetRadiusFromSlider(view, bar, p);
                return;
            }
            if (_touchGrab == TouchGrab.None) return;

            bool slot = _touchGrab == TouchGrab.Slot;
            Strip s = slot ? SlotStrip(view, bar) : bar;
            int count = slot ? TrayCells : TouchButtons.Length;

            // A row that overflows scrolls under the finger, and the drag is what tells a
            // scroll apart from a press.
            if (s.Scrolls) {
                float moved = Vector2.Dot(p - _touchDownAt, s.Along);
                if (_touchDragging || MathF.Abs(moved) > DragSlop) {
                    _touchDragging = true;
                    float at = MathHelper.Clamp(_touchScrollBase + moved, s.Slack, 0f);
                    if (slot) {
                        _trayScroll = at;
                    } else {
                        _barScroll = at;
                    }
                    return;
                }
            }
            if (_touchDragging || HitStrip(s, p, count) != _touchIndex) return;

            if (_touchFired) {
                // Undo and redo, which are the only ones that repeat.
                if (TweenHelper.TotalMS < _touchRepeatAt) return;
                _touchRepeatAt = TweenHelper.TotalMS + RepeatRateMs;
                ActivateTouchButton(TouchButtons[_touchIndex]);
                return;
            }
            if (TweenHelper.TotalMS - _touchDownMs < LongPressMs) return;
            if (slot) {
                // Holding a slot stores the view in it, the same thing the save cell arms.
                if (_touchIndex == 0 || _touchIndex == SaveCell) return;
                _touchFired = true;
                _touchSaveArmed = false;
                SaveCam(SlotKeys[_touchIndex]);
            } else if (TouchButtons[_touchIndex] == TouchButton.Color) {
                _touchFired = true;
                OpenTouchPicker(TouchPick.Background);
            }
        }

        /// <summary>Lifting off the cell the contact landed on is what presses it. Sliding away
        /// first cancels, which is the way out of a button you didn't mean to touch.</summary>
        private void ReleaseTouchUi(Vector2 view, Strip bar) {
            if (_touchDragging || _touchFired) return;
            if (_touchGrab == TouchGrab.Button) {
                if (HitStrip(bar, _touchLastAt, TouchButtons.Length) == _touchIndex) {
                    ActivateTouchButton(TouchButtons[_touchIndex]);
                }
            } else if (_touchGrab == TouchGrab.Slot) {
                Strip slots = SlotStrip(view, bar);
                if (HitStrip(slots, _touchLastAt, TrayCells) == _touchIndex) {
                    ActivateTouchSlot(_touchIndex);
                }
            }
        }

        private void ActivateTouchButton(TouchButton b) {
            switch (b) {
                case TouchButton.Undo:
                    Undo();
                    break;
                case TouchButton.Redo:
                    Redo();
                    break;
                case TouchButton.Erase:
                    SetTool(Tool.Erase);
                    break;
                case TouchButton.Size:
                    ToggleTray(TouchTray.Size);
                    break;
                case TouchButton.Camera:
                    ToggleTray(TouchTray.Camera);
                    break;
                case TouchButton.Color:
                    OpenTouchPicker(TouchPick.Color);
                    break;
            }
        }

        /// <summary>
        /// A slot travels to what it holds. Arming the save cell first turns the next slot into
        /// a write, once, so nothing is overwritten by a tap meant to travel.
        /// </summary>
        private void ActivateTouchSlot(int cell) {
            if (cell == SaveCell) {
                _touchSaveArmed = !_touchSaveArmed;
                return;
            }
            if (_touchSaveArmed) {
                // Slot 0 is written on every jump, so there is nothing to put there by hand.
                if (cell == 0) return;
                _touchSaveArmed = false;
                SaveCam(SlotKeys[cell]);
                return;
            }
            LoadCam(SlotKeys[cell]);
        }

        private void OpenTouchPicker(TouchPick want) {
            bool same = _touchPick == want;
            CloseTouchPanels();
            if (same) return;
            _touchPick = want;
            // Nothing has been touched yet, so the highlight starts in the middle rather than
            // parked on the first swatch.
            Vector2 view = ViewSize;
            Vector2 pad = TouchUiPad(view);
            _touchPickPoint = new Vector2((view.X + pad.X) / 2f, (view.Y - pad.Y) / 2f);
        }

        private void ToggleTray(TouchTray t) {
            bool same = _touchTray == t;
            CloseTouchPanels();
            if (same) return;
            _touchTray = t;
            _trayScroll = 0f;
        }

        private void CloseTouchPanels() {
            _touchPick = TouchPick.None;
            _touchPickHeld = false;
            _touchTray = TouchTray.None;
            _touchSaveArmed = false;
        }

        private static bool InRect((Vector2 A, Vector2 B) r, Vector2 p) {
            return p.X >= r.A.X && p.X <= r.B.X && p.Y >= r.A.Y && p.Y <= r.B.Y;
        }

        /// <summary>Everything the UI covers, both panels and the gap between them.</summary>
        private bool HitTouchPanel(Vector2 view, Strip bar, Vector2 p) {
            (Vector2 a, Vector2 b) = StripRect(bar, TouchButtons.Length);
            if (_touchTray == TouchTray.Size) {
                (Vector2 ta, Vector2 tb) = TrackRect(view, bar);
                (a, b) = (Vector2.Min(a, ta), Vector2.Max(b, tb));
            } else if (_touchTray == TouchTray.Camera) {
                (Vector2 ta, Vector2 tb) = StripRect(SlotStrip(view, bar), TrayCells);
                (a, b) = (Vector2.Min(a, ta), Vector2.Max(b, tb));
            }
            return InRect((a, b), p);
        }

        /// <summary>
        /// The track runs the brush size on a log scale, so a drag of the same length is the
        /// same relative change wherever it starts. Same bounds as the Control + Shift drag.
        /// </summary>
        private void SetRadiusFromSlider(Vector2 view, Strip bar, Vector2 p) {
            (Vector2 a, Vector2 b) = TouchTrack(view, bar);
            Vector2 dir = b - a;
            float length = dir.Length();
            if (length <= 0f) return;
            float t = MathHelper.Clamp(Vector2.Dot(p - a, dir / length) / length, 0f, 1f);
            _radius = BrushMin * MathF.Pow(BrushMax / BrushMin, t);
        }

        private static float SliderValue(float radius) {
            return MathHelper.Clamp(
                MathF.Log(radius / BrushMin) / MathF.Log(BrushMax / BrushMin), 0f, 1f);
        }

        /// <summary>
        /// The color picker driven by a finger. It commits on lift instead of on the release
        /// of a held key, and shows the color under the contact away from it, since a finger
        /// covers the swatch it's picking.
        /// </summary>
        private void UpdateTouchPicker() {
            Vector2 view = ViewSize;
            Vector2 pad = TouchUiPad(view);
            Vector2 xy = new(pad.X, 0f);
            Vector2 size = view - new Vector2(pad.X, pad.Y);

            if (_touchOwned < 0 && _touchPickTap.Held()
                && InputHelper.NewTouch.FindById(_touchPickTap.Owned[0], out TouchLocation t)) {
                _touchPickPoint = t.Position;
                _touchPickHeld = true;
                Color c = _cp.ColorAt(xy, size, _touchPickPoint);
                if (_touchPick == TouchPick.Background) {
                    _bgColor = c;
                } else {
                    _color = c;
                }
            } else if (_touchPickHeld) {
                // Lifting keeps whatever the last frame previewed.
                _touchPickHeld = false;
                _touchPick = TouchPick.None;
            }
        }

        private static bool MouseTookOver() {
            MouseState now = InputHelper.NewMouse;
            return now.Position != InputHelper.OldMouse.Position
                || now.ScrollWheelValue != InputHelper.OldMouse.ScrollWheelValue
                || now.LeftButton == ButtonState.Pressed
                || now.MiddleButton == ButtonState.Pressed
                || now.RightButton == ButtonState.Pressed;
        }

        private void DrawTouchPicker(Vector2 view) {
            Vector2 pad = TouchUiPad(view);
            Vector2 xy = new(pad.X, 0f);
            Vector2 size = view - new Vector2(pad.X, pad.Y);
            _cp.Draw(_font, _touchPick == TouchPick.Background, _bgColor, xy, size, _touchPickPoint);

            if (!_touchPickHeld) return;
            // Above the contact, where the hand isn't, and below it near the top edge where
            // there is no room above.
            float lift = _touchPickPoint.Y - PreviewLift - PreviewRadius >= 0f ? -PreviewLift : PreviewLift;
            Vector2 at = _touchPickPoint + new Vector2(0f, lift);
            Color c = _cp.ColorAt(xy, size, _touchPickPoint);
            _sb.Begin(UiMatrix);
            _sb.FillCircle(at, PreviewRadius, c);
            _sb.BorderCircle(at, PreviewRadius, TWColor.Black, 6f);
            _sb.BorderCircle(at, PreviewRadius - 2f, TWColor.White, 2f);
            _sb.End();
        }

        private void DrawTouchUi(Vector2 view) {
            if (!_touchUi) return;
            Strip bar = BarStrip(view);
            (Vector2 a, Vector2 b) = StripRect(bar, TouchButtons.Length);
            float pad = bar.Radius + PanelPad;

            _sb.Begin(UiMatrix);
            Panel(a, b, pad);
            if (_touchTray == TouchTray.Size) {
                (Vector2 ta, Vector2 tb) = TrackRect(view, bar);
                Panel(ta, tb, KnobRadius + PanelPad);
                (Vector2 ka, Vector2 kb) = TouchTrack(view, bar);
                Vector2 knob = ka + (kb - ka) * SliderValue(_radius);
                _sb.FillLine(ka, kb, TrackRadius, FaceIdle);
                _sb.FillLine(ka, knob, TrackRadius, SelectAccent);
            } else if (_touchTray == TouchTray.Camera) {
                Strip slots = SlotStrip(view, bar);
                (Vector2 ta, Vector2 tb) = StripRect(slots, TrayCells);
                float half = slots.Radius + PanelPad;
                Panel(ta, tb, half);
                // Clipped, so a scrolled row slides under the panel's ends instead of past them.
                _sb.SetClipRect(new RectangleF(ta.X, ta.Y, tb.X - ta.X, tb.Y - ta.Y), half);
                for (int i = 0; i < TrayCells; i++) {
                    _sb.FillCircle(slots.Center(i), slots.Radius, SlotFace(i));
                    // Armed, every slot that can take the view says so.
                    if (_touchSaveArmed && i > 0 && i < SaveCell) {
                        _sb.BorderCircle(slots.Center(i), slots.Radius, SelectAccent, 2f);
                    }
                }
                _sb.SetClipRect(null);
            }
            _sb.SetClipRect(new RectangleF(a.X, a.Y, b.X - a.X, b.Y - a.Y), pad);
            for (int i = 0; i < TouchButtons.Length; i++) {
                _sb.FillCircle(bar.Center(i), bar.Radius, TouchFace(TouchButtons[i], i));
            }
            _sb.SetClipRect(null);
            _sb.End();

            _sb.Begin(UiMatrix);
            _sb.SetClipRect(new RectangleF(a.X, a.Y, b.X - a.X, b.Y - a.Y), pad);
            for (int i = 0; i < TouchButtons.Length; i++) {
                DrawTouchIcon(TouchButtons[i], bar.Center(i), bar.Radius, TouchFace(TouchButtons[i], i));
            }
            _sb.SetClipRect(null);
            if (_touchTray == TouchTray.Size) {
                (Vector2 ka, Vector2 kb) = TouchTrack(view, bar);
                _sb.FillCircle(ka + (kb - ka) * SliderValue(_radius), KnobRadius, IconTint);
                // The brush at its real size, the same preview the Control + Shift drag gives.
                Vector2 padXY = TouchUiPad(view);
                Vector2 center = new((view.X + padXY.X) / 2f, (view.Y - padXY.Y) / 2f);
                Color fg = _tool == Tool.Erase ? _bgColor : _color;
                _sb.FillCircle(center, _radius, fg);
                _sb.BorderCircle(center, _radius, TWColor.White.SetAlpha(0.5f), MathF.Min(2f, _radius));
            } else if (_touchTray == TouchTray.Camera) {
                Strip slots = SlotStrip(view, bar);
                (Vector2 ta, Vector2 tb) = StripRect(slots, TrayCells);
                _sb.SetClipRect(new RectangleF(ta.X, ta.Y, tb.X - ta.X, tb.Y - ta.Y), slots.Radius + PanelPad);
                DrawSaveIcon(slots.Center(SaveCell), slots.Radius, IconTint);
                for (int i = 0; i < SlotKeys.Length; i++) {
                    Color tint = _savedCams.ContainsKey(SlotKeys[i]) ? IconTint : IconOff;
                    float size = slots.Radius * 1.1f;
                    Vector2 box = _font.MeasureString(SlotKeys[i], size);
                    _sb.DrawString(_font, SlotKeys[i], slots.Center(i) - box / 2f, size, tint);
                }
                _sb.SetClipRect(null);
            }
            _sb.End();

            void Panel(Vector2 pa, Vector2 pb, float rounding) {
                _sb.FillRectangle(pa, pb - pa, PanelFill, rounding);
                _sb.BorderRectangle(pa, pb - pa, PanelEdge, 1f, rounding);
            }
        }

        private Color TouchFace(TouchButton b, int i) {
            if (_touchGrab == TouchGrab.Button && _touchOwned >= 0 && _touchIndex == i && !_touchDragging) {
                return FaceDown;
            }
            return b switch {
                TouchButton.Erase => _tool == Tool.Erase ? FaceOn : FaceIdle,
                TouchButton.Size => _touchTray == TouchTray.Size ? FaceOn : FaceIdle,
                TouchButton.Camera => _touchTray == TouchTray.Camera ? FaceOn : FaceIdle,
                TouchButton.Color => _touchPick != TouchPick.None ? FaceOn : FaceIdle,
                _ => FaceIdle
            };
        }

        private Color SlotFace(int i) {
            if (_touchGrab == TouchGrab.Slot && _touchOwned >= 0 && _touchIndex == i && !_touchDragging) {
                return FaceDown;
            }
            if (i == SaveCell) return _touchSaveArmed ? FaceOn : FaceIdle;
            return _savedCams.ContainsKey(SlotKeys[i]) ? FaceOn : FaceIdle;
        }

        private void DrawTouchIcon(TouchButton b, Vector2 c, float r, Color face) {
            switch (b) {
                case TouchButton.Undo:
                    DrawHistoryIcon(c, r, false, _undoOps.Count > 0 ? IconTint : IconOff);
                    break;
                case TouchButton.Redo:
                    DrawHistoryIcon(c, r, true, _redoOps.Count > 0 ? IconTint : IconOff);
                    break;
                case TouchButton.Erase:
                    DrawEraserIcon(c, r, IconTint, face);
                    break;
                case TouchButton.Size:
                    // Three dots growing, which is the size the brush is set with.
                    _sb.FillCircle(c + new Vector2(-r * 0.59f, 0f), r * 0.09f, IconTint);
                    _sb.FillCircle(c + new Vector2(-r * 0.18f, 0f), r * 0.18f, IconTint);
                    _sb.FillCircle(c + new Vector2(r * 0.39f, 0f), r * 0.28f, IconTint);
                    break;
                case TouchButton.Camera:
                    DrawFramingIcon(c, r, IconTint);
                    break;
                case TouchButton.Color:
                    DrawColorIcon(c, r);
                    break;
            }
        }

        /// <summary>The ink over the paper, the pair of swatches a paint program puts in its
        /// corner. The one behind is the background, which is what a hold picks.</summary>
        private void DrawColorIcon(Vector2 c, float r) {
            float h = r * 0.38f;
            Vector2 paper = c + new Vector2(r * 0.26f, r * 0.26f);
            _sb.FillRectangle(paper - new Vector2(h), new Vector2(h * 2f), _bgColor, r * 0.14f);
            _sb.BorderRectangle(paper - new Vector2(h), new Vector2(h * 2f), IconTint, 2f, r * 0.14f);

            Vector2 ink = c - new Vector2(r * 0.22f, r * 0.22f);
            _sb.FillCircle(ink, r * 0.42f, _color);
            _sb.BorderCircle(ink, r * 0.42f, IconTint, 2f);
        }

        /// <summary>An arrow over the top of a circle, coming down on the left for undo and
        /// on the right for redo.</summary>
        private void DrawHistoryIcon(Vector2 c, float r, bool forward, Color tint) {
            float side = forward ? 1f : -1f;
            float arc = r * 0.46f;
            _sb.FillArc(c, MathHelper.Pi, MathHelper.TwoPi, arc, r * 0.13f, tint);
            Vector2 end = c + new Vector2(arc * side, 0f);
            _sb.FillTriangle(
                end + new Vector2(0f, r * 0.44f),
                end + new Vector2(-r * 0.30f, -r * 0.02f),
                end + new Vector2(r * 0.30f, -r * 0.02f),
                tint, r * 0.05f);
        }

        /// <summary>An eraser block on its side, its sleeve cut out of it in the button's own
        /// color.</summary>
        private void DrawEraserIcon(Vector2 c, float r, Color tint, Color face) {
            float rot = -MathHelper.PiOver4 * 0.55f;
            Vector2 size = new(r * 1.24f, r * 0.76f);
            _sb.FillRectangle(c - size / 2f, size, tint, r * 0.16f, rot);

            Vector2 band = new(r * 0.38f, r * 0.76f);
            Vector2 at = c + Rotate(new Vector2((size.X - band.X) / 2f, 0f), rot);
            _sb.FillRectangle(at - band / 2f, band, face, new CornerRadii(0f, r * 0.16f, r * 0.16f, 0f), rot);
        }

        /// <summary>An arrow going down into a shelf: the current view dropped into a slot.</summary>
        private void DrawSaveIcon(Vector2 c, float r, Color tint) {
            float t = r * 0.11f;
            Vector2 top = c + new Vector2(0f, -r * 0.52f);
            Vector2 tip = c + new Vector2(0f, r * 0.12f);
            _sb.FillLine(top, tip, t, tint);
            _sb.FillTriangle(
                tip + new Vector2(0f, r * 0.2f),
                tip + new Vector2(-r * 0.28f, -r * 0.08f),
                tip + new Vector2(r * 0.28f, -r * 0.08f),
                tint, r * 0.05f);
            _sb.FillLine(c + new Vector2(-r * 0.42f, r * 0.52f), c + new Vector2(r * 0.42f, r * 0.52f), t, tint);
        }

        /// <summary>Four corner brackets around a dot: the frame a camera slot puts back.</summary>
        private void DrawFramingIcon(Vector2 c, float r, Color tint) {
            float h = r * 0.5f;
            float arm = r * 0.26f;
            float t = r * 0.1f;
            for (int sx = -1; sx <= 1; sx += 2) {
                for (int sy = -1; sy <= 1; sy += 2) {
                    Vector2 corner = c + new Vector2(h * sx, h * sy);
                    _sb.FillLine(corner, corner - new Vector2(arm * sx, 0f), t, tint);
                    _sb.FillLine(corner, corner - new Vector2(0f, arm * sy), t, tint);
                }
            }
            _sb.FillCircle(c, r * 0.13f, tint);
        }

        private static Vector2 Rotate(Vector2 v, float angle) {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        bool _touchUi = false;
        /// <summary>Contact the bar is holding, or -1.</summary>
        int _touchOwned = -1;
        TouchGrab _touchGrab = TouchGrab.None;
        int _touchIndex = 0;
        Vector2 _touchDownAt = Vector2.Zero;
        Vector2 _touchLastAt = Vector2.Zero;
        double _touchDownMs = 0.0;
        double _touchRepeatAt = 0.0;
        double _touchSeenMs = 0.0;
        /// <summary>Whether the contact has already done its work, so the release doesn't
        /// repeat it.</summary>
        bool _touchFired = false;
        bool _touchDragging = false;
        float _touchScrollBase = 0f;

        float _barScroll = 0f;
        float _trayScroll = 0f;

        TouchTray _touchTray = TouchTray.None;
        bool _touchSaveArmed = false;

        TouchPick _touchPick = TouchPick.None;
        bool _touchPickHeld = false;
        Vector2 _touchPickPoint = Vector2.Zero;
        /// <summary>Any finger that isn't the bar's, while the picker is open.</summary>
        readonly Track.TouchCondition _touchPickTap = new(1);

        // Enough that the rail clears the zoom sidebar, which is 10 px wide up the left edge.
        const float EdgeGap = 18f;
        const float MinPitch = 44f;
        const float MaxPitch = 64f;
        const float MaxButtonRadius = 24f;
        const float MinCellPitch = 44f;
        const float MaxCellPitch = 56f;
        const float MaxSlotRadius = 20f;
        const float PanelPad = 8f;
        const float Slop = 8f;
        // Far enough out that the tray's panel clears the bar's rather than overlapping it,
        // which would show as a darker seam where the two translucent fills stack.
        const float TrayGap = 42f;
        const float TrackRadius = 5f;
        const float KnobRadius = 13f;
        const float DragSlop = 10f;
        const float PreviewRadius = 34f;
        const float PreviewLift = 72f;
        const float BrushMin = 0.5f;
        const float BrushMax = 1000f;
        const double RepeatDelayMs = 400.0;
        const double RepeatRateMs = 90.0;
        const double LongPressMs = 500.0;
        const double MouseGraceMs = 400.0;

        static readonly Color PanelFill = TWColor.Gray900.SetAlpha(0.82f);
        static readonly Color PanelEdge = TWColor.Gray600.SetAlpha(0.5f);
        static readonly Color FaceIdle = TWColor.Gray700;
        static readonly Color FaceDown = TWColor.Gray500;
        static readonly Color FaceOn = TWColor.Sky700;
        static readonly Color IconTint = TWColor.Gray100;
        static readonly Color IconOff = TWColor.Gray500;
    }
}
