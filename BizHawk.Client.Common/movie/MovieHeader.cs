using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	using System.Linq;

	public class MovieHeader : Dictionary<string, string>, IMovieHeader
	{
		public List<string> Comments { get; private set; }
		public Dictionary<string, string> BoardProperties { get; private set; }
		public SubtitleList Subtitles { get; private set; }

		public MovieHeader()
		{
			Comments = new List<string>();
			Subtitles = new SubtitleList();
			BoardProperties = new Dictionary<string, string>();

			this[HeaderKeys.EMULATIONVERSION] = VersionInfo.GetEmuVersion();
			this[HeaderKeys.MOVIEVERSION] = HeaderKeys.MovieVersion;
			this[HeaderKeys.PLATFORM] = Global.Emulator != null ? Global.Emulator.SystemId : String.Empty;
			this[HeaderKeys.GAMENAME] = String.Empty;
			this[HeaderKeys.AUTHOR] = String.Empty;
			this[HeaderKeys.RERECORDS] = "0";
			this[HeaderKeys.GUID] = HeaderKeys.NewGuid;
		}

		public new string this[string key]
		{
			get
			{
				return this.ContainsKey(key) ? base[key] : String.Empty;
			}

			set
			{
				if (ContainsKey(key))
				{
					base[key] = value;
				}
				else
				{
					Add(key, value);
				}
			}
		}

		public new void Clear()
		{
			BoardProperties.Clear();
			Comments.Clear();
			Subtitles.Clear();
			base.Clear();
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			foreach (var kvp in this)
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

			sb.Append(Subtitles);
			Comments.ForEach(comment => sb.AppendLine(comment));

			return sb.ToString();
		}

		public ulong Rerecords
		{
			get
			{
				if (!ContainsKey(HeaderKeys.RERECORDS))
				{
					this[HeaderKeys.RERECORDS] = "0";
				}

				return ulong.Parse(this[HeaderKeys.RERECORDS]);
			}

			set
			{
				this[HeaderKeys.RERECORDS] = value.ToString();
			}
		}

		public bool StartsFromSavestate
		{
			get
			{
				if (ContainsKey(HeaderKeys.STARTSFROMSAVESTATE))
				{
					return bool.Parse(this[HeaderKeys.STARTSFROMSAVESTATE]);
				}
				else
				{
					return false;
				}
			}

			set
			{
				if (value)
				{
					Add(HeaderKeys.STARTSFROMSAVESTATE, "True");
				}
				else
				{
					Remove(HeaderKeys.STARTSFROMSAVESTATE);
				}
			}
		}

		public string GameName
		{
			get
			{
				if (ContainsKey(HeaderKeys.GAMENAME))
				{
					return this[HeaderKeys.GAMENAME];
				}
				else
				{
					return String.Empty;
				}
			}

			set
			{
				this[HeaderKeys.GAMENAME] = value;
			}
		}

		public string SystemID
		{
			get
			{
				if (ContainsKey(HeaderKeys.PLATFORM))
				{
					return this[HeaderKeys.PLATFORM];
				}
				else
				{
					return String.Empty;
				}
			}

			set
			{
				this[HeaderKeys.PLATFORM] = value;
			}
		}

		public bool ParseLineFromFile(string line)
		{
			if (!String.IsNullOrWhiteSpace(line))
			{
				var splitLine = line.Split(new[] { ' ' }, 2);

				if (line.Contains(HeaderKeys.BOARDPROPERTIES))
				{
					var boardSplit = splitLine[1].Split(' ');
					if (!BoardProperties.ContainsKey(boardSplit[0]))
					{
						BoardProperties.Add(boardSplit[0], boardSplit[1]);
					}
				}
				else if (HeaderKeys.Contains(splitLine[0]))
				{
					Add(splitLine[0], splitLine[1]);
				}
				else if (line.StartsWith("subtitle") || line.StartsWith("sub"))
				{
					Subtitles.AddFromString(line);
				}
				else if (line.StartsWith("comment"))
				{
					Comments.Add(line.Substring(8, line.Length - 8));
				}
				else if (line.StartsWith("|"))
				{
					return false;
				}
				else if (ContainsKey(HeaderKeys.PLATFORM) && this[HeaderKeys.PLATFORM] == "N64" 
					&& ContainsKey(HeaderKeys.VIDEOPLUGIN))
				{
					if (this[HeaderKeys.VIDEOPLUGIN] == "Rice")
					{
						if (Global.Config.RicePlugin.GetPluginSettings().Keys.Any(line.Contains))
						{
							Add(splitLine[0], splitLine[1]);
						}
					}
					else if (this[HeaderKeys.VIDEOPLUGIN] == "Glide64")
					{
						if (Global.Config.GlidePlugin.GetPluginSettings().Keys.Any(line.Contains))
						{
							Add(splitLine[0], splitLine[1]);
						}
					}
				}
			}

			return true;
		}
	}
}
