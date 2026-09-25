using System;
using Content;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Step ids from tutorial.json that game code triggers with TryShow. Steps reached only through
    /// the previous step's showNextImmediately (care_intro, needs, lets_help, press_feed) don't need a
    /// constant — nothing calls them directly.
    /// </summary>
    public static class TutorialStepIds
    {
        /// <summary>Starts the greeting chain, ends with the "Покормить" button pulsing.</summary>
        public const string Welcome = "welcome";

        /// <summary>On entering the feeding screen.</summary>
        public const string TapFood = "tap_food";

        /// <summary>FoodPileView, after a feed; chains into go_home (home button pulses).</summary>
        public const string FedHappy = "fed_happy";

        /// <summary>TutorialZoneTrigger on the home zone; care button pulses.</summary>
        public const string CheckHealth = "check_health";

        /// <summary>TutorialZoneTrigger on the care zone; the 3D bath (ZoneEntryPoint) pulses.</summary>
        public const string GoShower = "go_shower";

        /// <summary>TutorialZoneTrigger on the shower zone (start of lathering).</summary>
        public const string Soap = "soap";

        /// <summary>ShowerMinigame, when lathering switches to rinsing.</summary>
        public const string Rinse = "rinse";

        /// <summary>TutorialZoneTrigger on the care zone (back from the shower); the sink pulses.</summary>
        public const string PressSink = "press_sink";

        /// <summary>TutorialZoneTrigger on the teeth zone.</summary>
        public const string BrushTeeth = "brush_teeth";

        /// <summary>TeethBrushingMinigame, after returning to Care; chains into go_home_work (home button pulses).</summary>
        public const string TeethDone = "teeth_done";

        /// <summary>TutorialZoneTrigger on the home zone; chains into press_work (work button pulses).</summary>
        public const string EarnMoney = "earn_money";

        /// <summary>TutorialZoneTrigger on the work zone; chains into pick_job ("Забрать" pulses once it appears).</summary>
        public const string JobsIntro = "jobs_intro";

        /// <summary>
        /// ActiveJobView, after collecting. While the tutorial is waiting for this step, JobService
        /// starts jobs with zero duration ("сейчас мы не будем ждать").
        /// </summary>
        public const string FirstMoney = "first_money";
    }

    /// <summary>
    /// Linear tutorial: steps go strictly in tutorial.json order, PlayerData.tutorialStep is the index
    /// of the next one. Game code calls TryShow(id) at the moment that step makes sense; the popup only
    /// appears if that id is the current step, so calling it again later (or out of order) is a no-op.
    /// TutorialPopup listens to ShowRequested and calls CompleteActive when the player taps.
    /// </summary>
    public class TutorialService
    {
        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;

        /// <summary>Raised when a step should be shown. The view may subscribe after it fired — it should check ActiveStep on subscribe.</summary>
        public event Action<TutorialStepDefinition> ShowRequested;

        /// <summary>The step currently on screen (shown, not yet dismissed), or null.</summary>
        public TutorialStepDefinition ActiveStep { get; private set; }

        public TutorialService(PlayerDataService playerData, ContentDatabase content)
        {
            _playerData = playerData;
            _content = content;
        }

        public bool IsFinished => _playerData.TutorialStep >= _content.TutorialSteps.Count;

        /// <summary>True when this step is the next one and not on screen yet — i.e. the tutorial is waiting for the player to do the thing that triggers it.</summary>
        public bool IsWaitingFor(string stepId)
        {
            return ActiveStep == null && !IsFinished && _content.TutorialSteps[_playerData.TutorialStep].id == stepId;
        }

        /// <summary>Shows the popup for this step if it's the current one. Returns true if it was shown.</summary>
        public bool TryShow(string stepId)
        {
            if (ActiveStep != null || IsFinished)
            {
                return false;
            }

            TutorialStepDefinition step = _content.TutorialSteps[_playerData.TutorialStep];
            if (step.id != stepId)
            {
                return false;
            }

            ActiveStep = step;
            ShowRequested?.Invoke(step);
            return true;
        }

        /// <summary>
        /// Called by TutorialPopup when the player dismisses the popup: moves on to the next step and
        /// saves, starts the step's highlight (if any), and chains straight into the next step if the
        /// dismissed one has showNextImmediately.
        /// </summary>
        public void CompleteActive()
        {
            if (ActiveStep == null)
            {
                return;
            }

            TutorialStepDefinition completed = ActiveStep;
            ActiveStep = null;
            _playerData.SetTutorialStep(_playerData.TutorialStep + 1);

            if (!string.IsNullOrEmpty(completed.highlight))
            {
                _playerData.AddTutorialHighlight(completed.highlight);
                HighlightsChanged?.Invoke();
            }

            if (completed.showNextImmediately && !IsFinished)
            {
                TryShow(_content.TutorialSteps[_playerData.TutorialStep].id);
            }
        }

        /// <summary>Raised when a highlight starts or stops. TutorialHighlight re-checks IsHighlighted for its key.</summary>
        public event Action HighlightsChanged;

        public bool IsHighlighted(string key) => !string.IsNullOrEmpty(key) && _playerData.HasTutorialHighlight(key);

        /// <summary>Called by TutorialHighlight when its target gets clicked.</summary>
        public void ClearHighlight(string key)
        {
            if (_playerData.RemoveTutorialHighlight(key))
            {
                HighlightsChanged?.Invoke();
            }
        }

        /// <summary>Debug helper: start the tutorial over.</summary>
        public void ResetProgress()
        {
            ActiveStep = null;
            _playerData.SetTutorialStep(0);
            _playerData.ClearTutorialHighlights();
            HighlightsChanged?.Invoke();
            Debug.Log("TutorialService: progress reset.");
        }
    }
}
