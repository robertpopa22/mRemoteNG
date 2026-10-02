using System.Drawing;
using mRemoteNG.Themes;
using NUnit.Framework;

namespace mRemoteNGTests.Themes;

public class OsAppThemeTests
{
    [Test]
    public void AMissingOrLightWindowsSettingSelectsTheLightTheme()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OsAppTheme.PrefersLight(null), Is.True);
            Assert.That(OsAppTheme.PrefersLight(1), Is.True);
            Assert.That(OsAppTheme.ThemeName(prefersLight: true), Is.EqualTo(OsAppTheme.LightThemeName));
        });
    }

    [Test]
    public void ADarkWindowsSettingSelectsTheDarkTheme()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OsAppTheme.PrefersLight(0), Is.False);
            Assert.That(OsAppTheme.ThemeName(prefersLight: false), Is.EqualTo(OsAppTheme.DarkThemeName));
        });
    }

    [Test]
    public void ADarkDialogBackgroundAsksForADarkCaption()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OsAppTheme.CaptionIsDark(Color.FromArgb(30, 30, 30)), Is.True);
            Assert.That(OsAppTheme.CaptionIsDark(Color.White), Is.False);
            Assert.That(OsAppTheme.CaptionIsDark(Color.Empty), Is.False);
        });
    }
}
