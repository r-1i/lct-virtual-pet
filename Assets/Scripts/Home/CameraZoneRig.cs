using UnityEngine;

namespace Home
{
    public class CameraZoneRig : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;

        private void Reset()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        public void SnapTo(Transform target)
        {
            if (target == null || cameraTransform == null)
            {
                return;
            }

            cameraTransform.position = target.position;
            cameraTransform.rotation = target.rotation;
        }
    }
}
