using System;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Tools;
using NUnit.Framework;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Waiting helpers for UI tests.
    ///
    /// No test in this battery sleeps for synchronisation. A sleep is either too short (flaky) or
    /// too long (slow), and it hides how long the app actually took. Everything here polls to a
    /// deadline and fails with a message that says what was being waited for — a timeout with no
    /// description is indistinguishable, to whoever reads the failure, from a test that found
    /// nothing to check.
    /// </summary>
    public static class UiWait
    {
        public static readonly TimeSpan Default = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

        public static AutomationElement FindRequired(AutomationElement root,
                                                     Func<ConditionFactory, ConditionBase> condition,
                                                     string what,
                                                     TimeSpan? timeout = null)
        {
            TimeSpan limit = timeout ?? Default;
            AutomationElement? element = Retry.WhileNull(
                () => root.FindFirstDescendant(condition),
                limit, PollInterval, throwOnTimeout: false).Result;

            Assert.That(element, Is.Not.Null,
                        $"Expected to find '{what}' within {limit.TotalSeconds:N0}s, but it never "
                        + "appeared. The attached UIA tree dump shows what was actually on screen.");
            return element!;
        }

        public static AutomationElement? FindOptional(AutomationElement root,
                                                      Func<ConditionFactory, ConditionBase> condition,
                                                      TimeSpan? timeout = null)
        {
            return Retry.WhileNull(() => root.FindFirstDescendant(condition),
                                   timeout ?? TimeSpan.FromSeconds(2), PollInterval,
                                   throwOnTimeout: false).Result;
        }

        public static void Until(Func<bool> condition, string what, TimeSpan? timeout = null)
        {
            TimeSpan limit = timeout ?? Default;
            bool ok = Retry.WhileFalse(condition, limit, PollInterval, throwOnTimeout: false).Result;

            Assert.That(ok, Is.True,
                        $"Timed out after {limit.TotalSeconds:N0}s waiting for: {what}");
        }

        /// <summary>
        /// True when the condition holds within the timeout, without failing the test. For checks
        /// where "it never happened" is the expected outcome.
        /// </summary>
        public static bool Happened(Func<bool> condition, TimeSpan timeout)
        {
            return Retry.WhileFalse(condition, timeout, PollInterval, throwOnTimeout: false).Result;
        }

        /// <summary>Lets the WinForms message loop drain after an interaction.</summary>
        public static void Settle(Window window)
        {
            try
            {
                window.Patterns.Window.PatternOrDefault?.WaitForInputIdle(2000);
            }
            catch (Exception)
            {
                // The window may have closed as a direct result of the interaction under test.
            }

            FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(250));
        }
    }
}
