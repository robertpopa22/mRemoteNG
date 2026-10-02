using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol;

[SupportedOSPlatform("windows")]
public class ExternalProcessProtocolBaseTests
{
    #region GetChildProcessIds

    [Test]
    public void GetChildProcessIds_CurrentProcess_ReturnsListWithoutError()
    {
        int currentPid = Environment.ProcessId;

        var children = TestProtocol.TestGetChildProcessIds(currentPid);

        Assert.That(children, Is.Not.Null);
        Assert.That(children, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void GetChildProcessIds_NonExistentPid_ReturnsEmptyList()
    {
        // Use a very high PID that doesn't exist
        var children = TestProtocol.TestGetChildProcessIds(999999999);

        Assert.That(children, Is.Empty);
    }

    #endregion

    #region GetDescendantProcessIds

    [Test]
    public void GetDescendantProcessIds_MaxDepthZero_ReturnsEmpty()
    {
        var descendants = TestProtocol.TestGetDescendantProcessIds(Environment.ProcessId, 0);

        Assert.That(descendants, Is.Empty);
    }

    [Test]
    public void GetDescendantProcessIds_CurrentProcess_ReturnsListWithoutError()
    {
        var descendants = TestProtocol.TestGetDescendantProcessIds(Environment.ProcessId, 3);

        Assert.That(descendants, Is.Not.Null);
        Assert.That(descendants, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void GetDescendantProcessIds_NonExistentPid_ReturnsEmptyList()
    {
        var descendants = TestProtocol.TestGetDescendantProcessIds(999999999, 2);

        Assert.That(descendants, Is.Empty);
    }

    #endregion

    #region PollMainWindowHandle

    [Test]
    public void PollMainWindowHandle_ExitedProcess_ReturnsZero()
    {
        // Start a process that exits immediately
        var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        })!;
        process.WaitForExit(5000);

        var handle = TestProtocol.TestPollMainWindowHandle(process, 100);

        Assert.That(handle, Is.EqualTo(IntPtr.Zero));
        process.Dispose();
    }

    [Test]
    public void PollMainWindowHandle_ZeroTimeout_ReturnsZero()
    {
        var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 2 127.0.0.1 >nul")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        })!;

        // With 0ms timeout, the while loop doesn't execute
        var handle = TestProtocol.TestPollMainWindowHandle(process, 0);

        Assert.That(handle, Is.EqualTo(IntPtr.Zero));

        try { process.Kill(); } catch { /* cleanup */ }
        process.Dispose();
    }

    #endregion

    #region FindWindowByProcessId

    [Test]
    public void FindWindowByProcessId_NonExistentPid_ReturnsZero()
    {
        var handle = TestProtocol.TestFindWindowByProcessId(999999999, 100);

        Assert.That(handle, Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void FindWindowByProcessId_ZeroTimeout_ReturnsZero()
    {
        var handle = TestProtocol.TestFindWindowByProcessId(Environment.ProcessId, 0);

        Assert.That(handle, Is.EqualTo(IntPtr.Zero));
    }

    #endregion

    #region FindWindowInChildProcesses

    [Test]
    public void FindWindowInChildProcesses_NonExistentPid_ReturnsZero()
    {
        var handle = TestProtocol.TestFindWindowInChildProcesses(999999999, 100);

        Assert.That(handle, Is.EqualTo(IntPtr.Zero));
    }

    #endregion

    #region FindWindowInDescendantProcesses

    [Test]
    public void FindWindowInDescendantProcesses_NonExistentPid_ReturnsZero()
    {
        var handle = TestProtocol.TestFindWindowInDescendantProcesses(999999999, 100, 2);

        Assert.That(handle, Is.EqualTo(IntPtr.Zero));
    }

    #endregion

    #region Close

    [Test]
    public void Close_WithNullProcess_DoesNotThrow()
    {
        var sut = new TestProtocol();

        Assert.DoesNotThrow(() => sut.Close());
    }

    [Test]
    public void Close_WithRunningProcess_KillsAndDisposesProcess()
    {
        var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 30 127.0.0.1 >nul")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        })!;
        int pid = process.Id;

        var sut = new TestProtocol();
        sut.SetProcess(process);

        sut.Close();

        // After Close(), the Process object is disposed (can't check HasExited).
        // Verify the process was killed by checking it no longer exists.
        bool stillRunning;
        try { stillRunning = Process.GetProcessById(pid) != null; }
        catch (ArgumentException) { stillRunning = false; }
        Assert.That(stillRunning, Is.False);
    }

    [Test]
    public void Close_WithExitedProcess_DoesNotThrow()
    {
        var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        })!;
        process.WaitForExit(5000);

        var sut = new TestProtocol();
        sut.SetProcess(process);

        Assert.DoesNotThrow(() => sut.Close());
    }

    #endregion

    #region Focus

    [Test]
    public void Focus_WithZeroHandle_DoesNotThrow()
    {
        var sut = new TestProtocol();
        sut.SetHandle(IntPtr.Zero);

        Assert.DoesNotThrow(() => sut.Focus());
    }

    [Test]
    public void Focus_WithInvalidHandle_DoesNotThrow()
    {
        var sut = new TestProtocol();
        sut.SetHandle(new IntPtr(0x12345));

        // SetForegroundWindow with invalid handle just returns false — no exception
        Assert.DoesNotThrow(() => sut.Focus());
    }

    #endregion

    #region Embeddable window

    [Test]
    public void APlaceholderConsoleClassIsNotDockable()
    {
        Assert.That(ExternalProcessProtocolBase.IsDockableWindowClass("PseudoConsoleWindow"), Is.False);
        Assert.That(ExternalProcessProtocolBase.IsDockableWindowClass("Notepad"), Is.True);
    }

    [Test]
    public void AZeroHandleIsNotEmbeddable()
    {
        Assert.That(TestProtocol.TestIsEmbeddableWindow(IntPtr.Zero), Is.False);
    }

    [Test]
    [Apartment(System.Threading.ApartmentState.STA)]
    public void AVisibleFormIsEmbeddable()
    {
        using var form = new System.Windows.Forms.Form { Text = "embeddable probe" };
        form.Show();
        try
        {
            Assert.That(TestProtocol.TestIsEmbeddableWindow(form.Handle), Is.True);
        }
        finally
        {
            form.Close();
        }
    }

    #endregion

    #region ProcessExited

    [Test]
    public void ProcessExited_FiresClosedEvent()
    {
        var sut = new TestProtocol();
        bool closedFired = false;
        sut.Closed += (sender) => closedFired = true;

        sut.TestProcessExited(sut, EventArgs.Empty);

        Assert.That(closedFired, Is.True);
    }

    #endregion

    #region Test Double

    [SupportedOSPlatform("windows")]
    internal sealed class TestProtocol : ExternalProcessProtocolBase
    {
        public static List<int> TestGetChildProcessIds(int parentPid)
            => GetChildProcessIds(parentPid);

        public static List<int> TestGetDescendantProcessIds(int rootPid, int maxDepth)
            => GetDescendantProcessIds(rootPid, maxDepth);

        public static IntPtr TestPollMainWindowHandle(Process process, int timeoutMs)
            => PollMainWindowHandle(process, timeoutMs);

        public static IntPtr TestFindWindowByProcessId(int processId, int timeoutMs)
            => FindWindowByProcessId(processId, timeoutMs);

        public static IntPtr TestFindWindowInChildProcesses(int parentPid, int timeoutMs)
            => FindWindowInChildProcesses(parentPid, timeoutMs);

        public static IntPtr TestFindWindowInDescendantProcesses(int rootPid, int timeoutMs, int maxDepth)
            => FindWindowInDescendantProcesses(rootPid, timeoutMs, maxDepth);

        public static bool TestIsEmbeddableWindow(IntPtr hWnd) => IsEmbeddableWindow(hWnd);

        public void SetProcess(Process? process) => _process = process;
        public void SetHandle(IntPtr handle) => _handle = handle;

        public void TestProcessExited(object sender, EventArgs e)
            => ProcessExited(sender, e);
    }

    #endregion
}
