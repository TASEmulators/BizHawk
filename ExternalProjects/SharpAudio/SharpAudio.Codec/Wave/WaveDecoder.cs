using System;
using System.IO;

namespace SharpAudio.Codec.Wave
{
    internal abstract class WavParser
    {
        public abstract int BitsPerSample { get; }

        public abstract byte[] Parse(BinaryReader reader, int size, WaveFormat format);

        public static WavParser GetParser(WaveFormatType type)
        {
            switch (type)
            {
                case WaveFormatType.Pcm: return new PcmParser();
                case WaveFormatType.DviAdpcm: return new DviAdpcmParser();
                default: throw new NotSupportedException("Invalid or unknown .wav compression format!");
            }
        }
    }

    internal class WaveDecoder : Decoder
    {
        private RiffHeader _header;
        private WaveFormat _format;
        private WaveFact _fact;
        private WaveData _data;
        private int _samplesLeft;
        private byte[] _decodedData;

        internal WaveDecoder(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s))
            {
                _header = RiffHeader.Parse(br);
                _format = WaveFormat.Parse(br);

                if (_format.AudioFormat != WaveFormatType.Pcm)
                {
                    _fact = WaveFact.Parse(br);
                }

                _data = WaveData.Parse(br);
                var variant = WavParser.GetParser(_format.AudioFormat);

                _decodedData = variant.Parse(br, (int) _data.SubChunkSize, _format);

                _audioFormat.BitsPerSample = variant.BitsPerSample;
                _audioFormat.Channels = _format.NumChannels;
                _audioFormat.SampleRate = (int) _format.SampleRate;

                _numSamples = _samplesLeft = _decodedData.Length / _audioFormat.BytesPerSample;
            }
        }

        public override bool IsFinished => _samplesLeft == 0;

        public override TimeSpan Position => TimeSpan.MinValue;

        public override bool HasPosition { get; } = false;

        public override long GetSamples(int samples, ref byte[] data)
        {
            int numSamples = Math.Min(samples, _samplesLeft);
            long byteSize = _audioFormat.BytesPerSample * numSamples;
            long byteOffset = (_numSamples - _samplesLeft) * _audioFormat.BytesPerSample;

            data = _decodedData.AsSpan<byte>().Slice((int) (byteOffset), (int) byteSize).ToArray();
            _samplesLeft -= numSamples;

            return numSamples;
        }
    }
}
