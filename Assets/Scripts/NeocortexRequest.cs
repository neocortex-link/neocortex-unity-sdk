using Neocortex.Data;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
/*
public struct Response
{
    public string message;
    public string action;
    public string transcription;
}

public class NeocortexRequest : MonoBehaviour
{
    private const string BASE_URL = "https://api.neocortex.link/v1/chat";
    private const string BASE_CHAT_URL = "https://api.neocortex.link/v1/chat";
    private const string BASE_AUDIO_URL = "https://api.neocortex.link/v1/audio";
    private const string BASE_TRANSCRIBE_URL = "https://api.neocortex.link/v1/transcribe";
    
    [SerializeField] string apiKey;
    [SerializeField] string projectId;
    
    [SerializeField] private AutoDetect audioHandler;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Text subtitle;
    [SerializeField] private GameObject robotThinking;
    
    private List<Message> messages = new List<Message>();
    
    private Response chatResponse;
    private Response transcribeResponse;
    
    public Action<string> OnActionReceived;
    
    private void Start()
    {
        audioHandler.OnAudioRecorded += SendAudioRequest;
    }
    
    private void SendAudioRequest(byte[] audio)
    {
        StartCoroutine(PostAudio(audio));
    }
    
    private IEnumerator PostAudio(byte[] audio)
    {
        robotThinking.SetActive(true);
        
        audioHandler.StopMicrophone();
        
        string url = $"{BASE_TRANSCRIBE_URL}/{projectId}";
        
        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("x-api-key", apiKey);
        request.uploadHandler = new UploadHandlerRaw(audio);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        yield return request.SendWebRequest();
            
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);
            
            chatResponse = JsonConvert.DeserializeObject<Response>(request.downloadHandler.text);
            
            if(chatResponse.transcription == "")
            {
                robotThinking.SetActive(false);
                audioHandler.StartMicrophone();
                yield break;
            }
            
            subtitle.text = $"YOU: <color=magenta>{chatResponse.transcription}</color>\n\n";

            messages.Add(new Message
            {
                content = chatResponse.transcription,
                role = "user"
            });
            
            var payload_msg = new
            {
                messages = messages.ToArray()
            };
        
            string json_msg = JsonConvert.SerializeObject(payload_msg);
            byte[] bodyRaw_msg = System.Text.Encoding.UTF8.GetBytes(json_msg);
        
            string url_msg = $"{BASE_CHAT_URL}/{projectId}";
        
            using UnityWebRequest request_msg = new UnityWebRequest(url_msg, "POST");
            request_msg.SetRequestHeader("Content-Type", "application/json");
            request_msg.SetRequestHeader("x-api-key", apiKey);
        
            request_msg.uploadHandler = new UploadHandlerRaw(bodyRaw_msg);
            request_msg.downloadHandler = new DownloadHandlerBuffer();
        
            yield return request_msg.SendWebRequest();
            
            if (request_msg.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request_msg.error);
            }
            else
            {
                Debug.Log(request_msg.downloadHandler.text);
                
                var parsed = request_msg.downloadHandler.text
                    .Replace("0:", "")
                    .Replace("\"\n\"", "")
                    .Replace("\n", "")
                    .Replace("\\n", "")
                    .Replace("\\", "");
                
                parsed = parsed.Substring(1);
                
                parsed = parsed.Substring(0, parsed.Length - 1);

                transcribeResponse = JsonConvert.DeserializeObject<Response>(parsed);
                
                subtitle.text += $"ROBO: <color=cyan>{transcribeResponse.message}</color>";

                messages.Add(new Message
                {
                    content = transcribeResponse.message,
                    role = "assistant"
                });
            }
            
            string url_audio = $"{BASE_AUDIO_URL}/{projectId}";
            
            var payload = new
            {
                text = transcribeResponse.message
            };
        
            string json = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            
            using UnityWebRequest request_audio = new UnityWebRequest(url_audio, "POST");
            request_audio.SetRequestHeader("x-api-key", apiKey);
            request_audio.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request_audio.downloadHandler = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
            
            yield return request_audio.SendWebRequest();
            
            if (request_audio.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request_audio.error);
            }
            else
            {
                OnActionReceived?.Invoke(transcribeResponse.action);
                InterfaceManager.Instance.CompleteQuest(1);
                robotThinking.SetActive(false);
                
                audioSource.clip = DownloadHandlerAudioClip.GetContent(request_audio);
                audioSource.Play();
                
                yield return new WaitForSeconds(audioSource.clip.length);
                
                audioHandler.StartMicrophone();
            }
        }
    }
}
*/