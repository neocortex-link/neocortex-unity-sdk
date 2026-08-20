using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Neocortex.Data;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Neocortex
{
    /// <summary>
    ///     Unified microphone component for all platforms (Standalone Windows/Mac, Android, iOS, WebGL).
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Audio Receiver", 0)]
    public class NeocortexAudioReceiver : AudioReceiver
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] public extern static void WebGL_Initialize(string objectName, float amplitudeThreshold, float maxWaitTime, bool usePushToTalk);
        [DllImport("__Internal")] public extern static void WebGL_StartRecording();
        [DllImport("__Internal")] public extern static void WebGL_StopRecording();
        [DllImport("__Internal")] public extern static bool WebGL_RecordingUpdatePointer(float[] idx);

        private const int WEBGL_FREQUENCY = 48000;
        private const int BUFFER_SIZE = 2048;

        private FloatArray currentBuffer;
        private MicrophoneState microphoneState = MicrophoneState.NotActive;
        private readonly List<FloatArray> binaryStreams = new();
        private bool sentPushToTalk;
        private float sentThreshold;
        private float sentWaitTime;
        private bool suppressNextEmit;
#else
        private const int NATIVE_FREQUENCY = 22050;
        private const int AUDIO_SAMPLE_WINDOW = 64;
        private const int AMPLITUDE_MULTIPLIER = 10;

        private AudioClip audioClip;
        private int startedMicIndex = -1;
        private bool wasPushToTalk;
#endif

        [Tooltip("Ask for microphone permission (Android/iOS) as soon as this component starts. Off = asked on first record.")]
        [SerializeField] private bool requestPermissionOnStart = true;

        [Space] public UnityEvent OnPermissionGranted = new();
        [Space] public UnityEvent OnPermissionDenied = new();

        private bool permissionGranted;
        private bool permissionResolved;

        /// <summary>Available microphone devices (a single placeholder entry on WebGL, the browser owns the mic).</summary>
        public string[] Microphones => NeocortexMicrophone.devices;

        /// <summary>The saved microphone index used by the capture layer on its next start.</summary>
        public int SelectedMicrophoneIndex
        {
            get => PlayerPrefs.GetInt(MIC_INDEX_KEY, 0);
            set => PlayerPrefs.SetInt(MIC_INDEX_KEY, value);
        }

        public string SelectedMicrophone { get; private set; }

        public void SelectMicrophone(int index) => SelectedMicrophoneIndex = index;

        public void SetPushToTalk(bool enabled) => usePushToTalk = enabled;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            currentBuffer = new FloatArray();
            currentBuffer.Buffer = new float[BUFFER_SIZE];
            PushConfigToBrowser();
            WebGL_RecordingUpdatePointer(currentBuffer.Buffer);
#else
            wasPushToTalk = usePushToTalk;
#endif
        }

        private IEnumerator Start()
        {
            if (requestPermissionOnStart)
            {
                yield return EnsurePermission();
            }
        }

        public override void StartMicrophone()
        {
            StartCoroutine(StartWhenPermitted());
        }

        private IEnumerator StartWhenPermitted()
        {
            yield return EnsurePermission();

            if (!permissionGranted)
            {
                OnRecordingFailed?.Invoke("Microphone permission denied.");
                yield break;
            }

            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (microphoneState == MicrophoneState.NotActive)
                {
                    microphoneState = MicrophoneState.Booting;
                    WebGL_StartRecording();
                }
#else
                string[] devices = NeocortexMicrophone.devices;
                startedMicIndex = Mathf.Clamp(PlayerPrefs.GetInt(MIC_INDEX_KEY, 0), 0, devices.Length - 1);
                SelectedMicrophone = devices.Length > 0 ? devices[startedMicIndex] : null;
                audioClip = NeocortexMicrophone.Start(SelectedMicrophone, true, 999, NATIVE_FREQUENCY);
                IsListening = true;
                IsUserSpeaking = usePushToTalk;
#endif
            }
            catch (Exception e)
            {
                OnRecordingFailed?.Invoke(e.Message);
            }
        }

        public override void StopMicrophone()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_StopRecording();
#else
            NeocortexMicrophone.End(SelectedMicrophone);
            IsListening = false;
            IsUserSpeaking = false;
            Amplitude = 0;
            EmitRecordedClip(audioClip);
#endif
        }

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (usePushToTalk != sentPushToTalk || !Mathf.Approximately(amplitudeThreshold, sentThreshold) || !Mathf.Approximately(maxWaitTime, sentWaitTime))
            {
                bool switchedToPushToTalk = usePushToTalk && !sentPushToTalk;
                PushConfigToBrowser();

                if (switchedToPushToTalk && microphoneState != MicrophoneState.NotActive)
                {
                    suppressNextEmit = true;
                    StopMicrophone();
                }
            }
