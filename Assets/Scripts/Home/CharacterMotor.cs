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
        [SerializeField] private Animator animator;

        public CharacterActivityState State { get; private set; } = CharacterActivityState.Idle;
        public bool IsBusy => State == CharacterActivityState.Busy;

        private Coroutine _busyRoutine;

        public void SnapTo(Vector3 position, float? faceYRotation, string actionTrigger, float actionDuration)
        {
            if (IsBusy)
            {
                return;
            }

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

        public void PlayAction(string actionTrigger, float duration)
        {
            if (_busyRoutine != null)
            {
                StopCoroutine(_busyRoutine);
            }

            if (animator != null && !string.IsNullOrEmpty(actionTrigger))
            {
                animator.SetTrigger(actionTrigger);
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
