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
    }

    public static class Database
    {
		private static Dictionary<string, GameInfo> db = new Dictionary<string, GameInfo>();

        public static void LoadDatabase(string path)
        {
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.ReadLine(); // Skip header row

                while (reader.EndOfStream == false)
                {
                    string line = reader.ReadLine();
                    try
                    {
                        if (line.Trim().Length == 0) continue;
                        string[] items = line.Split('\t');

                        var Game = new GameInfo();
                        Game.hash = items[0];
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
			string hash = string.Format("{0:X8}", CRC32.Calculate(RomData));
            if (db.ContainsKey(hash))
                return db[hash];

			hash = Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(RomData));
			if (db.ContainsKey(hash))
				return db[hash];

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
