using UnityEngine;

namespace Home
{
    [System.Serializable]
    public class Zone
    {
        public string id = "Zone";

        [Tooltip("Where the camera flies to when this zone is selected.")]
        public Transform cameraTarget;

        [Tooltip("Where the character walks to when this zone is selected.")]
        public Transform characterTarget;

        [Tooltip("Animator trigger played once the character arrives. Must match a Trigger parameter on the Animator Controller. Leave empty to just go Idle.")]
        public string actionTrigger;

        [Tooltip("If enabled, after arriving the character turns (Y axis only) to match this Character Target's Y rotation. Rotate the empty GameObject in the Editor to set the facing direction.")]
        public bool faceTargetRotation;

        [Tooltip("How long the character stays Busy (locked, playing its action) after arriving, in seconds.")]
        public float actionDuration = 2f;
    }
}
