using System;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Emulation.DiscSystem;

unsafe class MednadiscTester
{
	public class QuickSubcodeReader
	{
		public QuickSubcodeReader(byte[] buffer)
		{
			this.buffer = buffer;
		}

		public void ReadLBA_SubchannelQ(int offset, ref SubchannelQ sq)
		{
			sq.q_status = buffer[offset + 0];
			sq.q_tno = buffer[offset + 1];
			sq.q_index = buffer[offset + 2];
			sq.min.BCDValue = buffer[offset + 3];
			sq.sec.BCDValue = buffer[offset + 4];
			sq.frame.BCDValue = buffer[offset + 5];
			//nothing in byte[6]
			sq.ap_min.BCDValue = buffer[offset + 7];
			sq.ap_sec.BCDValue = buffer[offset + 8];
			sq.ap_frame.BCDValue = buffer[offset + 9];

			//CRC is stored inverted and big endian.. so... do the opposite
			byte hibyte = (byte)(~buffer[offset + 10]);
			byte lobyte = (byte)(~buffer[offset + 11]);
			sq.q_crc = (ushort)((hibyte << 8) | lobyte);
		}

		byte[] buffer;
	}

	public static void TestDirectory(string dpTarget)
	{
		bool skip = true;
		var po = new ParallelOptions();
		po.MaxDegreeOfParallelism = 1;
		var files = new DirectoryInfo(dpTarget).GetFiles();
		//foreach (var fi in new DirectoryInfo(dpTarget).GetFiles())
		Parallel.ForEach(files, po, (fi) =>
		{
			if (fi.Extension.ToLower() == ".cue") { }
			else if (fi.Extension.ToLower() == ".ccd") { }
			else return;

			//if (skip)
			//{
			//  //GOAL STORM!!!! (track flags)
			//  //Street Fighter Collection (USA)!!! (data track at end)
			//  //Wu-Tang Shaolin Style is a PS1 game that reads from the leadout area and will flip out and become unresponsive at the parental lock screen if you haven't got it at least somewhat right in regards to Q subchannel data.
			//  if (fi.FullName.Contains("Strike Point"))
			//    skip = false;
			//}
			//if (skip) return;
	
			NewTest(fi.FullName,true);
		});
		foreach (var di in new DirectoryInfo(dpTarget).GetDirectories())
			TestDirectory(di.FullName);
	}

	static void ReadBizTOC(Disc disc, ref MednadiscTOC read_target, ref MednadiscTOCTrack[] tracks101)
	{
		read_target.disc_type = (byte)disc.TOCRaw.Session1Format;
		read_target.first_track = (byte)disc.TOCRaw.FirstRecordedTrackNumber; //i _think_ thats what is meant here
		read_target.last_track = (byte)disc.TOCRaw.LastRecordedTrackNumber; //i _think_ thats what is meant here

		tracks101[0].lba = tracks101[0].adr = tracks101[0].control = 0;

		for (int i = 1; i < 100; i++)
		{
			var item = disc.TOCRaw.TOCItems[i];
			tracks101[i].adr = (byte)(item.Exists ? 1 : 0);
			tracks101[i].lba = (uint)item.LBATimestamp.Sector;
			tracks101[i].control = (byte)item.Control;
		}

		//"Convenience leadout track duplication." for mednafen purposes so follow mednafen rules
		tracks101[read_target.last_track + 1].adr = 1;
		tracks101[read_target.last_track + 1].control = (byte)(tracks101[read_target.last_track].control & 0x04);
		tracks101[read_target.last_track + 1].lba = (uint)disc.TOCRaw.LeadoutTimestamp.Sector;

		//element 100 is to be copied as the lead-out track
		tracks101[100] = tracks101[read_target.last_track + 1];
	}

