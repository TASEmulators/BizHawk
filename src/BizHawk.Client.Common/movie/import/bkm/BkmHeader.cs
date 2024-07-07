using System.Collections.Generic;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	internal class BkmHeader : Dictionary<string, string>
	{
		public BkmHeader()
		{
			this[HeaderKeys.GameName] = "";
			this[HeaderKeys.Author] = "";
			this[HeaderKeys.Rerecords] = "0";
			this[HeaderKeys.MovieVersion] = "BizHawk v0.0.1";
		}

		public List<string> Comments { get; } = new List<string>();
		public SubtitleList Subtitles { get; } = new SubtitleList();

		public string SavestateBinaryBase64Blob
		{
			get => TryGetValue(HeaderKeys.SavestateBinaryBase64Blob, out var s) ? s : null;
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
			get => TryGetValue(key, out var s) ? s : string.Empty;
			set => base[key] = value;
		}

		public new void Clear()
		{
			Comments.Clear();
			Subtitles.Clear();
			base.Clear();
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
				else if (line.StartsWithOrdinal("subtitle") || line.StartsWithOrdinal("sub"))
				{
					Subtitles.AddFromString(line);
				}
				else if (line.StartsWithOrdinal("comment"))
				{
					Comments.Add(line.Substring(8, line.Length - 8));
				}
				else if (line.StartsWith('|'))
				{
					return false;
				}
			}

			return true;
		}
	}
}
