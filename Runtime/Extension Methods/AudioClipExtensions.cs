using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Neocortex
{
    public static class AudioClipExtensions
    {
        private const int HeaderSize = 44;
        
        public static AudioClip Trim(this AudioClip audioClip, float threshold = 0.01f, float minSilenceDuration = 0.1f)
        {
            float[] samples = new float[audioClip.samples];
            audioClip.GetData(samples, 0);
            int sampleRate = audioClip.frequency;
            int channels = audioClip.channels;
            
            int minSilenceSamples = (int)(minSilenceDuration * sampleRate);

            List<float> newSamples = new List<float>();
            int i = 0;
            while (i < samples.Length)
            {
                if (Mathf.Abs(samples[i]) >= threshold)
                {
                    newSamples.Add(samples[i]);
                    i++;
                }
                else
                {
                    int silenceStart = i;
                    while (i < samples.Length && Mathf.Abs(samples[i]) < threshold)
                    {
                        i++;
                    }
                    int silenceLength = i - silenceStart;
                    
                    if (silenceLength < minSilenceSamples)
                    {
                        for (int j = silenceStart; j < i; j++)
                        {
                            newSamples.Add(samples[j]);
                        }
                    }
                }
            }
            
            if (newSamples.Count < sampleRate / 4)
            {
                return null;
            }
            
            int newLength = Mathf.Max(newSamples.Count, sampleRate);
            AudioClip tempClip = AudioClip.Create("TempClip", newLength, channels, sampleRate, false);
            tempClip.SetData(newSamples.ToArray(), 0);
            
            return tempClip;
        }
        
        public static byte[] EncodeToWav(this AudioClip clip)
        {
            return Encode(clip);
        }

        private static byte[] Encode(AudioClip clip)
        {
            using var memoryStream = CreateEmpty();
            Convert(memoryStream, clip);
            WriteHeader(memoryStream, clip);
            byte[] bytes = memoryStream.GetBuffer();
            
            // Check out the result for audio quality
            // File.WriteAllBytes(Application.persistentDataPath + "/input.wav", bytes);

            return bytes;
        }

        private static MemoryStream CreateEmpty()
        {
            var memoryStream = new MemoryStream();
            byte emptyByte = new byte();

            for (int i = 0; i < HeaderSize; i++)
            {
                memoryStream.WriteByte(emptyByte);
            }

            return memoryStream;
        }

        private static void Convert(MemoryStream memoryStream, AudioClip clip)
        {
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            Int16[] intData = new Int16[samples.Length];

            Byte[] bytesData = new Byte[samples.Length * 2];

            int rescaleFactor = Int16.MaxValue;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] bytes = BitConverter.GetBytes(intData[i]);
                bytes.CopyTo(bytesData, i * 2);
            }

            memoryStream.Write(bytesData, 0, bytesData.Length);
        }

        private static void WriteHeader(MemoryStream memoryStream, AudioClip clip)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            memoryStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            memoryStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(memoryStream.Length - 8);
            memoryStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            memoryStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            memoryStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            memoryStream.Write(subChunk1, 0, 4);

            UInt16 one = 1;

            Byte[] audioFormat = BitConverter.GetBytes(one);
            memoryStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            memoryStream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            memoryStream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
            memoryStream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            memoryStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            memoryStream.Write(bitsPerSample, 0, 2);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            memoryStream.Write(datastring, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            memoryStream.Write(subChunk2, 0, 4);

            memoryStream.Close();
        }
    }
}
