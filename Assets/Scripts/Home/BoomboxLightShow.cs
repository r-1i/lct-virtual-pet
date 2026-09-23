using UnityEngine;

namespace Home
{
    [System.Serializable]
    public class PulsingLight
    {
        public Light light;
        [Tooltip("Full sine cycle duration, in seconds.")]
        public float period = 1f;
        [Tooltip("How much intensity swings above/below the light's own intensity at the moment the boombox starts playing.")]
        public float intensityAmplitude = 1f;
        [Tooltip("Color the light oscillates towards and back from its own color at the moment the boombox starts playing.")]
        public Color peakColor = Color.magenta;

        [System.NonSerialized] public float baseIntensity;
        [System.NonSerialized] public Color baseColor;
    }

    public class BoomboxLightShow : MonoBehaviour
    {
        [SerializeField] private Boombox boombox;
        [SerializeField] private PulsingLight[] pulsingLights;

        [Header("Directional light")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private float directionalLightPlayingIntensity = 0.3f;
        [SerializeField] private float directionalLightFadeSpeed = 2f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem particles;

        private float _directionalBaseIntensity;
        private float _directionalTargetIntensity;
        private bool _isActive;

        private void Awake()
        {
            if (directionalLight != null)
            {
                _directionalBaseIntensity = directionalLight.intensity;
                _directionalTargetIntensity = directionalLight.intensity;
            }
        }

        private void OnEnable()
        {
            if (boombox != null)
            {
                boombox.Played += HandlePlayed;
                boombox.Stopped += HandleStopped;
            }
        }

        private void OnDisable()
        {
            if (boombox != null)
            {
                boombox.Played -= HandlePlayed;
                boombox.Stopped -= HandleStopped;
            }
        }

        private void HandlePlayed()
        {
            foreach (PulsingLight pulsing in pulsingLights)
            {
                if (pulsing.light == null)
                {
                    continue;
                }

                pulsing.light.gameObject.SetActive(true);
                pulsing.baseIntensity = pulsing.light.intensity;
                pulsing.baseColor = pulsing.light.color;
            }

            if (directionalLight != null)
            {
                _directionalBaseIntensity = directionalLight.intensity;
            }

            _directionalTargetIntensity = directionalLightPlayingIntensity;
            _isActive = true;

            if (particles != null)
            {
                particles.gameObject.SetActive(true);
                particles.Play();
            }
        }

        private void HandleStopped()
        {
            _isActive = false;

            foreach (PulsingLight pulsing in pulsingLights)
            {
                if (pulsing.light == null)
                {
                    continue;
                }

                pulsing.light.intensity = pulsing.baseIntensity;
                pulsing.light.color = pulsing.baseColor;
                pulsing.light.gameObject.SetActive(false);
            }

            _directionalTargetIntensity = _directionalBaseIntensity;

            if (particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_isActive)
            {
                foreach (PulsingLight pulsing in pulsingLights)
                {
                    if (pulsing.light == null || pulsing.period <= 0f)
                    {
                        continue;
                    }

                    float wave = Mathf.Sin(Time.time / pulsing.period * Mathf.PI * 2f);

                    pulsing.light.intensity = Mathf.Max(0f, pulsing.baseIntensity + wave * pulsing.intensityAmplitude);
                    pulsing.light.color = Color.Lerp(pulsing.baseColor, pulsing.peakColor, (wave + 1f) * 0.5f);
                }
            }

            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.MoveTowards(
                    directionalLight.intensity,
                    _directionalTargetIntensity,
                    directionalLightFadeSpeed * Time.deltaTime);
            }
        }
    }
}
