using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Neocortex.API
{
    /// <summary>
    ///     Reads a streamed WAV body and hands over samples as they arrive, so a voice line starts
    ///     playing while the rest of it is still being synthesized.
    /// </summary>
    /// <remarks>
    ///     The header declares an unknown length, since the server cannot know the size until
    ///     synthesis ends. Samples are converted to the float form <see cref="AudioClip.SetData"/>
    ///     wants; a sample split across two network chunks is carried over rather than dropped.
    /// </remarks>
    public sealed class PcmStreamDownloadHandler : DownloadHandlerScript
    {
        private const int DEFAULT_BUFFER_SIZE = 16 * 1024;
        private const int HEADER_MIN_BYTES = 44;

        private readonly Action<int, int> onFormat_nx;
        private readonly Action<float[]> onSamples;
        private readonly List<byte> carry = new();

        private bool headerParsed;
        private bool headerRejected;
        private byte[] headerScan = Array.Empty<byte>();

        /// <summary>Sample rate read from the header, 0 until it arrives.</summary>
        public int SampleRate { get; private set; }

        /// <summary>Channel count read from the header, 0 until it arrives.</summary>
        public int Channels { get; private set; }

        /// <summary>True when the body was not a WAV at all, which means the server sent an error instead.</summary>
        public bool IsNotAudio => headerRejected;

        /// <summary>The body as text. Only meaningful when <see cref="IsNotAudio"/> is true.</summary>
        public string RawText { get; private set; } = string.Empty;

        /// <param name="onFormat">Raised once with sample rate and channels, before any samples.</param>
        /// <param name="onSamples">Raised per chunk with the samples decoded from it.</param>
        /// <param name="bufferSize">Size of the reusable read buffer.</param>
        public PcmStreamDownloadHandler(Action<int, int> onFormat, Action<float[]> onSamples, int bufferSize = DEFAULT_BUFFER_SIZE)
            : base(new byte[bufferSize])
        {
            onFormat_nx = onFormat;
            this.onSamples = onSamples;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            // Returning false aborts the request, so an empty read must still say yes.
            if (data == null || dataLength <= 0)
            {
                return true;
            }

            if (headerRejected)
            {
                RawText += Encoding.UTF8.GetString(data, 0, dataLength);
                return true;
            }

            if (!headerParsed)
            {
                ConsumeHeader(data, dataLength);
                return true;
            }

            EmitSamples(data, 0, dataLength);
            return true;
        }

        protected override void CompleteContent()
        {
            // A body too short to hold a header never was audio.
            if (!headerParsed && !headerRejected && headerScan.Length > 0)
            {
                headerRejected = true;
                RawText = Encoding.UTF8.GetString(headerScan);
            }
        }

        protected override string GetText() => RawText;

        // The header can be split across chunks like anything else, so it is
        // accumulated until there is enough of it to read.
        private void ConsumeHeader(byte[] data, int dataLength)
        {
            int previous = headerScan.Length;
            Array.Resize(ref headerScan, previous + dataLength);
            Array.Copy(data, 0, headerScan, previous, dataLength);

            if (headerScan.Length >= 4 &&
                (headerScan[0] != 'R' || headerScan[1] != 'I' || headerScan[2] != 'F' || headerScan[3] != 'F'))
            {
                headerRejected = true;
                RawText = Encoding.UTF8.GetString(headerScan);
                headerScan = Array.Empty<byte>();
                return;
            }

            if (headerScan.Length < HEADER_MIN_BYTES)
            {
                return;
            }

            int dataOffset = ReadFormat(headerScan);
            if (dataOffset < 0)
            {
                headerRejected = true;
                RawText = Encoding.UTF8.GetString(headerScan);
                headerScan = Array.Empty<byte>();
                return;
            }

            headerParsed = true;

            try
            {
                onFormat_nx?.Invoke(SampleRate, Channels);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // Whatever followed the header in this same chunk is already audio.
            if (headerScan.Length > dataOffset)
            {
                EmitSamples(headerScan, dataOffset, headerScan.Length - dataOffset);
            }

            headerScan = Array.Empty<byte>();
        }

        /// <summary>Reads the format chunk and returns where the samples start, or -1 if this is not PCM.</summary>
        private int ReadFormat(byte[] header)
        {
            if (header.Length < 12) return -1;
            if (header[8] != 'W' || header[9] != 'A' || header[10] != 'V' || header[11] != 'E') return -1;

            int offset = 12;
            while (offset + 8 <= header.Length)
            {
                string chunkId = Encoding.ASCII.GetString(header, offset, 4);
                uint chunkSize = BitConverter.ToUInt32(header, offset + 4);

                if (chunkId == "fmt ")
                {
                    Channels = BitConverter.ToUInt16(header, offset + 10);
                    SampleRate = (int)BitConverter.ToUInt32(header, offset + 12);
                    int bitsPerSample = BitConverter.ToUInt16(header, offset + 22);
                    if (bitsPerSample != 16) return -1;
                }
                else if (chunkId == "data")
                {
                    return SampleRate > 0 && Channels > 0 ? offset + 8 : -1;
                }

                // Chunks are word aligned; an odd size carries a pad byte.
                offset += 8 + (int)chunkSize + (int)(chunkSize % 2);
            }

            return -1;
        }

        private void EmitSamples(byte[] data, int start, int length)
        {
            for (int i = 0; i < length; i++)
            {
                carry.Add(data[start + i]);
            }

            // An odd byte at the end is the first half of a sample whose second half
            // is still in flight, so it stays for the next chunk.
            int usable = carry.Count - (carry.Count % 2);
            if (usable <= 0) return;

            float[] samples = new float[usable / 2];
            for (int i = 0; i < usable; i += 2)
            {
                samples[i / 2] = ToFloat(carry[i], carry[i + 1]);
            }

            carry.RemoveRange(0, usable);

            try
            {
                onSamples?.Invoke(samples);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static float ToFloat(byte low, byte high)
        {
            short value = (short)(low | (high << 8));
            return value / 32768f;
        }
    }
}
