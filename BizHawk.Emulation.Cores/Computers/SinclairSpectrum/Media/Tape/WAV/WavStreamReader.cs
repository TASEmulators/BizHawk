using System;
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// From https://archive.codeplex.com/?p=zxmak2
    /// </summary>
    public class WavStreamReader
    {
        private Stream m_stream;
        private WavHeader m_header = new WavHeader();

        public WavStreamReader(Stream stream)
        {
            m_stream = stream;
            m_header.Deserialize(stream);
        }

        public WavHeader Header { get { return m_header; } }

        public int Count { get { return m_header.dataSize / m_header.fmtBlockAlign; } }

        public Int32 ReadNext()
        {
            // check - sample should be in PCM format
            if (m_header.fmtCode != WAVE_FORMAT_PCM &&
                m_header.fmtCode != WAVE_FORMAT_IEEE_FLOAT)
            {
                throw new FormatException(string.Format(
                    "Not supported audio format: fmtCode={0}, bitDepth={1}",
                    m_header.fmtCode,
                    m_header.bitDepth));
            }
            byte[] data = new byte[m_header.fmtBlockAlign];
            m_stream.Read(data, 0, data.Length);
            if (m_header.fmtCode == WAVE_FORMAT_PCM)
            {
                // use first channel only
                if (m_header.bitDepth == 8)
                    return getSamplePcm8(data, 0, 0);
                if (m_header.bitDepth == 16)
                    return getSamplePcm16(data, 0, 0);
                if (m_header.bitDepth == 24)
                    return getSamplePcm24(data, 0, 0);
                if (m_header.bitDepth == 32)
                    return getSamplePcm32(data, 0, 0);
            }
            else if (m_header.fmtCode == WAVE_FORMAT_IEEE_FLOAT)
            {
                // use first channel only
                if (m_header.bitDepth == 32)
                    return getSampleFloat32(data, 0, 0);
                if (m_header.bitDepth == 64)
                    return getSampleFloat64(data, 0, 0);
            }
            throw new NotSupportedException(string.Format(
                "Not supported audio format ({0}/{1} bit)",
                m_header.fmtCode == WAVE_FORMAT_PCM ? "PCM" : "FLOAT",
                m_header.bitDepth));
        }

        private Int32 getSamplePcm8(byte[] bufferRaw, int offset, int channel)
        {
            return bufferRaw[offset + channel] - 128;
        }

        private Int32 getSamplePcm16(byte[] bufferRaw, int offset, int channel)
        {
            return BitConverter.ToInt16(bufferRaw, offset + 2 * channel);
        }

        private Int32 getSamplePcm24(byte[] bufferRaw, int offset, int channel)
        {
            Int32 result;
            int subOffset = offset + channel * 3;
            if (BitConverter.IsLittleEndian)
            {
                result = ((sbyte)bufferRaw[2 + subOffset]) * 0x10000;
                result |= bufferRaw[1 + subOffset] * 0x100;
                result |= bufferRaw[0 + subOffset];
            }
            else
            {
                result = ((sbyte)bufferRaw[0 + subOffset]) * 0x10000;
                result |= bufferRaw[1 + subOffset] * 0x100;
                result |= bufferRaw[2 + subOffset];
            }
            return result;
        }

        private Int32 getSamplePcm32(byte[] bufferRaw, int offset, int channel)
        {
            return BitConverter.ToInt32(bufferRaw, offset + 4 * channel);
        }

        private Int32 getSampleFloat32(byte[] data, int offset, int channel)
        {
            float fSample = BitConverter.ToSingle(data, offset + 4 * channel);
            // convert to 32 bit integer
            return (Int32)(fSample * Int32.MaxValue);
        }

        private Int32 getSampleFloat64(byte[] data, int offset, int channel)
        {
            double fSample = BitConverter.ToDouble(data, offset + 8 * channel);
            // convert to 32 bit integer
            return (Int32)(fSample * Int32.MaxValue);
        }

        private const int WAVE_FORMAT_PCM = 1;              /* PCM */
        private const int WAVE_FORMAT_IEEE_FLOAT = 3;       /* IEEE float */
        private const int WAVE_FORMAT_ALAW = 6;             /* 8-bit ITU-T G.711 A-law */
        private const int WAVE_FORMAT_MULAW = 7;            /* 8-bit ITU-T G.711 µ-law */
        private const int WAVE_FORMAT_EXTENSIBLE = 0xFFFE;  /* Determined by SubFormat */
    }
}
