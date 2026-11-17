using System.Reflection;
using System.Threading;
using mRemoteNG.Connection;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls.ConnectionInfoPropertyGrid;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ConnectionInfoPropertyGridRefreshTests
    {
        [TestCase(true, PropertyMode.Connection, 1)]
        [TestCase(false, PropertyMode.Connection, 2)]
        [TestCase(false, PropertyMode.DefaultConnection, 1)]
        public void FilteringRefreshesOnlyThroughFilterSetters(bool root, PropertyMode mode, int expectedRefreshes)
        {
            using var grid = new CountingPropertyGrid();
            grid.SelectedConnectionInfo = root
                ? new RootNodeInfo(RootNodeType.Connection)
                : new ConnectionInfo();
            grid.PropertyMode = mode;
            grid.RefreshCount = 0;

            typeof(ConnectionInfoPropertyGrid)
                .GetMethod("ShowHideGridItems", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(grid, null);

            Assert.That(grid.RefreshCount, Is.EqualTo(expectedRefreshes));
            if (root)
                Assert.That(grid.VisibleProperties, Does.Contain(nameof(RootNodeInfo.Name)));
            else if (mode == PropertyMode.DefaultConnection)
                Assert.That(grid.VisibleProperties, Does.Not.Contain(nameof(ConnectionInfo.Hostname)));
            else
                Assert.That(grid.VisibleProperties, Does.Contain(nameof(ConnectionInfo.Name)));
        }

        private sealed class CountingPropertyGrid : ConnectionInfoPropertyGrid
        {
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public int RefreshCount { get; set; }

            public override void Refresh()
            {
                RefreshCount++;
                base.Refresh();
            }
        }
    }
}
