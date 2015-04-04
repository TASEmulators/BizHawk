using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class TasMovieRecord
	{
		// public KeyValuePair<int, byte[]> State { get; set; }
		public bool? Lagged { get; set; }
		public bool? WasLagged { get; set; }
		public string LogEntry { get; set; }

		public bool HasState { get; set; }
		//{
		//	get { return State.Value.Any(); }
		//}
	}
}
