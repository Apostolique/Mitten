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
//       Add tablet pressure sensitivity on macOS. (Windows uses Wintab, Linux uses XInput2.)
//       Rotation controls like Krita.

namespace GameProject {
    public partial class GameRoot : Game {
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

            #if SDLWINDOWS || SDLLINUX
            SDL2.SDL.SDL_SysWMinfo systemInfo = new();
            SDL2.SDL.SDL_VERSION(out systemInfo.version);
            SDL2.SDL.SDL_GetWindowWMInfo(Window.Handle, ref systemInfo);

            try {
                #if SDLWINDOWS
                Console.WriteLine($"Device {CWintabInfo.GetDeviceInfo()}");
                _logContext = CWintabInfo.GetDefaultSystemContext(ECTXOptionValues.CXO_MESSAGES);
                _logContext.Open(systemInfo.info.win.window, true);
                Console.WriteLine($"Context: {_logContext.HCtx}");
                _tabletIsValid = _logContext.HCtx != 0;
                if (_tabletIsValid) {
                    _data = new CWintabData(_logContext);
                }
                #else
                if (systemInfo.subsystem == SDL2.SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11) {
                    _xiTablet = new XInput2Tablet(systemInfo.info.x11.window);
                    _tabletIsValid = _xiTablet.IsValid;
                } else {
                    Console.WriteLine($"Tablet: pressure requires X11 or XWayland (got {systemInfo.subsystem}). Try launching with SDL_VIDEODRIVER=x11.");
                }
                #endif
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
            _sb = new ShapeBatch(GraphicsDevice) {
                // Every shape here is a solid color, so there is nothing to interpolate
                // and all three spaces pack the same bits. Rgb reaches them through a
                // table lookup where Oklab has to hash the color and read its cache.
                ColorSpace = ColorSpace.Rgb
            };

            // TODO: use this.Content to load your game content here
            InputHelper.Setup(this);

            _fontSystem = new FontSystem();
            _fontSystem.AddFont(TitleContainer.OpenStream($"{Content.RootDirectory}/source-code-pro-medium.ttf"));

            _lines = [];
            _strokes = [];
            _anchor = new Frame();
            _undoOps = [];
            _redoOps = [];
            _savedCams = [];
            _savedRadii = [];

            _camera = new CameraD(GraphicsDevice);

            _cp = new ColorPicker(GraphicsDevice);
            LoadPalette();

            LoadDrawing();
        }

