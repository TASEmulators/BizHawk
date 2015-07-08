using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//ARCHITECTURE NOTE:
//No provisions are made for caching synthesized data for later accelerated use.
//This is because, in the worst case that might result in synthesizing an entire disc in memory.
//Instead, users should be advised to `hawk` the disc first for most rapid access so that synthesis won't be necessary and speed will be maximized.
//This will result in a completely flattened CCD where everything comes right off the hard drive
//Our choice here might be an unwise decision for disc ID and miscellaneous purposes but it's best for gaming and stream-converting (hawking and hashing)

//TODO: in principle, we could mount audio to decode only on an as-needed basis
//this might result in hiccups during emulation, though, so it should be an option.
//This would imply either decode-length processing (scan file without decoding) or decoding and discarding the data.
//We should probably have some richer policy specifications for this kind of thing, but it's not a high priority. Main workflow is still discohawking.
//Alternate policies would probably be associated with copious warnings (examples: ? ? ?)

namespace BizHawk.Emulation.DiscSystem
{
	public partial class Disc : IDisposable
	{
		/// <summary>
		/// The DiscStructure corresponding to the TOCRaw
		/// </summary>
		public DiscStructure Structure;

		/// <summary>
		/// DiscStructure.Session 1 of the disc, since that's all thats needed most of the time.
		/// </summary>
		public DiscStructure.Session Session1 { get { return Structure.Sessions[1]; } }

		/// <summary>
		/// The DiscTOCRaw corresponding to the RawTOCEntries.
		/// TODO - rename to TOC
		/// TODO - there's one of these for every session, so... having one here doesnt make sense
		/// </summary>
		public DiscTOC TOC;

		/// <summary>
		/// The raw TOC entries found in the lead-in track.
		/// These aren't very useful, but theyre one of the most lowest-level data structures from which other TOC-related stuff is derived
		/// </summary>
		public List<RawTOCEntry> RawTOCEntries = new List<RawTOCEntry>();

		/// <summary>
		/// Free-form optional memos about the disc
		/// </summary>
		public Dictionary<string, object> Memos = new Dictionary<string, object>();

		public void Dispose()
		{
			foreach (var res in DisposableResources)
			{
				res.Dispose();
			}
		}

		/// <summary>
		/// The DiscMountPolicy used to mount the disc. Consider this read-only.
		/// NOT SURE WE NEED THIS
		/// </summary>
		//public DiscMountPolicy DiscMountPolicy;


		//----------------------------------------------------------------------------

		/// <summary>
		/// Disposable resources (blobs, mostly) referenced by this disc
		/// </summary>
		internal List<IDisposable> DisposableResources = new List<IDisposable>();

		/// <summary>
		/// The sectors on the disc
		/// </summary>
		internal List<ISectorSynthJob2448> Sectors = new List<ISectorSynthJob2448>();

		/// <summary>
		/// Parameters set during disc loading which can be referenced by the sector synthesizers
		/// </summary>
		internal SectorSynthParams SynthParams = new SectorSynthParams();

		internal Disc()
		{
		}

		

		/// <summary>
		/// Automagically loads a disc, without any fine-tuned control at all
		/// </summary>
		public static Disc LoadAutomagic(string path)
		{
			var job = new DiscMountJob { IN_FromPath = path };
			//job.IN_DiscInterface = DiscInterface.MednaDisc; //TEST
			job.Run();
			return job.OUT_Disc;
		}

		class SS_PatchQ : ISectorSynthJob2448
		{
			public ISectorSynthJob2448 Original;
			public byte[] Buffer_SubQ = new byte[12];
			public void Synth(SectorSynthJob job)
			{
				Original.Synth(job);

				if ((job.Parts & ESectorSynthPart.SubchannelQ) == 0)
					return;

				//apply patched subQ
				for (int i = 0; i < 12; i++)
					job.DestBuffer2448[2352 + 12 + i] = Buffer_SubQ[i];
			}
		}

		/// <summary>
		/// applies an SBI file to the disc
		/// </summary>
		public void ApplySBI(SBI.SubQPatchData sbi, bool asMednafen)
		{
			//TODO - could implement as a blob, to avoid allocating so many byte buffers

			//save this, it's small, and we'll want it for disc processing a/b checks
			Memos["sbi"] = sbi;

			DiscSectorReader dsr = new DiscSectorReader(this);

			int n = sbi.ABAs.Count;
			int b=0;
			for (int i = 0; i < n; i++)
			{
				int lba = sbi.ABAs[i] - 150;

				//create a synthesizer which can return the patched data
				var ss_patchq = new SS_PatchQ() { Original = this.Sectors[lba+150] };
				byte[] subQbuf = ss_patchq.Buffer_SubQ;

				//read the old subcode
				dsr.ReadLBA_SubQ(lba, subQbuf, 0);

				//insert patch
				Sectors[lba + 150] = ss_patchq;

				//apply SBI patch
				for (int j = 0; j < 12; j++)
				{
					short patch = sbi.subq[b++];
					if (patch == -1) continue;
					else subQbuf[j] = (byte)patch;
				}

				//Apply mednafen hacks
				//The reasoning here is that we know we expect these sectors to have a wrong checksum. therefore, generate a checksum, and make it wrong
				//However, this seems senseless to me. The whole point of the SBI data is that it stores the patches needed to generate an acceptable subQ, right?
				if (asMednafen)
				{
					SynthUtils.SubQ_SynthChecksum(subQbuf, 0);
					subQbuf[10] ^= 0xFF;
					subQbuf[11] ^= 0xFF;
				}
			}
		}

