using System.Text.Json.Serialization;

namespace GameProject {
    public class Settings {
        public int X { get; set; } = 320;
        public int Y { get; set; } = 180;
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public bool IsFixedTimeStep { get; set; } = true;
        public bool IsVSync { get; set; } = false;
        public bool IsFullscreen { get; set; } = false;
        public bool IsBorderless { get; set; } = false;
        public bool ShowMouse { get; set; } = true;
        // Temporary mode (T): how long after pen-down a stroke's start lingers
        // before the erase front sets off after it, and how fast the front moves
        // relative to the speed the stroke was drawn.
        public float TempDelaySeconds { get; set; } = 1f;
        public float TempDecaySpeed { get; set; } = 1f;
    }

    [JsonSourceGenerationOptionsAttribute(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true)]
    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsContext : JsonSerializerContext { }
}