        protected override void UnloadContent() {
            #if SDLWINDOWS
            if (_logContext is not null && _logContext.HCtx != 0) {
                _logContext.Close();
            }
            #elif SDLLINUX
            _xiTablet?.Dispose();
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
            #if SDLWINDOWS || SDLLINUX
            bool tabletProcessed = false;
            #endif
            #if SDLLINUX
            // Pumps hotplug events so a tablet plugged in after startup starts working.
            _tabletIsValid = _xiTablet is not null && _xiTablet.IsValid;
            #endif

            InputHelper.UpdateSetup();
            TweenHelper.UpdateSetup(gameTime);

            if (_quit.Pressed())
                Exit();

            if (_toggleDebug.Pressed()) _showDebug = !_showDebug;
            if (_togglePaths.Pressed()) _usePaths = !_usePaths;
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

                if (_tool == Tool.Select) {
                    UpdateSelect();
                } else if (!_isMouseDrawing && _thickness.Held()) {
                    if (_thickness.Pressed()) {
                        _radiusStart = _radius;
                        _thicknessStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                    }
                    var diffX = (InputHelper.NewMouse.X - _thicknessStart.X) / 2f;
                    _radius = MathHelper.Clamp(_radiusStart + diffX, 0.5f, 1000f);
                } else {
                    #if SDLWINDOWS || SDLLINUX
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

            if (!_isMouseDrawing && _selGesture == SelGesture.None) {
                if (_toggleEraser.Pressed()) {
                    SetTool(Tool.Erase);
                }
                if (_toggleSelect.Pressed()) {
                    SetTool(Tool.Select);
                }
                if (_toggleTemp.Pressed()) {
                    _tempMode = !_tempMode;
                }
                if (_linkRadii.Pressed()) {
                    _radiiLinked = !_radiiLinked;
                    if (_radiiLinked) {
                        // Relinking resyncs: the active tool's size wins.
                        if (_tool == Tool.Erase) {
                            _drawRadius = _eraseRadius;
                        } else {
                            _eraseRadius = _drawRadius;
                        }
                    }
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

            for (int i = 0; i < 9; i++) {
                if (_saveRadius[i].Pressed()) {
                    _savedRadii[(i + 1).ToString()] = _radius;
                }
                if (_loadRadius[i].Pressed() && _savedRadii.TryGetValue((i + 1).ToString(), out float r)) {
                    _radius = r;
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

            UpdateTempStrokes();

            #if SDLWINDOWS || SDLLINUX
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
            _pathCount = 0;

            var fgColor = _color;
            if (_tool == Tool.Erase) {
                fgColor = _bgColor;
            }

            _drawables.Clear();
            _screenRadius = 0.5f * MathF.Sqrt(
                GraphicsDevice.Viewport.Width * (float)GraphicsDevice.Viewport.Width +
                GraphicsDevice.Viewport.Height * (float)GraphicsDevice.Viewport.Height) + 2f;
            int fullCoverId = _coverage.Collect(_drawables, _camera.XY, _camera.Scale, _screenRadius);
            _emitFullCoverId = -1;
            CollectVisible();
            // Tree-queried strokes can cover the screen too (ink zoomed into from
            // inside stacks up fast near a deep cell corner); fold them into the
            // occlusion cutoff so everything underneath skips the fill.
            if (_emitFullCoverId > fullCoverId) fullCoverId = _emitFullCoverId;
            _drawables.Sort(static (x, y) => x.Id.CompareTo(y.Id));
            // A selection drag previews in view space: the trees are only touched on
            // release, when the whole gesture commits as one MoveOp/ScaleOp.
            Vector2 moveDelta = _selGesture == SelGesture.Move
                ? (Vector2)((_moveCurrent - _moveOrigin) * _camera.Scale)
                : Vector2.Zero;
            float scaleF = _selGesture == SelGesture.Scale ? (float)ScaleFactor() : 1f;
            Vector2 scaleC = _selGesture == SelGesture.Scale
                ? _camera.WorldToView((_selBoundsMin + _selBoundsMax) / 2.0)
                : Vector2.Zero;
            int inView = 0;
            foreach (var d in _drawables) {
                if (d.Id < fullCoverId) continue;
                var c = d.Color == TWColor.Transparent ? _bgColor : d.Color;
                Vector2 a = d.A;
                Vector2 b = d.B;
                float r = d.Radius;
                if (_selGesture != SelGesture.None && _selectedIds.Contains(d.Id)) {
                    if (_selGesture == SelGesture.Move) {
                        a += moveDelta;
                        b += moveDelta;
                    } else if (_selGesture == SelGesture.Scale) {
                        a = scaleC + (a - scaleC) * scaleF;
                        b = scaleC + (b - scaleC) * scaleF;
                        r *= scaleF;
                    }
                }
                StrokeSegment(a, b, r, c);
                inView++;
            }
            EndStroke();
            // Accents draw after every fill so stroke joints don't overpaint them.
            if (_selectedIds.Count > 0) {
                foreach (var d in _drawables) {
                    if (d.Id < fullCoverId || !_selectedIds.Contains(d.Id)) continue;
                    Vector2 a = d.A;
                    Vector2 b = d.B;
                    float r = d.Radius;
                    if (_selGesture == SelGesture.Move) {
                        a += moveDelta;
                        b += moveDelta;
                    } else if (_selGesture == SelGesture.Scale) {
                        a = scaleC + (a - scaleC) * scaleF;
                        b = scaleC + (b - scaleC) * scaleF;
                        r *= scaleF;
                    }
                    StrokeSegment(a, b, MathF.Max(1f, r * 0.35f), SelectAccent);
                }
                EndStroke();
            }
            DrawSelectOverlay(moveDelta);
            DrawTempStrokes();
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
            // The Select tool has no brush: the OS cursor is the pointer.
            if (_tool != Tool.Select) {
                if (_thickness.Held()) {
                    var thicknessView = _camera.WorldToView(_camera.ScreenToWorld(_thicknessStart));
                    _sb.FillCircle(thicknessView, _radius, fgColor);
                    if (_tool == Tool.Erase) {
                        _sb.BorderCircle(thicknessView, _radius, TWColor.Black, 6f);
                        _sb.BorderCircle(thicknessView, _radius - 2f, TWColor.White, 2f);
                        // A second white ring marks unlinked pen and eraser sizes.
                        if (!_radiiLinked) {
                            _sb.BorderCircle(thicknessView, _radius - 8f, TWColor.White, 2f);
                        }
                    }
                    if (_tempMode) {
                        _sb.BorderCircle(thicknessView, _radius + 6f, TempAccent, 2f);
                    }
                } else {
                    var mouseView = _camera.WorldToView(_mouseWorld);
                    _sb.FillCircle(mouseView, _radius * _tabletPressure, fgColor);
                    if (_tool == Tool.Erase) {
                        _sb.BorderCircle(mouseView, _radius * _tabletPressure, TWColor.Black, 6f);
                        _sb.BorderCircle(mouseView, (_radius - 2f) * _tabletPressure, TWColor.White, 2f);
                        if (!_radiiLinked) {
                            _sb.BorderCircle(mouseView, (_radius - 8f) * _tabletPressure, TWColor.White, 2f);
                        }
                    }
                    if (_tempMode) {
                        _sb.BorderCircle(mouseView, _radius * _tabletPressure + 6f, TempAccent, 2f);
                    }
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
                _s.DrawString(font, _usePaths ? $"Paths: {_pathCount}" : "Paths: off", new Vector2(10, 34), TWColor.White);
                _s.DrawString(font, $"Level: {_anchor.Level} -- Cell: ({_anchor.Index.X}, {_anchor.Index.Y}) -- Coverage: {_coverage.Count}", new Vector2(10, GraphicsDevice.Viewport.Height - 48), TWColor.White);
                _s.End();
            }

            base.Draw(gameTime);
        }

        #if SDLWINDOWS || SDLLINUX
        private void UpdateTablet() {
            #if SDLWINDOWS
            _data.FlushDataPackets(100);
            #else
            _xiTablet.Flush();
            #endif
        }

        private void StrokeWithTablet(double totalTime) {
            bool ranOnce = false;

            #if SDLWINDOWS
            using IEnumerator<(int, int, float)> t = new QueryTablet(_data);
            #else
            using IEnumerator<(int, int, float)> t = _xiTablet.GetPackets();
            #endif
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

                    #if SDLWINDOWS
                    // Wintab reports bottom-up screen coordinates; XInput2 packets are already window-relative.
                    y = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - y - Window.ClientBounds.Y - 1;
                    x -= Window.ClientBounds.X;
                    #endif

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
                    CommitPending();
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
                CommitPending();
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
                        _rePinZoom = false;
                    } else if (_rePinZoom) {
                        _expStart = _targetExp;
                        _zoomStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                        _rePinZoom = false;
                    }
                    var diffY = (InputHelper.NewMouse.Y - _zoomStart.Y) / 100.0;
                    SetExpTween(_expStart + diffY, 0);

                    ShowZoomSidebar();

                    _rePinZoom = WrapMouse();
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
                    _rePinDrag = false;
                }
                if (_dragCamera.Held()) {
                    if (_rePinDrag) {
                        _dragAnchor = _mouseWorld;
                        _rePinDrag = false;
                    }
                    SetXYTween(_xy.Value + _dragAnchor - _mouseWorld, 0);
                    _mouseWorld = _dragAnchor;
                    _rePinDrag = WrapMouse();
                }
            }

            RebaseCamera();

            UpdateFlight();
        }
        private bool WrapMouse() {
            if (!InputHelper.IsActive) return false;

            var vp = GraphicsDevice.Viewport;
            if (vp.Width < 4 || vp.Height < 4) return false;

            int x = InputHelper.NewMouse.X;
            int y = InputHelper.NewMouse.Y;
            int nx = x;
            int ny = y;
            if (x <= 0) nx = Math.Clamp(x + vp.Width - 2, 1, vp.Width - 2);
            else if (x >= vp.Width - 1) nx = Math.Clamp(x - (vp.Width - 2), 1, vp.Width - 2);
            if (y <= 0) ny = Math.Clamp(y + vp.Height - 2, 1, vp.Height - 2);
            else if (y >= vp.Height - 1) ny = Math.Clamp(y - (vp.Height - 2), 1, vp.Height - 2);

            if (nx == x && ny == y) return false;

            Mouse.SetPosition(nx, ny);
            return true;
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
        // Sibling and cousin branches have no coverage entries, so their frames'
        // trees are queried at whatever height the walk reaches them. Up to two
        // levels up the emitted geometry is precise (error bounded by the strokes'
        // own ~2^-36-of-a-cell coordinate precision, the best any path can do);
        // higher than that every visible stroke projects past BigRadiusPx and
        // EmitLine reduces it to a full cover or a screen-local edge — exact and
        // stable in the interior, drifting on the edge the same accepted way
        // coverage entries do — so no height cap is needed.
        // How many ancestor levels the collection walk visits at minimum. Strokes
        // reach at most one parent cell beyond their frame's cell (NormalizeAnchor),
        // so content from sibling branches up to 3 levels above can still overhang
        // into the view. Near a nested cell corner the walk keeps climbing past this
        // until the view sits clear of every span boundary (see DeepInside).
        private const int MaxWalkHeight = 3;
        // Above this radius in pixels a stroke renders as a screen-local edge or full
        // cover: float vertices cannot place the edge of a larger capsule precisely.
        private const double BigRadiusPx = 1e6;

        /// <summary>
        /// Draws one segment, chaining it onto the open path when it continues the
        /// previous one in the same color. Consecutive segments of a stroke share an
        /// endpoint exactly (both sides come out of the same frame transform), so a whole
        /// stroke streams into one path: the ink blends once across a joint instead of
        /// stacking two capsules, which is what a translucent color needs, and the joint
        /// quads are cut to the bisector so a thick stroke stops paying fill rate for the
        /// overlap. A point can carry its own radius, so a pressure-varying stroke stays
        /// one path instead of breaking at every segment. Anything that breaks the chain —
        /// a gap, a color change, an impostor dot — ends the path and starts a new one.
        /// </summary>
        private void StrokeSegment(Vector2 a, Vector2 b, float radius, Color c) {
            if (!_usePaths) {
                if (a == b) {
                    _sb.FillCircle(a, radius, c);
                } else {
                    _sb.FillLine(a, b, radius, c);
                }
                return;
            }
            if (a == b) {
                EndStroke();
                _sb.FillCircle(a, radius, c);
                _pathCount++;
                return;
            }
            if (_pathOpen && a == _pathEnd && c == _pathColor) {
                // Only a point that leaves the radius the path opened with has to carry
                // one. Carrying one is what moves the path onto the tapered geometry, so
                // a pen reporting no pressure would otherwise pay for the taper on every
                // stroke and have the library scan the radii back off again.
                if (radius == _pathRadius) {
                    _sb.PathTo(b);
                } else {
                    _sb.PathTo(b, radius);
                }
            } else {
                EndStroke();
                _sb.BeginFillPath(radius, c);
                _sb.PathTo(a);
                _sb.PathTo(b);
                _pathOpen = true;
                _pathColor = c;
                _pathRadius = radius;
                _pathCount++;
            }
            _pathEnd = b;
        }

        /// <summary>
        /// Emits the open path, if any. Must run before anything else draws: the path's
        /// geometry only reaches the batch here, so leaving it open would paint it over
        /// whatever was queued in the meantime.
        /// </summary>
        private void EndStroke() {
            if (!_pathOpen) return;
            _pathOpen = false;
            _sb.EndPath();
        }

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
            _walkUp.Clear();
            for (int height = 0; ; height++) {
                CollectFrame(f, ix, iy, fx, fy, ppu, height, skip, height <= MaxAncestorQuery);
                if (f.Parent == null) break;
                if (height >= MaxWalkHeight && DeepInside(_camera.ViewRectIn(new Vector2D(ix + fx, iy + fy), ppu))) break;

                (long qx, long rx) = FloorDivMod(ix, Frame.CellCount);
                (long qy, long ry) = FloorDivMod(iy, Frame.CellCount);
                _walkUp.Add((rx, ry, fx, fy));
                fx = (rx + fx) / Frame.K;
                fy = (ry + fy) / Frame.K;
                ix = qx + f.Index.X;
                iy = qy + f.Index.Y;
                ppu *= Frame.K;
                skip = f;
                f = f.Parent;
            }
        }

        // True when the rect sits a couple of cells clear of the frame's span
        // boundary: content in branches that diverge higher up stays within a cell
        // of its own span, so nothing above can reach the rect and the walk can
        // stop. Zooming in place exhausts the camera's mantissa and lands it
        // exactly on a nested cell corner; the view then hugs a span boundary at
        // every level below the corner's own, and the walk keeps climbing to it so
        // ink drawn across the corner (anchored in branches diverging there) is
        // still found.
        private static bool DeepInside(RectangleD r) {
            return r.X >= 2.0 && r.Y >= 2.0 && r.Right < Frame.K - 2.0 && r.Bottom < Frame.K - 2.0;
        }

        private static (long Q, long R) FloorDivMod(long v, long k) {
            long q = Math.DivRem(v, k, out long r);
            if (r < 0) { q--; r += k; }
            return (q, r);
        }

        /// <summary>
        /// The camera's sub-cell digits for descending one level from a frame at the
        /// given walk height. Levels the walk ascended through read the ledger
        /// recorded on the way up, so descending back into a sibling or cousin
        /// branch reuses the exact digits no matter how high the walk went — deriving
        /// them from fx again would amplify its ascent rounding by K per level.
        /// Below the anchor the digits come straight off the fraction's bits, an
        /// exact shift.
        /// </summary>
        private (long Wx, long Wy, double Fx, double Fy) SplitCam(double fx, double fy, int height) {
            if (height >= 1 && height <= _walkUp.Count) {
                return _walkUp[height - 1];
            }
            double sx = fx * Frame.K;
            double sy = fy * Frame.K;
            long wx = (long)sx;
            long wy = (long)sy;
            return (wx, wy, sx - wx, sy - wy);
        }

        private void CollectFrame(Frame f, long ix, long iy, double fx, double fy, double ppu, int height, Frame? skip, bool ownTree) {
            // Collapsing the split camera is safe for queries: visited frames keep the
            // camera within a few cells of their origin, and the rects have margins.
            Vector2D cam = new(ix + fx, iy + fy);
            RectangleD view = _camera.ViewRectIn(cam, ppu);
            // Strokes anchored here are at most one cell (K units) across: below half
            // a pixel none of them can be visible.
            if (ownTree && f.Tree.Count > 0 && ppu * Frame.K >= 0.5) {
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
                    var (wx, wy, sx, sy) = SplitCam(fx, fy, height);
                    CollectFrame(child,
                        (ix - child.Index.X) * Frame.CellCount + wx,
                        (iy - child.Index.Y) * Frame.CellCount + wy,
                        sx, sy,
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
                    if (l.Id > _emitFullCoverId) _emitFullCoverId = l.Id;
                } else if (edge > -fill) {
                    Vector2 n = new((float)(-cx / dist), (float)(-cy / dist));
                    Vector2 tangent = new(-n.Y, n.X);
                    Vector2 center = n * (float)(edge - fill);
                    _drawables.Add(new Drawable(l.Id, center - tangent * (float)(fill * 2.0), center + tangent * (float)(fill * 2.0), (float)fill, l.Color));
                    if (edge >= _screenRadius && l.Id > _emitFullCoverId) _emitFullCoverId = l.Id;
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

            // The capsule occludes the whole screen when the origin sits deeper
            // inside it than the screen radius.
            double tdx = bx - ax, tdy = by - ay;
            double td2 = tdx * tdx + tdy * tdy;
            double tt = td2 > 0.0 ? Math.Clamp(-(ax * tdx + ay * tdy) / td2, 0.0, 1.0) : 0.0;
            double ex = ax + tdx * tt, ey = ay + tdy * tt;
            if (radius - Math.Sqrt(ex * ex + ey * ey) >= _screenRadius && l.Id > _emitFullCoverId) {
                _emitFullCoverId = l.Id;
            }
        }

        /// <summary>
        /// Visits every line whose AABB intersects the probe (given in current anchor
        /// units), walking the same frames with the same visibility thresholds as
        /// CollectVisible — only content that can resolve on screen is hit, so a
        /// sweeping edit can never touch invisible deep-frame data. The callback gets
        /// the line's camera-relative geometry in pixels (A, B, Radius), the space
        /// narrow phases run in.
        /// </summary>
        private void HitTestVisible(RectangleD probe, Action<Line, Vector2D, Vector2D, double> visit) {
            long ix = (long)Math.Floor(_camera.XY.X);
            long iy = (long)Math.Floor(_camera.XY.Y);
            double fx = _camera.XY.X - ix;
            double fy = _camera.XY.Y - iy;
            double ppu = _camera.Scale;
            // The probe rides along relative to the camera, like the view rect does.
            double px = probe.X - _camera.XY.X;
            double py = probe.Y - _camera.XY.Y;
            double pw = probe.Width;
            double ph = probe.Height;

            Frame f = _anchor;
            Frame? skip = null;
            _walkUp.Clear();
            for (int height = 0; ; height++) {
                HitTestFrame(f, ix, iy, fx, fy, ppu, px, py, pw, ph, height, skip, height <= MaxAncestorQuery, visit);
                if (f.Parent == null) break;
                if (height >= MaxWalkHeight && DeepInside(new RectangleD(ix + fx + px, iy + fy + py, pw, ph))) break;

                (long qx, long rx) = FloorDivMod(ix, Frame.CellCount);
                (long qy, long ry) = FloorDivMod(iy, Frame.CellCount);
                _walkUp.Add((rx, ry, fx, fy));
                fx = (rx + fx) / Frame.K;
                fy = (ry + fy) / Frame.K;
                ix = qx + f.Index.X;
                iy = qy + f.Index.Y;
                ppu *= Frame.K;
                px /= Frame.K;
                py /= Frame.K;
                pw /= Frame.K;
                ph /= Frame.K;
                skip = f;
                f = f.Parent;
            }
        }

        private void HitTestFrame(Frame f, long ix, long iy, double fx, double fy, double ppu, double px, double py, double pw, double ph, int height, Frame? skip, bool ownTree, Action<Line, Vector2D, Vector2D, double> visit) {
            RectangleD probe = new(ix + fx + px, iy + fy + py, pw, ph);
            if (ownTree && f.Tree.Count > 0 && ppu * Frame.K >= 0.5) {
                foreach (Line l in f.Tree.Query(probe)) {
                    Vector2D a = new(((l.A.X - ix) - fx) * ppu, ((l.A.Y - iy) - fy) * ppu);
                    Vector2D b = new(((l.B.X - ix) - fx) * ppu, ((l.B.Y - iy) - fy) * ppu);
                    visit(l, a, b, l.Radius * ppu);
                }
            }

            if (f.Children.Count == 0) return;

            bool recurse = ppu >= 0.5;

            // Strokes overhang their frame's cell by at most one cell width.
            RectangleD near = new(probe.X - 1.0, probe.Y - 1.0, probe.Width + 2.0, probe.Height + 2.0);
            foreach (Frame child in f.Children.Values) {
                if (child == skip) continue;
                var cell = new RectangleD(child.Index.X, child.Index.Y, 1.0, 1.0);
                if (!cell.Intersects(near)) continue;
                if (recurse) {
                    var (wx, wy, sx, sy) = SplitCam(fx, fy, height);
                    HitTestFrame(child,
                        (ix - child.Index.X) * Frame.CellCount + wx,
                        (iy - child.Index.Y) * Frame.CellCount + wy,
                        sx, sy,
                        ppu / Frame.K,
                        px * Frame.K, py * Frame.K, pw * Frame.K, ph * Frame.K,
                        height - 1, null, true, visit);
                }
            }
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
            _marqueeA = _marqueeA / Frame.K + idx;
            _marqueeB = _marqueeB / Frame.K + idx;
            _moveOrigin = _moveOrigin / Frame.K + idx;
            _moveCurrent = _moveCurrent / Frame.K + idx;
            _selBoundsMin = _selBoundsMin / Frame.K + idx;
            _selBoundsMax = _selBoundsMax / Frame.K + idx;
            AscendTempStrokes(idx);
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
            _marqueeA = (_marqueeA - idx) * Frame.K;
            _marqueeB = (_marqueeB - idx) * Frame.K;
            _moveOrigin = (_moveOrigin - idx) * Frame.K;
            _moveCurrent = (_moveCurrent - idx) * Frame.K;
            _selBoundsMin = (_selBoundsMin - idx) * Frame.K;
            _selBoundsMax = (_selBoundsMax - idx) * Frame.K;
            DescendTempStrokes(idx);
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
            long cx = (long)Math.Floor(center.X);
            long cy = (long)Math.Floor(center.Y);
            if (cx >= 0 && cx < Frame.CellCount && cy >= 0 && cy < Frame.CellCount) {
                return (f, a, b, radius);
            }

            // Re-home to the frame at this level whose span contains the center.
            // Same-level frames tile space with period K, so that frame's origin sits
            // exactly K * (qx, qy) away and the coordinates shift in one exact step.
            // (Walking a and b up the tree and back in doubles instead absorbed their
            // sub-cell bits into the ancestors' cell indices — at high zoom that
            // snapped strokes drawn across nested cell corners onto a coarse grid or
            // collapsed them onto the corner itself.)
            (long qx, _) = FloorDivMod(cx, Frame.CellCount);
            (long qy, _) = FloorDivMod(cy, Frame.CellCount);
            Vector2D shift = new(qx * Frame.K, qy * Frame.K);
            a -= shift;
            b -= shift;

            // Find that frame with exact integer cell arithmetic: carry the center's
            // cell up until an ancestor's span contains it, then descend along the
            // remainders.
            _rehomePath.Clear();
            Frame above = f.EnsureParent();
            long cellX = f.Index.X + qx;
            long cellY = f.Index.Y + qy;
            f = above;
            while (cellX < 0 || cellX >= Frame.CellCount || cellY < 0 || cellY >= Frame.CellCount) {
                (long ux, long rx) = FloorDivMod(cellX, Frame.CellCount);
                (long uy, long ry) = FloorDivMod(cellY, Frame.CellCount);
                _rehomePath.Add((rx, ry));
                above = f.EnsureParent();
                cellX = f.Index.X + ux;
                cellY = f.Index.Y + uy;
                f = above;
            }
            f = f.GetOrCreateChild((cellX, cellY));
            for (int i = _rehomePath.Count - 1; i >= 0; i--) {
                f = f.GetOrCreateChild(_rehomePath[i]);
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
                CommitPending();
            }
            _selGesture = SelGesture.None;
            ClearSelection();
            // Temp segments are in old-anchor units and short-lived: an instant hop
            // to an arbitrary frame has no meaningful transform for them.
            ClearTempStrokes();
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
            if (_tempMode) {
                CreateTempLine(a, b, radius);
                return;
            }
            // Erasing paints with the background color rather than deleting: it stays
            // exactly as accurate as the cursor, where segment deletion takes whole
            // segments in one go. True deletion lives on the Select tool instead, and
            // since moves and scales are proportional, an eraser mark selected along
            // with what it covers stays in sync with it.
            var c = _tool == Tool.Erase ? TWColor.Transparent : _color;
            var (f, na, nb, nr) = NormalizeAnchor(_anchor, a, b, radius);
            Line l = new(_nextId++, na, nb, nr, c) { Node = f, StrokeId = _group.First };

            AttachLine(l);
            _group.Last = l.Id;
            _hasPendingHistory = true;
        }
        private void CommitPending() {
            CommitTempStroke();
            if (_hasPendingHistory) {
                _undoOps.Push(new DrawOp { First = _group.First, Last = _group.Last });
                _group = (_nextId, _nextId);
                _redoOps.Clear();
                _hasPendingHistory = false;
            }
        }
        private void AttachLine(Line l) {
            l.Leaf = l.Node.Tree.Add(l.AABB, l);
            l.Node.BubbleAdd(l.AABB, l.Id, l.Color == TWColor.Transparent ? null : l.Color);
            _lines.Add(l.Id, l);
            if (!_strokes.TryGetValue(l.StrokeId, out List<Line>? stroke)) {
                stroke = [];
                _strokes.Add(l.StrokeId, stroke);
            }
            stroke.Add(l);
        }
        private void DetachLine(Line l) {
            l.Node.Tree.Remove(l.Leaf);
            l.Node.BubbleRemove();
            _lines.Remove(l.Id);
            List<Line> stroke = _strokes[l.StrokeId];
            stroke.Remove(l);
            if (stroke.Count == 0) {
                _strokes.Remove(l.StrokeId);
            }
        }
        private void Undo() {
            if (_undoOps.Count > 0) {
                EditOp op = _undoOps.Pop();
                RevertOp(op);
                _redoOps.Push(op);
                // Ops may delete or replace selected lines out from under the selection.
                ClearSelection();
                RebuildCoverage();
            }
        }
        private void Redo() {
            if (_redoOps.Count > 0) {
                EditOp op = _redoOps.Pop();
                ApplyOp(op);
                _undoOps.Push(op);
                ClearSelection();
                RebuildCoverage();
            }
        }
        private void UndoAll() {
            while (_undoOps.Count > 0) {
                EditOp op = _undoOps.Pop();
                RevertOp(op);
                _redoOps.Push(op);
            }
            ClearSelection();
            RebuildCoverage();
        }
        private void RedoAll() {
            while (_redoOps.Count > 0) {
                EditOp op = _redoOps.Pop();
                ApplyOp(op);
                _undoOps.Push(op);
            }
            ClearSelection();
            RebuildCoverage();
        }
        private void RevertOp(EditOp op) {
            switch (op) {
                case DrawOp d:
                    // Every id in the range is alive: any later op touching it was
                    // already reverted (LIFO), and only draws mint ids.
                    d.Lines = new List<Line>(d.Last - d.First + 1);
                    for (int i = d.First; i <= d.Last; i++) {
                        Line l = _lines[i];
                        DetachLine(l);
                        d.Lines.Add(l);
                    }
                    _nextId = d.First;
                    _group = (_nextId, _nextId);
                    break;
                case DeleteOp del:
                    foreach (Line l in del.Lines) {
                        AttachLine(l);
                    }
                    break;
                case MoveOp m:
                    RestoreSnapshots(m.Originals);
                    break;
                case ScaleOp s:
                    RestoreSnapshots(s.Originals);
                    break;
            }
        }
        private void ApplyOp(EditOp op) {
            switch (op) {
                case DrawOp d:
                    foreach (Line l in d.Lines!) {
                        AttachLine(l);
                    }
                    d.Lines = null;
                    _nextId = d.Last + 1;
                    _group = (_nextId, _nextId);
                    break;
                case DeleteOp del:
                    foreach (Line l in del.Lines) {
                        DetachLine(l);
                    }
                    break;
                case MoveOp m:
                    ApplyMove(m);
                    break;
                case ScaleOp s:
                    ApplyScale(s);
                    break;
            }
        }
        private void RestoreSnapshots(List<LineSnapshot> snapshots) {
            foreach (LineSnapshot s in snapshots) {
                Line l = s.Line;
                DetachLine(l);
                l.Node = s.Node;
                l.A = s.A;
                l.B = s.B;
                l.Radius = s.Radius;
                l.RecomputeAABB();
                AttachLine(l);
            }
        }
        private void ApplyMove(MoveOp m) {
            foreach (LineSnapshot s in m.Originals) {
                Line l = s.Line;
                DetachLine(l);
                // Unit ratios between levels are exact powers of two, so the delta
                // converts exactly into each line's own frame. Recomputing from the
                // snapshot (not the live state) makes redo bit-identical.
                Vector2D delta = m.Delta * Math.ScaleB(1.0, 16 * (int)(s.Node.Level - m.Ref.Level));
                var (f, a, b, r) = NormalizeAnchor(s.Node, s.A + delta, s.B + delta, s.Radius);
                l.Node = f;
                l.A = a;
                l.B = b;
                l.Radius = r;
                l.RecomputeAABB();
                AttachLine(l);
            }
        }
        private void ApplyScale(ScaleOp op) {
            foreach (LineSnapshot s in op.Originals) {
                Line l = s.Line;
                DetachLine(l);
                Vector2D c = TransformPoint(op.Ref, s.Node, op.Center);
                Vector2D a = c + (s.A - c) * op.Factor;
                Vector2D b = c + (s.B - c) * op.Factor;
                var (f, na, nb, nr) = NormalizeAnchor(s.Node, a, b, s.Radius * op.Factor);
                l.Node = f;
                l.A = na;
                l.B = nb;
                l.Radius = nr;
                l.RecomputeAABB();
                AttachLine(l);
            }
        }
        /// <summary>
        /// Expresses a point given in one frame's units in another frame's units:
        /// raises it to the common ancestor, then descends the target's chain. Both
        /// frames sit within a few levels of each other when editing (the content was
        /// on screen), so the descent precision cap that limits TransformCam does not
        /// bite here.
        /// </summary>
        private static Vector2D TransformPoint(Frame from, Frame to, Vector2D p) {
            Frame b = from;
            Frame a = to;
            while (b.Level > a.Level) {
                p = p / Frame.K + b.IndexOffset;
                b = b.Parent!;
            }
            List<Frame> chain = [];
            while (a.Level > b.Level) {
                chain.Add(a);
                a = a.Parent!;
            }
            while (a != b) {
                chain.Add(a);
                a = a.Parent!;
                p = p / Frame.K + b.IndexOffset;
                b = b.Parent!;
            }
            for (int i = chain.Count - 1; i >= 0; i--) {
                p = (p - chain[i].IndexOffset) * Frame.K;
            }
            return p;
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
                Version = 3,
                NextId = _nextId,
                BackgroundColor = new DrawingData.Color { R = _bgColor.R, G = _bgColor.G, B = _bgColor.B },
                Nodes = frames.Select(f => new DrawingData.JsonNode {
                    Id = frameIds[f],
                    ParentId = f.Parent == null ? -1 : frameIds[f.Parent],
                    I = f.Index.X,
                    J = f.Index.Y,
                    // By id so the output is canonical: the tree's enumeration order
                    // depends on its insertion history, which undo/redo can shuffle.
                    Lines = f.Tree.Select(ToJsonLine).OrderBy(l => l.Id).ToList()
                }).ToList(),
                UndoOps = _undoOps.Select(op => ToJsonOp(op, frameIds)).ToList(),
                RedoOps = _redoOps.Select(op => ToJsonOp(op, frameIds)).ToList(),

                Camera = ToJsonCam(frameIds, _anchor, _camera.XY, ScaleToExp(_camera.Scale), _camera.Rotation),

                SavedCams = _savedCams.ToDictionary(
                    kv => kv.Key,
                    kv => ToJsonCam(frameIds, kv.Value.Node, kv.Value.XY, kv.Value.Exp, kv.Value.Rotation)),

                RadiiLinked = _radiiLinked,
                DrawRadius = _drawRadius,
                EraseRadius = _eraseRadius,
                SavedRadii = new Dictionary<string, float>(_savedRadii)
            };

            SaveJson("Drawing.json", dd, DrawingDataContext.Default.DrawingData);
        }
        private static DrawingData.JsonLine ToJsonLine(Line e) {
            return new DrawingData.JsonLine {
                Id = e.Id,
                A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                Radius = e.Radius,
                Color = e.Color == TWColor.Transparent ? null : new DrawingData.Color { R = e.Color.R, G = e.Color.G, B = e.Color.B },
                StrokeId = e.StrokeId
            };
        }
        private static DrawingData.JsonLine ToJsonLine(Line e, Dictionary<Frame, int> frameIds) {
            var line = ToJsonLine(e);
            line.NodeId = frameIds[e.Node];
            return line;
        }
        private static DrawingData.JsonOp ToJsonOp(EditOp op, Dictionary<Frame, int> frameIds) {
            switch (op) {
                case DrawOp d:
                    return new DrawingData.JsonOp {
                        Type = "draw",
                        First = d.First,
                        Last = d.Last,
                        Lines = d.Lines?.Select(l => ToJsonLine(l, frameIds)).ToList()
                    };
                case DeleteOp del:
                    return new DrawingData.JsonOp {
                        Type = "delete",
                        Lines = del.Lines.Select(l => ToJsonLine(l, frameIds)).ToList()
                    };
                case MoveOp m:
                    return new DrawingData.JsonOp {
                        Type = "move",
                        RefId = frameIds[m.Ref],
                        Delta = new DrawingData.XY { X = m.Delta.X, Y = m.Delta.Y },
                        Originals = m.Originals.Select(s => ToJsonSnapshot(s, frameIds)).ToList()
                    };
                case ScaleOp s:
                    return new DrawingData.JsonOp {
                        Type = "scale",
                        RefId = frameIds[s.Ref],
                        Center = new DrawingData.XY { X = s.Center.X, Y = s.Center.Y },
                        Factor = s.Factor,
                        Originals = s.Originals.Select(o => ToJsonSnapshot(o, frameIds)).ToList()
                    };
                default:
                    throw new InvalidOperationException($"Unknown op type: {op.GetType().Name}");
            }
        }
        private static DrawingData.JsonLine ToJsonSnapshot(LineSnapshot s, Dictionary<Frame, int> frameIds) {
            return new DrawingData.JsonLine {
                Id = s.Line.Id,
                A = new DrawingData.XY { X = s.A.X, Y = s.A.Y },
                B = new DrawingData.XY { X = s.B.X, Y = s.B.Y },
                Radius = s.Radius,
                Color = null,
                NodeId = frameIds[s.Node]
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
            _radiiLinked = dd.RadiiLinked;
            _drawRadius = dd.DrawRadius;
            _eraseRadius = dd.EraseRadius;
            foreach (var kv in dd.SavedRadii) {
                _savedRadii[kv.Key] = kv.Value;
            }

            if (dd.Version >= 3) {
                LoadDrawingV3(dd);
            } else if (dd.Version == 2) {
                BackupV2Drawing(dd);
                MigrateGroups(dd, LoadDrawingV2(dd));
            } else {
                BackupV1Drawing(dd);
                MigrateGroups(dd, LoadDrawingV1(dd));
            }

            RebaseCamera();
            RebuildCoverage();
        }
        private void LoadDrawingV3(DrawingData dd) {
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
                    AttachLine(NewLine(e, f));
                }
            }
            Frame root = frames.Count > 0 ? frames[0] : new Frame();

            // Every line an op references lives in exactly one place: the live set, an
            // undo-stack DeleteOp, or a redo-stack DrawOp (LIFO guarantees this).
            // Materialize the dead ones into the map first, then wire the by-id
            // references (move/scale snapshots, redo deletes) to those instances.
            Dictionary<int, Line> byId = new(_lines);
            List<(DrawingData.JsonOp Json, EditOp Op)> undoOps =
                dd.UndoOps.Select(j => (j, MaterializeOp(j, frames, root, byId, redo: false))).ToList();
            List<(DrawingData.JsonOp Json, EditOp Op)> redoOps =
                dd.RedoOps.Select(j => (j, MaterializeOp(j, frames, root, byId, redo: true))).ToList();
            foreach (var (json, op) in undoOps) {
                ResolveOp(json, op, frames, root, byId, redo: false);
            }
            foreach (var (json, op) in redoOps) {
                ResolveOp(json, op, frames, root, byId, redo: true);
            }
            for (int i = undoOps.Count - 1; i >= 0; i--) {
                _undoOps.Push(undoOps[i].Op);
            }
            for (int i = redoOps.Count - 1; i >= 0; i--) {
                _redoOps.Push(redoOps[i].Op);
            }

            LoadCams(dd, root);
        }
        private static Line NewLine(DrawingData.JsonLine e, Frame f) {
            return new Line(e.Id, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius, JsonColor(e.Color)) {
                Node = f,
                StrokeId = e.StrokeId >= 0 ? e.StrokeId : e.Id
            };
        }
        private static Frame FrameAt(List<Frame> frames, Frame root, int id) {
            return id >= 0 && id < frames.Count ? frames[id] : root;
        }
        private static EditOp MaterializeOp(DrawingData.JsonOp json, List<Frame> frames, Frame root, Dictionary<int, Line> byId, bool redo) {
            switch (json.Type) {
                case "delete": {
                    DeleteOp op = new();
                    if (!redo && json.Lines != null) {
                        // Undo-stack deletes own their dead lines.
                        foreach (var e in json.Lines) {
                            Line l = NewLine(e, FrameAt(frames, root, e.NodeId));
                            byId[l.Id] = l;
                            op.Lines.Add(l);
                        }
                    }
                    return op;
                }
                case "move":
                    return new MoveOp {
                        Ref = FrameAt(frames, root, json.RefId),
                        Delta = new Vector2D(json.Delta?.X ?? 0.0, json.Delta?.Y ?? 0.0)
                    };
                case "scale":
                    return new ScaleOp {
                        Ref = FrameAt(frames, root, json.RefId),
                        Center = new Vector2D(json.Center?.X ?? 0.0, json.Center?.Y ?? 0.0),
                        Factor = json.Factor
                    };
                default: {
                    DrawOp op = new() { First = json.First, Last = json.Last };
                    if (redo && json.Lines != null) {
                        op.Lines = json.Lines.Select(e => {
                            Line l = NewLine(e, FrameAt(frames, root, e.NodeId));
                            byId[l.Id] = l;
                            return l;
                        }).ToList();
                    }
                    return op;
                }
            }
        }
        private static void ResolveOp(DrawingData.JsonOp json, EditOp op, List<Frame> frames, Frame root, Dictionary<int, Line> byId, bool redo) {
            switch (op) {
                case DeleteOp del when redo && json.Lines != null:
                    // Redo-stack deletes reference lines that are alive or held by a
                    // redo DrawOp above them.
                    foreach (var e in json.Lines) {
                        if (byId.TryGetValue(e.Id, out Line? l)) {
                            del.Lines.Add(l);
                        }
                    }
                    break;
                case MoveOp m when json.Originals != null:
                    m.Originals = ResolveSnapshots(json.Originals, frames, root, byId);
                    break;
                case ScaleOp s when json.Originals != null:
                    s.Originals = ResolveSnapshots(json.Originals, frames, root, byId);
                    break;
            }
        }
        private static List<LineSnapshot> ResolveSnapshots(List<DrawingData.JsonLine> originals, List<Frame> frames, Frame root, Dictionary<int, Line> byId) {
            List<LineSnapshot> result = new(originals.Count);
            foreach (var e in originals) {
                if (!byId.TryGetValue(e.Id, out Line? l)) continue;
                result.Add(new LineSnapshot {
                    Line = l,
                    Node = FrameAt(frames, root, e.NodeId),
                    A = new Vector2D(e.A.X, e.A.Y),
                    B = new Vector2D(e.B.X, e.B.Y),
                    Radius = e.Radius
                });
            }
            return result;
        }
        private void LoadCams(DrawingData dd, Frame root) {
            (_anchor, Vector2D xy, double exp) = FromJsonCam(root, dd.Camera);
            SetXYTween(xy, 0);
            SetExpTween(exp, 0);
            SetRotationTween(dd.Camera.Rotation, 0);

            foreach (var kv in dd.SavedCams) {
                var (node, camXY, camExp) = FromJsonCam(root, kv.Value);
                _savedCams[kv.Key] = new SavedCamD { Node = node, XY = camXY, Exp = camExp, Rotation = kv.Value.Rotation };
            }
        }
        private List<Line> LoadDrawingV2(DrawingData dd) {
            List<(int First, int Last)> ranges = GroupRanges(dd);
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
                    Line l = new(e.Id, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius, JsonColor(e.Color)) {
                        Node = f,
                        StrokeId = DeriveStrokeId(ranges, e.Id)
                    };
                    AttachLine(l);
                }
            }
            Frame root = frames.Count > 0 ? frames[0] : new Frame();

            List<Line> redoLines = dd.RedoLines.Select(e => new Line(e.Id, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius, JsonColor(e.Color)) {
                Node = FrameAt(frames, root, e.NodeId),
                StrokeId = DeriveStrokeId(ranges, e.Id)
            }).ToList();

            LoadCams(dd, root);
            return redoLines;
        }
        /// <summary>
        /// Converts the pre-v3 id-range group stacks into DrawOps. redoLines is in
        /// file order: the top group's lines first (ids descending within a group),
        /// matching how the old Undo pushed them.
        /// </summary>
        private void MigrateGroups(DrawingData dd, List<Line> redoLines) {
            for (int i = dd.UndoGroups.Count - 1; i >= 0; i--) {
                var g = dd.UndoGroups[i];
                _undoOps.Push(new DrawOp { First = g.First, Last = g.Last });
            }
            List<DrawOp> redoOps = [];
            int cursor = 0;
            foreach (var g in dd.RedoGroups) {
                int count = g.Last - g.First + 1;
                if (cursor + count > redoLines.Count) break;
                redoOps.Add(new DrawOp { First = g.First, Last = g.Last, Lines = redoLines.GetRange(cursor, count) });
                cursor += count;
            }
            for (int i = redoOps.Count - 1; i >= 0; i--) {
                _redoOps.Push(redoOps[i]);
            }
        }
        private static List<(int First, int Last)> GroupRanges(DrawingData dd) {
            List<(int First, int Last)> ranges = dd.UndoGroups.Concat(dd.RedoGroups)
                .Select(g => (g.First, g.Last)).ToList();
            ranges.Sort();
            return ranges;
        }
        private static int DeriveStrokeId(List<(int First, int Last)> ranges, int id) {
            int lo = 0, hi = ranges.Count - 1, best = -1;
            while (lo <= hi) {
                int mid = lo + (hi - lo) / 2;
                if (ranges[mid].First <= id) {
                    best = mid;
                    lo = mid + 1;
                } else {
                    hi = mid - 1;
                }
            }
            if (best >= 0 && id <= ranges[best].Last) return ranges[best].First;
            return id;
        }
        private static void BackupV2Drawing(DrawingData dd) {
            // Same safety net as the v1 migration: a v2 build reads a v3 file's Nodes
            // fine but sees empty group stacks, so it would auto-save over the op
            // history on exit.
            if (dd.Nodes.Count == 0 && dd.RedoLines.Count == 0) return;

            string source = GetPath("Drawing.json");
            string backup = GetPath("Drawing.v2.bak.json");
            try {
                if (File.Exists(source) && !File.Exists(backup)) {
                    File.Copy(source, backup);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Drawing backup failed: {ex}");
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
        private List<Line> LoadDrawingV1(DrawingData dd) {
            // v1: float coordinates in one flat world frame. Everything lands in a
            // fresh root and gets re-homed to the proper cells by NormalizeAnchor.
            List<(int First, int Last)> ranges = GroupRanges(dd);
            Frame root = _anchor;
            foreach (var e in dd.Lines) {
                var (f, a, b, r) = NormalizeAnchor(root, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius);
                Line l = new(e.Id, a, b, r, JsonColor(e.Color)) {
                    Node = f,
                    StrokeId = DeriveStrokeId(ranges, e.Id)
                };
                AttachLine(l);
            }
            List<Line> redoLines = dd.RedoLines.Select(e => {
                var (f, a, b, r) = NormalizeAnchor(root, new Vector2D(e.A.X, e.A.Y), new Vector2D(e.B.X, e.B.Y), e.Radius);
                return new Line(e.Id, a, b, r, JsonColor(e.Color)) {
                    Node = f,
                    StrokeId = DeriveStrokeId(ranges, e.Id)
                };
            }).ToList();

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
            return redoLines;
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

        static readonly string _savePath = FindSavePath();
        public static string GetPath(string name) => Path.Combine(_savePath, name);
        // On macOS the executable lives inside Mitten.app, and an updater is free to
        // replace a bundle wholesale, so drawings go to the usual per-user directory
        // instead. Everywhere else they stay next to the executable.
        private static string FindSavePath() {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory!;

            if (!OperatingSystem.IsMacOS()) return baseDirectory;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home)) return baseDirectory;

            string path = Path.Combine(home, "Library", "Application Support", "Mitten");
            try {
                Directory.CreateDirectory(path);
            } catch (Exception) {
                // Losing the drawing beats failing to start, so fall back to the bundle.
                return baseDirectory;
            }

            return path;
        }
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
        // Live lines of each stroke, keyed by StrokeId. Kept in sync with _lines.
        Dictionary<int, List<Line>> _strokes = null!;
        readonly List<Drawable> _drawables = [];
        float _screenRadius;
        readonly CoverageStack _coverage = new();
        (int First, int Last) _group = (0, 0);
        bool _hasPendingHistory = false;
        Stack<EditOp> _undoOps = null!;
        Stack<EditOp> _redoOps = null!;

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
        ICondition _togglePaths = new KeyboardCondition(Keys.F3);
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
        ICondition _linkRadii = new KeyboardCondition(Keys.R);

        static ICondition _ctrl =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );
        static ICondition _shift =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftShift),
                new KeyboardCondition(Keys.RightShift)
            );

