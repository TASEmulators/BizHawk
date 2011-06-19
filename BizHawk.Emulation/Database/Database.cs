using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace BizHawk
{
    public class GameInfo
    {
        public string Name;
        public string System;
        public string MetaData;
        public string hash;

		public enum HashType
		{
			CRC32, MD5
		}

        public string[] GetOptions()
        {
            if (string.IsNullOrEmpty(MetaData))
                return new string[0];
            return MetaData.Split(';').Where(opt => string.IsNullOrEmpty(opt) == false).ToArray();
        }

		public Dictionary<string, string> ParseOptionsDictionary()
		{
			var ret = new Dictionary<string, string>();
			foreach (var opt in GetOptions())
			{
				var parts = opt.Split('=');
				var key = parts[0];
				var value = parts.Length > 1 ? parts[1] : "";
				ret[key] = value;
			}
			return ret;
		}
    }

    public static class Database
    {
		private static Dictionary<string, GameInfo> db = new Dictionary<string, GameInfo>();

		static string RemoveHashType(string hash)
		{
			hash = hash.ToUpper();
			if (hash.StartsWith("MD5:")) hash = hash.Substring(4);
			if (hash.StartsWith("SHA1:")) hash = hash.Substring(5);
			return hash;
		}

		public static GameInfo CheckDatabase(string hash)
		{
			GameInfo ret = null;
			hash = RemoveHashType(hash);
			db.TryGetValue(hash, out ret);
			return ret;
		}

		static void LoadDatabase_Escape(string line)
		{
			if (!line.ToUpper().StartsWith("#INCLUDE")) return;
			line = line.Substring(8).TrimStart();
			if (File.Exists(line))
			{
				Console.WriteLine("loaded external game database {0}", line);
				LoadDatabase(line);
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
							LoadDatabase_Escape(line);
							continue;
						}
                        if (line.Trim().Length == 0) continue;
                        string[] items = line.Split('\t');

                        var Game = new GameInfo();
						//remove a hash type identifier. well don't really need them for indexing (theyre just there for human purposes)
						Game.hash = RemoveHashType(items[0].ToUpper());
                        Game.Name = items[2];
                        Game.System = items[3];
                        Game.MetaData = items.Length >= 6 ? items[5] : null;
                        db[Game.hash] = Game;
                    } catch (Exception)
                    {
                        Console.WriteLine("Error parsing database entry: "+line);
                    }
                }
            }
        }

        public static GameInfo GetGameInfo(byte[] RomData, string fileName)
        {
			GameInfo ret;
			string hash = string.Format("{0:X8}", CRC32.Calculate(RomData));
			if (db.TryGetValue(hash, out ret))
				return ret;

			hash = Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(RomData));
			if (db.TryGetValue(hash, out ret))
				return ret;

			hash = Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(RomData));
			if (db.TryGetValue(hash, out ret))
				return ret;

            // rom is not in database. make some best-guesses
            var Game = new GameInfo();
            Game.hash = hash;
            Game.MetaData = "NotInDatabase";

            string ext = Path.GetExtension(fileName).ToUpperInvariant();

            switch (ext)
            {
                case ".SMS": Game.System = "SMS"; break;
                case ".GG" : Game.System = "GG";  break;
                case ".SG" : Game.System = "SG";  break;
                case ".PCE": Game.System = "PCE"; break;
                case ".SGX": Game.System = "SGX"; break;
                case ".GB" : Game.System = "GB";  break;
                case ".BIN":
                case ".SMD": Game.System = "GEN"; break;
                case ".NES": Game.System = "NES"; break;
            }

            Game.Name = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');
            // If filename is all-caps, then attempt to proper-case the title.
            if (Game.Name == Game.Name.ToUpperInvariant())
                Game.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Game.Name.ToLower());

            return Game;
        }
    }
}
