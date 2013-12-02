using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class MovieRecord : IMovieRecord
	{
		private List<byte> _state = new List<byte>();

		public string Input { get; private set; }
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
		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var record in this)
			{
				sb.AppendLine(record.ToString());
			}
			return sb.ToString();
		}
	}
}
