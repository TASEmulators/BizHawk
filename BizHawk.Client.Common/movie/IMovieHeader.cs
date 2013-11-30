using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IMovieHeader : IDictionary<string, string>
	{
		SubtitleList Subtitles { get; }
		Dictionary<string, string> BoardProperties { get; }
		List<string> Comments { get; }

		ulong Rerecords { get; set; }
		bool StartsFromSavestate { get; set; }
		string GameName { get; set; }
		string SystemID { get; set; }

		/// <summary>
		/// Receives a line and attempts to add as a header
		/// </summary>
		/// <param name="line">
		/// The line of text loaded from a movie file.
		/// </param>
		/// <returns>
		/// returns false if not a useable header line
		/// </returns>
		bool ParseLineFromFile(string line);
	}
}
