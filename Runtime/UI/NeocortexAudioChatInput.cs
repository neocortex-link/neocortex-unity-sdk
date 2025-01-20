using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Neocortex
{
    public class NeocortexAudioChatInput : UIBehaviour
    {
        [SerializeField] private AudioReceiver audioReceiver;
        
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

        protected override void Start()
        {
            base.Start();
            chatState.SetActive(!audioReceiver.UsePushToTalk);
            pushToTalkButton.gameObject.SetActive(audioReceiver.UsePushToTalk);
            pushToTalkButton.OnButtonPressed.AddListener(() => OnPushToTalk(true));
            pushToTalkButton.OnButtonReleased.AddListener(() => OnPushToTalk(false));
            
            if(!audioReceiver.UsePushToTalk) 
                audioReceiver.StartMicrophone();
        }
        
        private void Update()
        {
            if (!audioReceiver.UsePushToTalk || isPushToTalkActive)
            {
                float amplitude = Mathf.Clamp(audioReceiver.Amplitude * 5, 0.1f, 1);
                Vector3 scale = new Vector3(amplitude, amplitude, 1);
                amplitudeBar.localScale = Vector3.Lerp(amplitudeBar.localScale, scale, 0.1f);
                waitingBar.fillAmount = audioReceiver.ElapsedWaitTime;
            }
        }
        
        public void SetChatState(bool isRecording)
        {
            recordingIcon.SetActive(isRecording);
            waitingButton.SetActive(!isRecording);
        }
        
        private void OnPushToTalk(bool active)
        {
            if (active)
            {
                audioReceiver.StartMicrophone();
            }
            else
            {
                audioReceiver.StopMicrophone();
            }
            
            isPushToTalkActive = active;
        }
    }
}
