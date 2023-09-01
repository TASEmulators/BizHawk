using System.Collections.Generic;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class Bk2Header : Dictionary<string, string>
	{
		public new string this[string key]
		{
			get => TryGetValue(key, out string s) ? s : string.Empty;
			set => base[key] = value;
		}

		public override string ToString()
		{
			StringBuilder sb = new();
			foreach (var (k, v) in this) sb.Append(k).Append(' ').Append(v).AppendLine();
			return sb.ToString();
		}
	}
}
