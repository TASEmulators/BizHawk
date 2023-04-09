using System.Collections.Generic;

namespace SharpAudio
{
    /// <summary>
    /// A circular buffer chain used for playing audio without gaps
    /// </summary>
    public sealed class BufferChain
    {
        private List<AudioBuffer> _buffers;
        private readonly int _numBuffers = 3;
        private int _currentBuffer = 0;

        /// <summary>
        /// Creates a bufferchain
        /// </summary>
        /// <param name="engine">The audio engine with which this should be created</param>
        public BufferChain(AudioEngine engine)
        {
            _buffers = new List<AudioBuffer>();

            for (int i = 0; i < _numBuffers; i++)
            {
                _buffers.Add(engine.CreateBuffer());
            }
        }

        /// <summary>
        /// Buffer data into the currently buffer and switch to the next one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer">The data to buffer</param>
        /// <param name="format">The format of the data</param>
        /// <returns></returns>
        public AudioBuffer BufferData<T>(T[] buffer, AudioFormat format) where T : unmanaged
        {
            var buf = _buffers[_currentBuffer];

            _buffers[_currentBuffer].BufferData(buffer, format);

            _currentBuffer++;
            _currentBuffer %= 3;

            return buf;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The audiosource where we want to queue the data to</param>
        /// <param name="buffer">The data to buffer</param>
        /// <param name="format">The format of the data</param>
        public void QueueData<T>(AudioSource target, T[] buffer, AudioFormat format) where T : unmanaged
        {
            var buf = BufferData(buffer, format);
            target.QueueBuffer(buf);
        }
    }
}
