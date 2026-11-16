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

    private static readonly Rectangle[] StackedScreens =
    [
        new Rectangle(0, 0, 1920, 1080),
        new Rectangle(0, 1080, 1920, 1080)
    ];

    [Test]
    public void FirstRunBoundsCoverNinetyPercentOfTheWorkingAreaAndStayCentred()
    {
        Rectangle workingArea = new(100, 40, 1000, 800);

        Rectangle bounds = MainWindowPlacement.FirstRunBounds(workingArea);

        Assert.Multiple(() =>
        {
            Assert.That(bounds.Size, Is.EqualTo(new Size(900, 720)));
            Assert.That(bounds.Location, Is.EqualTo(new Point(150, 80)));
            Assert.That(MainWindowPlacement.FirstRunScreenFraction, Is.EqualTo(0.9));
        });
    }

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
    public void RestoreBoundsKeepTheirVerticalOffsetWhenMovingBetweenStackedScreens()
    {
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            StackedScreens[0], new Rectangle(480, 1380, 974, 620), StackedScreens);

        Assert.That(result, Is.EqualTo(new Rectangle(480, 300, 974, 620)));
    }

    [Test]
    public void RestoreBoundsUseTheLargestOverlapEvenWhenAnotherScreenHasANearerCenter()
    {
        Rectangle smallerScreen = new(0, 0, 800, 600);
        Rectangle largerScreen = new(800, 0, 1920, 1080);

        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            smallerScreen, new Rectangle(700, 150, 400, 300), [smallerScreen, largerScreen]);

        Assert.That(result, Is.EqualTo(new Rectangle(0, 150, 400, 300)));
    }

    [TestCase(250, 250)]
    [TestCase(1200, 680)]
    public void RestoreBoundsPreferTheFirstScreenWhenOverlapOrDistanceTies(int restoreY, int expectedY)
    {
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            TwoScreens[1], new Rectangle(1620, restoreY, 600, 400), TwoScreens);

        Assert.That(result, Is.EqualTo(new Rectangle(3240, expectedY, 600, 400)));
    }

    [Test]
    public void RestoreBoundsKeepTheirSizeWhenLargerThanTheMaximizedScreen()
    {
        Rectangle smallerScreen = new(0, 0, 800, 600);
        Rectangle largerScreen = new(800, 0, 1920, 1080);

        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            smallerScreen, new Rectangle(900, 150, 1000, 700), [smallerScreen, largerScreen]);

        Assert.That(result, Is.EqualTo(new Rectangle(0, 0, 1000, 700)));
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

    [TestCase(720, 1, 2640)]
    [TestCase(1900, 0, 0)]
    public void RestoreBoundsBelowBothScreensUseHorizontalCenterDistance(
        int restoreX, int maximizedScreenIndex, int expectedX)
    {
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            TwoScreens[maximizedScreenIndex], new Rectangle(restoreX, 1200, 1000, 600), TwoScreens);

        Assert.That(result, Is.EqualTo(new Rectangle(expectedX, 480, 1000, 600)));
    }

    [TestCase(500, 1, 1580)]
    [TestCase(900, 0, 0)]
    public void RestoreBoundsBesideStackedScreensUseVerticalCenterDistance(
        int restoreY, int maximizedScreenIndex, int expectedY)
    {
        Rectangle result = MainWindowPlacement.RestoreBoundsOnMaximizedScreen(
            StackedScreens[maximizedScreenIndex], new Rectangle(2200, restoreY, 600, 400), StackedScreens);

        Assert.That(result, Is.EqualTo(new Rectangle(1320, expectedY, 600, 400)));
    }

    [Test]
    public void UnknownBoundsAreLeftAlone()
    {
        Rectangle restore = new(2400, 300, 974, 620);
        Rectangle maximized = new(-8, -8, 1936, 1048);

        Assert.Multiple(() =>
        {
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(Rectangle.Empty, restore, TwoScreens),
                        Is.EqualTo(restore));
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(maximized, Rectangle.Empty, TwoScreens),
                        Is.EqualTo(Rectangle.Empty));
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(maximized, restore, []),
                        Is.EqualTo(restore));
            Assert.That(MainWindowPlacement.RestoreBoundsOnMaximizedScreen(maximized, restore, null),
                        Is.EqualTo(restore));
        });
    }
}
