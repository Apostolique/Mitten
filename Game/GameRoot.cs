using Apos.Camera;
using Apos.Input;
using Track = Apos.Input.Track;
using Apos.Shapes;
using Apos.Spatial;
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

// TODO:
//       Add tablet pressure sensitivity.
//       Rotation controls like Krita.

namespace GameProject {
    public class GameRoot : Game {
        public GameRoot() {
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Content.RootDirectory = "Content";

            _settings = EnsureJson<Settings>("Settings.json");
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            IsFixedTimeStep = _settings.IsFixedTimeStep;
            _graphics.SynchronizeWithVerticalRetrace = _settings.IsVSync;

            _settings.IsFullscreen = _settings.IsFullscreen || _settings.IsBorderless;

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

            _lines = new Dictionary<int, Line>();
            _tree = new AABBTree<Line>();
            _undoGroups = new Stack<(int, int)>();
            _redoGroups = new Stack<(int, int)>();
            _redoLines = new Stack<Line>();

            _camera = new Camera(new DefaultViewport(GraphicsDevice, Window));

            _cp = new ColorPicker(GraphicsDevice, Content);

            LoadDrawing();
        }

        protected override void UnloadContent() {
            SaveDrawing();

            if (!_settings.IsFullscreen) {
                SaveWindow();
            }

            SaveJson<Settings>("Settings.json", _settings);

            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

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
                UpdateCamera(gameTime);

                if (!_isDrawing && _thickness.Held()) {
                    if (_thickness.Pressed()) {
                        _radiusStart = _radius;
                        _thicknessStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                    }
                    var diffX = (InputHelper.NewMouse.X - _thicknessStart.X) / 2f;
                    _radius = MathHelper.Clamp(_radiusStart + diffX, 0.5f, 1000f);
                } else {
                    if (_draw.Pressed()) {
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
                _sb.FillLine(_start, _end, _radius * _camera.ScreenToWorldScale(), fgColor);
            }
            if (_thickness.Held()) {
                _sb.FillCircle(_camera.ScreenToWorld(_thicknessStart), _radius * _camera.ScreenToWorldScale(), fgColor);
                if (_isErasing) {
                    _sb.BorderCircle(_camera.ScreenToWorld(_thicknessStart), _radius * _camera.ScreenToWorldScale(), TWColor.Black, 6f);
                    _sb.BorderCircle(_camera.ScreenToWorld(_thicknessStart), (_radius - 2f) * _camera.ScreenToWorldScale(), TWColor.White, 2f);
                }
            } else {
                _sb.FillCircle(_mouseWorld, _radius * _camera.ScreenToWorldScale(), fgColor);
                if (_isErasing) {
                    _sb.BorderCircle(_mouseWorld, _radius * _camera.ScreenToWorldScale(), TWColor.Black, 6f);
                    _sb.BorderCircle(_mouseWorld, (_radius - 2f) * _camera.ScreenToWorldScale(), TWColor.White, 2f);
                }
            }
            _sb.End();

            _sb.Begin();
            var camExp = ScaleToExp(_camera.ZToScale(_camera.Z, 0f));
            if (_showZoomUntil > gameTime.TotalGameTime.Ticks) {
                var length = _minExp - _maxExp;
                var percent = (camExp - _maxExp) / length;
                _sb.DrawLine(new Vector2(0, GraphicsDevice.Viewport.Height), new Vector2(0, GraphicsDevice.Viewport.Height * percent), 10f, TWColor.White * 0.2f, TWColor.Black, 2f);
            }
            _sb.End();

            if (_pickColor.Held()) {
                _cp.Draw();
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

        private void UpdateCamera(GameTime gameTime) {
            if (_hyperZoom.Pressed()) {
                _preservedExp = _targetExp;
                _targetExp = _preservedExp + _hyperZoomExp;
            }
            if (_hyperZoom.Held()) {
                _targetExp = _preservedExp + _hyperZoomExp;

                _showZoomUntil = gameTime.TotalGameTime.Ticks + TimeSpan.TicksPerSecond;
            } else if (_hyperZoom.Released()) {
                _targetExp = _preservedExp;
            } else {
                if (_dragZoom.Held()) {
                    if (_dragZoom.Pressed()) {
                        _expStart = _targetExp;
                        _zoomStart = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                        _dragAnchor = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                        _pinCamera = new Vector2(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
                    }
                    var diffY = (InputHelper.NewMouse.Y - _zoomStart.Y) / 100f;
                    _targetExp = MathHelper.Clamp(_expStart + diffY, _maxExp, _minExp);
                    _camera.Z = _camera.ScaleToZ(ExpToScale(_targetExp), 0f);

                    _showZoomUntil = gameTime.TotalGameTime.Ticks + TimeSpan.TicksPerSecond;
                } else if (MouseCondition.Scrolled() && !_thickness.Held()) {
                    _targetExp = MathHelper.Clamp(_targetExp - MouseCondition.ScrollDelta * _expDistance, _maxExp, _minExp);

                    _showZoomUntil = gameTime.TotalGameTime.Ticks + TimeSpan.TicksPerSecond;
                }
            }

            if (_rotateLeft.Pressed()) {
                _targetRotation += MathHelper.PiOver4;
            }
            if (_rotateRight.Pressed()) {
                _targetRotation -= MathHelper.PiOver4;
            }

            _camera.Z = _camera.ScaleToZ(ExpToScale(Interpolate(ScaleToExp(_camera.ZToScale(_camera.Z, 0f)), _targetExp, _speed, _snapDistance)), 0f);
            _camera.Rotation = Interpolate(_camera.Rotation, _targetRotation, _speed, _snapDistance);

            if (_dragZoom.Held()) {
                _camera.XY += _dragAnchor - _camera.ScreenToWorld(_pinCamera);
                _mouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);
            } else {
                _mouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

                if (_dragCamera.Pressed()) {
                    _dragAnchor = _mouseWorld;
                }
                if (_dragCamera.Held()) {
                    _camera.XY += _dragAnchor - _mouseWorld;
                    _mouseWorld = _dragAnchor;
                }
            }
        }
        private float Interpolate(float from, float target, float speed, float snapNear) {
            float result = MathHelper.Lerp(from, target, speed);

            if (from < target) {
                result = MathHelper.Clamp(result, from, target);
            } else {
                result = MathHelper.Clamp(result, target, from);
            }

            if (MathF.Abs(target - result) < snapNear) {
                return target;
            } else {
                return result;
            }
        }
        private float ScaleToExp(float scale) {
            return -MathF.Log(scale);
        }
        private float ExpToScale(float exp) {
            return MathF.Exp(-exp);
        }

        private void CreateLine(Vector2 a, Vector2 b, float radius) {
            var c = _isErasing ? TWColor.Transparent : _color;
            Line l = new Line(_nextId++, a, b, radius, c);

            l.Leaf = _tree.Add(l.AABB, l);
            _lines.Add(l.Id, l);
            _group.Last = l.Id;
        }
        private void CreateGroup() {
            _undoGroups.Push(_group);
            _group = (_nextId, _nextId);
            _redoGroups.Clear();
            _redoLines.Clear();
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
            DrawingData dd = new DrawingData();
            dd.NextId = _nextId;
            dd.BackgroundColor = new DrawingData.Color { R = _bgColor.R, G = _bgColor.G, B = _bgColor.B };
            dd.Lines = _tree.Select(e => new DrawingData.JsonLine {
                Id = e.Id,
                A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                Radius = e.Radius,
                Color = e.Color == TWColor.Transparent ? null : new DrawingData.Color { R = e.Color.R, G = e.Color.G, B = e.Color.B }
            }).ToList();
            dd.UndoGroups = _undoGroups.Select(e => new DrawingData.Group {
                First = e.First,
                Last = e.Last
            }).ToList();
            dd.RedoGroups = _redoGroups.Select(e => new DrawingData.Group {
                First = e.First,
                Last = e.Last
            }).ToList();
            dd.RedoLines = _redoLines.Select(e => new DrawingData.JsonLine {
                Id = e.Id,
                A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                Radius = e.Radius,
                Color = new DrawingData.Color { R = e.Color.R, G = e.Color.G, B = e.Color.B }
            }).ToList();

            dd.Camera = new DrawingData.Cam { X = _camera.X, Y = _camera.Y, Z = _camera.Z, Rotation = _camera.Rotation };

            SaveJson<DrawingData>("Drawing.json", dd);
        }
        private void LoadDrawing() {
            DrawingData dd = EnsureJson<DrawingData>("Drawing.json");
            _nextId = dd.NextId;
            _group = (_nextId, _nextId);
            _bgColor = new Color(dd.BackgroundColor.R, dd.BackgroundColor.G, dd.BackgroundColor.B);
            foreach (var e in dd.Lines) {
                Color c = TWColor.Transparent;
                if (e.Color != null) {
                    c = new Color(e.Color.R, e.Color.G, e.Color.B);
                }
                Line l = new Line(e.Id, new Vector2(e.A.X, e.A.Y), new Vector2(e.B.X, e.B.Y), e.Radius, c);
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
                _redoLines.Push(new Line(l.Id, new Vector2(l.A.X, l.A.Y), new Vector2(l.B.X, l.B.Y), l.Radius, new Color(l.Color.R, l.Color.G, l.Color.B)));
            }

            _camera.XY = new Vector2(dd.Camera.X, dd.Camera.Y);
            _camera.Z = dd.Camera.Z;
            _camera.Rotation = dd.Camera.Rotation;
            _targetExp = ScaleToExp(_camera.ZToScale(_camera.Z, 0f));
            _targetRotation = _camera.Rotation;
        }

        public static string GetPath(string name) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, name);
        public static void SaveJson<T>(string name, T json) {
            string jsonPath = GetPath(name);
            string jsonString = JsonSerializer.Serialize(json, _options);
            File.WriteAllText(jsonPath, jsonString);
        }
        public static T EnsureJson<T>(string name) where T : new() {
            T json;
            string jsonPath = GetPath(name);

            if (File.Exists(jsonPath)) {
                json = JsonSerializer.Deserialize<T>(File.ReadAllText(jsonPath), _options)!;
            } else {
                json = new T();
                string jsonString = JsonSerializer.Serialize(json, _options);
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

        GraphicsDeviceManager _graphics;
        Camera _camera = null!;
        SpriteBatch _s = null!;
        ShapeBatch _sb = null!;
        FontSystem _fontSystem = null!;

        Settings _settings;

        AABBTree<Line> _tree = null!;
        Dictionary<int, Line> _lines = null!;
        (int First, int Last) _group = (0, 0);
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

        bool _isErasing = false;
        bool _isDrawing = false;
        Vector2 _start;
        Vector2 _end;
        float _radius = 10f;
        Color _color = TWColor.Gray300;
        Color _bgColor = TWColor.Black;

        ColorPicker _cp = null!;

        Vector2 _mouseWorld;
        Vector2 _dragAnchor = Vector2.Zero;
        float _targetExp = 0f;
        float _targetRotation = 0f;
        float _speed = 0.08f;
        float _snapDistance = 0.001f;
        float _expDistance = 0.002f;
        float _maxExp = -4f;
        float _minExp = 4f;

        float _radiusStart;
        Vector2 _thicknessStart;
        float _expStart;
        Vector2 _zoomStart;
        Vector2 _pinCamera;

        float _preservedExp = 0f;
        float _hyperZoomExp = 4f;

        bool _showDebug = false;

        long _showZoomUntil = 0;

        FPSCounter _fps = new FPSCounter();

        private static JsonSerializerOptions _options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
    }
}
