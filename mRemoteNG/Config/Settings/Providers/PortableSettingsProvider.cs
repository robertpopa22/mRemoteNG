// The MIT License (MIT)
//
// Copyright(c) crdx
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// https://github.com/crdx/PortableSettingsProvider
//

using System;
using System.Collections;
using System.Configuration;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Xml;
using System.IO;
using mRemoteNG.App.Info;
using mRemoteNG.Security;

namespace mRemoteNG.Config.Settings.Providers
{
    public class PortableSettingsProvider : SettingsProvider, IApplicationSettingsProvider
    {
        private const string _rootNodeName = "settings";
        private const string _localSettingsNodeName = "localSettings";
        private const string _globalSettingsNodeName = "globalSettings";
        private const string _className = "PortableSettingsProvider";
        private XmlDocument? _xmlDocument;

        /// <summary>
        /// Test hook: when set, the settings are read from and written to this file instead of
        /// the edition-specific settings folder.
        /// </summary>
        internal string? FilePathOverride { get; set; }

        private string _filePath
        {
            get
            {
                if (!string.IsNullOrEmpty(FilePathOverride))
                    return FilePathOverride;

                // Reuse SettingsFileInfo's writable path logic which falls back to
                // %APPDATA% when the exe directory is read-only (e.g. Program Files).
                string settingsDir = SettingsFileInfo.SettingsPath;
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);
                return Path.Combine(settingsDir, $"{ApplicationName}.settings");
            }
        }

        private XmlNode _localSettingsNode => GetSettingsNode(_localSettingsNodeName);

        private XmlNode _globalSettingsNode => GetSettingsNode(_globalSettingsNodeName);

        private XmlNode _rootNode => _rootDocument.SelectSingleNode(_rootNodeName)
            ?? throw new InvalidOperationException("Root settings node not found in XML document");

        private XmlDocument _rootDocument
        {
            get
            {
                if (_xmlDocument != null) return _xmlDocument;
                try
                {
                    _xmlDocument = SecureXmlHelper.LoadXmlFromFile(_filePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PortableSettingsProvider: Failed to load {_filePath}: {ex.Message}");
                }

                if (_xmlDocument?.SelectSingleNode(_rootNodeName) != null)
                    return _xmlDocument;

                _xmlDocument = GetBlankXmlDocument();

                return _xmlDocument;
            }
        }

        public override string ApplicationName
        {
            get => Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            set { }
        }

        public override string Name => _className;

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(Name, config);
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            // ApplicationSettingsBase.Save() hands over every property; when none of them changed
            // there is nothing to persist. Rewriting the file anyway churned mRemoteNG.settings on
            // every connections reload (#210). A missing file is still created.
            // Known limitation: reading a non-primitive property (e.g. a collection) through the
            // settings base class marks its value dirty, so such a read still triggers a rewrite.
            bool anyDirty = false;
            foreach (SettingsPropertyValue propertyValue in collection)
            {
                if (propertyValue.IsDirty)
                {
                    anyDirty = true;
                    break;
                }
            }

            if (!anyDirty && File.Exists(_filePath))
                return;

            foreach (SettingsPropertyValue propertyValue in collection)
                SetValue(propertyValue);

            try
            {
                _rootDocument.Save(_filePath);
                foreach (SettingsPropertyValue propertyValue in collection)
                    propertyValue.IsDirty = false;
            }
            catch (Exception ex)
            {
                // Log but don't crash — the device may have been removed (portable edition).
                System.Diagnostics.Debug.WriteLine($"PortableSettingsProvider: Failed to save {_filePath}: {ex.Message}");
            }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context,
                                                                          SettingsPropertyCollection collection)
        {
            SettingsPropertyValueCollection values = new();

            foreach (SettingsProperty property in collection)
            {
                values.Add(new SettingsPropertyValue(property)
                {
                    SerializedValue = GetValue(property)
                });
            }

            return values;
        }

        private void SetValue(SettingsPropertyValue propertyValue)
        {
            XmlNode targetNode = IsGlobal(propertyValue.Property) ? _globalSettingsNode : _localSettingsNode;

            XmlNode? settingNode = targetNode.SelectSingleNode($"setting[@name='{propertyValue.Name}']");

            if (settingNode != null)
                settingNode.InnerText = propertyValue.SerializedValue?.ToString() ?? string.Empty;
            else
            {
                settingNode = _rootDocument.CreateElement("setting");

                XmlAttribute nameAttribute = _rootDocument.CreateAttribute("name");
                nameAttribute.Value = propertyValue.Name;

                settingNode.Attributes?.Append(nameAttribute);
                settingNode.InnerText = propertyValue.SerializedValue?.ToString() ?? string.Empty;

                targetNode.AppendChild(settingNode);
            }
        }

        private string GetValue(SettingsProperty property)
        {
            XmlNode targetNode = IsGlobal(property) ? _globalSettingsNode : _localSettingsNode;
            XmlNode? settingNode = targetNode.SelectSingleNode($"setting[@name='{property.Name}']");

            if (settingNode == null)
                return property.DefaultValue?.ToString() ?? string.Empty;

            return settingNode.InnerText;
        }

        private static bool IsGlobal(SettingsProperty property)
        {
            foreach (DictionaryEntry attribute in property.Attributes)
            {
                if (attribute.Value is SettingsManageabilityAttribute)
                    return true;
            }

            return false;
        }

        private XmlNode GetSettingsNode(string name)
        {
            XmlNode? settingsNode = _rootNode.SelectSingleNode(name);

            if (settingsNode != null) return settingsNode;
            settingsNode = _rootDocument.CreateElement(name);
            _rootNode.AppendChild(settingsNode);

            return settingsNode;
        }

        private static XmlDocument GetBlankXmlDocument()
        {
            XmlDocument blankXmlDocument = new();
            blankXmlDocument.AppendChild(blankXmlDocument.CreateXmlDeclaration("1.0", "utf-8", string.Empty));
            blankXmlDocument.AppendChild(blankXmlDocument.CreateElement(_rootNodeName));

            return blankXmlDocument;
        }

        public void Reset(SettingsContext context)
        {
            _localSettingsNode.RemoveAll();
            _globalSettingsNode.RemoveAll();

            _xmlDocument?.Save(_filePath);
        }

        public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
        {
            // do nothing
            return new SettingsPropertyValue(property);
        }

        public void Upgrade(SettingsContext context, SettingsPropertyCollection properties)
        {
        }
    }
}