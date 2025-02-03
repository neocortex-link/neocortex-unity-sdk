using UnityEngine;

namespace Neocortex
{
    public sealed class NeocortexMicrophone
    {
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            return Microphone.Start(deviceName, loop, lengthSec, frequency);
            #else
            return null;
            #endif
        }
        
        public static void End(string deviceName)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            Microphone.End(deviceName);
            #else
            return null;
            #endif
        }
        
        public static int GetPosition(string deviceName)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            return Microphone.GetPosition(deviceName);
            #else
            return 0;
            #endif
        }
    }
}
