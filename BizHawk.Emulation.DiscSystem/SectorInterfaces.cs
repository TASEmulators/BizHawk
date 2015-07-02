using System;
using System.Collections.Generic;
using System.IO;

//TODO - most of these sector interfaces are really only useful for CUEs, I guess. most other disc formats arent nearly as lame.. I think

namespace BizHawk.Emulation.DiscSystem
{
	public interface ISector
	{
		/// <summary>
		/// reads the entire sector, raw
		/// </summary>
		int Read_2352(byte[] buffer, int offset);

		/// <summary>
		/// reads 2048 bytes of userdata.. precisely what this means isnt always 100% certain (for instance mode2 form 0 has 2336 bytes of userdata instead of 2048)..
		/// ..but its certain enough for this to be useful
		/// THIS IS SO ANNOYING!!!! UGH!!!!!!!!!
		/// </summary>
		int Read_2048(byte[] buffer, int offset);
	}

	/// <summary>
	/// Indicates which part of a sector are needing to be synthesized.
	/// Sector synthesis may create too much data, but this is a hint as to what's needed
	/// TODO - add a flag indicating whether clearing has happened
	/// TODO - add output to the job indicating whether interleaving has happened. let the sector reader be responsible
	/// </summary>
	[Flags] enum ESectorSynthPart
	{
		/// <summary>
		/// The header is required
		/// </summary>
		Header16 = 1,
		
		/// <summary>
		/// The main 2048 user data bytes are required
		/// </summary>
		User2048 = 2,

		/// <summary>
		/// The 276 bytes of error correction are required
		/// </summary>
		ECC276 = 4,

		/// <summary>
		/// The 12 bytes preceding the ECC section are required (usually EDC and zero but also userdata sometimes)
		/// </summary>
		EDC12 = 8,

		/// <summary>
		/// The entire possible 276+12=288 bytes of ECM data is required (ECC276|EDC12)
		/// </summary>
		ECM288Complete = (ECC276 | EDC12),

		/// <summary>
		/// An alias for ECM288Complete
		/// </summary>
		ECMAny = ECM288Complete,

		/// <summary>
		/// A mode2 userdata section is required: the main 2048 user bytes AND the ECC and EDC areas
		/// </summary>
		User2336 = (User2048 | ECM288Complete),

		/// <summary>
		/// The complete sector userdata (2352 bytes) is required
		/// </summary>
		UserComplete = 15,

		/// <summary>
		/// An alias for UserComplete
		/// </summary>
		UserAny = UserComplete,

		/// <summary>
		/// An alias for UserComplete
		/// </summary>
		User2352 = UserComplete,

		/// <summary>
		/// SubP is required
		/// </summary>
		SubchannelP = 16,

		/// <summary>
		/// SubQ is required
		/// </summary>
		SubchannelQ = 32,

		/// <summary>
		/// Subchannels R-W (all except for P and Q)
		/// </summary>
		Subchannel_RSTUVW = (64|128|256|512|1024|2048),

		/// <summary>
		/// Complete subcode is required
		/// </summary>
		SubcodeComplete = (SubchannelP | SubchannelQ | Subchannel_RSTUVW),

		/// <summary>
		/// Any of the subcode might be required (just another way of writing SubcodeComplete)
		/// </summary>
		SubcodeAny = SubcodeComplete,

		/// <summary>
		/// The subcode should be deinterleaved
		/// </summary>
		SubcodeDeinterleave = 4096,

		/// <summary>
		/// The 100% complete sector is required including 2352 bytes of userdata and 96 bytes of subcode
		/// </summary>
		Complete2448 = SubcodeComplete | User2352,
	}

	interface ISectorSynthJob2448
	{
		void Synth(SectorSynthJob job);
	}

	/// <summary>
	/// Not a proper job? maybe with additional flags, it could be
	/// </summary>
	class SectorSynthJob
	{
		public int LBA;
		public ESectorSynthPart Parts;
		public byte[] DestBuffer2448;
		public int DestOffset;
		public SectorSynthParams Params;
		public Disc Disc;
	}

