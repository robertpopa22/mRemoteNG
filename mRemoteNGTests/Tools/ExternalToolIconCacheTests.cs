using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

/// <summary>
/// An external tool's icon is read from an executable on disk. Resolving it on every read was
/// affordable while only menus asked for it; it stopped being affordable when the External Tools
/// editor began committing on every keystroke (#179), because each commit rebuilds the whole
/// toolbar and the rebuild reads every visible tool's icon. The reporter felt that as the editor
/// going sluggish while typing.
///
/// A disposed ToolStripItem leaves its Image alone (measured), so every rebuild also stranded one
/// Bitmap and one Icon per tool.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows")]
public class ExternalToolIconCacheTests
{
    private static string AnExecutableOnDisk =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");

    [Test]
    public void TheIconIsResolvedOnceAndHandedOutAgainOnEveryRead()
    {
        ExternalTool tool = new("Notepad", AnExecutableOnDisk);

        Icon first = tool.Icon;
        Icon second = tool.Icon;

        Assert.That(second, Is.SameAs(first),
                    "reading the icon went back to the disk — the toolbar does this for every tool on "
                    + "every keystroke typed in the editor");
    }

    [Test]
    public void TheImageIsBuiltOnceAndHandedOutAgainOnEveryRead()
    {
        ExternalTool tool = new("Notepad", AnExecutableOnDisk);

        Image first = tool.Image;
        Image second = tool.Image;

        Assert.That(second, Is.SameAs(first),
                    "a fresh bitmap per read: the old one is stranded, because disposing a toolbar "
                    + "button does not dispose the image it was given");
    }

    [Test]
    public void ChangingTheIconPathIsPickedUp()
    {
        // The cache must not outlive the thing it caches: editing the icon path in the editor has
        // to change what the toolbar shows.
        ExternalTool tool = new("Notepad", AnExecutableOnDisk);
        Image before = tool.Image;

        tool.IconPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");

        Assert.That(tool.Image, Is.Not.SameAs(before), "the icon path changed and the image did not");
    }

    [Test]
    public void ChangingTheFileNameIsPickedUp()
    {
        ExternalTool tool = new("Notepad", AnExecutableOnDisk);
        Image before = tool.Image;

        tool.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");

        Assert.That(tool.Image, Is.Not.SameAs(before), "the executable changed and the image did not");
    }

    [Test]
    public void EditingAFieldTheIconDoesNotDependOnKeepsTheCachedImage()
    {
        // This is the keystroke case: typing in any other field must not send the toolbar back to
        // the disk. CommitEditorValues writes every field on every keystroke, so "any other field"
        // is most of them.
        ExternalTool tool = new("Notepad", AnExecutableOnDisk);
        Image before = tool.Image;

        tool.DisplayName = "Notepad renamed";
        tool.Arguments = "-a";
        tool.WorkingDir = @"C:\";
        tool.AuthenticationUsername = "user";

        Assert.That(tool.Image, Is.SameAs(before), "an unrelated edit threw away the resolved icon");
    }

    [Test]
    public void AToolWhoseExecutableDoesNotExistStillHasAnIcon()
    {
        ExternalTool tool = new("Nothing", @"Z:\no\such\program.exe");

        Assert.Multiple(() =>
        {
            Assert.That(tool.Icon, Is.Not.Null);
            Assert.That(tool.Image, Is.Not.Null);
            Assert.That(tool.Image, Is.SameAs(tool.Image), "the fallback is cached too");
        });
    }
}
