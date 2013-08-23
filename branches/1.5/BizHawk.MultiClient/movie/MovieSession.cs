namespace BizHawk.MultiClient
{
	public class MovieSession
	{
		public MultitrackRecording MultiTrack = new MultitrackRecording();
		public Movie Movie;
		public MovieControllerAdapter MovieControllerAdapter = new MovieControllerAdapter();

		public void LatchMultitrackPlayerInput(IController playerSource, MultitrackRewiringControllerAdapter rewiredSource)
		{
			if (MultiTrack.IsActive)
			{
				rewiredSource.PlayerSource = 1;
				rewiredSource.PlayerTargetMask = 1 << (MultiTrack.CurrentPlayer);
				if (MultiTrack.RecordAll) rewiredSource.PlayerTargetMask = unchecked((int)0xFFFFFFFF);
			}
			else rewiredSource.PlayerSource = -1;

			MovieControllerAdapter.LatchPlayerFromSource(rewiredSource, MultiTrack.CurrentPlayer);
		}

		public void LatchInputFromPlayer(IController source)
		{
			MovieControllerAdapter.LatchFromSource(source);
		}

		/// <summary>
		/// latch input from the input log, if available
		/// </summary>
		public void LatchInputFromLog()
		{
			string loggedFrame = Movie.GetInput(Global.Emulator.Frame);
			MovieControllerAdapter.SetControllersAsMnemonic(loggedFrame);
		}
	}

}