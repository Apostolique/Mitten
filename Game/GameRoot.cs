using Apos.Input;
using Track = Apos.Input.Track;
using Apos.Shapes;
using Apos.Tweens;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Collections;

#if SDLWINDOWS
using Apos.WintabDN;
#endif
using System.Text.Json.Serialization.Metadata;

// TODO:
//       Add tablet pressure sensitivity.
//       Rotation controls like Krita.

namespace GameProject {
    public class GameRoot : Game {
        public GameRoot() {
            _graphics = new GraphicsDeviceManager(this) {
                GraphicsProfile = GraphicsProfile.HiDef
            };
            Content.RootDirectory = "Content";

            _settings = EnsureJson("Settings.json", SettingsContext.Default.Settings);
            IsMouseVisible = _settings.ShowMouse;
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            IsFixedTimeStep = _settings.IsFixedTimeStep;
            _graphics.SynchronizeWithVerticalRetrace = _settings.IsVSync;

            _settings.IsFullscreen = _settings.IsFullscreen || _settings.IsBorderless;

            #if SDLWINDOWS
            SDL2.SDL.SDL_SysWMinfo systemInfo = new();
            SDL2.SDL.SDL_VERSION(out systemInfo.version);
            SDL2.SDL.SDL_GetWindowWMInfo(Window.Handle, ref systemInfo);

            try {
                Console.WriteLine($"Device {CWintabInfo.GetDeviceInfo()}");
                _logContext = CWintabInfo.GetDefaultSystemContext(ECTXOptionValues.CXO_MESSAGES);
                _logContext.Open(systemInfo.info.win.window, true);
                Console.WriteLine($"Context: {_logContext.HCtx}");
                _tabletIsValid = _logContext.HCtx != 0;
                if (_tabletIsValid) {
                    _data = new CWintabData(_logContext);
                }

                // while (true) {
                //     uint count = 0;
                //     WintabPacket[] results = _data.GetDataPackets(1, true, ref count);
                //     for (int i = 0; i < count; i++) {
                //         int x = results[i].pkX;
                //         int y = results[i].pkY;
                //         uint pressure = results[i].pkNormalPressure;

                //         Console.WriteLine($"X: {x} -- Y: {y} ::: {pressure}");
                //     }
                // }
            } catch (Exception ex) {
                Console.WriteLine($"Tablet Exception {ex}");
            }
            #endif

            RestoreWindow();
            if (_settings.IsFullscreen) {
                ApplyFullscreenChange(false);
            }

            base.Initialize();
        }

        protected override void LoadContent() {
            _s = new SpriteBatch(GraphicsDevice);
            _sb = new ShapeBatch(GraphicsDevice, Content);

            // TODO: use this.Content to load your game content here
            InputHelper.Setup(this);

            _fontSystem = new FontSystem();
            _fontSystem.AddFont(TitleContainer.OpenStream($"{Content.RootDirectory}/source-code-pro-medium.ttf"));

            _lines = [];
            _anchor = new Frame();
            _undoGroups = [];
            _redoGroups = [];
            _redoLines = [];
            _savedCams = [];

            _camera = new CameraD(GraphicsDevice);

            _cp = new ColorPicker(GraphicsDevice, Content);
            LoadPalette();

            LoadDrawing();
        }

        protected override void UnloadContent() {
            #if SDLWINDOWS
            if (_logContext is not null && _logContext.HCtx != 0) {
                _logContext.Close();
            }
            #endif

            SaveDrawing();
            // SavePalette(); // Not required unless we code a palette creation UI.

            if (!_settings.IsFullscreen) {
                SaveWindow();
            }

            SaveJson("Settings.json", _settings, SettingsContext.Default.Settings);

            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            #if SDLWINDOWS
            bool tabletProcessed = false;
            #endif

            InputHelper.UpdateSetup();
            TweenHelper.UpdateSetup(gameTime);

            if (_quit.Pressed())
                Exit();

            if (_toggleDebug.Pressed()) _showDebug = !_showDebug;
            if (_toggleMouse.Pressed()) {
                _settings.ShowMouse = !_settings.ShowMouse;
                IsMouseVisible = _settings.ShowMouse;
            }
            if (_resetFPS.Pressed()) _fps.DroppedFrames = 0;
            _fps.Update(gameTime);

            if (_toggleFullscreen.Pressed()) {
                ToggleFullscreen();
            }
            if (_toggleBorderless.Pressed()) {
                ToggleBorderless();
            }

            if (_pickColor.Held()) {
                if (_pickBackground.Held()) {
                    _bgColor = _cp.UpdateInput();
                } else {
                    _color = _cp.UpdateInput();
                }
            } else {
                UpdateCamera();

                if (!_isMouseDrawing && _thickness.Held()) {
                    if (_thickness.Pressed()) {
                        _radiusStart = _radius;
                        _thicknessStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                    }
                    var diffX = (InputHelper.NewMouse.X - _thicknessStart.X) / 2f;
                    _radius = MathHelper.Clamp(_radiusStart + diffX, 0.5f, 1000f);
                } else {
                    #if SDLWINDOWS
                    if (!_isMouseDrawing && _tabletIsValid) {
                        StrokeWithTablet(gameTime.TotalGameTime.TotalMilliseconds);
                        tabletProcessed = true;
                    }
                    #endif

                    if (!_isTabletDrawing) {
                        StrokeWithMouse();
                    }
                }
            }

            if (!_isMouseDrawing) {
                if (_toggleEraser.Pressed()) {
                    _isErasing = !_isErasing;
                }

                if (_redo.Pressed()) {
                    Redo();
                }
                if (_undo.Pressed()) {
                    Undo();
                }
                if (_redoAll.Pressed()) {
                    RedoAll();
                }
                if (_undoAll.Pressed()) {
                    UndoAll();
                }
                if (_save.Pressed()) {
                    SaveDrawing();
                }
            }

            if (_saveCam1.Pressed()) {
                SaveCam("1");
            }
            if (_loadCam1.Pressed()) {
                LoadCam("1");
            }
            if (_saveCam2.Pressed()) {
                SaveCam("2");
            }
            if (_loadCam2.Pressed()) {
                LoadCam("2");
            }
            if (_saveCam3.Pressed()) {
                SaveCam("3");
            }
            if (_loadCam3.Pressed()) {
                LoadCam("3");
            }
            if (_saveCam4.Pressed()) {
                SaveCam("4");
            }
            if (_loadCam4.Pressed()) {
                LoadCam("4");
            }
            if (_saveCam5.Pressed()) {
                SaveCam("5");
            }
            if (_loadCam5.Pressed()) {
                LoadCam("5");
            }
            if (_saveCam6.Pressed()) {
                SaveCam("6");
            }
            if (_loadCam6.Pressed()) {
                LoadCam("6");
            }
            if (_saveCam7.Pressed()) {
                SaveCam("7");
            }
            if (_loadCam7.Pressed()) {
                LoadCam("7");
            }
            if (_saveCam8.Pressed()) {
                SaveCam("8");
            }
            if (_loadCam8.Pressed()) {
                LoadCam("8");
            }
            if (_saveCam9.Pressed()) {
                SaveCam("9");
            }
            if (_loadCam9.Pressed()) {
                LoadCam("9");
            }

            if (_loadCam0.Pressed()) {
                LoadCam("0");
            }

            #if SDLWINDOWS
            if (!tabletProcessed && _tabletIsValid) {
                UpdateTablet();
            }
            #endif

            InputHelper.UpdateCleanup();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            _fps.Draw(gameTime);
            GraphicsDevice.Clear(_bgColor);

            _sb.Begin(_camera.View);

            var fgColor = _color;
            if (_isErasing) {
                fgColor = _bgColor;
            }

            _drawables.Clear();
            _screenRadius = 0.5f * MathF.Sqrt(
                GraphicsDevice.Viewport.Width * (float)GraphicsDevice.Viewport.Width +
                GraphicsDevice.Viewport.Height * (float)GraphicsDevice.Viewport.Height) + 2f;
            int fullCoverId = _coverage.Collect(_drawables, _camera.XY, _camera.Scale, _screenRadius);
            CollectVisible();
            _drawables.Sort(static (x, y) => x.Id.CompareTo(y.Id));
            int inView = 0;
            foreach (var d in _drawables) {
                if (d.Id < fullCoverId) continue;
                var c = d.Color == TWColor.Transparent ? _bgColor : d.Color;
                if (d.A == d.B) {
                    _sb.FillCircle(d.A, d.Radius, c);
                } else {
                    _sb.FillLine(d.A, d.B, d.Radius, c);
                }
                inView++;
            }
            if (_isTabletDrawing) {
                float pressure = _tabletPressure;
                if (_line.Held()) {
                    pressure = _maxPressure;
                }
                _sb.FillLine(_camera.WorldToView(_start), _camera.WorldToView(_end), _radius * pressure, fgColor);
            }
            if (_isMouseDrawing) {
                _sb.FillLine(_camera.WorldToView(_start), _camera.WorldToView(_end), _radius, fgColor);
            }
            if (_thickness.Held()) {
                var thicknessView = _camera.WorldToView(_camera.ScreenToWorld(_thicknessStart));
                _sb.FillCircle(thicknessView, _radius, fgColor);
                if (_isErasing) {
                    _sb.BorderCircle(thicknessView, _radius, TWColor.Black, 6f);
                    _sb.BorderCircle(thicknessView, _radius - 2f, TWColor.White, 2f);
                }
            } else {
                var mouseView = _camera.WorldToView(_mouseWorld);
                _sb.FillCircle(mouseView, _radius * _tabletPressure, fgColor);
                if (_isErasing) {
                    _sb.BorderCircle(mouseView, _radius * _tabletPressure, TWColor.Black, 6f);
                    _sb.BorderCircle(mouseView, (_radius - 2f) * _tabletPressure, TWColor.White, 2f);
                }
            }

            // _sb.FillCircle(_tabletXY, 100f * _tabletPressure, TWColor.White);
            _sb.End();

            _sb.Begin();
            var camExp = ScaleToExp(_camera.Scale);
            if (_zoomSidebarTween.Value > 0f) {
                var length = _minExp - _maxExp;
                var percent = (float)((camExp - _maxExp) / length);
                _sb.DrawLine(new Vector2(0, GraphicsDevice.Viewport.Height), new Vector2(0, GraphicsDevice.Viewport.Height * percent), 10f, TWColor.White.SetAlpha(_zoomSidebarTween.Value), TWColor.Black.SetAlpha(_zoomSidebarTween.Value), 2f);
            }
            _sb.End();

            if (_zoomSidebarTween.Value > 0f) {
                // Absolute zoom relative to the original top frame, as a power of ten.
                double absLog10 = (_anchor.Level * Frame.LnK - camExp) / Math.Log(10.0);
                var font = _fontSystem.GetFont(20);
                _s.Begin();
                _s.DrawString(font, $"x10^{absLog10:0.0}", new Vector2(16, GraphicsDevice.Viewport.Height - 28), TWColor.White.SetAlpha(_zoomSidebarTween.Value));
                _s.End();
            }

            if (_pickColor.Held()) {
                _cp.Draw(_fontSystem, _pickBackground.Held(), _bgColor);
            }

            if (_showDebug) {
                var font = _fontSystem.GetFont(24);
                _s.Begin();
                _s.DrawString(font, $"fps: {_fps.FramesPerSecond} - Dropped Frames: {_fps.DroppedFrames} - Draw ms: {_fps.TimePerFrame} - Update ms: {_fps.TimePerUpdate}", new Vector2(10, 10), TWColor.White);
                _s.DrawString(font, $"In view: {inView} -- Total: {_lines.Count} -- {_camera.ScreenToWorldScale()}", new Vector2(10, GraphicsDevice.Viewport.Height - 24), TWColor.White);
                _s.DrawString(font, $"Level: {_anchor.Level} -- Cell: ({_anchor.Index.X}, {_anchor.Index.Y}) -- Coverage: {_coverage.Count}", new Vector2(10, GraphicsDevice.Viewport.Height - 48), TWColor.White);
                _s.End();
            }

            base.Draw(gameTime);
        }

