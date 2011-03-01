//http://nesdev.parodius.com/bbs/viewtopic.php?p=4571&sid=db4c7e35316cc5d734606dd02f11dccb

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;


namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		static class BoardDetector
		{
			public static string Detect(RomInfo romInfo)
			{
				string key = string.Format("{0}	{1}	{2}",romInfo.MapperNumber,romInfo.PRG_Size,romInfo.CHR_Size);
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
					if (parts.Length < 4) continue;
					line = line.Replace(parts[3],"");
					line = line.TrimEnd('\t');
					Table[line] = parts[3];
				}
			}
//MAP	PRG	CHR	BOARD
			static string ClassifyTable = @"
0	1	1	NROM
0	2	1	NROM
2	8	0	UNROM
2	16	0	UOROM
3	2	2	CNROM
3	2	4	CNROM
7	8	0	ANROM
7	16	0	AOROM
11	4	2	Discrete_74x377
11	2	4	Discrete_74x377
13	2	0	CPROM
66	4	2	GxROM
66	8	4	GxROM
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