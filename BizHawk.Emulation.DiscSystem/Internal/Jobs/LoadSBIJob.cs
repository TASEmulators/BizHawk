using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.DiscSystem.SBI
{
	/// <summary>
	/// Loads SBI files into an internal representation.
	/// </summary>
	class LoadSBIJob : DiscJob
	{
		/// <summary>
		/// The file to be loaded
		/// </summary>
		public string IN_Path;

		/// <summary>
		/// The resulting interpreted data
		/// </summary>
		public SubQPatchData OUT_Data;

		public void Run()
		{
			using (var fs = File.OpenRead(IN_Path))
			{
				BinaryReader br = new BinaryReader(fs);
				string sig = br.ReadStringFixedAscii(4);
				if (sig != "SBI\0")
					throw new SBIParseException("Missing magic number");

				SubQPatchData ret = new SubQPatchData();
				List<short> bytes = new List<short>();

				//read records until done
				for (; ; )
				{
					//graceful end
					if (fs.Position == fs.Length)
						break;

					if (fs.Position + 4 > fs.Length) throw new SBIParseException("Broken record");
					var m = BCD2.BCDToInt(br.ReadByte());
					var s = BCD2.BCDToInt(br.ReadByte());
					var f = BCD2.BCDToInt(br.ReadByte());
					var ts = new Timestamp(m, s, f);
					ret.ABAs.Add(ts.Sector);
					int type = br.ReadByte();
					switch (type)
					{
						case 1: //Q0..Q9
							if (fs.Position + 10 > fs.Length) throw new SBIParseException("Broken record");
							for (int i = 0; i <= 9; i++) bytes.Add(br.ReadByte());
							for (int i = 10; i <= 11; i++) bytes.Add(-1);
							break;
						case 2: //Q3..Q5
							if (fs.Position + 3 > fs.Length) throw new SBIParseException("Broken record");
							for (int i = 0; i <= 2; i++) bytes.Add(-1);
							for (int i = 3; i <= 5; i++) bytes.Add(br.ReadByte());
							for (int i = 6; i <= 11; i++) bytes.Add(-1);
							break;
						case 3: //Q7..Q9
							if (fs.Position + 3 > fs.Length) throw new SBIParseException("Broken record");
							for (int i = 0; i <= 6; i++) bytes.Add(-1);
							for (int i = 7; i <= 9; i++) bytes.Add(br.ReadByte());
							for (int i = 10; i <= 11; i++) bytes.Add(-1);
							break;
						default:
							throw new SBIParseException("Broken record");
					}
				}

				ret.subq = bytes.ToArray();

				OUT_Data = ret;
			}
		}
	}
}