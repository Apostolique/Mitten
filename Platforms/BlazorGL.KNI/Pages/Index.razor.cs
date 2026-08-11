using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace GameProject.Pages {
    public partial class Index {
        GameRoot? _game;

        protected override void OnAfterRender(bool firstRender) {
            base.OnAfterRender(firstRender);

            if (firstRender) {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet() {
            // The page sizes the canvas in device pixels, and this is how much denser that made
            // it plus the size the back buffer has to match. Read every frame rather than once,
            // since a rotation or a browser zoom moves it and KNI resets the canvas whenever the
            // window resizes. The call is in-process under WASM, so it costs no round trip.
            if (JsRuntime is IJSInProcessRuntime js) {
                GameRoot.UiScale = Math.Clamp(js.Invoke<float>("mittenUiScale"), 1f, 8f);
                GameRoot.BackBuffer = new Point(
                    js.Invoke<int>("mittenBackBufferWidth"),
                    js.Invoke<int>("mittenBackBufferHeight"));
            }

            // init game
            if (_game == null) {
                // Before the constructor, which is what loads the settings.
                InstallStore();
                if (JsRuntime is IJSInProcessRuntime pen) GameRoot.Pen = new PointerTablet(pen);
                _game = new GameRoot();
                _game.Run();
            }

            // run gameloop
            _game.Tick();
        }

        /// <summary>
        /// Writes the drawing out at the host's request. The browser never calls
        /// <c>UnloadContent</c>, so a tab closing on an unsaved drawing is the default rather
        /// than the accident, and the page drives the saving instead.
        /// </summary>
        [JSInvokable]
        public void SaveDotNet() {
            _game?.SaveNow();
        }

        /// <summary>
        /// Points the game's file names at browser storage.
        /// </summary>
        /// <remarks>
        /// The drawing goes to IndexedDB while the two small files go to localStorage. A
        /// drawing outgrows localStorage: the quota is ~5 MB and itch.io serves every html5
        /// game from one origin, so that budget is shared with every other game the player has
        /// opened, and a 1300 segment sketch already writes ~580 KB.
        ///
        /// Reads are synchronous because <c>LoadDrawing</c> runs inside <c>LoadContent</c>.
        /// IndexedDB has no synchronous API, so the page fetches every key into a plain object
        /// before the first tick and these read out of that.
        /// </remarks>
        void InstallStore() {
            if (JsRuntime is not IJSInProcessRuntime js) return;

            // itch.io serves every html5 game off one origin, so the key says which game it is.
            const string prefix = "mitten/";

            Store.Read = name => IsDrawing(name)
                ? js.Invoke<string?>("mittenRead", name)
                : js.Invoke<string?>("localStorage.getItem", prefix + name);

            Store.Write = (name, text) => {
                if (IsDrawing(name)) {
                    js.InvokeVoid("mittenWrite", name, text);
                } else {
                    js.InvokeVoid("localStorage.setItem", prefix + name, text);
                }
            };
        }

        /// <summary>The drawing and the backups a version migration leaves next to it, which are
        /// full copies of it and just as far past what localStorage would hold.</summary>
        static bool IsDrawing(string name) => name.StartsWith("Drawing", StringComparison.Ordinal);
    }
}
