using System.Runtime.Versioning;
using mRemoteNG.Tools.Cmdline;
using NUnit.Framework;

namespace mRemoteNGTests.Tools.Cmdline;

[SupportedOSPlatform("windows")]
public class CmdArgumentsInterpreterTests
{
    [Test]
    public void SpaceSeparatedWindowsPathStaysWhole()
    {
        CmdArgumentsInterpreter args = new(["--cons", @"C:\Users\a\confCons.xml"]);

        Assert.That(args["cons"], Is.EqualTo(@"C:\Users\a\confCons.xml"));
    }

    [Test]
    public void AttachedColonWindowsPathStaysWhole()
    {
        CmdArgumentsInterpreter args = new([@"/cons:C:\Users\a\confCons.xml"]);

        Assert.That(args["cons"], Is.EqualTo(@"C:\Users\a\confCons.xml"));
    }

    [Test]
    public void QuotedAttachedValueKeepsInnerColons()
    {
        CmdArgumentsInterpreter args = new(["/param3:\"Test-:-work\""]);

        Assert.That(args["param3"], Is.EqualTo("Test-:-work"));
    }

    [Test]
    public void BareSwitchIsTrue()
    {
        CmdArgumentsInterpreter args = new(["--param2"]);

        Assert.That(args["param2"], Is.EqualTo("true"));
    }

    [Test]
    public void EqualsAttachedValueIsTaken()
    {
        CmdArgumentsInterpreter args = new(["/param4=happy"]);

        Assert.That(args["param4"], Is.EqualTo("happy"));
    }
}
