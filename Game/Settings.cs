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
    }
}
