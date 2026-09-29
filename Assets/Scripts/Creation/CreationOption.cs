using UnityEngine;
using UnityEngine.UI;

namespace Creation
{
    /// <summary>One tappable card on a character creation screen (model / colour / accessory). CharacterCreationFlow listens to its button and turns the frame on for the picked one.</summary>
    public class CreationOption : MonoBehaviour
    {
        [SerializeField] private Button button;
        [Tooltip("Frame/glow shown only on the picked card.")]
        [SerializeField] private GameObject selectedMark;

        public Button Button => button;

        public void SetSelected(bool selected)
        {
            if (selectedMark != null)
            {
                selectedMark.SetActive(selected);
            }
        }
    }
}
