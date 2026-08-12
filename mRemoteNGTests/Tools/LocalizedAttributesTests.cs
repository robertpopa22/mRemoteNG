using System.Globalization;
using System.Threading;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    /// <summary>
    /// The connection property grid must follow the UI language (CurrentUICulture), like every
    /// other string in the app, and not the regional format setting (CurrentCulture). Resolving
    /// against CurrentCulture produced a property grid in the region's language while the rest of
    /// the UI stayed in the display language. (#162)
    /// </summary>
    [TestFixture]
    public class LocalizedAttributesTests
    {
        private CultureInfo _originalCulture;
        private CultureInfo _originalUiCulture;

        [SetUp]
        public void Setup()
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUiCulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
        }

        [TearDown]
        public void Teardown()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUiCulture;
        }

        [Test]
        public void DisplayNameFollowsUiCultureNotRegionalFormat()
        {
            var attribute = new LocalizedAttributes.LocalizedDisplayNameAttribute("Name");

            Assert.That(attribute.DisplayName, Is.EqualTo("Name"));
        }

        [Test]
        public void DescriptionFollowsUiCultureNotRegionalFormat()
        {
            var attribute = new LocalizedAttributes.LocalizedDescriptionAttribute("PropertyDescriptionName");

            Assert.That(attribute.Description,
                        Is.EqualTo("This is the name that will be displayed in the connections tree."));
        }

        [Test]
        public void DisplayNameStillHonoursAnExplicitlySelectedUiLanguage()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("it-IT");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var attribute = new LocalizedAttributes.LocalizedDisplayNameAttribute("Name");

            Assert.That(attribute.DisplayName, Is.EqualTo("Nome"));
        }

        [Test]
        public void CategoryFollowsUiCultureNotRegionalFormat()
        {
            var attribute = new LocalizedAttributes.LocalizedCategoryAttribute("Display");

            Assert.That(attribute.Category.TrimStart('\t'), Is.EqualTo("Display"));
        }
    }
}
