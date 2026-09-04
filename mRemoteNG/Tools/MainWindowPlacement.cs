using System.Collections.Generic;
using System.Drawing;

namespace mRemoteNG.Tools
{
    /// <summary>
    /// Decides where the main window belongs when a saved maximized state is restored.
    /// </summary>
    public static class MainWindowPlacement
    {
        /// <summary>
        /// A window saved while maximized remembers two rectangles: the bounds it filled on the
        /// monitor it was maximized on, and the smaller bounds it returns to when un-maximized.
        /// Only the first records which monitor the user actually had it on, because the restore
        /// bounds are refreshed only while the window is not in the Normal state - so they can
        /// still name a monitor the window was dragged away from long ago. Placing the window at
        /// those stale coordinates before maximizing it maximizes onto the wrong monitor (#171).
        /// Keep the remembered restore size, but move it onto the monitor the maximized bounds
        /// name, at the same position relative to that monitor's own origin.
        /// </summary>
        /// <param name="maximizedBounds">Bounds the window occupied while maximized.</param>
        /// <param name="restoreBounds">Bounds the window returns to when un-maximized.</param>
        /// <param name="screenBounds">Bounds of every attached screen.</param>
        /// <returns>The restore bounds, moved onto the maximized monitor if they were elsewhere.</returns>
        public static Rectangle RestoreBoundsOnMaximizedScreen(Rectangle maximizedBounds,
                                                               Rectangle restoreBounds,
                                                               IReadOnlyList<Rectangle> screenBounds)
        {
            if (maximizedBounds.IsEmpty || restoreBounds.IsEmpty || screenBounds == null || screenBounds.Count == 0)
                return restoreBounds;

            Rectangle maximizedScreen = ScreenFor(maximizedBounds, screenBounds);
            Rectangle restoreScreen = ScreenFor(restoreBounds, screenBounds);
            if (maximizedScreen == restoreScreen)
                return restoreBounds;

            return new Rectangle(Clamp(maximizedScreen.X + (restoreBounds.X - restoreScreen.X),
                                      maximizedScreen.X, maximizedScreen.Right - restoreBounds.Width),
                                 Clamp(maximizedScreen.Y + (restoreBounds.Y - restoreScreen.Y),
                                       maximizedScreen.Y, maximizedScreen.Bottom - restoreBounds.Height),
                                 restoreBounds.Width,
                                 restoreBounds.Height);
        }

        /// <summary>
        /// Keeps the window on the monitor even when the offset it carried over does not fit there
        /// - a wider window, or bounds that were off every screen to begin with.
        /// </summary>
        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
                return min;

            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Screen.FromRectangle's rule, over plain rectangles so it can be exercised without a
        /// second monitor attached: the screen sharing the most area with the window, or - when
        /// the window is off every screen - the one whose centre is nearest.
        /// </summary>
        private static Rectangle ScreenFor(Rectangle bounds, IReadOnlyList<Rectangle> screenBounds)
        {
            Rectangle best = screenBounds[0];
            long bestArea = 0;

            foreach (Rectangle screen in screenBounds)
            {
                Rectangle overlap = Rectangle.Intersect(screen, bounds);
                long area = (long)overlap.Width * overlap.Height;
                if (area <= bestArea)
                    continue;

                bestArea = area;
                best = screen;
            }

            if (bestArea > 0)
                return best;

            double bestDistance = double.MaxValue;
            foreach (Rectangle screen in screenBounds)
            {
                double dx = (screen.X + (screen.Width / 2.0)) - (bounds.X + (bounds.Width / 2.0));
                double dy = (screen.Y + (screen.Height / 2.0)) - (bounds.Y + (bounds.Height / 2.0));
                double distance = (dx * dx) + (dy * dy);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = screen;
            }

            return best;
        }
    }
}
