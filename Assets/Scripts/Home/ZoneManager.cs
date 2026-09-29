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

        /// <summary>While true, switching zones moves only the camera (set by CharacterWorkingPresenter while the character is at work).</summary>
        public bool CharacterLocked { get; set; }

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

            // Only work pins the character; a short action (eating) is interrupted by the move (CharacterMotor.SnapTo).
            if (!CharacterLocked)
            {
                float? faceYRotation = zone.faceTargetRotation ? zone.characterTarget.eulerAngles.y : (float?)null;
                character.SnapTo(zone.characterTarget.position, faceYRotation, zone.actionTrigger, zone.actionDuration);
            }

            CurrentZoneIndex = zoneIndex;
            EventBus.Publish(new ZoneChangedEvent(zoneIndex));
        }

        /// <summary>Puts the character on the current zone's Character Target without playing the zone's action. Faces the target's Y rotation if the zone has Face Target Rotation, otherwise takes <paramref name="rotation"/>.</summary>
        public void PlaceCharacterAtCurrentZone(Quaternion rotation)
        {
            if (CurrentZoneIndex < 0)
            {
                return;
            }

            Zone zone = zones[CurrentZoneIndex];
            if (zone.faceTargetRotation)
            {
                rotation = Quaternion.Euler(0f, zone.characterTarget.eulerAngles.y, 0f);
            }

            character.ForcePlace(zone.characterTarget.position, rotation);
        }
    }
}
