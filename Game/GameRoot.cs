using Apos.Camera;
using Apos.Input;
using Track = Apos.Input.Track;
using Apos.Shapes;
using Apos.Spatial;
using Apos.Tweens;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
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
            IsMouseVisible = true;
            Content.RootDirectory = "Content";

            _settings = EnsureJson("Settings.json", SettingsContext.Default.Settings);
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
            _tree = [];
            _undoGroups = new Stack<(int, int)>();
            _redoGroups = new Stack<(int, int)>();
            _redoLines = new Stack<Line>();
            _savedCams = [];

            _camera = new Camera(new DefaultViewport(GraphicsDevice, Window));

            _cp = new ColorPicker(GraphicsDevice, Content);

            LoadDrawing();
        }

        protected override void UnloadContent() {
            #if SDLWINDOWS
            if (_logContext.HCtx != 0) {
                _logContext.Close();
            }
            #endif

            SaveDrawing();

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

                if (!_isDrawing && _thickness.Held()) {
                    if (_thickness.Pressed()) {
                        _radiusStart = _radius;
                        _thicknessStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                    }
                    var diffX = (InputHelper.NewMouse.X - _thicknessStart.X) / 2f;
                    _radius = MathHelper.Clamp(_radiusStart + diffX, 0.5f, 1000f);
                } else {
                    #if SDLWINDOWS
                    if (!_isDrawing && _tabletIsValid) {
                        _oldIsTablet = _isTablet;
                        _isTablet = DrawTablet();
                        if (_oldIsTablet && !_isTablet) {
                            CreateGroup();
                        }
                        tabletProcessed = true;
                    }
                    #endif

                    if (_draw.Pressed() && !_isTablet) {
                        _start = _mouseWorld;
                        _isDrawing = true;
                    }
                    if (_isDrawing && _draw.Held()) {
                        _end = _mouseWorld;

                        if (_start != _end && !_line.Held()) {
                            CreateLine(_start, _end, _radius * _camera.ScreenToWorldScale());
                            _start = _mouseWorld;
                        }
                    }
                    if (_isDrawing && _draw.Released()) {
                        _isDrawing = false;
                        _end = _mouseWorld;

                        if (_start == _end) {
                            _end += new Vector2(_camera.ScreenToWorldScale());
                        }

                        CreateLine(_start, _end, _radius * _camera.ScreenToWorldScale());
                        CreateGroup();
                    }
                }
            }

            if (!_isDrawing) {
                if (_toggleEraser.Pressed()) {
                    _isErasing = !_isErasing;
                }

                if (_redo.Pressed()) {
                    Redo();
                }
                if (_undo.Pressed()) {
                    Undo();
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

            int inView = 0;
            foreach (Line l in _tree.Query(_camera.ViewRect).OrderBy(e => e.Id)) {
                var c = l.Color == TWColor.Transparent ? _bgColor : l.Color;
                _sb.FillLine(l.A, l.B, l.Radius, c);
                inView++;
            }
            if (_isDrawing) {
                _sb.FillLine(_start, _end, _radius * _camera.ScreenToWorldScale() * _tabletPressure, fgColor);
            }
            if (_thickness.Held()) {
                _sb.FillCircle(_camera.ScreenToWorld(_thicknessStart), _radius * _camera.ScreenToWorldScale(), fgColor);
                if (_isErasing) {
                    _sb.BorderCircle(_camera.ScreenToWorld(_thicknessStart), _radius * _camera.ScreenToWorldScale(), TWColor.Black, 6f);
                    _sb.BorderCircle(_camera.ScreenToWorld(_thicknessStart), (_radius - 2f) * _camera.ScreenToWorldScale(), TWColor.White, 2f);
                }
            } else {
                _sb.FillCircle(_mouseWorld, _radius * _camera.ScreenToWorldScale() * _tabletPressure, fgColor);
                if (_isErasing) {
                    _sb.BorderCircle(_mouseWorld, _radius * _camera.ScreenToWorldScale() * _tabletPressure, TWColor.Black, 6f);
                    _sb.BorderCircle(_mouseWorld, (_radius - 2f) * _camera.ScreenToWorldScale() * _tabletPressure, TWColor.White, 2f);
                }
            }

            // _sb.FillCircle(_tabletXY, 100f * _tabletPressure, TWColor.White);
            _sb.End();

            _sb.Begin();
            var camExp = ScaleToExp(_camera.ZToScale(_camera.Z, 0f));
            if (_zoomSidebarTween.Value > 0f) {
                var length = _minExp - _maxExp;
                var percent = (camExp - _maxExp) / length;
                _sb.DrawLine(new Vector2(0, GraphicsDevice.Viewport.Height), new Vector2(0, GraphicsDevice.Viewport.Height * percent), 10f, TWColor.White * _zoomSidebarTween.Value, TWColor.Black, 2f);
            }
            _sb.End();

            if (_pickColor.Held()) {
                _cp.Draw(_fontSystem, _pickBackground.Held(), _bgColor);
            }

            if (_showDebug) {
                var font = _fontSystem.GetFont(24);
                _s.Begin();
                _s.DrawString(font, $"fps: {_fps.FramesPerSecond} - Dropped Frames: {_fps.DroppedFrames} - Draw ms: {_fps.TimePerFrame} - Update ms: {_fps.TimePerUpdate}", new Vector2(10, 10), TWColor.White);
                _s.DrawString(font, $"In view: {inView} -- Total: {_lines.Count} -- {_camera.ScreenToWorldScale()}", new Vector2(10, GraphicsDevice.Viewport.Height - 24), TWColor.White);
                _s.End();
            }

            base.Draw(gameTime);
        }

        #if SDLWINDOWS
        private void UpdateTablet() {
            float maxPressure = CWintabInfo.GetMaxPressure();
            // _tabletPressure = 1f;

            uint count = 0;
            WintabPacket[] results = _data.GetDataPackets(100, true, ref count);
            for (int i = 0; i < count; i++) {
                int x = results[i].pkX;
                int y = results[i].pkY;
                float pressure = results[i].pkNormalPressure / maxPressure;

                y = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - y - Window.ClientBounds.Y;
                x -= Window.ClientBounds.X;

                Console.WriteLine($"   X: {x} -- Y: {y} ::: {pressure}");
                _tabletXYa = _tabletXYb;
                _tabletXYb = _camera.ScreenToWorld(x, y);
                _tabletPressure = pressure;
            }
        }
        private bool DrawTablet() {
            bool usedTablet = false;
            float maxPressure = CWintabInfo.GetMaxPressure();
            _tabletPressure = 1f;

            uint count = 0;
            WintabPacket[] results = _data.GetDataPackets(100, true, ref count);
            for (int i = 0; i < count; i++) {
                int x = results[i].pkX;
                int y = results[i].pkY;
                float pressure = results[i].pkNormalPressure / maxPressure;

                y = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - y - Window.ClientBounds.Y;
                x -= Window.ClientBounds.X;

                Console.WriteLine($"   X: {x} -- Y: {y} ::: {pressure}");
                _tabletXYa = _tabletXYb;
                _tabletXYb = _camera.ScreenToWorld(x, y);
                _tabletPressure = pressure;
                if (pressure > 0) {
                    usedTablet = true;

                    if (_tabletXYa.HasValue && _tabletXYb.HasValue && _tabletXYa.Value != _tabletXYb.Value) {
                        CreateLine(_tabletXYa.Value, _tabletXYb.Value, _radius * _camera.ScreenToWorldScale() * pressure);
                    }
                } else {
                    _tabletPressure = 1f;
                }
            }

            return usedTablet;
        }
        #endif

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
                    var diffY = (InputHelper.NewMouse.Y - _zoomStart.Y) / 100f;
                    SetExpTween(MathHelper.Clamp(_expStart + diffY, _maxExp, _minExp), 0);

                    ShowZoomSidebar();
                } else if (MouseCondition.Scrolled() && !_thickness.Held()) {
                    SetExpTween(MathHelper.Clamp(_targetExp - MouseCondition.ScrollDelta * _expDistance, _maxExp, _minExp));

                    ShowZoomSidebar();
                }
            }

            if (_rotateLeft.Pressed()) {
                SetRotationTween(_rotation.B + MathHelper.PiOver4);
            }
            if (_rotateRight.Pressed()) {
                SetRotationTween(_rotation.B - MathHelper.PiOver4);
            }

            _camera.Z = _camera.ScaleToZ(ExpToScale(_exp.Value), 0f);
            _camera.Rotation = _rotation.Value;

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

            _camera.XY = _xy.Value;
        }
        private static float ScaleToExp(float scale) {
            return -MathF.Log(scale);
        }
        private static float ExpToScale(float exp) {
            return MathF.Exp(-exp);
        }
        private void SaveCam(string key) {
            _savedCams[key] = new DrawingData.Cam {
                X = _camera.X,
                Y = _camera.Y,
                Z = _camera.Z,
                Rotation = _camera.Rotation
            };
        }
        private void LoadCam(string key) {
            if (_savedCams.TryGetValue(key, out DrawingData.Cam? cam)) {
                _savedCams["0"] = new DrawingData.Cam {
                    X = _xy.B.X,
                    Y = _xy.B.Y,
                    Z = _camera.ScaleToZ(ExpToScale(_exp.B), 0f),
                    Rotation = _rotation.B
                };

                SetXYTween(cam.X, cam.Y);
                SetZTween(cam.Z);
                SetRotationTween(cam.Rotation);
                ShowZoomSidebar();
            }
        }
        private void SetXYTween(float targetX, float targetY, long duration = 1200) {
            SetXYTween(new Vector2(targetX, targetY), duration);
        }
        private void SetXYTween(Vector2 target, long duration = 1200) {
            _xy.A = _xy.Value;
            _xy.B = target;
            _xy.StartTime = TweenHelper.TotalMS;
            _xy.Duration = duration;
        }
        private void SetExpTween(float target, long duration = 1200) {
            _targetExp = target;
            _exp.A = _exp.Value;
            _exp.B = _targetExp;
            _exp.StartTime = TweenHelper.TotalMS;
            _exp.Duration = duration;
            ShowZoomSidebar();
        }
        private void SetZTween(float target, long duration = 1200) {
            _targetExp = ScaleToExp(_camera.ZToScale(target, 0f));
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

        private void CreateLine(Vector2 a, Vector2 b, float radius) {
            var c = _isErasing ? TWColor.Transparent : _color;
            Line l = new(_nextId++, a, b, radius, c);

            l.Leaf = _tree.Add(l.AABB, l);
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
                    _tree.Remove(l.Leaf);

                    _redoLines.Push(l);
                }
                _redoGroups.Push(group);
                _nextId = group.First;
                _group = (_nextId, _nextId);
            }
        }
        private void Redo() {
            if (_redoGroups.Count > 0) {
                var group = _redoGroups.Pop();
                while (true) {
                    var l = _redoLines.Pop();
                    l.Leaf = _tree.Add(l.AABB, l);
                    _lines.Add(l.Id, l);
                    _group.Last = l.Id;

                    if (l.Id == group.First) break;
                }
                _undoGroups.Push(group);
                _nextId = group.Last + 1;
                _group = (_nextId, _nextId);
            }
        }
        private void SaveDrawing() {
            DrawingData dd = new() {
                NextId = _nextId,
                BackgroundColor = new DrawingData.Color { R = _bgColor.R, G = _bgColor.G, B = _bgColor.B },
                Lines = _tree.Select(e => new DrawingData.JsonLine {
                    Id = e.Id,
                    A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                    B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                    Radius = e.Radius,
                    Color = e.Color == TWColor.Transparent ? null : new DrawingData.Color { R = e.Color.R, G = e.Color.G, B = e.Color.B }
                }).ToList(),
                UndoGroups = _undoGroups.Select(e => new DrawingData.Group {
                    First = e.First,
                    Last = e.Last
                }).ToList(),
                RedoGroups = _redoGroups.Select(e => new DrawingData.Group {
                    First = e.First,
                    Last = e.Last
                }).ToList(),
                RedoLines = _redoLines.Select(e => new DrawingData.JsonLine {
                    Id = e.Id,
                    A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                    B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                    Radius = e.Radius,
                    Color = new DrawingData.Color { R = e.Color.R, G = e.Color.G, B = e.Color.B }
                }).ToList(),

                Camera = new DrawingData.Cam { X = _camera.X, Y = _camera.Y, Z = _camera.Z, Rotation = _camera.Rotation },

                SavedCams = _savedCams
            };

            SaveJson("Drawing.json", dd, DrawingDataContext.Default.DrawingData);
        }
        private void LoadDrawing() {
            DrawingData dd = EnsureJson("Drawing.json", DrawingDataContext.Default.DrawingData);
            _nextId = dd.NextId;
            _group = (_nextId, _nextId);
            _bgColor = new Color(dd.BackgroundColor.R, dd.BackgroundColor.G, dd.BackgroundColor.B);
            foreach (var e in dd.Lines) {
                Color c = TWColor.Transparent;
                if (e.Color != null) {
                    c = new Color(e.Color.R, e.Color.G, e.Color.B);
                }
                Line l = new(e.Id, new Vector2(e.A.X, e.A.Y), new Vector2(e.B.X, e.B.Y), e.Radius, c);
                l.Leaf = _tree.Add(l.AABB, l);
                _lines.Add(l.Id, l);
            }
            for (int i = dd.UndoGroups.Count - 1; i >= 0; i--) {
                var group = dd.UndoGroups[i];
                _undoGroups.Push((group.First, group.Last));
            }
            for (int i = dd.RedoGroups.Count - 1; i >= 0; i--) {
                var group = dd.RedoGroups[i];
                _redoGroups.Push((group.First, group.Last));
            }
            for (int i = dd.RedoLines.Count - 1; i >= 0; i--) {
                var l = dd.RedoLines[i];
                Color c = TWColor.Transparent;
                if (l.Color != null) {
                    c = new Color(l.Color.R, l.Color.G, l.Color.B);
                }
                _redoLines.Push(new Line(l.Id, new Vector2(l.A.X, l.A.Y), new Vector2(l.B.X, l.B.Y), l.Radius, c));
            }

            SetXYTween(new Vector2(dd.Camera.X, dd.Camera.Y), 0);
            SetExpTween(ScaleToExp(_camera.ZToScale(dd.Camera.Z, 0f)), 0);
            SetRotationTween(dd.Camera.Rotation, 0);

            _savedCams = dd.SavedCams;
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

        private class Line {
            public Line(int id, Vector2 a, Vector2 b, float radius, Color c) {
                Id = id;
                A = a;
                B = b;
                Radius = radius;
                Color = c;
                AABB = ComputeAABB();
            }

            public int Id { get; set; }
            public int Leaf { get; set; }
            public Vector2 A { get; set; }
            public Vector2 B { get; set; }
            public float Radius { get; set; }
            public Color Color { get; set; }

            public RectangleF AABB { get; set; }

            private RectangleF ComputeAABB() {
                float left = MathF.Min(A.X, B.X) - Radius;
                float top = MathF.Min(A.Y, B.Y) - Radius;
                float right = MathF.Max(A.X, B.X) + Radius;
                float bottom = MathF.Max(A.Y, B.Y) + Radius;

                return new RectangleF(left, top, right - left, bottom - top);
            }
        }

        readonly GraphicsDeviceManager _graphics;
        Camera _camera = null!;
        SpriteBatch _s = null!;
        ShapeBatch _sb = null!;
        FontSystem _fontSystem = null!;

        readonly Settings _settings;

        AABBTree<Line> _tree = null!;
        Dictionary<int, Line> _lines = null!;
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
                new MouseCondition(MouseButton.MiddleButton)
            );

        ICondition _toggleDebug = new KeyboardCondition(Keys.F1);
        ICondition _resetFPS = new KeyboardCondition(Keys.F2);

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

        Dictionary<string, DrawingData.Cam> _savedCams = null!;

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
        bool _isDrawing = false;
        bool _oldIsTablet = false;
        bool _isTablet = false;
        Vector2 _start;
        Vector2 _end;
        float _radius = 10f;
        Color _color = TWColor.Gray300;
        Color _bgColor = TWColor.Black;

        ColorPicker _cp = null!;

        Vector2 _mouseWorld;
        Vector2 _dragAnchor = Vector2.Zero;
        float _targetExp = 0f;
        readonly float _expDistance = 0.002f;
        readonly float _maxExp = -4f;
        readonly float _minExp = 4f;

        float _radiusStart;
        Vector2 _thicknessStart;
        float _expStart;
        Vector2 _zoomStart;
        Vector2 _pinCamera;

        float _preservedExp = 0f;
        readonly float _hyperZoomExp = 4f;

        bool _showDebug = false;

        static readonly FloatTween _zoomSidebarStart = new FloatTween(0f, 0.2f, 1000, Easing.QuintOut);
        static readonly ITween<float> _zoomSidebarWait = _zoomSidebarStart.Wait(1000);
        readonly ITween<float> _zoomSidebarTween = _zoomSidebarWait.To(0f, 1000, Easing.QuintOut);

        readonly Vector2Tween _xy = new(Vector2.Zero, Vector2.Zero, 0, Easing.QuintOut);
        readonly FloatTween _exp = new(0f, 0f, 0, Easing.QuintOut);
        readonly FloatTween _rotation = new(0f, 0f, 0, Easing.QuintOut);

        readonly FPSCounter _fps = new();

        #if SDLWINDOWS
        CWintabContext _logContext = null!;
        CWintabData _data = null!;
        bool _tabletIsValid = false;
        Vector2? _tabletXYa = null;
        Vector2? _tabletXYb = null;
        #endif

        float _tabletPressure = 1f;
    }
}
