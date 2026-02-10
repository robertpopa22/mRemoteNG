using System;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.UI.Controls.ConnectionTree;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Controls
{
    [TestFixture]
    public class ConnectionTreeSearchTextFilterTests
    {
        private ConnectionTreeSearchTextFilter _filter;
        private ConnectionInfo _rdpConnection;
        private ConnectionInfo _sshConnection;

        [SetUp]
        public void Setup()
        {
            _filter = new ConnectionTreeSearchTextFilter();
            
            _rdpConnection = new ConnectionInfo
            {
                Name = "Windows Server",
                Hostname = "rdp-host",
                Protocol = ProtocolType.RDP,
                EnvironmentTags = "production, core"
            };

            _sshConnection = new ConnectionInfo
            {
                Name = "Linux Box",
                Hostname = "ssh-host",
                Protocol = ProtocolType.SSH2,
                EnvironmentTags = "staging, web"
            };
        }

        [Test]
        public void Filter_WithEmptyText_ReturnsTrue()
        {
            _filter.FilterText = "";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
        }

        [Test]
        public void Filter_ByName_MatchesSubstring()
        {
            _filter.FilterText = "Windows";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
            Assert.That(_filter.Filter(_sshConnection), Is.False);
        }

        [Test]
        public void Filter_ByHostname_MatchesSubstring()
        {
            _filter.FilterText = "rdp";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
            Assert.That(_filter.Filter(_sshConnection), Is.False);
        }

        [Test]
        public void Filter_ByProtocolPrefix_MatchesProtocolName()
        {
            _filter.FilterText = "protocol:rdp";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
            Assert.That(_filter.Filter(_sshConnection), Is.False);
        }

        [Test]
        public void Filter_ByProtocolPrefix_MatchesSSH2()
        {
            _filter.FilterText = "protocol:ssh2";
            Assert.That(_filter.Filter(_rdpConnection), Is.False);
            Assert.That(_filter.Filter(_sshConnection), Is.True);
        }

        [Test]
        public void Filter_ByTagPrefix_MatchesSingleTag()
        {
            _filter.FilterText = "tag:production";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
            Assert.That(_filter.Filter(_sshConnection), Is.False);
        }

        [Test]
        public void Filter_ByTagPrefix_MatchesPartialTag()
        {
            _filter.FilterText = "tag:prod";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
        }

        [Test]
        public void Filter_ByTagsInNormalSearch_MatchesTags()
        {
            _filter.FilterText = "production";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
        }

        [Test]
        public void Filter_WithProtocolFilterSet_ExcludesNonMatchingProtocols()
        {
            _filter.FilterProtocol = ProtocolType.SSH2;
            _filter.FilterText = "";
            Assert.That(_filter.Filter(_rdpConnection), Is.False);
            Assert.That(_filter.Filter(_sshConnection), Is.True);
        }

        [Test]
        public void Filter_WithSpecialInclusionList_AlwaysReturnsTrueForIncludedItems()
        {
            _filter.SpecialInclusionList.Add(_rdpConnection);
            _filter.FilterText = "something-that-wont-match";
            Assert.That(_filter.Filter(_rdpConnection), Is.True);
        }
    }
}
