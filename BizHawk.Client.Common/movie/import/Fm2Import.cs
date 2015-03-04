using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Client.Common
{
	[ImportExtension(".fm2")]
	public class Fm2Import : MovieImporter
	{
		protected override void RunImport()
		{
			var emulator = "FCEUX";
			var platform = "NES"; // TODO: FDS?

			Result.Movie.HeaderEntries[HeaderKeys.PLATFORM] = platform;

			using (var sr = SourceFile.OpenText())
			{
				string line;
				int lineNum = 0;

				while ((line = sr.ReadLine()) != null)
				{
					lineNum++;

					if (line == string.Empty)
					{
						continue;
					}
					else if (line[0] == '|')
					{
						// TODO: import a frame of input
						// TODO: report any errors importing this frame and bail out if so
					}
					else if (line.ToLower().StartsWith("sub"))
					{
						var subtitle = ImportTextSubtitle(line);

						if (!string.IsNullOrEmpty(subtitle))
						{
							Result.Movie.Subtitles.AddFromString(subtitle);
						}
					}
					else if (line.ToLower().StartsWith("emuversion"))
					{
						Result.Movie.Comments.Add(
							string.Format("{0} {1} version {2}", EMULATIONORIGIN, emulator, ParseHeader(line, "emuVersion"))
						);
					}
					else if (line.ToLower().StartsWith("version"))
					{
						string version = ParseHeader(line, "version");

						if (version != "3")
						{
							Result.Warnings.Add("Detected a .fm2 movie version other than 3, which is unsupported");
						}
						else
						{
							Result.Movie.Comments.Add(MOVIEORIGIN + " .fm2 version 3");
						}
					}
					else if (line.ToLower().StartsWith("romfilename"))
					{
						Result.Movie.HeaderEntries[HeaderKeys.GAMENAME] = ParseHeader(line, "romFilename");
					}
					else if (line.ToLower().StartsWith("cdgamename"))
					{
						Result.Movie.HeaderEntries[HeaderKeys.GAMENAME] = ParseHeader(line, "cdGameName");
					}
					else if (line.ToLower().StartsWith("romchecksum"))
					{
						string blob = ParseHeader(line, "romChecksum");
						byte[] md5 = DecodeBlob(blob);
						if (md5 != null && md5.Length == 16)
						{
							Result.Movie.HeaderEntries[MD5] = md5.BytesToHexString().ToLower();
						}
						else
						{
							Result.Warnings.Add("Bad ROM checksum.");
						}
					}
					else if (line.ToLower().StartsWith("comment author"))
					{
						Result.Movie.HeaderEntries[HeaderKeys.AUTHOR] = ParseHeader(line, "comment author");
					}
					else if (line.ToLower().StartsWith("rerecordcount"))
					{
						int rerecordCount = 0;
						int.TryParse(ParseHeader(line, "rerecordCount"), out rerecordCount);

						Result.Movie.Rerecords = (ulong)rerecordCount;
					}
					else if (line.ToLower().StartsWith("guid"))
					{
						continue; //We no longer care to keep this info
					}
					else if (line.ToLower().StartsWith("startsfromsavestate"))
					{
						// If this movie starts from a savestate, we can't support it.
						if (ParseHeader(line, "StartsFromSavestate") == "1")
						{
							Result.Errors.Add("Movies that begin with a savestate are not supported.");
							break;
						}
					}
					else if (line.ToLower().StartsWith("palflag"))
					{
						Result.Movie.HeaderEntries[HeaderKeys.PAL] = ParseHeader(line, "palFlag");
					}
					else if (line.ToLower().StartsWith("fourscore"))
					{
						bool fourscore = (ParseHeader(line, "fourscore") == "1");
						if (fourscore)
						{
							// TODO: set controller config sync settings
						}
					}
					else
					{
						Result.Movie.Comments.Add(line); // Everything not explicitly defined is treated as a comment.
					}
				}
			}
		}

		private static string ImportTextSubtitle(string line)
		{
			line = SingleSpaces(line);

			// The header name, frame, and message are separated by whitespace.
			int first = line.IndexOf(' ');
			int second = line.IndexOf(' ', first + 1);
			if (first != -1 && second != -1)
			{
				// Concatenate the frame and message with default values for the additional fields.
				string frame = line.Substring(0, first);
				string length = line.Substring(first + 1, second - first - 1);
				string message = line.Substring(second + 1).Trim();

				return "subtitle " + frame + " 0 0 " + length + " FFFFFFFF " + message;
			}

			return null;
		}

		// Reduce all whitespace to single spaces.
		private static string SingleSpaces(string line)
		{
			line = line.Replace("\t", " ");
			line = line.Replace("\n", " ");
			line = line.Replace("\r", " ");
			line = line.Replace("\r\n", " ");
			string prev;
			do
			{
				prev = line;
				line = line.Replace("  ", " ");
			}
			while (prev != line);
			return line;
		}

		// Decode a blob used in FM2 (base64:..., 0x123456...)
		private static byte[] DecodeBlob(string blob)
		{
			if (blob.Length < 2)
			{
				return null;
			}
			if (blob[0] == '0' && (blob[1] == 'x' || blob[1] == 'X'))
			{
				// hex
				return Util.HexStringToBytes(blob.Substring(2));
			}
			else
			{
				// base64
				if (!blob.ToLower().StartsWith("base64:"))
				{
					return null;
				}
				try
				{
					return Convert.FromBase64String(blob.Substring(7));
				}
				catch (FormatException)
				{
					return null;
				}
			}
		}
	}
}
