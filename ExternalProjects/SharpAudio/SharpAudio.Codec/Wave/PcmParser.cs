using System.IO;

namespace SharpAudio.Codec.Wave
{
    internal class PcmParser : WavParser
    {
        private int _bitsPerSample;

        public override int BitsPerSample => _bitsPerSample;

        public override byte[] Parse(BinaryReader reader, int size, WaveFormat format)
        {
            _bitsPerSample = format.BitsPerSample;
            return reader.ReadBytes(size);
        }
    }
}
