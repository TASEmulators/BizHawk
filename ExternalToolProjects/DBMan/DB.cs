#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common.StringExtensions;
using Community.CsharpSqlite.SQLiteClient;

namespace BizHawk.DBManTool
{
	public class Rom
	{
		public long RomId;
		public string CRC32;
		public string MD5;
		public string SHA1;
		public string System;
		public string Name;
		public string Region;
		public string VersionTags;
		public string RomMetadata;
		public string RomStatus;
		public string Catalog;
		public long Size;
		public DateTime Created;
		public DateTime Modified;

		public override string ToString() { return Name + " " + VersionTags; }
		public Game Game;
		public string CombinedMetaData
		{
			get
			{
				if (Game == null) return RomMetadata;
				if (Game.GameMetadata == null) return RomMetadata;
				if (RomMetadata == null) return Game.GameMetadata;
				return Game.GameMetadata + ";" + RomMetadata;
			}
		}
		
		public string SizeFriendly => $"{Size} bytes ({Size >> 10}k)";
		public bool New { get { return (Created > Modified); } }

		public string NameWithTheFlipped
		{
			get
			{
				if (!Name.EndsWithOrdinal(", The")) return Name;
				return "The "+Name.Substring(0, Name.Length-5);
			}
		}

		public string SortString()
		{
			var ret = "";
			if (Game.Classification == "Firmware") ret += "[BIOS] ";
			if (Game.Classification == "Homebrew") ret += "[Homebrew] ";
			if (Game.Classification == "Test Rom") ret += "[Test ROM] ";
			ret += Name;
			ret += "|" + RegionRank + "|" + VersionTags;
			return ret;
		}

		public string DisplayName
		{
			get
			{
				var ret = "";
				if (Game.Classification == "Firmware") ret += "[BIOS] ";
				if (Game.Classification == "Homebrew") ret += "[Homebrew] ";
				if (Game.Classification == "Test Rom") ret += "[Test ROM] ";
				ret += Name;
				return ret;
			}
		}

		public int RegionRank
		{
			get
			{
				if (string.IsNullOrEmpty(Region)) return 99;
				if (Region.Contains("World")) return 1;
				if (Region.Contains("USA")) return 2;
				if (Region.Contains("Europe")) return 3;
				if (Region.Contains("Japan")) return 4;
				return Region.GetHashCode();
			}
		}
	}

	public class Game
	{
		public long GameId;
		public string System;
		public string Name;
		public string Developer;
		public string Publisher;
		public string Classification;
		public string ReleaseDate;
		public string Players;
		public string GameMetadata;
		public string Tags;
		public string AltNames;
		public string Notes;
		public DateTime Created;
		public DateTime Modified;

		public override string ToString() { return Name; }
	}

	public static class DB
	{
		public static List<Rom> Roms { get; private set; } = new List<Rom>();
		public static List<Game> Games { get; } = new List<Game>();
		public static Dictionary<string, Game> GameMap { get; } = new Dictionary<string, Game>();

		public static SqliteConnection Con { get; set; }

		public static void LoadDbForSystem(string system)
		{
			Games.Clear();
			Roms.Clear();

			LoadGames(system);
			LoadRoms(system);
		}

		static void LoadGames(string system)
		{
			var cmd = Con.CreateCommand();
			cmd.CommandText = 
				"SELECT game_id, system, name, developer, publisher, classification, release_date, players, game_metadata, tags, alternate_names, notes "+
				"FROM game "+
				"WHERE system = @System";
			cmd.Parameters.Add(new SqliteParameter("@System", system));
			var reader = cmd.ExecuteReader();
			while (reader.NextResult())
			{
				var game = new Game();
				game.GameId = reader.GetInt64(0);
				game.System = reader.GetString(1);
				game.Name = reader.GetString(2);
				game.Developer = reader.GetString(3);
				game.Publisher = reader.GetString(4);
				game.Classification = reader.GetString(5);
				game.ReleaseDate = reader.GetString(6);
				game.Players = reader.GetString(7);
				game.GameMetadata = reader.GetString(8);
				game.Tags = reader.GetString(9);
				game.AltNames = reader.GetString(10);
				game.Notes = reader.GetString(11);
				Games.Add(game);
				GameMap[game.Name] = game;
			}
			reader.Dispose();
			cmd.Dispose();
		}