        // Saved brush sizes, on Shift + digits like the camera slots on plain digits.
        // Checked before the camera slots so their Track conditions consume the digit.
        Dictionary<string, float> _savedRadii = null!;
        readonly ICondition[] _loadRadius = SlotConditions(_shift);
        readonly ICondition[] _saveRadius = SlotConditions(_ctrl, _shift);

        static ICondition[] SlotConditions(params ICondition[] mods) {
            ICondition[] slots = new ICondition[9];
            for (int i = 0; i < 9; i++) {
                slots[i] = new AllCondition([.. mods, new Track.KeyboardCondition(Keys.D1 + i)]);
            }
            return slots;
        }

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

        bool _isMouseDrawing = false;
        bool _isTabletDrawing = false;
        Vector2D _start;
        Vector2D _end;
        // The pen and the eraser share one size until R unlinks them; then each keeps
        // its own and R relinks them, snapping the other tool to the active size.
        // _radius is whichever size the current tool uses.
        bool _radiiLinked = true;
        float _drawRadius = 10f;
        float _eraseRadius = 10f;
        float _radius {
            get => _tool == Tool.Erase ? _eraseRadius : _drawRadius;
            set {
                if (_radiiLinked) {
                    _drawRadius = value;
                    _eraseRadius = value;
                } else if (_tool == Tool.Erase) {
                    _eraseRadius = value;
                } else {
                    _drawRadius = value;
                }
            }
        }
        Color _color = TWColor.Gray300;
        Color _bgColor = TWColor.Black;