		static byte IntToBCD(int n)
		{
			int ones;
			int tens = Math.DivRem(n,10,out ones);
			return (byte)((tens<<4)|ones);
		}
	}

	/// <summary>
	/// encapsulates a 2 digit BCD number as used various places in the CD specs
	/// </summary>
	public struct BCD2
	{
		/// <summary>
		/// The raw BCD value. you can't do math on this number! but you may be asked to supply it to a game program.
		/// The largest number it can logically contain is 99
		/// </summary>
		public byte BCDValue;

		/// <summary>
		/// The derived decimal value. you can do math on this! the largest number it can logically contain is 99.
		/// </summary>
		public int DecimalValue
		{
			get { return (BCDValue & 0xF) + ((BCDValue >> 4) & 0xF) * 10; }
			set { BCDValue = IntToBCD(value); }
		}

		/// <summary>
		/// makes a BCD2 from a decimal number. don't supply a number > 99 or you might not like the results
		/// </summary>
		public static BCD2 FromDecimal(int d)
		{
			return new BCD2 {DecimalValue = d};
		}

		public static BCD2 FromBCD(byte b)
		{
			return new BCD2 { BCDValue = b };
		}

		public static int BCDToInt(byte n)
		{
			var bcd = new BCD2();
			bcd.BCDValue = n;
			return bcd.DecimalValue;
		}

		public static byte IntToBCD(int n)
		{
			int ones;
			int tens = Math.DivRem(n, 10, out ones);
			return (byte)((tens << 4) | ones);
		}

		public override string ToString()
		{
			return BCDValue.ToString("X2");
		}
	}

	/// <summary>
	/// todo - rename to MSF? It can specify durations, so maybe it should be not suggestive of timestamp
	/// TODO - can we maybe use BCD2 in here
	/// </summary>
	public struct Timestamp
	{
		/// <summary>
		/// Checks if the string is a legit MSF. It's strict.
		/// </summary>
		public static bool IsMatch(string str)
		{
			return new Timestamp(str).Valid;
		}

		/// <summary>
		/// creates a timestamp from a string in the form mm:ss:ff
		/// </summary>
		public Timestamp(string str)
		{
			if (str.Length != 8) goto BOGUS;
			if (str[0] < '0' || str[0] > '9') goto BOGUS;
			if (str[1] < '0' || str[1] > '9') goto BOGUS;
			if (str[2] != ':') goto BOGUS;
			if (str[3] < '0' || str[3] > '9') goto BOGUS;
			if (str[4] < '0' || str[4] > '9') goto BOGUS;
			if (str[5] != ':') goto BOGUS;
			if (str[6] < '0' || str[6] > '9') goto BOGUS;
			if (str[7] < '0' || str[7] > '9') goto BOGUS;
			MIN = (byte)((str[0] - '0') * 10 + (str[1] - '0'));
			SEC = (byte)((str[3] - '0') * 10 + (str[4] - '0'));
			FRAC = (byte)((str[6] - '0') * 10 + (str[7] - '0'));
			Valid = true;
			Negative = false;
			return;
		BOGUS:
			MIN = SEC = FRAC = 0;
			Valid = false;
			Negative = false;
			return;
		}

		/// <summary>
		/// The string representation of the MSF
		/// </summary>
		public string Value
		{
			get
			{
				if (!Valid) return "--:--:--";
				return string.Format("{0}{1:D2}:{2:D2}:{3:D2}", Negative?'-':'+',MIN, SEC, FRAC);
			}
		}

		public readonly byte MIN, SEC, FRAC;
		public readonly bool Valid, Negative;

		/// <summary>
		/// The fully multiplied out flat-address Sector number
		/// </summary>
		public int Sector { get { return MIN * 60 * 75 + SEC * 75 + FRAC; } }

		/// <summary>
		/// creates timestamp from the supplied MSF
		/// </summary>
		public Timestamp(int m, int s, int f)
		{
			MIN = (byte)m;
			SEC = (byte)s;
			FRAC = (byte)f;
			Valid = true;
			Negative = false;
		}

		/// <summary>
		/// creates timestamp from supplied SectorNumber
		/// </summary>
		public Timestamp(int SectorNumber)
		{
			if (SectorNumber < 0)
			{
				SectorNumber = -SectorNumber;
				Negative = true;
			}
			else Negative = false;
			MIN = (byte)(SectorNumber / (60 * 75));
			SEC = (byte)((SectorNumber / 75) % 60);
			FRAC = (byte)(SectorNumber % 75);
			Valid = true;
		}

		public override string ToString()
		{
			return Value;
		}
	}


