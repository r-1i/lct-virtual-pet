using System.Collections.Generic;
using Core;
using Home;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Care
{
    /// <summary>
    /// Shower minigame, shower-only (teeth brushing is a separate script). Two phases, two tools,
    /// two different interaction styles:
    ///   Applying: the sponge sticks to the character's actual surface (raycast hit point + normal
    ///             offset), sliding along the body as you drag. Small soap-bubble mesh prefabs spawn
    ///             at random near the contact point while it's touching the character.
    ///   Clearing: the shower head doesn't stick to anything — it just glides on its own horizontal
    ///             plane (X/Z only, its Y never changes) toward wherever the pointer projects onto
    ///             that plane. Rinsing itself isn't proximity-based: as long as ANY touch/press is
    ///             held (anywhere), a random bubble is destroyed every so often. There's no abstract
    ///             0..1 progress float; the actual bubble count IS the progress in both phases.
    /// Only active while the current zone is Active Zone Index.
    /// </summary>
    public class ShowerMinigame : MonoBehaviour
    {
        private enum Phase
        {
            Applying,
            Clearing,
            Done
        }

        [Header("Zone")]
        [SerializeField] private ZoneManager zoneManager;
        [Tooltip("Only responds to input while this is the current zone — otherwise fully hidden, even if this GameObject is active.")]
        [SerializeField] private int activeZoneIndex;

        [Header("Sponge (sticks to the character's surface)")]
        [SerializeField] private Transform sponge;
        [SerializeField] private bool alignSpongeToSurfaceNormal = true;
        [Tooltip("How far off the character's surface the sponge sits, along the hit normal — avoids clipping into the mesh.")]
        [SerializeField] private float surfaceOffset = 0.02f;

        [Header("Shower head (glides on its own horizontal plane, Y locked)")]
        [SerializeField] private Transform showerHead;
        [Tooltip("Shower head can't wander further than this from its starting X/Z position — keeps it from flying off if the camera angle makes the plane math unstable (near-grazing angles near the horizon).")]
        [SerializeField] private float showerHeadSwayRadius = 0.3f;

        [Header("Character hit-test")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private float raycastMaxDistance = 100f;
        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera raycastCamera;

        [Header("Soap bubbles")]
        [Tooltip("Small mesh prefab — a soap bubble/foam blob. No Collider or ParticleSystem needed.")]
        [SerializeField] private GameObject bubblePrefab;
        [Tooltip("Bubbles scatter within this radius of the sponge's current contact point.")]
        [SerializeField] private float scatterRadius = 0.03f;
        [SerializeField] private float minSpawnInterval = 0.05f;
        [SerializeField] private float maxSpawnInterval = 0.15f;
        [Tooltip("Lathering switches to rinsing once this many bubbles exist.")]
        [SerializeField] private int targetBubbleCount = 40;
        [Tooltip("Each spawned bubble gets a random uniform scale multiplier in this range, applied on top of the prefab's own scale.")]
        [SerializeField] private float minBubbleScale = 0.7f;
        [SerializeField] private float maxBubbleScale = 1.3f;
        [Tooltip("While the shower head is active (any touch/press held, anywhere), one random bubble is removed every this-many seconds (randomized between min/max for a less mechanical feel).")]
        [SerializeField] private float minRinseInterval = 0.08f;
        [SerializeField] private float maxRinseInterval = 0.25f;

        [Header("Reward")]
        [SerializeField] private float healthReward = 20f;

        [Header("Auto-return to Care")]
        [SerializeField] private int careZoneIndex;
        [SerializeField] private float returnDelay = 0.5f;

        private Phase _phase;
        private float _spawnTimer;
        private float _rinseTimer;
        private Vector3 _showerHeadHomePosition;
        private readonly List<Transform> _bubbles = new List<Transform>();
        private PlayerDataService _playerData;

        private void Awake()
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }

            if (showerHead != null)
            {
                _showerHeadHomePosition = showerHead.position;
            }
        }

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
        }

        private void OnEnable()
        {
            _phase = Phase.Applying;
            ClearAllBubbles();
            SetToolsVisible(false, false);
        }

        private void OnDisable()
        {
            ClearAllBubbles();
        }

        private void Update()
        {
            if (_phase == Phase.Done)
            {
                return;
            }

            if (zoneManager == null || zoneManager.CurrentZoneIndex != activeZoneIndex)
            {
                SetToolsVisible(false, false);
                return;
            }

            if (!TryGetHeldPointer(out Vector2 screenPosition) || PointerUtils.IsPointerOverUi(screenPosition))
            {
                SetToolsVisible(false, false);
                return;
            }

            if (_phase == Phase.Applying)
            {
                UpdateApplying(screenPosition);
            }
            else
            {
                UpdateClearing(screenPosition);
            }
        }

        /// <summary>Sponge only moves/counts while it's actually touching the character.</summary>
        private void UpdateApplying(Vector2 screenPosition)
        {
            if (!TryStickToCharacter(screenPosition, out Vector3 hitPoint, out Vector3 hitNormal))
            {
                SetToolsVisible(false, false);
                return;
            }

            PositionSponge(hitPoint, hitNormal);
            SetToolsVisible(true, false);

            TickSpawning(hitPoint);

            if (_bubbles.Count >= targetBubbleCount)
            {
                _phase = Phase.Clearing;
                ServiceLocator.Get<TutorialService>().TryShow(TutorialStepIds.Rinse);
            }
        }

        /// <summary>Shower head doesn't need to touch the character at all — any held press counts as "showering".</summary>
        private void UpdateClearing(Vector2 screenPosition)
        {
            MoveShowerHead(screenPosition);
            SetToolsVisible(false, true);

            TickRinsing();

            if (_bubbles.Count == 0)
            {
                Complete();
            }
        }

        private void TickSpawning(Vector3 hitPoint)
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
            SpawnBubble(hitPoint);
        }

        private void SpawnBubble(Vector3 hitPoint)
        {
            if (bubblePrefab == null)
            {
                return;
            }

            Vector3 position = hitPoint + Random.insideUnitSphere * scatterRadius;
            Transform parent = characterRoot != null ? characterRoot : transform;
            GameObject bubble = Instantiate(bubblePrefab, position, Random.rotation, parent);

            float scaleMultiplier = Random.Range(minBubbleScale, maxBubbleScale);
            bubble.transform.localScale *= scaleMultiplier;

            _bubbles.Add(bubble.transform);
        }

        private void TickRinsing()
        {
            _rinseTimer -= Time.deltaTime;
            if (_rinseTimer > 0f)
            {
                return;
            }

            _rinseTimer = Random.Range(minRinseInterval, maxRinseInterval);
            RemoveRandomBubble();
        }

        private void RemoveRandomBubble()
        {
            // Prune anything already destroyed some other way before picking, so we don't "waste" a tick on a stale entry.
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                if (_bubbles[i] == null)
                {
                    _bubbles.RemoveAt(i);
                }
            }

            if (_bubbles.Count == 0)
            {
                return;
            }

            int index = Random.Range(0, _bubbles.Count);
            Destroy(_bubbles[index].gameObject);
            _bubbles.RemoveAt(index);
        }

        private void ClearAllBubbles()
        {
            foreach (Transform bubble in _bubbles)
            {
                if (bubble != null)
                {
                    Destroy(bubble.gameObject);
                }
            }

            _bubbles.Clear();
        }

        private static bool TryGetHeldPointer(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        /// <summary>Raycasts the pointer against the character's collider; on a hit, returns a point sitting just off the surface along the normal — this is what makes the tool "stick" and slide along the body instead of floating on a flat plane.</summary>
        private bool TryStickToCharacter(Vector2 screenPosition, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = Vector3.up;

            if (raycastCamera == null || characterRoot == null)
            {
                return false;
            }

            Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, raycastMask) || !hit.transform.IsChildOf(characterRoot))
            {
                return false;
            }

            point = hit.point + hit.normal * surfaceOffset;
            normal = hit.normal;
            return true;
        }

        private void PositionSponge(Vector3 point, Vector3 normal)
        {
            if (sponge == null)
            {
                return;
            }

            sponge.position = point;

            if (alignSpongeToSurfaceNormal)
            {
                sponge.rotation = Quaternion.LookRotation(-normal, Vector3.up);
            }
        }

        /// <summary>
        /// Projects the pointer onto the shower head's own horizontal plane (fixed Y, from wherever
        /// it was placed in the editor) — X/Z only, never moves up or down, doesn't need to hit the
        /// character at all. Plane.Raycast can report a technically-valid but huge or negative
        /// distance when the ray is nearly parallel to the plane (camera looking close to the
        /// horizon) — that's what was flinging the shower head far away. Guarded against here by
        /// rejecting out-of-range distances and clamping the result to a radius around the start
        /// position, so a bad frame just doesn't move it instead of teleporting it off into space.
        /// </summary>
        private void MoveShowerHead(Vector2 screenPosition)
        {
            if (showerHead == null || raycastCamera == null)
            {
                return;
            }

            var horizontalPlane = new Plane(Vector3.up, new Vector3(0f, _showerHeadHomePosition.y, 0f));
            Ray ray = raycastCamera.ScreenPointToRay(screenPosition);

            if (!horizontalPlane.Raycast(ray, out float distance) || distance <= 0f || distance > raycastMaxDistance)
            {
                return;
            }

            Vector3 point = ray.GetPoint(distance);
            Vector3 offset = new Vector3(point.x - _showerHeadHomePosition.x, 0f, point.z - _showerHeadHomePosition.z);
            offset = Vector3.ClampMagnitude(offset, showerHeadSwayRadius);

            showerHead.position = _showerHeadHomePosition + offset;
        }

        private void SetToolsVisible(bool spongeVisible, bool showerHeadVisible)
        {
            if (sponge != null && sponge.gameObject.activeSelf != spongeVisible)
            {
                sponge.gameObject.SetActive(spongeVisible);
            }

            if (showerHead != null && showerHead.gameObject.activeSelf != showerHeadVisible)
            {
                showerHead.gameObject.SetActive(showerHeadVisible);
            }
        }

        private void Complete()
        {
            _phase = Phase.Done;
            SetToolsVisible(false, false);

            _playerData.ApplyStatDelta(0f, 0f, healthReward);

            Invoke(nameof(ReturnToCare), returnDelay);
        }

        private void ReturnToCare()
        {
            zoneManager.GoToZone(careZoneIndex);
        }
    }
}
