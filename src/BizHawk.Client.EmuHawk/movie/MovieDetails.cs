using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Used for the sorting of the movie details in PlayMovie.cs
	/// </summary>
	public class MovieDetails
	{
		public string Keys { get; set; } = "";
		public string Values { get; set; } = "";
		public Color BackgroundColor { get; set; } = Color.White;
	}
}
