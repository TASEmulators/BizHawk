using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	internal class Synthesize_DiscTracks_From_DiscTOC_Job
	{
		private readonly Disc IN_Disc;

		private readonly DiscSession IN_Session;
		
		private DiscTOC TOCRaw => IN_Session.TOC;
		private IList<DiscTrack> Tracks => IN_Session.Tracks;

		public Synthesize_DiscTracks_From_DiscTOC_Job(Disc disc, DiscSession session)
		{
			IN_Disc = disc;
			IN_Session = session;
		}

		/// <exception cref="InvalidOperationException">first track of <see cref="TOCRaw"/> is not <c>1</c></exception>
		public void Run()
		{
			var dsr = new DiscSectorReader(IN_Disc) { Policy = { DeterministicClearBuffer = false } };

			//add a lead-in track
			Tracks.Add(new()
			{
				Number = 0,
				Control = EControlQ.None, //we'll set this later
				LBA = -new Timestamp(99,99,99).Sector //obvious garbage
			});

			for (var i = TOCRaw.FirstRecordedTrackNumber; i <= TOCRaw.LastRecordedTrackNumber; i++)
			{
				var item = TOCRaw.TOCItems[i];
				var track = new DiscTrack
				{
					Number = i,
					Control = item.Control,
					LBA = item.LBA
				};
				Tracks.Add(track);

				if (!item.IsData)
					track.Mode = 0;
				else
				{
					//determine the mode by a hardcoded heuristic: check mode of first sector
					track.Mode = dsr.ReadLBA_Mode(track.LBA);
				}

				//determine track length according to... how? It isn't clear.
				//Let's not do this until it's needed.
				//if (i == ntracks - 1)
				//  track.Length = TOCRaw.LeadoutLBA.Sector - track.LBA;
				//else track.Length = (TOCRaw.TOCItems[i + 2].LBATimestamp.Sector - track.LBA);
			}

			//add lead-out track
			Tracks.Add(new()
			{
				Number = 0xA0, //right?
				//kind of a guess, but not completely
				Control = Tracks[Tracks.Count - 1].Control,
				Mode = Tracks[Tracks.Count - 1].Mode,
				LBA = TOCRaw.LeadoutLBA
			});

			//link track list
			for (var i = 0; i < Tracks.Count - 1; i++)
			{
				Tracks[i].NextTrack = Tracks[i + 1];
			}

			//fix lead-in track type
			//guesses:
			Tracks[0].Control = Tracks[1].Control;
			Tracks[0].Mode = Tracks[1].Mode;
		}
	}
}