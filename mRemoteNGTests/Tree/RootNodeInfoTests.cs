using mRemoteNG.Security;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;


namespace mRemoteNGTests.Tree
{
    public class RootNodeInfoTests
    {
        private RootNodeInfo _rootNodeInfo;

        [SetUp]
        public void Setup()
        {
            _rootNodeInfo = new RootNodeInfo(RootNodeType.Connection);
        }

        [Test]
        public void AutoLockOnMinimizeIsDisabledByDefault()
        {
            Assert.That(_rootNodeInfo.AutoLockOnMinimize, Is.False);
        }

        [Test]
        public void DefaultPasswordReturnsExpectedValue()
        {
            var defaultPassword = _rootNodeInfo.DefaultPassword;
            Assert.That(defaultPassword, Is.EqualTo("mR3m"));
        }

        [TestCase("a", true)]
        [TestCase("mR3m", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void PasswordPropertyReflectsWhetherACustomPasswordIsInUse(string password, bool expected)
        {
            _rootNodeInfo.PasswordString = password;
            Assert.That(_rootNodeInfo.Password, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase(null)]
        public void PasswordStringReturnsDefaultPasswordWhenNoCustomOneIsSet(string password)
        {
            _rootNodeInfo.PasswordString = password;
            Assert.That(_rootNodeInfo.PasswordString, Is.EqualTo(_rootNodeInfo.DefaultPassword));
        }

        [TestCase("a")]
        [TestCase("1234")]
        public void PasswordStringReturnsCustomPassword(string password)
        {
            _rootNodeInfo.PasswordString = password;
            Assert.That(_rootNodeInfo.PasswordString, Is.EqualTo(password));
        }

        [Test]
        public void PasswordStringReturnsDefaultWhenPasswordPropertySetWithoutPasswordString()
        {
            // Edge case: Password property set to true directly without setting PasswordString
            _rootNodeInfo.Password = true;
            Assert.That(_rootNodeInfo.PasswordString, Is.EqualTo(_rootNodeInfo.DefaultPassword));
        }

        [Test]
        public void IsPasswordMatchReturnsTrueForDefaultPasswordWhenNoCustomPasswordSet()
        {
            Assert.That(_rootNodeInfo.IsPasswordMatch(_rootNodeInfo.DefaultPassword.ConvertToSecureString()), Is.True);
        }

        [Test]
        public void IsPasswordMatchReturnsTrueForCustomPasswordEvenWhenPasswordFlagIsFalse()
        {
            _rootNodeInfo.PasswordString = "custom";
            _rootNodeInfo.Password = false;

            Assert.That(_rootNodeInfo.IsPasswordMatch("custom".ConvertToSecureString()), Is.True);
        }

        [Test]
        public void IsPasswordMatchReturnsFalseForWrongPassword()
        {
            _rootNodeInfo.PasswordString = "custom";

            Assert.That(_rootNodeInfo.IsPasswordMatch("wrong".ConvertToSecureString()), Is.False);
        }

        [Test]
        public void IsPasswordMatchReturnsFalseForNullPassword()
        {
            Assert.That(_rootNodeInfo.IsPasswordMatch(null), Is.False);
        }

        [TestCase(RootNodeType.Connection, TreeNodeType.Root)]
        [TestCase(RootNodeType.PuttySessions, TreeNodeType.PuttyRoot)]
        public void RootNodeHasCorrectTreeNodeType(RootNodeType rootNodeType, TreeNodeType expectedTreeNodeType)
        {
            var rootNode = new RootNodeInfo(rootNodeType);
            Assert.That(rootNode.GetTreeNodeType(), Is.EqualTo(expectedTreeNodeType));
        }

        // The connection file is saved when the tree raises PropertyChanged (SaveConnectionsOnEdit,
        // and the dirty flag the exit save reads). Without these notifications turning on password
        // protection never reached disk: the file stayed under the public default key while the UI
        // said it was protected (upstream #3418 / #3592).
        [TestCase(nameof(RootNodeInfo.Password))]
        [TestCase(nameof(RootNodeInfo.PasswordString))]
        [TestCase(nameof(RootNodeInfo.AutoLockOnMinimize))]
        [TestCase(nameof(RootNodeInfo.TotpEnabled))]
        [TestCase(nameof(RootNodeInfo.TotpSecret))]
        public void ChangingASecuritySettingOfTheRootReachesTheTreeModel(string property)
        {
            var model = new ConnectionTreeModel();
            model.AddRootNode(_rootNodeInfo);
            var changed = new System.Collections.Generic.List<string>();
            model.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            switch (property)
            {
                case nameof(RootNodeInfo.Password): _rootNodeInfo.Password = true; break;
                case nameof(RootNodeInfo.PasswordString): _rootNodeInfo.PasswordString = "a-new-master-password"; break;
                case nameof(RootNodeInfo.AutoLockOnMinimize): _rootNodeInfo.AutoLockOnMinimize = true; break;
                case nameof(RootNodeInfo.TotpEnabled): _rootNodeInfo.TotpEnabled = true; break;
                case nameof(RootNodeInfo.TotpSecret): _rootNodeInfo.TotpSecret = "JBSWY3DPEHPK3PXP"; break;
            }

            Assert.That(changed, Does.Contain(property));
        }

        [Test]
        public void ClearingPasswordStringRaisesPropertyChanged()
        {
            _rootNodeInfo.PasswordString = "a-new-master-password";
            var changed = new System.Collections.Generic.List<string>();
            _rootNodeInfo.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            _rootNodeInfo.PasswordString = "";

            Assert.That(changed, Does.Contain(nameof(RootNodeInfo.PasswordString)).And.Contain(nameof(RootNodeInfo.Password)));
            Assert.That(_rootNodeInfo.Password, Is.False);
        }

        [Test]
        public void SettingAnUnchangedValueRaisesNothing()
        {
            _rootNodeInfo.PasswordString = "same";
            var changed = new System.Collections.Generic.List<string>();
            _rootNodeInfo.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            _rootNodeInfo.PasswordString = "same";

            Assert.That(changed, Is.Empty);
        }
    }
}
