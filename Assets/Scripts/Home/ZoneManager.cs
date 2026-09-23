using Core;
using UnityEngine;

namespace Home
{
    public class ZoneManager : MonoBehaviour
    {
        [SerializeField] private CharacterMotor character;
        [SerializeField] private CameraZoneRig cameraRig;
        [SerializeField] private Zone[] zones;

        /// <summary>Index into Zones of the zone currently shown. -1 until the first GoToZone call (e.g. right after Bootstrap, before any nav button was pressed).</summary>
        public int CurrentZoneIndex { get; private set; } = -1;

        /// <summary>Wire this to each bottom button's OnClick, with the zone's index in the array as the int argument.</summary>
        public void GoToZone(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= zones.Length)
            {
                Debug.LogWarning($"ZoneManager: zone index {zoneIndex} is out of range.");
                return;
            }

            Zone zone = zones[zoneIndex];

            cameraRig.SnapTo(zone.cameraTarget);

            if (!character.IsBusy)
            {
                float? faceYRotation = zone.faceTargetRotation ? zone.characterTarget.eulerAngles.y : (float?)null;
                character.SnapTo(zone.characterTarget.position, faceYRotation, zone.actionTrigger, zone.actionDuration);
            }

            CurrentZoneIndex = zoneIndex;
            EventBus.Publish(new ZoneChangedEvent(zoneIndex));
        }
    }
}
