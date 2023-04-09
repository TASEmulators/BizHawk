using System;

namespace SharpAudio
{
    /// <summary>
    /// This class represents an audio buffer, which is used to transfer data to the hardware
    /// </summary>
    public abstract class AudioBuffer : IDisposable
    {
        internal AudioFormat _format;

        /// <summary>
        /// The format of this buffer
        /// </summary>
        public AudioFormat Format => _format;

        /// <summary>
        /// Buffer data to this audio buffer
        /// </summary>
        /// <typeparam name="T">the type of data we're buffering</typeparam>
        /// <param name="buffer">The data that will be queried</param>
        /// <param name="format">How the data should be interpreted</param>
        public abstract void BufferData<T>(T[] buffer, AudioFormat format) where T : unmanaged;
        /// <summary>
        /// Buffer data to this audio buffer
        /// </summary>
        /// <typeparam name="T">the type of data we're buffering</typeparam>
        /// <param name="buffer">The data that will be queried</param>
        /// <param name="format">How the data should be interpreted</param>
        public abstract void BufferData<T>(Span<T> buffer, AudioFormat format) where T : unmanaged;
        /// <summary>
        /// Buffer data to this audio buffer
        /// </summary>
        /// <typeparam name="T">the type of data we're buffering</typeparam>
        /// <param name="buffer">A raw pointer for the data</param>
        /// <param name="sizeInBytes">A raw pointer for the data</param>
        /// <param name="format">How the data should be interpreted</param>
        public abstract void BufferData(IntPtr buffer, int sizeInBytes, AudioFormat format);
        /// <summary>
        /// Free this instance
        /// </summary>
        public abstract void Dispose();
    }
}
