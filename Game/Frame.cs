using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameProject {
    /// <summary>
    /// A node in the infinite tree of coordinate frames that makes the canvas truly
    /// unbounded. Every position in the app is relative to some frame; there are no
    /// absolute coordinates anywhere.
    ///
    /// Conventions:
    /// - A frame spans [0, K)² in its own units.
    /// - The child with index (i, j) covers the 1×1 rect [i, i+1)×[j, j+1) of this
    ///   frame and spans [0, K)² in its own frame: p_child = (p_parent - (i, j)) * K.
    /// - K is a power of two so those transforms multiply exactly in doubles.
    /// - Levels increase downward (deeper = finer). Only level differences matter.
    /// - The tree grows lazily: down when zooming in, up (EnsureParent) when zooming
    ///   out or panning beyond the current top frame. Child indices stay in [0, K)²
    ///   so every region of space has exactly one node.
    /// </summary>
    public class Frame {
        public const long CellCount = 65536;
        public const double K = 65536.0;
        public static readonly double LnK = Math.Log(K);
        /// <summary>
        /// The camera stays anchored to a frame while its scale (pixels per frame unit)
        /// is within [BandMin, BandMax]. BandMax / BandMin = K, so crossing one edge
        /// re-anchors exactly onto the other edge of the neighboring band.
        /// </summary>
        public const double BandMax = 256.0;
        public const double BandMin = 1.0 / 256.0;

        public Frame? Parent;
        public (long X, long Y) Index;
        public long Level;
        public readonly Dictionary<(long X, long Y), Frame> Children = [];
        public readonly AABBTreeD<Line> Tree = new();

        public Vector2D IndexOffset => new(Index.X, Index.Y);

        // Subtree aggregates for impostor rendering when the whole subtree projects
        // below a pixel. Count is exact; bounds, max id and color are grow-only and
        // may go stale after undo, which only affects impostor looks, not content.
        public int SubtreeCount;
        public int SubtreeMaxId;
        public RectangleD? SubtreeBounds;
        public Color? SubtreeColor;

        public void BubbleAdd(RectangleD aabb, int id, Color? color) {
            Frame? f = this;
            RectangleD b = aabb;
            while (f != null) {
                f.SubtreeCount++;
                if (id > f.SubtreeMaxId) f.SubtreeMaxId = id;
                f.SubtreeBounds = f.SubtreeBounds == null ? b : RectangleD.Union(f.SubtreeBounds.Value, b);
                if (color != null) f.SubtreeColor = color;
                if (f.Parent == null) break;
                b = new RectangleD(b.X / K + f.Index.X, b.Y / K + f.Index.Y, b.Width / K, b.Height / K);
                f = f.Parent;
            }
        }
        public void BubbleRemove() {
            for (Frame? f = this; f != null; f = f.Parent) {
                f.SubtreeCount--;
            }
        }

        public Frame EnsureParent() {
            if (Parent == null) {
                Parent = new Frame { Level = Level - 1 };
                Index = (CellCount / 2, CellCount / 2);
                Parent.Children.Add(Index, this);
                Parent.SubtreeCount = SubtreeCount;
                Parent.SubtreeMaxId = SubtreeMaxId;
                Parent.SubtreeColor = SubtreeColor;
                if (SubtreeBounds is RectangleD b) {
                    Parent.SubtreeBounds = new RectangleD(b.X / K + Index.X, b.Y / K + Index.Y, b.Width / K, b.Height / K);
                }
            }
            return Parent;
        }
        public Frame GetOrCreateChild((long X, long Y) index) {
            if (!Children.TryGetValue(index, out Frame? child)) {
                child = new Frame { Parent = this, Index = index, Level = Level + 1 };
                Children.Add(index, child);
            }
            return child;
        }
        public Frame TopRoot() {
            Frame f = this;
            while (f.Parent != null) {
                f = f.Parent;
            }
            return f;
        }
    }
}
