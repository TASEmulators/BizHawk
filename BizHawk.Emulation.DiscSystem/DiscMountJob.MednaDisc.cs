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
					SynthUtils.DeinterleaveSubcodeInplace(job.DestBuffer2448, 2352);
				}
			}
		}

		void RunMednaDisc()
		{
			var disc = new Disc();
			OUT_Disc = disc;

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
			OUT_Disc.SynthProvider = new SimpleSectorSynthProvider() { SS = synth };

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
					q_crc = 0 //meaningless
				};

				//a special fixup: mednafen's entry 100 is the lead-out track, so change it into the A2 raw toc entry
				if (i == 100)
				{
					q.q_index.BCDValue = 0xA2;
				}

				disc.RawTOCEntries.Add(new RawTOCEntry { QData = q });
			}

			//synth A0 and A1 entries (indicating first and last recorded tracks and also session type)
			var qA0 = new SubchannelQ
			{
				q_status = SubchannelQ.ComputeStatus(kADR, kUnknownControl),
				q_tno = BCD2.FromDecimal(0), //unknown with mednadisc
				q_index = BCD2.FromBCD(0xA0),
				min = BCD2.FromDecimal(0), //unknown with mednadisc
				sec = BCD2.FromDecimal(0), //unknown with mednadisc
				frame = BCD2.FromDecimal(0), //unknown with mednadisc
				zero = 0, //unknown with mednadisc
				ap_min = BCD2.FromDecimal(md.TOC.first_track),
				ap_sec = BCD2.FromDecimal(md.TOC.disc_type),
				ap_frame = BCD2.FromDecimal(0),
				q_crc = 0, //meaningless
			};
			disc.RawTOCEntries.Add(new RawTOCEntry { QData = qA0 });
			var qA1 = new SubchannelQ
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
				q_crc = 0, //meaningless
			};
			disc.RawTOCEntries.Add(new RawTOCEntry { QData = qA1 });

		}
	}
}