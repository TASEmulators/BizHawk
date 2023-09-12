using System;
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// From https://archive.codeplex.com/?p=zxmak2
	/// </summary>
	public class WavStreamReader
	{
		private readonly Stream m_stream;

		public WavStreamReader(Stream stream)
		{
			m_stream = stream;
			Header.Deserialize(stream);
		}

		public WavHeader Header { get; } = new();

		public int Count => Header.dataSize / Header.fmtBlockAlign;

		public int ReadNext()
		{
			// check - sample should be in PCM format
			if (Header.fmtCode is not WAVE_FORMAT_PCM and
				not WAVE_FORMAT_IEEE_FLOAT)
			{
				throw new FormatException($"Not supported audio format: fmtCode={Header.fmtCode}, bitDepth={Header.bitDepth}");
			}
			byte[] data = new byte[Header.fmtBlockAlign];
			m_stream.Read(data, 0, data.Length);
			if (Header.fmtCode == WAVE_FORMAT_PCM)
			{
				// use first channel only
				if (Header.bitDepth == 8)
					return getSamplePcm8(data, 0, 0);
				if (Header.bitDepth == 16)
					return getSamplePcm16(data, 0, 0);
				if (Header.bitDepth == 24)
					return getSamplePcm24(data, 0, 0);
				if (Header.bitDepth == 32)
					return getSamplePcm32(data, 0, 0);
			}
			else if (Header.fmtCode == WAVE_FORMAT_IEEE_FLOAT)
			{
				// use first channel only
				if (Header.bitDepth == 32)
					return getSampleFloat32(data, 0, 0);
				if (Header.bitDepth == 64)
					return getSampleFloat64(data, 0, 0);
			}
			throw new NotSupportedException($"Not supported audio format ({(Header.fmtCode == WAVE_FORMAT_PCM ? "PCM" : "FLOAT")}/{Header.bitDepth} bit)");
		}

		private int getSamplePcm8(byte[] bufferRaw, int offset, int channel) => bufferRaw[offset + channel] - 128;

		private int getSamplePcm16(byte[] bufferRaw, int offset, int channel) => BitConverter.ToInt16(bufferRaw, offset + 2 * channel);

		private int getSamplePcm24(byte[] bufferRaw, int offset, int channel)
		{
			int result;
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

		private int getSamplePcm32(byte[] bufferRaw, int offset, int channel) => BitConverter.ToInt32(bufferRaw, offset + 4 * channel);

		private int getSampleFloat32(byte[] data, int offset, int channel)
		{
			float fSample = BitConverter.ToSingle(data, offset + 4 * channel);
			// convert to 32 bit integer
			return (int) (fSample * int.MaxValue);
		}

		private int getSampleFloat64(byte[] data, int offset, int channel)
		{
			double fSample = BitConverter.ToDouble(data, offset + 8 * channel);
			// convert to 32 bit integer
			return (int) (fSample * int.MaxValue);
		}

		#pragma warning disable IDE0051
		private const int WAVE_FORMAT_PCM = 1;              /* PCM */
		private const int WAVE_FORMAT_IEEE_FLOAT = 3;       /* IEEE float */
		private const int WAVE_FORMAT_ALAW = 6;             /* 8-bit ITU-T G.711 A-law */
		private const int WAVE_FORMAT_MULAW = 7;            /* 8-bit ITU-T G.711 µ-law */
		private const int WAVE_FORMAT_EXTENSIBLE = 0xFFFE;  /* Determined by SubFormat */
		#pragma warning restore IDE0051
	}
}
