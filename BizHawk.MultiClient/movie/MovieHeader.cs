using System.Collections.Generic;
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
		public const string PAL = "PAL";

		//Gameboy Settings that affect sync
		public const string GB_FORCEDMG = "Force_DMG_Mode";
		public const string GB_GBA_IN_CGB = "GBA_In_CGB";
		public const string SGB = "SGB"; //a snes movie will set this to indicate that it's actually SGB
		
		//BIO skipping setting (affects sync)
		public const string SKIPBIOS = "Skip_Bios";

		//Plugin Settings
		public const string VIDEOPLUGIN = "VideoPlugin";

		public static string MovieVersion = "BizHawk v0.0.1";

		public static string MakeGUID()
		{
			return System.Guid.NewGuid().ToString();
		}

		public MovieHeader() //All required fields will be set to default values
		{
			if (Global.MainForm != null)
			{
				HeaderParams.Add(EMULATIONVERSION, Global.MainForm.GetEmuVersion());
			}
			else
			{
				HeaderParams.Add(EMULATIONVERSION, MainForm.EMUVERSION);
			}
			HeaderParams.Add(MOVIEVERSION, MovieVersion);
			HeaderParams.Add(PLATFORM, "");
			HeaderParams.Add(GAMENAME, "");
			HeaderParams.Add(AUTHOR, "");
			HeaderParams.Add(RERECORDS, "0");
			HeaderParams.Add(GUID, MakeGUID());
		}

		/// <summary>
		/// Adds the key value pair to header params.  If key already exists, value will be updated
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void AddHeaderLine(string key, string value)
		{
			string temp;

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
			string value;
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

			foreach (string t in Comments)
			{
				sw.WriteLine(t);
			}
		}

		private string ParseHeader(string line, string headerName)
		{
			int x = line.LastIndexOf(headerName) + headerName.Length;
			string str = line.Substring(x + 1, line.Length - x - 1);
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
			else if (line.Contains(EMULATIONVERSION))
			{
				line = ParseHeader(line, EMULATIONVERSION);
				AddHeaderLine(EMULATIONVERSION, line);
			}
			else if (line.Contains(MOVIEVERSION))
			{
				line = ParseHeader(line, MOVIEVERSION);
				AddHeaderLine(MOVIEVERSION, line);
			}
			else if (line.Contains(PLATFORM))
			{
				line = ParseHeader(line, PLATFORM);
				AddHeaderLine(PLATFORM, line);
			}
			else if (line.Contains(GAMENAME))
			{
				line = ParseHeader(line, GAMENAME);
				AddHeaderLine(GAMENAME, line);
			}
			else if (line.Contains(RERECORDS))
			{
				line = ParseHeader(line, RERECORDS);
				AddHeaderLine(RERECORDS, line);
			}
			else if (line.Contains(AUTHOR))
			{
				line = ParseHeader(line, AUTHOR);
				AddHeaderLine(AUTHOR, line);
			}
			else if (line.ToUpper().Contains(GUID))
			{
				line = ParseHeader(line, GUID);
				AddHeaderLine(GUID, line);
			}
			else if (line.Contains(STARTSFROMSAVESTATE))
			{
				line = ParseHeader(line, STARTSFROMSAVESTATE);
				AddHeaderLine(STARTSFROMSAVESTATE, line);
			}
			else if (line.Contains(SHA1))
			{
				line = ParseHeader(line, SHA1);
				AddHeaderLine(SHA1, line);
			}
			else if (line.Contains(SKIPBIOS))
			{
				line = ParseHeader(line, SKIPBIOS);
				AddHeaderLine(SKIPBIOS, line);
			}
			else if (line.Contains(GB_FORCEDMG))
			{
				line = ParseHeader(line, GB_FORCEDMG);
				AddHeaderLine(GB_FORCEDMG, line);
			}
			else if (line.Contains(GB_GBA_IN_CGB))
			{
				line = ParseHeader(line, GB_GBA_IN_CGB);
				AddHeaderLine(GB_GBA_IN_CGB, line);
			}
			else if (line.Contains(SGB))
			{
				line = ParseHeader(line, SGB);
				AddHeaderLine(SGB, line);
			}
			else if (line.Contains(PAL))
			{
				line = ParseHeader(line, PAL);
				AddHeaderLine(PAL, line);
			}
			else if (line.Contains(VIDEOPLUGIN))
			{
				line = ParseHeader(line, VIDEOPLUGIN);
				AddHeaderLine(VIDEOPLUGIN, line);
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
			{
				if (HeaderParams[PLATFORM] == "N64")
				{
					if (HeaderParams[VIDEOPLUGIN] == "Rice")
					{
						ICollection<string> settings = Global.Config.RicePlugin.GetPluginSettings().Keys;
						foreach (string setting in settings)
						{
							if (line.Contains(setting))
							{
								line = ParseHeader(line, setting);
								AddHeaderLine(setting, line);
								break;
							}
						}
					}
					else if (HeaderParams[VIDEOPLUGIN] == "Glide64")
					{
						ICollection<string> settings = Global.Config.GlidePlugin.GetPluginSettings().Keys;
						foreach (string setting in settings)
						{
							if (line.Contains(setting))
							{
								line = ParseHeader(line, setting);
								AddHeaderLine(setting, line);
								break;
							}
						}
					}
				}
				else
				{
					Comments.Add(line);
				}
			}

			return true;
		}

		public void ReadHeader(StreamReader reader)
		{
			using (reader)
			{
				string str;
				while ((str = reader.ReadLine()) != null)
				{
					AddHeaderFromLine(str);
				}
				reader.Close();
			}
		}
	}
}
