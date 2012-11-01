using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BizHawk
{
	internal class CompactGameInfo
	{
		public string Name;
		public string System;
		public string MetaData;
		public string Hash;
		public RomStatus Status;
	}

	public static class Database
	{
		private static Dictionary<string, CompactGameInfo> db = new Dictionary<string, CompactGameInfo>();

		static string RemoveHashType(string hash)
		{
			hash = hash.ToUpper();
			if (hash.StartsWith("MD5:")) hash = hash.Substring(4);
			if (hash.StartsWith("SHA1:")) hash = hash.Substring(5);
			return hash;
		}

		public static GameInfo CheckDatabase(string hash)
		{
			CompactGameInfo cgi;
			string hash_notype = RemoveHashType(hash);
			db.TryGetValue(hash_notype, out cgi);
			if (cgi == null)
			{
				Console.WriteLine("DB: hash " + hash + " not in game database.");
				return null;
			}
			return new GameInfo(cgi);
		}

		static void LoadDatabase_Escape(string line, string path)
		{
			if (!line.ToUpper().StartsWith("#INCLUDE")) return;
			line = line.Substring(8).TrimStart();
			string filename = Path.Combine(path, line);
			if (File.Exists(filename))
			{
				Console.WriteLine("loading external game database {0}", line);
				LoadDatabase(filename);
			}
			else
				Console.WriteLine("BENIGN: missing external game database {0}", line);
		}

		public static void LoadDatabase(string path)
		{
			using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
			{
				while (reader.EndOfStream == false)
				{
					string line = reader.ReadLine();
					try
					{
						if (line.StartsWith(";")) continue; //comment
						if (line.StartsWith("#"))
						{
							LoadDatabase_Escape(line, Path.GetDirectoryName(path));
							continue;
						}
						if (line.Trim().Length == 0) continue;
						string[] items = line.Split('\t');

						var Game = new CompactGameInfo();
						//remove a hash type identifier. well don't really need them for indexing (theyre just there for human purposes)
						Game.Hash = RemoveHashType(items[0].ToUpper());
						switch (items[1].Trim())
						{
							case "B": Game.Status = RomStatus.BadDump; break;
							case "V": Game.Status = RomStatus.BadDump; break;
							case "T": Game.Status = RomStatus.TranslatedRom; break;
							case "O": Game.Status = RomStatus.Overdump; break;
							case "I": Game.Status = RomStatus.BIOS; break;
							case "D": Game.Status = RomStatus.Homebrew; break;
							case "H": Game.Status = RomStatus.Hack; break;
							case "U": Game.Status = RomStatus.Unknown; break;
							default: Game.Status = RomStatus.GoodDump; break;
						}
						Game.Name = items[2];
						Game.System = items[3];
						Game.MetaData = items.Length >= 6 ? items[5] : null;

						if (db.ContainsKey(Game.Hash))
							Console.WriteLine("gamedb: Multiple hash entries {0}, duplicate detected on \"{1}\" and \"{2}\"", Game.Hash, Game.Name, db[Game.Hash].Name);

						db[Game.Hash] = Game;
					}
					catch
					{
						Console.WriteLine("Error parsing database entry: " + line);
					}
				}
			}
		}

		public static GameInfo GetGameInfo(byte[] RomData, string fileName)
		{
			CompactGameInfo cgi;
			string hash = string.Format("{0:X8}", CRC32.Calculate(RomData));
			if (db.TryGetValue(hash, out cgi))
				return new GameInfo(cgi);

			hash = Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(RomData));
			if (db.TryGetValue(hash, out cgi))
				return new GameInfo(cgi);

			hash = Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(RomData));
			if (db.TryGetValue(hash, out cgi))
				return new GameInfo(cgi);

			// rom is not in database. make some best-guesses
			var Game = new GameInfo { Hash = hash, Status = RomStatus.NotInDatabase, NotInDatabase = true };
			Console.WriteLine("Game was not in DB. CRC: {0:X8} MD5: {1}",
					CRC32.Calculate(RomData),
					Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(RomData)));

			string ext = Path.GetExtension(fileName).ToUpperInvariant();

			switch (ext)
			{
				case ".NES":
				case ".UNF":
				case ".FDS":
					Game.System = "NES";
					break;
				case ".SFC":
				case ".SMC": 
					Game.System = "SNES"; 
					break;
				case ".SMS": Game.System = "SMS"; break;
				case ".GG": Game.System = "GG"; break;
				case ".SG": Game.System = "SG"; break;
				case ".PCE": Game.System = "PCE"; break;
				case ".SGX": Game.System = "SGX"; break;
				case ".GBC": Game.System = "GBC"; break;
				case ".GB": Game.System = "GB"; break;

				case ".BIN":
				case ".GEN":
                case ".MD":
				case ".SMD": Game.System = "GEN"; break;
				case ".A26": Game.System = "A26"; break;
				case ".A78": Game.System = "A78"; break;
				case ".COL": Game.System = "COLV"; break;
				case ".ROM":
				case ".INT": Game.System = "INTV"; break;
				case ".PRG":
				case ".D64":
				case ".G64":
				case ".CRT":
					Game.System = "C64";
					break;
			}

			Game.Name = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');
			// If filename is all-caps, then attempt to proper-case the title.
			if (Game.Name == Game.Name.ToUpperInvariant())
				Game.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Game.Name.ToLower());

			return Game;
		}
	}
}