        #if SDLWINDOWS
        private void UpdateTablet() {
            _data.FlushDataPackets(100);
        }

        private void StrokeWithTablet(double totalTime) {
            bool ranOnce = false;

            using IEnumerator<(int, int, float)> t = new QueryTablet(_data);
            bool isValid;
            do {
                isValid = t.MoveNext();

                if (ranOnce && !isValid) {
                    break;
                }

                _tabletPressure = 0;
                Vector2D currentCursor;

                if (isValid) {
                    int x = t.Current.Item1;
                    int y = t.Current.Item2;
                    _tabletPressure = t.Current.Item3;

                    y = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - y - Window.ClientBounds.Y - 1;
                    x -= Window.ClientBounds.X;

                    currentCursor = _camera.ScreenToWorld(x, y);
                    _lastTablet = currentCursor;
                    _lastPressure = _tabletPressure;

                    if (totalTime - _maxLastTime < 300) {
                        _maxPressure = MathF.Max(_tabletPressure, _maxPressure);
                    } else {
                        _maxPressure = _tabletPressure;
                        _maxLastTime = totalTime;
                    }
                } else {
                    currentCursor = _lastTablet;
                    _tabletPressure = _lastPressure;
                }

                if (!_isTabletDrawing && _tabletPressure > 0) {
                    _start = currentCursor;
                    _isTabletDrawing = true;
                }
                if (_isTabletDrawing) {
                    _end = currentCursor;

                    if (_start != _end && !_line.Held()) {
                        CreateLine(_start, _end, _radius * _camera.ScreenToWorldScale() * _tabletPressure);
                        _start = currentCursor;
                    }
                }
                if (_isTabletDrawing && _tabletPressure == 0) {
                    _isTabletDrawing = false;
                    _end = currentCursor;

                    if (_start == _end) {
                        _end += new Vector2D(_camera.ScreenToWorldScale());
                    }

                    if (_line.Held()) {
                        _lastPressure = _maxPressure;
                    }

                    CreateLine(_start, _end, _radius * _camera.ScreenToWorldScale() * _lastPressure);
                    CreateGroup();
                }

                ranOnce = true;
            } while (true);
        }
        #endif

        private void StrokeWithMouse() {
            _tabletPressure = 1f;
            if (_draw.Pressed()) {
                _start = _mouseWorld;
                _isMouseDrawing = true;
            }
            if (_isMouseDrawing && _draw.Held()) {
                _end = _mouseWorld;

                if (_start != _end && !_line.Held()) {
                    CreateLine(_start, _end, _radius * _camera.ScreenToWorldScale());
                    _start = _mouseWorld;
                }
            }
            if (_isMouseDrawing && _draw.Released()) {
                _isMouseDrawing = false;
                _end = _mouseWorld;

                if (_start == _end) {
                    _end += new Vector2D(_camera.ScreenToWorldScale());
                }

                CreateLine(_start, _end, _radius * _camera.ScreenToWorldScale());
                CreateGroup();
            }
        }

        private void UpdateCamera() {
            if (_hyperZoom.Pressed()) {
                _preservedExp = _targetExp;
                SetExpTween(_preservedExp + _hyperZoomExp);
            }
            if (_hyperZoom.Held()) {
                SetExpTween(_preservedExp + _hyperZoomExp);

                ShowZoomSidebar();
            } else if (_hyperZoom.Released()) {
                SetExpTween(_preservedExp);
            } else {
                if (_dragZoom.Held()) {
                    if (_dragZoom.Pressed()) {
                        _expStart = _targetExp;
                        _zoomStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                        _dragAnchor = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                        _pinCamera = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                    }
                    var diffY = (InputHelper.NewMouse.Y - _zoomStart.Y) / 100.0;
                    SetExpTween(_expStart + diffY, 0);

                    ShowZoomSidebar();
                } else if (MouseCondition.Scrolled() && !_thickness.Held()) {
                    SetExpTween(_targetExp - MouseCondition.ScrollDelta * _expDistance);

                    ShowZoomSidebar();
                }
            }

            if (_rotateLeft.Pressed()) {
                SetRotationTween(_rotation.B + MathHelper.PiOver4);
            }
            if (_rotateRight.Pressed()) {
                SetRotationTween(_rotation.B - MathHelper.PiOver4);
            }

            _camera.Scale = ExpToScale(_exp.Value);
            _camera.Rotation = _rotation.Value;
            // Sync XY before reading the mouse, otherwise the cursor projects through
            // last frame's camera position and trails during XY tweens (LoadCam).
            _camera.XY = _xy.Value;

            if (_dragZoom.Held()) {
                SetXYTween(_xy.Value + _dragAnchor - _camera.ScreenToWorld(_pinCamera), 0);
                _mouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
            } else {
                _mouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

                if (_dragCamera.Pressed()) {
                    _dragAnchor = _mouseWorld;
                }
                if (_dragCamera.Held()) {
                    SetXYTween(_xy.Value + _dragAnchor - _mouseWorld, 0);
                    _mouseWorld = _dragAnchor;
                }
            }

            RebaseCamera();

            UpdateFlight();
        }
        private static double ScaleToExp(double scale) {
            return -Math.Log(scale);
        }
        private static double ExpToScale(double exp) {
            return Math.Exp(-exp);
        }
        private static double ZToScale(double z) {
            return 1.0 / z;
        }

