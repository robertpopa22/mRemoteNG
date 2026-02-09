using System.Threading;
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
            // Show() + force native handle creation so Win32 EM_SETCUEBANNER works
            _textBoxExtensionsTestForm.Show();
            _ = _textBoxExtensionsTestForm.textBox1.Handle;
            // Pump the message loop until the control is fully realized
            for (int i = 0; i < 10; i++)
                System.Windows.Forms.Application.DoEvents();
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