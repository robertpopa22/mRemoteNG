using System;
using System.Security;
using mRemoteNG.Connection;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Security;

[TestFixture]
public class MasterPasswordGateTests
{
    private Func<string?, Optional<SecureString>> _originalPrompt;

    [SetUp]
    public void Setup()
    {
        _originalPrompt = MasterPasswordGate.PasswordPrompt;
    }

    [TearDown]
    public void Teardown()
    {
        MasterPasswordGate.PasswordPrompt = _originalPrompt;
    }

    private static ConnectionInfo BuildConnectionUnderRoot(RootNodeInfo root)
    {
        var connection = new ConnectionInfo();
        root.AddChild(connection);
        return connection;
    }

    private static Optional<SecureString> Secure(string password)
    {
        return new Optional<SecureString>(password.ConvertToSecureString());
    }

    [Test]
    public void PassesThroughWithoutPromptWhenNoCustomMasterPasswordIsSet()
    {
        var root = new RootNodeInfo(RootNodeType.Connection);
        ConnectionInfo connection = BuildConnectionUnderRoot(root);
        MasterPasswordGate.PasswordPrompt = _ => throw new InvalidOperationException("Prompt must not be shown");

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.True);
    }

    [Test]
    public void CorrectMasterPasswordGrantsAccess()
    {
        var root = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "s3cret" };
        ConnectionInfo connection = BuildConnectionUnderRoot(root);
        MasterPasswordGate.PasswordPrompt = _ => Secure("s3cret");

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.True);
    }

    [Test]
    public void WrongMasterPasswordDeniesAccess()
    {
        var root = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "s3cret" };
        ConnectionInfo connection = BuildConnectionUnderRoot(root);
        MasterPasswordGate.PasswordPrompt = _ => Secure("nope");

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.False);
    }

    [Test]
    public void CancelledPromptDeniesAccess()
    {
        var root = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "s3cret" };
        ConnectionInfo connection = BuildConnectionUnderRoot(root);
        MasterPasswordGate.PasswordPrompt = _ => Optional<SecureString>.Empty;

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.False);
    }

    [Test]
    public void EmptyPasswordEntryDeniesAccess()
    {
        var root = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "s3cret" };
        ConnectionInfo connection = BuildConnectionUnderRoot(root);
        MasterPasswordGate.PasswordPrompt = _ => Secure(string.Empty);

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.False);
    }

    [Test]
    public void ResolvesTheRootTheConnectionBelongsToNotAnotherLoadedRoot()
    {
        // A second loaded file with a custom password must not gate connections that
        // live under an unprotected root (and vice versa) — the gate resolves the
        // connection's own root via GetRootParent (multi-root bypass fix, #128).
        var unprotectedRoot = new RootNodeInfo(RootNodeType.Connection);
        ConnectionInfo connection = BuildConnectionUnderRoot(unprotectedRoot);
        _ = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "s3cret" };
        MasterPasswordGate.PasswordPrompt = _ => throw new InvalidOperationException("Prompt must not be shown");

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.True);
    }

    [Test]
    public void PasswordProtectedRootPromptsEvenWhenEntryEqualsDefaultKey()
    {
        // Entering the public legacy key must not unlock a custom-password file.
        var root = new RootNodeInfo(RootNodeType.Connection) { PasswordString = "s3cret" };
        ConnectionInfo connection = BuildConnectionUnderRoot(root);
        MasterPasswordGate.PasswordPrompt = _ => Secure(ConnectionFileDefaults.LegacyEncryptionKey);

        Assert.That(MasterPasswordGate.VerifyMasterPasswordIfSet(connection), Is.False);
    }
}