#else
            if (usePushToTalk != wasPushToTalk)
            {
                wasPushToTalk = usePushToTalk;
                if (usePushToTalk && IsListening)
                {
                    NeocortexMicrophone.End(SelectedMicrophone);
                    IsListening = false;
                    IsUserSpeaking = false;
                    ElapsedWaitTime = 0;
                }
            }

            if (!IsListening) return;

            if (!usePushToTalk && !IsUserSpeaking && PlayerPrefs.GetInt(MIC_INDEX_KEY, 0) != startedMicIndex)
            {
                NeocortexMicrophone.End(SelectedMicrophone);
                ElapsedWaitTime = 0;
                StartMicrophone();
                return;
            }

            UpdateAmplitude();

            if (usePushToTalk) return;

            if (!IsUserSpeaking && Amplitude > amplitudeThreshold)
            {
                IsUserSpeaking = true;
            }

            if (IsUserSpeaking)
            {
                if (Amplitude < amplitudeThreshold)
                {
                    ElapsedWaitTime += Time.deltaTime;
                    if (ElapsedWaitTime >= maxWaitTime)
                    {
                        ElapsedWaitTime = 0;
                        StopMicrophone();
                    }
                }
                else
                {
                    ElapsedWaitTime = 0;
                }
            }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void PushConfigToBrowser()
        {
            sentPushToTalk = usePushToTalk;
            sentThreshold = amplitudeThreshold;
            sentWaitTime = maxWaitTime;
            WebGL_Initialize(name, amplitudeThreshold, maxWaitTime, usePushToTalk);
        }

        // WebGL JS Callbacks
        public void LogWrittenBuffer(int written)
        {
            if (microphoneState != MicrophoneState.Recording) return;

            currentBuffer.Written = written;
            binaryStreams.Add(currentBuffer);
            currentBuffer = new FloatArray { Buffer = new float[BUFFER_SIZE] };
            WebGL_RecordingUpdatePointer(currentBuffer.Buffer);
        }

        public void NotifyRecordingChange(int newRecordingState)
        {
            if ((int)microphoneState == newRecordingState) return;

            microphoneState = (MicrophoneState)newRecordingState;
            IsUserSpeaking = microphoneState == MicrophoneState.Recording;
            IsListening = microphoneState != MicrophoneState.NotActive;

            if (microphoneState == MicrophoneState.NotActive)
            {
                OnWebGLAudioRecorded();
            }
        }

        public void UpdateAmplitude(float amplitude) => Amplitude = amplitude;
        public void UpdateElapsedWaitTime(float elapsedWaitTime) => ElapsedWaitTime = elapsedWaitTime;

        private void OnWebGLAudioRecorded()
        {
            if (suppressNextEmit)
            {
                suppressNextEmit = false;
                binaryStreams.Clear();
                return;
            }

            int fCt = 0;
            foreach (FloatArray fa in binaryStreams) fCt += fa.Written;

            float[] ret = new float[fCt];
            int write = 0;
            foreach (FloatArray fa in binaryStreams)
            {
                Buffer.BlockCopy(fa.Buffer, 0, ret, write * 4, fa.Written * 4);
                write += fa.Written;
            }
            binaryStreams.Clear();

            if (ret.Length == 0) return;
            AudioClip clip = AudioClip.Create("", ret.Length, 1, WEBGL_FREQUENCY, false);
            clip.SetData(ret, 0);
            EmitRecordedClip(clip);
        }
#else
        private void UpdateAmplitude()
        {
            if (audioClip == null) return;
            int clipPosition = NeocortexMicrophone.GetPosition(SelectedMicrophone);
            int startPosition = Mathf.Max(0, clipPosition - AUDIO_SAMPLE_WINDOW);
            float[] audioSamples = new float[AUDIO_SAMPLE_WINDOW];
            audioClip.GetData(audioSamples, startPosition);

            float sum = 0;
            for (int i = 0; i < AUDIO_SAMPLE_WINDOW; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }

            Amplitude = Mathf.Clamp01(sum / AUDIO_SAMPLE_WINDOW * AMPLITUDE_MULTIPLIER);
        }

        private void OnDestroy()
        {
            if (IsListening)
            {
                NeocortexMicrophone.End(SelectedMicrophone);
            }
        }
#endif

        // ── Permission ──────────────────────────────────────────────────────────────────────────

        private IEnumerator EnsurePermission()
        {
            if (permissionResolved)
            {
                yield break;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                ResolvePermission(true);
                yield break;
            }

            bool answered = false;
            bool granted = false;
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { answered = true; granted = true; };
            callbacks.PermissionDenied += _ => { answered = true; };
            Permission.RequestUserPermission(Permission.Microphone, callbacks);

            while (!answered)
            {
                yield return null;
            }

            ResolvePermission(granted);
#elif UNITY_IOS && !UNITY_EDITOR
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                ResolvePermission(true);
                yield break;
            }

            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            ResolvePermission(Application.HasUserAuthorization(UserAuthorization.Microphone));
#else
            ResolvePermission(true);
            yield break;
#endif
        }

        private void ResolvePermission(bool granted)
        {
            permissionResolved = true;
            permissionGranted = granted;

            if (granted)
            {
                OnPermissionGranted?.Invoke();
            }
            else
            {
                OnPermissionDenied?.Invoke();
            }
        }
    }
}
