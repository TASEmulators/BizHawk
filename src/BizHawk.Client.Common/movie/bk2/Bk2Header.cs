using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public class Bk2Header : Dictionary<string, string>
	{
		public new string this[string key]
		{
			get => TryGetValue(key, out var s) ? s : string.Empty;
			set => base[key] = value;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var (k, v) in this) sb.Append(k).Append(' ').Append(v).AppendLine();
			return sb.ToString();
		}
	}
}
