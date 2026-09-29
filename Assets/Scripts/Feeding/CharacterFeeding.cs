using Home;
using UnityEngine;

namespace Feeding
{
    /// <summary>
    /// Feeding-specific character behaviour: plays the eating animation when the player taps a food
    /// pile on the table. Reuses CharacterMotor.PlayAction for the bite (Busy for the duration of the animation);
    /// leaving the zone mid-bite interrupts it and the character goes with the camera.
    /// </summary>
    public class CharacterFeeding : MonoBehaviour
    {
        [SerializeField] private CharacterMotor characterMotor;
        [Tooltip("Gives the Animator of the model shown right now. Empty = the Animator field below is used.")]
        [SerializeField] private CharacterAppearance appearance;
        [SerializeField] private Animator animator;
        [SerializeField] private string eatTrigger = "Eat";
        [SerializeField] private float eatActionDuration = 1.5f;

        public void PlayEat()
        {
            if (characterMotor != null)
            {
                characterMotor.PlayAction(eatTrigger, eatActionDuration);
            }
            else
            {
                Animator current = appearance != null ? appearance.Animator : animator;
                if (current != null)
                {
                    current.SetTrigger(eatTrigger);
                }
            }
        }
    }
}
