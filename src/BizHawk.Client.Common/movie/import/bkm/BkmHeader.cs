using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	internal class BkmHeader : Dictionary<string, string>
	{
		public BkmHeader()
		{
			this[HeaderKeys.EmulationVersion] = VersionInfo.GetEmuVersion();
			this[HeaderKeys.GameName] = "";
			this[HeaderKeys.Author] = "";
			this[HeaderKeys.Rerecords] = "0";
			this[HeaderKeys.MovieVersion] = "BizHawk v0.0.1";
		}

		public List<string> Comments { get; } = new List<string>();
		public SubtitleList Subtitles { get; } = new SubtitleList();

		public string SavestateBinaryBase64Blob
		{
			get => ContainsKey(HeaderKeys.SavestateBinaryBase64Blob)
				? this[HeaderKeys.SavestateBinaryBase64Blob]
				: null;
			set
			{
				if (value == null)
				{
					Remove(HeaderKeys.SavestateBinaryBase64Blob);
				}
				else
				{
					Add(HeaderKeys.SavestateBinaryBase64Blob, value);
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
