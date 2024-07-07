using System.Collections.Generic;
using System.IO;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected IStringLog Log { get; set; } = StringLogUtil.MakeStringLog();
		public string LogKey { get; set; }

		public void WriteInputLog(TextWriter writer)
		{
			writer.WriteLine("[Input]");
			writer.Write("LogKey:");
			writer.WriteLine(string.IsNullOrEmpty(LogKey) ? Bk2LogEntryGenerator.GenerateLogKey(Session.MovieController.Definition) : LogKey);

			foreach (var record in Log)
			{
				writer.WriteLine(record);
			}

			writer.WriteLine("[/Input]");
		}

		public string GetInputLogEntry(int frame)
		{
			return frame < FrameCount && frame >= 0
				? Log[frame]
				: "";
		}

		public virtual bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			errorMessage = "";
			int? stateFrame = null;

			// We are in record mode so replace the movie log with the one from the savestate
			if (Session.Settings.EnableBackupMovies && MakeBackup && Log.Count != 0)
			{
				SaveBackup();
				MakeBackup = false;
			}

			Log.Clear();
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if (line.StartsWith('|'))
				{
					Log.Add(line);
				}
				else if (line.StartsWithOrdinal("Frame "))
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
				else if (line.StartsWithOrdinal("LogKey:"))
				{
					LogKey = line.Replace("LogKey:", "");
				}
			}

			if (!stateFrame.HasValue)
			{
				errorMessage = "Savestate Frame number failed to parse";
			}

			var stateFramei = stateFrame ?? 0;

			if (stateFramei.StrictlyBoundedBy(0.RangeTo(Log.Count)))
			{
				if (!Session.Settings.VBAStyleMovieLoadState)
				{
					Truncate(stateFramei);
				}
			}
			else if (stateFramei > Log.Count) // Post movie savestate
			{
				if (!Session.Settings.VBAStyleMovieLoadState)
				{
					Truncate(Log.Count);
				}

				Mode = MovieMode.Finished;
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
			errorMessage = "";
			var newLog = new List<string>();
			var stateFrame = 0;
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if (line.StartsWith('|'))
				{
					newLog.Add(line);
				}
				else if (line.StartsWithOrdinal("Frame "))
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
			}

			if (stateFrame == 0)
			{
				stateFrame = newLog.Count;  // In case the frame count failed to parse, revert to using the entire state input log
			}

			if (stateFrame > newLog.Count)
			{
				errorMessage = $"Savestate has invalid frame number {stateFrame} (expected maximum {newLog.Count})";
				return false;
			}

			if (Log.Count < stateFrame)
			{
				if (this.IsFinished())
				{
					return true;
				}

				errorMessage = $"The savestate is from frame {newLog.Count} which is greater than the current movie length of {Log.Count}";

				return false;
			}

			for (var i = 0; i < stateFrame; i++)
			{
				if (Log[i] != newLog[i])
				{
					errorMessage = $"The savestate input does not match the movie input at frame {(i + 1)}.";

					return false;
				}
			}

			if (stateFrame > newLog.Count) // stateFrame is greater than state input log, so movie finished mode
			{
				if (Mode == MovieMode.Play || Mode == MovieMode.Finished)
				{
					Mode = MovieMode.Finished;
					return true;
				}

				return false;
			}

			if (Mode == MovieMode.Finished)
			{
				Mode = MovieMode.Play;
			}

			return true;
		}
	}
}
