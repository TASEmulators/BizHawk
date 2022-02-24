#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public static class Database
	{
		private static readonly Dictionary<string, CompactGameInfo> DB = new Dictionary<string, CompactGameInfo>();

		/// <summary>
		/// blocks until the DB is done loading
		/// </summary>
		private static readonly EventWaitHandle acquire = new EventWaitHandle(false, EventResetMode.ManualReset);

		private static string _bundledRoot = null;

		private static IList<string> _expected = null;

		private static string _userRoot = null;

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

		private static void LoadDatabase_Escape(string line, bool inUser, bool silent)
		{
			if (!line.StartsWith("#include", StringComparison.InvariantCultureIgnoreCase)) return;

			var isUserInclude = line.StartsWith("#includeuser", StringComparison.InvariantCultureIgnoreCase);
			var searchUser = inUser || isUserInclude;
			line = line.Substring(isUserInclude ? 12 : 8).TrimStart();
			var filename = Path.Combine(searchUser ? _userRoot : _bundledRoot, line);
			if (File.Exists(filename))
			{
				if (!silent) Util.DebugWriteLine($"loading external game database {line} ({(searchUser ? "user" : "bundled")})");
				initializeWork(filename, inUser: searchUser, silent: silent);
			}
			else if (inUser)
			{
				Util.DebugWriteLine($"BENIGN: missing external game database {line} (user)");
			}
			else if (!isUserInclude)
			{
				throw new FileNotFoundException($"missing external game database {line} (bundled)");
			}
		}

		public static void SaveDatabaseEntry(CompactGameInfo gameInfo, string filename = "gamedb_user.txt")
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

			File.AppendAllText(Path.Combine(_userRoot, filename), sb.ToString());
			DB[gameInfo.Hash] = gameInfo;
		}

		private static bool initialized = false;

		private static void initializeWork(string path, bool inUser, bool silent)
		{
			if (!inUser) _expected.Remove(Path.GetFileName(path));
			//reminder: this COULD be done on several threads, if it takes even longer
			using var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
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
						LoadDatabase_Escape(line, inUser: inUser, silent: silent);
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
					if (game.Hash is SHA1Checksum.EmptyFile or MD5Checksum.EmptyFile)
					{
						Console.WriteLine($"WARNING: gamedb {path} contains entry for empty rom as \"{game.Name}\"!");
					}
					if (!silent && DB.TryGetValue(game.Hash, out var dupe))
					{
						Console.WriteLine("gamedb: Multiple hash entries {0}, duplicate detected on \"{1}\" and \"{2}\"", game.Hash, game.Name, dupe.Name);
					}

					DB[game.Hash] = game;
				}
				catch (FileNotFoundException e) when (e.Message.Contains("missing external game database"))
				{
#if DEBUG
					throw;
#else
					Console.WriteLine(e.Message);
#endif
				}
				catch
				{
					Util.DebugWriteLine($"Error parsing database entry: {line}");
				}
			}

			acquire.Set();
		}

		public static void InitializeDatabase(string path, bool silent)
		{
			if (initialized) throw new InvalidOperationException("Did not expect re-initialize of game Database");
			initialized = true;

			_bundledRoot = Path.GetDirectoryName(path);
			_userRoot = Environment.GetEnvironmentVariable("BIZHAWK_DATA_HOME");
			if (!string.IsNullOrEmpty(_userRoot) && Directory.Exists(_userRoot)) _userRoot = Path.Combine(_userRoot, "gamedb");
			else _userRoot = _bundledRoot;
			Console.WriteLine($"user root: {_userRoot}");

			_expected = new DirectoryInfo(_bundledRoot!).EnumerateFiles("*.txt").Select(static fi => fi.Name).ToList();

			var stopwatch = Stopwatch.StartNew();
			ThreadPool.QueueUserWorkItem(_=> {
				initializeWork(path, inUser: false, silent: silent);
				if (_expected.Count is not 0) Util.DebugWriteLine($"extra bundled gamedb files were not #included: {string.Join(", ", _expected)}");
				Util.DebugWriteLine("GameDB load: " + stopwatch.Elapsed + " sec");
			});
		}

		public static GameInfo CheckDatabase(string hash)
		{
			acquire.WaitOne();

			var hashNoType = RemoveHashType(hash);
			DB.TryGetValue(hashNoType, out var cgi);
			if (cgi == null)
			{
				Console.WriteLine($"DB: hash {hash} not in game database.");
				return null;
			}

			return new GameInfo(cgi);
		}

		public static GameInfo GetGameInfo(byte[] romData, string fileName)
		{
			acquire.WaitOne();

			var hashSHA1 = SHA1Checksum.ComputeDigestHex(romData);
			if (DB.TryGetValue(hashSHA1, out var cgi))
			{
				return new GameInfo(cgi);
			}

			var hashMD5 = MD5Checksum.ComputeDigestHex(romData);
			if (DB.TryGetValue(hashMD5, out cgi))
			{
				return new GameInfo(cgi);
			}

			var hashCRC32 = CRC32Checksum.ComputeDigestHex(romData);
			if (DB.TryGetValue(hashCRC32, out cgi))
			{
				return new GameInfo(cgi);
			}

			// rom is not in database. make some best-guesses
			var game = new GameInfo
			{
				Hash = hashSHA1,
				Status = RomStatus.NotInDatabase,
				NotInDatabase = true
			};

			Console.WriteLine($"Game was not in DB. CRC: {hashCRC32} MD5: {hashMD5}");

			var ext = Path.GetExtension(fileName)?.ToUpperInvariant();

			switch (ext)
			{
				case ".NES":
				case ".UNF":
				case ".FDS":
					game.System = VSystemID.Raw.NES;
					break;

				case ".SFC":
				case ".SMC":
					game.System = VSystemID.Raw.SNES;
					break;

				case ".GB":
					game.System = VSystemID.Raw.GB;
					break;
				case ".GBC":
					game.System = VSystemID.Raw.GBC;
					break;
				case ".GBA":
					game.System = VSystemID.Raw.GBA;
					break;
				case ".NDS":
					game.System = VSystemID.Raw.NDS;
					break;

				case ".SMS":
					game.System = VSystemID.Raw.SMS;
					break;
				case ".GG":
					game.System = VSystemID.Raw.GG;
					break;
				case ".SG":
					game.System = VSystemID.Raw.SG;
					break;

				case ".GEN":
				case ".MD":
				case ".SMD":
					game.System = VSystemID.Raw.GEN;
					break;

				case ".PSF":
				case ".MINIPSF":
					game.System = VSystemID.Raw.PSX;
					break;

				case ".PCE":
					game.System = VSystemID.Raw.PCE;
					break;
				case ".SGX":
					game.System = VSystemID.Raw.SGX;
					break;

				case ".A26":
					game.System = VSystemID.Raw.A26;
					break;
				case ".A78":
					game.System = VSystemID.Raw.A78;
					break;

				case ".COL":
					game.System = VSystemID.Raw.Coleco;
					break;

				case ".INT":
					game.System = VSystemID.Raw.INTV;
					break;

				case ".PRG":
				case ".D64":
				case ".T64":
				case ".G64":
				case ".CRT":
					game.System = VSystemID.Raw.C64;
					break;

				case ".TZX":
				case ".PZX":
				case ".CSW":
				case ".WAV":
					game.System = VSystemID.Raw.ZXSpectrum;
					break;

				case ".CDT":
					game.System = VSystemID.Raw.AmstradCPC;
					break;

				case ".TAP":
					byte[] head = romData.Take(8).ToArray();
					game.System = Encoding.Default.GetString(head).Contains("C64-TAPE")
						? VSystemID.Raw.C64
						: VSystemID.Raw.ZXSpectrum;
					break;

				case ".Z64":
				case ".V64":
				case ".N64":
					game.System = VSystemID.Raw.N64;
					break;

				case ".DEBUG":
					game.System = VSystemID.Raw.DEBUG;
					break;

				case ".WS":
				case ".WSC":
					game.System = VSystemID.Raw.WSWAN;
					break;

				case ".LNX":
					game.System = VSystemID.Raw.Lynx;
					break;

				case ".83P":
					game.System = VSystemID.Raw.TI83;
					break;

				case ".DSK":
					var dId = new DskIdentifier(romData);
					game.System = dId.IdentifiedSystem;
					break;

				case ".PO":
				case ".DO":
					game.System = VSystemID.Raw.AppleII;
					break;

				case ".VB":
					game.System = VSystemID.Raw.VB;
					break;

				case ".NGP":
				case ".NGC":
					game.System = VSystemID.Raw.NGP;
					break;

				case ".O2":
					game.System = VSystemID.Raw.O2;
					break;

				case ".UZE":
					game.System = VSystemID.Raw.UZE;
					break;

				case ".32X":
					game.System = VSystemID.Raw.Sega32X;
					game.AddOption("32X", "true");
					break;

				case ".VEC":
					game.System = VSystemID.Raw.VEC;
					game.AddOption("VEC", "true");
					break;

				// refactor to use mame db (output of "mame -listxml" command)
				// there's no good definition for Arcade anymore, so we might limit to coin-based machines?
				case ".ZIP":
					game.System = VSystemID.Raw.MAME;
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
