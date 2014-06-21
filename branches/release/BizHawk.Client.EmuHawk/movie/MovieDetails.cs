using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Used for the sorting of the moviedetails in PlayMovie.cs
	/// </summary>
	public class MovieDetails
	{
		public string Keys { get; set; }
		public string Values { get; set; }
		public Color BackgroundColor { get; set; }

		public MovieDetails()
		{
			Keys = string.Empty;
			Values = string.Empty;
			BackgroundColor = Color.White;
		}
	}
}