        // Ancestors above this height hand their strokes to the coverage stack. A tree
        // query recomputes camera-relative positions every frame, so its error must
        // stay subpixel or the content jitters; coverage entries are seeded once and
        // transformed exactly, so they stay put. One level up the per-frame error is
        // ~2^(16-37) * scale px (always subpixel in band); two levels up it is not.
        private const int MaxAncestorQuery = 1;
        // Sibling branches have no coverage entries, so their frames' trees are still
        // queried up to this height. At height 2 the error is bounded by the strokes'
        // own coordinate precision (~2^-36 of a cell), the best any path can do.
        private const int MaxQueryHeight = 2;
        // How many ancestor levels the collection walk visits. Strokes reach at most
        // one parent cell beyond their frame's cell (NormalizeAnchor), so content from
        // sibling branches up to 3 levels above can still overhang into the view.
        private const int MaxWalkHeight = 3;
        // Above this radius in pixels a stroke renders as a screen-local edge or full
        // cover: float vertices cannot place the edge of a larger capsule precisely.
        private const double BigRadiusPx = 1e6;

        private void CollectVisible() {
            // The camera is carried as exact integer cell offsets plus fractions in
            // [0, 1)². Descending is then exact and ascending only rounds at 2^-53,
            // so camera-relative positions never inherit the magnitude of ancestor
            // coordinates. (Walking a plain double through u ancestor levels and back
            // used to cost up to 2^(16u-37) * scale px: tens of pixels of drift and
            // pan jitter at high zoom, catastrophic at u = 3.)
            long ix = (long)Math.Floor(_camera.XY.X);
            long iy = (long)Math.Floor(_camera.XY.Y);
            double fx = _camera.XY.X - ix;
            double fy = _camera.XY.Y - iy;
            double ppu = _camera.Scale;

            Frame f = _anchor;
            Frame? skip = null;
            for (int height = 0; ; height++) {
                CollectFrame(f, ix, iy, fx, fy, ppu, height, skip, height <= MaxAncestorQuery);
                if (height >= MaxWalkHeight || f.Parent == null) break;

                (long qx, long rx) = FloorDivMod(ix, Frame.CellCount);
                (long qy, long ry) = FloorDivMod(iy, Frame.CellCount);
                fx = (rx + fx) / Frame.K;
                fy = (ry + fy) / Frame.K;
                ix = qx + f.Index.X;
                iy = qy + f.Index.Y;
                ppu *= Frame.K;
                skip = f;
                f = f.Parent;
            }
        }

        private static (long Q, long R) FloorDivMod(long v, long k) {
            long q = Math.DivRem(v, k, out long r);
            if (r < 0) { q--; r += k; }
            return (q, r);
        }

        private void CollectFrame(Frame f, long ix, long iy, double fx, double fy, double ppu, int height, Frame? skip, bool ownTree) {
            // Collapsing the split camera is safe for queries: visited frames keep the
            // camera within a few cells of their origin, and the rects have margins.
            Vector2D cam = new(ix + fx, iy + fy);
            RectangleD view = _camera.ViewRectIn(cam, ppu);
            // Strokes anchored here are at most one cell (K units) across: below half
            // a pixel none of them can be visible.
            if (ownTree && height <= MaxQueryHeight && f.Tree.Count > 0 && ppu * Frame.K >= 0.5) {
                foreach (Line l in f.Tree.Query(view)) {
                    EmitLine(l, ix, iy, fx, fy, ppu);
                }
            }

            if (f.Children.Count == 0) return;

            // A child cell is 1 unit wide, so it projects to ppu pixels: below half a
            // pixel nothing inside can be individually visible and the whole subtree
            // renders as one impostor dot instead. This also terminates the recursion:
            // ppu divides by K per level.
            bool recurse = ppu >= 0.5;

            // Strokes overhang their frame's cell by at most one cell width.
            RectangleD near = new(view.X - 1.0, view.Y - 1.0, view.Width + 2.0, view.Height + 2.0);
            foreach (Frame child in f.Children.Values) {
                if (child == skip) continue;
                var cell = new RectangleD(child.Index.X, child.Index.Y, 1.0, 1.0);
                if (!cell.Intersects(near)) continue;
                if (recurse) {
                    double sx = fx * Frame.K;
                    double sy = fy * Frame.K;
                    long wx = (long)sx;
                    long wy = (long)sy;
                    CollectFrame(child,
                        (ix - child.Index.X) * Frame.CellCount + wx,
                        (iy - child.Index.Y) * Frame.CellCount + wy,
                        sx - wx, sy - wy,
                        ppu / Frame.K, height - 1, null, true);
                } else if (child.SubtreeCount > 0 && child.SubtreeColor != null && child.SubtreeBounds is RectangleD b) {
                    // Impostor: the subtree's bounds in this frame's units, as a dot.
                    double bx = b.X / Frame.K + child.Index.X;
                    double by = b.Y / Frame.K + child.Index.Y;
                    double bw = b.Width / Frame.K;
                    double bh = b.Height / Frame.K;
                    double cx = ((bx + bw / 2.0 - ix) - fx) * ppu;
                    double cy = ((by + bh / 2.0 - iy) - fy) * ppu;
                    Vector2 viewPos = new((float)cx, (float)cy);
                    float sizePx = (float)(Math.Max(bw, bh) * ppu);
                    float radius = Math.Max(0.75f, sizePx * 0.5f);
                    _drawables.Add(new Drawable(child.SubtreeMaxId, viewPos, viewPos, radius, child.SubtreeColor.Value));
                }
            }
        }

        /// <summary>
        /// Converts a stroke to a camera-relative drawable. All geometry is reduced in
        /// doubles first — gigantic strokes become a screen-local edge or full cover,
        /// long strokes get trimmed to the screen's vicinity — so the float vertices
        /// handed to the GPU always stay small enough to be subpixel exact.
        /// </summary>
        private void EmitLine(Line l, long ix, long iy, double fx, double fy, double ppu) {
            double ax = ((l.A.X - ix) - fx) * ppu;
            double ay = ((l.A.Y - iy) - fy) * ppu;
            double bx = ((l.B.X - ix) - fx) * ppu;
            double by = ((l.B.Y - iy) - fy) * ppu;
            double radius = l.Radius * ppu;
            double fill = 2.0 * _screenRadius;

            if (radius > BigRadiusPx) {
                // Locally the stroke is a half-plane: emit its edge nearest the camera
                // (the origin here), the same shape CoverageStack.Collect emits.
                double abx = bx - ax, aby = by - ay;
                double len2 = abx * abx + aby * aby;
                double t = len2 > 0.0 ? Math.Clamp(-(ax * abx + ay * aby) / len2, 0.0, 1.0) : 0.0;
                double cx = ax + abx * t, cy = ay + aby * t;
                double dist = Math.Sqrt(cx * cx + cy * cy);
                double edge = radius - dist;   // camera is inside by this many pixels
                if (dist < 1e-9 || edge > fill) {
                    _drawables.Add(new Drawable(l.Id, Vector2.Zero, Vector2.Zero, (float)fill, l.Color));
                } else if (edge > -fill) {
                    Vector2 n = new((float)(-cx / dist), (float)(-cy / dist));
                    Vector2 tangent = new(-n.Y, n.X);
                    Vector2 center = n * (float)(edge - fill);
                    _drawables.Add(new Drawable(l.Id, center - tangent * (float)(fill * 2.0), center + tangent * (float)(fill * 2.0), (float)fill, l.Color));
                }
                return;
            }

            double dx = bx - ax, dy = by - ay;
            double d2 = dx * dx + dy * dy;
            double keep = radius + fill + 64.0;
            if (d2 > 0.0) {
                // Trim to the sub-segment within reach of the screen. Cutting a capsule
                // at an interior point only sheds fill that was off screen anyway.
                double m = -(ax * dx + ay * dy) / d2;
                double c0 = (ax * ax + ay * ay - keep * keep) / d2;
                double disc = m * m - c0;
                if (disc <= 0.0) return;
                double sq = Math.Sqrt(disc);
                double t0 = Math.Max(0.0, m - sq);
                double t1 = Math.Min(1.0, m + sq);
                if (t0 >= t1) return;
                (bx, by) = (ax + dx * t1, ay + dy * t1);
                (ax, ay) = (ax + dx * t0, ay + dy * t0);
            } else if (ax * ax + ay * ay > keep * keep) {
                return;
            }

            _drawables.Add(new Drawable(l.Id, new Vector2((float)ax, (float)ay), new Vector2((float)bx, (float)by), (float)radius, l.Color));
        }

