using System;

//TODO - call on unmanaged code in mednadisc if available to do deinterleaving faster. be sure to benchmark it though..

//a decent little subcode reference
//http://www.jbum.com/cdg_revealed.html

//NOTES: the 'subchannel Q' stuff here has a lot to do with the q-Mode 1. q-Mode 2 is different, 
//and q-Mode 1 technically is defined a little differently in the lead-in area, although the fields align so the datastructures can be reused

//Q subchannel basic structure: (quick ref: https://en.wikipedia.org/wiki/Compact_Disc_subcode)
//Byte 1: (aka `status`)
// q-Control: 4 bits (i.e. flags) 
// q-Mode: 4 bits (aka ADR; WHY is this called ADR?)
//q-Data: other stuff depending on q-Mode and type of track
//q-CRC: CRC of preceding

namespace BizHawk.Emulation.DiscSystem
{
	//YET ANOTHER BAD IDEA
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

	public static class SubcodeUtils
	{
		/// <summary>
		/// Converts the useful (but unrealistic) deinterleaved data into the useless (but realistic) interleaved subchannel format.
		/// in_buf and out_buf should not overlap
		/// </summary>
		public static void Interleave(byte[] in_buf, int in_buf_index, byte[] out_buf, int out_buf_index)
		{
			for (int d = 0; d < 12; d++)
			{
				for (int bitpoodle = 0; bitpoodle < 8; bitpoodle++)
				{
					int rawb = 0;

					for (int ch = 0; ch < 8; ch++)
					{
						rawb |= ((in_buf[ch * 12 + d + in_buf_index] >> (7 - bitpoodle)) & 1) << (7 - ch);
					}
					out_buf[(d << 3) + bitpoodle + out_buf_index] = (byte)rawb;
				}
			}
		}

		/// <summary>
		/// Converts the useless (but realistic) interleaved subchannel data into a useful (but unrealistic) deinterleaved format.
		/// in_buf and out_buf should not overlap
		/// </summary>
		public static void Deinterleave(byte[] in_buf, int in_buf_index, byte[] out_buf, int out_buf_index)
		{
			for (int i = 0; i < 96; i++)
				out_buf[i] = 0;

			for (int ch = 0; ch < 8; ch++)
			{
				for (int i = 0; i < 96; i++)
				{
					out_buf[(ch * 12) + (i >> 3) + out_buf_index] |= (byte)(((in_buf[i + in_buf_index] >> (7 - ch)) & 0x1) << (7 - (i & 0x7)));
				}
			}
		}

		/// <summary>
		/// Converts the useful (but unrealistic) deinterleaved data into the useless (but realistic) interleaved subchannel format.
		/// </summary>
		public unsafe static void InterleaveInplace(byte[] buf, int buf_index)
		{
			byte* out_buf = stackalloc byte[96];

			for (int i = 0; i < 96; i++)
				out_buf[i] = 0;

			for (int d = 0; d < 12; d++)
			{
				for (int bitpoodle = 0; bitpoodle < 8; bitpoodle++)
				{
					int rawb = 0;

					for (int ch = 0; ch < 8; ch++)
					{
						rawb |= ((buf[ch * 12 + d + buf_index] >> (7 - bitpoodle)) & 1) << (7 - ch);
					}
					out_buf[(d << 3) + bitpoodle] = (byte)rawb;
				}
			}

			for (int i = 0; i < 96; i++)
				buf[i + buf_index] = out_buf[i];
		}

		/// <summary>
		/// Converts the useless (but realistic) interleaved subchannel data into a useful (but unrealistic) deinterleaved format.
		/// </summary>
		public unsafe static void DeinterleaveInplace(byte[] buf, int buf_index)
		{
			byte* out_buf = stackalloc byte[96];

			for (int i = 0; i < 96; i++)
				out_buf[i] = 0;

			for (int ch = 0; ch < 8; ch++)
			{
				for (int i = 0; i < 96; i++)
				{
					out_buf[(ch * 12) + (i >> 3)] |= (byte)(((buf[i + buf_index] >> (7 - ch)) & 0x1) << (7 - (i & 0x7)));
				}
			}

			for (int i = 0; i < 96; i++)
				buf[i + buf_index] = out_buf[i];
		}
	}


	/// <summary>
	/// Reads subcode from an internally-managed buffer
	/// </summary>
	class BufferedSubcodeSector : ISubcodeSector
	{
		public void Synthesize_SubchannelP(bool pause)
		{
			byte val = pause ? (byte)0xFF : (byte)0x00;
			for (int i = 0; i < 12; i++)
				SubcodeDeinterleaved[i] = val;
		}

