using System;

namespace SharpAudio
{
    /// <summary>
    /// Represents an abstract source
    /// </summary>
    public abstract class Submixer : IDisposable
    {
        protected float _volume = 1.0f;

        /// <summary>
        /// Set the volume of this source. Ranges for 0 to 1.
        /// </summary>
        public abstract float Volume { get; set; }

        public abstract void Dispose();
    }
}
