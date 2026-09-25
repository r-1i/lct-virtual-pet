using System;

namespace Content
{
    /// <summary>One tutorial popup, loaded from Resources/Content/tutorial.json. Steps go strictly in file order; PlayerData.tutorialStep is the index of the next one to show.</summary>
    [Serializable]
    public class TutorialStepDefinition
    {
        /// <summary>What game code passes to TutorialService.TryShow — see TutorialSteps for the constants.</summary>
        public string id;

        /// <summary>Text shown in the cloud.</summary>
        public string description;

        /// <summary>Optional. Key of the TutorialHighlight that starts pulsing once this popup is dismissed (and pulses until it's clicked). Empty = nothing pulses.</summary>
        public string highlight;

        /// <summary>Optional. Show the next step right after this one is dismissed, for back-to-back hints.</summary>
        public bool showNextImmediately;
    }

    /// <summary>JsonUtility can't parse a bare top-level array, so tutorial.json is { "steps": [ ... ] }.</summary>
    [Serializable]
    public class TutorialStepDefinitionList
    {
        public TutorialStepDefinition[] steps;
    }
}