	/// <summary>
	/// Generic parameters for sector synthesis.
	/// To cut down on resource utilization, these can be stored in a disc and are tightly coupled to
	/// the SectorSynths that have been setup for it
	/// </summary>
	struct SectorSynthParams
	{
		public long[] BlobOffsets;
		public MednaDisc MednaDisc;
	}

	class SS_Multi : ISectorSynthJob2448
	{
		public List<ISectorSynthJob2448> Agenda = new List<ISectorSynthJob2448>();

		public void Synth(SectorSynthJob job)
		{
			foreach (var a in Agenda)
			{
				a.Synth(job);
			}
		}
	}



	/// <summary>
	/// this ISector is dumb and only knows how to drag chunks off a source blob
	/// TODO - garbage, delete me
	/// </summary>
	public class Sector_RawBlob : ISector
	{
		public IBlob Blob;
		public long Offset;
		public int Read_2352(byte[] buffer, int offset)
		{
			return Blob.Read(Offset, buffer, offset, 2352);
		}
		public int Read_2048(byte[] buffer, int offset)
		{
			//this depends on CD-XA mode and such. so we need to read the mode bytes
			//HEY!!!!!! SHOULD THIS BE DONE BASED ON THE CLAIMED TRACK TYPE, OR ON WHATS IN THE SECTOR?
			//this is kind of a function of the CD reader.. it's not clear how this function should work.
			//YIKES!!!!!!!!!!!!!!
			//well, we need to scrutinize it for CCD files anyway, so...
			//this sucks.

			//read mode byte, use that to determine what kind of sector this is
			Blob.Read(Offset + 15, buffer, 0, 1); 
			byte mode = buffer[0];
			if(mode == 1)
				return Blob.Read(Offset + 16, buffer, offset, 2048);
			else
				return Blob.Read(Offset + 24, buffer, offset, 2048); //PSX assumptions about CD-XA.. BAD BAD BAD
		}
	}

	/// <summary>
	/// this ISector always returns zeroes
	/// (not even SYNC stuff is set.... pretty bogus and useless, this)
	/// </summary>
	class Sector_Zero : ISector
	{
		public int Read_2352(byte[] buffer, int offset)
		{
			Array.Clear(buffer, 0, 2352);
			return 2352;
		}
		public int Read_2048(byte[] buffer, int offset)
		{
			Array.Clear(buffer, 0, 2048);
			return 2048;
		}
	}

	abstract class Sector_Mode1_or_Mode2_2352 : ISector
	{
		public ISector BaseSector;
		public abstract int Read_2352(byte[] buffer, int offset);
		public abstract int Read_2048(byte[] buffer, int offset);
	}

	/// <summary>
	/// This ISector is a raw MODE1 sector
	/// </summary>
	class Sector_Mode1_2352 : Sector_Mode1_or_Mode2_2352
	{
		public override int Read_2352(byte[] buffer, int offset)
		{
			return BaseSector.Read_2352(buffer, offset);
		}
		public override int Read_2048(byte[] buffer, int offset)
		{
			//to get 2048 bytes out of this sector type, start 16 bytes in
			int ret = BaseSector.Read_2352(TempSector, 0);
			Buffer.BlockCopy(TempSector, 16, buffer, offset, 2048);
			System.Diagnostics.Debug.Assert(buffer != TempSector);
			return 2048;
		}

		[ThreadStatic]
		static byte[] TempSector = new byte[2352];
	}

	/// <summary>
	/// this ISector is a raw MODE2 sector. could be form 0,1,2... who can say? supposedly:
	/// To tell the different Mode 2s apart you have to examine bytes 16-23 of the sector (the first 8 bytes of Mode Data). 
	/// If bytes 16-19 are not the same as 20-23, then it is Mode 2. If they are equal and bit 5 is on (0x20), then it is Mode 2 Form 2. Otherwise it is Mode 2 Form 1.
	/// ...but we're not using this information in any way
	/// </summary>
	class Sector_Mode2_2352 : Sector_Mode1_or_Mode2_2352
	{
		public override int Read_2352(byte[] buffer, int offset)
		{
			return BaseSector.Read_2352(buffer, offset);
		}

