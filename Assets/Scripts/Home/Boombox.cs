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
        [SerializeField] private Animator playerAnimator;
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

            if (playerAnimator != null)
            {
                playerAnimator.SetBool(playerDanceParam, true);
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

            if (playerAnimator != null)
            {
                playerAnimator.SetBool(playerDanceParam, false);
            }

            Stopped?.Invoke();
        }
    }
}
