using System;
using System.IO;
using mRemoteNG.UI.Forms;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms;

public class FrmUnhandledExceptionTests
{
    private const string Folder = @"C:\Portable\mRemoteNG";

    [Test]
    public void AMissingShippedAssemblyIsReportedAsAnIncompleteInstallation()
    {
        // #178 arrived as a crash report: the connection tree could not be created because
        // ObjectListView.dll was not in the folder. The loader message names the assembly and
        // explains nothing, so it reads as an application bug.
        FileNotFoundException exception = new(
            "Could not load file or assembly 'ObjectListView, Version=2.9.3.0, Culture=neutral, PublicKeyToken=null'.",
            "ObjectListView, Version=2.9.3.0, Culture=neutral, PublicKeyToken=null");

        string description = FrmUnhandledException.DescribeException(exception, Folder);

        Assert.Multiple(() =>
        {
            Assert.That(description, Does.Contain("ObjectListView.dll"));
            Assert.That(description, Does.Contain(Folder));
            Assert.That(description, Does.Contain(exception.Message),
                        "the original loader message must still be there for the crash report");
        });
    }

    [Test]
    public void AnAssemblyMissingBehindAnotherExceptionIsStillRecognised()
    {
        FileNotFoundException inner = new(
            "Could not load file or assembly 'WeifenLuo.WinFormsUI.Docking, Version=3.1.0.0'.",
            "WeifenLuo.WinFormsUI.Docking, Version=3.1.0.0, Culture=neutral, PublicKeyToken=null");
        TypeInitializationException exception = new("mRemoteNG.UI.Window.ConnectionTreeWindow", inner);

        Assert.That(FrmUnhandledException.DescribeException(exception, Folder),
                    Does.Contain("WeifenLuo.WinFormsUI.Docking.dll"));
    }

    [Test]
    public void AnOrdinaryMissingFileIsNotCalledAnIncompleteInstallation()
    {
        // FileNotFoundException also reports plain data files. Telling someone to reinstall
        // because their connections file moved would be worse than saying nothing.
        FileNotFoundException exception = new("Could not find file 'confCons.xml'.",
                                              @"C:\Portable\mRemoteNG\Settings\confCons.xml");

        Assert.That(FrmUnhandledException.DescribeException(exception, Folder),
                    Is.EqualTo(exception.Message));
    }

    [Test]
    public void AnUnrelatedExceptionIsLeftExactlyAsItIs()
    {
        InvalidOperationException exception = new("Cross-thread operation not valid.");

        Assert.That(FrmUnhandledException.DescribeException(exception, Folder),
                    Is.EqualTo(exception.Message));
    }
}
