using Apos.Input;
using Track = Apos.Input.Track;
using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameProject {
    /// <summary>
    /// The Select tool: marquee box selection of whole strokes (touching selects,
    /// Shift extends, Ctrl subtracts, like Blender's grease pencil in stroke mode),
    /// dragging the selection to move it, and Delete/Backspace to delete it.
    /// </summary>
    public partial class GameRoot {
        enum Tool { Draw, Erase, Select }
        enum SelGesture { None, Marquee, Move, Scale }

        private void SetTool(Tool t) {
            if (_tool == Tool.Select && t != Tool.Select) {
                _selGesture = SelGesture.None;
                ClearSelection();
            }
            _tool = t == _tool ? Tool.Draw : t;
        }

        /// <summary>
        /// LMB gestures while the Select tool is active: press inside the selection
        /// bounds drags the selection, press anywhere else drags a marquee. A no-drag
        /// click selects whatever is under the cursor (empty space deselects).
        /// </summary>
        private void UpdateSelect() {
            if (_draw.Pressed()) {
                bool mod = _extendSelect.Held() || _subtractSelect.Held();
                if (!mod && _selBoundsValid && HitHandle(_mouseWorld)) {
                    _selGesture = SelGesture.Scale;
                    _moveOrigin = _mouseWorld;
                    _moveCurrent = _mouseWorld;
                } else if (!mod && _selBoundsValid && SelBounds().Contains(_mouseWorld)) {
                    _selGesture = SelGesture.Move;
                    _moveOrigin = _mouseWorld;
                    _moveCurrent = _mouseWorld;
                } else {
                    _selGesture = SelGesture.Marquee;
                    _marqueeA = _mouseWorld;
                    _marqueeB = _mouseWorld;
                }
            }
            if (_draw.Held()) {
                if (_selGesture == SelGesture.Marquee) {
                    _marqueeB = _mouseWorld;
                } else if (_selGesture == SelGesture.Move || _selGesture == SelGesture.Scale) {
                    _moveCurrent = _mouseWorld;
                }
            }
            if (_draw.Released()) {
                if (_selGesture == SelGesture.Marquee) {
                    FinishMarquee();
                } else if (_selGesture == SelGesture.Move) {
                    if ((_moveCurrent - _moveOrigin).Length() * _camera.Scale < 2.0) {
                        // No real drag: treat as a click-select inside the bounds.
                        _marqueeA = _moveOrigin;
                        _marqueeB = _moveCurrent;
                        FinishMarquee();
                    } else {
                        CommitMove();
                    }
                } else if (_selGesture == SelGesture.Scale) {
                    CommitScale();
                }
                _selGesture = SelGesture.None;
            }
            if (_selGesture == SelGesture.None && _selectedIds.Count > 0
                && !_subtractSelect.Held() && _deleteSelection.Pressed()) {
                DeleteSelection();
            }
        }

        private void FinishMarquee() {
            RectangleD rect = RectangleD.FromBounds(
                Math.Min(_marqueeA.X, _marqueeB.X), Math.Min(_marqueeA.Y, _marqueeB.Y),
                Math.Max(_marqueeA.X, _marqueeB.X), Math.Max(_marqueeA.Y, _marqueeB.Y));
            bool extend = _extendSelect.Held();
            bool subtract = _subtractSelect.Held();

            double scale = _camera.Scale;
            RectangleD rectPx = new(
                (rect.X - _camera.XY.X) * scale, (rect.Y - _camera.XY.Y) * scale,
                rect.Width * scale, rect.Height * scale);
            HashSet<int> hits = [];
            HitTestVisible(rect, (l, a, b, r) => {
                if (MathD.SegmentRectDistance(a, b, rectPx) <= r) {
                    hits.Add(l.StrokeId);
                }
            });

            if (!extend && !subtract) {
                _selectedStrokes.Clear();
            }
            foreach (int sid in hits) {
                if (subtract) {
                    _selectedStrokes.Remove(sid);
                } else {
                    _selectedStrokes.Add(sid);
                }
            }
            RefreshSelection();
        }

        private void CommitMove() {
            Vector2D delta = _moveCurrent - _moveOrigin;
            if (_selectedStrokes.Count == 0 || delta == Vector2D.Zero) return;
            MoveOp op = new() { Ref = _anchor, Delta = delta };
            foreach (int sid in _selectedStrokes) {
                if (!_strokes.TryGetValue(sid, out List<Line>? lines)) continue;
                foreach (Line l in lines) {
                    op.Originals.Add(new LineSnapshot { Line = l, Node = l.Node, A = l.A, B = l.B, Radius = l.Radius });
                }
            }
            if (op.Originals.Count == 0) return;
            ApplyMove(op);
            _undoOps.Push(op);
            _redoOps.Clear();
            RebuildCoverage();
            RefreshSelection();
        }

        /// <summary>Uniform scale about the selection bounds center: the factor is the
        /// ratio of the cursor's current and initial distances to the center.</summary>
        private double ScaleFactor() {
            Vector2D c = (_selBoundsMin + _selBoundsMax) / 2.0;
            double d0 = (_moveOrigin - c).Length();
            double d1 = (_moveCurrent - c).Length();
            if (d0 <= 0.0) return 1.0;
            return Math.Clamp(d1 / d0, 1e-6, 1e6);
        }

        private bool HitHandle(Vector2D p) {
            Vector2 pv = _camera.WorldToView(p);
            Span<Vector2D> corners = [
                _selBoundsMin,
                new Vector2D(_selBoundsMax.X, _selBoundsMin.Y),
                _selBoundsMax,
                new Vector2D(_selBoundsMin.X, _selBoundsMax.Y)
            ];
            foreach (Vector2D corner in corners) {
                if (Vector2.Distance(pv, _camera.WorldToView(corner)) <= HandleGrabPx) return true;
            }
            return false;
        }

        private void CommitScale() {
            double factor = ScaleFactor();
            // A sloppy click on a handle is not a scale.
            if (_selectedStrokes.Count == 0 || Math.Abs(factor - 1.0) < 0.005) return;
            ScaleOp op = new() { Ref = _anchor, Center = (_selBoundsMin + _selBoundsMax) / 2.0, Factor = factor };
            foreach (int sid in _selectedStrokes) {
                if (!_strokes.TryGetValue(sid, out List<Line>? lines)) continue;
                foreach (Line l in lines) {
                    op.Originals.Add(new LineSnapshot { Line = l, Node = l.Node, A = l.A, B = l.B, Radius = l.Radius });
                }
            }
            if (op.Originals.Count == 0) return;
            ApplyScale(op);
            _undoOps.Push(op);
            _redoOps.Clear();
            RebuildCoverage();
            RefreshSelection();
        }

        private void DeleteSelection() {
            List<Line> doomed = [];
            foreach (int sid in _selectedStrokes) {
                if (_strokes.TryGetValue(sid, out List<Line>? lines)) {
                    doomed.AddRange(lines);
                }
            }
            ClearSelection();
            if (doomed.Count == 0) return;
            DeleteOp op = new();
            foreach (Line l in doomed) {
                DetachLine(l);
                op.Lines.Add(l);
            }
            _undoOps.Push(op);
            _redoOps.Clear();
            RebuildCoverage();
        }

        private void ClearSelection() {
            _selectedStrokes.Clear();
            _selectedIds.Clear();
            _selBoundsValid = false;
        }

        /// <summary>Rebuilds the per-line id set and the bounds after any change to
        /// the selection or to the selected strokes themselves.</summary>
        private void RefreshSelection() {
            _selectedIds.Clear();
            _selectedStrokes.RemoveWhere(sid => !_strokes.ContainsKey(sid));
            foreach (int sid in _selectedStrokes) {
                foreach (Line l in _strokes[sid]) {
                    _selectedIds.Add(l.Id);
                }
            }
            RecomputeSelectionBounds();
        }

        private void RecomputeSelectionBounds() {
            _selBoundsValid = false;
            foreach (int sid in _selectedStrokes) {
                if (!_strokes.TryGetValue(sid, out List<Line>? lines)) continue;
                foreach (Line l in lines) {
                    // The frame-to-frame transform is a positive scale plus a
                    // translation, so corner order is preserved.
                    Vector2D mn = TransformPoint(l.Node, _anchor, new Vector2D(l.AABB.Left, l.AABB.Top));
                    Vector2D mx = TransformPoint(l.Node, _anchor, new Vector2D(l.AABB.Right, l.AABB.Bottom));
                    if (!_selBoundsValid) {
                        _selBoundsMin = mn;
                        _selBoundsMax = mx;
                        _selBoundsValid = true;
                    } else {
                        _selBoundsMin = Vector2D.Min(_selBoundsMin, mn);
                        _selBoundsMax = Vector2D.Max(_selBoundsMax, mx);
                    }
                }
            }
        }

        private RectangleD SelBounds() {
            return RectangleD.FromBounds(_selBoundsMin.X, _selBoundsMin.Y, _selBoundsMax.X, _selBoundsMax.Y);
        }

        /// <summary>Marquee or selection-bounds rectangle with its corner scale
        /// handles, drawn inside the view batch so they follow camera rotation.</summary>
        private void DrawSelectOverlay(Vector2 moveDelta) {
            if (_tool != Tool.Select) return;
            if (_selGesture == SelGesture.Marquee) {
                Vector2D mn = Vector2D.Min(_marqueeA, _marqueeB);
                Vector2D mx = Vector2D.Max(_marqueeA, _marqueeB);
                Vector2 a = _camera.WorldToView(mn);
                Vector2 b = _camera.WorldToView(mx);
                DrawViewRect(a, b, TWColor.Gray200, false);
                return;
            }
            if (!_selBoundsValid) return;
            Vector2 tl = _camera.WorldToView(_selBoundsMin);
            Vector2 br = _camera.WorldToView(_selBoundsMax);
            if (_selGesture == SelGesture.Move) {
                tl += moveDelta;
                br += moveDelta;
            } else if (_selGesture == SelGesture.Scale) {
                float f = (float)ScaleFactor();
                Vector2 c = _camera.WorldToView((_selBoundsMin + _selBoundsMax) / 2.0);
                tl = c + (tl - c) * f;
                br = c + (br - c) * f;
            }
            DrawViewRect(tl, br, SelectAccent, true);
        }

        private void DrawViewRect(Vector2 tl, Vector2 br, Color c, bool handles) {
            Vector2 tr = new(br.X, tl.Y);
            Vector2 bl = new(tl.X, br.Y);
            _sb.FillLine(tl, tr, 1f, c);
            _sb.FillLine(tr, br, 1f, c);
            _sb.FillLine(br, bl, 1f, c);
            _sb.FillLine(bl, tl, 1f, c);
            if (handles) {
                _sb.FillCircle(tl, HandleDrawPx, c);
                _sb.FillCircle(tr, HandleDrawPx, c);
                _sb.FillCircle(br, HandleDrawPx, c);
                _sb.FillCircle(bl, HandleDrawPx, c);
            }
        }

        Tool _tool = Tool.Draw;
        SelGesture _selGesture = SelGesture.None;
        readonly HashSet<int> _selectedStrokes = [];
        readonly HashSet<int> _selectedIds = [];
        // All of these are in current anchor units and MUST be transformed by
        // AscendCamera/DescendCamera or they teleport on band crossings.
        Vector2D _marqueeA;
        Vector2D _marqueeB;
        Vector2D _moveOrigin;
        Vector2D _moveCurrent;
        Vector2D _selBoundsMin;
        Vector2D _selBoundsMax;
        bool _selBoundsValid = false;

        static readonly Color SelectAccent = TWColor.Sky400;
        const float HandleDrawPx = 4f;
        const float HandleGrabPx = 10f;

        ICondition _toggleSelect = new KeyboardCondition(Keys.B);
        ICondition _extendSelect =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftShift),
                new KeyboardCondition(Keys.RightShift)
            );
        ICondition _subtractSelect =
            new AnyCondition(
                new KeyboardCondition(Keys.LeftControl),
                new KeyboardCondition(Keys.RightControl)
            );
        ICondition _deleteSelection =
            new AnyCondition(
                new Track.KeyboardCondition(Keys.Back),
                new Track.KeyboardCondition(Keys.Delete)
            );
    }
}
