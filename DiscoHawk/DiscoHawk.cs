using System;
using System.Collections.Generic;
using System.IO;

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
			//string testfile = @"d:\Radiant Silvergun (J)\Radiant Silvergun (J).cue";
			//var disc = Disc.Disc.FromCuePath(testfile);
			
			//string testfile = @"r:\isos\memtest86-3.2.iso";
			//var disc = Disc.Disc.FromIsoPath(testfile);

			//Console.WriteLine(disc.ReadTOC().DebugPrint());
			//disc.DumpBin_2352("d:\\test.2352");

			//test reading the subcode data. unfortunately we don't have lead-in subcode so we have no TOC
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