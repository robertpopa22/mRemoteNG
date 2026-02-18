using FlaUI.Core.AutomationElements;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace mRemoteNGSpecs.StepDefinitions
{
    [Binding]
    public class SmokeTestSteps
    {
        private readonly ScenarioContext _scenarioContext;

        public SmokeTestSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given(@"the application is running")]
        public void GivenTheApplicationIsRunning()
        {
            var mainWindow = _scenarioContext.Get<Window>();
            Assert.That(mainWindow, Is.Not.Null, "Main window should be available after launch.");
        }

        [Then(@"the main window title contains ""(.*)""")]
        public void ThenTheMainWindowTitleContains(string expectedTitle)
        {
            var mainWindow = _scenarioContext.Get<Window>();
            Assert.That(mainWindow.Title, Does.Contain(expectedTitle),
                $"Expected main window title to contain '{expectedTitle}', but was '{mainWindow.Title}'.");
        }
    }
}
