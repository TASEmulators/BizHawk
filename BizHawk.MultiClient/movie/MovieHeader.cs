using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
	public class MovieHeader
	{
		//Required Header Params
		//Emulation - Core version, will be 1.0.0 until there is a versioning system
		//Movie -     Versioning for the Movie code itself, or perhaps this could be changed client version?
		//Platform -  Must know what platform we are making a movie on!
		//GameName -  Which game
		//TODO: checksum of game, other stuff

		public Dictionary<string, string> HeaderParams = new Dictionary<string, string>(); //Platform specific options go here
		public List<string> Comments = new List<string>();

		public const string EMULATIONVERSION = "emuVersion";
		public const string MOVIEVERSION = "MovieVersion";
		public const string PLATFORM = "Platform";
		public const string GAMENAME = "GameName";
		public const string AUTHOR = "Author";
		public const string RERECORDS = "rerecordCount";
		public const string GUID = "GUID";
		public const string STARTSFROMSAVESTATE = "StartsFromSavestate";
		public const string FOURSCORE = "FourScore";
		public const string SHA1 = "SHA1";
		public const string FIRMWARESHA1 = "FirmwareSHA1";

		//Gameboy Settings that affect sync
		public const string GB_FORCEDMG = "Force_DMG_Mode";
		public const string GB_GBA_IN_CGB = "GBA_In_CGB";
		public const string SGB = "SGB"; //a snes movie will set this to indicate that it's actually SGB


		public static string MovieVersion = "BizHawk v0.0.1";

		public static string MakeGUID()
		{
			return System.Guid.NewGuid().ToString();
		}

		public MovieHeader() //All required fields will be set to default values
		{
			HeaderParams.Add(EMULATIONVERSION, MainForm.EMUVERSION);
			HeaderParams.Add(MOVIEVERSION, MovieVersion);
			HeaderParams.Add(PLATFORM, "");
			HeaderParams.Add(GAMENAME, "");
			HeaderParams.Add(AUTHOR, "");
			HeaderParams.Add(RERECORDS, "0");
			HeaderParams.Add(GUID, MakeGUID());
		}

		public MovieHeader(string EmulatorVersion, string MovieVersion, string Platform, string GameName, string Author, int rerecords)
		{
			HeaderParams.Add(EMULATIONVERSION, EmulatorVersion);
			HeaderParams.Add(MOVIEVERSION, MovieVersion);
			HeaderParams.Add(PLATFORM, Platform);
			HeaderParams.Add(GAMENAME, GameName);
			HeaderParams.Add(AUTHOR, Author);
			HeaderParams.Add(RERECORDS, rerecords.ToString());
			HeaderParams.Add(GUID, MakeGUID());
		}

		/// <summary>
		/// Adds the key value pair to header params.  If key already exists, value will be updated
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void AddHeaderLine(string key, string value)
		{
			string temp = value;

			if (!HeaderParams.TryGetValue(key, out temp)) //TODO: does a failed attempt mess with value?
				HeaderParams.Add(key, value);
		}

		public void UpdateRerecordCount(int count)
		{
			HeaderParams[RERECORDS] = count.ToString();
		}

		public bool RemoveHeaderLine(string key)
		{
			return HeaderParams.Remove(key);
		}

		public void Clear()
		{
			HeaderParams.Clear();
		}

		public string GetHeaderLine(string key)
		{
			string value = "";
			HeaderParams.TryGetValue(key, out value);
			return value;
		}

		public void SetHeaderLine(string key, string value)
		{
			HeaderParams[key] = value;
		}

		public void WriteText(StreamWriter sw)
		{
			foreach (KeyValuePair<string, string> kvp in HeaderParams)
			{
				sw.WriteLine(kvp.Key + " " + kvp.Value);
			}

			for (int x = 0; x < Comments.Count; x++)
			{
				sw.WriteLine(Comments[x]);
			}
		}

		private string ParseHeader(string line, string headerName)
		{
			string str;
			int x = line.LastIndexOf(headerName) + headerName.Length;
			str = line.Substring(x + 1, line.Length - x - 1);
			return str;
		}

		//TODO: replace Movie Preload & Load functions with this
		/// <summary>
		/// Receives a line and attempts to add as a header, returns false if not a useable header line
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public bool AddHeaderFromLine(string line)
		{
			if (line.Length == 0) return false;
			else if (line.Contains(MovieHeader.EMULATIONVERSION))
			{
				line = ParseHeader(line, MovieHeader.EMULATIONVERSION);
				AddHeaderLine(MovieHeader.EMULATIONVERSION, line);
			}
			else if (line.Contains(MovieHeader.MOVIEVERSION))
			{
				line = ParseHeader(line, MovieHeader.MOVIEVERSION);
				AddHeaderLine(MovieHeader.MOVIEVERSION, line);
			}
			else if (line.Contains(MovieHeader.PLATFORM))
			{
				line = ParseHeader(line, MovieHeader.PLATFORM);
				AddHeaderLine(MovieHeader.PLATFORM, line);
			}
			else if (line.Contains(MovieHeader.GAMENAME))
			{
				line = ParseHeader(line, MovieHeader.GAMENAME);
				AddHeaderLine(MovieHeader.GAMENAME, line);
			}
			else if (line.Contains(MovieHeader.RERECORDS))
			{
				line = ParseHeader(line, MovieHeader.RERECORDS);
				AddHeaderLine(MovieHeader.RERECORDS, line);
			}
			else if (line.Contains(MovieHeader.AUTHOR))
			{
				line = ParseHeader(line, MovieHeader.AUTHOR);
				AddHeaderLine(MovieHeader.AUTHOR, line);
			}
			else if (line.ToUpper().Contains(MovieHeader.GUID))
			{
				line = ParseHeader(line, MovieHeader.GUID);
				AddHeaderLine(MovieHeader.GUID, line);
			}
			else if (line.Contains(MovieHeader.STARTSFROMSAVESTATE))
			{
				line = ParseHeader(line, MovieHeader.STARTSFROMSAVESTATE);
				AddHeaderLine(MovieHeader.STARTSFROMSAVESTATE, line);
			}
			else if (line.Contains(MovieHeader.SHA1))
			{
				line = ParseHeader(line, MovieHeader.SHA1);
				AddHeaderLine(MovieHeader.SHA1, line);
			}
			else if (line.StartsWith("subtitle") || line.StartsWith("sub"))
			{
				return false;
			}
			else if (line.StartsWith("comment"))
			{
				Comments.Add(line.Substring(8, line.Length - 8));
			}
			else if (line[0] == '|')
			{
				return false;
			}
			else
				Comments.Add(line);

			return true;
		}

		public void ReadHeader(StreamReader reader)
		{
			using (reader)
			{
				string str = "";
				while ((str = reader.ReadLine()) != null)
				{
					AddHeaderFromLine(str);
				}
				reader.Close();
			}
		}
	}
}
