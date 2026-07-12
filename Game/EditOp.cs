using System.Collections.Generic;

namespace GameProject {
    /// <summary>
    /// One entry in the undo/redo history. Undo and redo are strictly LIFO, which is
    /// what keeps DrawOp's dense id-range assumption sound: by the time a DrawOp is
    /// undone, every later op touching its lines has already been undone, so the whole
    /// range is alive. Only DrawOp mints or releases ids; the other ops reference
    /// existing lines, so memory scales with edits, not with history length.
    /// </summary>
    public abstract class EditOp { }

    /// <summary>One pen stroke: the contiguous id range minted while drawing it.</summary>
    public sealed class DrawOp : EditOp {
        public int First;
        public int Last;
        // Holds the detached lines only while the op sits in the redo stack.
        public List<Line>? Lines;
    }

    /// <summary>One erase gesture or selection delete. Owns the dead lines, with
    /// their <see cref="Line.Node"/> anchors intact so undo can re-attach them.</summary>
    public sealed class DeleteOp : EditOp {
        public List<Line> Lines = [];
    }

    /// <summary>
    /// Exact pre-transform state of one line. Undo restores it verbatim (bit-identical,
    /// no float drift); redo recomputes the transform from it, which is deterministic.
    /// </summary>
    public sealed class LineSnapshot {
        public Line Line = null!;
        public Frame Node = null!;
        public Vector2D A;
        public Vector2D B;
        public double Radius;
    }

    /// <summary>Rigid translation of a set of lines. Delta is in <see cref="Ref"/>
    /// units; frames are never pruned, so the reference stays valid forever.</summary>
    public sealed class MoveOp : EditOp {
        public Frame Ref = null!;
        public Vector2D Delta;
        public List<LineSnapshot> Originals = [];
    }

    /// <summary>Uniform scale of a set of lines about Center (in <see cref="Ref"/>
    /// units). Radius scales along with the geometry.</summary>
    public sealed class ScaleOp : EditOp {
        public Frame Ref = null!;
        public Vector2D Center;
        public double Factor;
        public List<LineSnapshot> Originals = [];
    }
}
