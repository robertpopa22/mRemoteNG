using System.Drawing;
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
    }
}
