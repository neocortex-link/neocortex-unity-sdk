using UnityEngine;

namespace Neocortex
{
    public sealed class NeocortexMicrophone
    {
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            return Microphone.Start(deviceName, loop, lengthSec, frequency);
            #endif
            
            return null;
        }
        
        public static void End(string deviceName)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            Microphone.End(deviceName);
            #endif
            
            return;
        }
        
        public static int GetPosition(string deviceName)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            return Microphone.GetPosition(deviceName);
            #endif
            
            return 0;
        }
    }
}