		static void LoadRoms(string system)
		{
			var cmd = Con.CreateCommand();
			cmd.CommandText =
				"SELECT rom_id, crc32, md5, sha1, system, name, region, version_tags, rom_metadata, rom_status, catalog, size, created_date, modified_date " +
				"FROM rom " +
				"WHERE system = @System";
			cmd.Parameters.Add(new SqliteParameter("@System", system));
			var reader = cmd.ExecuteReader();
			while (reader.NextResult())
			{
				var rom = new Rom();
				rom.RomId = reader.GetInt64(0);
				rom.CRC32 = reader.GetString(1);
				rom.MD5 = reader.GetString(2);
				rom.SHA1 = reader.GetString(3);
				rom.System = reader.GetString(4);
				rom.Name = reader.GetString(5);
				rom.Region = reader.GetString(6);
				rom.VersionTags = reader.GetString(7);
				rom.RomMetadata = reader.GetString(8);
				rom.RomStatus = reader.GetString(9);
				rom.Catalog = reader.GetString(10);
				rom.Size = reader.GetInt64(11);
				rom.Created = reader.GetDateTime(12);
				rom.Modified = reader.GetDateTime(13);
				rom.Game = GameMap[rom.Name];
				Roms.Add(rom);
			}
			reader.Dispose();
			cmd.Dispose();
			Roms = Roms.OrderBy(x=>x.SortString()).ToList();
		}

