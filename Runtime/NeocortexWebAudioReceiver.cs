using System;
using UnityEngine;
using Neocortex.Data;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Neocortex
{
    // Internal: spawned by NeocortexAudioReceiver on WebGL. Add the facade, not this. The jslib
    // calls back into this component BY GAMEOBJECT NAME, so its GameObject name must be unique
    //, the facade guarantees that by spawning it on a uniquely-named child.
    [AddComponentMenu("")]
    public class NeocortexWebAudioReceiver : AudioReceiver
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] public extern static void WebGL_Initialize(string objectName, float amplitudeThreshold, float maxWaitTime, bool usePushToTalk);
        [DllImport("__Internal")] public extern static void WebGL_StartRecording();
        [DllImport("__Internal")] public extern static void WebGL_StopRecording();
        [DllImport("__Internal")] public extern static bool WebGL_RecordingUpdatePointer(float [] idx);
        #endif
        
        private const int FREQUENCY = 48000;
        private const int BUFFER_SIZE = 2048;
        
        private AudioClip audioClip;
        private FloatArray currentBuffer;
        private MicrophoneState microphoneState = MicrophoneState.NotActive;
        private readonly List<FloatArray> binaryStreams = new List<FloatArray>();

        private bool sentPushToTalk;
        private float sentThreshold;
        private float sentWaitTime;
        private bool suppressNextEmit;

        public void Awake()
        {
            currentBuffer = new FloatArray();
            currentBuffer.Buffer = new float[BUFFER_SIZE];

            PushConfigToBrowser();

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_RecordingUpdatePointer(currentBuffer.Buffer);
            #endif
        }

        // WebGL_Initialize only sets JS-side variables, so it is safe to re-call whenever the
        // config changes at runtime (push-to-talk toggle, threshold, timeout).
        private void PushConfigToBrowser()
        {
            sentPushToTalk = UsePushToTalk;
            sentThreshold = amplitudeThreshold;
            sentWaitTime = maxWaitTime;

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_Initialize(name, amplitudeThreshold, maxWaitTime, UsePushToTalk);
            #endif
        }

        private void Update()
        {
            if (UsePushToTalk == sentPushToTalk && Mathf.Approximately(amplitudeThreshold, sentThreshold) && Mathf.Approximately(maxWaitTime, sentWaitTime))
            {
                return;
            }

            bool switchedToPushToTalk = UsePushToTalk && !sentPushToTalk;
            PushConfigToBrowser();

            // Leaving continuous listening: stop the hot browser mic without emitting the tail.
            if (switchedToPushToTalk && microphoneState != MicrophoneState.NotActive)
            {
                suppressNextEmit = true;
                StopMicrophone();
            }
        }
        
        // called from JS
        public void LogWrittenBuffer(int written)
        { 
            if(microphoneState != MicrophoneState.Recording) return;

            currentBuffer.Written = written;
            binaryStreams.Add(currentBuffer);
            currentBuffer = new FloatArray { Buffer = new float[BUFFER_SIZE] };
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_RecordingUpdatePointer(currentBuffer.Buffer);
            #endif
        }
    
        // called from JS
        public void NotifyRecordingChange(int newRecordingState)
        {
            if((int)microphoneState == newRecordingState) return;

            MicrophoneState oldState = microphoneState;
            microphoneState = (MicrophoneState)newRecordingState;

            if (microphoneState == MicrophoneState.NotActive)
            {
                AudioRecorded();
            }
        }
        
        public void UpdateAmplitude(float amplitude)
        {
            Amplitude = amplitude;
        }
        
        public void UpdateElapsedWaitTime(float elapsedWaitTime)
        {
            ElapsedWaitTime = elapsedWaitTime;
        }
        
        public override void StartMicrophone()
        {
            try
            {
                if (microphoneState == MicrophoneState.NotActive)
                {
                    microphoneState = MicrophoneState.Booting;
                    audioClip = null;
                    
                    #if UNITY_WEBGL && !UNITY_EDITOR
                    WebGL_StartRecording();
                    #endif
                }
            }
            catch (Exception e)
            {
                OnRecordingFailed?.Invoke(e.Message);
            }
        }

        public override void StopMicrophone()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_StopRecording();
            #endif
        }
        
        private float [] GetData()
        { 
            int fCt = 0;
            foreach(FloatArray fa in binaryStreams)
                fCt += fa.Written;

            float [] ret = new float[fCt];

            int write = 0;
            foreach(FloatArray fa in binaryStreams)
            { 
                Buffer.BlockCopy(fa.Buffer, 0, ret, write * 4, fa.Written * 4);
                write += fa.Written;
            }

            binaryStreams.Clear();

            return ret;
        }

        private void AudioRecorded()
        {
            if (suppressNextEmit)
            {
                // Mode-switch shutdown: discard the tail instead of surfacing it as speech.
                suppressNextEmit = false;
                binaryStreams.Clear();
                return;
            }

            float[] pcm = GetData();
            if (pcm is not { Length: > 0 }) return;
            audioClip = AudioClip.Create("", pcm.Length, 1, FREQUENCY, false);
            audioClip.SetData(pcm, 0);

            EmitRecordedClip(audioClip);
        }
    }
}
