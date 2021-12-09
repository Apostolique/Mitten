using System.Collections.Generic;

namespace GameProject {
    public class DrawingData {
        public int NextId { get; set; } = 0;
        public List<JsonLine> Lines { get; set; } = new List<JsonLine>();
        public List<Group> UndoGroups { get; set; } = new List<Group>();
        public List<Group> RedoGroups { get; set; } = new List<Group>();
        public List<JsonLine> RedoLines { get; set; } = new List<JsonLine>();

        public class JsonLine {
            public int Id { get; set; } = 0;
            public XY A { get; set; } = new XY();
            public XY B { get; set; } = new XY();
            public float Radius { get; set; } = 10f;
            public Color Color { get; set; } = new Color();
        }
        public class XY {
            public float X { get; set; } = 0;
            public float Y { get; set; } = 0;
        }
        public class Color {
            public byte R { get; set; } = 212;
            public byte G { get; set; } = 212;
            public byte B { get; set; } = 216;
        }
        public class Group {
            public int First { get; set; } = 0;
            public int Last { get; set; } = 0;
        }
    }
}
