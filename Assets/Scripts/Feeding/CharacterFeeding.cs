using Home;
using UnityEngine;

namespace Feeding
{
    /// <summary>
    /// Feeding-specific character behaviour: plays the eating animation when the player taps a food
    /// pile on the table. Reuses CharacterMotor.PlayAction for the bite so the character is Busy
    /// (can't be teleported away) for the duration of the animation.
    /// </summary>
    public class CharacterFeeding : MonoBehaviour
    {
        [SerializeField] private CharacterMotor characterMotor;
        [SerializeField] private Animator animator;
        [SerializeField] private string eatTrigger = "Eat";
        [SerializeField] private float eatActionDuration = 1.5f;

        public void PlayEat()
        {
            if (characterMotor != null)
            {
                characterMotor.PlayAction(eatTrigger, eatActionDuration);
            }
            else if (animator != null)
            {
                animator.SetTrigger(eatTrigger);
            }
        }
    }
}
