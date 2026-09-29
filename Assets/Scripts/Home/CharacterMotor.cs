using System.Collections;
using UnityEngine;

namespace Home
{
    public enum CharacterActivityState
    {
        Idle,
        Busy
    }

    public class CharacterMotor : MonoBehaviour
    {
        [Header("Animator")]
        [Tooltip("Gives the Animator of the model shown right now. Empty = the Animator field below is used.")]
        [SerializeField] private CharacterAppearance appearance;
        [SerializeField] private Animator animator;

        private Animator CurrentAnimator => appearance != null ? appearance.Animator : animator;

        public CharacterActivityState State { get; private set; } = CharacterActivityState.Idle;
        public bool IsBusy => State == CharacterActivityState.Busy;

        private Coroutine _busyRoutine;

        /// <summary>
        /// Zone switch: always moves the character. A running action (e.g. eating) is interrupted — otherwise leaving a zone
        /// mid-animation left the character behind in the old zone while the camera moved on.
        /// </summary>
        public void SnapTo(Vector3 position, float? faceYRotation, string actionTrigger, float actionDuration)
        {
            CancelAction();

            transform.position = position;

            if (faceYRotation.HasValue)
            {
                transform.rotation = Quaternion.Euler(0f, faceYRotation.Value, 0f);
            }

            if (!string.IsNullOrEmpty(actionTrigger))
            {
                PlayAction(actionTrigger, actionDuration);
            }
        }

        /// <summary>Teleports with the full rotation even while Busy (cancels the current action lock). Used for the work point.</summary>
        public void ForcePlace(Vector3 position, Quaternion rotation)
        {
            CancelAction();
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Ends the Busy lock of the current action right away (the clip itself just plays out and returns to Idle).</summary>
        public void CancelAction()
        {
            if (_busyRoutine != null)
            {
                StopCoroutine(_busyRoutine);
                _busyRoutine = null;
            }

            State = CharacterActivityState.Idle;
        }

        public void PlayAction(string actionTrigger, float duration)
        {
            if (_busyRoutine != null)
            {
                StopCoroutine(_busyRoutine);
            }

            Animator current = CurrentAnimator;
            if (current != null && !string.IsNullOrEmpty(actionTrigger))
            {
                current.SetTrigger(actionTrigger);
            }

            _busyRoutine = StartCoroutine(BusyRoutine(duration));
        }

        private IEnumerator BusyRoutine(float duration)
        {
            State = CharacterActivityState.Busy;
            yield return new WaitForSeconds(duration);
            State = CharacterActivityState.Idle;
            _busyRoutine = null;
        }

        /// <summary>Optional: call from an Animation Event at the end of an action clip to end Busy exactly on the last frame instead of relying on actionDuration.</summary>
        public void OnActionAnimationEvent_Complete()
        {
            if (_busyRoutine != null)
            {
                StopCoroutine(_busyRoutine);
                _busyRoutine = null;
            }

            State = CharacterActivityState.Idle;
        }
    }
}
