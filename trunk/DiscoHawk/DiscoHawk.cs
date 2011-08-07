using System;
using System.Collections.Generic;
using System.IO;

//cue format preferences notes

//pcejin -
//does not like session commands
//it can handle binpercue
//it seems not to be able to handle binpertrack, or maybe i am doing something wrong (still havent ruled it out)

//isobuster -
//does not like onebinpertrack images with index 00


namespace BizHawk
{
	class DiscoHawk
	{
		[STAThread]
		static void Main(string[] args)
		{
			new DiscoHawk().Run(args);
		}

		void Run(string[] args)
		{
			bool gui = false;
			foreach (var arg in args)
			{
				if (arg.ToUpper() == "GUI") gui = true;
			}

			if (gui)
			{
				var dialog = new DiscoHawkDialog();
				dialog.ShowDialog();
			}

			return;
			//string exedir = BizHawk.MultiClient.PathManager.GetExeDirectoryAbsolute();
			//ffMpeg.Converter._ffExe = Path.Combine(exedir, "ffmpeg.exe");

			////cue+bin+mp3 tests
			//var conv = new ffMpeg.Converter();
			////conv.WorkingPath = Environment.GetEnvironmentVariable("TEMP");
			////var vf = conv.GetVideoInfo(@"D:\isos\scd\persia\Prince of Persia 01.mp3");
			//var o = conv.ConvertToFLV(@"D:\isos\scd\persia\Prince of Persia 01.mp3");

			////-i mp3file.mp3 -f wav outfile.wav


			//Disc.CueBin munged;
			//Disc.CueBinPrefs prefs = new Disc.CueBinPrefs();
			//prefs.SingleSession = true;
			//Disc.Disc disc;

			//string testfile = @"D:\isos\pcecd\cosmicfantasy2\Cosmic Fantasy II [U][CD][WTG990301][Telenet Japan][1992][PCE][thx-1138-darkwater].cue";
			//disc = Disc.Disc.FromCuePath(testfile);
			//prefs.ReallyDumpBin = true;
			//prefs.AnnotateCue = false;
			//prefs.OneBinPerTrack = true;
			//munged = disc.DumpCueBin("test", prefs);
			//munged.Dump("d:\\test", prefs);
			//File.WriteAllText("d:\\test\\redump.txt", munged.CreateRedumpReport());

			//try roundtripping back to one file
			//disc = Disc.Disc.FromCuePath("d:\\test\\test.cue");
			//prefs.ReallyDumpBin = false;
			//prefs.OneBinPerTrack = false;
			//munged = disc.DumpCueBin("one", prefs);
			//munged.Dump("d:\\test", prefs);

			
			//string testfile = @"r:\isos\memtest86-3.2.iso";
			//var disc = Disc.Disc.FromIsoPath(testfile);

			//Console.WriteLine(disc.ReadTOC().DebugPrint());
			//disc.DumpBin_2352("d:\\test.2352");

			////test reading the subcode data. unfortunately we don't have lead-in subcode so we have no TOC
			//using (FileStream fs = File.OpenRead("c:\\bof4.sub"))
			//{
			//    Disc.SubcodeStream stream = new Disc.SubcodeStream(fs, 0);
			//    stream.Channel = 'q';
			//    using (FileStream fsOut = File.OpenWrite("c:\\bof4.sub.q"))
			//    {
			//        for (; ; )
			//        {
			//            int ret = stream.ReadByte();
			//            if (ret == -1) break;
			//            fsOut.WriteByte((byte)ret);
			//        }
			//    }
			//}

			//notes: daemon tools does not like INDEX 00 00:00:00 / INDEX 01 00:00:00 in track 1 (audio track)
			//obviously, this is because the lead-in is supposed to be specified. we need to write that out
			//DiscSystem.Disc disc = DiscSystem.Disc.FromCuePath("D:\\discs\\Bomberman_'94_Taikenban_(SCD)(JPN)_-_wav'd\\Bomberman '94 Taikenban (SCD)(JPN).cue");
			//DiscSystem.Disc disc = DiscSystem.Disc.FromCuePath("D:\\discs\\Syd Mead's Terra Forming [U][CD.SCD][TGXCD1040][Syd Mead][1993][PCE][rigg].cue");
			//var prefs = new DiscSystem.CueBinPrefs();
			//prefs.AnnotateCue = false;
			//prefs.OneBlobPerTrack = true;
			//prefs.ReallyDumpBin = true;
			//prefs.OmitRedundantIndex0 = true;
			//prefs.SingleSession = true;
			//var cueBin = disc.DumpCueBin("testroundtrip", prefs);
			//cueBin.Dump("d:\\", prefs);
		}
	}

}