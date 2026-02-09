using System;
using NUnit.Framework;
using mRemoteNG.UI.Forms;

namespace mRemoteNGTests.UI.Forms
{
    public class OptionsFormSetupAndTeardown
    {
        protected FrmOptions _optionsForm;

        [OneTimeSetUp]
        public void OnetimeSetup()
        {
        }

        [SetUp]
        public void Setup()
        {
            _optionsForm = new FrmOptions();
            _optionsForm.Show();
        }

        [TearDown]
        public void Teardown()
        {
            var form = _optionsForm;
            _optionsForm = null;

            if (form == null) return;

            try { form.Close(); } catch { }
            try { form.Dispose(); } catch { }

            // Force finalization now while the runtime is still alive,
            // preventing CLR assert during AppDomain shutdown.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}