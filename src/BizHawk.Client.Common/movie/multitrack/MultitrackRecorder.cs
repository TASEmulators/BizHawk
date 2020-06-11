namespace BizHawk.Client.Common
{
	public class MultitrackRecorder
	{
		public MultitrackRecorder()
		{
			Restart(1);
		}

		internal MultitrackRewiringControllerAdapter RewiringAdapter { get; } = new MultitrackRewiringControllerAdapter();

		public bool IsActive { get; set; }
		public int CurrentPlayer { get; private set; }
		public int PlayerCount { get; private set; }
		public bool RecordAll { get; private set; }

		/// <summary>
		/// Gets a user friendly multi-track status
		/// </summary>
		public string Status
		{
			get
			{
				if (!IsActive)
				{
					return "";
				}

				if (RecordAll)
				{
					return "Recording All";
				}

				if (CurrentPlayer == 0)
				{
					return "Recording None";
				}

				return $"Recording Player {CurrentPlayer}";
			}
		}

		public void Restart(int playerCount)
		{
			PlayerCount = playerCount;
			IsActive = false;
			CurrentPlayer = 0;
			RecordAll = false;
		}

		public void SelectAll()
		{
			CurrentPlayer = 0;
			RecordAll = true;
		}

		public void SelectNone()
		{
			RecordAll = false;
			CurrentPlayer = 0;
		}

		public void Increment()
		{
			RecordAll = false;
			CurrentPlayer++;
			if (CurrentPlayer > PlayerCount)
			{
				CurrentPlayer = 1;
			}
		}

		public void Decrement()
		{
			RecordAll = false;
			CurrentPlayer--;
			if (CurrentPlayer < 1)
			{
				CurrentPlayer = PlayerCount;
			}
		}
	}
}
