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
        
        /// <summary>
        ///     Cuts the silence off both ends of a recording, keeping <paramref name="margin"/>
        ///     seconds of it so the first and last sounds are whole. Pauses inside the recording
        ///     are kept: cutting them out glued words together, which speech to text then misheard.
        ///     Returns null when the sound in it spans 0.2 seconds or less (a click, not speech).
        ///     The result is at least a second long, padded with silence at the end.
        /// </summary>
        public static AudioClip Trim(this AudioClip audioClip, float threshold = 0.001f, float margin = 0.25f)
        {
            int channels = audioClip.channels;
            float[] samples = new float[audioClip.samples * channels];
            audioClip.GetData(samples, 0);

            int firstLoud = -1;
            int lastLoud = -1;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) >= threshold)
                {
                    if (firstLoud < 0) firstLoud = i;
                    lastLoud = i;
                }
            }

            int clickFrames = Mathf.RoundToInt(audioClip.frequency * 0.2f);
            if (firstLoud < 0 || (lastLoud - firstLoud) / channels <= clickFrames)
            {
                return null;
            }

            int marginFrames = Mathf.RoundToInt(audioClip.frequency * Mathf.Max(0f, margin));
            int startFrame = Mathf.Max(0, firstLoud / channels - marginFrames);
            int endFrame = Mathf.Min(audioClip.samples, lastLoud / channels + 1 + marginFrames);
            int frames = endFrame - startFrame;

            float[] kept = new float[frames * channels];
            Array.Copy(samples, startFrame * channels, kept, 0, kept.Length);

            int lengthFrames = Mathf.Max(frames, audioClip.frequency);
            AudioClip tempClip = AudioClip.Create("TempClip", lengthFrames, channels, audioClip.frequency, false);
            tempClip.SetData(kept, 0);

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
