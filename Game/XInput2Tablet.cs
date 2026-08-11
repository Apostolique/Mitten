#if SDLLINUX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GameProject {
    /// <summary>
    /// Linux counterpart to the Wintab path. Listens on a dedicated X connection for
    /// XInput2 events from devices that expose an "Abs Pressure" axis (drawing tablets)
    /// over the SDL window. Packets are window-relative pixels with pressure normalized
    /// to 0..1, the same shape QueryTablet produces on Windows. Works on X11 and on
    /// Wayland through XWayland. Tablets plugged in while running are picked up via
    /// XI_HierarchyChanged.
    /// </summary>
    public sealed class XInput2Tablet : IDisposable {
        public XInput2Tablet(IntPtr sdlWindow) {
            _window = sdlWindow;
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero) {
                throw new InvalidOperationException("XOpenDisplay failed.");
            }

            if (XQueryExtension(_display, "XInputExtension", out _xiOpcode, out _, out _) == 0) {
                throw new InvalidOperationException("XInputExtension not present.");
            }
            int major = 2;
            int minor = 2;
            if (XIQueryVersion(_display, ref major, ref minor) != 0) {
                throw new InvalidOperationException("Server does not support XInput2.");
            }

            _absPressure = XInternAtom(_display, "Abs Pressure", 0);
            _eventBuffer = Marshal.AllocHGlobal(XEventSize);

            RefreshDevices();
            SelectHierarchyEvents();
        }

        /// <summary>True when at least one pressure-capable device is connected. Pumps
        /// hotplug events so a tablet plugged in after startup flips this to true.</summary>
        public bool IsValid {
            get {
                if (_devices.Count == 0) {
                    Drain();
                }
                return _devices.Count > 0;
            }
        }

        public string DeviceSummary => _devices.Count == 0 ? "none" : string.Join(", ", _deviceNames);

        /// <summary>Window-relative (x, y, pressure 0..1) packets since the last call.</summary>
        public IEnumerator<(float, float, float)> GetPackets() {
            _packets.Clear();
            Drain();
            return _packets.GetEnumerator();
        }

        /// <summary>Discard pending packets (drawing input is going elsewhere this frame).</summary>
        public void Flush() {
            _packets.Clear();
            Drain();
            _packets.Clear();
        }

        public void Dispose() {
            if (_eventBuffer != IntPtr.Zero) {
                Marshal.FreeHGlobal(_eventBuffer);
                _eventBuffer = IntPtr.Zero;
            }
            if (_display != IntPtr.Zero) {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }

        private void Drain() {
            if (_display == IntPtr.Zero) return;
            while (XPending(_display) > 0) {
                XNextEvent(_display, _eventBuffer);
                if (Marshal.ReadInt32(_eventBuffer) != GenericEvent) continue;
                var cookie = Marshal.PtrToStructure<XGenericEventCookie>(_eventBuffer);
                if (cookie.extension != _xiOpcode) continue;
                if (XGetEventData(_display, _eventBuffer) == 0) continue;
                try {
                    cookie = Marshal.PtrToStructure<XGenericEventCookie>(_eventBuffer);
                    HandleEvent(cookie.evtype, cookie.data);
                } finally {
                    XFreeEventData(_display, _eventBuffer);
                }
            }
        }

        private void HandleEvent(int evtype, IntPtr data) {
            if (evtype == XI_HierarchyChanged) {
                RefreshDevices();
                return;
            }
            if (evtype != XI_ButtonPress && evtype != XI_ButtonRelease && evtype != XI_Motion) return;

            var e = Marshal.PtrToStructure<XIDeviceEvent>(data);
            if (!_devices.TryGetValue(e.deviceid, out DeviceState? dev)) return;

            // The pressure valuator is only present in the event when it changed;
            // otherwise carry the device's last known value.
            if (IsBitSet(e.valuators_mask, e.valuators_mask_len, dev.Valuator)) {
                int index = 0;
                for (int i = 0; i < dev.Valuator; i++) {
                    if (IsBitSet(e.valuators_mask, e.valuators_mask_len, i)) index++;
                }
                double raw = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(e.valuators_values, index * sizeof(double)));
                dev.LastPressure = (float)((raw - dev.Min) / (dev.Max - dev.Min));
            }

            _packets.Add(((int)e.event_x, (int)e.event_y, Math.Clamp(dev.LastPressure, 0f, 1f)));
        }

        private static bool IsBitSet(IntPtr mask, int maskLen, int bit) {
            if (bit >= maskLen * 8) return false;
            return (Marshal.ReadByte(mask, bit >> 3) & (1 << (bit & 7))) != 0;
        }

        private void RefreshDevices() {
            var previous = _devices;
            _devices = [];
            _deviceNames.Clear();

            IntPtr infos = XIQueryDevice(_display, XIAllDevices, out int count);
            if (infos == IntPtr.Zero) return;
            try {
                int stride = Marshal.SizeOf<XIDeviceInfo>();
                for (int i = 0; i < count; i++) {
                    var di = Marshal.PtrToStructure<XIDeviceInfo>(infos + i * stride);
                    // Masters mirror whichever slave is active; only track the slaves themselves.
                    if (di.use != XISlavePointer && di.use != XIFloatingSlave) continue;

                    for (int c = 0; c < di.num_classes; c++) {
                        IntPtr cls = Marshal.ReadIntPtr(di.classes, c * IntPtr.Size);
                        if (Marshal.ReadInt32(cls) != XIValuatorClass) continue;
                        var v = Marshal.PtrToStructure<XIValuatorClassInfo>(cls);
                        if (v.label != _absPressure || v.max <= v.min) continue;

                        var dev = new DeviceState { Valuator = v.number, Min = v.min, Max = v.max };
                        if (previous.TryGetValue(di.deviceid, out DeviceState? old)) {
                            dev.LastPressure = old.LastPressure;
                        }
                        _devices[di.deviceid] = dev;
                        _deviceNames.Add(Marshal.PtrToStringAnsi(di.name) ?? $"device {di.deviceid}");
                        break;
                    }
                }
            } finally {
                XIFreeDeviceInfo(infos);
            }

            if (_devices.Count > 0) {
                SelectDeviceEvents();
            }

            string summary = DeviceSummary;
            if (summary != _lastSummary) {
                Console.WriteLine($"Tablet: {summary}");
                _lastSummary = summary;
            }
        }

        private void SelectDeviceEvents() {
            IntPtr mask = Marshal.AllocHGlobal(4);
            try {
                Marshal.WriteInt32(mask, (1 << XI_ButtonPress) | (1 << XI_ButtonRelease) | (1 << XI_Motion));
                var masks = new XIEventMask[_devices.Count];
                int i = 0;
                foreach (int deviceid in _devices.Keys) {
                    masks[i++] = new XIEventMask { deviceid = deviceid, mask_len = 4, mask = mask };
                }
                XISelectEvents(_display, _window, masks, masks.Length);
                XFlush(_display);
            } finally {
                Marshal.FreeHGlobal(mask);
            }
        }

        private void SelectHierarchyEvents() {
            IntPtr mask = Marshal.AllocHGlobal(4);
            try {
                Marshal.WriteInt32(mask, 1 << XI_HierarchyChanged);
                var masks = new XIEventMask[] {
                    new() { deviceid = XIAllDevices, mask_len = 4, mask = mask }
                };
                XISelectEvents(_display, XDefaultRootWindow(_display), masks, 1);
                XFlush(_display);
            } finally {
                Marshal.FreeHGlobal(mask);
            }
        }

        private class DeviceState {
            public int Valuator;
            public double Min;
            public double Max;
            public float LastPressure;
        }

        private readonly IntPtr _window;
        private IntPtr _display;
        private IntPtr _eventBuffer;
        private readonly int _xiOpcode;
        private readonly IntPtr _absPressure;
        private Dictionary<int, DeviceState> _devices = [];
        private readonly List<string> _deviceNames = [];
        private string _lastSummary = "";
        private readonly List<(float, float, float)> _packets = [];

        private const int GenericEvent = 35;
        private const int XEventSize = 192;
        private const int XIAllDevices = 0;
        private const int XISlavePointer = 3;
        private const int XIFloatingSlave = 5;
        private const int XIValuatorClass = 2;
        private const int XI_ButtonPress = 4;
        private const int XI_ButtonRelease = 5;
        private const int XI_Motion = 6;
        private const int XI_HierarchyChanged = 11;

        [StructLayout(LayoutKind.Sequential)]
        private struct XIEventMask {
            public int deviceid;
            public int mask_len;
            public IntPtr mask;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XIDeviceInfo {
            public int deviceid;
            public IntPtr name;
            public int use;
            public int attachment;
            public int enabled;
            public int num_classes;
            public IntPtr classes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XIValuatorClassInfo {
            public int type;
            public int sourceid;
            public int number;
            public IntPtr label;
            public double min;
            public double max;
            public double value;
            public int resolution;
            public int mode;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct XGenericEventCookie {
            [FieldOffset(0)] public int type;
            [FieldOffset(32)] public int extension;
            [FieldOffset(36)] public int evtype;
            [FieldOffset(40)] public uint cookie;
            [FieldOffset(48)] public IntPtr data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct XIDeviceEvent {
            [FieldOffset(0)] public int type;
            [FieldOffset(32)] public int extension;
            [FieldOffset(36)] public int evtype;
            [FieldOffset(48)] public int deviceid;
            [FieldOffset(52)] public int sourceid;
            [FieldOffset(56)] public int detail;
            [FieldOffset(88)] public double root_x;
            [FieldOffset(96)] public double root_y;
            [FieldOffset(104)] public double event_x;
            [FieldOffset(112)] public double event_y;
            [FieldOffset(120)] public int flags;
            [FieldOffset(128)] public int buttons_mask_len;
            [FieldOffset(136)] public IntPtr buttons_mask;
            [FieldOffset(144)] public int valuators_mask_len;
            [FieldOffset(152)] public IntPtr valuators_mask;
            [FieldOffset(160)] public IntPtr valuators_values;
        }

        private const string LibX11 = "libX11.so.6";
        private const string LibXi = "libXi.so.6";

        [DllImport(LibX11)] private static extern IntPtr XOpenDisplay(IntPtr name);
        [DllImport(LibX11)] private static extern int XCloseDisplay(IntPtr display);
        [DllImport(LibX11)] private static extern int XPending(IntPtr display);
        [DllImport(LibX11)] private static extern int XNextEvent(IntPtr display, IntPtr ev);
        [DllImport(LibX11)] private static extern int XFlush(IntPtr display);
        [DllImport(LibX11)] private static extern int XGetEventData(IntPtr display, IntPtr cookie);
        [DllImport(LibX11)] private static extern void XFreeEventData(IntPtr display, IntPtr cookie);
        [DllImport(LibX11)] private static extern IntPtr XDefaultRootWindow(IntPtr display);
        [DllImport(LibX11, CharSet = CharSet.Ansi)] private static extern int XQueryExtension(IntPtr display, string name, out int opcode, out int firstEvent, out int firstError);
        [DllImport(LibX11, CharSet = CharSet.Ansi)] private static extern IntPtr XInternAtom(IntPtr display, string name, int onlyIfExists);
        [DllImport(LibXi)] private static extern int XIQueryVersion(IntPtr display, ref int major, ref int minor);
        [DllImport(LibXi)] private static extern IntPtr XIQueryDevice(IntPtr display, int deviceid, out int ndevices);
        [DllImport(LibXi)] private static extern void XIFreeDeviceInfo(IntPtr info);
        [DllImport(LibXi)] private static extern int XISelectEvents(IntPtr display, IntPtr window, [In] XIEventMask[] masks, int nmasks);
    }
}
#endif
