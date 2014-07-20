namespace BizHawk.Client.Common
{
	public class MultitrackRecording
	{
		public bool IsActive { get; set; }

		public int CurrentPlayer { get; set; }

		public bool RecordAll { get; set; }

		/// <summary>
		/// A user friendly multitrack status
		/// </summary>
		public string CurrentState { get; set; }

		public void SelectAll()
		{
			CurrentPlayer = 0;
			RecordAll = true;
			CurrentState = "Recording All";
		}

		public void SelectNone()
		{
			RecordAll = false;
			CurrentPlayer = 0;
			CurrentState = "Recording None";
		}

		public void Increment()
		{
			RecordAll = false;
			CurrentPlayer++;
			if (CurrentPlayer > Global.Emulator.ControllerDefinition.PlayerCount)
			{
				CurrentPlayer = 1;
			}

			CurrentState = "Recording Player " + CurrentPlayer;
		}

		public void Decrement()
		{
			RecordAll = false;
			CurrentPlayer--;
			if (CurrentPlayer < 1)
			{
				CurrentPlayer = Global.Emulator.ControllerDefinition.PlayerCount;
			}

			CurrentState = "Recording Player " + CurrentPlayer;
		}
	}
}
