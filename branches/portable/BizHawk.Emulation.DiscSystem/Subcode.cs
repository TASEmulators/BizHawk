using System;

//a decent little subcode reference
//http://www.jbum.com/cdg_revealed.html

namespace BizHawk.Emulation.DiscSystem
{
	public interface ISubcodeSector
	{
		/// <summary>
		/// reads 96 bytes of subcode data (deinterleaved) for this sector into the supplied buffer
		/// </summary>
		void ReadSubcodeDeinterleaved(byte[] buffer, int offset);

		/// <summary>
		/// Reads just one of the channels. p=0, q=1, etc.
		/// </summary>
		void ReadSubcodeChannel(int number, byte[] buffer, int offset);
	}

	/// <summary>
	/// Reads subcode from an internally-managed buffer
	/// </summary>
	class BufferedSubcodeSector : ISubcodeSector
	{
		/// <summary>
		/// Fills this subcode buffer with subchannel Q data. calculates the required CRC, as well.
		/// Returns the crc, calculated or otherwise.
		/// </summary>
		public ushort Synthesize_SubchannelQ(ref SubchannelQ sq, bool calculateCRC)
		{
			int offset = 12; //Q subchannel begins after P, 12 bytes in
			SubcodeDeinterleaved[offset + 0] = sq.q_status;
			SubcodeDeinterleaved[offset + 1] = sq.q_tno;
			SubcodeDeinterleaved[offset + 2] = sq.q_index;
			SubcodeDeinterleaved[offset + 3] = sq.min.BCDValue;
			SubcodeDeinterleaved[offset + 4] = sq.sec.BCDValue;
			SubcodeDeinterleaved[offset + 5] = sq.frame.BCDValue;
			SubcodeDeinterleaved[offset + 6] = sq.zero;
			SubcodeDeinterleaved[offset + 7] = sq.ap_min.BCDValue;
			SubcodeDeinterleaved[offset + 8] = sq.ap_sec.BCDValue;
			SubcodeDeinterleaved[offset + 9] = sq.ap_frame.BCDValue;

			ushort crc16;
			if (calculateCRC)
				crc16 = CRC16_CCITT.Calculate(SubcodeDeinterleaved, offset, 10);
			else crc16 = sq.q_crc;

			//CRC is stored inverted and big endian
			SubcodeDeinterleaved[offset + 10] = (byte)(~(crc16 >> 8));
			SubcodeDeinterleaved[offset + 11] = (byte)(~(crc16));

			return crc16;
		}

		public void ReadSubcodeDeinterleaved(byte[] buffer, int offset)
		{
			Buffer.BlockCopy(SubcodeDeinterleaved, 0, buffer, offset, 96);
		}

		public void ReadSubcodeChannel(int number, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(SubcodeDeinterleaved, number * 12, buffer, offset, 12);
		}

		public byte[] SubcodeDeinterleaved = new byte[96];
	}

	public class ZeroSubcodeSector : ISubcodeSector
	{
		public void ReadSubcodeDeinterleaved(byte[] buffer, int offset)
		{
			for (int i = 0; i < 96; i++) buffer[i + offset] = 0;
		}

		public void ReadSubcodeChannel(int number, byte[] buffer, int offset)
		{
			for (int i = 0; i < 12; i++)
				buffer[i + offset] = 0;
		}
	}

	/// <summary>
	/// Reads subcode data from a blob, assuming it was already stored in deinterleaved format
	/// </summary>
	public class BlobSubcodeSectorPreDeinterleaved : ISubcodeSector
	{
		public void ReadSubcodeDeinterleaved(byte[] buffer, int offset)
		{
			Blob.Read(Offset, buffer, offset, 96);
		}

		public void ReadSubcodeChannel(int number, byte[] buffer, int offset)
		{
			Blob.Read(Offset + number * 12, buffer, offset, 12);
		}

		public IBlob Blob;
		public long Offset;
	}

	/// <summary>
	/// Control bit flags for the Q Subchannel
	/// </summary>
	[Flags]
	public enum EControlQ
	{
		None = 0,

		StereoNoPreEmph = 0,
		StereoPreEmph = 1,
		MonoNoPreemph = 8,
		MonoPreEmph = 9,
		DataUninterrupted = 4,
		DataIncremental = 5,

		CopyProhibitedMask = 0,
		CopyPermittedMask = 2,
	}

	/// <summary>
	/// Why did I make this a struct? I thought there might be a shitton of these and I was trying to cut down on object creation churn during disc-loading.
	/// But I ended up mostly just having a shitton of byte[] for each buffer (I could improve that later to possibly reference a blob on top of a memorystream)
	/// So, I should probably change that.
	/// </summary>
	public struct SubchannelQ
	{
		/// <summary>
		/// ADR and CONTROL
		/// </summary>
		public byte q_status;

