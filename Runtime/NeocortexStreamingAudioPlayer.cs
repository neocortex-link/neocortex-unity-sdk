using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Neocortex
{
    /// <summary>
    ///     Plays speech that is still being generated: samples are written into a clip as they
    ///     arrive and playback starts once there is enough of a head start to stay ahead of them.
    /// </summary>
    /// <remarks>
    ///     Unity cannot grow an <see cref="AudioClip"/>, so one is allocated with room for a long
    ///     reply and the written length is tracked separately. If the network falls behind the
    ///     playhead the source is paused rather than allowed to play silence, and resumes once the
    ///     buffer has refilled.
    /// </remarks>
    internal sealed class NeocortexStreamingAudioPlayer
    {
        /// <summary>Longest reply that can be buffered. A voiced reply runs to a few seconds.</summary>
        private const float CAPACITY_SECONDS = 60f;

        /// <summary>How much audio must be buffered before playback starts or resumes.</summary>
        private const float PREBUFFER_SECONDS = 0.15f;

        private readonly AudioSource source_nx;

        private AudioClip clip;
        private int sampleRate;
        private int channels = 1;
        private int written;
        private bool complete;
        private bool started;
        private bool overflowed;

        public NeocortexStreamingAudioPlayer(AudioSource source)
        {
            source_nx = source;
        }

        /// <summary>True once playback has begun and has not finished.</summary>
        public bool IsPlaying => started && source_nx != null && source_nx.isPlaying;

        /// <summary>True once playback started, whether or not it is currently paused for buffering.</summary>
        public bool HasStarted => started;

        /// <summary>Allocates the clip. Called when the format is known, before any samples.</summary>
        public void Begin(int rate, int channelCount)
        {
            Reset();

            sampleRate = rate > 0 ? rate : 24000;
            channels = channelCount > 0 ? channelCount : 1;
            clip = AudioClip.Create("neocortex-stream", Mathf.CeilToInt(sampleRate * CAPACITY_SECONDS), channels, sampleRate, false);
        }

        /// <summary>Appends audio. Safe to call before <see cref="Begin"/>, in which case it is dropped.</summary>
        public void Feed(float[] samples)
        {
            if (clip == null || samples == null || samples.Length == 0) return;

            // Writes stay on frame boundaries; a half-frame would swap the channels of
            // everything after it.
            if (channels > 1 && samples.Length % channels != 0)
            {
                Array.Resize(ref samples, samples.Length - (samples.Length % channels));
                if (samples.Length == 0) return;
            }

            int room = clip.samples * channels - written;
            if (room <= 0)
            {
                if (!overflowed)
                {
                    overflowed = true;
                    Debug.LogWarning($"[Neocortex] Streamed reply exceeded {CAPACITY_SECONDS} seconds; the rest is not played.");
                }
                return;
            }

            if (samples.Length > room)
            {
                Array.Resize(ref samples, room - (room % channels));
                if (samples.Length == 0) return;
            }

            clip.SetData(samples, written / channels);
            written += samples.Length;
        }

        /// <summary>Marks the end of the audio, so playback can stop when the playhead reaches it.</summary>
        public void Complete()
        {
            complete = true;
        }

        /// <summary>
        ///     Drives playback. Call once per frame while a reply is streaming: it starts the source
        ///     when enough is buffered, pauses on an underrun and resumes when the buffer recovers.
        /// </summary>
        public void Tick()
        {
            if (clip == null || source_nx == null) return;

            int prebuffer = Mathf.CeilToInt(PREBUFFER_SECONDS * sampleRate) * channels;

            if (!started)
            {
                // Start once there is a head start, or immediately when the whole (short)
                // line has already arrived.
                if (written >= prebuffer || (complete && written > 0))
                {
                    source_nx.clip = clip;
                    source_nx.timeSamples = 0;
                    source_nx.Play();
                    started = true;
                }
                return;
            }

            int playhead = source_nx.timeSamples * channels;

            // Everything written has been heard. Only the end of the audio ends playback;
            // a source paused for buffering also reports "not playing", so it must never
            // be mistaken for a finished line.
            if (playhead >= written)
            {
                if (complete)
                {
                    Stop();
                }
                else if (source_nx.isPlaying)
                {
                    source_nx.Pause();
                }
                return;
            }

            if (source_nx.isPlaying)
            {
                return;
            }

            // There is more audio than the playhead has reached. Resume once enough has
            // built up again, or immediately when this is the last of it, however short.
            if (complete || written - playhead >= prebuffer)
            {
                source_nx.UnPause();
            }
        }

        /// <summary>True once the whole line has arrived and been heard.</summary>
        public bool IsFinished => complete && (!started || clip == null || source_nx == null || !source_nx.isPlaying);

        /// <summary>Drives playback until the line has been fully heard, the token changes, or the object goes away.</summary>
        public async Task WaitUntilFinished(Func<bool> stillValid)
        {
            while (stillValid())
            {
                Tick();
                if (IsFinished) return;
                await Task.Yield();
            }
        }

        /// <summary>The audio actually received, sized to what was written, for listeners that want a clip.</summary>
        public AudioClip ToTrimmedClip()
        {
            if (clip == null || written <= 0) return null;

            float[] samples = new float[written];
            clip.GetData(samples, 0);

            AudioClip trimmed = AudioClip.Create("neocortex-stream-complete", written / channels, channels, sampleRate, false);
            trimmed.SetData(samples, 0);
            return trimmed;
        }

        public void Stop()
        {
            if (source_nx != null && source_nx.isPlaying)
            {
                source_nx.Stop();
            }

            complete = true;
        }

        public void Reset()
        {
            Stop();
            clip = null;
            written = 0;
            started = false;
            complete = false;
            overflowed = false;
        }
    }
}
