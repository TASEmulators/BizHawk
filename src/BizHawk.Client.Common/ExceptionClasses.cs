namespace BizHawk.Client.Common
{
	public class MoviePlatformMismatchException : InvalidOperationException
	{
		public MoviePlatformMismatchException(string message) : base(message)
		{
		}
	}
}
