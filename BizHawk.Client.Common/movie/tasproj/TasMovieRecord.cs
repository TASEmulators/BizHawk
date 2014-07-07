using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class TasMovieRecord
	{
		public byte[] State { get; set; }
		public bool Lagged { get; set; }
		public string LogEntry { get; set; }

		public bool HasState
		{
			get { return State.Any(); }
		}
	}
}
