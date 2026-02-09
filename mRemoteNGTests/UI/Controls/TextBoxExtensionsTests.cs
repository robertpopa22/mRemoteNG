using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TextBoxExtensionsTests
    {
        private TextBoxExtensionsTestForm _textBoxExtensionsTestForm;

        [SetUp]
        public void Setup()
        {
            _textBoxExtensionsTestForm = new TextBoxExtensionsTestForm();
            _textBoxExtensionsTestForm.Show();
            // Force native window handle creation for Win32 SendMessage
            _ = _textBoxExtensionsTestForm.Handle;
            _ = _textBoxExtensionsTestForm.textBox1.Handle;
            Application.DoEvents();
            // Skip test gracefully if Win32 handle could not be created
            Assume.That(_textBoxExtensionsTestForm.textBox1.IsHandleCreated,
                        "TextBox handle not created - batch test environment limitation");
        }

        [TearDown]
        public void Teardown()
        {
            _textBoxExtensionsTestForm.Dispose();
            while (_textBoxExtensionsTestForm.Disposing)
            { }
            _textBoxExtensionsTestForm = null;
        }

        [Test]
        public void SetCueBannerSetsTheBannerText()
        {
            const string text = "Type Here";
            var textBox = _textBoxExtensionsTestForm.textBox1;
            Assert.That(textBox.SetCueBannerText(text), Is.True);
        }

        [Test]
        public void GetCueBannerReturnsCorrectValue()
        {
            const string text = "Type Here";
            var textBox = _textBoxExtensionsTestForm.textBox1;
            textBox.SetCueBannerText(text);
            Assert.That(textBox.GetCueBannerText(), Is.EqualTo(text));
        }
    }
}
