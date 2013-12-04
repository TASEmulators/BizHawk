using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class MovieRecord : IMovieRecord
	{
		private List<byte> _state = new List<byte>();

		public string Input { get; set; }
		public bool Lagged { get; private set; }
		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public MovieRecord()
		{

		}

		public override string ToString()
		{
			//TODO: consider the fileformat of binary and lagged data
			return Input;
		}
	}

	public class MovieRecordList : List<MovieRecord>
	{
		public MovieRecordList()
			: base()
		{

		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb
				.AppendLine("[Input]")

				.Append("Frame ")
				.Append(Global.Emulator.Frame)
				.AppendLine();

			foreach (var record in this)
			{
				sb.AppendLine(record.ToString());
			}
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
