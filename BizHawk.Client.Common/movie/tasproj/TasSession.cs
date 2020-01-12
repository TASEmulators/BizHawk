namespace BizHawk.Client.Common
{
	public class TasSession
	{
		public int CurrentFrame { get; set; }
		public int CurrentBranch { get; set; } = -1;

		public void UpdateValues(int frame, int currentBranch)
		{
			CurrentFrame = frame;
			CurrentBranch = currentBranch;
		}
	}
}
