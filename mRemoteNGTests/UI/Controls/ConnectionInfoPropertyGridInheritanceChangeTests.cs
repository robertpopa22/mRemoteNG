using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.UI.Controls.ConnectionInfoPropertyGrid;
using NUnit.Framework;
using PropertyGridControl = mRemoteNG.UI.Controls.ConnectionInfoPropertyGrid.ConnectionInfoPropertyGrid;

namespace mRemoteNGTests.UI.Controls
{
    /// <summary>
    /// An edit made in the grid's inheritance view tells the listeners of the edited connections,
    /// so the save after every edit and the unsaved-changes flag see it (upstream #3570).
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ConnectionInfoPropertyGridInheritanceChangeTests
    {
        [Test]
        public void EditingTheInheritanceOfAConnectionRaisesAnInheritanceChange()
        {
            ConnectionInfo connection = new();
            using TestablePropertyGrid grid = new();
            grid.SelectedConnectionInfo = connection;
            grid.PropertyMode = PropertyMode.Inheritance;
            List<string> changedProperties = [];
            connection.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            grid.RaisePropertyValueChanged();

            Assert.That(changedProperties, Is.EqualTo(new[] { nameof(ConnectionInfo.Inheritance) }));
        }

        [Test]
        public void EditingTheInheritanceOfSeveralConnectionsRaisesAChangeOnEachOfThem()
        {
            ConnectionInfo first = new();
            ConnectionInfo second = new();
            using TestablePropertyGrid grid = new();
            grid.SelectedConnectionInfos = new[] { first, second };
            grid.PropertyMode = PropertyMode.Inheritance;
            List<string> firstChanges = [];
            List<string> secondChanges = [];
            first.PropertyChanged += (sender, args) => firstChanges.Add(args.PropertyName);
            second.PropertyChanged += (sender, args) => secondChanges.Add(args.PropertyName);

            grid.RaisePropertyValueChanged();

            Assert.That(firstChanges, Is.EqualTo(new[] { nameof(ConnectionInfo.Inheritance) }));
            Assert.That(secondChanges, Is.EqualTo(new[] { nameof(ConnectionInfo.Inheritance) }));
        }

        [Test]
        public void EditingAConnectionPropertyRaisesNoInheritanceChange()
        {
            ConnectionInfo connection = new();
            using TestablePropertyGrid grid = new();
            grid.SelectedConnectionInfo = connection;
            grid.PropertyMode = PropertyMode.Connection;
            List<string> changedProperties = [];
            connection.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            grid.RaisePropertyValueChanged();

            Assert.That(changedProperties, Has.None.EqualTo(nameof(ConnectionInfo.Inheritance)));
        }

        private sealed class TestablePropertyGrid : PropertyGridControl
        {
            public void RaisePropertyValueChanged() => OnPropertyValueChanged(new PropertyValueChangedEventArgs(null, false));
        }
    }
}