		public static void SaveRom(Rom rom)
		{
			var cmd = Con.CreateCommand();
			cmd.CommandText = 
				"UPDATE rom SET "+
				"region=@Region, "+
				"version_tags=@VersionTags, "+
				"rom_metadata=@RomMetadata, "+
				"rom_status=@RomStatus, "+
				"catalog=@Catalog, " +
				"modified_date=datetime('now','localtime') " +
				"WHERE rom_id=@RomId";
			cmd.Parameters.Add(new SqliteParameter("@Region", rom.Region));
			cmd.Parameters.Add(new SqliteParameter("@VersionTags", rom.VersionTags));
			cmd.Parameters.Add(new SqliteParameter("@RomMetadata", rom.RomMetadata));
			cmd.Parameters.Add(new SqliteParameter("@RomStatus", rom.RomStatus));
			cmd.Parameters.Add(new SqliteParameter("@Catalog", rom.Catalog));
			cmd.Parameters.Add(new SqliteParameter("@RomId", rom.RomId));
			cmd.ExecuteNonQuery();
			cmd.Dispose();

			cmd = Con.CreateCommand();
			cmd.CommandText = 
				"UPDATE game SET "+
				"developer=@Developer, "+
				"publisher=@Publisher, "+
				"classification=@Classification, "+
				"release_date=@ReleaseDate, "+
				"players=@Players, "+				
				"game_metadata=@GameMetadata, "+
				"tags=@Tags, "+
				"alternate_names=@AltNames, "+
				"notes=@Notes, "+
				"modified_date=datetime('now','localtime') " +
				"WHERE game_id=@GameId";
			cmd.Parameters.Add(new SqliteParameter("@Developer", rom.Game.Developer));
			cmd.Parameters.Add(new SqliteParameter("@Publisher", rom.Game.Publisher));
			cmd.Parameters.Add(new SqliteParameter("@Classification", rom.Game.Classification));
			cmd.Parameters.Add(new SqliteParameter("@ReleaseDate", rom.Game.ReleaseDate));
			cmd.Parameters.Add(new SqliteParameter("@Players", rom.Game.Players));			
			cmd.Parameters.Add(new SqliteParameter("@GameMetadata", rom.Game.GameMetadata));
			cmd.Parameters.Add(new SqliteParameter("@Tags", rom.Game.Tags));
			cmd.Parameters.Add(new SqliteParameter("@AltNames", rom.Game.AltNames));
			cmd.Parameters.Add(new SqliteParameter("@Notes", rom.Game.Notes));
			cmd.Parameters.Add(new SqliteParameter("@GameId", rom.Game.GameId));
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public static void SaveRom1(Rom rom, string origSystem, string origName)
		{
			// Does this game already exist?
			var game = GameExists(rom.System, rom.Name);
			if (game != null) // existing game with this system/name already exists. we'll merge to it. (existing game wins)
			{
				DeleteGame(origSystem, origName);
				rom.Game = game;
			}
			
			var cmd = Con.CreateCommand();
			cmd.CommandText =
				"UPDATE rom SET " +
				"system=@System, " +
				"name=@Name " +
				"WHERE system=@OrigSystem and name=@OrigName";
			cmd.Parameters.Add(new SqliteParameter("@System", rom.System));
			cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
			cmd.Parameters.Add(new SqliteParameter("@OrigSystem", origSystem));
			cmd.Parameters.Add(new SqliteParameter("@OrigName", origName));
			cmd.ExecuteNonQuery();
			cmd.Dispose();

			cmd = Con.CreateCommand();
			cmd.CommandText =
				"UPDATE game SET " +
				"system=@System, " +
				"name=@Name " +
				"WHERE system=@OrigSystem and name=@OrigName";
			cmd.Parameters.Add(new SqliteParameter("@System", rom.System));
			cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
			cmd.Parameters.Add(new SqliteParameter("@OrigSystem", origSystem));
			cmd.Parameters.Add(new SqliteParameter("@OrigName", origName));
			cmd.ExecuteNonQuery();
			cmd.Dispose();

			SaveRom(rom);
		}

		public static void SaveRom2(Rom rom)
		{
			var cmd = Con.CreateCommand();
			cmd.CommandText =
				"UPDATE rom SET " +
				"system=@System, "+
				"name=@Name, "+
				"region=@Region, " +
				"version_tags=@VersionTags, " +
				"rom_metadata=@RomMetadata, " +
				"rom_status=@RomStatus, " +
				"catalog=@Catalog, " +
				"modified_date=datetime('now','localtime') " +
				"WHERE rom_id=@RomId";
			cmd.Parameters.Add(new SqliteParameter("@System", rom.System));
			cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
			cmd.Parameters.Add(new SqliteParameter("@Region", rom.Region));
			cmd.Parameters.Add(new SqliteParameter("@VersionTags", rom.VersionTags));
			cmd.Parameters.Add(new SqliteParameter("@RomMetadata", rom.RomMetadata));
			cmd.Parameters.Add(new SqliteParameter("@RomStatus", rom.RomStatus));
			cmd.Parameters.Add(new SqliteParameter("@Catalog", rom.Catalog));
			cmd.Parameters.Add(new SqliteParameter("@RomId", rom.RomId));
			cmd.ExecuteNonQuery();
			cmd.Dispose();

			bool gameAlreadyExists = false;
			cmd = Con.CreateCommand();
			cmd.CommandText = "SELECT game_id FROM game WHERE system=@System and name=@Name";
			cmd.Parameters.Add(new SqliteParameter("@System", rom.System));
			cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
			gameAlreadyExists = cmd.ExecuteScalar() != null;
			cmd.Dispose();

			if (!gameAlreadyExists)
			{
				cmd = Con.CreateCommand();
				cmd.CommandText = "INSERT INTO game (system, name, created_date) values (@System, @Name, datetime('now','localtime'))";
				cmd.Parameters.Add(new SqliteParameter("@System", rom.System));
				cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
				cmd.ExecuteNonQuery();
				cmd.Dispose();
			}

			cmd = Con.CreateCommand();
			cmd.CommandText =
				"UPDATE game SET " +
				"developer=@Developer, " +
				"publisher=@Publisher, " +
				"classification=@Classification, " +
				"release_date=@ReleaseDate, " +
				"players=@Players, " +
				"game_metadata=@GameMetadata, " +
				"tags=@Tags, " +
				"alternate_names=@AltNames, " +
				"notes=@Notes, " +
				"modified_date=datetime('now','localtime') " +
				"WHERE system=@System and name=@Name";
			cmd.Parameters.Add(new SqliteParameter("@Developer", rom.Game.Developer));
			cmd.Parameters.Add(new SqliteParameter("@Publisher", rom.Game.Publisher));
			cmd.Parameters.Add(new SqliteParameter("@Classification", rom.Game.Classification));
			cmd.Parameters.Add(new SqliteParameter("@ReleaseDate", rom.Game.ReleaseDate));
			cmd.Parameters.Add(new SqliteParameter("@Players", rom.Game.Players));
			cmd.Parameters.Add(new SqliteParameter("@GameMetadata", rom.Game.GameMetadata));
			cmd.Parameters.Add(new SqliteParameter("@Tags", rom.Game.Tags));
			cmd.Parameters.Add(new SqliteParameter("@System", rom.System));
			cmd.Parameters.Add(new SqliteParameter("@Name", rom.Name));
			cmd.Parameters.Add(new SqliteParameter("@AltNames", rom.Game.AltNames));
			cmd.Parameters.Add(new SqliteParameter("@Notes", rom.Game.Notes));
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public static void Cleanup()
		{
			var orphanedGameList = new List<Tuple<string, string>>();
			
			var cmd = Con.CreateCommand();
			cmd.CommandText = 
				"SELECT system, name FROM game "+
				"EXCEPT "+
				"SELECT system, name FROM rom";
			var reader = cmd.ExecuteReader();
			while (reader.NextResult())
			{
				string system = reader.GetString(0);
				string name = reader.GetString(1);
				orphanedGameList.Add(new Tuple<string, string>(system, name));
			}
			reader.Dispose();
			cmd.Dispose();

			cmd = Con.CreateCommand();
			cmd.CommandText = "DELETE FROM game WHERE system=@System and name=@Name";
			foreach (var orphanedGame in orphanedGameList)
			{
				cmd.Parameters.Clear();
				cmd.Parameters.Add(new SqliteParameter("@System", orphanedGame.Item1));
				cmd.Parameters.Add(new SqliteParameter("@Name", orphanedGame.Item2));
				cmd.ExecuteNonQuery();
			}
			cmd.Dispose();

			cmd = Con.CreateCommand();
			cmd.CommandText = "VACUUM";
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public static List<string> GetDeveloperPublisherNames()
		{
			var names = new List<string>();

			var cmd = Con.CreateCommand();
			cmd.CommandText =
				"SELECT DISTINCT developer FROM game WHERE developer is not null and classification not in ('Homebrew','Test Rom')" +
				"UNION " +
				"SELECT DISTINCT publisher FROM game WHERE publisher is not null and classification not in ('Homebrew','Test Rom')";
			var reader = cmd.ExecuteReader();
			while (reader.NextResult())
			{
				names.Add(reader.GetString(0));
			}
			reader.Dispose();
			cmd.Dispose();

			return names;
		}

		static Game GameExists(string system, string name)
		{
			var game = new Game();
			var cmd = Con.CreateCommand();
			cmd.CommandText = "SELECT game_id, system, name, developer, publisher, classification, release_date, players, game_metadata, tags, alternate_names, notes FROM game WHERE system = @System and name = @Name";
			cmd.Parameters.Add(new SqliteParameter("@System", system));
			cmd.Parameters.Add(new SqliteParameter("@Name", name));
			var reader = cmd.ExecuteReader();
			if (reader.NextResult())
			{
				game.GameId = reader.GetInt64(0);
				game.System = reader.GetString(1);
				game.Name = reader.GetString(2);
				game.Developer = reader.GetString(3);
				game.Publisher = reader.GetString(4);
				game.Classification = reader.GetString(5);
				game.ReleaseDate = reader.GetString(6);
				game.Players = reader.GetString(7);
				game.GameMetadata = reader.GetString(8);
				game.Tags = reader.GetString(9);
				game.AltNames = reader.GetString(10);
				game.Notes = reader.GetString(11);
			}
			reader.Dispose();
			cmd.Dispose();

			if (game.GameId == 0) return null;
			return game;
		}

		static void DeleteGame(string system, string name)
		{
			var cmd = Con.CreateCommand();
			cmd.CommandText = "DELETE FROM game WHERE system=@System and name=@Name";
			cmd.Parameters.Add(new SqliteParameter("@System", system));
			cmd.Parameters.Add(new SqliteParameter("@Name", name));
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}

		public static void DeleteRom(Rom rom)
		{
			var cmd = Con.CreateCommand();
			cmd.CommandText = "DELETE FROM rom WHERE md5 = @Md5";
			cmd.Parameters.Add(new SqliteParameter("@Md5", rom.MD5));
			cmd.ExecuteNonQuery();
			cmd.Dispose();
		}
	}
}
