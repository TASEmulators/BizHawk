using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	internal class BkmHeader : Dictionary<string, string>
	{
		public BkmHeader()
		{
			Comments = new List<string>();
			Subtitles = new SubtitleList();

			this[HeaderKeys.EMULATIONVERSION] = VersionInfo.GetEmuVersion();
			this[HeaderKeys.PLATFORM] = Global.Emulator != null ? Global.Emulator.SystemId : "";
			this[HeaderKeys.GAMENAME] = "";
			this[HeaderKeys.AUTHOR] = "";
			this[HeaderKeys.RERECORDS] = "0";
		}

		public List<string> Comments { get; }
		public SubtitleList Subtitles { get; }

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

		public new string this[string key]
		{
			get => ContainsKey(key) ? base[key] : "";

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

				if (HeaderKeys.Contains(splitLine[0]) && !ContainsKey(splitLine[0]))
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
