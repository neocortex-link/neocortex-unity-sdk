using Neocortex;
using UnityEngine;
using UnityEngine.UI;

public class TranscriptionTest : MonoBehaviour
{
    [SerializeField] private Image amplitudeBar;
    [SerializeField] private Text messages;
    [SerializeField] private AudioSource audioSource;

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
        if (audioReceiver)
        {
            amplitudeBar.fillAmount = audioReceiver.Amplitude;
        }
    }

    private async void OnAudioRecorded(byte[] data)
    { 
        audioSource.clip = await agent.Send(data);
        audioSource.Play();
    }
}
