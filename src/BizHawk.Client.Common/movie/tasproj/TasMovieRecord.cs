namespace BizHawk.Client.Common
{
	public interface ITasMovieRecord
	{
		bool? Lagged { get; }
		bool? WasLagged { get; }
		string LogEntry { get; }
		bool HasState { get; }
	}

	public class TasMovieRecord : ITasMovieRecord
	{
		public bool? Lagged { get; internal set; }
		public bool? WasLagged { get; internal set; }
		public string LogEntry { get; internal set; }
		public bool HasState { get; internal set; }
	}
}