        /// <summary>
        /// Re-anchors the camera when it leaves its frame's cell or zoom band, applying
        /// the exact inverse transform to every piece of frame-relative state (tween
        /// endpoints and gesture anchors) so nothing observable changes.
        /// </summary>
        private void RebaseCamera() {
            for (int guard = 0; guard < 256; guard++) {
                Vector2D xy = _xy.Value;
                double scale = ExpToScale(_exp.Value);
                if (xy.X < 0.0 || xy.X >= Frame.K || xy.Y < 0.0 || xy.Y >= Frame.K) {
                    // Out of the cell laterally: go up; the next iterations descend
                    // back into the right cell, which composes into an exact hop.
                    AscendCamera();
                } else if (scale > Frame.BandMax) {
                    DescendCamera();
                } else if (scale < Frame.BandMin) {
                    AscendCamera();
                } else {
                    break;
                }
            }

            _camera.XY = _xy.Value;
            _camera.Scale = ExpToScale(_exp.Value);
        }
        private void AscendCamera() {
            Frame parent = _anchor.EnsureParent();
            Vector2D idx = _anchor.IndexOffset;
            _xy.A = _xy.A / Frame.K + idx;
            _xy.B = _xy.B / Frame.K + idx;
            _dragAnchor = _dragAnchor / Frame.K + idx;
            _mouseWorld = _mouseWorld / Frame.K + idx;
            _start = _start / Frame.K + idx;
            _end = _end / Frame.K + idx;
            ShiftExp(-Frame.LnK);
            _anchor = parent;
            _coverage.OnAscend(idx, AncestorAt(_anchor, MaxAncestorQuery));
        }
        private void DescendCamera() {
            // The ancestor about to leave tree-query range hands its strokes near the
            // camera over to the coverage stack (still in current anchor units, so the
            // OnDescend below transforms the fresh entries too).
            Frame? source = AncestorAt(_anchor, MaxAncestorQuery);
            if (source != null) {
                _coverage.SeedFrom(source, _anchor, _xy.Value, ExpToScale(_exp.Value));
            }

            Vector2D xy = _xy.Value;
            var index = ((long)Math.Floor(xy.X), (long)Math.Floor(xy.Y));
            Frame child = _anchor.GetOrCreateChild(index);
            Vector2D idx = new(index.Item1, index.Item2);
            _xy.A = (_xy.A - idx) * Frame.K;
            _xy.B = (_xy.B - idx) * Frame.K;
            _dragAnchor = (_dragAnchor - idx) * Frame.K;
            _mouseWorld = (_mouseWorld - idx) * Frame.K;
            _start = (_start - idx) * Frame.K;
            _end = (_end - idx) * Frame.K;
            ShiftExp(Frame.LnK);
            _anchor = child;
            _coverage.OnDescend(idx);
        }
        private static Frame? AncestorAt(Frame f, int distance) {
            Frame? cur = f;
            for (int i = 0; i < distance && cur != null; i++) {
                cur = cur.Parent;
            }
            return cur;
        }
        private void RebuildCoverage() {
            _coverage.Clear();
            Vector2D xy = _xy.Value;
            double scale = ExpToScale(_exp.Value);
            for (Frame? src = AncestorAt(_anchor, MaxAncestorQuery + 1); src != null; src = src.Parent) {
                _coverage.SeedFrom(src, _anchor, xy, scale);
            }
        }
        private void ShiftExp(double delta) {
            _exp.A += delta;
            _exp.B += delta;
            _targetExp += delta;
            _preservedExp += delta;
            _expStart += delta;
        }

        /// <summary>
        /// Finds the right frame for a stroke given camera-frame coordinates: promotes
        /// it up while it is too big for one cell, then re-homes it so its center lies
        /// inside its frame's cell. Keeps per-frame overhang bounded to one cell width,
        /// which is what CollectFrame's one-cell margin relies on.
        /// </summary>
        private (Frame Node, Vector2D A, Vector2D B, double Radius) NormalizeAnchor(Frame f, Vector2D a, Vector2D b, double radius) {
            while (Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) + radius * 2.0 > Frame.K) {
                Frame parent = f.EnsureParent();
                Vector2D idx = f.IndexOffset;
                a = a / Frame.K + idx;
                b = b / Frame.K + idx;
                radius /= Frame.K;
                f = parent;
            }

            Vector2D center = (a + b) / 2.0;
            int up = 0;
            while (center.X < 0.0 || center.X >= Frame.K || center.Y < 0.0 || center.Y >= Frame.K) {
                Frame parent = f.EnsureParent();
                Vector2D idx = f.IndexOffset;
                a = a / Frame.K + idx;
                b = b / Frame.K + idx;
                center = (a + b) / 2.0;
                radius /= Frame.K;
                f = parent;
                up++;
            }
            while (up-- > 0) {
                var index = ((long)Math.Floor(center.X), (long)Math.Floor(center.Y));
                Frame child = f.GetOrCreateChild(index);
                Vector2D idx = new(index.Item1, index.Item2);
                a = (a - idx) * Frame.K;
                b = (b - idx) * Frame.K;
                center = (a + b) / 2.0;
                radius *= Frame.K;
                f = child;
            }

