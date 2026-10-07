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
        // Create only the windows this diagnostic needs. Pumping unrelated messages here can
        // consume a queued thread-exit message from an earlier form-lifecycle test.
        _ = form.Handle;
        _ = combo.Handle;
        Assert.That(neverShown.IsHandleCreated, Is.False);

        typeof(Control).GetMethod("DestroyHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(combo, null);

        Assert.That(form.IsHandleCreated, Is.True, "the parent must survive the failed recreation");
        Assert.That(combo.IsHandleCreated, Is.False, "the diagnostic requires a missing child window");

        string[] lost = WindowHandleDiagnostics.FindControlsWithoutHandle([form]).ToArray();
        form.Close();

        Assert.Multiple(() =>
        {
            Assert.That(lost, Has.Some.Contains("ComboBox:quickConnect"));
            Assert.That(lost, Has.None.Contains("hiddenPanel"), "a control that was simply never shown is not evidence");
        });
    }

    [Test]
    public void TheBlockNamesTheWin32Error()
    {
        using Form form = new() { Name = "probe" };
        _ = form.Handle;

        string block = WindowHandleDiagnostics.Describe(AWindowCreationFailure(), [form]);

        Assert.That(block, Does.Contain("Win32 error: 1158 (0x486)"));
    }

    private static void DestroyHandleOf(Control control) =>
        typeof(Control).GetMethod("DestroyHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(control, null);

    [Test]
    public void AToolStripComboBoxThatLostItsWindowGetsItBack()
    {
        // #209: the Quick Connect combo box is hosted in a ToolStrip, uses autocomplete, and is
        // the control whose window a DPI change failed to recreate.
        using Form form = new() { Name = "host", ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, -3000) };
        ToolStrip strip = new() { Name = "quickConnectStrip" };
        ToolStripComboBox item = new() { Name = "quickConnect" };
        item.Items.AddRange(["alpha", "beta"]);
        strip.Items.Add(item);
        form.Controls.Add(strip);
        form.Show();
        item.ComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        item.ComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        item.ComboBox.Text = "beta";
        IntPtr before = item.ComboBox.Handle;
        Assert.That(item.ComboBox.Visible, Is.True, "precondition: shown before the loss");

        DestroyHandleOf(item.ComboBox);
        Assert.That(item.ComboBox.IsHandleCreated, Is.False, "the repair needs a missing window");

        var results = WindowHandleDiagnostics.RecreateLostComboBoxes([form]);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Does.EndWith("window recreated"));
            Assert.That(item.ComboBox.IsHandleCreated, Is.True);
            Assert.That(item.ComboBox.Handle, Is.Not.EqualTo(before));
            Assert.That(item.ComboBox.Visible, Is.True, "a recreated combo box must be shown again");
            Assert.That(item.Placement, Is.EqualTo(ToolStripItemPlacement.Main), "and laid out on its strip again");
            Assert.That(item.ComboBox.Text, Is.EqualTo("beta"));
            Assert.That(WindowHandleDiagnostics.FindControlsWithoutHandle([form]), Is.Empty);
        });
        form.Close();
    }

    [Test]
    public void NothingIsRecreatedWhenNoWindowIsMissing()
    {
        using Form form = new() { Name = "host", ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, -3000) };
        ComboBox combo = new() { Name = "fine" };
        form.Controls.Add(combo);
        _ = form.Handle;
        _ = combo.Handle;

        Assert.That(WindowHandleDiagnostics.RecreateLostComboBoxes([form]), Is.Empty);
    }

    [Test]
    public void TheRuntimeLineNamesDotNetAndTheWinFormsBuild() =>
        Assert.That(WindowHandleDiagnostics.RuntimeLine(), Does.StartWith(".NET: ").And.Contain("WinForms "));
}
