namespace BizHawk.MultiClient
{
	public class MultitrackRecording
	{
		public bool IsActive;
		public int CurrentPlayer;
		public bool RecordAll;
		public MultitrackRecording()
		{
			IsActive = false;
			CurrentPlayer = 0;
			RecordAll = false;
		}
	}
}
