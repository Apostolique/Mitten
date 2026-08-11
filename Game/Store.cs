using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
#if !BLAZORGL
using System.IO;
#endif

namespace GameProject {
    /// <summary>
    /// Reads and writes the game's json files. Desktop answers out of a directory, while the
    /// browser has none to write to, so a host there installs <see cref="Read"/> and
    /// <see cref="Write"/> and the same names go somewhere else.
    /// </summary>
    /// <remarks>
    /// A bad read throws rather than returning a fresh object. An empty canvas would be saved
    /// back over the file it failed to parse, so the drawing is worth more than the crash costs.
    /// </remarks>
    public static class Store {
        public const string DrawingFile = "Drawing.json";
        public const string PaletteFile = "Palette.json";
        public const string SettingsFile = "Settings.json";

        /// <summary>Returns the text stored under a name, or null when nothing is stored there.
        /// Left null on desktop, where the file system answers instead.</summary>
        public static Func<string, string?>? Read;
        public static Action<string, string>? Write;

        public static void Save<T>(string name, T value, JsonTypeInfo<T> info) {
            WriteText(name, JsonSerializer.Serialize(value, info));
        }

        /// <summary>Loads a file, writing out the defaults first when nothing is stored yet.</summary>
        public static T Ensure<T>(string name, JsonTypeInfo<T> info) where T : new() {
            string? text = ReadText(name);
            if (text != null) return JsonSerializer.Deserialize(text, info)!;

            T fresh = new T();
            Save(name, fresh, info);
            return fresh;
        }

        /// <summary>Copies <paramref name="name"/> to <paramref name="backup"/>, leaving any
        /// backup that is already there alone.</summary>
        public static void Backup(string name, string backup) {
            try {
                if (Exists(backup)) return;
                string? text = ReadText(name);
                if (text != null) WriteText(backup, text);
            } catch (Exception ex) {
                Console.WriteLine($"Drawing backup failed: {ex}");
            }
        }

        static string? ReadText(string name) {
            if (Read != null) return Read(name);
#if BLAZORGL
            return null;
#else
            string path = GetPath(name);
            return File.Exists(path) ? File.ReadAllText(path) : null;
#endif
        }

        static void WriteText(string name, string text) {
            if (Write != null) {
                Write(name, text);
                return;
            }
#if !BLAZORGL
            File.WriteAllText(GetPath(name), text);
#endif
        }

        static bool Exists(string name) {
            if (Read != null) return Read(name) != null;
#if BLAZORGL
            return false;
#else
            return File.Exists(GetPath(name));
#endif
        }

#if !BLAZORGL
        static readonly string _savePath = FindSavePath();
        static string GetPath(string name) => Path.Combine(_savePath, name);

        // On macOS the executable lives inside Mitten.app, and an updater is free to
        // replace a bundle wholesale, so drawings go to the usual per-user directory
        // instead. Everywhere else they stay next to the executable.
        static string FindSavePath() {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory!;

            if (!OperatingSystem.IsMacOS()) return baseDirectory;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home)) return baseDirectory;

            string path = Path.Combine(home, "Library", "Application Support", "Mitten");
            try {
                Directory.CreateDirectory(path);
            } catch (Exception) {
                // Losing the drawing beats failing to start, so fall back to the bundle.
                return baseDirectory;
            }

            return path;
        }
#endif
    }
}
