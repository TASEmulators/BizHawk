using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

//cue format preferences notes

//pcejin -
//does not like session commands
//it can handle binpercue
//it seems not to be able to handle binpertrack, or maybe i am doing something wrong (still havent ruled it out)

namespace BizHawk
{
	class DiscoHawk
	{

		public static string GetExeDirectoryAbsolute()
		{
			string p = Path.GetDirectoryName(Assembly.GetEntryAssembly().GetName().CodeBase);
			if (p.Substring(0, 6) == "file:\\")
				p = p.Remove(0, 6);
			string z = p;
			return p;
		}

		[STAThread]
		static void Main(string[] args)
		{
			DiscSystem.FFMpeg.FFMpegPath = Path.Combine(GetExeDirectoryAbsolute(), "ffmpeg.exe");
			new DiscoHawk().Run(args);
		}

		void Run(string[] args)
		{
			bool gui = true;
			foreach (var arg in args)
			{
				if (arg.ToUpper() == "COMMAND") gui = false;
			}

			if (gui)
			{
				var dialog = new MainDiscoForm();
				dialog.ShowDialog();
				return;
			}

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

			//test reading the subcode data. unfortunately we don't have lead-in subcode so we have no TOC
			//using (FileStream fs = File.OpenRead(@"D:\programs\cdrdao\awakening.bin"))
			//{
			//    using (FileStream fsOut = File.OpenWrite(@"D:\programs\cdrdao\awakening.sub.q"))
			//    //using (FileStream fsOut = File.OpenWrite(@"D:\programs\cdrdao\data.sub.q"))
			//    {
			//        int numSectors = (int)fs.Length / (2352 + 96);
			//        for (int i = 0; i < numSectors; i++)
			//        {
			//            fs.Position = i * (2352 + 96) + 2352;
			//            byte[] tempout = new byte[12];
			//            byte[] tempin = new byte[96];
			//            fs.Read(tempin, 0, 96);
			//            DiscSystem.SubcodeDataDecoder.Unpack_Q(tempout, 0, tempin, 0);
			//            fsOut.Write(tempout, 0, 12);
			//        }

			//        //for (; ; )
			//        //{
			//        //    int ret = stream.ReadByte();
			//        //    if (ret == -1) break;
			//        //    fsOut.WriteByte((byte)ret);
			//        //}
			//    }
			//} return;

			//DiscSystem.Disc disc = DiscSystem.Disc.FromCuePath(@"D:\discs\Bomberman_'94_Taikenban_(SCD)(JPN)_-_wav'd\Bomberman '94 Taikenban (SCD)(JPN)_hawked.cue");
			//DiscSystem.Disc disc = DiscSystem.Disc.FromCuePath(@"D:\discs\Bomberman_'94_Taikenban_(SCD)(JPN)_-_wav'd\Bomberman '94 Taikenban (SCD)(JPN).cue");
			//var prefs = new DiscSystem.CueBinPrefs();
			//prefs.AnnotateCue = false;
			//prefs.OneBlobPerTrack = false;
			//prefs.ReallyDumpBin = true;
			//prefs.SingleSession = true;
			////var cueBin = disc.DumpCueBin("Bomberman '94 Taikenban (SCD)(JPN)_hawked_hawked", prefs);
			//var cueBin = disc.DumpCueBin("Bomberman '94 Taikenban (SCD)(JPN)_hawked", prefs);
			//cueBin.Dump(@"D:\discs\Bomberman_'94_Taikenban_(SCD)(JPN)_-_wav'd", prefs);

			DiscSystem.Disc disc = DiscSystem.Disc.FromCuePath(@"D:\discs\R-Type Complete CD (J)");
			var prefs = new DiscSystem.CueBinPrefs();
			prefs.AnnotateCue = false;
			prefs.OneBlobPerTrack = false;
			prefs.ReallyDumpBin = true;
			prefs.DumpSubchannelQ = true;
			//prefs.OmitRedundantIndex0 = true;
			prefs.SingleSession = true;
			//var cueBin = disc.DumpCueBin("Bomberman '94 Taikenban (SCD)(JPN)_hawked_hawked", prefs);
			var cueBin = disc.DumpCueBin("entire", prefs);
			cueBin.Dump(@"D:\programs\cdrdao\eac-ripped", prefs);
		}
	}

}