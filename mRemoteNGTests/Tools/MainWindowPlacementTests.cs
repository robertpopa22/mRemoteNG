using System.Drawing;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

public class MainWindowPlacementTests
{
    // Two 1920x1080 monitors side by side, the left one primary.
    private static readonly Rectangle[] TwoScreens =
    [
        new Rectangle(0, 0, 1920, 1080),
        new Rectangle(1920, 0, 1920, 1080)
    ];

    [Test]
    public void RestoreBoundsLeftAloneWhenTheyShareTheMaximizedScreen()
    {
        Rectangle restore = new(200, 150, 974, 620);

        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            new Rectangle(-8, -8, 1936, 1048), restore, TwoScreens);

        Assert.That(result, Is.EqualTo(restore));
    }

    [Test]
    public void RestoreBoundsMoveOntoTheScreenTheWindowWasMaximizedOn()
    {
        // #171: maximized on the primary, but the restore bounds still name the second monitor.
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            new Rectangle(-8, -8, 1936, 1048),
            new Rectangle(2400, 300, 974, 620),
            TwoScreens);

        // Same offset within the monitor (2400 - 1920 = 480), same size.
        Assert.That(result, Is.EqualTo(new Rectangle(480, 300, 974, 620)));
    }

    [Test]
    public void RestoreBoundsMoveOntoTheSecondScreenToo()
    {
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            new Rectangle(1912, -8, 1936, 1048),
            new Rectangle(480, 300, 974, 620),
            TwoScreens);

        Assert.That(result, Is.EqualTo(new Rectangle(2400, 300, 974, 620)));
    }

    [Test]
    public void ASingleScreenNeverMovesAnything()
    {
        Rectangle restore = new(2400, 300, 974, 620);

        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            new Rectangle(-8, -8, 1936, 1048), restore, [new Rectangle(0, 0, 1920, 1080)]);

        Assert.That(result, Is.EqualTo(restore));
    }

    [Test]
    public void RestoreBoundsOffEveryScreenAreTreatedAsBelongingToTheNearestOne()
    {
        // A monitor that has since been unplugged: the bounds sit beyond the right-hand screen.
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            new Rectangle(-8, -8, 1936, 1048),
            new Rectangle(4200, 300, 974, 620),
            TwoScreens);

        // Carried-over offset would land past the target screen, so it is pulled back onto it.
        Assert.That(result, Is.EqualTo(new Rectangle(946, 300, 974, 620)));
    }

    [Test]
    public void UnknownBoundsAreLeftAlone()
    {
        Rectangle restore = new(2400, 300, 974, 620);

        Assert.Multiple(() =>
        {
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(Rectangle.Empty, restore, TwoScreens),
                        Is.EqualTo(restore));
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(new Rectangle(-8, -8, 1936, 1048),
                                                                           Rectangle.Empty, TwoScreens),
                        Is.EqualTo(Rectangle.Empty));
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(new Rectangle(-8, -8, 1936, 1048),
                                                                           restore, []),
                        Is.EqualTo(restore));
        });
    }
}
