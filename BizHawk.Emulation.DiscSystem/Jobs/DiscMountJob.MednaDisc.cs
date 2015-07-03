using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	partial class DiscMountJob
	{
		class SS_MednaDisc : ISectorSynthJob2448
		{
			public void Synth(SectorSynthJob job)
			{
				//mednafen is always synthesizing everything, no need to worry about flags.. mostly./
				job.Params.MednaDisc.Read_2442(job.LBA, job.DestBuffer2448, job.DestOffset);
				
				//we may still need to deinterleave it if subcode was requested and it needs deinterleaving
				if ((job.Parts & (ESectorSynthPart.SubcodeDeinterleave | ESectorSynthPart.SubcodeAny)) != 0)
				{
					SubcodeUtils.DeinterleaveInplace(job.DestBuffer2448, 2352);
				}
			}
		}

		void RunMednaDisc()
		{
			var disc = new Disc();

			//create a MednaDisc and give it to the disc for ownership
			var md = new MednaDisc(IN_FromPath);
			disc.DisposableResources.Add(md);

			//"length of disc" for bizhawk's purposes (NOT a robust concept!) is determined by beginning of leadout track
			var m_leadoutTrack = md.TOCTracks[100];
			int nSectors = (int)m_leadoutTrack.lba;

			//make synth param memos
			disc.SynthParams.MednaDisc = md;

			//this is the sole sector synthesizer we'll need
			var synth = new SS_MednaDisc();

			//make sector interfaces:
			var pregap_sector_zero = new Sector_Zero();
			var pregap_subcode_zero = new ZeroSubcodeSector();
			for (int i = 0; i < 150; i++)
			{
				var se = new SectorEntry(pregap_sector_zero);
				se.SectorSynth = synth;
				disc.Sectors.Add(se);
				se.SubcodeSector = pregap_subcode_zero;
			}

			//2. actual sectors
			for (int i = 0; i < nSectors; i++)
			{
				//var sectorInterface = new MednaDiscSectorInterface() { LBA = i, md = md };
				var se = new SectorEntry(null);
				//se.SubcodeSector = new MednaDiscSubcodeSectorInterface() { LBA = i, md = md };
				se.SectorSynth = synth;
				disc.Sectors.Add(se);
			}

			BufferedSubcodeSector bss = new BufferedSubcodeSector(); //TODO - its hacky that we need this..

			//ADR (q-Mode) is necessarily 0x01 for a RawTOCEntry
			const int kADR = 1;
			const int kUnknownControl = 0;

			//mednafen delivers us what is essentially but not exactly (or completely) a TOCRaw.
			//we need to synth RawTOCEntries from this and then turn it into a proper TOCRaw
			//when coming from mednafen, there are 101 entries.
			//entry[0] is placeholder junk, not to be used
			//entry[100] is the leadout track (A0)
			//A1 and A2 are in the form of FirstRecordedTrackNumber and LastRecordedTrackNumber
			for (int i = 1; i < 101; i++)
			{
				var m_te = md.TOCTracks[i];

				//dont add invalid (absent) items
				if (!m_te.Valid)
					continue;

				var m_ts = new Timestamp((int)m_te.lba + 150); //these are supposed to be absolute timestamps

				var q = new SubchannelQ
				{
					q_status = SubchannelQ.ComputeStatus(kADR, (EControlQ)m_te.control), 
					q_tno = BCD2.FromDecimal(0), //unknown with mednadisc
					q_index = BCD2.FromDecimal(i),
					min = BCD2.FromDecimal(0), //unknown with mednadisc
					sec = BCD2.FromDecimal(0), //unknown with mednadisc
					frame = BCD2.FromDecimal(0), //unknown with mednadisc
					zero = 0, //unknown with mednadisc
					ap_min = BCD2.FromDecimal(m_ts.MIN),
					ap_sec = BCD2.FromDecimal(m_ts.SEC),
					ap_frame = BCD2.FromDecimal(m_ts.FRAC),
				};

				//a special fixup: mednafen's entry 100 is the lead-out track
				if (i == 100)
					q.q_index.BCDValue = 0xA2;

				//CRC cant be calculated til we've got all the fields setup
				q.q_crc = bss.Synthesize_SubchannelQ(ref q, true);

				disc.RawTOCEntries.Add(new RawTOCEntry { QData = q });
			}

			//synth A1 and A2 entries instead of setting FirstRecordedTrackNumber and LastRecordedTrackNumber below
			var qA1 = new SubchannelQ
			{
				q_status = SubchannelQ.ComputeStatus(kADR, kUnknownControl),
				q_tno = BCD2.FromDecimal(0), //unknown with mednadisc
				q_index = BCD2.FromBCD(0xA0),
				min = BCD2.FromDecimal(0), //unknown with mednadisc
				sec = BCD2.FromDecimal(0), //unknown with mednadisc
				frame = BCD2.FromDecimal(0), //unknown with mednadisc
				zero = 0, //unknown with mednadisc
				ap_min = BCD2.FromDecimal(md.TOC.first_track),
				ap_sec = BCD2.FromDecimal(0),
				ap_frame = BCD2.FromDecimal(0),
			};
			qA1.q_crc = bss.Synthesize_SubchannelQ(ref qA1, true);
			disc.RawTOCEntries.Add(new RawTOCEntry { QData = qA1 });
			var qA2 = new SubchannelQ
			{
				q_status = SubchannelQ.ComputeStatus(kADR, kUnknownControl),
				q_tno = BCD2.FromDecimal(0), //unknown with mednadisc
				q_index = BCD2.FromBCD(0xA1),
				min = BCD2.FromDecimal(0), //unknown with mednadisc
				sec = BCD2.FromDecimal(0), //unknown with mednadisc
				frame = BCD2.FromDecimal(0), //unknown with mednadisc
				zero = 0, //unknown with mednadisc
				ap_min = BCD2.FromDecimal(md.TOC.last_track),
				ap_sec = BCD2.FromDecimal(0),
				ap_frame = BCD2.FromDecimal(0),
			};
			qA2.q_crc = bss.Synthesize_SubchannelQ(ref qA2, true);
			disc.RawTOCEntries.Add(new RawTOCEntry { QData = qA2 });

			//generate the toc from the entries. still not sure we're liking this idea
			var tocSynth = new DiscTOCRaw.SynthesizeFromRawTOCEntriesJob() { Entries = disc.RawTOCEntries };
			tocSynth.Run();
			disc.TOCRaw = tocSynth.Result;

			//DO THIS IN A MORE UNIFORM WAY PLEASE
			//setup the DiscStructure 
			//disc.Structure = new DiscStructure();
			//var ses = new DiscStructure.Session();
			//disc.Structure.Sessions.Add(ses);
			//for (int i = 1; i < 100; i++)
			//{
			//  var m_te = md.TOCTracks[i];
			//  if (!m_te.Valid) continue;

			//  DiscStructure.Track track = new DiscStructure.Track() { Number = i };
			//  ses.Tracks.Add(track);
			//  if ((m_te.control & (int)EControlQ.DATA) == 0)
			//    track.IsData = false;
			//  else
			//    track.IsData = true;

			//  track.Start_LBA = (int)m_te.lba;
			//  track.

			//  //from mednafen, we couldnt build the index 0, and that's OK, since that's not really a sensible thing in CD terms anyway. 
			//  //I need to refactor this thing to oblivion
			//  //track.Indexes.Add(new DiscStructure.Index { Number = 0, LBA = (int)m_te.lba }); //<-- not accurate, but due for deletion
			//  //track.Indexes.Add(new DiscStructure.Index { Number = 1, LBA = (int)m_te.lba });
			//}

			//NOT FULLY COMPLETE

			OUT_Disc = disc;
		}
	}
}