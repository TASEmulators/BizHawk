using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using BizHawk.API.ApiHawk;
using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.Common
{
	public static class Database
	{
		private static readonly Dictionary<string, CompactGameInfo> DB = new Dictionary<string, CompactGameInfo>();

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
			var hashNoType = RemoveHashType(hash);
			DB.TryGetValue(hashNoType, out var cgi);
			if (cgi == null)
			{
				Console.WriteLine($"DB: hash {hash} not in game database.");
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
				Debug.WriteLine("loading external game database {0}", line);
				LoadDatabase(filename);
			}
			else
			{
				Debug.WriteLine("BENIGN: missing external game database {0}", line);
			}
		}

		public static void SaveDatabaseEntry(string path, CompactGameInfo gameInfo)
		{
			var sb = new StringBuilder();
			sb
				.Append("sha1:") // TODO: how do we know it is sha1?
				.Append(gameInfo.Hash)
				.Append('\t');

			sb.Append(gameInfo.Status switch
			{
				RomStatus.BadDump => "B",
				RomStatus.TranslatedRom => "T",
				RomStatus.Overdump => "O",
				RomStatus.Bios => "I",
				RomStatus.Homebrew => "D",
				RomStatus.Hack => "H",
				RomStatus.NotInDatabase => "U",
				RomStatus.Unknown => "U",
				_ => ""
			});

			sb
				.Append('\t')
				.Append(gameInfo.Name)
				.Append('\t')
				.Append(gameInfo.System)
				.Append('\t')
				.Append(gameInfo.MetaData)
				.Append(Environment.NewLine);

			File.AppendAllText(path, sb.ToString());
		}

		public static void LoadDatabase(string path)
		{
			using var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			while (reader.EndOfStream == false)
			{
				var line = reader.ReadLine() ?? "";
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
						Hash = RemoveHashType(items[0].ToUpper()),
						// remove a hash type identifier. well don't really need them for indexing (they're just there for human purposes)
						Status = items[1].Trim()
							switch
							{
								"B" => RomStatus.BadDump,
								"V" => RomStatus.BadDump,
								"T" => RomStatus.TranslatedRom,
								"O" => RomStatus.Overdump,
								"I" => RomStatus.Bios,
								"D" => RomStatus.Homebrew,
								"H" => RomStatus.Hack,
								"U" => RomStatus.Unknown,
								_ => RomStatus.GoodDump
							},
						Name = items[2],
						System = items[3],
						MetaData = items.Length >= 6 ? items[5] : null,
						Region = items.Length >= 7 ? items[6] : "",
						ForcedCore = items.Length >= 8 ? items[7].ToLowerInvariant() : ""
					};

#if DEBUG
					if (DB.ContainsKey(game.Hash))
					{
						Console.WriteLine("gamedb: Multiple hash entries {0}, duplicate detected on \"{1}\" and \"{2}\"", game.Hash, game.Name, DB[game.Hash].Name);
					}
#endif

					DB[game.Hash] = game;
				}
				catch
				{
					Debug.WriteLine($"Error parsing database entry: {line}");
				}
			}
		}

		public static GameInfo GetGameInfo(byte[] romData, string fileName)
		{
			var hash = $"{CRC32.Calculate(romData):X8}";
			if (DB.TryGetValue(hash, out var cgi))
			{
				return new GameInfo(cgi);
			}

			hash = romData.HashMD5();
			if (DB.TryGetValue(hash, out cgi))
			{
				return new GameInfo(cgi);
			}

			hash = romData.HashSHA1();
			if (DB.TryGetValue(hash, out cgi))
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

			var ext = Path.GetExtension(fileName)?.ToUpperInvariant();

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
				case ".NDS":
					game.System = "NDS";
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
					game.System = "C64";
					break;

				case ".TZX":
				case ".PZX":
				case ".CSW":
				case ".WAV":
					game.System = "ZXSpectrum";
					break;

				case ".CDT":
					game.System = "AmstradCPC";
					break;

				case ".TAP":
					byte[] head = romData.Take(8).ToArray();
					game.System = Encoding.Default.GetString(head).Contains("C64-TAPE")
						? "C64"
						: "ZXSpectrum";
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
					var dId = new DskIdentifier(romData);
					game.System = dId.IdentifiedSystem;
					break;

				case ".PO":
				case ".DO":
					game.System = "AppleII";
					break;

				case ".VB":
					game.System = "VB";
					break;

				case ".NGP":
				case ".NGC":
					game.System = "NGP";
					break;

				case ".O2":
					game.System = "O2";
					break;

				case ".UZE":
					game.System = "UZE";
					break;

				case ".32X":
					game.System = "32X";
					game.AddOption("32X", "true");
					break;

				case ".VEC":
					game.System = "VEC";
					game.AddOption("VEC", "true");
					break;

				// refactor to use mame db (output of "mame -listxml" command)
				// there's no good definition for Arcade anymore, so we might limit to coin-based machines?
				case ".ZIP":
					game.System = "Arcade";
					break;
			}

			game.Name = Path.GetFileNameWithoutExtension(fileName)?.Replace('_', ' ');

			// If filename is all-caps, then attempt to proper-case the title.
			if (!string.IsNullOrWhiteSpace(game.Name) && game.Name == game.Name.ToUpperInvariant())
			{
				game.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(game.Name.ToLower());
			}

			return game;
		}
	}

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
}