	public static bool NewTest(string path, bool useNewCuew)
	{
		bool ret = false;

		Console.WriteLine(Path.GetFileNameWithoutExtension(path));
		
		Disc disc;
		if (Path.GetExtension(path).ToLower() == ".cue" || Path.GetExtension(path).ToLower() == ".ccd")
		{
			DiscMountJob dmj = new DiscMountJob();
			dmj.IN_FromPath = path;
			dmj.Run();
			disc = dmj.OUT_Disc;
		}
		else return false;

		IntPtr mednadisc = mednadisc_LoadCD(path);
		if (mednadisc == IntPtr.Zero)
		{
			Console.WriteLine("MEDNADISC COULDNT LOAD FILE");
			goto END;
		}

		//check tocs
		MednadiscTOC medna_toc;
		MednadiscTOCTrack[] medna_tracks = new MednadiscTOCTrack[101];
		fixed (MednadiscTOCTrack* _tracks = &medna_tracks[0])
			mednadisc_ReadTOC(mednadisc, &medna_toc, _tracks);
		MednadiscTOC biz_toc = new MednadiscTOC();
		MednadiscTOCTrack[] biz_tracks = new MednadiscTOCTrack[101];
		ReadBizTOC(disc, ref biz_toc, ref biz_tracks);
		if (medna_toc.first_track != biz_toc.first_track) System.Diagnostics.Debugger.Break();
		if (medna_toc.last_track != biz_toc.last_track) System.Diagnostics.Debugger.Break();
		if (medna_toc.disc_type != biz_toc.disc_type) System.Diagnostics.Debugger.Break();
		for (int i = 0; i < 101; i++)
		{
			if(medna_tracks[i].adr != biz_tracks[i].adr) System.Diagnostics.Debugger.Break();
			if (medna_tracks[i].control != biz_tracks[i].control) System.Diagnostics.Debugger.Break();
			if (medna_tracks[i].lba != biz_tracks[i].lba) System.Diagnostics.Debugger.Break();
		}


		//TODO - determine length some superior way

		int nSectors = (int)(disc.Structure.BinarySize / 2352) - 150;
		var subbuf = new byte[96];
		var discbuf = new byte[2352 + 96];
		var monkeybuf = new byte[2352 + 96];
		var disc_qbuf = new byte[96];
		var monkey_qbuf = new byte[96];

		int startSector = 0;
		for (int i = startSector; i < nSectors; i++)
		{
			mednadisc_ReadSector(mednadisc, i, monkeybuf);
			disc.ReadLBA_2352(i, discbuf, 0);
			disc.ReadLBA_SectorEntry(i).SubcodeSector.ReadSubcodeDeinterleaved(subbuf, 0);
			SubcodeUtils.Interleave(subbuf, 0, discbuf, 2352);
			//remove P 
			for (int q = 2352; q < 2352 + 96; q++)
			{
				discbuf[q] &= 0x7F;
				monkeybuf[q] &= 0x7F;
			}
			for (int q = 0; q < 2352 + 96; q++)
			{
				if (discbuf[q] != monkeybuf[q])
				{
					Console.WriteLine("MISMATCH: " + Path.GetFileName(path));

					//decode Q subchannels for manual investigation
					SubcodeUtils.Deinterleave(discbuf, 2352, disc_qbuf, 0);
					var asr = new QuickSubcodeReader(disc_qbuf);
					SubchannelQ disc_q = new SubchannelQ();
					asr.ReadLBA_SubchannelQ(12, ref disc_q);

					SubcodeUtils.Deinterleave(monkeybuf, 2352, monkey_qbuf, 0);
					asr = new QuickSubcodeReader(monkey_qbuf);
					SubchannelQ monkey_q = new SubchannelQ();
					asr.ReadLBA_SubchannelQ(12, ref monkey_q);

					System.Diagnostics.Debugger.Break();
					goto END;
				}
			}
		}

		ret = true;

	END:
		disc.Dispose();
		if(mednadisc != IntPtr.Zero)
			mednadisc_CloseCD(mednadisc);

		return ret;
	}
}