using System.Collections.Generic;

namespace GameProject {
    public class DrawingData {
        public int NextId { get; set; } = 0;
        public List<JsonLine> Lines { get; set; } = new List<JsonLine>();
        public List<Group> UndoGroups { get; set; } = new List<Group>();
        public List<Group> RedoGroups { get; set; } = new List<Group>();
        public List<JsonLine> RedoLines { get; set; } = new List<JsonLine>();

        public class JsonLine {
            public int Id { get; set; }
            public XY A { get; set; }
            public XY B { get; set; }
            public float Radius { get; set; }
        }
        public class XY {
            public float X { get; set; }
            public float Y { get; set; }
        }
        public class Group {
            public int First { get; set; }
            public int Last { get; set; }
        }
    }
}
