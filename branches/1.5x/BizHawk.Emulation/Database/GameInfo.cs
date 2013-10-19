using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace BizHawk
{
	public enum RomStatus
	{
		GoodDump,
		BadDump,
		Homebrew,
		TranslatedRom,
		Hack,
		Unknown,
		BIOS,
		Overdump,
		NotInDatabase
	}

	public class GameInfo
	{
		public bool IsRomStatusBad()
		{
			return Status == RomStatus.BadDump || Status == RomStatus.Overdump;
		}

		public string Name;
		public string System;
		public string Hash;
		public RomStatus Status = RomStatus.NotInDatabase;
		public bool NotInDatabase = true;
		public string FirmwareHash;

		Dictionary<string, string> Options = new Dictionary<string, string>();

        public GameInfo() { }

        public static GameInfo GetNullGame()
        {
            return new GameInfo() 
            {
                Name = "Null",
                System = "NULL",
                Hash = "",
                Status = RomStatus.GoodDump,
                NotInDatabase = false
            };
        }

		internal GameInfo(CompactGameInfo cgi)
		{
			Name = cgi.Name;
			System = cgi.System;
			Hash = cgi.Hash;
			Status = cgi.Status;
			NotInDatabase = false;
			ParseOptionsDictionary(cgi.MetaData);
		}

		public void AddOption(string option)
		{
			Options[option] = "";
		}

		public void AddOption(string option, string param)
		{
			Options[option] = param;
		}

		public void RemoveOption(string option)
		{
			Options.Remove(option);
		}

		public bool this[string option]
		{
			get { return Options.ContainsKey(option); }
		}

		public bool OptionPresent(string option)
		{
			return Options.ContainsKey(option);
		}

		public string OptionValue(string option)
		{
			if (Options.ContainsKey(option))
				return Options[option];
			return null;
		}

        public int GetIntValue(string option)
        {
            return int.Parse(Options[option]);
        }

        public int GetHexValue(string option)
        {
            return int.Parse(Options[option], NumberStyles.HexNumber);
        }

		/// <param name="parameter">The option to look up</param>
		/// <param name="defaultVal">The value to return if the option is invalid or doesn't exist</param>
		/// <returns> The bool value from the database if present, otherwise the given default value</returns>
		public bool GetBool(string parameter, bool defaultVal)
		{
			if (OptionPresent(parameter) && OptionValue(parameter) == "true")
				return true;
			else if (OptionPresent(parameter) && OptionValue(parameter) == "false")
				return false;
			else
				return defaultVal;
		}

		/// <param name="parameter">The option to look up</param>
		/// <param name="defaultVal">The value to return if the option is invalid or doesn't exist</param>
		/// <returns> The int value from the database if present, otherwise the given default value</returns>
		public int GetInt(string parameter, int defaultVal)
		{
			if (OptionPresent(parameter))
				try
				{
					return int.Parse(OptionValue(parameter));
				}
				catch
				{
					return defaultVal;
				}
			else
				return defaultVal;
		}

		public ICollection<string> GetOptions()
		{
			return Options.Keys;

		}
		public IDictionary<string, string> GetOptionsDict()
		{
			return new ReadOnlyDictionary<string, string>(Options);
		}

		void ParseOptionsDictionary(string metaData)
		{
			if (string.IsNullOrEmpty(metaData))
				return;

			var options = metaData.Split(';').Where(opt => string.IsNullOrEmpty(opt) == false).ToArray();

			foreach (var opt in options)
			{
				var parts = opt.Split('=');
				var key = parts[0];
				var value = parts.Length > 1 ? parts[1] : "";
				Options[key] = value;
			}
		}
	}
}