            return (f, a, b, radius);
        }
        private void SaveCam(string key) {
            _savedCams[key] = new SavedCamD {
                Node = _anchor,
                XY = _camera.XY,
                Exp = ScaleToExp(_camera.Scale),
                Rotation = _camera.Rotation
            };
        }
        private void LoadCam(string key) {
            if (_savedCams.TryGetValue(key, out SavedCamD? cam)) {
                _savedCams["0"] = new SavedCamD {
                    Node = _anchor,
                    XY = _xy.B,
                    Exp = _exp.B,
                    Rotation = _rotation.B
                };

                if (TryTransformCam(cam, out Vector2D xy, out double exp)) {
                    _flightCam = null;
                    SetXYTween(xy);
                    SetExpTween(exp);
                    SetRotationTween(cam.Rotation);
                } else if (!TryStartFlight(cam)) {
                    TeleportCam(cam);
                }
                ShowZoomSidebar();
            }
        }
        /// <summary>
        /// Expresses a saved camera in the current anchor frame so it can be animated
        /// to with a single direct tween. Fails when the target is too many levels or
        /// cells away for that tween to be meaningful; the caller flies or teleports
        /// instead.
        /// </summary>
        private bool TryTransformCam(SavedCamD cam, out Vector2D xy, out double exp) {
            xy = cam.XY;
            exp = cam.Exp;
            if (Math.Abs(cam.Node.Level - _anchor.Level) > 2) return false;
            if (!TransformCam(cam, out xy, out exp)) return false;

            // Too far to tween meaningfully.
            if (Math.Abs(xy.X) > 1e9 || Math.Abs(xy.Y) > 1e9) return false;
            if (Math.Abs(exp - _exp.Value) > 3.0 * Frame.LnK) return false;

            return true;
        }
        /// <summary>
        /// Expresses a saved camera in the current anchor frame: raises it to the
        /// common ancestor, then descends the anchor's chain. The raise rounds at
        /// 2^-53 and each descended level amplifies that by K, so the chain is capped
        /// at 2 levels; past that the result would not land where the camera was
        /// saved.
        /// </summary>
        private bool TransformCam(SavedCamD cam, out Vector2D xy, out double exp) {
            xy = cam.XY;
            exp = cam.Exp;

            Frame b = cam.Node;
            while (b.Level > _anchor.Level) {
                if (b.Parent == null) return false;
                xy = xy / Frame.K + b.IndexOffset;
                exp -= Frame.LnK;
                b = b.Parent;
            }
            Frame a = _anchor;
            List<Frame> chain = [];
            while (a.Level > b.Level) {
                if (a.Parent == null) return false;
                chain.Add(a);
                a = a.Parent;
            }
            while (a != b) {
                if (a.Parent == null || b.Parent == null) return false;
                chain.Add(a);
                a = a.Parent;
                xy = xy / Frame.K + b.IndexOffset;
                exp -= Frame.LnK;
                b = b.Parent;
            }
            if (chain.Count > 2) return false;
            for (int i = chain.Count - 1; i >= 0; i--) {
                xy = (xy - chain[i].IndexOffset) * Frame.K;
                exp += Frame.LnK;
            }
            return true;
        }
        /// <summary>
        /// Starts a three phase flight to a saved camera that is too far for a direct
        /// tween: zoom out over the current spot until the target is about half a
        /// screen away (and the anchor is near the common ancestor, where the target's
        /// coordinates are precise), pan over to it, then dive back in. Each phase is
        /// a plain tween in current anchor coordinates, so RebaseCamera keeps the
        /// endpoints exact across re-anchoring; UpdateFlight re-derives them from the
        /// saved camera every frame, so precision improves as the anchor approaches
        /// the target and the dive lands exactly.
        /// </summary>
        private bool TryStartFlight(SavedCamD cam) {
            // Raise both cameras to their common ancestor to measure their separation.
            Frame a = _anchor;
            Frame b = cam.Node;
            Vector2D pa = _xy.Value;
            Vector2D pb = cam.XY;
            long drop = 0;
            while (b.Level > a.Level) {
                if (b.Parent == null) return false;
                pb = pb / Frame.K + b.IndexOffset;
                b = b.Parent;
            }
            while (a.Level > b.Level) {
                if (a.Parent == null) return false;
                pa = pa / Frame.K + a.IndexOffset;
                a = a.Parent;
                drop++;
            }
            while (a != b) {
                if (a.Parent == null || b.Parent == null) return false;
                pa = pa / Frame.K + a.IndexOffset;
                a = a.Parent;
                drop++;
                pb = pb / Frame.K + b.IndexOffset;
                b = b.Parent;
            }

            var vp = GraphicsDevice.Viewport;
            double halfScreen = 0.5 * Math.Min(vp.Width, vp.Height);
            // Altitude where the pan spans about half the screen...
            double expPan = Math.Log(Math.Max(Vector2D.Distance(pa, pb), 1e-9) / halfScreen) + drop * Frame.LnK;
            // ...but no lower than where TransformCam's 2 level descent cap holds, so
            // the pan and dive endpoints are trustworthy.
            double expPrecise = (drop - 2) * Frame.LnK - Math.Log(Frame.BandMax);
            double expOut = Math.Max(Math.Max(expPan, expPrecise), _exp.Value);

            _flightCam = cam;
            if (expOut > _exp.Value) {
                _flightPhase = FlightPhase.Out;
                SetXYTween(_xy.Value, 0);
                SetExpTween(expOut, ZoomDuration(expOut - _exp.Value));
            } else {
                StartFlightPan();
            }
            return true;
        }
        private void StartFlightPan() {
            SavedCamD cam = _flightCam!;
            if (!TransformCam(cam, out Vector2D xy, out double _)) {
                _flightCam = null;
                TeleportCam(cam);
                return;
            }
            // The dive must start exactly over the target: any lateral leftover gets
            // amplified exponentially by the zoom and shoots the target off screen.
            // Only skip the pan when the leftover is subpixel.
            if ((xy - _xy.Value).Length() * ExpToScale(_exp.Value) < 1.0) {
                StartFlightDive();
                return;
            }
            _flightPhase = FlightPhase.Pan;
            SetXYTween(xy, 800);
        }
        private void StartFlightDive() {
            SavedCamD cam = _flightCam!;
            if (!TransformCam(cam, out Vector2D xy, out double exp)) {
                _flightCam = null;
                TeleportCam(cam);
                return;
            }
            _flightPhase = FlightPhase.In;
            long duration = ZoomDuration(exp - _exp.Value);
            SetXYTween(xy, 0);
            SetExpTween(exp, duration);
            SetRotationTween(cam.Rotation, duration);
        }
        private void UpdateFlight() {
            if (_flightCam == null) return;
            // Any manual camera gesture takes over and ends the flight where it is.
            if (_hyperZoom.Held() || _hyperZoom.Released() || _dragZoom.Held() || _dragCamera.Held()
                || (MouseCondition.Scrolled() && !_thickness.Held())) {
                _flightCam = null;
                return;
            }
            switch (_flightPhase) {
                case FlightPhase.Out:
                    if (TweenHelper.TotalMS >= _exp.StartTime + _exp.Duration) StartFlightPan();
                    break;
                case FlightPhase.Pan:
                    if (TransformCam(_flightCam, out Vector2D panXY, out double _)) {
                        _xy.B = panXY;
                    }
                    if (TweenHelper.TotalMS >= _xy.StartTime + _xy.Duration) StartFlightDive();
                    break;
                case FlightPhase.In:
                    if (TransformCam(_flightCam, out Vector2D diveXY, out double diveExp)) {
                        _xy.A = diveXY;
                        _xy.B = diveXY;
                        _exp.B = diveExp;
                        _targetExp = diveExp;
                    }
                    if (TweenHelper.TotalMS >= _exp.StartTime + _exp.Duration) _flightCam = null;
                    break;
            }
        }
        private static long ZoomDuration(double expDelta) {
            // Matches the direct jump's zoom rate (3 levels in 1200ms), capped so the
            // deepest flights stay snappy.
            return (long)Math.Clamp(Math.Abs(expDelta) / Frame.LnK * 400.0, 1200.0, 4000.0);
        }
        private void TeleportCam(SavedCamD cam) {
            _flightCam = null;
            // Gesture and stroke state is meaningless across an instant frame switch.
            if (_isMouseDrawing || _isTabletDrawing) {
                _isMouseDrawing = false;
                _isTabletDrawing = false;
                CreateGroup();
            }
            _anchor = cam.Node;
            _dragAnchor = cam.XY;
            _mouseWorld = cam.XY;
            _start = cam.XY;
            _end = cam.XY;
            _preservedExp = cam.Exp;
            _expStart = cam.Exp;
            SetXYTween(cam.XY, 0);
            SetExpTween(cam.Exp, 0);
            SetRotationTween(cam.Rotation, 0);
            _camera.XY = cam.XY;
            _camera.Scale = ExpToScale(cam.Exp);
            RebuildCoverage();
        }
        private void SetXYTween(double targetX, double targetY, long duration = 1200) {
            SetXYTween(new Vector2D(targetX, targetY), duration);
        }
        private void SetXYTween(Vector2D target, long duration = 1200) {
            _xy.A = _xy.Value;
            _xy.B = target;
            _xy.StartTime = TweenHelper.TotalMS;
            _xy.Duration = duration;
        }
        private void SetExpTween(double target, long duration = 1200) {
            _targetExp = target;
            _exp.A = _exp.Value;
            _exp.B = _targetExp;
            _exp.StartTime = TweenHelper.TotalMS;
            _exp.Duration = duration;
            ShowZoomSidebar();
        }
        private void SetRotationTween(float target, long duration = 1200) {
            _rotation.A = _rotation.Value;
            _rotation.B = target;
            _rotation.StartTime = TweenHelper.TotalMS;
            _rotation.Duration = duration;
        }
        private void ShowZoomSidebar() {
            if (TweenHelper.TotalMS >= _zoomSidebarTween.StartTime + _zoomSidebarTween.Duration) {
                _zoomSidebarStart.StartTime = TweenHelper.TotalMS;
                _zoomSidebarStart.A = 0f;
                _zoomSidebarStart.B = 0.2f;
            } else if (TweenHelper.TotalMS < _zoomSidebarStart.StartTime + _zoomSidebarStart.Duration) {

            } else if (TweenHelper.TotalMS < _zoomSidebarTween.StartTime + _zoomSidebarTween.Duration) {
                _zoomSidebarStart.A = _zoomSidebarTween.Value;
                _zoomSidebarStart.StartTime = TweenHelper.TotalMS;
            } else {
                _zoomSidebarStart.StartTime = TweenHelper.TotalMS - _zoomSidebarStart.Duration;
            }
        }

