namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IEmulator Emulator;
		public static Config Config;
		public static GameInfo Game;
		public static CheatList CheatList;

		//Movie

		/// <summary>
		/// the global MovieSession can use this to deal with multitrack player remapping (should this be here? maybe it should be in MovieSession)
		/// </summary>
		public static MultitrackRewiringControllerAdapter MultitrackRewiringControllerAdapter = new MultitrackRewiringControllerAdapter();
		public static MovieSession MovieSession = new MovieSession();
	}
}
