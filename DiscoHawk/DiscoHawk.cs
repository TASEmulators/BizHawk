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
		static void Main(string[] args)
		{
			new DiscoHawk().Run(args);
		}

		void Run(string[] args)
		{
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
			//prefs.PreferPregapCommand = false;
			//munged = disc.DumpCueBin("test", prefs);
			//munged.Dump("d:\\test", prefs);
			//File.WriteAllText("d:\\test\\redump.txt", munged.CreateRedumpReport());

			//try roundtripping back to one file
			//disc = Disc.Disc.FromCuePath("d:\\test\\test.cue");
			//prefs.ReallyDumpBin = false;
			//prefs.OneBinPerTrack = false;
			//prefs.PreferPregapCommand = true;
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

		}
	}

}