        private void CreateLine(Vector2D a, Vector2D b, double radius) {
            var c = _isErasing ? TWColor.Transparent : _color;
            var (f, na, nb, nr) = NormalizeAnchor(_anchor, a, b, radius);
            Line l = new(_nextId++, na, nb, nr, c) { Node = f };

            l.Leaf = f.Tree.Add(l.AABB, l);
            f.BubbleAdd(l.AABB, l.Id, c == TWColor.Transparent ? null : c);
            _lines.Add(l.Id, l);
            _group.Last = l.Id;
            _hasPendingHistory = true;
        }
        private void CreateGroup() {
            if (_hasPendingHistory) {
                _undoGroups.Push(_group);
                _group = (_nextId, _nextId);
                _redoGroups.Clear();
                _redoLines.Clear();
                _hasPendingHistory = false;
            }
        }
        private void Undo() {
            if (_undoGroups.Count > 0) {
                var group = _undoGroups.Pop();
                for (int i = group.First; i <= group.Last; i++) {
                    Line l = _lines[i];
                    _lines.Remove(i);
                    l.Node.Tree.Remove(l.Leaf);
                    l.Node.BubbleRemove();

                    _redoLines.Push(l);
                }
                _redoGroups.Push(group);
                _nextId = group.First;
                _group = (_nextId, _nextId);
                RebuildCoverage();
            }
        }
        private void Redo() {
            if (_redoGroups.Count > 0) {
                var group = _redoGroups.Pop();
                while (true) {
                    var l = _redoLines.Pop();
                    l.Leaf = l.Node.Tree.Add(l.AABB, l);
                    l.Node.BubbleAdd(l.AABB, l.Id, l.Color == TWColor.Transparent ? null : l.Color);
                    _lines.Add(l.Id, l);
                    _group.Last = l.Id;

                    if (l.Id == group.First) break;
                }
                _undoGroups.Push(group);
                _nextId = group.Last + 1;
                _group = (_nextId, _nextId);
                RebuildCoverage();
            }
        }
        private void UndoAll() {
            while (_undoGroups.Count > 0) {
                var group = _undoGroups.Pop();
                for (int i = group.First; i <= group.Last; i++) {
                    Line l = _lines[i];
                    _lines.Remove(i);
                    l.Node.Tree.Remove(l.Leaf);
                    l.Node.BubbleRemove();

                    _redoLines.Push(l);
                }
                _redoGroups.Push(group);
                _nextId = group.First;
                _group = (_nextId, _nextId);
            }
            RebuildCoverage();
        }
        private void RedoAll() {
            while (_redoGroups.Count > 0) {
                var group = _redoGroups.Pop();
                while (true) {
                    var l = _redoLines.Pop();
                    l.Leaf = l.Node.Tree.Add(l.AABB, l);
                    l.Node.BubbleAdd(l.AABB, l.Id, l.Color == TWColor.Transparent ? null : l.Color);
                    _lines.Add(l.Id, l);
                    _group.Last = l.Id;

                    if (l.Id == group.First) break;
                }
                _undoGroups.Push(group);
                _nextId = group.Last + 1;
                _group = (_nextId, _nextId);
            }
            RebuildCoverage();
        }
        private void SaveDrawing() {
            // Serialize the frame tree as a flat list, parents before children (nested
            // JSON would hit System.Text.Json's depth limit long before deep zooms do).
            Frame top = _anchor.TopRoot();
            List<Frame> frames = [];
            Dictionary<Frame, int> frameIds = [];
            Stack<Frame> stack = [];
            stack.Push(top);
            while (stack.Count > 0) {
                Frame f = stack.Pop();
                frameIds[f] = frames.Count;
                frames.Add(f);
                foreach (Frame child in f.Children.Values) {
                    stack.Push(child);
                }
            }

            DrawingData dd = new() {
                Version = 2,
                NextId = _nextId,
                BackgroundColor = new DrawingData.Color { R = _bgColor.R, G = _bgColor.G, B = _bgColor.B },
                Nodes = frames.Select(f => new DrawingData.JsonNode {
                    Id = frameIds[f],
                    ParentId = f.Parent == null ? -1 : frameIds[f.Parent],
                    I = f.Index.X,
                    J = f.Index.Y,
                    Lines = f.Tree.Select(ToJsonLine).ToList()
                }).ToList(),
                UndoGroups = _undoGroups.Select(e => new DrawingData.Group {
                    First = e.First,
                    Last = e.Last
                }).ToList(),
                RedoGroups = _redoGroups.Select(e => new DrawingData.Group {
                    First = e.First,
                    Last = e.Last
                }).ToList(),
                RedoLines = _redoLines.Select(e => {
                    var line = ToJsonLine(e);
                    line.NodeId = frameIds[e.Node];
                    return line;
                }).ToList(),

                Camera = ToJsonCam(frameIds, _anchor, _camera.XY, ScaleToExp(_camera.Scale), _camera.Rotation),

                SavedCams = _savedCams.ToDictionary(
                    kv => kv.Key,
                    kv => ToJsonCam(frameIds, kv.Value.Node, kv.Value.XY, kv.Value.Exp, kv.Value.Rotation))
            };

            SaveJson("Drawing.json", dd, DrawingDataContext.Default.DrawingData);
        }
        private static DrawingData.JsonLine ToJsonLine(Line e) {
            return new DrawingData.JsonLine {
                Id = e.Id,
                A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                Radius = e.Radius,
                Color = e.Color == TWColor.Transparent ? null : new DrawingData.Color { R = e.Color.R, G = e.Color.G, B = e.Color.B }
            };
        }
        private static DrawingData.Cam ToJsonCam(Dictionary<Frame, int> frameIds, Frame node, Vector2D xy, double exp, float rotation) {
            List<(long X, long Y)> pairs = [];
            Frame f = node;
            while (f.Parent != null) {
                pairs.Add(f.Index);
                f = f.Parent;
            }
            pairs.Reverse();
            List<long> path = [];
            foreach (var (x, y) in pairs) {
                path.Add(x);
                path.Add(y);
            }
            return new DrawingData.Cam {
                Path = path,
                X = xy.X,
                Y = xy.Y,
                Exp = exp,
                Rotation = rotation
            };
        }
        private void LoadDrawing() {
            DrawingData dd = EnsureJson("Drawing.json", DrawingDataContext.Default.DrawingData);
            _nextId = dd.NextId;
            _group = (_nextId, _nextId);
            _bgColor = new Color(dd.BackgroundColor.R, dd.BackgroundColor.G, dd.BackgroundColor.B);

            if (dd.Version >= 2) {
                LoadDrawingV2(dd);
            } else {
                BackupV1Drawing(dd);
                LoadDrawingV1(dd);
            }

            for (int i = dd.UndoGroups.Count - 1; i >= 0; i--) {
                var group = dd.UndoGroups[i];
                _undoGroups.Push((group.First, group.Last));
            }
            for (int i = dd.RedoGroups.Count - 1; i >= 0; i--) {
                var group = dd.RedoGroups[i];
                _redoGroups.Push((group.First, group.Last));
            }

            RebaseCamera();
            RebuildCoverage();
        }
        private void LoadDrawingV2(DrawingData dd) {
            List<Frame> frames = [];
            foreach (var n in dd.Nodes) {
                Frame f;
                if (n.ParentId < 0 || n.ParentId >= frames.Count) {
                    f = new Frame();
                } else {
                    f = frames[n.ParentId].GetOrCreateChild((n.I, n.J));
                }
                frames.Add(f);
                foreach (var e in n.Lines) {
                    Line l = new(e.Id, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius, JsonColor(e.Color)) { Node = f };
                    l.Leaf = f.Tree.Add(l.AABB, l);
                    f.BubbleAdd(l.AABB, l.Id, l.Color == TWColor.Transparent ? null : l.Color);
                    _lines.Add(l.Id, l);
                }
            }
            Frame root = frames.Count > 0 ? frames[0] : new Frame();

            for (int i = dd.RedoLines.Count - 1; i >= 0; i--) {
                var e = dd.RedoLines[i];
                Frame f = e.NodeId >= 0 && e.NodeId < frames.Count ? frames[e.NodeId] : root;
                _redoLines.Push(new Line(e.Id, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius, JsonColor(e.Color)) { Node = f });
            }

            (_anchor, Vector2D xy, double exp) = FromJsonCam(root, dd.Camera);
            SetXYTween(xy, 0);
            SetExpTween(exp, 0);
            SetRotationTween(dd.Camera.Rotation, 0);

            foreach (var kv in dd.SavedCams) {
                var (node, camXY, camExp) = FromJsonCam(root, kv.Value);
                _savedCams[kv.Key] = new SavedCamD { Node = node, XY = camXY, Exp = camExp, Rotation = kv.Value.Rotation };
            }
        }
        private static void BackupV1Drawing(DrawingData dd) {
            // One-time safety net before migrating: the v2 save is not readable by
            // older builds (they would see an empty canvas and auto-save over it), so
            // keep the original around for rollbacks.
            if (dd.Lines.Count == 0 && dd.RedoLines.Count == 0) return;

            string source = GetPath("Drawing.json");
            string backup = GetPath("Drawing.v1.bak.json");
            try {
                if (File.Exists(source) && !File.Exists(backup)) {
                    File.Copy(source, backup);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Drawing backup failed: {ex}");
            }
        }
        private void LoadDrawingV1(DrawingData dd) {
            // v1: float coordinates in one flat world frame. Everything lands in a
            // fresh root and gets re-homed to the proper cells by NormalizeAnchor.
            Frame root = _anchor;
            foreach (var e in dd.Lines) {
                var (f, a, b, r) = NormalizeAnchor(root, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius);
                Line l = new(e.Id, a, b, r, JsonColor(e.Color)) { Node = f };
                l.Leaf = f.Tree.Add(l.AABB, l);
                f.BubbleAdd(l.AABB, l.Id, l.Color == TWColor.Transparent ? null : l.Color);
                _lines.Add(l.Id, l);
            }
            for (int i = dd.RedoLines.Count - 1; i >= 0; i--) {
                var e = dd.RedoLines[i];
                var (f, a, b, r) = NormalizeAnchor(root, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius);
                _redoLines.Push(new Line(e.Id, a, b, r, JsonColor(e.Color)) { Node = f });
            }

            SetXYTween(new Vector2D(dd.Camera.X, dd.Camera.Y), 0);
            SetExpTween(ScaleToExp(ZToScale(dd.Camera.Z)), 0);
            SetRotationTween(dd.Camera.Rotation, 0);

            foreach (var kv in dd.SavedCams) {
                _savedCams[kv.Key] = new SavedCamD {
                    Node = root,
                    XY = new Vector2D(kv.Value.X, kv.Value.Y),
                    Exp = ScaleToExp(ZToScale(kv.Value.Z)),
                    Rotation = kv.Value.Rotation
                };
            }
        }
        private static Color JsonColor(DrawingData.Color? c) {
            return c == null ? TWColor.Transparent : new Color(c.R, c.G, c.B);
        }
        private static (Frame Node, Vector2D XY, double Exp) FromJsonCam(Frame root, DrawingData.Cam cam) {
            Frame f = root;
            if (cam.Path != null) {
                for (int i = 0; i + 1 < cam.Path.Count; i += 2) {
                    f = f.GetOrCreateChild((cam.Path[i], cam.Path[i + 1]));
                }
            }
            return (f, new Vector2D(cam.X, cam.Y), cam.Exp);
        }
        private void SavePalette() {
            Palette.Color[][] colors = new Palette.Color[_cp.Colors.Length][];
            for (int i = 0; i < _cp.Colors.Length; i++) {
                colors[i] = new Palette.Color[_cp.Colors[i].Length];
                for (int j = 0; j < _cp.Colors[i].Length; j++) {
                    colors[i][j] = _cp.Colors[i][j];
                }
            }
            Palette p = new() {
                Colors = colors
            };
            SaveJson("Palette.json", p, PaletteContext.Default.Palette);
        }
        private void LoadPalette() {
            Palette p = EnsureJson("Palette.json", PaletteContext.Default.Palette);

            Color[][] colors = new Color[p.Colors.Length][];

            for (int i = 0; i < p.Colors.Length; i++) {
                colors[i] = new Color[p.Colors[i].Length];
                for (int j = 0; j < p.Colors[i].Length; j++) {
                    Palette.Color c = p.Colors[i][j];
                    colors[i][j] = new Color(c.R, c.G, c.B);
                }
            }

            _cp.Colors = colors;
        }

        public static string GetPath(string name) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, name);
        public static void SaveJson<T>(string name, T json, JsonTypeInfo<T> typeInfo) {
            string jsonPath = GetPath(name);
            string jsonString = JsonSerializer.Serialize(json, typeInfo);
            File.WriteAllText(jsonPath, jsonString);
        }
        public static T EnsureJson<T>(string name, JsonTypeInfo<T> typeInfo) where T : new() {
            T json;
            string jsonPath = GetPath(name);

            if (File.Exists(jsonPath)) {
                json = JsonSerializer.Deserialize(File.ReadAllText(jsonPath), typeInfo)!;
            } else {
                json = new T();
                string jsonString = JsonSerializer.Serialize(json, typeInfo);
                File.WriteAllText(jsonPath, jsonString);
            }

            return json;
        }

