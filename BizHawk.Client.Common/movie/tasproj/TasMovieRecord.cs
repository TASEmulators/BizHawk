namespace BizHawk.Client.Common
{
	public class TasMovieRecord
	{
		public bool? Lagged { get; set; }
		public bool? WasLagged { get; set; }
		public string LogEntry { get; set; }

		public bool HasState { get; set; }
	}
}
