using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IBasicMovieInfo
	{
		// Filename of the movie, settable by the client
		string Filename { get; set; }

		string Name { get; }

		string GameName { get; set; }

		/// <summary>
		/// Gets the total number of frames that count towards the completion time of the movie
		/// </summary>
		int FrameCount { get; }

		/// <summary>
		/// Gets the actual length of time a movie lasts for. For subframe cores, this will be different then the above two options
		/// </summary>
		TimeSpan TimeLength { get; }

		/// <summary>
		/// Gets the frame rate in frames per second for the movie's system.
		/// </summary>
		double FrameRate { get; }

		SubtitleList Subtitles { get; }

		IList<string> Comments { get; }

		string SystemID { get; set; }

		ulong Rerecords { get; set; }

		/// <value>either CRC32, MD5, or SHA1, hex-encoded, unprefixed</value>
		string Hash { get; set; }

		string Author { get; set; }
		string Core { get; set; }
		string EmulatorVersion { get; set; }
		string OriginalEmulatorVersion { get; set; }
		string FirmwareHash { get; set; }
		string BoardName { get; set; }

		/// <summary>
		/// Gets the header key value pairs stored in the movie file
		/// </summary>
		IDictionary<string, string> HeaderEntries { get; }

		/// <summary>
		/// Tells the movie to load the contents of Filename
		/// </summary>
		/// <returns>Return whether or not the file was successfully loaded</returns>
		bool Load();
	}
}
