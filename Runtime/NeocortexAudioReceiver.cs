using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Neocortex
{
    /// <summary>
    ///     THE microphone component: add it anywhere and voice input works on every platform.
    ///     Internally it spawns the right capture implementation for the target (standalone/mobile
    ///     vs WebGL) on a uniquely-named child, handles the Android/iOS microphone permission, and
    ///     owns microphone selection — so games never touch the platform backends directly.
    ///
    ///     Anything that takes an <see cref="AudioReceiver"/> (e.g.
    ///     <see cref="NeocortexAudioChatInput"/>) accepts it as-is. Push-to-talk, the
    ///     voice-activity threshold, and the silence timeout are configured here and passed down.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Audio Receiver", 0)]
    public class NeocortexAudioReceiver : AudioReceiver
    {
        [Tooltip("Ask for microphone permission (Android/iOS) as soon as this component starts. Off = asked on first record.")]
        [SerializeField] private bool requestPermissionOnStart = true;

        [Space] public UnityEvent OnPermissionGranted = new();
        [Space] public UnityEvent OnPermissionDenied = new();

        private AudioReceiver receiver;
        private bool permissionGranted;
        private bool permissionResolved;

        /// <summary>The platform capture implementation in use. Internal detail; exposed for advanced scenarios.</summary>
        public AudioReceiver Receiver => receiver;

        /// <summary>Available microphone devices (a single placeholder entry on WebGL — the browser owns the mic).</summary>
        public string[] Microphones => NeocortexMicrophone.devices;

        /// <summary>The saved microphone index used by the capture layer on its next start.</summary>
        public int SelectedMicrophoneIndex
        {
            get => PlayerPrefs.GetInt(MIC_INDEX_KEY, 0);
            set => PlayerPrefs.SetInt(MIC_INDEX_KEY, value);
        }

        public void SelectMicrophone(int index) => SelectedMicrophoneIndex = index;

        /// <summary>Runtime mode switch, bindable from a UI Toggle. The capture layer follows within a frame.</summary>
        public void SetPushToTalk(bool enabled) => UsePushToTalk = enabled;

        private void Awake()
        {
            if (receiver == null)
            {
                CreatePlatformReceiver();
            }
        }

        private IEnumerator Start()
        {
            if (requestPermissionOnStart)
            {
                yield return EnsurePermission();
            }
        }

        private void CreatePlatformReceiver()
        {
            // Inactive-while-configuring: the WebGL backend initializes its jslib in Awake, so
            // config must be in place before Awake runs. The unique name is what the browser JS
            // uses to call back into the right object.
            GameObject child = new GameObject($"Neocortex Audio Backend [{Guid.NewGuid():N}]");
            child.SetActive(false);
            child.transform.SetParent(transform, false);

#if UNITY_WEBGL && !UNITY_EDITOR
            AudioReceiver spawned = child.AddComponent<NeocortexWebAudioReceiver>();
#else
            AudioReceiver spawned = child.AddComponent<NeocortexNativeAudioReceiver>();
#endif
            spawned.UsePushToTalk = UsePushToTalk;
            spawned.AmplitudeThreshold = amplitudeThreshold;
            spawned.MaxWaitTime = maxWaitTime;

            Bind(spawned);
            child.SetActive(true);
        }

        /// <summary>
        ///     Advanced: swap in a custom capture source (e.g. a test feed or third-party mic
        ///     stack). Replaces and destroys the spawned platform backend.
        /// </summary>
        public void SetCustomReceiver(AudioReceiver custom)
        {
            if (custom == null || custom == this) return;

            if (receiver != null)
            {
                Unbind(receiver);
                if (receiver.transform.parent == transform)
                {
                    Destroy(receiver.gameObject);
                }
            }

            custom.UsePushToTalk = UsePushToTalk;
            custom.AmplitudeThreshold = amplitudeThreshold;
            custom.MaxWaitTime = maxWaitTime;
            Bind(custom);
        }

        private void Bind(AudioReceiver target)
        {
            receiver = target;
            receiver.OnAudioRecorded.AddListener(EmitAudioRecorded);
            receiver.OnRecordingFailed.AddListener(EmitRecordingFailed);
        }

        private void Unbind(AudioReceiver target)
        {
            target.OnAudioRecorded.RemoveListener(EmitAudioRecorded);
            target.OnRecordingFailed.RemoveListener(EmitRecordingFailed);
        }

        private void EmitAudioRecorded(AudioClip clip) => OnAudioRecorded?.Invoke(clip);
        private void EmitRecordingFailed(string error) => OnRecordingFailed?.Invoke(error);

        private void Update()
        {
            if (receiver == null) return;

            // Mirror the live capture state so UI (amplitude bars, wait rings) reads the facade.
            Amplitude = receiver.Amplitude;
            ElapsedWaitTime = receiver.ElapsedWaitTime;

            // All capture config can be changed at runtime; keep the capture layer in sync.
            if (receiver.UsePushToTalk != UsePushToTalk)
            {
                receiver.UsePushToTalk = UsePushToTalk;
            }
            if (!Mathf.Approximately(receiver.AmplitudeThreshold, amplitudeThreshold))
            {
                receiver.AmplitudeThreshold = amplitudeThreshold;
            }
            if (!Mathf.Approximately(receiver.MaxWaitTime, maxWaitTime))
            {
                receiver.MaxWaitTime = maxWaitTime;
            }
        }

        public override void StartMicrophone()
        {
            StartCoroutine(StartWhenPermitted());
        }

        public override void StopMicrophone()
        {
            if (receiver != null)
            {
                receiver.StopMicrophone();
            }
        }

        private IEnumerator StartWhenPermitted()
        {
            yield return EnsurePermission();

            if (!permissionGranted)
            {
                OnRecordingFailed?.Invoke("Microphone permission denied.");
                yield break;
            }

            receiver.StartMicrophone();
        }

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
            // Desktop: no runtime prompt. WebGL: the browser prompts via the template's JS.
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
