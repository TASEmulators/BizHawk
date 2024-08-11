#nullable disable

using System.Collections.Generic;
using System.IO;
using Community.CsharpSqlite.SQLiteClient;

using BizHawk.Common.StringExtensions;

namespace BizHawk.DBManTool
{
	public static class DirectoryScan
	{
		public static List<InitialRomInfo> GetRomInfos(string path)
		{
			var dirInfo = new DirectoryInfo(path);
			var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
			var romInfos = new List<InitialRomInfo>();

			foreach (var f in files)
			{
				if (!IsRomFile(f.Extension, f.Length))
					continue;
				romInfos.Add(RomHasher.Generate(f.FullName));
			}

			return romInfos;
		}

		const long BiggestBinToHash = 16 * 1024 * 1024;

		public static bool IsRomFile(string ext, long size)
		{
			if (string.IsNullOrEmpty(ext) || ext.Length <= 1)
				return false;

			ext = ext.Substring(1).ToLowerInvariant();
			if (ext.In("cue", "iso", "nes", "unf", "fds", "sfc", "smc", "sms", "gg", "sg", "pce", "sgx", "gb", "gbc", "gba", "gen", "md", "smd", "a26", "a78", "col", "z64", "v64", "n64"))
				return true;

			// the logic here is related to cue/bin cd images.
			// when we see a .cue file, we will hash the cd image including the bin.
			// so we don't really want to hash the whole bin a second time.
			// however, there are also non-cdimage roms with BIN extension.
			// hopefully this differentiates them. It may have to be tweaked as systems are added.

			if (ext == "bin" && size < BiggestBinToHash)
				return true;

			return false;
		}

		// ========================================================================================

		public static void MergeRomInfosWithDatabase(IList<InitialRomInfo> roms)
		{
			
			foreach (var rom in roms)
			{
				if (!RomInDatabase(rom.MD5))
				{
					InsertRom(rom);

					if (!GameInDatabase(rom))
						InsertGame(rom);
				}
			}
		}

		static bool RomInDatabase(string md5)
		{
			using (var cmd = DB.Con.CreateCommand())
			{
				cmd.CommandText = "SELECT rom_id FROM rom WHERE md5 = @md5";
				cmd.Parameters.Add(new SqliteParameter("@md5", md5));
				var result = cmd.ExecuteScalar();
				return result != null;
			}
		}

		static bool GameInDatabase(InitialRomInfo rom)
		{
			using (var cmd = DB.Con.CreateCommand())
			{
				cmd.CommandText = "SELECT game_id FROM game WHERE system = @System and name = @Name";
				cmd.Parameters.Add(new SqliteParameter("@System", rom.GuessedSystem));
				cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
				var result = cmd.ExecuteScalar();
				return result != null;
			}
		}

		static void InsertRom(InitialRomInfo rom)
		{
			using (var cmd = DB.Con.CreateCommand())
			{
				cmd.CommandText = 
					"INSERT INTO rom (crc32, md5, sha1, size, system, name, region, version_tags, created_date) "+
					"VALUES (@crc32, @md5, @sha1, @size, @System, @Name, @Region, @VersionTags, datetime('now','localtime'))";
				cmd.Parameters.Add(new SqliteParameter("@crc32", rom.CRC32));
				cmd.Parameters.Add(new SqliteParameter("@md5", rom.MD5));
				cmd.Parameters.Add(new SqliteParameter("@sha1", rom.SHA1));
				cmd.Parameters.Add(new SqliteParameter("@size", rom.Size));
				cmd.Parameters.Add(new SqliteParameter("@System", rom.GuessedSystem));
				cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
				cmd.Parameters.Add(new SqliteParameter("@Region", rom.GuessedRegion));
				cmd.Parameters.Add(new SqliteParameter("@VersionTags", rom.VersionTags));
				cmd.ExecuteNonQuery();
			}
		}

		static void InsertGame(InitialRomInfo rom)
		{
			using (var cmd = DB.Con.CreateCommand())
			{
				cmd.CommandText = "INSERT INTO game (system, name, created_date) VALUES (@System, @Name, datetime('now','localtime'))";
				cmd.Parameters.Add(new SqliteParameter("@System", rom.GuessedSystem));
				cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
				cmd.ExecuteNonQuery();
			}
		}
	}
}
