using System.Collections.Generic;
using System.Reflection;
using mRemoteNG.Connection.Protocol.VNC;
using NUnit.Framework;
using VncSharpCore;

namespace mRemoteNGTests.Connection.Protocol.VNC
{
    [TestFixture]
    public class VncKeyTranslationTablePatchTests
    {
        private const int VK_CAPITAL = 0x14;
        private const int XK_Caps_Lock = 0xFFE5;

        [Test]
        public void PatchMapsCapsLockToTheX11CapsLockKeysym()
        {
            MethodInfo? patch = typeof(ProtocolVNC)
                .GetMethod("PatchVncKeyTranslationTable", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(patch, Is.Not.Null, "PatchVncKeyTranslationTable not found on ProtocolVNC");

            patch!.Invoke(null, null);

            FieldInfo? tableField = typeof(RemoteDesktop)
                .GetField("KeyTranslationTable", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(tableField, Is.Not.Null, "KeyTranslationTable not found on RemoteDesktop");
            var table = tableField!.GetValue(null) as Dictionary<int, int>;
            Assert.That(table, Is.Not.Null, "KeyTranslationTable is not a Dictionary<int, int>");

            // This table is the fallback when VncLockKeyFilter lets the message through.
            // 0xFF20 is XK_Multi_key (compose), which an earlier revision sent here.
            Assert.That(table!.TryGetValue(VK_CAPITAL, out int keysym), Is.True);
            Assert.That(keysym, Is.EqualTo(XK_Caps_Lock));
        }
    }
}