        ColorPicker _cp = null!;

        Vector2D _mouseWorld;
        Vector2D _dragAnchor = Vector2D.Zero;
        // Scratch: NormalizeAnchor's descent path, and the collect/hit-test walks'
        // per-level camera digit ledger (see SplitCam).
        readonly List<(long X, long Y)> _rehomePath = [];
        readonly List<(long Rx, long Ry, double Fx, double Fy)> _walkUp = [];
        // Highest id EmitLine saw whose drawable covers the whole screen this frame.
        int _emitFullCoverId = -1;
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
        bool _rePinDrag;
        bool _rePinZoom;

        double _preservedExp = 0.0;
        readonly double _hyperZoomExp = 4.0;

        bool _showDebug = false;
        // Strokes render as continuous paths; F3 falls back to a capsule per segment
        // for comparing the two.
        bool _usePaths = true;
        bool _pathOpen = false;
        Vector2 _pathEnd;
        Color _pathColor;
        float _pathRadius;
        int _pathCount = 0;

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
        #elif SDLLINUX
        XInput2Tablet _xiTablet = null!;
        #endif
        #if SDLWINDOWS || SDLLINUX
        bool _tabletIsValid = false;
        Vector2D _lastTablet = Vector2D.Zero;
        float _lastPressure = 0f;
        #endif

        float _tabletPressure = 0f;

        double _maxLastTime = 0f;
        float _maxPressure = 0f;
    }
}
