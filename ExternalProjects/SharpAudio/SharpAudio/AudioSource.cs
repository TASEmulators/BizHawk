using System;

namespace SharpAudio
{
    /// <summary>
    /// Represents an abstract source
    /// </summary>
    public abstract class AudioSource : IDisposable
    {
        protected float _volume = 1.0f;
        protected bool _looping = false;

        /// <summary>
        /// Return the number of buffers that are currently buffered
        /// </summary>
        public abstract int BuffersQueued { get; }

        /// <summary>
        /// Return the number of samples that have been played
        /// </summary>
        public abstract int SamplesPlayed { get; }

        /// <summary>
        /// Set the volume of this source. Ranges for 0 to 1.
        /// </summary>
        public abstract float Volume { get; set; }

        /// <summary>
        /// Set the volume of this source. Ranges for 0 to 1.
        /// </summary>
        public abstract bool Looping { get; set; }

        /// <summary>
        /// Queries to buffer to the playback queue
        /// </summary>
        /// <param name="buffer">the buffer to submit</param>
        public abstract void QueueBuffer(AudioBuffer buffer);

        /// <summary>
        /// Checks if this source is still playing
        /// </summary>
        /// <returns>wether or not the source is currently playing</returns>
        public abstract bool IsPlaying();

        /// <summary>
        /// Stop playback
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Start playing this source
        /// </summary>
        public abstract void Play();

        /// <summary>
        /// Clears all queried buffers. Useful for seeking etc.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Frees unmanaged device resources controlled by this instance.
        /// </summary>
        public abstract void Dispose();
    }
}
