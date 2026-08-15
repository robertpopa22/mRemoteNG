using System;
using System.ComponentModel;
using System.Security;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.Tree.Root
{
    [SupportedOSPlatform("windows")]
    [DefaultProperty("Name")]
    public class RootNodeInfo : ContainerInfo
    {
        private string _name = Language.Connections;
        private string _customPassword = "";

        public RootNodeInfo(RootNodeType rootType, string uniqueId) : base(uniqueId)
        {
            Type = rootType;
            // The base ContainerInfo constructor runs SetDefaults(), whose "New Folder" assignment
            // virtual-dispatches into this class's Name setter and overwrites the field
            // initializer. Only the other constructor used to repair it, so every root created
            // through this one -- which is how the SQL deserializer builds the tree root -- was
            // literally named "New Folder". (#148)
            _name = Language.Connections;
        }

        public RootNodeInfo(RootNodeType rootType)
            : this(rootType, Guid.NewGuid().ToString())
        {
        }

        #region Public Properties

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous)),
         Browsable(true),
         LocalizedAttributes.LocalizedDefaultValue(nameof(Language.Connections)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Name)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionName))]
        // The override exists only to carry the property-grid attributes above, but assigning the
        // backing field directly also dropped the base's change notification. Renaming the root
        // therefore raised no PropertyChanged and queued no save: the new name reached tblRoot
        // only if an unrelated later edit happened to flush the same in-memory instance first,
        // which is why the rename looked like it persisted "sometimes". (#148)
        public override string Name
        {
            get => _name;
            set => SetField(ref _name, value, nameof(Name));
        }
        
        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous)),
         Browsable(true),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.PasswordProtect)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionPasswordProtect)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter))]
        public new bool Password { get; set; }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous)),
         Browsable(true),
         DisplayName("Auto lock on minimize"),
         Description("Require master password when restoring the app after minimize."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter))]
        public bool AutoLockOnMinimize { get; set; }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous)),
         Browsable(true),
         DisplayName("Two-Factor Authentication (TOTP)"),
         Description("Require a TOTP code from an authenticator app in addition to the master password."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter))]
        public bool TotpEnabled { get; set; }

        [Browsable(false)]
        public string TotpSecret { get; set; } = "";

        [Browsable(false)]
        public string PasswordString
        {
            get => (Password && !string.IsNullOrEmpty(_customPassword)) ? _customPassword : DefaultPassword;
            set
            {
                _customPassword = value;
                Password = !string.IsNullOrEmpty(value) && _customPassword != DefaultPassword;
            }
        }

        [Browsable(false)] public string DefaultPassword { get; } = Security.ConnectionFileDefaults.LegacyEncryptionKey;

        [Browsable(false)]
        public bool IsPasswordMatch(SecureString? providedPassword)
        {
            if (providedPassword == null)
                return false;

            string expectedPassword = string.IsNullOrEmpty(_customPassword) ? DefaultPassword : _customPassword;
            string suppliedPassword = providedPassword.ConvertToUnsecureString();
            return string.Equals(expectedPassword, suppliedPassword, StringComparison.Ordinal);
        }

        [Browsable(false)] public RootNodeType Type { get; set; }

        public override TreeNodeType GetTreeNodeType()
        {
            return Type == RootNodeType.Connection
                ? TreeNodeType.Root
                : TreeNodeType.PuttyRoot;
        }

        [Browsable(false)]
        public string Filename { get; set; } = string.Empty;
        #endregion
    }
}
