using System;
using System.IO;
using NUnit.Framework;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;
using System.Drawing; // For Color
using System.Collections.Generic; // For Dictionary

namespace mRemoteNGTests.Themes
{
    [TestFixture]
    public class ThemeSerializerTests
    {
        private string _tempDirectory;

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_tempDirectory, true);
        }

        [Test]
        public void SaveAndLoadThemeInfo_RoundTripsCorrectly()
        {
            // 1. Arrange
            var baseThemeName = "BaseTheme";
            var customThemeName = "CustomTheme";
            var baseThemePath = Path.Combine(_tempDirectory, baseThemeName + ".vstheme");
            var customThemePath = Path.Combine(_tempDirectory, customThemeName + ".vstheme");

            // Create a minimal base .vstheme file content
            var baseThemeContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Theme version=""1.0"">
  <Properties>
    <Property name=""DockPanel:BackColor"" value=""#FF111111"" />
  </Properties>
  <ExtendedPalette>
    <Color Name=""Accent"" A=""255"" R=""100"" G=""100"" B=""100"" />
  </ExtendedPalette>
</Theme>";
            File.WriteAllText(baseThemePath, baseThemeContent);

            // Mock ThemeBase (required by ThemeInfo constructor)
            var mockThemeBase = new VS2015BlueTheme(); // Using a concrete theme for simplicity

            // Create base ThemeInfo
            var baseExtendedPalette = new ExtendedColorPalette();
            baseExtendedPalette.ExtColorPalette.Add("Accent", Color.FromArgb(255, 100, 100, 100)); // Example color
            var baseThemeInfo = new ThemeInfo(baseThemeName, mockThemeBase, baseThemePath, VisualStudioToolStripExtender.VsVersion.Vs2015, baseExtendedPalette);

            // Create custom ThemeInfo with a different color to be saved
            var customExtendedPalette = new ExtendedColorPalette();
            customExtendedPalette.ExtColorPalette.Add("PrimaryBackground", Color.FromArgb(255, 50, 50, 50));
            customExtendedPalette.ExtColorPalette.Add("SecondaryHighlight", Color.FromArgb(255, 200, 200, 200));
            var customThemeInfo = new ThemeInfo(customThemeName, mockThemeBase, "dummy", VisualStudioToolStripExtender.VsVersion.Vs2015, customExtendedPalette);
            customThemeInfo.URI = customThemePath; // Set URI for the custom theme

            // 2. Act - Save the custom theme based on the base theme
            ThemeSerializer.SaveToXmlFile(customThemeInfo, baseThemeInfo);
            ThemeSerializer.UpdateThemeXMLValues(customThemeInfo); // Update the XML with the custom palette

            // 3. Act - Load the custom theme
            var loadedThemeInfo = ThemeSerializer.LoadFromXmlFile(customThemePath);

            // 4. Assert
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(customThemePath), Is.True, "Custom theme file should exist.");
                Assert.That(loadedThemeInfo.Name, Is.EqualTo(customThemeName), "Loaded theme name should match.");
                Assert.That(loadedThemeInfo.URI, Is.EqualTo(customThemePath), "Loaded theme URI should match.");
                Assert.That(loadedThemeInfo.ExtendedPalette, Is.Not.Null, "Loaded theme ExtendedPalette should not be null.");

                // Verify a specific color from the custom palette (more robust check)
                Assert.That(loadedThemeInfo.ExtendedPalette.ExtColorPalette.ContainsKey("PrimaryBackground"), Is.True, "Loaded palette should contain PrimaryBackground.");
                Assert.That(loadedThemeInfo.ExtendedPalette.ExtColorPalette["PrimaryBackground"], Is.EqualTo(Color.FromArgb(255, 50, 50, 50)), "PrimaryBackground color should match.");
                Assert.That(loadedThemeInfo.ExtendedPalette.ExtColorPalette.ContainsKey("SecondaryHighlight"), Is.True, "Loaded palette should contain SecondaryHighlight.");
                Assert.That(loadedThemeInfo.ExtendedPalette.ExtColorPalette["SecondaryHighlight"], Is.EqualTo(Color.FromArgb(255, 200, 200, 200)), "SecondaryHighlight color should match.");
            });
        }
    }
}
