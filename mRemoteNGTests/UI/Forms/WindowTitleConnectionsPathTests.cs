using System.IO;
using mRemoteNG.UI.Forms;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms;

public class WindowTitleConnectionsPathTests
{
    [Test]
    public void ARelativeConnectionsFileShowsItsFullPath()
    {
        const string relative = @"configs\confCons.xml";

        string shown = WindowTitleConnectionsPath.For(relative, showCompletePath: true);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.EqualTo(Path.GetFullPath(relative)));
            Assert.That(Path.IsPathRooted(shown), Is.True);
        });
    }

    [Test]
    public void TheShortTitleKeepsOnlyTheFileName()
    {
        string shown = WindowTitleConnectionsPath.For(@"configs\confCons.xml", showCompletePath: false);

        Assert.That(shown, Is.EqualTo("confCons.xml"));
    }

    [Test]
    public void AnAbsoluteConnectionsFileStaysAbsolute()
    {
        const string absolute = @"C:\Data\confCons.xml";

        string shown = WindowTitleConnectionsPath.For(absolute, showCompletePath: true);

        Assert.That(shown, Is.EqualTo(Path.GetFullPath(absolute)));
    }
}
