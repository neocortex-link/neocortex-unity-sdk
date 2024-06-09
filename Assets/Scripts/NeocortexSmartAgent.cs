using UnityEngine;
using Neocortex.API;
using System.Threading.Tasks;

public class NeocortexSmartAgent : MonoBehaviour
{
    [SerializeField] private string id;

    private ChatRequest chatRequest;
    private AudioRequest audioRequest;

    private void Awake()
    {
        chatRequest = new ChatRequest();
        audioRequest = new AudioRequest();
    }

    public async Task<string> Send(string message)
    {
        ApiResponse response = await chatRequest.Send(id, message);
        return response.message;
    }
    
    public async Task<AudioClip> Send(byte[] audio)
    {
        ApiResponse response = await audioRequest.Send(id, audio);
        return response.audio;
    }
}
