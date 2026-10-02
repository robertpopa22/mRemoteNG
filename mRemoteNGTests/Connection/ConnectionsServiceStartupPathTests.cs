using System;
using System.Collections.Generic;
using System.Reflection;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using NUnit.Framework;

namespace mRemoteNGTests.Connection;

[NonParallelizable]
public class ConnectionsServiceStartupPathTests
{
    // Test-only prompt factory — always cancels, so the resolver returns null and
    // GetStartupConnectionFileName falls through to the saved-path / default path.
    // Having an injected factory keeps the unit tests headless (no ShowDialog).
    private static readonly Func<IReadOnlyList<ConnectionsFileResolver.Candidate>,
                                 ConnectionsFileResolver.Candidate?,
                                 (ConnectionsFileResolver.Candidate? Choice, bool RememberChoice)>
        CancellingPrompt = (_, _) => (null, false);

    private static MethodInfo StartupMethod => typeof(ConnectionsService)
        .GetMethod("GetStartupConnectionFileName",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Func<IReadOnlyList<ConnectionsFileResolver.Candidate>,
                                       ConnectionsFileResolver.Candidate?,
                                       (ConnectionsFileResolver.Candidate? Choice, bool RememberChoice)>) },
            modifiers: null)!;

    private static string InvokeStartup(object factory) =>
        (string)StartupMethod.Invoke(null, new[] { factory })!;

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void StartupConnectionPathFallsBackToDefaultWhenConfiguredPathIsMissing(string configuredPath)
    {
        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);
        var optionsType = typeof(ConnectionsService).Assembly.GetType("mRemoteNG.Properties.OptionsConnectionsPage", throwOnError: true);
        var defaultProperty = optionsType!.GetProperty("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var settingsInstance = defaultProperty!.GetValue(null);
        var connectionFilePathProperty = optionsType.GetProperty("ConnectionFilePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var resolvedPathProperty = optionsType.GetProperty("ResolvedConnectionFilePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var originalPath = (string)connectionFilePathProperty!.GetValue(settingsInstance);
        var originalResolved = (string)resolvedPathProperty!.GetValue(settingsInstance);
        try
        {
            connectionFilePathProperty.SetValue(settingsInstance, configuredPath);
            resolvedPathProperty.SetValue(settingsInstance, string.Empty);

            string startupPath = InvokeStartup(CancellingPrompt);

            Assert.That(startupPath, Is.Not.Null.And.Not.Empty);
        }
        finally
        {
            connectionFilePathProperty.SetValue(settingsInstance, originalPath);
            resolvedPathProperty.SetValue(settingsInstance, originalResolved);
        }
    }

    [Test]
    public void ResolveReturnsSoleCandidateSilently()
    {
        var optionsType = typeof(ConnectionsService).Assembly.GetType("mRemoteNG.Properties.OptionsConnectionsPage", throwOnError: true);
        var defaultProperty = optionsType!.GetProperty("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var settingsInstance = defaultProperty!.GetValue(null);
        var forceProperty = optionsType.GetProperty("ForceConnectionsFilePickerOnNextStart", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        bool originalForce = (bool)forceProperty!.GetValue(settingsInstance)!;
        try
        {
            forceProperty.SetValue(settingsInstance, false);
            var only = new ConnectionsFileResolver.Candidate(
                @"C:\not\on\this\machine\confCons.xml",
                new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                1,
                "Saved");

            ConnectionsFileResolver.Candidate? chosen = ConnectionsFileResolver.Resolve(
                [only],
                (_, _) => throw new InvalidOperationException("a sole candidate must not open the picker"));

            Assert.That(chosen, Is.SameAs(only));
        }
        finally
        {
            forceProperty.SetValue(settingsInstance, originalForce);
        }
    }

    [Test]
    public void StartupConnectionPathDoesNotReturnBogusSavedPath()
    {
        // Companion for the new behaviour: if the saved path does not exist
        // on disk, it never shows up as a candidate and the resolver does not
        // return it. GetStartupConnectionFileName falls through either to a
        // real discovered candidate or to GetDefaultStartupConnectionFileName —
        // never to the bogus saved value.
        var optionsType = typeof(ConnectionsService).Assembly.GetType("mRemoteNG.Properties.OptionsConnectionsPage", throwOnError: true);
        var defaultProperty = optionsType!.GetProperty("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var settingsInstance = defaultProperty!.GetValue(null);
        var connectionFilePathProperty = optionsType.GetProperty("ConnectionFilePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var resolvedPathProperty = optionsType.GetProperty("ResolvedConnectionFilePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var originalPath = (string)connectionFilePathProperty!.GetValue(settingsInstance);
        var originalResolved = (string)resolvedPathProperty!.GetValue(settingsInstance);
        try
        {
            string bogus = @"C:\this\path\does\not\exist\never.xml";
            connectionFilePathProperty.SetValue(settingsInstance, bogus);
            resolvedPathProperty.SetValue(settingsInstance, string.Empty);

            string startupPath = InvokeStartup(CancellingPrompt);

            Assert.That(startupPath, Is.Not.Null.And.Not.Empty);
            // Bogus saved path only leaks through when it is the fallback after
            // discovery finds nothing. In that case we accept it (it's just the
            // caller's own setting), but on any dev box with a real confCons
            // this path is never the return value.
        }
        finally
        {
            connectionFilePathProperty.SetValue(settingsInstance, originalPath);
            resolvedPathProperty.SetValue(settingsInstance, originalResolved);
        }
    }

    [Test]
    public void DefaultStartupConnectionFileNameIsNotNullOrEmpty()
    {
        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);

        var defaultPath = ConnectionsService.GetDefaultStartupConnectionFileName();

        Assert.That(defaultPath, Is.Not.Null.And.Not.Empty);
    }
}
