using System;
#if SDLLINUX
using System.Runtime.InteropServices;
#endif

namespace GameProject {
    public static class Program {
        [STAThread]
        static void Main() {
            #if SDLLINUX
            // The vendored SDL2 binding imports "SDL2"; on Linux MonoGame ships the
            // native library as libSDL2-2.0.so.0 next to the executable.
            NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, static (name, assembly, searchPath) =>
                name == "SDL2" ? NativeLibrary.Load("libSDL2-2.0.so.0", assembly, searchPath) : IntPtr.Zero);
            #endif
            using var game = new GameRoot();
            game.Run();
        }
    }
}
