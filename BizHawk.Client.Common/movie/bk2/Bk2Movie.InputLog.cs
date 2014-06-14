using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		private readonly BkmLog _log = new BkmLog();

		public string GetInputLog()
		{
			var sb = new StringBuilder();

			sb.AppendLine("[Input]");
			sb.Append(RawInputLog());
			sb.AppendLine("[/Input]");

			return sb.ToString();
		}

		public bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			errorMessage = string.Empty;
			int? stateFrame = null;

			// We are in record mode so replace the movie log with the one from the savestate
			if (!Global.MovieSession.MultiTrack.IsActive)
			{
				if (Global.Config.EnableBackupMovies && _makeBackup && _log.Length > 0)
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
						_log.AppendFrame(line);
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
						_log.SetFrameAt(i, line);
						i++;
					}
				}
			}

			if (!stateFrame.HasValue)
			{
				errorMessage = "Savestate Frame number failed to parse";
			}

			var stateFramei = stateFrame ?? 0;

			if (stateFramei > 0 && stateFramei < _log.Length)
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					_log.TruncateStates(stateFramei);
					_log.TruncateMovie(stateFramei);
				}
			}
			else if (stateFramei > _log.Length) // Post movie savestate
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					_log.TruncateStates(_log.Length);
					_log.TruncateMovie(_log.Length);
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
			var log = new BkmLog();
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
					log.AppendFrame(line);
				}
			}

			if (stateFrame == 0)
			{
				stateFrame = log.Length;  // In case the frame count failed to parse, revert to using the entire state input log
			}

			if (_log.Length < stateFrame)
			{
				if (IsFinished)
				{
					return true;
				}

				errorMessage = "The savestate is from frame "
					+ log.Length
					+ " which is greater than the current movie length of "
					+ _log.Length;

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

			if (stateFrame > log.Length) // stateFrame is greater than state input log, so movie finished mode
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

		private StringBuilder RawInputLog()
		{
			var sb = new StringBuilder();
			foreach (var record in _log)
			{
				sb.AppendLine(record);
			}

			return sb;
		}
	}
}
