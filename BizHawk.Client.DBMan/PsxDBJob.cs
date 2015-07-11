using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using BizHawk.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DBMan
{
	class PsxDBJob
	{
		class HashRecord
		{
			public string name, bizhash, datahash;
			public bool matched;
		}

		public void Run(string[] args)
		{
			string fpHash = null, fpRedump = null, fpOutfile = null;
			for (int i = 0; ; )
			{
				if (i == args.Length) break;
				var arg = args[i++];
				if (arg == "--hashes")
					fpHash = args[i++];
				if (arg == "--redump")
					fpRedump = args[i++];
				if (arg == "--outfile")
					fpOutfile = args[i++];
			}

			var hashes = new Dictionary<string, HashRecord>();

			Console.WriteLine("Loading redump data");
			RedumpPSX rdpsx = new RedumpPSX();
			rdpsx.Load(fpRedump);

			Console.WriteLine("Loading hash data");
			var splitSlashes = new string[]{"//"};
			foreach (var line in File.ReadAllLines(fpHash))
			{
				var parts = line.Split(splitSlashes, StringSplitOptions.None);
				var hr = new HashRecord()
				{
					name = parts[1],
					bizhash = parts[0].Substring(8, 8),
					datahash = parts[0].Substring(26, 8),
				};
				hashes[hr.datahash] = hr;
			}

			Console.WriteLine("merging");
			foreach (var rr in rdpsx.Records)
			{
				HashRecord hr;
				if (!hashes.TryGetValue(rr.crc, out hr))
					continue;
				hr.matched = true;
				//correct name to redump current
				hr.name = rr.name;
			}

			Console.WriteLine("writing results");
			using (var outf = new StreamWriter(fpOutfile))
			{
				foreach (var hr in hashes.Values)
				{
					if (!hr.matched)
						continue;
					outf.WriteLine("{0}\tG\t{1}\tPSX\t\tdh={2}", hr.bizhash, hr.name, hr.datahash);
				}
			}

		}
	}

	class RedumpPSX
	{
		public class RedumpRecord
		{
			public string name;
			public string crc;
		}

		public List<RedumpRecord> Records = new List<RedumpRecord>();

		public void Load(string datpath)
		{
			var xd = XDocument.Load(datpath);

			Dictionary<uint, string> knownHashes = new Dictionary<uint, string>();
			var games = xd.Root.Descendants("game").ToArray();
			for(int i=0;i<games.Length;i++)
			{
				var game = games[i];
				if (i % 100 == 0)
					Console.WriteLine("{0}/{1}", i, games.Length);

				var name = game.Attribute("name").Value;
				BizHawk.Emulation.DiscSystem.DiscHasher.SpecialCRC32 spec_crc_calc = new Emulation.DiscSystem.DiscHasher.SpecialCRC32();
				spec_crc_calc.Current = 0;
				foreach (var rom in game.Elements("rom"))
				{
					var ext = Path.GetExtension(rom.Attribute("name").Value).ToLower();
					if (ext == ".cue") continue;
					uint onecrc = uint.Parse(rom.Attribute("crc").Value, NumberStyles.HexNumber);
					int size = int.Parse(rom.Attribute("size").Value);
					spec_crc_calc.Incorporate(onecrc, size);
				}

				//Console.WriteLine("{0:X8}", spec_crc_calc.Current);
				Records.Add(new RedumpRecord()
				{
					name = name,
					crc = spec_crc_calc.Current.ToString("X8")
				});
			}
		}
	}
}