using System;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	partial class CUE_Format2
	{
		abstract class SS_Base : ISectorSynthJob2448
		{
			public IBlob Blob;
			public long BlobOffset;
			public SubchannelQ sq;
			public bool Pause;

			public abstract void Synth(SectorSynthJob job);

			protected void SynthSubcode(SectorSynthJob job)
			{
				if ((job.Parts & ESectorSynthPart.SubchannelP) != 0)
				{
					SynthUtils.SubP(job.DestBuffer2448, job.DestOffset + 2352, Pause);
				}

				if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0)
				{
					var subcode = new BufferedSubcodeSector();
					subcode.Synthesize_SubchannelQ(ref sq, true);
					Buffer.BlockCopy(subcode.SubcodeDeinterleaved, 12, job.DestBuffer2448, job.DestOffset + 2352 + 12, 12);
				}
			}
		}

		static class SynthUtils
		{
			public static void SubP(byte[] buffer, int offset, bool pause)
			{
				byte val = (byte)(pause?0xFF:0x00);
				for (int i = 0; i < 12; i++)
					buffer[offset + i] = val;
			}

			public static void SectorHeader(byte[] buffer, int offset, int LBA, byte mode)
			{
				buffer[offset + 0] = 0x00;
				for (int i = 1; i < 11; i++) buffer[offset + i] = 0xFF;
				buffer[offset + 11] = 0x00;
				Timestamp ts = new Timestamp(LBA + 150);
				buffer[offset + 12] = ts.MIN;
				buffer[offset + 13] = ts.SEC;
				buffer[offset + 14] = ts.FRAC;
				buffer[offset + 15] = mode;
			}

			/// <summary>
			/// Make sure everything else in the sector userdata is done before calling this
			/// </summary>
			public static void ECM_Mode1(byte[] buffer, int offset, int LBA)
			{
				//EDC
				uint edc = ECM.EDC_Calc(buffer, offset, 2064);
				ECM.PokeUint(buffer, 2064, edc);

				//reserved, zero
				for (int i = 0; i < 8; i++) buffer[offset + 2068 + i] = 0;
				
				//ECC
				ECM.ECC_Populate(buffer, offset, buffer, offset, false);
			}
		}

		/// <summary>
		/// Represents a Mode1 2048-byte sector
		/// </summary>
		class SS_Mode1_2048 : SS_Base
		{
			public override void Synth(SectorSynthJob job)
			{
				//read the sector user data
				if((job.Parts & ESectorSynthPart.User2048) != 0)
					Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 16, 2048);

				if ((job.Parts & ESectorSynthPart.Header16) != 0)
					SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 1);

				if ((job.Parts & ESectorSynthPart.ECMAny) != 0)
					SynthUtils.ECM_Mode1(job.DestBuffer2448, job.DestOffset + 0, job.LBA);

				SynthSubcode(job);
			}
		}

		/// <summary>
		/// Represents a pregap or postgap sector.
		/// The Pause flag isn't set in here because it might need special logic varying between sectors
		/// Implemented as another sector type with a blob reading all zeros
		/// </summary>
		class SS_Gap : SS_Mode1_2048
		{
			public SS_Gap()
			{
				Blob = new Disc.Blob_Zeros();
			}
		}

		/// <summary>
		/// Represents a Mode1 or Mode2 2352-byte sector
		/// </summary>
		class SS_2352 : SS_Base
		{
			public override void Synth(SectorSynthJob job)
			{
				//read the sector user data
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);

				//if subcode is needed, synthesize it
				SynthSubcode(job);
			}
		}

	}
}