using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameProject {
    /// <summary>
    /// Tracks strokes anchored so far above the camera that their frames can no longer
    /// be tree-queried (the view rect is not representable in their units). Each such
    /// stroke near the camera is reduced to its locally visible boundary: a half-plane
    /// (the stroke's edge is locally a straight line at these zoom ratios) or a full
    /// cover. Entries are seeded once, when their frame crosses out of tree-query
    /// range, and afterwards only transformed by the exact camera rebases, so they are
    /// stable frame to frame and continuous across re-anchoring.
    ///
    /// Accepted precision trade: deliberately zooming onto a stroke's edge for L
    /// levels past the seed accumulates ~2^(16L-25) px of absolute edge drift. The
    /// drift is deterministic and continuous (the same stored values, exactly
    /// transformed), so the edge stays straight and jitter-free — there is nothing to
    /// observe it against. Zooming into a stroke's interior turns it into a full cover
    /// within a couple of levels, which involves no geometry at all.
    /// </summary>
    public class CoverageStack {
        private class Entry {
            public int Id;
            public Color Color;
            public Frame Source = null!;
            public bool FullCover;
            // Unit normal from the stroke's centerline toward the seeded camera side,
            // and the edge line offset in current anchor units: edge = { p : N·p = D }.
            // The camera side is inside the stroke when N·cam < D.
            public Vector2D N;
            public double D;
        }

        // How far around the camera strokes get seeded, in pixels. Lateral hops
        // re-seed after at most 2^24 px of pan, so panning can never outrun this.
        private const double SeedReachPx = 1e9;
        // Near this magnitude doubles approach overflow; freeze the entry into a
        // boolean. Re-approaching such an edge visually takes 50+ levels of zoom-out
        // and the frame re-enters tree-query range long before that.
        private const double FreezeD = 1e250;

        private readonly List<Entry> _entries = [];

        public int Count => _entries.Count;

        public void Clear() => _entries.Clear();

        /// <summary>
        /// Seeds entries from an ancestor's strokes around the camera. camXY and scale
        /// are in anchor units; source must be an ancestor of anchor.
        /// </summary>
        public void SeedFrom(Frame source, Frame anchor, Vector2D camXY, double scale) {
            if (source.Tree.Count == 0) return;

            // Express the camera in the source frame, remembering the descent path.
            List<Vector2D> path = [];
            Frame f = anchor;
            Vector2D cam = camXY;
            double ppu = scale;
            while (f != source) {
                if (f.Parent == null) return;
                path.Add(f.IndexOffset);
                cam = cam / Frame.K + f.IndexOffset;
                ppu *= Frame.K;
                f = f.Parent;
            }

            double reach = SeedReachPx / ppu;
            RectangleD rect = new(cam.X - reach, cam.Y - reach, reach * 2.0, reach * 2.0);
            foreach (Line l in source.Tree.Query(rect)) {
                // Closest point on the stroke's centerline to the camera.
                Vector2D ab = l.B - l.A;
                double len2 = ab.LengthSquared();
                double t = len2 > 0.0 ? Math.Clamp(Vector2D.Dot(cam - l.A, ab) / len2, 0.0, 1.0) : 0.0;
                Vector2D c = l.A + ab * t;
                Vector2D delta = cam - c;
                double dist = delta.Length();

                Entry e = new() { Id = l.Id, Color = l.Color, Source = source };
                if (dist * ppu < 0.001) {
                    // Camera is on the centerline: deep inside, no meaningful normal.
                    e.FullCover = true;
                } else {
                    Vector2D n = delta / dist;
                    double d = Vector2D.Dot(n, c) + l.Radius;
                    // Bring the edge into anchor units through the exact descent path.
                    for (int i = path.Count - 1; i >= 0; i--) {
                        d = (d - Vector2D.Dot(n, path[i])) * Frame.K;
                    }
                    e.N = n;
                    e.D = d;
                }
                _entries.Add(e);
            }
        }

        public void OnDescend(Vector2D idx) {
            foreach (Entry e in _entries) {
                if (!e.FullCover) {
                    e.D = (e.D - Vector2D.Dot(e.N, idx)) * Frame.K;
                }
            }
        }
        /// <summary>requeryable is the frame that re-entered tree-query range: its
        /// entries get dropped, the trees take over again.</summary>
        public void OnAscend(Vector2D idx, Frame? requeryable) {
            for (int i = _entries.Count - 1; i >= 0; i--) {
                Entry e = _entries[i];
                if (e.Source == requeryable) {
                    _entries.RemoveAt(i);
                } else if (!e.FullCover) {
                    e.D = e.D / Frame.K + Vector2D.Dot(e.N, idx);
                }
            }
        }

        /// <summary>
        /// Classifies entries against the current camera and emits drawables (in
        /// camera-relative pixels). Returns the highest stroke id that fully covers the
        /// screen, or -1: everything below it in paint order is invisible.
        /// </summary>
        public int Collect(List<Drawable> drawables, Vector2D camXY, double scale, float screenRadius) {
            int fullCoverId = -1;
            float fill = screenRadius * 2f;
            for (int i = _entries.Count - 1; i >= 0; i--) {
                Entry e = _entries[i];
                if (!e.FullCover && Math.Abs(e.D) > FreezeD) {
                    if (e.D - Vector2D.Dot(e.N, camXY) > 0.0) {
                        e.FullCover = true;
                    } else {
                        _entries.RemoveAt(i);
                        continue;
                    }
                }

                if (e.FullCover) {
                    drawables.Add(new Drawable(e.Id, Vector2.Zero, Vector2.Zero, fill, e.Color));
                    if (e.Id > fullCoverId) fullCoverId = e.Id;
                    continue;
                }

                double dPx = (e.D - Vector2D.Dot(e.N, camXY)) * scale;
                if (dPx > fill) {
                    // The edge is beyond the screen with the camera inside: full cover.
                    drawables.Add(new Drawable(e.Id, Vector2.Zero, Vector2.Zero, fill, e.Color));
                    if (e.Id > fullCoverId) fullCoverId = e.Id;
                } else if (dPx > -fill) {
                    // The edge crosses the screen's vicinity: fill the inner half-plane
                    // with a thick line hugging the edge from the inside.
                    Vector2 n = new((float)e.N.X, (float)e.N.Y);
                    Vector2 tangent = new(-n.Y, n.X);
                    Vector2 center = n * ((float)dPx - fill);
                    drawables.Add(new Drawable(e.Id, center - tangent * (fill * 2f), center + tangent * (fill * 2f), fill, e.Color));
                }
                // Else the screen is fully outside this stroke; the entry stays, the
                // camera can come back.
            }
            return fullCoverId;
        }
    }
}
