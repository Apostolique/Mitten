#if BLAZORGL
using System.Collections;
using System.Collections.Generic;
using Microsoft.JSInterop;

namespace GameProject {
    /// <summary>
    /// Browser counterpart to the Wintab and XInput2 paths. The page queues every pen point it
    /// sees and this drains the queue once a frame, in the same (x, y, pressure) packets the
    /// other two produce.
    /// </summary>
    /// <remarks>
    /// Pen only. A mouse and a finger both arrive as pointer events too, reporting 0.5 and
    /// somewhere around 1 while they are down, and routing those here would draw every mouse
    /// stroke at half thickness. They keep going through the mouse path, which is where they go
    /// on desktop.
    ///
    /// Positions are CSS pixels, which is already the unit the camera works in, so nothing has
    /// to be converted the way the bottom-up screen coordinates from Wintab do.
    /// </remarks>
    public sealed class PointerTablet {
        public PointerTablet(IJSInProcessRuntime js) {
            _js = js;
        }

        /// <summary>Whether a pen has been seen. False on a machine driven by a mouse, which is
        /// what sends the game down the mouse path instead.</summary>
        public bool IsValid => _js.Invoke<bool>("mittenPenSeen");

        /// <summary>Takes everything queued since the last call, as flat (x, y, pressure)
        /// triples.</summary>
        public IEnumerator<(float, float, float)> GetPackets() {
            float[] flat = _js.Invoke<float[]>("mittenDrainPen");
            _packets.Clear();
            for (int i = 0; i + 2 < flat.Length; i += 3) {
                _packets.Add((flat[i], flat[i + 1], flat[i + 2]));
            }
            return _packets.GetEnumerator();
        }

        readonly IJSInProcessRuntime _js;
        readonly List<(float, float, float)> _packets = [];
    }
}
#endif
