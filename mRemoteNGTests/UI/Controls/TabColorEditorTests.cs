using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;

using mRemoteNG.Connection;
using mRemoteNG.UI.Controls.ConnectionInfoPropertyGrid;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    /// <summary>
    /// #176: picking a colour threw "Object of type 'System.Drawing.Color' cannot be converted to
    /// type 'System.String'." The PropertyGrid assigns whatever the editor returns straight through
    /// the PropertyDescriptor, so the editor — not the TypeConverter — has to speak the property's
    /// language.
    /// </summary>
    public class TabColorEditorTests
    {
        [TestCase("Color")]
        [TestCase("TabColor")]
        public void ColourPropertyUsesAnEditorThatReturnsWhatTheSetterAccepts(string propertyName)
        {
            PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(ConnectionInfo))[propertyName]!;
            Assert.That(property, Is.Not.Null);

            var editor = property.GetEditor(typeof(UITypeEditor));

            Assert.That(editor, Is.InstanceOf<TabColorEditor>(),
                        "the stock ColorEditor hands back a Color, which cannot be assigned to a string property");
        }

        [TestCase("Color")]
        [TestCase("TabColor")]
        public void AssigningThroughThePropertyDescriptorSucceedsForAPickedColour(string propertyName)
        {
            ConnectionInfo connection = new();
            PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(ConnectionInfo))[propertyName]!;

            // What the grid does with an editor's result: convert nothing, just assign it.
            object? edited = EditorResultFor(Color.Firebrick);

            Assert.DoesNotThrow(() => property.SetValue(connection, edited));
            Assert.That(property.GetValue(connection), Is.EqualTo("Firebrick"));
        }

        [Test]
        public void AnEmptyValueStaysEmptyRatherThanBecomingBlack()
        {
            Assert.That(EditorResultFor(Color.Empty), Is.Empty);
        }

        [Test]
        public void ACustomColourRoundTripsAsHex()
        {
            Assert.That(EditorResultFor(Color.FromArgb(0x12, 0x34, 0x56)), Is.EqualTo("#123456"));
        }

        /// <summary>
        /// The other half of #176: "no color is shown in the color box". The stock editor paints
        /// nothing for a value that is not a Color, so the swatch stayed blank even for connections
        /// that had a colour set.
        /// </summary>
        [Test]
        public void TheSwatchIsPaintedForAStoredColour()
        {
            Color painted = PaintSwatch("Firebrick");

            Assert.That(painted.ToArgb(), Is.EqualTo(Color.Firebrick.ToArgb()));
        }

        [Test]
        public void TheSwatchStaysBlankWhenNoColourIsSet()
        {
            Color painted = PaintSwatch(string.Empty);

            Assert.That(painted.ToArgb(), Is.EqualTo(Color.White.ToArgb()), "an unset colour must not paint over the cell");
        }

        /// <summary>
        /// Third report of this bug (#156 → #173 → #176), and the review on PR #156 had already
        /// found what the first fix here still did: the stock editor hands back the colour it was
        /// given when the picker is dismissed, and re-encoding that rewrote the stored text —
        /// "crimson" to "Crimson", "#FF0000" to "Red", and an unparseable legacy value to "".
        /// Without an IWindowsFormsEditorService the stock editor echoes its input, which is
        /// exactly the dismiss path, so this runs with no UI.
        /// </summary>
        [TestCase("Crimson")]
        [TestCase("crimson")]
        [TestCase("#FF0000")]
        [TestCase("#DC143C")]
        [TestCase("")]
        [TestCase("not-a-color")]
        public void DismissingThePickerLeavesTheStoredValueExactlyAsItWas(string stored)
        {
            object? result = new TabColorEditor().EditValue(null, new NoEditorService(), stored);

            Assert.That(result, Is.EqualTo(stored));
        }

        [Test]
        public void DismissingThePickerOnANullValueReturnsNull()
        {
            Assert.That(new TabColorEditor().EditValue(null, new NoEditorService(), null), Is.Null);
        }

        /// <summary>
        /// PR #156's other finding: the quick-pick list is committed straight through the
        /// descriptor when the user double-clicks or arrows through it, so for a string property
        /// it has to hold strings — a Color there is #176 by another route.
        /// </summary>
        [Test]
        public void TheStandardValuesAreStringsTheStringPropertyCanTake()
        {
            var converter = new mRemoteNG.Tools.MiscTools.TabColorConverter();
            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(null)!;

            Assert.That(values, Is.Not.Empty);
            foreach (object? value in values)
                Assert.That(value, Is.InstanceOf<string>().And.Not.Empty);
        }

        private sealed class NoEditorService : System.IServiceProvider
        {
            public object? GetService(System.Type serviceType) => null;
        }

        private static Color PaintSwatch(string storedValue)
        {
            var editor = new TabColorEditor();
            using Bitmap bitmap = new(20, 20);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                editor.PaintValue(new PaintValueEventArgs(null, storedValue, graphics, new Rectangle(0, 0, 20, 20)));
            }

            return bitmap.GetPixel(10, 10);
        }

        // The conversion the editor applies to the colour the picker returned.
        private static object? EditorResultFor(Color color)
        {
            var converter = new mRemoteNG.Tools.MiscTools.TabColorConverter();
            return converter.ConvertFrom(null, null, color);
        }
    }
}
