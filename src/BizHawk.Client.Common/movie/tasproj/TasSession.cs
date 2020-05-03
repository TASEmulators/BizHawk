namespace BizHawk.Client.Common
{
	public interface ITasSession
	{
		int CurrentFrame { get; }
		int CurrentBranch { get; }
		void UpdateValues(int frame, int currentBranch);
	}

	public class TasSession : ITasSession
	{
		public int CurrentFrame { get; private set; }
		public int CurrentBranch { get; private set; } = -1;

		public void UpdateValues(int frame, int currentBranch)
		{
			CurrentFrame = frame;
			CurrentBranch = currentBranch;
		}
	}
}
