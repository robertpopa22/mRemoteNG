using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using mRemoteNG.Messages;
using mRemoteNG.Messages.MessageWriters;
using mRemoteNG.Themes;
using mRemoteNG.UI.Window;
using NUnit.Framework;
using Message = mRemoteNG.Messages.Message;

namespace mRemoteNGTests.Messages.MessageWriters;

[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class NotificationPanelMessageWriterTests
{
    [Test]
    public void StartupAndWorkerMessagesArePostedInOrderToThePanel()
    {
        using var window = new ErrorAndInfoWindow();
        // Keep the singleton theme manager from retaining the disposed test window.
        var applyTheme = typeof(ErrorAndInfoWindow).GetMethod("ApplyTheme",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
        ThemeManager.getInstance().ThemeChanged -= applyTheme.CreateDelegate<ThemeManager.ThemeChangedEventHandler>(window);

        var writer = new NotificationPanelMessageWriter(window);
        ListView list = window.lvErrorCollector;
        var startup = new Message(MessageClass.InformationMsg, "startup");
        var beforeOpen = new Message(MessageClass.WarningMsg, "before opening the panel");
        var afterOpen = new Message(MessageClass.ErrorMsg, "after opening the panel");

        Assert.That(list.IsHandleCreated, Is.False);
        writer.Write(startup);
        WriteFromWorker(writer, beforeOpen);
        Assert.That(list.IsHandleCreated, Is.False);

        _ = list.Handle;
        Assert.That(list.Items.Count, Is.Zero, "The drain must run after HandleCreated returns.");
        Application.DoEvents();
        Assert.That(MessagesIn(list), Is.EqualTo(new[] { beforeOpen, startup }));

        WriteFromWorker(writer, afterOpen);
        Assert.That(list.Items.Count, Is.EqualTo(2), "The worker must post to the panel's thread.");
        Application.DoEvents();
        Assert.That(MessagesIn(list), Is.EqualTo(new[] { afterOpen, beforeOpen, startup }));
    }

    private static void WriteFromWorker(NotificationPanelMessageWriter writer, IMessage message) =>
        Task.Run(() => writer.Write(message)).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

    private static object?[] MessagesIn(ListView list) =>
        list.Items.Cast<ListViewItem>().Select(item => item.Tag).ToArray();
}
