using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class MovieHeader : Dictionary<string, string>, IMovieHeader
	{
		//Required Header Params
		//Emulation - Core version, will be 1.0.0 until there is a versioning system
		//Movie -     Versioning for the Movie code itself, or perhaps this could be changed client version?
		//Platform -  Must know what platform we are making a movie on!
		//GameName -  Which game
		//TODO: checksum of game, other stuff

		public Dictionary<string, string> Parameters { get; private set; }
		public List<string> Comments { get; private set; }

		public Dictionary<string, string> BoardProperties { get; private set; }

		public SubtitleList Subtitles { get; private set; }

		public MovieHeader() //All required fields will be set to default values
		{
			Parameters = new Dictionary<string, string>(); //Platform specific options go here
			BoardProperties = new Dictionary<string, string>();
			Comments = new List<string>();
			Subtitles = new SubtitleList();

			Parameters.Add(HeaderKeys.EMULATIONVERSION, VersionInfo.GetEmuVersion());
			Parameters.Add(HeaderKeys.MOVIEVERSION, HeaderKeys.MovieVersion);
			Parameters.Add(HeaderKeys.PLATFORM, String.Empty);
			Parameters.Add(HeaderKeys.GAMENAME, String.Empty);
			Parameters.Add(HeaderKeys.AUTHOR, String.Empty);
			Parameters.Add(HeaderKeys.RERECORDS, "0");
			Parameters.Add(HeaderKeys.GUID, HeaderKeys.NewGuid);
		}

		/// <summary>
		/// Adds the key value pair to header params.  If key already exists, value will be updated
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void AddHeaderLine(string key, string value)
		{
			string temp;

			if (!Parameters.TryGetValue(key, out temp)) //TODO: does a failed attempt mess with value?
				Parameters.Add(key, value);
		}

		private void AddBoardProperty(string key, string value)
		{
			string temp;
			if (!BoardProperties.TryGetValue(key, out temp))
			{
				BoardProperties.Add(key, value);
			}
		}

		new public void Clear()
		{
			Parameters.Clear();
			BoardProperties.Clear();
			Comments.Clear();
			Subtitles.Clear();
			base.Clear();
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			foreach (var kvp in Parameters)
			{
				sb
					.Append(kvp.Key)
					.Append(' ')
					.Append(kvp.Value)
					.AppendLine();
			}

			foreach (var kvp in BoardProperties)
			{
				sb
					.Append(HeaderKeys.BOARDPROPERTIES)
					.Append(' ')
					.Append(kvp.Key)
					.Append(' ')
					.Append(kvp.Value)
					.AppendLine();
			}

			foreach (string t in Comments)
			{
				sb.AppendLine(t);
			}

			//TOD: subtitles go here not wherever it is currently located

			return sb.ToString();
		}

		public bool AddHeaderFromLine(string line)
		{
			if (!String.IsNullOrWhiteSpace(line))
			{
				var splitLine = line.Split(new char[] { ' ' }, 2);

				if (line.Contains(HeaderKeys.BOARDPROPERTIES))
				{
					var boardSplit = splitLine[1].Split(' ');
					AddBoardProperty(boardSplit[0], boardSplit[1]);
				}
				else if (HeaderKeys.Contains(splitLine[0]))
				{
					Parameters.Add(splitLine[0], splitLine[1]);
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
				else if (Parameters.ContainsKey(HeaderKeys.PLATFORM) && Parameters[HeaderKeys.PLATFORM] == "N64")
				{
					if (Parameters.ContainsKey(HeaderKeys.VIDEOPLUGIN))
					{
						if (Parameters[HeaderKeys.VIDEOPLUGIN] == "Rice")
						{
							ICollection<string> settings = Global.Config.RicePlugin.GetPluginSettings().Keys;
							foreach (var setting in settings)
							{
								if (line.Contains(setting))
								{
									Parameters.Add(splitLine[0], splitLine[1]);
									break;
								}
							}
						}
						else if (Parameters[HeaderKeys.VIDEOPLUGIN] == "Glide64")
						{
							ICollection<string> settings = Global.Config.GlidePlugin.GetPluginSettings().Keys;
							foreach (string setting in settings)
							{
								if (line.Contains(setting))
								{
									Parameters.Add(splitLine[0], splitLine[1]);
									break;
								}
							}
						}
					}
				}
			}

			return true;
		}
	}
}
