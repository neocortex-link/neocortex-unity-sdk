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
    /// <remarks>
    ///     On native platforms the device records into a short ring, and a recording is cut out of
    ///     it: from <see cref="leadIn"/> before the voice crossed the threshold (never before
    ///     listening began) to a moment after it went quiet. The first sound of an answer, which is
    ///     often softer than the threshold, is kept that way.
    /// </remarks>
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

        // The ring the device records into. A recording is never longer than this, less a second.
        private const int RING_SECONDS = 30;
        // Kept after the last loud sound, so the end of the last word is not cut.
        private const float TAIL_SECONDS = 0.3f;
        // A loud sound shorter than this is a click or a knock, not speech, and does not end listening.
        private const float SHORTEST_SPEECH_SECONDS = 0.15f;

        private AudioClip ring;
        private int startedMicIndex = -1;
        private bool wasPushToTalk;
        private bool opening;
        private int ringOrigin;
        private int lastPosition;
        private int listenRequest;

        // Sample positions counted from when the device opened, so they never wrap with the ring.
        private long written;
        private long listenStart;
        private long speechStart = -1;
        private long lastVoice;
#endif

        [Tooltip("Ask for microphone permission (Android/iOS) as soon as this component starts. Off = asked on first record.")]
        [SerializeField] private bool requestPermissionOnStart = true;

        [Tooltip("Keep the device recording between recordings, so the next one starts at once instead of waiting for the device to start (and losing the first sound). Off = the device stops after every recording.")]
        public bool keepMicrophoneOpen;

        [Tooltip("Voice-activity mode: seconds kept from before the voice crossed the threshold, so a soft first sound is not cut. Never reaches back before listening began. Native platforms only.")]
        [Range(0f, 1f)] public float leadIn = 0.3f;

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

#if !UNITY_WEBGL || UNITY_EDITOR
            // Started up front, so even the first recording does not wait for the device.
            if (keepMicrophoneOpen && permissionGranted)
            {
                yield return OpenDevice();
            }
#endif
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

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (microphoneState == MicrophoneState.NotActive)
                {
                    microphoneState = MicrophoneState.Booting;
                    WebGL_StartRecording();
                }
            }
            catch (Exception e)
            {
                OnRecordingFailed?.Invoke(e.Message);
            }
#else
            if (IsListening)
            {
                yield break;
            }

            int request = ++listenRequest;
            yield return OpenDevice();

            // Shut again while the device was starting, or listening was asked for once more since.
            if (request != listenRequest || ring == null || IsListening)
            {
                yield break;
            }

            SyncPosition();
            listenStart = written;
            speechStart = -1;
            ElapsedWaitTime = 0;
            IsListening = true;
            IsUserSpeaking = usePushToTalk;
#endif
        }

        public override void StopMicrophone()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_StopRecording();
