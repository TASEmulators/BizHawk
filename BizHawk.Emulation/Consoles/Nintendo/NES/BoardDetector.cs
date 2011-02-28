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
					line = line.Replace(parts[0],"");
					line = line.TrimStart('\t');
					Table[line] = parts[0];
				}
			}
//board		MAP	PRG	CHR
			static string ClassifyTable = @"
NROM	0	1	1
NROM	0	2	1
UNROM	2	8	0
UOROM	2	16	0
CNROM	3	2	2
CNROM	3	2	4
";

		}
	}
}