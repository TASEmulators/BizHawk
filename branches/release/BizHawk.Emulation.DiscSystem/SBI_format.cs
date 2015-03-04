using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	public class SBI_Format
	{
		public class SBIParseException : Exception
		{
			public SBIParseException(string message) : base(message) { }
		}

		public class SBIFile
		{
			/// <summary>
			/// a list of patched ABAs
			/// </summary>
			public List<int> ABAs = new List<int>();

			/// <summary>
			/// 12 values (Q subchannel data) for every patched ABA; -1 means unpatched
			/// </summary>
			public short[] subq;
		}

		/// <summary>
		/// Does a cursory check to see if the file looks like an SBI
		/// </summary>
		public static bool QuickCheckISSBI(string path)
		{
			using (var fs = File.OpenRead(path))
			{
				BinaryReader br = new BinaryReader(fs);
				string sig = br.ReadStringFixedAscii(4);
				if (sig != "SBI\0")
					return false;
			}
			return true;
		}

		/// <summary>
		/// Loads an SBI file from the specified path
		/// </summary>
		public SBIFile LoadSBIPath(string path)
		{
			using(var fs = File.OpenRead(path))
			{
				BinaryReader br = new BinaryReader(fs);
				string sig = br.ReadStringFixedAscii(4);
				if (sig != "SBI\0")
					throw new SBIParseException("Missing magic number");

				SBIFile ret = new SBIFile();
				List<short> bytes = new List<short>();

				//read records until done
				for (; ; )
				{
					//graceful end
					if (fs.Position == fs.Length)
						break;

					if (fs.Position+4 > fs.Length) throw new SBIParseException("Broken record");
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
				return ret;

			}
		}
	}
}