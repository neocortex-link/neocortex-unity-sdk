using System;
using UnityEngine;
using Neocortex.Data;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Neocortex
{
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
        
        [SerializeField, Range(0, 1)] private float amplitudeThreshold = 0.1f;
        [SerializeField] private float maxWaitTime = 1f;
        
        public void Awake()
        {
            currentBuffer = new FloatArray();
            currentBuffer.Buffer = new float[BUFFER_SIZE];
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGL_Initialize(name, amplitudeThreshold, maxWaitTime, UsePushToTalk);
            WebGL_RecordingUpdatePointer(currentBuffer.Buffer);
            #endif
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
            Debug.Log("NotifyRecordingChange: " + newRecordingState);
            
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
            float[] pcm = GetData();
            if (pcm is not { Length: > 0 }) return;
            audioClip = AudioClip.Create("", pcm.Length, 1, FREQUENCY, false);
            audioClip.SetData(pcm, 0);
            
            AudioClip trimmed = audioClip.Trim();
            OnAudioRecorded?.Invoke(trimmed);
        }
    }
}
