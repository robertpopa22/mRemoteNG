using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App.Diagnostics;
using NUnit.Framework;

namespace mRemoteNGTests.App;

/// <summary>
/// The evidence block added to "Error creating window handle" crash reports (#174, #183, #197).
/// </summary>
[Apartment(ApartmentState.STA)]
public class WindowHandleDiagnosticsTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateHandleThatFails() => throw new Win32Exception(1158, "Error creating window handle.");

    private static Win32Exception AWindowCreationFailure()
    {
        try
        {
            CreateHandleThatFails();
        }
        catch (Win32Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("unreachable");
    }

    [Test]
    public void AWin32ExceptionThrownWhileCreatingAHandleIsRecognised() =>
        Assert.That(WindowHandleDiagnostics.IsWindowCreationFailure(AWindowCreationFailure()), Is.True);

    [Test]
    public void OtherExceptionsGetNoBlock()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WindowHandleDiagnostics.Describe(new FileNotFoundException("x")), Is.Empty);
            Assert.That(WindowHandleDiagnostics.Describe(new Win32Exception(5, "not about handles")), Is.Empty,
                        "a Win32Exception that was never thrown from CreateHandle is not this crash");
            Assert.That(WindowHandleDiagnostics.Describe(null), Is.Empty);
        });
    }

    [Test]
    public void TheBlockReportsObjectCountsAndTheFormsDpi()
    {
        using Form form = new() { Name = "probe" };
        _ = form.Handle;

        string block = WindowHandleDiagnostics.Describe(AWindowCreationFailure(), [form]);

        Assert.Multiple(() =>
        {
            Assert.That(block, Does.StartWith("Window handle diagnostics:"));
            Assert.That(block, Does.Contain("USER objects:").And.Contain("GDI objects:"));
            Assert.That(block, Does.Contain("DeviceDpi"));
        });
    }

    [Test]
    public void AComboBoxLeftWithoutItsWindowIsNamed_AndAHiddenControlIsNot()
    {
        // What RecreateHandleCore leaves behind when CreateHandle throws: a control with no window
        // under a parent that still has one. Destroying the window also makes WinForms report the
        // combo as not Visible, which is why the scan does not rely on Visible for combo boxes.
        using Form form = new() { Name = "host", ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, -3000) };
        ComboBox combo = new() { Name = "quickConnect" };
        Panel neverShown = new() { Name = "hiddenPanel", Visible = false };
        form.Controls.Add(combo);
        form.Controls.Add(neverShown);
        form.Show();
        Application.DoEvents();

        typeof(Control).GetMethod("DestroyHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(combo, null);

        string[] lost = WindowHandleDiagnostics.FindControlsWithoutHandle([form]).ToArray();
        form.Close();

        Assert.Multiple(() =>
        {
            Assert.That(lost, Has.Some.Contains("ComboBox:quickConnect"));
            Assert.That(lost, Has.None.Contains("hiddenPanel"), "a control that was simply never shown is not evidence");
        });
    }

    [Test]
    public void TheRuntimeLineNamesDotNetAndTheWinFormsBuild() =>
        Assert.That(WindowHandleDiagnostics.RuntimeLine(), Does.StartWith(".NET: ").And.Contain("WinForms "));
}
