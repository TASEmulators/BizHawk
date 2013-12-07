using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public class MovieRecordList : List<MovieRecord>
	{
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb
				.AppendLine("[Input]")

				.Append("Frame ")
				.Append(Global.Emulator.Frame)
				.AppendLine();

			ForEach(record => sb.Append(record.ToString()));

			sb.AppendLine("[/Input]");
			
			return sb.ToString();
		}

		public void Truncate(int index)
		{
			if (index < Count)
			{
				RemoveRange(index, Count - index);
			}
		}
	}
}
