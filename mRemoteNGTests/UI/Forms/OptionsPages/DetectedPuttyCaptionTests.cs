using mRemoteNG.UI.Forms.OptionsPages;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms.OptionsPages;

public class DetectedPuttyCaptionTests
{
    [Test]
    public void ADetectedFileIsNamedOnTheAdvancedPage()
    {
        string text = DetectedPuttyCaption.For(
            @"C:\Tools\putty.exe", true, "Detected PuTTY:", "(not detected)");

        Assert.That(text, Is.EqualTo(@"Detected PuTTY: C:\Tools\putty.exe"));
    }

    [Test]
    public void AMissingDetectionSaysItWasNotFound()
    {
        string text = DetectedPuttyCaption.For(
            @"C:\missing\putty.exe", false, "Detected PuTTY:", "(not detected)");

        Assert.That(text, Is.EqualTo("(not detected)"));
    }
}
