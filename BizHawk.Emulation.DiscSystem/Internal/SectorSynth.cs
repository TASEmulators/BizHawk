using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Indicates which part of a sector are needing to be synthesized.
	/// Sector synthesis may create too much data, but this is a hint as to what's needed
	/// TODO - add a flag indicating whether clearing has happened
	/// TODO - add output to the job indicating whether interleaving has happened. let the sector reader be responsible
	/// </summary>
	[Flags] enum ESectorSynthPart
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

	/// <summary>
	/// Basic unit of sector synthesis
	/// </summary>
	interface ISectorSynthJob2448
	{
		/// <summary>
		/// Synthesizes a sctor with the given job parameters
		/// </summary>
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
	/// an ISectorSynthProvider that just returns a value from an array of pre-made sectors
	/// </summary>
	class ArraySectorSynthProvider : ISectorSynthProvider
	{
		public List<ISectorSynthJob2448> Sectors = new List<ISectorSynthJob2448>();
		public int FirstLBA;

		public ISectorSynthJob2448 Get(int lba)
		{
			int index = lba - FirstLBA;
			if (index < 0) return null;
			if (index >= Sectors.Count) return null;
			return Sectors[index];
		}
	}

	/// <summary>
	/// an ISectorSynthProvider that just returns a fixed synthesizer
	/// </summary>
	class SimpleSectorSynthProvider : ISectorSynthProvider
	{
		public ISectorSynthJob2448 SS;

		public ISectorSynthJob2448 Get(int lba) { return SS; }
	}

	/// <summary>
	/// Returns 'Patch' synth if the provided condition is met
	/// </summary>
	class ConditionalSectorSynthProvider : ISectorSynthProvider
	{
		Func<int,bool> Condition;
		ISectorSynthJob2448 Patch;
		ISectorSynthProvider Parent;
		public void Install(Disc disc, Func<int, bool> condition, ISectorSynthJob2448 patch)
		{
			Parent = disc.SynthProvider;
			disc.SynthProvider = this;
			Condition = condition;
			Patch = patch;
		}
		public ISectorSynthJob2448 Get(int lba)
		{
			if (Condition(lba))
				return Patch;
			else return Parent.Get(lba);
		}
	}

	/// <summary>
	/// When creating a disc, this is set with a callback that can deliver an ISectorSynthJob2448 for the given LBA
	/// </summary>
	interface ISectorSynthProvider
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
	struct SectorSynthParams
	{
		//public long[] BlobOffsets;
		public MednaDisc MednaDisc;
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

	class SS_Leadout : ISectorSynthJob2448
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

			var ses = job.Disc.Structure.Sessions[SessionNumber];
			int lba_relative = job.LBA - ses.LeadoutTrack.LBA;

			//data is zero

			int ts = lba_relative;
			int ats = job.LBA;

			const int ADR = 0x1; // Q channel data encodes position
			EControlQ control = ses.LeadoutTrack.Control;

			//ehhh? CDI?
		 //if(toc.tracks[toc.last_track].valid)
		 // control |= toc.tracks[toc.last_track].control & 0x4;
		 //else if(toc.disc_type == DISC_TYPE_CD_I)
		 // control |= 0x4;
			control |= (EControlQ)(((int)ses.LastInformationTrack.Control) & 4);
			
			SubchannelQ sq = new SubchannelQ();
			sq.SetStatus(ADR, control);
			sq.q_tno.BCDValue = 0xAA;
			sq.q_index.BCDValue = 0x01;
			sq.Timestamp = ts;
			sq.AP_Timestamp = ats;
			sq.zero = 0;

			//finally, rely on a gap sector to do the heavy lifting to synthesize this
			CUE.CueTrackType TrackType = CUE.CueTrackType.Audio;
			if (ses.LeadoutTrack.IsData)
			{
				if (job.Disc.TOC.Session1Format == SessionFormat.Type20_CDXA || job.Disc.TOC.Session1Format == SessionFormat.Type10_CDI)
					TrackType = CUE.CueTrackType.Mode2_2352;
				else
					TrackType = CUE.CueTrackType.Mode1_2352;
			}

			CUE.SS_Gap ss_gap = new CUE.SS_Gap()
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