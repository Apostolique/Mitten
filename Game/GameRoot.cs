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

// TODO: Add way to pick line color.

namespace GameProject {
    public class GameRoot : Game {
        public GameRoot() {
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

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

            LoadDrawing();
        }

        protected override void UnloadContent() {
            SaveDrawing();

            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (_quit.Pressed())
                Exit();

            if (_toggleDebug.Pressed()) _showDebug = !_showDebug;
            if (_resetFPS.Pressed()) _fps.DroppedFrames = 0;
            _fps.Update(gameTime);

            UpdateCamera();

            if (_thickness.Held() && MouseCondition.Scrolled()) {
                _radius = MathHelper.Clamp(ExpToScale(ScaleToExp(_radius) - MouseCondition.ScrollDelta * 0.001f), 1f, 400f);
            }

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

            if (!_isDrawing) {
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
            GraphicsDevice.Clear(TWColor.Black);

            _sb.Begin(_camera.View);
            int inView = 0;
            foreach (Line l in _tree.Query(_camera.ViewRect).OrderBy(e => e.Id)) {
                _sb.FillLine(l.A, l.B, l.Radius, TWColor.Gray300);
                inView++;
            }
            if (_isDrawing) {
                _sb.FillLine(_start, _end, _radius * _camera.ScreenToWorldScale(), TWColor.Gray300);
            }
            _sb.FillCircle(_mouseWorld, _radius * _camera.ScreenToWorldScale(), TWColor.Gray300);
            _sb.End();

            if (_showDebug) {
                var font = _fontSystem.GetFont(24);
                _s.Begin();
                _s.DrawString(font, $"fps: {_fps.FramesPerSecond} - Dropped Frames: {_fps.DroppedFrames} - Draw ms: {_fps.TimePerFrame} - Update ms: {_fps.TimePerUpdate}", new Vector2(10, 10), TWColor.White);
                _s.DrawString(font, $"In view: {inView} -- Total: {_lines.Count} -- {_camera.ScreenToWorldScale()}", new Vector2(10, GraphicsDevice.Viewport.Height - 24), TWColor.White);
                _s.End();
            }

            base.Draw(gameTime);
        }

        public void UpdateCamera() {
            if (MouseCondition.Scrolled() && !_thickness.Held()) {
                _targetExp = MathHelper.Clamp(_targetExp - MouseCondition.ScrollDelta * _expDistance, _maxExp, _minExp);
            }

            if (_rotateLeft.Pressed()) {
                _targetRotation += MathHelper.PiOver4;
            }
            if (_rotateRight.Pressed()) {
                _targetRotation -= MathHelper.PiOver4;
            }

            _mouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

            if (_dragCamera.Pressed()) {
                _dragAnchor = _mouseWorld;
                _isDragging = true;
            }
            if (_isDragging && _dragCamera.HeldOnly()) {
                _camera.XY += _dragAnchor - _mouseWorld;
                _mouseWorld = _dragAnchor;
            }
            if (_isDragging && _dragCamera.Released()) {
                _isDragging = false;
            }

            _camera.Z = _camera.ScaleToZ(ExpToScale(Interpolate(ScaleToExp(_camera.ZToScale(_camera.Z, 0f)), _targetExp, _speed, _snapDistance)), 0f);
            _camera.Rotation = Interpolate(_camera.Rotation, _targetRotation, _speed, _snapDistance);
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
            Line l = new Line(_nextId++, a, b, radius);

            l.Leaf = _tree.Add(l.AABB, l);
            _lines.Add(l.Id, l);
            _group.Last = l.Id;
        }
        private void CreateGroup() {
            _undoGroups.Push(_group);
            _group = (_nextId, _nextId);
            _redoGroups.Clear();
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
            dd.Lines = _tree.Select(e => new DrawingData.JsonLine {
                Id = e.Id,
                A = new DrawingData.XY { X = e.A.X, Y = e.A.Y },
                B = new DrawingData.XY { X = e.B.X, Y = e.B.Y },
                Radius = e.Radius
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
                Radius = e.Radius
            }).ToList();

            SaveJson<DrawingData>("Drawing.json", dd);
        }
        private void LoadDrawing() {
            DrawingData dd = EnsureJson<DrawingData>("Drawing.json");
            _nextId = dd.NextId;
            _group = (_nextId, _nextId);
            foreach (var e in dd.Lines) {
                Line l = new Line(e.Id, new Vector2(e.A.X, e.A.Y), new Vector2(e.B.X, e.B.Y), e.Radius);
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
                _redoLines.Push(new Line(l.Id, new Vector2(l.A.X, l.A.Y), new Vector2(l.B.X, l.B.Y), l.Radius));
            }
        }

        public static string GetPath(string name) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
        public static void SaveJson<T>(string name, T json) {
            string jsonPath = GetPath(name);
            string jsonString = JsonSerializer.Serialize(json, _options);
            File.WriteAllText(jsonPath, jsonString);
        }
        public static T EnsureJson<T>(string name) where T : new() {
            T json;
            string jsonPath = GetPath(name);

            if (File.Exists(jsonPath)) {
                json = JsonSerializer.Deserialize<T>(File.ReadAllText(jsonPath), _options);
            } else {
                json = new T();
                string jsonString = JsonSerializer.Serialize(json, _options);
                File.WriteAllText(jsonPath, jsonString);
            }

            return json;
        }

        private class Line {
            public Line(int id, Vector2 a, Vector2 b, float radius) {
                Id = id;
                A = a;
                B = b;
                Radius = radius;
                AABB = ComputeAABB();
            }

            public int Id { get; set; }
            public int Leaf { get; set; }
            public Vector2 A { get; set; }
            public Vector2 B { get; set; }
            public float Radius { get; set; }

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
        Camera _camera;
        SpriteBatch _s;
        ShapeBatch _sb;
        FontSystem _fontSystem;

        AABBTree<Line> _tree;
        Dictionary<int, Line> _lines;
        (int First, int Last) _group = (0, 0);
        Stack<(int First, int Last)> _undoGroups;
        Stack<(int First, int Last)> _redoGroups;
        Stack<Line> _redoLines;

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
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );
        ICondition _rotateLeft = new KeyboardCondition(Keys.OemComma);
        ICondition _rotateRight = new KeyboardCondition(Keys.OemPeriod);

        ICondition _dragCamera = new MouseCondition(MouseButton.MiddleButton);

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

        bool _isDrawing = false;
        Vector2 _start;
        Vector2 _end;
        float _radius = 10f;

        Vector2 _mouseWorld;
        Vector2 _dragAnchor = Vector2.Zero;
        bool _isDragging = false;
        float _targetExp = 0f;
        float _targetRotation = 0f;
        float _speed = 0.08f;
        float _snapDistance = 0.001f;
        float _expDistance = 0.002f;
        float _maxExp = -4f;
        float _minExp = 4f;

        bool _showDebug = false;

        FPSCounter _fps = new FPSCounter();

        private static JsonSerializerOptions _options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
    }
}
