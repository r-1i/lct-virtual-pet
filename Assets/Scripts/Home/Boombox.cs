using System;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Home
{
    public class Boombox : MonoBehaviour
    {
        public event Action Played;
        public event Action Stopped;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Boombox animator")]
        [SerializeField] private Animator boomboxAnimator;
        [SerializeField] private string boomboxPlayingParam = "IsPlaying";

        [Header("Player")]
        [Tooltip("Gives the Animator of the model shown right now. Empty = the Player Animator field below is used.")]
        [SerializeField] private CharacterAppearance playerAppearance;
        [SerializeField] private Animator playerAnimator;

        private Animator PlayerAnimator => playerAppearance != null ? playerAppearance.Animator : playerAnimator;
        [SerializeField] private string playerDanceParam = "IsDancing";

        [Header("Input")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private float raycastMaxDistance = 100f;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (IsPlaying && audioSource != null && !audioSource.isPlaying)
            {
                Stop();
            }

            if (WasTappedThisFrame(out Vector2 screenPosition) && HitThisObject(screenPosition))
            {
                Toggle();
            }
        }

        private static bool WasTappedThisFrame(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private bool HitThisObject(Vector2 screenPosition)
        {
            if (PointerUtils.IsPointerOverUi(screenPosition))
            {
                return false;
            }

            if (raycastCamera == null)
            {
                return false;
            }

            Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, raycastMask)
                   && hit.transform.IsChildOf(transform);
        }

        public void Toggle()
        {
            if (IsPlaying)
            {
                Stop();
            }
            else
            {
                Play();
            }
        }

        /// <summary>Turns the player's dance back on if the music is still playing (e.g. after work, which forces the dance off).</summary>
        public void ReapplyPlayerDance()
        {
            Animator player = PlayerAnimator;
            if (IsPlaying && player != null)
            {
                player.SetBool(playerDanceParam, true);
            }
        }

        private void Play()
        {
            IsPlaying = true;

            if (audioSource != null)
            {
                audioSource.Play();
            }

            if (boomboxAnimator != null)
            {
                boomboxAnimator.SetBool(boomboxPlayingParam, true);
            }

            Animator player = PlayerAnimator;
            if (player != null)
            {
                player.SetBool(playerDanceParam, true);
            }

            Played?.Invoke();
        }

        private void Stop()
        {
            IsPlaying = false;

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            if (boomboxAnimator != null)
            {
                boomboxAnimator.SetBool(boomboxPlayingParam, false);
            }

            Animator player = PlayerAnimator;
            if (player != null)
            {
                player.SetBool(playerDanceParam, false);
            }

            Stopped?.Invoke();
        }
    }
}