		/// <summary>
		/// normal track: BCD indications of the current track number
		/// leadin track: should be 0 
		/// </summary>
		public byte q_tno;

		/// <summary>
		/// normal track: BCD indications of the current index
		/// leadin track: 'POINT' field used to ID the TOC entry #
		/// </summary>				
		public byte q_index;

		/// <summary>
		/// These are the initial set of timestamps. Meaning varies:
		/// check yellowbook 22.3.3 and 22.3.4
		/// normal track: relative timestamp
		/// leadin track: unknown
		/// </summary>
		public BCD2 min, sec, frame;

		/// <summary>
		/// This is supposed to be zero.. but CCD format stores it, so maybe it's useful for copy protection or something
		/// </summary>
		public byte zero;

		/// <summary>
		/// These are the second set of timestamps.  Meaning varies:
		/// check yellowbook 22.3.3 and 22.3.4
		/// normal track: absolute timestamp
		/// leadin track: timestamp of toc entry
		/// </summary>
		public BCD2 ap_min, ap_sec, ap_frame;

		/// <summary>
		/// The CRC. This is the actual CRC value as would be calculated from our library (it is inverted and written big endian to the disc)
		/// Don't assume this CRC is correct-- If this SubchannelQ was read from a dumped disc, the CRC might be wrong.
		/// CCD doesnt specify this for TOC entries, so it will be wrong. It may or may not be right for data track sectors from a CCD file.
		/// Or we may have computed this SubchannelQ data and generated the correct CRC at that time.
		/// </summary>
		public ushort q_crc;


		/// <summary>
		/// Retrieves the initial set of timestamps (min,sec,frac) as a convenient Timestamp
		/// </summary>
		public Timestamp Timestamp { get { return new Timestamp(min.DecimalValue, sec.DecimalValue, frame.DecimalValue); } }

		/// <summary>
		/// Retrieves the second set of timestamps (ap_min, ap_sec, ap_frac) as a convenient Timestamp
		/// </summary>
		public Timestamp AP_Timestamp { get { return new Timestamp(ap_min.DecimalValue, ap_sec.DecimalValue, ap_frame.DecimalValue); } }

		/// <summary>
		/// sets the status byte from the provided adr and control values
		/// </summary>
		public void SetStatus(byte adr, EControlQ control)
		{
			q_status = ComputeStatus(adr, control);
		}

		/// <summary>
		/// computes a status byte from the provided adr and control values
		/// </summary>
		public static byte ComputeStatus(int adr, EControlQ control)
		{
			return (byte)(adr | (((int)control) << 4));
		}

		/// <summary>
		/// Retrives the ADR field of the q_status member (low 4 bits)
		/// </summary>
		public int ADR { get { return q_status & 0xF; } }

		/// <summary>
		/// Retrieves the CONTROL field of the q_status member (high 4 bits)
		/// </summary>
		public EControlQ CONTROL { get { return (EControlQ)((q_status >> 4) & 0xF); } }
	}


	//this has been checked against mednafen's and seems to match
	//there are a few dozen different ways to do CRC16-CCITT
	//this table is backwards or something. at any rate its tailored to the needs of the Q subchannel
	internal static class CRC16_CCITT
	{
		private static readonly ushort[] table = new ushort[256];

		static CRC16_CCITT()
		{
			for (ushort i = 0; i < 256; ++i)
			{
				ushort value = 0;
				ushort temp = (ushort)(i << 8);
				for (byte j = 0; j < 8; ++j)
				{
					if (((value ^ temp) & 0x8000) != 0)
						value = (ushort)((value << 1) ^ 0x1021);
					else
						value <<= 1;
					temp <<= 1;
				}
				table[i] = value;
			}
		}

		public static ushort Calculate(byte[] data, int offset, int length)
		{
			ushort Result = 0;
			for(int i=0;i<length;i++)
			{
				byte b = data[offset + i];
				int index = (b ^ ((Result >> 8) & 0xFF));
				Result = (ushort)((Result << 8) ^ table[index]);
			}
			return Result;
		}
	}

	public class SubcodeDataDecoder
	{
		/// <summary>
		/// This seems to deinterleave Q from a subcode buffer? Not sure.. it isn't getting used anywhere right now, as you can see.
		/// </summary>
		public static void Unpack_Q(byte[] output, int out_ofs, byte[] input, int in_ofs)
		{
			for (int i = 0; i < 12; i++)
				output[out_ofs + i] = 0;
			for (int i = 0; i < 96; i++)
			{
				int bytenum = i >> 3;
				int bitnum = i & 7;
				bitnum = 7 - bitnum;
				int bitval = (byte)((input[in_ofs + i] >> 6) & 1);
				bitval <<= bitnum;
				output[out_ofs + bytenum] |= (byte)bitval;
			}
		}
	}
}
