using System;

namespace SharpAudio.Codec
{
    internal abstract class Decoder : IDisposable
    {
        protected AudioFormat _audioFormat;
        protected int _numSamples = 0;
        protected int _readSize;

        /// <summary>
        /// The format of the decoded data
        /// </summary>
        public AudioFormat Format => _audioFormat;

        /// <summary>
        /// Specifies the length of the decoded data. If not available returns 0
        /// </summary>
        public virtual TimeSpan Duration => TimeSpan.FromSeconds((float) _numSamples / (_audioFormat.SampleRate * _audioFormat.Channels));

        /// <summary>
        /// Specifies the current position of the decoded data. If not available returns 0
        /// </summary>
        public abstract TimeSpan Position { get; }

        /// <summary>
        /// Specifies if the decoder can return track position data or not.
        /// </summary>
        public abstract bool HasPosition { get; }

        /// <summary>
        /// Wether or not the decoder reached the end of data
        /// </summary>
        public abstract bool IsFinished { get; }

        /// <summary>
        /// Reads the specified amount of samples
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract long GetSamples(int samples, ref byte[] data);

        /// <summary>	
        /// Read all samples from this stream	
        /// </summary>	
        /// <param name="data"></param>	
        /// <returns></returns>	
        public long GetSamples(ref byte[] data)
        {
            return GetSamples(_numSamples, ref data);
        }

        /// <summary>
        /// Reads the specified amount of samples
        /// </summary>
        /// <param name="span"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public long GetSamples(TimeSpan span, ref byte[] data)
        {
            int numSamples = (int) (span.TotalSeconds * Format.SampleRate * Format.Channels);

            return GetSamples(numSamples, ref data);
        }

        public bool Probe(ref byte[] fourcc) => false;

        public virtual void Dispose()
        { }

        public virtual bool TrySeek(TimeSpan time)
        {
            return false;
        }
    }
}
