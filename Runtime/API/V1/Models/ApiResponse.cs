using System;
using UnityEngine;

namespace Neocortex.API
{
    [Serializable]
    public class ApiResponse
    {
        public string message;
        public string action;
        public string transcription;
        public AudioClip audio;
    }
}
