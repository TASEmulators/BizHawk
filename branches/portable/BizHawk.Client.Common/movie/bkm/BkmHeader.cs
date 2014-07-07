using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public class BkmHeader : Dictionary<string, string>
	{
		public BkmHeader()
		{
			Comments = new List<string>();
			Subtitles = new SubtitleList();

			this[HeaderKeys.EMULATIONVERSION] = VersionInfo.GetEmuVersion();
			this[HeaderKeys.PLATFORM] = Global.Emulator != null ? Global.Emulator.SystemId : string.Empty;
			this[HeaderKeys.GAMENAME] = string.Empty;
			this[HeaderKeys.AUTHOR] = string.Empty;
			this[HeaderKeys.RERECORDS] = "0";
		}

		public List<string> Comments { get; private set; }
		public SubtitleList Subtitles { get; private set; }

		public string SavestateBinaryBase64Blob
		{
			get
			{
				if (ContainsKey(HeaderKeys.SAVESTATEBINARYBASE64BLOB))
				{
					return this[HeaderKeys.SAVESTATEBINARYBASE64BLOB];
				}

				return null;
			}

			set
			{
				if (value == null)
				{
					Remove(HeaderKeys.SAVESTATEBINARYBASE64BLOB);
				}
				else
				{
					Add(HeaderKeys.SAVESTATEBINARYBASE64BLOB, value);
				}
			}
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
				
				return false;
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
				
				return string.Empty;
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
				
				return string.Empty;
			}

			set
			{
				this[HeaderKeys.PLATFORM] = value;
			}
		}

		public new string this[string key]
		{
			get
			{
				return this.ContainsKey(key) ? base[key] : string.Empty;
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

			sb.Append(Subtitles);
			Comments.ForEach(comment => sb.AppendLine(comment));

			return sb.ToString();
		}

		public bool ParseLineFromFile(string line)
		{
			if (!string.IsNullOrWhiteSpace(line))
			{
				var splitLine = line.Split(new[] { ' ' }, 2);

				if (HeaderKeys.Contains(splitLine[0]) && !this.ContainsKey(splitLine[0]))
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
			}

			return true;
		}
	}
}