        private void ToggleFullscreen() {
            bool oldIsFullscreen = _settings.IsFullscreen;

            if (_settings.IsBorderless) {
                _settings.IsBorderless = false;
            } else {
                _settings.IsFullscreen = !_settings.IsFullscreen;
            }

            ApplyFullscreenChange(oldIsFullscreen);
        }
        private void ToggleBorderless() {
            bool oldIsFullscreen = _settings.IsFullscreen;

            _settings.IsBorderless = !_settings.IsBorderless;
            _settings.IsFullscreen = _settings.IsBorderless;

            ApplyFullscreenChange(oldIsFullscreen);
        }

        private void ApplyFullscreenChange(bool oldIsFullscreen) {
            if (_settings.IsFullscreen) {
                if (oldIsFullscreen) {
                    ApplyHardwareMode();
                } else {
                    SetFullscreen();
                }
            } else {
                UnsetFullscreen();
            }
        }
        private void ApplyHardwareMode() {
            _graphics.HardwareModeSwitch = !_settings.IsBorderless;
            _graphics.ApplyChanges();
        }
        private void SetFullscreen() {
            SaveWindow();

            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.HardwareModeSwitch = !_settings.IsBorderless;

            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
        private void UnsetFullscreen() {
            _graphics.IsFullScreen = false;
            RestoreWindow();
        }
        private void SaveWindow() {
            _settings.X = Window.ClientBounds.X;
            _settings.Y = Window.ClientBounds.Y;
            _settings.Width = Window.ClientBounds.Width;
            _settings.Height = Window.ClientBounds.Height;
        }
        private void RestoreWindow() {
            Window.Position = new Point(_settings.X, _settings.Y);
            _graphics.PreferredBackBufferWidth = _settings.Width;
            _graphics.PreferredBackBufferHeight = _settings.Height;
            _graphics.ApplyChanges();
        }

        private class SavedCamD {
            public Frame Node = null!;
            public Vector2D XY;
            public double Exp;
            public float Rotation;
        }

        #if SDLWINDOWS
        private struct QueryTablet : IEnumerator<(int, int, float)>, IEnumerable<(int, int, float)> {
            public QueryTablet(CWintabData data) {
                _count = 0;
                _at = data.GetDataPackets(100, true, ref _count);
                _maxPressure = CWintabInfo.GetMaxPressure();
                if (_count > 0) {
                    _isDone = false;
                } else {
                    _isDone = true;
                }
                _isStarted = false;
                _current = default;
            }

            public readonly (int, int, float) Current => _current!.Value;

            readonly object IEnumerator.Current {
                get {
                    if (!_isStarted || _isDone) {
                        throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                    }
                    return _current!;
                }
            }

            public readonly void Dispose() { }

            public bool MoveNext() {
                _isStarted = true;

                if (_index < _count) {
                    WintabPacket wp = _at[_index];
                    _current = (wp.pkX, wp.pkY, wp.pkNormalPressure / _maxPressure);
                    _index++;
                    return true;
                } else {
                    _isDone = true;
                    _current = default;
                }

                return false;
            }

            public void Reset() {
                _index = 0;
                _isDone = false;
                _isStarted = false;
                _current = default;
            }

            public readonly IEnumerator<(int, int, float)> GetEnumerator() => this;
            readonly IEnumerator IEnumerable.GetEnumerator() => this;

            private readonly WintabPacket[] _at;
            private int _index = 0;
            private readonly uint _count;
            private readonly float _maxPressure;
            private (int, int, float)? _current;
            private bool _isDone;
            private bool _isStarted;
        }
        #endif

        readonly GraphicsDeviceManager _graphics;
        CameraD _camera = null!;
        SpriteBatch _s = null!;
        ShapeBatch _sb = null!;
        FontSystem _fontSystem = null!;

        readonly Settings _settings;

        Frame _anchor = null!;
        Dictionary<int, Line> _lines = null!;
        readonly List<Drawable> _drawables = [];
        float _screenRadius;
        readonly CoverageStack _coverage = new();
        (int First, int Last) _group = (0, 0);
        bool _hasPendingHistory = false;
        Stack<(int First, int Last)> _undoGroups = null!;
        Stack<(int First, int Last)> _redoGroups = null!;
        Stack<Line> _redoLines = null!;

        int _nextId;

        ICondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );

