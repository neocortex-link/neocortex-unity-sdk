using UnityEngine;
using Neocortex.API;
using System.Threading.Tasks;

public class NeocortexSmartAgent : MonoBehaviour
{
    [SerializeField] private string id;

    private ChatRequest chatRequest;
    private TranscriptionRequest transcriptionRequest;

    private void Awake()
    {
        chatRequest = new ChatRequest();
        transcriptionRequest = new TranscriptionRequest();
    }

    public async Task<string> Send(string message)
    {
        ApiResponse response = await chatRequest.Send(id, message);
        return response.message;
    }
    
    public async Task<string> Send(byte[] audio)
    {
        ApiResponse response = await transcriptionRequest.Send(id, audio);
        return response.transcription;
    }
}