		public override int Read_2048(byte[] buffer, int offset)
		{
			//to get 2048 bytes out of this sector type, start 24 bytes in
			int ret = BaseSector.Read_2352(TempSector, 0);
			Buffer.BlockCopy(TempSector, 24, buffer, offset, 2048);
			System.Diagnostics.Debug.Assert(buffer != TempSector);
			return 2048;
		}

		[ThreadStatic]
		static byte[] TempSector = new byte[2352];
	}

	//a blob that also has an ECM cache associated with it. maybe one day.
	//UHHH this is kind of redundant right now... see how Sector_Mode1_2048 manages its own cache
	class ECMCacheBlob
	{
		public ECMCacheBlob(IBlob blob)
		{
			BaseBlob = blob;
		}
		public IBlob BaseBlob;
	}

	/// <summary>
	/// transforms Mode1/2048 -> Mode1/2352
	/// </summary>
	class Sector_Mode1_2048 : ISector
	{
		public Sector_Mode1_2048(int ABA)
		{
			byte aba_min = (byte)(ABA / 60 / 75);
			byte aba_sec = (byte)((ABA / 75) % 60);
			byte aba_frac = (byte)(ABA % 75);
			bcd_aba_min = aba_min.BCD_Byte();
			bcd_aba_sec = aba_sec.BCD_Byte();
			bcd_aba_frac = aba_frac.BCD_Byte();
		}
		byte bcd_aba_min, bcd_aba_sec, bcd_aba_frac;

		public ECMCacheBlob Blob;
		public long Offset;
		byte[] extra_data;
		bool has_extra_data;

		public int Read_2048(byte[] buffer, int offset)
		{
			//this is easy. we only have 2048 bytes, and 2048 bytes were requested
			return Blob.BaseBlob.Read(Offset, buffer, offset, 2048);
		}

		public int Read_2352(byte[] buffer, int offset)
		{
			//user data
			int read = Blob.BaseBlob.Read(Offset, buffer, offset + 16, 2048);

			//if we read the 2048 physical bytes OK, then return the complete sector
			if (read == 2048 && has_extra_data)
			{
				Buffer.BlockCopy(extra_data, 0, buffer, offset, 16);
				Buffer.BlockCopy(extra_data, 16, buffer, offset + 2064, 4 + 8 + 172 + 104);
				return 2352;
			}

			//sync
			buffer[offset + 0] = 0x00; buffer[offset + 1] = 0xFF; buffer[offset + 2] = 0xFF; buffer[offset + 3] = 0xFF;
			buffer[offset + 4] = 0xFF; buffer[offset + 5] = 0xFF; buffer[offset + 6] = 0xFF; buffer[offset + 7] = 0xFF;
			buffer[offset + 8] = 0xFF; buffer[offset + 9] = 0xFF; buffer[offset + 10] = 0xFF; buffer[offset + 11] = 0x00;
			//sector address
			buffer[offset + 12] = bcd_aba_min;
			buffer[offset + 13] = bcd_aba_sec;
			buffer[offset + 14] = bcd_aba_frac;
			//mode 1
			buffer[offset + 15] = 1;

			//calculate EDC and poke into the sector
			uint edc = ECM.EDC_Calc(buffer, offset, 2064);
			ECM.PokeUint(buffer, 2064, edc);

			//intermediate
			for (int i = 0; i < 8; i++) buffer[offset + 2068 + i] = 0;
			//ECC
			ECM.ECC_Populate(buffer, offset, buffer, offset, false);

			//VALIDATION - check our homemade algorithms against code derived from ECM
			////EDC
			//GPL_ECM.edc_validateblock(buffer, 2064, buffer, offset + 2064);
			////ECC
			//GPL_ECM.ecc_validate(buffer, offset, false);

			//if we read the 2048 physical bytes OK, then return the complete sector
			if (read == 2048)
			{
				extra_data = new byte[16 + 4 + 8 + 172 + 104]; //aka 2048
				Buffer.BlockCopy(buffer, 0, extra_data, 0, 16);
				Buffer.BlockCopy(buffer, 2064, extra_data, 16, 4 + 8 + 172 + 104);
				has_extra_data = true;
				return 2352;
			}
			//otherwise, return a smaller value to indicate an error
			else return read;
		}
	}
}