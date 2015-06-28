using System;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	partial class CUE_Format2
	{
		abstract class SS_Base : ISectorSynthJob2448
		{
			public SubchannelQ sq;

			public abstract void Synth(SectorSynthJob job);
		}

		class SS_Mode1_2048 : SS_Base
		{
			public override void Synth(SectorSynthJob job)
			{
			}
		}


		static class SubSynth
		{
			public static void P(byte[] buffer, int offset, bool pause)
			{
				byte val = (byte)(pause?0xFF:0x00);
				for (int i = 0; i < 12; i++)
					buffer[offset + i] = val;
			}
		}

		/// <summary>
		/// Represents a pregap sector
		/// </summary>
		class SS_Pregap : SS_Base
		{
			public override void Synth(SectorSynthJob job)
			{
				if ((job.Parts & ESectorSynthPart.SubchannelP) != 0)
				{
					SubSynth.P(job.DestBuffer2448, job.DestOffset + 2352, false); //for now....
				}

				if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0)
				{
					var subcode = new BufferedSubcodeSector();
					subcode.Synthesize_SubchannelQ(ref sq, true);
					Buffer.BlockCopy(subcode.SubcodeDeinterleaved, 12, job.DestBuffer2448, job.DestOffset + 2352 + 12, 12);
				}
			}
		}

		/// <summary>
		/// Represents a Mode1 or Mode2 2352-byte sector
		/// </summary>
		class SS_2352 : SS_Base
		{
			public IBlob Blob;
			public long BlobOffset;
			public override void Synth(SectorSynthJob job)
			{
				//read the sector user data
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);

				//if subcode is needed, synthesize it

				if ((job.Parts & ESectorSynthPart.SubchannelP) != 0)
				{
					SubSynth.P(job.DestBuffer2448, job.DestOffset + 2352, false); //for now....
				}

				if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0)
				{
					var subcode = new BufferedSubcodeSector();
					subcode.Synthesize_SubchannelQ(ref sq, true);
					Buffer.BlockCopy(subcode.SubcodeDeinterleaved, 12, job.DestBuffer2448, job.DestOffset + 2352 + 12, 12);
				}
			}
		}

	}
}