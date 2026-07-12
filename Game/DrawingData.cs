using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameProject {
    public class DrawingData {
        // Files without the field deserialize as version 1 (flat float lines in one
        // frame); SaveDrawing always writes version 2 (frame tree, double coords).
        public int Version { get; set; } = 1;
        public Cam Camera { get; set; } = new Cam();
        public int NextId { get; set; } = 0;
        public Color BackgroundColor { get; set; } = new Color { R = 0, G = 0, B = 0 };
        public List<JsonLine> Lines { get; set; } = new List<JsonLine>();
        public List<JsonNode> Nodes { get; set; } = new List<JsonNode>();
        public List<Group> UndoGroups { get; set; } = new List<Group>();
        public List<Group> RedoGroups { get; set; } = new List<Group>();
        public List<JsonLine> RedoLines { get; set; } = new List<JsonLine>();
        public Dictionary<string, Cam> SavedCams { get; set; } = new Dictionary<string, Cam>();

        public class Cam {
            // v2: (i, j) index pairs, flattened, from the top root down to the anchor
            // frame. Null on v1 files where X, Y are top root coordinates and Z holds
            // the zoom as 1 / scale.
            public List<long>? Path { get; set; } = null;
            public double X { get; set; } = 0.0;
            public double Y { get; set; } = 0.0;
            public float Z { get; set; } = 1f;
            public double Exp { get; set; } = 0.0;
            public float Rotation { get; set; } = 0f;
        }

        public class JsonNode {
            public int Id { get; set; } = 0;
            // Parents are serialized before their children, so ParentId always refers
            // to an earlier entry. -1 marks the top root.
            public int ParentId { get; set; } = -1;
            public long I { get; set; } = 0;
            public long J { get; set; } = 0;
            public List<JsonLine> Lines { get; set; } = new List<JsonLine>();
        }

        public class JsonLine {
            public int Id { get; set; } = 0;
            public XY A { get; set; } = new XY();
            public XY B { get; set; } = new XY();
            public double Radius { get; set; } = 10.0;
            public Color? Color { get; set; } = new Color();
            // v2 redo lines only: index of the node the line is anchored to.
            public int NodeId { get; set; } = 0;
        }
        public class XY {
            public double X { get; set; } = 0;
            public double Y { get; set; } = 0;
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

    [JsonSourceGenerationOptionsAttribute(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true)]
    [JsonSerializable(typeof(DrawingData))]
    internal partial class DrawingDataContext : JsonSerializerContext { }
}
