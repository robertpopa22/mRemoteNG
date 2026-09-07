using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Real mouse input for the clicks the UIA Invoke pattern cannot reach.
    ///
    /// AutomationElement.Click() prefers Invoke, which two real cases in this codebase never
    /// respond to: right-click on ConnectionTree's rows does not open its context menu through
    /// Invoke -- the custom ObjectListView-derived control only wires that to a genuine mouse
    /// event -- and there is no Invoke equivalent for a middle click at all, which the #142
    /// middle-click-closes-a-tab scenario needed to reach DockPaneStripNG.MiddleClickCloseTab.
    /// Both were found by falling back to FlaUI's own Mouse class by hand during manual
    /// verification; this collects that pattern once instead of re-deriving it per scenario.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class Win32Mouse
    {
        /// <summary>The element's centre, in screen coordinates -- BoundingRectangle already is.</summary>
        public static Point CentreOf(AutomationElement element)
        {
            Rectangle bounds = element.BoundingRectangle;
            return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }

        public static void RightClick(AutomationElement element) =>
            Mouse.Click(CentreOf(element), MouseButton.Right);

        public static void MiddleClick(AutomationElement element) =>
            Mouse.Click(CentreOf(element), MouseButton.Middle);

        /// <summary>
        /// A double-click the way the OS sees one: four button events in a single SendInput
        /// batch, so nothing can stretch the gap between the two clicks past the double-click time.
        ///
        /// FlaUI's DoubleClick is two Clicks, each of which waits for input to be processed. On a
        /// lab guest without a GPU that wait ran the pair past 500 ms, the tree took them as two
        /// single clicks — the second on an already selected row, which is the slow-click rename —
        /// and a scenario that needed the double-click handler saw a rename box instead.
        /// The point is offset from the row's left edge, onto the caption, past the indent and
        /// the expand glyph that a first-level row keeps on its left.
        /// </summary>
        public static void DoubleClick(AutomationElement element, int captionOffset = 60)
        {
            Rectangle bounds = element.BoundingRectangle;
            Point point = new(bounds.X + captionOffset, bounds.Y + bounds.Height / 2);
            Mouse.MoveTo(point);
            Wait.UntilInputIsProcessed();

            INPUT[] inputs =
            [
                ButtonEvent(MouseEventLeftDown), ButtonEvent(MouseEventLeftUp),
                ButtonEvent(MouseEventLeftDown), ButtonEvent(MouseEventLeftUp),
            ];
            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent != inputs.Length)
                throw new InvalidOperationException($"SendInput delivered {sent} of {inputs.Length} mouse events");
            Wait.UntilInputIsProcessed();
        }

        private const uint InputMouse = 0;
        private const uint MouseEventLeftDown = 0x0002;
        private const uint MouseEventLeftUp = 0x0004;

        private static INPUT ButtonEvent(uint flags) =>
            new() { type = InputMouse, u = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } } };

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, INPUT[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
