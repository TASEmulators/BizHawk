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
		/// Automagically loads a disc, without any fine-tuned control at all
		/// </summary>
		public static Disc LoadAutomagic(string path)
		{
			var job = new DiscMountJob { IN_FromPath = path };
			//job.IN_DiscInterface = DiscInterface.MednaDisc; //TEST
			job.Run();
			return job.OUT_Disc;
		}

		/// <summary>
		/// The DiscStructure corresponding to the TOCRaw
		/// </summary>
		public DiscStructure Structure;

		/// <summary>
		/// DiscStructure.Session 1 of the disc, since that's all thats needed most of the time.
		/// </summary>
		public DiscStructure.Session Session1 { get { return Structure.Sessions[1]; } }

		/// <summary>
		/// The name of a disc. Loosely based on the filename. Just for informational purposes.
		/// </summary>
		public string Name;

		/// <summary>
		/// The DiscTOCRaw corresponding to the RawTOCEntries.
		/// TODO - there's one of these for every session, so... having one here doesnt make sense
		/// so... 
		/// TODO - remove me
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
		/// The sectors on the disc. Don't use this directly! Use the SectorSynthProvider instead.
		/// TODO - eliminate this entirely and do entirely with the delegate (much faster disc loading... but massively annoying architecture inside-out logic)
		/// </summary>
		internal List<ISectorSynthJob2448> _Sectors = new List<ISectorSynthJob2448>();

		/// <summary>
		/// ISectorSynthProvider instance for the disc. May be daisy-chained
		/// </summary>
		internal ISectorSynthProvider SynthProvider;

		/// <summary>
		/// Parameters set during disc loading which can be referenced by the sector synthesizers
		/// </summary>
		internal SectorSynthParams SynthParams = new SectorSynthParams();

		/// <summary>
		/// Forbid public construction
		/// </summary>
		internal Disc()
		{}

	}
}