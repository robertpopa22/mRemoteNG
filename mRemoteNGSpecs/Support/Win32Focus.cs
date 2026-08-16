using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Reads keyboard focus through Win32 rather than UI Automation.
    ///
    /// UIA cannot answer the question while an RDP session has focus: FocusedElement() throws
    /// PropertyNotSupportedException every time, measured over five retries, because the RDP ActiveX
    /// control does not expose the property. A scenario that depends on knowing where focus went is
    /// therefore permanently inconclusive — which is exactly the scenario for the issue about the
    /// search box becoming unusable while a session is open.
    ///
    /// The product already reached the same conclusion from the other side. The fixes for #118 and
    /// #143 had to abandon managed focus for GetGUIThreadInfo and AttachThreadInput, because
    /// keyboard focus is per-input-queue and the managed view of it is not authoritative. Measuring
    /// it the same way the product manipulates it is the consistent choice.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class Win32Focus
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct GuiThreadInfo
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public int left, top, right, bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, [Out] char[] className, int max);

        /// <summary>
        /// The window with keyboard focus anywhere on the desktop, or zero if nothing has it.
        ///
        /// Thread id zero asks about the foreground thread's queue, which is the global answer —
        /// asking about our own thread would report our own queue and always look empty.
        /// </summary>
        public static IntPtr FocusedWindow()
        {
            GuiThreadInfo info = new() { cbSize = Marshal.SizeOf<GuiThreadInfo>() };
            return GetGUIThreadInfo(0, ref info) ? info.hwndFocus : IntPtr.Zero;
        }

        /// <summary>The process owning a window, or zero when it cannot be determined.</summary>
        public static int ProcessOf(IntPtr window)
        {
            if (window == IntPtr.Zero)
                return 0;

            // A zero thread id means the window handle is no longer valid, which is a real
            // possibility here: focus can move to a session window that is closing.
            uint threadId = GetWindowThreadProcessId(window, out uint processId);
            return threadId == 0 ? 0 : (int)processId;
        }

        /// <summary>The window class, which identifies the kind of control that holds focus.</summary>
        public static string ClassOf(IntPtr window)
        {
            if (window == IntPtr.Zero)
                return "";

            char[] buffer = new char[256];
            int length = GetClassName(window, buffer, buffer.Length);
            return length > 0 ? new string(buffer, 0, length) : "";
        }

        /// <summary>"handle / class / process", for putting in a failure message.</summary>
        public static string Describe(IntPtr window) =>
            window == IntPtr.Zero
                ? "nothing has keyboard focus"
                : $"hwnd 0x{window.ToInt64():X} class '{ClassOf(window)}' pid {ProcessOf(window)}";
    }
}
