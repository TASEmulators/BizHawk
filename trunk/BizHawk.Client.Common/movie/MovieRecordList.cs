using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Client.Common
{
	public class MovieRecordList : List<MovieRecord>
	{
		/// <summary>
		/// DONT USE ME RIGHT NOW
		/// </summary>
		public void WriteToText(TextWriter tw)
		{
			tw.WriteLine("[Input]");
			tw.WriteLine("Frame {0}", Global.Emulator.Frame);
			ForEach(record => tw.WriteLine(record.ToString()));
			tw.WriteLine("[/Input]");
		}

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

		public void TruncateStates(int index)
		{
			for (int i = index; i < Count; i++)
			{
				this[i].ClearState();
			}
		}
	}
}
