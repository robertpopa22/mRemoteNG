using System;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace mRemoteNGSpecs.Drivers
{
    /// <summary>
    /// Serialises what was on screen when a test failed.
    ///
    /// A screenshot shows a human what happened; it cannot be grepped. This dump is what lets an
    /// automated pipeline tell "the property grid never appeared" apart from "the property grid
    /// appeared but the row I wanted wasn't in it" — two failures that look identical in a PNG and
    /// need completely different fixes.
    ///
    /// Every top-level window is walked, not just the app's main one, because the interesting
    /// element at failure time is often a dialog the test never expected.
    /// </summary>
    public static class UiaTreeDumper
    {
        public static string DumpDesktop(UIA3Automation automation, int maxDepth = 12)
        {
            StringBuilder sb = new();
            sb.AppendLine($"# UIA snapshot {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                foreach (AutomationElement window in automation.GetDesktop().FindAllChildren())
                    Walk(window, sb, 0, maxDepth);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"# dump aborted: {ex.GetType().Name}: {ex.Message}");
            }

            return sb.ToString();
        }

        public static string DumpElement(AutomationElement root, int maxDepth = 12)
        {
            StringBuilder sb = new();
            Walk(root, sb, 0, maxDepth);
            return sb.ToString();
        }

        private static void Walk(AutomationElement element, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            string indent = new(' ', depth * 2);
            try
            {
                sb.Append(indent)
                  .Append(element.ControlType)
                  .Append("  id=").Append(Safe(() => element.AutomationId))
                  .Append("  name=").Append(Safe(() => element.Name))
                  .Append("  enabled=").Append(Safe(() => element.IsEnabled.ToString()))
                  .Append("  offscreen=").Append(Safe(() => element.IsOffscreen.ToString()))
                  .Append("  rect=").Append(Safe(() => element.BoundingRectangle.ToString()))
                  .AppendLine();

                foreach (AutomationElement child in element.FindAllChildren())
                    Walk(child, sb, depth + 1, maxDepth);
            }
            catch (Exception ex)
            {
                sb.Append(indent).AppendLine($"<unreadable: {ex.GetType().Name}>");
            }
        }

        private static string Safe(Func<string> read)
        {
            try
            {
                string value = read();
                return string.IsNullOrEmpty(value) ? "-" : value;
            }
            catch (Exception)
            {
                return "?";
            }
        }
    }
}
