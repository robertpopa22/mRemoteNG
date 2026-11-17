using System.Globalization;
using mRemoteNG.App;
using NUnit.Framework;

namespace mRemoteNGTests.App;

public class SupportedCulturesTests
{
    // #232: the static table replaced a CultureInfo + NativeName per language. If .NET or ICU
    // ever renders a name differently, this says which row to update.
    [Test]
    public void TheStaticNativeNamesMatchWhatCultureInfoProduces()
    {
        Assert.Multiple(() =>
        {
            foreach ((string name, string nativeName) in SupportedCultures.KnownNativeNamesForTests)
            {
                CultureInfo culture = new(name);
                Assert.That(nativeName, Is.EqualTo(culture.TextInfo.ToTitleCase(culture.NativeName)), name);
            }
        });
    }

    [Test]
    public void EveryShippedLanguageHasAStaticRow()
    {
        foreach (string name in mRemoteNG.Properties.AppUI.Default.SupportedUICultures.Split(','))
            Assert.That(SupportedCultures.KnownNativeNamesForTests.ContainsKey(name.Trim()), Is.True, name);
    }
}