		/// <summary>
		/// Fills this subcode buffer with subchannel Q data. calculates the required CRC, as well.
		/// Returns the crc, calculated or otherwise.
		/// </summary>
		public ushort Synthesize_SubchannelQ(ref SubchannelQ sq, bool calculateCRC)
		{
			int offset = 12; //Q subchannel begins after P, 12 bytes in
			SubcodeDeinterleaved[offset + 0] = sq.q_status;
			SubcodeDeinterleaved[offset + 1] = sq.q_tno.BCDValue;
			SubcodeDeinterleaved[offset + 2] = sq.q_index.BCDValue;
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

		public void Synthesize_SunchannelQ_Checksum()
		{
			int offset = 12; //Q subchannel begins after P, 12 bytes in

			ushort crc16 = CRC16_CCITT.Calculate(SubcodeDeinterleaved, offset, 10);

			//CRC is stored inverted and big endian
			SubcodeDeinterleaved[offset + 10] = (byte)(~(crc16 >> 8));
			SubcodeDeinterleaved[offset + 11] = (byte)(~(crc16));
		}

		public void ReadSubcodeDeinterleaved(byte[] buffer, int offset)
		{
			Buffer.BlockCopy(SubcodeDeinterleaved, 0, buffer, offset, 96);
		}

		public void ReadSubcodeChannel(int number, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(SubcodeDeinterleaved, number * 12, buffer, offset, 12);
		}

		public BufferedSubcodeSector()
		{
			SubcodeDeinterleaved = new byte[96];
		}

		public static BufferedSubcodeSector CloneFromBytesDeinterleaved(byte[] buffer)
		{
			var ret = new BufferedSubcodeSector();
			Buffer.BlockCopy(buffer, 0, ret.SubcodeDeinterleaved, 0, 96);
			return ret;
		}

		public byte[] SubcodeDeinterleaved;
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
	/// Control bit flags for the Q Subchannel.
	/// </summary>
	[Flags]
	public enum EControlQ
	{
		None = 0,

		PRE = 1, //Pre-emphasis enabled (audio tracks only)
		DCP = 2, //Digital copy permitted
		DATA = 4, //set for data tracks, clear for audio tracks
		_4CH = 8, //Four channel audio
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
		/// TODO - make BCD2? PROBABLY NOT. I DONT KNOW.
		/// </summary>
		public byte q_status;

		/// <summary>
		/// normal track: BCD indication of the current track number
		/// leadin track: should be 0 
		/// </summary>
		public BCD2 q_tno;

		/// <summary>
		/// normal track: BCD indication of the current index
		/// leadin track: 'POINT' field used to ID the TOC entry #
		/// </summary>				
		public BCD2 q_index;

		/// <summary>
		/// These are the initial set of timestamps. Meaning varies:
		/// check yellowbook 22.3.3 and 22.3.4
		/// normal track: relative timestamp
		/// leadin track: unknown
		/// leadout: relative timestamp
		/// TODO - why are these BCD2? having things in BCD2 is freaking annoying, I should only make them BCD2 when serializing into a subchannel Q buffer
		/// EDIT - elsewhere I rambled "why not BCD2?". geh. need to make a final organized approach
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
		/// leadin track q-mode 1: TOC entry, absolute MSF of track
		/// leadout: absolute timestamp
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
		public Timestamp Timestamp
		{
			get { return new Timestamp(min.DecimalValue, sec.DecimalValue, frame.DecimalValue); }
			set { min.DecimalValue = value.MIN; sec.DecimalValue = value.SEC; frame.DecimalValue = value.FRAC; }
		}

		/// <summary>
		/// Retrieves the second set of timestamps (ap_min, ap_sec, ap_frac) as a convenient Timestamp.
		/// </summary>
		public Timestamp AP_Timestamp { 
			get { return new Timestamp(ap_min.DecimalValue, ap_sec.DecimalValue, ap_frame.DecimalValue); }
			set { ap_min.DecimalValue = value.MIN; ap_sec.DecimalValue = value.SEC; ap_frame.DecimalValue = value.FRAC; }
		}

		/// <summary>
		/// sets the status byte from the provided adr/qmode and control values
		/// </summary>
		public void SetStatus(byte adr_or_qmode, EControlQ control)
		{
			q_status = ComputeStatus(adr_or_qmode, control);
		}

		/// <summary>
		/// computes a status byte from the provided adr/qmode and control values
		/// </summary>
		public static byte ComputeStatus(int adr_or_qmode, EControlQ control)
		{
			return (byte)(adr_or_qmode | (((int)control) << 4));
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
