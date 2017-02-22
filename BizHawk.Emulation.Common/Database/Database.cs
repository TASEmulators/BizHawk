using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.Common
{
	public class CompactGameInfo
	{
		public string Name { get; set; }
		public string System { get; set; }
		public string MetaData { get; set; }
		public string Hash { get; set; }
		public string Region { get; set; }
		public RomStatus Status { get; set; }
		public string ForcedCore { get; set; }
	}

	public static class Database
	{
		private static readonly Dictionary<string, CompactGameInfo> db = new Dictionary<string, CompactGameInfo>();

		private static string RemoveHashType(string hash)
		{
			hash = hash.ToUpper();
			if (hash.StartsWith("MD5:"))
			{
				hash = hash.Substring(4);
			}

			if (hash.StartsWith("SHA1:"))
			{
				hash = hash.Substring(5);
			}

			return hash;
		}

		public static GameInfo CheckDatabase(string hash)
		{
			CompactGameInfo cgi;
			var hash_notype = RemoveHashType(hash);
			db.TryGetValue(hash_notype, out cgi);
			if (cgi == null)
			{
				Console.WriteLine("DB: hash " + hash + " not in game database.");
				return null;
			}

			return new GameInfo(cgi);
		}

		private static void LoadDatabase_Escape(string line, string path)
		{
			if (!line.ToUpperInvariant().StartsWith("#INCLUDE"))
			{
				return;
			}

			line = line.Substring(8).TrimStart();
			var filename = Path.Combine(path, line);
			if (File.Exists(filename))
			{
				Console.WriteLine("loading external game database {0}", line);
				LoadDatabase(filename);
			}
			else
			{
				Console.WriteLine("BENIGN: missing external game database {0}", line);
			}
		}

		public static void SaveDatabaseEntry(string path, CompactGameInfo gameInfo)
		{
			var sb = new StringBuilder();
			sb
				.Append("sha1:") // TODO: how do we know it is sha1?
				.Append(gameInfo.Hash)
				.Append('\t');

			switch (gameInfo.Status)
			{
				case RomStatus.BadDump:
					sb.Append("B");
					break;
				case RomStatus.TranslatedRom:
					sb.Append("T");
					break;
				case RomStatus.Overdump:
					sb.Append("O");
					break;
				case RomStatus.BIOS:
					sb.Append("I");
					break;
				case RomStatus.Homebrew:
					sb.Append("D");
					break;
				case RomStatus.Hack:
					sb.Append("H");
					break;
				case RomStatus.Unknown:
					sb.Append("U");
					break;
			}

			sb
				.Append('\t')
				.Append(gameInfo.Name)
				.Append('\t')
				.Append(gameInfo.System)
				.Append('\t')
				.Append(gameInfo.MetaData)
				.Append(Environment.NewLine);
			try
			{
				File.AppendAllText(path, sb.ToString());
			}
			catch (Exception ex)
			{
				string blah = ex.ToString();
			}
		}

		public static void LoadDatabase(string path)
		{
			using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
			{
				while (reader.EndOfStream == false)
				{
					var line = reader.ReadLine() ?? string.Empty;
					try
					{
						if (line.StartsWith(";")) 
						{
							continue; // comment
						}

						if (line.StartsWith("#"))
						{
							LoadDatabase_Escape(line, Path.GetDirectoryName(path));
							continue;
						}

						if (line.Trim().Length == 0)
						{
							continue;
						}

						var items = line.Split('\t');

						var game = new CompactGameInfo
						{
							Hash = RemoveHashType(items[0].ToUpper())
						};

						// remove a hash type identifier. well don't really need them for indexing (theyre just there for human purposes)
						switch (items[1].Trim())
						{
							case "B":
								game.Status = RomStatus.BadDump;
								break;
							case "V":
								game.Status = RomStatus.BadDump;
								break;
							case "T":
								game.Status = RomStatus.TranslatedRom;
								break;
							case "O":
								game.Status = RomStatus.Overdump;
								break;
							case "I":
								game.Status = RomStatus.BIOS;
								break;
							case "D":
								game.Status = RomStatus.Homebrew;
								break;
							case "H":
								game.Status = RomStatus.Hack;
								break;
							case "U":
								game.Status = RomStatus.Unknown;
								break;
							default:
								game.Status = RomStatus.GoodDump;
								break;
						}

						game.Name = items[2];
						game.System = items[3];
						game.MetaData = items.Length >= 6 ? items[5] : null;
						game.Region = items.Length >= 7 ? items[6] : string.Empty;
						game.ForcedCore = items.Length >= 8 ? items[7].ToLowerInvariant() : string.Empty;

						if (db.ContainsKey(game.Hash))
						{
							Console.WriteLine("gamedb: Multiple hash entries {0}, duplicate detected on \"{1}\" and \"{2}\"", game.Hash, game.Name, db[game.Hash].Name);
						}

						db[game.Hash] = game;
					}
					catch
					{
						Console.WriteLine("Error parsing database entry: " + line);
					}
				}
			}
		}

		public static GameInfo GetGameInfo(byte[] romData, string fileName)
		{
			CompactGameInfo cgi;
			var hash = string.Format("{0:X8}", CRC32.Calculate(romData));
			if (db.TryGetValue(hash, out cgi))
			{
				return new GameInfo(cgi);
			}

			hash = romData.HashMD5();
			if (db.TryGetValue(hash, out cgi))
			{
				return new GameInfo(cgi);
			}

			hash = romData.HashSHA1();
			if (db.TryGetValue(hash, out cgi))
			{
				return new GameInfo(cgi);
			}

			// rom is not in database. make some best-guesses
			var game = new GameInfo
			{
				Hash = hash,
				Status = RomStatus.NotInDatabase,
				NotInDatabase = true
			};

			Console.WriteLine(
				"Game was not in DB. CRC: {0:X8} MD5: {1}",
				CRC32.Calculate(romData),
				System.Security.Cryptography.MD5.Create().ComputeHash(romData).BytesToHexString());

			var ext = Path.GetExtension(fileName).ToUpperInvariant();

			switch (ext)
			{
				case ".NES":
				case ".UNF":
				case ".FDS":
					game.System = "NES";
					break;

				case ".SFC":
				case ".SMC": 
					game.System = "SNES"; 
					break;

				case ".GB":
					game.System = "GB";
					break;
				case ".GBC":
					game.System = "GBC";
					break;
				case ".GBA":
					game.System = "GBA";
					break;

				case ".SMS":
					game.System = "SMS";
					break;
				case ".GG":
					game.System = "GG";
					break;
				case ".SG":
					game.System = "SG";
					break;

				case ".GEN":
				case ".MD":
				case ".SMD":
					game.System = "GEN";
					break;

				case ".PSF":
				case ".MINIPSF":
					game.System = "PSX";
					break;

				case ".PCE":
					game.System = "PCE";
					break;
				case ".SGX":
					game.System = "SGX";
					break;

				case ".A26":
					game.System = "A26";
					break;
				case ".A78":
					game.System = "A78";
					break;

				case ".COL":
					game.System = "Coleco";
					break;

				case ".INT":
					game.System = "INTV";
					break;

				case ".PRG":
				case ".D64":
				case ".T64":
				case ".G64":
				case ".CRT":
				case ".TAP":
					game.System = "C64";
					break;

				case ".Z64":
				case ".V64":
				case ".N64":
					game.System = "N64";
					break;

				case ".DEBUG":
					game.System = "DEBUG";
					break;

				case ".WS":
				case ".WSC":
					game.System = "WSWAN";
					break;

				case ".LNX":
					game.System = "Lynx";
					break;

				case ".83P":
					game.System = "83P";
					break;

				case ".DSK":
				case ".PO":
				case ".DO":
					game.System = "AppleII";
					break;
			}

			game.Name = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');

			// If filename is all-caps, then attempt to proper-case the title.
			if (game.Name == game.Name.ToUpperInvariant())
			{
				game.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(game.Name.ToLower());
			}

			return game;
		}
	}
}
