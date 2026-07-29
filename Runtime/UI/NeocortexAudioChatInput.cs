using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("Neocortex/Neocortex Audio Chat Input", 0)]
    public class NeocortexAudioChatInput : UIBehaviour
    {
        [Tooltip("The receiver this widget drives. Set before Start.")]
        public AudioReceiver voiceInput;

        [Header("Amplitude Bar")]
        [SerializeField] private RectTransform amplitudeBar;
        [SerializeField] private Image waitingBar;
        
        [Header("Chat State")]
        [SerializeField] private GameObject chatState;
        [SerializeField] private GameObject recordingIcon;
        [SerializeField] private GameObject waitingButton;
        
        [Header("Push To Talk")]
        [SerializeField] private NeocortexPushToTalkButton pushToTalkButton;
        private bool isPushToTalkActive;
        private bool pushToTalkMode;

        protected override void Start()
        {
            base.Start();

            if (voiceInput == null)
            {
                Debug.LogWarning("[Neocortex] Audio Chat Input has no receiver assigned, add a NeocortexAudioReceiver and drag it in (or let NeocortexChatUI wire it).", this);
                enabled = false;
                return;
            }

            pushToTalkButton.OnButtonPressed.AddListener(OnButtonPressed);
            pushToTalkButton.OnButtonReleased.AddListener(OnButtonReleased);

            ApplyMode();
        }

        // Reflects the receiver's current mode; called again automatically when it changes at runtime.
        private void ApplyMode()
        {
            pushToTalkMode = voiceInput.usePushToTalk;
            isPushToTalkActive = false;

            chatState.SetActive(!pushToTalkMode);
            pushToTalkButton.gameObject.SetActive(pushToTalkMode);

            if (!pushToTalkMode)
            {
                voiceInput.StartMicrophone(); // voice-activity mode listens continuously
            }
        }

        private void Update()
        {
            // The mode can be flipped at runtime (settings toggle, code), follow it live.
            if (voiceInput.usePushToTalk != pushToTalkMode)
            {
                ApplyMode();
            }

            if (!voiceInput.usePushToTalk || isPushToTalkActive)
            {
                float amplitude = Mathf.Clamp(voiceInput.Amplitude * 5, 0.1f, 1);
                Vector3 scale = new Vector3(amplitude, amplitude, 1);
                amplitudeBar.localScale = Vector3.Lerp(amplitudeBar.localScale, scale, 0.1f);
                waitingBar.fillAmount = voiceInput.ElapsedWaitTime;
            }
        }
        
        public void SetChatState(bool isRecording)
        {
            recordingIcon.SetActive(isRecording);
            waitingButton.SetActive(!isRecording);
        }

        private void OnButtonPressed()
        {
            if (!isPushToTalkActive)
            {
                voiceInput.StartMicrophone();
                isPushToTalkActive = true;
            }
        }
        
        private void OnButtonReleased()
        {
            if (isPushToTalkActive)
            {
                voiceInput.StopMicrophone();
                isPushToTalkActive = false;
            }
        }
    }
}
