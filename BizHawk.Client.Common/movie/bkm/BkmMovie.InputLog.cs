using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class BkmMovie
	{
		private readonly List<string> _log = new List<string>();

		public string GetInputLog()
		{
			var sb = new StringBuilder();
			var writer = new StringWriter(sb);
			WriteInputLog(writer);
			writer.Flush();
			return sb.ToString();
		}

		public void WriteInputLog(TextWriter writer)
		{
			writer.WriteLine("[Input]");

			foreach (var record in _log)
			{
				writer.WriteLine(record);
			}

			writer.WriteLine("[/Input]");
		}

		public string GetInputLogEntry(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{
				return _log[frame];
			}

			return string.Empty;
		}

		public bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			errorMessage = string.Empty;
			int? stateFrame = null;

			// We are in record mode so replace the movie log with the one from the savestate
			if (!Global.MovieSession.MultiTrack.IsActive)
			{
				if (Global.Config.EnableBackupMovies && _makeBackup && _log.Any())
				{
					SaveBackup();
					_makeBackup = false;
				}

				_log.Clear();
				while (true)
				{
					var line = reader.ReadLine();
					if (line == null)
					{
						break;
					}

					if (line.Trim() == string.Empty || line == "[Input]")
					{
						continue;
					}

					if (line == "[/Input]")
					{
						break;
					}

					if (line.Contains("Frame 0x")) // NES stores frame count in hex, yay
					{
						var strs = line.Split('x');
						try
						{
							stateFrame = int.Parse(strs[1], NumberStyles.HexNumber);
						}
						catch
						{
							errorMessage = "Savestate Frame number failed to parse";
							return false;
						}
					}
					else if (line.Contains("Frame "))
					{
						var strs = line.Split(' ');
						try
						{
							stateFrame = int.Parse(strs[1]);
						}
						catch
						{
							errorMessage = "Savestate Frame number failed to parse";
							return false;
						}
					}
					else if (line[0] == '|')
					{
						_log.Add(line);
					}
				}
			}
			else
			{
				var i = 0;
				while (true)
				{
					var line = reader.ReadLine();
					if (line == null)
					{
						break;
					}

					if (line.Trim() == string.Empty || line == "[Input]")
					{
						continue;
					}

					if (line == "[/Input]")
					{
						break;
					}

					if (line.Contains("Frame 0x")) // NES stores frame count in hex, yay
					{
						var strs = line.Split('x');
						try
						{
							stateFrame = int.Parse(strs[1], NumberStyles.HexNumber);
						}
						catch
						{
							errorMessage = "Savestate Frame number failed to parse";
							return false;
						}
					}
					else if (line.Contains("Frame "))
					{
						var strs = line.Split(' ');
						try
						{
							stateFrame = int.Parse(strs[1]);
						}
						catch
						{
							errorMessage = "Savestate Frame number failed to parse";
							return false;
						}
					}
					else if (line.StartsWith("|"))
					{
						SetFrameAt(i, line);
						i++;
					}
				}
			}

			if (!stateFrame.HasValue)
			{
				errorMessage = "Savestate Frame number failed to parse";
			}

			var stateFramei = stateFrame ?? 0;

			if (stateFramei > 0 && stateFramei < _log.Count)
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Truncate(stateFramei);
				}
			}
			else if (stateFramei > _log.Count) // Post movie savestate
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Truncate(_log.Count);
				}

				_mode = Moviemode.Finished;
			}

			if (IsCountingRerecords)
			{
				Rerecords++;
			}

			return true;
		}

		public bool CheckTimeLines(TextReader reader, out string errorMessage)
		{
			// This function will compare the movie data to the savestate movie data to see if they match
			errorMessage = string.Empty;
			var log = new List<string>();
			var stateFrame = 0;
			while (true)
			{
				var line = reader.ReadLine();
				if (line == null)
				{
					return false;
				}

				if (line.Trim() == string.Empty)
				{
					continue;
				}

				if (line.Contains("Frame 0x")) // NES stores frame count in hex, yay
				{
					var strs = line.Split('x');
					try
					{
						stateFrame = int.Parse(strs[1], NumberStyles.HexNumber);
					}
					catch
					{
						errorMessage = "Savestate Frame number failed to parse";
						return false;
					}
				}
				else if (line.Contains("Frame "))
				{
					var strs = line.Split(' ');
					try
					{
						stateFrame = int.Parse(strs[1]);
					}
					catch
					{
						errorMessage = "Savestate Frame number failed to parse";
						return false;
					}
				}
				else if (line == "[Input]")
				{
					continue;
				}
				else if (line == "[/Input]")
				{
					break;
				}
				else if (line[0] == '|')
				{
					log.Add(line);
				}
			}

			if (stateFrame == 0)
			{
				stateFrame = log.Count;  // In case the frame count failed to parse, revert to using the entire state input log
			}

			if (_log.Count < stateFrame)
			{
				if (IsFinished)
				{
					return true;
				}

				errorMessage = "The savestate is from frame "
					+ log.Count
					+ " which is greater than the current movie length of "
					+ _log.Count;

				return false;
			}

			for (var i = 0; i < stateFrame; i++)
			{
				if (_log[i] != log[i])
				{
					errorMessage = "The savestate input does not match the movie input at frame "
						+ (i + 1)
						+ ".";

					return false;
				}
			}

			if (stateFrame > log.Count) // stateFrame is greater than state input log, so movie finished mode
			{
				if (_mode == Moviemode.Play || _mode == Moviemode.Finished)
				{
					_mode = Moviemode.Finished;
					return true;
				}

				return false;
			}

			if (_mode == Moviemode.Finished)
			{
				_mode = Moviemode.Play;
			}

			return true;
		}
	}
}
