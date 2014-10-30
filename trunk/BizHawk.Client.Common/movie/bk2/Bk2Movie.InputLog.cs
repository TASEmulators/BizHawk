using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected List<string> _log = new List<string>();
		private string _logKey = string.Empty;

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
				if (Global.Config.EnableBackupMovies && _makeBackup && _log.Any())
				{
					SaveBackup();
					_makeBackup = false;
				}

				_log.Clear();
				while (true)
				{
					var line = reader.ReadLine();
					if (string.IsNullOrEmpty(line))
					{
						break;
					}
					else if (line.Contains("Frame 0x")) // NES stores frame count in hex, yay
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
					else if (line.StartsWith("LogKey:"))
					{
						_logKey = line.Replace("LogKey:", "");
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
					else if (line.StartsWith("LogKey:"))
					{
						_logKey = line.Replace("LogKey:", "");
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
			var newLog = new List<string>();
			var stateFrame = 0;
			while (true)
			{
				var line = reader.ReadLine();
				if (line == null)
				{
					break;
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
				else if (line[0] == '|')
				{
					newLog.Add(line);
				}
			}

			if (stateFrame == 0)
			{
				stateFrame = newLog.Count;  // In case the frame count failed to parse, revert to using the entire state input log
			}

			if (_log.Count < stateFrame)
			{
				if (IsFinished)
				{
					return true;
				}

				errorMessage = "The savestate is from frame "
					+ newLog.Count
					+ " which is greater than the current movie length of "
					+ _log.Count;

				return false;
			}

			for (var i = 0; i < stateFrame; i++)
			{
				if (_log[i] != newLog[i])
				{
					errorMessage = "The savestate input does not match the movie input at frame "
						+ (i + 1)
						+ ".";

					return false;
				}
			}

			if (stateFrame > newLog.Count) // stateFrame is greater than state input log, so movie finished mode
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

		protected StringBuilder RawInputLog()
		{
			var lg = new Bk2LogEntryGenerator(_logKey);
			lg.SetSource(Global.MovieOutputHardpoint);

			var sb = new StringBuilder();
			sb.AppendLine(lg.GenerateLogKey());
			foreach (var record in _log)
			{
				sb.AppendLine(record);
			}

			return sb;
		}

		/// <summary>
		/// Takes a log entry from a line in an input log,
		/// If the log key differs from the system's, it will be coverted
		/// </summary>
		/// <param name="line">a log entry line of text from the input log</param>
		/// /// <param name="logKey">a log entry line of text from the input log</param>
		private string ConvertLogEntryFromFile(string line, string logKey)
		{
			var adapter = new Bk2LogEntryGenerator(logKey).MovieControllerAdapter;
			adapter.Type = Global.MovieSession.MovieControllerAdapter.Type;
			adapter.SetControllersAsMnemonic(line);

			var lg = LogGeneratorInstance();
			lg.SetSource(adapter);
			return lg.GenerateLogEntry();
		}
	}
}
