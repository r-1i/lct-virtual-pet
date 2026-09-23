using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Home
{
    /// <summary>Push-to-record mic: press "mic on", talk, then press "mic off" (or wait for the max duration) and the character plays back everything that was recorded, pitched up, with a talking animation.</summary>
    public class MicrophoneMimic : MonoBehaviour
    {
        private const int VolumeWindowSamples = 1024;

        [Header("Recording")]
        [SerializeField] private int sampleRate = 44100;
        [Tooltip("Recording stops automatically and plays back after this many seconds.")]
        [FormerlySerializedAs("maxPhraseSeconds")]
        [SerializeField] private float maxRecordSeconds = 8f;
        [Tooltip("Recordings shorter than this are discarded (accidental double tap).")]
        [FormerlySerializedAs("minPhraseSeconds")]
        [SerializeField] private float minRecordSeconds = 0.3f;

        [Header("UI")]
        [Tooltip("GO_0: 'mic on' button object. Hidden while recording. Must be or contain a Button.")]
        [SerializeField] private GameObject micOnObject;
        [Tooltip("GO_1: 'mic off' button object with the volume meter. Shown only while recording. Must be or contain a Button.")]
        [SerializeField] private GameObject micOffObject;
        [Tooltip("Image showing the current volume. Set Image Type = Filled (Horizontal, Origin Left); the script drives Fill Amount.")]
        [SerializeField] private Image volumeBar;
        [Tooltip("Multiplier for the raw RMS level (speech is usually 0.02-0.2) to fill the bar.")]
        [SerializeField] private float volumeGain = 6f;
        [Tooltip("Higher = the bar reacts faster.")]
        [SerializeField] private float volumeSmoothing = 20f;

        [Header("Playback")]
        [SerializeField] private AudioSource playbackSource;
        [SerializeField] private float playbackPitch = 1.7f;

        [Header("Animation")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private string talkingParam = "IsTalking";

        private Button _micOnButton;
        private Button _micOffButton;
        private string _micDevice;
        private bool _isReady;
        private bool _isRecording;
        private float _startTime;
        private float _level;
        private float[] _volumeBuffer;
        private AudioClip _micClip;
        private AudioClip _playbackClip;
        private Coroutine _talkingRoutine;

        private void Awake()
        {
            _micOnButton = micOnObject != null ? micOnObject.GetComponentInChildren<Button>(true) : null;
            _micOffButton = micOffObject != null ? micOffObject.GetComponentInChildren<Button>(true) : null;

            if (_micOnButton != null)
            {
                _micOnButton.onClick.AddListener(StartRecording);
            }

            if (_micOffButton != null)
            {
                _micOffButton.onClick.AddListener(StopRecording);
            }

            SetRecordingUi(false);
        }

        private IEnumerator Start()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogWarning("MicrophoneMimic: microphone permission was denied.");
                yield break;
            }

            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("MicrophoneMimic: no microphone device found.");
                yield break;
            }

            _micDevice = Microphone.devices[0];
            _isReady = true;
        }

        private void Update()
        {
            if (!_isRecording)
            {
                return;
            }

            if (Time.unscaledTime - _startTime >= maxRecordSeconds)
            {
                StopRecording();
                return;
            }

            UpdateVolumeMeter();
        }

        /// <summary>Hooked to the "mic on" button automatically; can also be called from a UnityEvent.</summary>
        public void StartRecording()
        {
            if (!_isReady || _isRecording)
            {
                return;
            }

            StopPlayback();

            _micClip = Microphone.Start(_micDevice, false, Mathf.Max(1, Mathf.CeilToInt(maxRecordSeconds)), sampleRate);
            if (_micClip == null)
            {
                Debug.LogWarning("MicrophoneMimic: failed to start the microphone.");
                return;
            }

            _isRecording = true;
            _startTime = Time.unscaledTime;
            _level = 0f;
            SetRecordingUi(true);
        }

        /// <summary>Hooked to the "mic off" button automatically; also called when the max duration is reached. Plays back what was recorded.</summary>
        public void StopRecording()
        {
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;
            float elapsed = Time.unscaledTime - _startTime;
            int position = Microphone.GetPosition(_micDevice);
            Microphone.End(_micDevice);
            SetRecordingUi(false);

            AudioClip recorded = TrimClip(_micClip, position, elapsed);
            Destroy(_micClip);
            _micClip = null;

            if (recorded != null)
            {
                PlayMimicry(recorded);
            }
        }

        /// <summary>Copies the recorded part out of the mic clip (which is always maxRecordSeconds long).</summary>
        private AudioClip TrimClip(AudioClip source, int position, float elapsed)
        {
            // GetPosition can report 0 once a non-looping recording has filled the clip; fall back to wall-clock time.
            int frames = position > 0 ? position : Mathf.RoundToInt(elapsed * source.frequency);
            frames = Mathf.Min(frames, source.samples);

            if (frames < minRecordSeconds * source.frequency)
            {
                return null;
            }

            float[] data = new float[frames * source.channels];
            source.GetData(data, 0);

            AudioClip clip = AudioClip.Create("MimicPhrase", frames, source.channels, source.frequency, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void UpdateVolumeMeter()
        {
            if (volumeBar == null || _micClip == null)
            {
                return;
            }

            float rms = ReadRms(Microphone.GetPosition(_micDevice));
            _level = Mathf.Lerp(_level, rms, 1f - Mathf.Exp(-volumeSmoothing * Time.unscaledDeltaTime));
            SetMeter(_level * volumeGain);
        }

        private float ReadRms(int position)
        {
            int window = Mathf.Min(VolumeWindowSamples, position);
            if (window <= 0)
            {
                return 0f;
            }

            int length = window * _micClip.channels;
            if (_volumeBuffer == null || _volumeBuffer.Length != length)
            {
                _volumeBuffer = new float[length];
            }

            _micClip.GetData(_volumeBuffer, position - window);

            float sumSquares = 0f;
            for (int i = 0; i < _volumeBuffer.Length; i++)
            {
                sumSquares += _volumeBuffer[i] * _volumeBuffer[i];
            }

            return Mathf.Sqrt(sumSquares / _volumeBuffer.Length);
        }

        private void SetMeter(float value01)
        {
            if (volumeBar == null)
            {
                return;
            }

            volumeBar.fillAmount = Mathf.Clamp01(value01);
        }

        private void SetRecordingUi(bool recording)
        {
            if (micOnObject != null)
            {
                micOnObject.SetActive(!recording);
            }

            if (micOffObject != null)
            {
                micOffObject.SetActive(recording);
            }

            if (!recording)
            {
                SetMeter(0f);
            }
        }

        private void PlayMimicry(AudioClip clip)
        {
            if (playbackSource == null)
            {
                Destroy(clip);
                return;
            }

            StopPlayback();

            if (_playbackClip != null)
            {
                Destroy(_playbackClip);
            }

            _playbackClip = clip;
            playbackSource.pitch = playbackPitch;
            playbackSource.clip = clip;
            playbackSource.Play();

            _talkingRoutine = StartCoroutine(TalkingAnimationRoutine(clip.length / playbackPitch));
        }

        private void StopPlayback()
        {
            if (_talkingRoutine != null)
            {
                StopCoroutine(_talkingRoutine);
                _talkingRoutine = null;
            }

            if (characterAnimator != null)
            {
                characterAnimator.SetBool(talkingParam, false);
            }

            if (playbackSource != null)
            {
                playbackSource.Stop();
            }
        }

        private IEnumerator TalkingAnimationRoutine(float duration)
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetBool(talkingParam, true);
            }

            yield return new WaitForSeconds(duration);

            if (characterAnimator != null)
            {
                characterAnimator.SetBool(talkingParam, false);
            }

            _talkingRoutine = null;
        }

        private void OnDisable()
        {
            // Discard an unfinished recording if this object gets switched off mid-record.
            if (_isRecording)
            {
                _isRecording = false;
                Microphone.End(_micDevice);
                SetRecordingUi(false);
            }
        }

        private void OnDestroy()
        {
            if (_micOnButton != null)
            {
                _micOnButton.onClick.RemoveListener(StartRecording);
            }

            if (_micOffButton != null)
            {
                _micOffButton.onClick.RemoveListener(StopRecording);
            }

            if (!string.IsNullOrEmpty(_micDevice) && Microphone.IsRecording(_micDevice))
            {
                Microphone.End(_micDevice);
            }
        }
    }
}
