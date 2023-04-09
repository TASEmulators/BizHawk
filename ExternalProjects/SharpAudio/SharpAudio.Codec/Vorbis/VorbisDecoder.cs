using System;
using System.IO;
using NVorbis;

namespace SharpAudio.Codec.Vorbis
{
    internal class VorbisDecoder : Decoder
    {
        private VorbisReader _reader;
        private float[] _readBuf;

        public override bool IsFinished => _reader.IsEndOfStream;

        public override TimeSpan Position => TimeSpan.MinValue;

        public override bool HasPosition { get; } = false;

        public VorbisDecoder(Stream s)
        {
            _reader = new VorbisReader(s, true);

            _audioFormat.Channels = _reader.Channels;
            _audioFormat.BitsPerSample = 16;
            _audioFormat.SampleRate = _reader.SampleRate;

            _numSamples = (int) _reader.TotalSamples;
        }

        private static void CastBuffer(float[] inBuffer, byte[] outBuffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                var temp = (int) (short.MaxValue * inBuffer[i]);

                if (temp > short.MaxValue)
                {
                    temp = short.MaxValue;
                }
                else if (temp < short.MinValue)
                {
                    temp = short.MinValue;
                }

                outBuffer[2 * i] = (byte) (((short) temp) & 0xFF);
                outBuffer[2 * i + 1] = (byte) (((short) temp) >> 8);
            }
        }

        public override long GetSamples(int samples, ref byte[] data)
        {
            int bytes = _audioFormat.BytesPerSample * samples;
            Array.Resize(ref data, bytes);

            Array.Resize(ref _readBuf, samples);
            _reader.ReadSamples(_readBuf, 0, samples);

            CastBuffer(_readBuf, data, samples);

            return samples;
        }
    }
}
