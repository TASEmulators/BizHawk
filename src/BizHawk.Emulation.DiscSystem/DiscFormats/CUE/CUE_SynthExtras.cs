namespace BizHawk.Emulation.DiscSystem.CUE
{
	// extra synths using SS_Base, not used by CUEs but used for other formats

	/// <summary>
	/// Represents a Mode2 Form1 2048-byte sector
	/// Only used by NRG, MDS, and CHD
	/// </summary>
	internal class SS_Mode2_Form1_2048 : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			var ecm = (job.Parts & ESectorSynthPart.ECMAny) != 0;
			if (ecm)
			{
				// ecm needs these parts for synth
				job.Parts |= ESectorSynthPart.User2048;
				job.Parts |= ESectorSynthPart.Header16;
			}

			//read the sector user data
			if ((job.Parts & ESectorSynthPart.User2048) != 0)
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 24, 2048);

			if ((job.Parts & ESectorSynthPart.Header16) != 0)
				SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);

			if (ecm)
				SynthUtils.ECM_Mode2_Form1(job.DestBuffer2448, job.DestOffset);

			SynthSubchannelAsNeed(job);
		}
	}

	/// <summary>
	/// Represents a Mode2 Form1 2324-byte sector
	/// Only used by MDS and CHD
	/// </summary>
	internal class SS_Mode2_Form2_2324 : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			//read the sector userdata (note: ECC data is now userdata in this regard)
			if ((job.Parts & ESectorSynthPart.User2336) != 0)
			{
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 24, 2324);
				// only needs userdata for synth
				SynthUtils.ECM_Mode2_Form2(job.DestBuffer2448, job.DestOffset);
			}

			if ((job.Parts & ESectorSynthPart.Header16) != 0)
				SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);

			SynthSubchannelAsNeed(job);
		}
	}

	/// <summary>
	/// Represents a Mode2 Form1 2328-byte sector
	/// Only used by MDS
	/// </summary>
	internal class SS_Mode2_Form2_2328 : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			//read the sector userdata (note: ECC data is now userdata in this regard)
			if ((job.Parts & ESectorSynthPart.User2336) != 0)
			{
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 24, 2328);
				// only subheader needs to be synthed
				SynthUtils.SectorSubHeader(job.DestBuffer2448, job.DestOffset + 16, 2);
			}

			if ((job.Parts & ESectorSynthPart.Header16) != 0)
				SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);

			SynthSubchannelAsNeed(job);
		}
	}

	/// <summary>
	/// Represents a full 2448-byte sector with interleaved subcode
	/// Only used by MDS, NRG, and CDI
	/// </summary>
	internal class SS_2448_Interleaved : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			// all subcode is present and interleaved, just read it all
			Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2448);

			// deinterleave it if needed
			if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) != 0)
				SynthUtils.DeinterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
		}
	}

	/// <summary>
	/// Represents a 2364-byte (2352 + 12) sector with deinterleaved Q subcode
	/// Only used by CDI
	/// </summary>
	internal class SS_2364_DeinterleavedQ : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			if ((job.Parts & ESectorSynthPart.User2352) != 0)
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);

			if ((job.Parts & ESectorSynthPart.SubchannelP) != 0)
				SynthUtils.SubP(job.DestBuffer2448, job.DestOffset + 2352, Pause);

			// Q is present in the blob and non-interleaved
			if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0)
				Blob.Read(BlobOffset + 2352, job.DestBuffer2448, job.DestOffset + 2352 + 12, 12);

			// clear R-W if needed
			if ((job.Parts & ESectorSynthPart.Subchannel_RSTUVW) != 0)
				Array.Clear(job.DestBuffer2448, job.DestOffset + 2352 + 12 + 12, 12 * 6);

			// subcode has been generated deinterleaved; we may still need to interleave it
			if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0 && (job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0)
				SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
		}
	}
}