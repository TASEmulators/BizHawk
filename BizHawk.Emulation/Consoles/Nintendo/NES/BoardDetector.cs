//http://nesdev.parodius.com/bbs/viewtopic.php?p=4571&sid=db4c7e35316cc5d734606dd02f11dccb

using System;
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		static class BoardDetector
		{
			public static string Detect(RomInfo romInfo)
			{
				string key = string.Format("{0}	{1}	{2}	{3}",romInfo.MapperNumber,romInfo.PRG_Size,romInfo.CHR_Size,romInfo.PRAM_Size);
				string board;
				Table.TryGetValue(key, out board);
				return board;
			}

			public static Dictionary<string,string> Table = new Dictionary<string,string>();
			static BoardDetector()
			{
				var sr = new StringReader(ClassifyTable);
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					var parts = line.Split('\t');
					if (parts.Length < 5) continue;
					string key = parts[0] + "\t" + parts[1] + "\t" + parts[2] + "\t" + parts[3];
					string board = line.Replace(key, "");
					board = board.TrimStart('\t');
					if (board.IndexOf(';') != -1)
						board = board.Substring(0, board.IndexOf(';'));
					Table[key] = board;
				}
			}
//MAP	PRG	CHR	PRAM	BOARD
			static string ClassifyTable = @"
0	1	1	0	NROM
0	2	1	0	NROM
1	8	0	8	SNROM;	this handles zelda,
2	8	0	0	UNROM
2	16	0	0	UOROM
3	2	2	0	CNROM
3	2	4	0	CNROM
7	8	0	0	ANROM
7	16	0	0	AOROM
11	4	2	0	Discrete_74x377
11	2	4	0	Discrete_74x377
13	2	0	0	CPROM
66	4	2	0	GxROM
66	8	4	0	GxROM
";

		}
	}
}

                        //STD_SAROM                  = MakeId<    1,   64,   64,  8,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SBROM                  = MakeId<    1,   64,   64,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SCROM                  = MakeId<    1,   64,  128,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SEROM                  = MakeId<    1,   32,   64,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SFROM                  = MakeId<    1,  256,   64,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SGROM                  = MakeId<    1,  256,    0,  0,  0, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SHROM                  = MakeId<    1,   32,  128,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SJROM                  = MakeId<    1,  256,   64,  8,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SKROM                  = MakeId<    1,  256,  128,  8,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SLROM                  = MakeId<    1,  256,  128,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SNROM                  = MakeId<    1,  256,    0,  8,  0, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SOROM                  = MakeId<    1,  256,    0,  8,  8, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SUROM                  = MakeId<    1,  512,    0,  8,  0, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SXROM                  = MakeId<    1,  512,    0, 32,  0, CRM_8,  NMT_H,  0 >::ID,