#else
            if (IsListening)
            {
                FinishRecording(true);
            }
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

                // Switching to push to talk while listening shuts the microphone and drops what it heard.
                if (usePushToTalk && IsListening)
                {
                    CloseListening();

                    if (!keepMicrophoneOpen)
                    {
                        CloseDevice();
                    }
                }
            }

            if (!IsListening || ring == null) return;

            if (!usePushToTalk && !IsUserSpeaking && PlayerPrefs.GetInt(MIC_INDEX_KEY, 0) != startedMicIndex)
            {
                CloseListening();
                CloseDevice();
                StartMicrophone();
                return;
            }

            SyncPosition();
            UpdateAmplitude();

            long longest = (long)(RING_SECONDS - 1) * ring.frequency;

            if (usePushToTalk)
            {
                if (written - listenStart >= longest)
                {
                    FinishRecording(true);
                }

                return;
            }

            if (Amplitude > amplitudeThreshold)
            {
                if (speechStart < 0)
                {
                    speechStart = written;
                }

                lastVoice = written;
                ElapsedWaitTime = 0;
                IsUserSpeaking = true;
            }
            else if (IsUserSpeaking)
            {
                ElapsedWaitTime += Time.deltaTime;
                if (ElapsedWaitTime >= maxWaitTime)
                {
                    ElapsedWaitTime = 0;

                    if (lastVoice - speechStart < (long)(SHORTEST_SPEECH_SECONDS * ring.frequency))
                    {
                        // A click or a knock: listening goes on.
                        speechStart = -1;
                        IsUserSpeaking = false;
                    }
                    else
                    {
                        FinishRecording(false);
                    }

                    return;
                }
            }

            if (IsUserSpeaking && written - speechStart >= longest)
            {
                FinishRecording(true);
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
        /// <summary>
        ///     Cuts the recording out of the ring and hands it on. <paramref name="stopped"/> means
        ///     it was ended on purpose (StopMicrophone or the length limit) rather than by silence,
        ///     so everything up to now is kept, even speech too soft to have crossed the threshold.
        /// </summary>
        private void FinishRecording(bool stopped)
        {
            SyncPosition();

            int frequency = ring.frequency;
            bool heardSpeech = speechStart >= 0;

            long from = heardSpeech ? Math.Max(listenStart, speechStart - (long)(leadIn * frequency)) : listenStart;
            long to = heardSpeech && !stopped ? Math.Min(written, lastVoice + (long)(TAIL_SECONDS * frequency)) : written;
            from = Math.Max(from, written - (ring.samples - frequency));

            float[] samples = to > from ? ReadRing(from, to) : Array.Empty<float>();

            CloseListening();

            if (!keepMicrophoneOpen)
            {
                CloseDevice();
            }

            AudioClip recording = AudioClip.Create("Recording", Math.Max(1, samples.Length), 1, frequency, false);
            if (samples.Length > 0)
            {
                recording.SetData(samples, 0);
            }

            EmitRecordedClip(recording);
            Destroy(recording);
        }

        private void CloseListening()
        {
            listenRequest++;
            IsListening = false;
            IsUserSpeaking = false;
            Amplitude = 0;
            ElapsedWaitTime = 0;
            speechStart = -1;
        }

        private IEnumerator OpenDevice()
        {
            while (opening)
            {
                yield return null;
            }

            string[] devices = NeocortexMicrophone.devices;
            int micIndex = Mathf.Clamp(PlayerPrefs.GetInt(MIC_INDEX_KEY, 0), 0, Mathf.Max(0, devices.Length - 1));

            if (ring != null && micIndex == startedMicIndex && NeocortexMicrophone.IsRecording(SelectedMicrophone))
            {
                yield break;
            }

            CloseDevice();
            opening = true;

            string error = null;
            try
            {
                startedMicIndex = micIndex;
                SelectedMicrophone = devices.Length > 0 ? devices[micIndex] : null;
                ring = NeocortexMicrophone.Start(SelectedMicrophone, true, RING_SECONDS, NATIVE_FREQUENCY);
                if (ring == null)
                {
                    error = "The microphone did not start.";
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            if (error != null)
            {
                ring = null;
                opening = false;
                OnRecordingFailed?.Invoke(error);
                yield break;
            }

            // A device takes a moment before it delivers anything, and sits at 0 until then.
            float waited = 0;
            while (NeocortexMicrophone.GetPosition(SelectedMicrophone) <= 0 && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            ringOrigin = NeocortexMicrophone.GetPosition(SelectedMicrophone);
            lastPosition = ringOrigin;
            written = 0;
            opening = false;
        }

        private void CloseDevice()
        {
            if (ring == null) return;

            NeocortexMicrophone.End(SelectedMicrophone);
            Destroy(ring);
            ring = null;
        }

        private void SyncPosition()
        {
            int position = NeocortexMicrophone.GetPosition(SelectedMicrophone);
            int delta = position - lastPosition;
            if (delta < 0)
            {
                delta += ring.samples;
            }

            written += delta;
            lastPosition = position;
        }

        /// <summary>The samples written between two positions, unwrapped from the ring.</summary>
        private float[] ReadRing(long from, long to)
        {
            int length = (int)(to - from);
            float[] result = new float[length];
            int start = (int)((ringOrigin + from) % ring.samples);

            int first = Math.Min(length, ring.samples - start);
            float[] head = new float[first];
            ring.GetData(head, start);
            Array.Copy(head, result, first);

            if (first < length)
            {
                float[] rest = new float[length - first];
                ring.GetData(rest, 0);
                Array.Copy(rest, 0, result, first, rest.Length);
            }

            return result;
        }

        private void UpdateAmplitude()
        {
            if (ring == null || written < AUDIO_SAMPLE_WINDOW) return;

            float[] audioSamples = ReadRing(written - AUDIO_SAMPLE_WINDOW, written);

            float sum = 0;
            for (int i = 0; i < AUDIO_SAMPLE_WINDOW; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }

            Amplitude = Mathf.Clamp01(sum / AUDIO_SAMPLE_WINDOW * AMPLITUDE_MULTIPLIER);
        }

        private void OnDestroy()
        {
            CloseDevice();
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
