using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Indicates which part of a sector are needing to be synthesized.
	/// Sector synthesis may create too much data, but this is a hint as to what's needed
	/// TODO - add a flag indicating whether clearing has happened
	/// TODO - add output to the job indicating whether interleaving has happened. let the sector reader be responsible
	/// </summary>
	[Flags]
	internal enum ESectorSynthPart
	{
		/// <summary>
		/// The data sector header is required. There's no header for audio tracks/sectors.
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
		UserComplete = (Header16 | User2048 | ECM288Complete),

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

	/// <summary>
	/// Basic unit of sector synthesis
	/// </summary>
	internal interface ISectorSynthJob2448
	{
		/// <summary>
		/// Synthesizes a sctor with the given job parameters
		/// </summary>
		void Synth(SectorSynthJob job);
	}

	/// <summary>
	/// Not a proper job? maybe with additional flags, it could be
	/// </summary>
	internal class SectorSynthJob
	{
		public int LBA;
		public ESectorSynthPart Parts;
		public byte[] DestBuffer2448;
		public int DestOffset;
		public SectorSynthParams Params;
		public Disc Disc;
	}

	/// <summary>
	/// an ISectorSynthProvider that just returns a value from an array of pre-made sectors
	/// </summary>
	internal class ArraySectorSynthProvider : ISectorSynthProvider
	{
		public List<ISectorSynthJob2448> Sectors = new();
		public int FirstLBA;

		public ISectorSynthJob2448 Get(int lba)
		{
			var index = lba - FirstLBA;
			if (index < 0) return null;
			return index >= Sectors.Count ? null : Sectors[index];
		}
	}

	/// <summary>
	/// an ISectorSynthProvider that just returns a fixed synthesizer
	/// </summary>
	internal class SimpleSectorSynthProvider : ISectorSynthProvider
	{
		public ISectorSynthJob2448 SS;

		public ISectorSynthJob2448 Get(int lba) { return SS; }
	}

	/// <summary>
	/// Returns 'Patch' synth if the provided condition is met
	/// </summary>
	internal class ConditionalSectorSynthProvider : ISectorSynthProvider
	{
		private Func<int,bool> Condition;
		private ISectorSynthJob2448 Patch;
		private ISectorSynthProvider Parent;
		
		public void Install(Disc disc, Func<int, bool> condition, ISectorSynthJob2448 patch)
		{
			Parent = disc.SynthProvider;
			disc.SynthProvider = this;
			Condition = condition;
			Patch = patch;
		}
		
		public ISectorSynthJob2448 Get(int lba)
		{
			return Condition(lba) ? Patch : Parent.Get(lba);
		}
	}

	/// <summary>
	/// When creating a disc, this is set with a callback that can deliver an ISectorSynthJob2448 for the given LBA
	/// </summary>
	internal interface ISectorSynthProvider
	{
		/// <summary>
		/// Retrieves an ISectorSynthJob2448 for the given LBA
		/// </summary>
		ISectorSynthJob2448 Get(int lba);
	}

	/// <summary>
	/// Generic parameters for sector synthesis.
	/// To cut down on resource utilization, these can be stored in a disc and are tightly coupled to
	/// the SectorSynths that have been setup for it
	/// </summary>
	internal struct SectorSynthParams
	{
		//public long[] BlobOffsets;
		public MednaDisc MednaDisc;
	}


	internal class SS_PatchQ : ISectorSynthJob2448
	{
		public ISectorSynthJob2448 Original;
		public readonly byte[] Buffer_SubQ = new byte[12];
		
		public void Synth(SectorSynthJob job)
		{
			Original.Synth(job);

			if ((job.Parts & ESectorSynthPart.SubchannelQ) == 0)
				return;

			//apply patched subQ
			for (var i = 0; i < 12; i++)
				job.DestBuffer2448[2352 + 12 + i] = Buffer_SubQ[i];
		}
	}

	internal class SS_Leadout : ISectorSynthJob2448
	{
		public int SessionNumber;
		public DiscMountPolicy Policy;

		public void Synth(SectorSynthJob job)
		{
			//be lazy, just generate the whole sector unconditionally
			//this is mostly based on mednafen's approach, which was probably finely tailored for PSX
			//heres the comments on the subject:
			//  I'm not trusting that the "control" field for the TOC leadout entry will always be set properly, so | the control fields for the last track entry
			//  and the leadout entry together before extracting the D2 bit.  Audio track->data leadout is fairly benign though maybe noisy(especially if we ever implement
			//  data scrambling properly), but data track->audio leadout could break things in an insidious manner for the more accurate drive emulation code).

			var ses = job.Disc.Sessions[SessionNumber];
			var lba_relative = job.LBA - ses.LeadoutTrack.LBA;

			//data is zero

			var ts = lba_relative;
			var ats = job.LBA;

			const int ADR = 0x1; // Q channel data encodes position
			var control = ses.LeadoutTrack.Control;

			//ehhh? CDI?
//			if(toc.tracks[toc.last_track].valid) control |= toc.tracks[toc.last_track].control & 0x4;
//			else if(toc.disc_type == DISC_TYPE_CD_I) control |= 0x4;
			control |= (EControlQ)(((int)ses.LastInformationTrack.Control) & 4);
			
			SubchannelQ sq = default;
			sq.SetStatus(ADR, control);
			sq.q_tno.BCDValue = 0xAA;
			sq.q_index.BCDValue = 0x01;
			sq.Timestamp = ts;
			sq.AP_Timestamp = ats;
			sq.zero = 0;

			//finally, rely on a gap sector to do the heavy lifting to synthesize this
			var TrackType = CUE.CueTrackType.Audio;
			if (ses.LeadoutTrack.IsData)
			{
				if (job.Disc.TOC.SessionFormat is SessionFormat.Type20_CDXA or SessionFormat.Type10_CDI)
					TrackType = CUE.CueTrackType.Mode2_2352;
				else
					TrackType = CUE.CueTrackType.Mode1_2352;
			}

			var ss_gap = new CUE.SS_Gap
			{
				Policy = Policy,
				sq = sq,
				TrackType = TrackType,
				Pause = true //?
			};

			ss_gap.Synth(job);
		}
	}

}