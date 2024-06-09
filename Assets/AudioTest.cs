using Neocortex;
using UnityEngine;
using UnityEngine.UI;

public class AudioTest : MonoBehaviour
{
    [SerializeField] private Image amplitudeBar;
    [SerializeField] private Text messages;

    private AudioReceiver audioReceiver;
    private NeocortexSmartAgent agent;
    
    private bool isRecording;
    
    private void Start()
    {
        audioReceiver = GetComponent<AudioReceiver>();
        agent = GetComponent<NeocortexSmartAgent>();
        audioReceiver.OnAudioRecorded += OnAudioRecorded;
        audioReceiver.StartMicrophone();
    }
    
    private void Update()
    {
        if (audioReceiver != null)
        {
            amplitudeBar.fillAmount = audioReceiver.Amplitude;
        }
    }

    private async void OnAudioRecorded(byte[] data)
    { 
        string transcription = await agent.Send(data);
        messages.text = transcription;
    }
}
