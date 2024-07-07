namespace BizHawk.Emulation.DiscSystem.CUE
{
	internal abstract class SS_Base : ISectorSynthJob2448
	{
		public IBlob Blob;
		public long BlobOffset;

		public DiscMountPolicy Policy;

		//subQ data
		public SubchannelQ sq;

		//subP data
		public bool Pause;

		public abstract void Synth(SectorSynthJob job);

		protected void SynthSubchannelAsNeed(SectorSynthJob job)
		{
			//synth P if needed
			if ((job.Parts & ESectorSynthPart.SubchannelP) != 0)
			{
				SynthUtils.SubP(job.DestBuffer2448, job.DestOffset + 2352, Pause);
			}

			//synth Q if needed
			//TODO - why not already have it serialized? Into a disc resource, even.
			if ((job.Parts & ESectorSynthPart.SubchannelQ) != 0)
			{
				SynthUtils.SubQ_Serialize(job.DestBuffer2448, job.DestOffset + 2352 + 12, ref sq);
			}

			//clear R-W if needed
			if ((job.Parts & ESectorSynthPart.Subchannel_RSTUVW) != 0)
			{
				Array.Clear(job.DestBuffer2448, job.DestOffset + 2352 + 12 + 12, (12 * 6));
			}

			//subcode has been generated deinterleaved; we may still need to interleave it
			if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0 && (job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0)
			{
				SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
			}
		}
	}

	/// <summary>
	/// Represents a Mode1 2048-byte sector
	/// </summary>
	internal class SS_Mode1_2048 : SS_Base
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
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 16, 2048);

			if ((job.Parts & ESectorSynthPart.Header16) != 0)
				SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 1);

			if (ecm)
				SynthUtils.ECM_Mode1(job.DestBuffer2448, job.DestOffset + 0);

			SynthSubchannelAsNeed(job);
		}
	}

	/// <summary>
	/// Represents a Mode2 2336-byte sector
	/// </summary>
	internal class SS_Mode2_2336 : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			//read the sector sector user data + ECM data
			if ((job.Parts & ESectorSynthPart.User2336) != 0)
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset + 16, 2336);

			if ((job.Parts & ESectorSynthPart.Header16) != 0)
				SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, 2);

			//if subcode is needed, synthesize it
			SynthSubchannelAsNeed(job);
		}
	}

	/// <summary>
	/// Represents a 2352-byte sector of any sort
	/// </summary>
	internal class SS_2352 : SS_Base
	{
		public override void Synth(SectorSynthJob job)
		{
			//read the sector user data
			if ((job.Parts & ESectorSynthPart.User2352) != 0)
				Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);

			//if subcode is needed, synthesize it
			SynthSubchannelAsNeed(job);
		}
	}

	/// <summary>
	/// Encodes a pre-gap sector
	/// </summary>
	internal class SS_Gap : SS_Base
	{
		public CueTrackType TrackType;

		public override void Synth(SectorSynthJob job)
		{
			//this isn't fully analyzed/optimized
			Array.Clear(job.DestBuffer2448, job.DestOffset, 2352);

			byte mode = 255;
			var form = -1;
			switch (TrackType)
			{
				case CueTrackType.Audio:
					mode = 0;
					break;

				case CueTrackType.CDI_2352:
				case CueTrackType.Mode1_2352:
					mode = 1;
					break;

				case CueTrackType.CDI_2336:
				case CueTrackType.Mode2_2336:
				case CueTrackType.Mode2_2352:
					mode = 2;
					if (Policy.CUE_PregapMode2_As_XA_Form2)
					{
						job.DestBuffer2448[job.DestOffset + 12 + 6] = 0x20;
						job.DestBuffer2448[job.DestOffset + 12 + 10] = 0x20;
					}
					form = 2; //no other choice right now really
					break;

				case CueTrackType.Mode1_2048:
					mode = 1;
					Pause = true;
					break;

				default:
					throw new InvalidOperationException($"Not supported: {TrackType}");
			}

			//audio has no sector header but the others do
			if (mode != 0)
			{
				if ((job.Parts & ESectorSynthPart.Header16) != 0)
					SynthUtils.SectorHeader(job.DestBuffer2448, job.DestOffset + 0, job.LBA, mode);
			}

			switch (mode)
			{
				case 1:
				{
					if ((job.Parts & ESectorSynthPart.ECMAny) != 0)
						SynthUtils.ECM_Mode1(job.DestBuffer2448, job.DestOffset + 0);
					break;
				}
				case 2 when form == 2:
					SynthUtils.EDC_Mode2_Form2(job.DestBuffer2448, job.DestOffset);
					break;
			}

			SynthSubchannelAsNeed(job);
		}
	}
}