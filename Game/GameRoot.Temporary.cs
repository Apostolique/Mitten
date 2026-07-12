using Apos.Input;
using Apos.Tweens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameProject {
    /// <summary>
    /// Temporary mode (T): strokes draw normally but live in a transient overlay —
    /// they never enter the frame trees, the undo history, or the save file. Each
    /// stroke erases itself from its starting point on an absolute clock: the erase
    /// front sets off Settings.TempDelaySeconds after pen-down and retraces the
    /// stroke at the speed it was drawn (scaled by Settings.TempDecaySpeed), so the
    /// oldest ink always disappears first — a long stroke's tail starts fading
    /// while it is still being drawn. Meant for pointing at things and quick
    /// throwaway doodles during a presentation.
    /// </summary>
    public partial class GameRoot {
        private sealed class TempSeg {
            public Vector2D A;
            public Vector2D B;
            public double Radius;
            // Interval on the stroke's drawing timeline, in ms since pen down.
            public double U0;
            public double U1;
        }
        private sealed class TempStroke {
            public List<TempSeg> Segs = [];
            public Color Color;
            public double StartMs;          // pen-down: the erase clock runs from here
            public double LastMs;           // while drawing: when the previous segment ended
            public double TotalU;           // drawing duration accumulated so far
            public bool Released;
        }

        private void CreateTempLine(Vector2D a, Vector2D b, double radius) {
            double now = TweenHelper.TotalMS;
            if (_tempActive == null) {
                _tempActive = new TempStroke {
                    Color = _tool == Tool.Erase ? TWColor.Transparent : _color,
                    StartMs = now,
                    LastMs = now
                };
                _tempStrokes.Add(_tempActive);
            }
            TempStroke s = _tempActive;
            double u1 = s.TotalU + (now - s.LastMs);
            s.Segs.Add(new TempSeg { A = a, B = b, Radius = radius, U0 = s.TotalU, U1 = u1 });
            s.TotalU = u1;
            s.LastMs = now;
        }

        private void CommitTempStroke() {
            if (_tempActive == null) return;
            TempStroke s = _tempActive;
            _tempActive = null;
            // Instant strokes (a click dot, a shift-line placed on release) have no
            // drawing duration to replay: give them a nominal one so they still
            // erase visibly instead of blinking out.
            if (s.TotalU <= 0.0) {
                double step = MinEraseMs / s.Segs.Count;
                for (int i = 0; i < s.Segs.Count; i++) {
                    s.Segs[i].U0 = i * step;
                    s.Segs[i].U1 = (i + 1) * step;
                }
                s.TotalU = MinEraseMs;
            }
            s.Released = true;
        }

        /// <summary>
        /// How much of the stroke's drawing timeline has been erased. The clock is
        /// absolute from pen-down: 0 while the stroke's start still lingers, TotalU
        /// or more once the erase front has swept the whole stroke.
        /// </summary>
        private double TempCursor(TempStroke s, double now) {
            double elapsed = now - s.StartMs - _settings.TempDelaySeconds * 1000.0;
            if (elapsed <= 0.0) return 0.0;
            return elapsed * Math.Max(_settings.TempDecaySpeed, 0.01);
        }

        private void UpdateTempStrokes() {
            if (_tempStrokes.Count == 0) return;
            double now = TweenHelper.TotalMS;
            for (int i = _tempStrokes.Count - 1; i >= 0; i--) {
                TempStroke s = _tempStrokes[i];
                // An active stroke stays even when the front has caught up to the
                // pen: segments keep being laid ahead of it.
                if (s.Released && TempCursor(s, now) >= s.TotalU) {
                    _tempStrokes.RemoveAt(i);
                }
            }
        }

        private void DrawTempStrokes() {
            if (_tempStrokes.Count == 0) return;
            double now = TweenHelper.TotalMS;
            foreach (TempStroke s in _tempStrokes) {
                double cursor = TempCursor(s, now);
                Color c = s.Color == TWColor.Transparent ? _bgColor : s.Color;
                foreach (TempSeg seg in s.Segs) {
                    // Strict comparison so zero-duration segments (the first one of a
                    // stroke) stay visible until the erase front actually passes them.
                    if (seg.U1 < cursor) continue;
                    Vector2D a = seg.A;
                    if (seg.U0 < cursor) {
                        // The erase front is inside this segment: trim its head.
                        double t = (cursor - seg.U0) / (seg.U1 - seg.U0);
                        a = seg.A + (seg.B - seg.A) * t;
                    }
                    Vector2 av = _camera.WorldToView(a);
                    Vector2 bv = _camera.WorldToView(seg.B);
                    float r = (float)(seg.Radius * _camera.Scale);
                    if (av == bv) {
                        _sb.FillCircle(av, r, c);
                    } else {
                        _sb.FillLine(av, bv, r, c);
                    }
                }
            }
        }

        // Temp segments are in current anchor units like every other world-space
        // gesture field, so band crossings must transform them too.
        private void AscendTempStrokes(Vector2D idx) {
            foreach (TempStroke s in _tempStrokes) {
                foreach (TempSeg seg in s.Segs) {
                    seg.A = seg.A / Frame.K + idx;
                    seg.B = seg.B / Frame.K + idx;
                    seg.Radius /= Frame.K;
                }
            }
        }
        private void DescendTempStrokes(Vector2D idx) {
            foreach (TempStroke s in _tempStrokes) {
                foreach (TempSeg seg in s.Segs) {
                    seg.A = (seg.A - idx) * Frame.K;
                    seg.B = (seg.B - idx) * Frame.K;
                    seg.Radius *= Frame.K;
                }
            }
        }
        private void ClearTempStrokes() {
            _tempStrokes.Clear();
            _tempActive = null;
        }

        bool _tempMode = false;
        TempStroke? _tempActive;
        readonly List<TempStroke> _tempStrokes = [];
        const double MinEraseMs = 250.0;

        static readonly Color TempAccent = TWColor.Amber400;

        ICondition _toggleTemp = new KeyboardCondition(Keys.T);
    }
}
