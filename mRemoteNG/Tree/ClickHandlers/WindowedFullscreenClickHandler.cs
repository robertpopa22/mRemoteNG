using System;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;

namespace mRemoteNG.Tree.ClickHandlers;

public sealed class WindowedFullscreenClickHandler(IConnectionInitiator connectionInitiator)
    : ITreeNodeClickHandler<ConnectionInfo>
{
    public static bool CanOpen(ConnectionInfo? node) =>
        node is { IsContainer: false, IsTemplate: false, Protocol: ProtocolType.RDP }
        && node.GetTreeNodeType() == TreeNodeType.Connection;

    internal static bool IsGesture(MouseButtons button, int clicks, Keys modifiers) =>
        button == MouseButtons.Left && clicks == 1 && modifiers == Keys.Alt;

    public void Execute(ConnectionInfo clickedNode)
    {
        ArgumentNullException.ThrowIfNull(clickedNode);
        if (CanOpen(clickedNode))
            connectionInitiator.OpenConnection(clickedNode, ConnectionInfo.Force.WindowedFullscreen);
    }
}