        ICondition _draw = new MouseCondition(MouseButton.LeftButton);
        ICondition _line =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftShift),
                new KeyboardCondition(Keys.RightShift)
            );
        ICondition _thickness =
            new AllCondition(
                new AnyCondition(
                    new KeyboardCondition(Keys.LeftControl),
                    new KeyboardCondition(Keys.RightControl)
                ),
                new AnyCondition(
                    new KeyboardCondition(Keys.LeftShift),
                    new KeyboardCondition(Keys.RightShift)
                ),
                new MouseCondition(MouseButton.LeftButton)
            );
        ICondition _dragZoom =
            new AllCondition(
                new AnyCondition(
                    new KeyboardCondition(Keys.LeftControl),
                    new KeyboardCondition(Keys.RightControl)
                ),
                new MouseCondition(MouseButton.MiddleButton)
            );
        ICondition _rotateLeft = new KeyboardCondition(Keys.OemComma);
        ICondition _rotateRight = new KeyboardCondition(Keys.OemPeriod);

        ICondition _dragCamera =
            new AnyCondition(
                new MouseCondition(MouseButton.RightButton),
                new MouseCondition(MouseButton.MiddleButton),
                new KeyboardCondition(Keys.X)
            );

        ICondition _toggleDebug = new KeyboardCondition(Keys.F1);
        ICondition _resetFPS = new KeyboardCondition(Keys.F2);
        ICondition _toggleMouse = new KeyboardCondition(Keys.M);

        ICondition _undo =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new Track.KeyboardCondition(Keys.Z)
            );
        ICondition _redo =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftShift),
                    new Track.KeyboardCondition(Keys.RightShift)
                ),
                new Track.KeyboardCondition(Keys.Z)
            );
        ICondition _undoAll =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.Back),
                    new Track.KeyboardCondition(Keys.Delete)
                )
            );
        ICondition _redoAll =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftShift),
                    new Track.KeyboardCondition(Keys.RightShift)
                ),
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.Back),
                    new Track.KeyboardCondition(Keys.Delete)
                )
            );
        ICondition _save =
            new AllCondition(
                new AnyCondition(
                    new Track.KeyboardCondition(Keys.LeftControl),
                    new Track.KeyboardCondition(Keys.RightControl)
                ),
                new Track.KeyboardCondition(Keys.S)
            );

        ICondition _toggleFullscreen =
            new AllCondition(
                new KeyboardCondition(Keys.LeftAlt),
                new KeyboardCondition(Keys.Enter)
            );
        ICondition _toggleBorderless = new KeyboardCondition(Keys.F11);

        ICondition _pickBackground =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );
        ICondition _pickColor =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftAlt),
                new KeyboardCondition(Keys.RightAlt)
            );

        ICondition _hyperZoom = new KeyboardCondition(Keys.Space);

        ICondition _toggleEraser = new KeyboardCondition(Keys.E);

        static ICondition _ctrl =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );

        Dictionary<string, SavedCamD> _savedCams = null!;

        enum FlightPhase { Out, Pan, In }
        FlightPhase _flightPhase;
        SavedCamD? _flightCam;

        ICondition _loadCam1 = new Track.KeyboardCondition(Keys.D1);
        ICondition _saveCam1 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D1)
            );

        ICondition _loadCam2 = new Track.KeyboardCondition(Keys.D2);
        ICondition _saveCam2 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D2)
            );

        ICondition _loadCam3 = new Track.KeyboardCondition(Keys.D3);
        ICondition _saveCam3 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D3)
            );

        ICondition _loadCam4 = new Track.KeyboardCondition(Keys.D4);
        ICondition _saveCam4 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D4)
            );

        ICondition _loadCam5 = new Track.KeyboardCondition(Keys.D5);
        ICondition _saveCam5 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D5)
            );

        ICondition _loadCam6 = new Track.KeyboardCondition(Keys.D6);
        ICondition _saveCam6 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D6)
            );

        ICondition _loadCam7 = new Track.KeyboardCondition(Keys.D7);
        ICondition _saveCam7 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D7)
            );

        ICondition _loadCam8 = new Track.KeyboardCondition(Keys.D8);
        ICondition _saveCam8 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D8)
            );

        ICondition _loadCam9 = new Track.KeyboardCondition(Keys.D9);
        ICondition _saveCam9 =
            new AllCondition(
                _ctrl,
                new Track.KeyboardCondition(Keys.D9)
            );

        ICondition _loadCam0 =
            new AnyCondition(
                new Track.KeyboardCondition(Keys.D0),
                new MouseCondition(MouseButton.XButton1),
                new MouseCondition(MouseButton.XButton2)
            );

        bool _isErasing = false;
        bool _isMouseDrawing = false;
        bool _isTabletDrawing = false;
        Vector2D _start;
        Vector2D _end;
        float _radius = 10f;
        Color _color = TWColor.Gray300;
        Color _bgColor = TWColor.Black;

        ColorPicker _cp = null!;

        Vector2D _mouseWorld;
        Vector2D _dragAnchor = Vector2D.Zero;
        double _targetExp = 0.0;
        readonly double _expDistance = 0.002;
        // Sidebar range: the current frame's zoom band (zoom itself is unbounded).
        readonly double _maxExp = -Math.Log(Frame.BandMax);
        readonly double _minExp = -Math.Log(Frame.BandMin);

        float _radiusStart;
        Vector2 _thicknessStart;
        double _expStart;
        Vector2 _zoomStart;
        Vector2 _pinCamera;

        double _preservedExp = 0.0;
        readonly double _hyperZoomExp = 4.0;

        bool _showDebug = false;

        static readonly FloatTween _zoomSidebarStart = new(0f, 0.2f, 1000, Easing.QuintOut);
        static readonly ITween<float> _zoomSidebarWait = _zoomSidebarStart.Wait(1000);
        readonly ITween<float> _zoomSidebarTween = _zoomSidebarWait.To(0f, 1000, Easing.QuintOut);

        readonly Vector2DTween _xy = new(Vector2D.Zero, Vector2D.Zero, 0, EasingD.QuintOut);
        readonly DoubleTween _exp = new(0.0, 0.0, 0, EasingD.QuintOut);
        readonly FloatTween _rotation = new(0f, 0f, 0, Easing.QuintOut);

        readonly FPSCounter _fps = new();

        #if SDLWINDOWS
        CWintabContext _logContext = null!;
        CWintabData _data = null!;
        bool _tabletIsValid = false;
        Vector2D _lastTablet = Vector2D.Zero;
        float _lastPressure = 0f;
        #endif

        float _tabletPressure = 0f;

        double _maxLastTime = 0f;
        float _maxPressure = 0f;
    }
}
