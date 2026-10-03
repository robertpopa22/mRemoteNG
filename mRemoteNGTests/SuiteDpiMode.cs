using System;
using System.Windows.Forms;
using NUnit.Framework;

[SetUpFixture]
public sealed class SuiteDpiMode
{
    [OneTimeSetUp]
    public void UsePerMonitorDpi()
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        }
        catch (InvalidOperationException)
        {
            // A window already exists. The arrow test still sets the thread context.
        }
    }
}
