namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IMovieSession MovieSession { get; set; }
		public static InputManager InputManager { get; } = new InputManager();
	}
}