	static class SynthUtils
	{
		/// <summary>
		/// Calculates the checksum of the provided Q subchannel buffer and emplaces it
		/// </summary>
		/// <param name="buffer">12 byte Q subchannel buffer: input and output buffer for operation</param>
		/// <param name="offset">location within buffer of Q subchannel</param>
		public static ushort SubQ_SynthChecksum(byte[] buf12, int offset)
		{
			ushort crc16 = CRC16_CCITT.Calculate(buf12, offset, 10);

			//CRC is stored inverted and big endian
			buf12[offset + 10] = (byte)(~(crc16 >> 8));
			buf12[offset + 11] = (byte)(~(crc16));

			return crc16;
		}

		/// <summary>
		/// Caclulates the checksum of the provided Q subchannel buffer
		/// </summary>
		public static ushort SubQ_CalcChecksum(byte[] buf12, int offset)
		{
			return CRC16_CCITT.Calculate(buf12, offset, 10);
		}

		/// <summary>
		/// Serializes the provided SubchannelQ structure into a buffer
		/// Returns the crc, calculated or otherwise.
		/// </summary>
		public static ushort SubQ_Serialize(byte[] buf12, int offset, ref SubchannelQ sq)
		{
			buf12[offset + 0] = sq.q_status;
			buf12[offset + 1] = sq.q_tno.BCDValue;
			buf12[offset + 2] = sq.q_index.BCDValue;
			buf12[offset + 3] = sq.min.BCDValue;
			buf12[offset + 4] = sq.sec.BCDValue;
			buf12[offset + 5] = sq.frame.BCDValue;
			buf12[offset + 6] = sq.zero;
			buf12[offset + 7] = sq.ap_min.BCDValue;
			buf12[offset + 8] = sq.ap_sec.BCDValue;
			buf12[offset + 9] = sq.ap_frame.BCDValue;

			return SubQ_SynthChecksum(buf12, offset);
		}

		/// <summary>
		/// Synthesizes the typical subP data into the provided buffer depending on the indicated pause flag
		/// </summary>
		public static void SubP(byte[] buffer12, int offset, bool pause)
		{
			byte val = (byte)(pause ? 0xFF : 0x00);
			for (int i = 0; i < 12; i++)
				buffer12[offset + i] = val;
		}

		/// <summary>
		/// Synthesizes a data sector header
		/// </summary>
		public static void SectorHeader(byte[] buffer16, int offset, int LBA, byte mode)
		{
			buffer16[offset + 0] = 0x00;
			for (int i = 1; i < 11; i++) buffer16[offset + i] = 0xFF;
			buffer16[offset + 11] = 0x00;
			Timestamp ts = new Timestamp(LBA + 150);
			buffer16[offset + 12] = BCD2.IntToBCD(ts.MIN);
			buffer16[offset + 13] = BCD2.IntToBCD(ts.SEC);
			buffer16[offset + 14] = BCD2.IntToBCD(ts.FRAC);
			buffer16[offset + 15] = mode;
		}

		/// <summary>
		/// Synthesizes the EDC checksum for a Mode 2 Form 1 data sector (and puts it in place)
		/// </summary>
		public static void EDC_Mode2_Form1(byte[] buf2352, int offset)
		{
			uint edc = ECM.EDC_Calc(buf2352, offset + 16, 2048 + 8);
			ECM.PokeUint(buf2352, offset + 2072, edc);
		}

		/// <summary>
		/// Synthesizes the EDC checksum for a Mode 2 Form 2 data sector (and puts it in place)
		/// </summary>
		public static void EDC_Mode2_Form2(byte[] buf2352, int offset)
		{
			uint edc = ECM.EDC_Calc(buf2352, offset + 16, 2324 + 8);
			ECM.PokeUint(buf2352, offset + 2348, edc);
		}


		/// <summary>
		/// Synthesizes the complete ECM data (EDC + ECC) for a Mode 1 data sector (and puts it in place)
		/// Make sure everything else in the sector userdata is done before calling this
		/// </summary>
		public static void ECM_Mode1(byte[] buf2352, int offset, int LBA)
		{
			//EDC
			uint edc = ECM.EDC_Calc(buf2352, offset, 2064);
			ECM.PokeUint(buf2352, offset + 2064, edc);

			//reserved, zero
			for (int i = 0; i < 8; i++) buf2352[offset + 2068 + i] = 0;

			//ECC
			ECM.ECC_Populate(buf2352, offset, buf2352, offset, false);
		}

		/// <summary>
		/// Converts the useful (but unrealistic) deinterleaved subchannel data into the useless (but realistic) interleaved format.
		/// in_buf and out_buf should not overlap
		/// </summary>
		public static void InterleaveSubcode(byte[] in_buf, int in_buf_index, byte[] out_buf, int out_buf_index)
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
		public static void DeinterleaveSubcode(byte[] in_buf, int in_buf_index, byte[] out_buf, int out_buf_index)
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
		public unsafe static void InterleaveSubcodeInplace(byte[] buf, int buf_index)
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
		public unsafe static void DeinterleaveSubcodeInplace(byte[] buf, int buf_index)
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

}