using System.Collections.Generic;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected IStringLog Log { get; set; } = StringLogUtil.MakeStringLog();
		protected string LogKey { get; set; } = "";

		public void WriteInputLog(TextWriter writer)
		{
			writer.WriteLine("[Input]");

			var lg = new Bk2LogEntryGenerator(LogKey, Session.MovieController);
			writer.WriteLine(lg.GenerateLogKey());

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
				if (line.StartsWith("|"))
				{
					Log.Add(line);
				}
				else if (line.StartsWith("Frame "))
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
				if (line.StartsWith("|"))
				{
					newLog.Add(line);
				}
				else if (line.StartsWith("Frame "))
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

			// RetroEdit: Wait, if this check is ostensibly to account for parsing errors,
			// why is there a failure condition above for failing to parse the savestate number?
			// Besides, stateFrame == 0 is a perfectly valid savestate;
			// it's just a savestate at the beginning of the movie before any inputs.
			if (stateFrame == 0)
			{
				stateFrame = newLog.Count;  // In case the frame count failed to parse, revert to using the entire state input log
			}

			if (Log.Count < stateFrame)
			{
				// RetroEdit: This is questionable; why is loading a state in Finished mode only allowed if we're already in Finished mode?
				// Loading a state in any case should be equally safe (after all, we're in ReadOnly mode either way).
				// (CheckTimeLines is only called in ReadOnly mode).
				// Also, the < is probably an off-by-one error, and should be <=.
				// Savestate N for an N-length movie is after the last frame, which is input N-1
				if (this.IsFinished())
				{
					// This is probably unreachable; if the movie is ReadOnly, we won't be in Finished mode (at least in current codebase).
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
				// RetroEdit: The prior condition should be >=, not >.
				// Savestate N for an N-length movie is after the last frame, which is input N-1
				// Currently, this is unreachable,
				// because an earlier check above prohibits logs greater than the movie length.
				if (Mode == MovieMode.Play || Mode == MovieMode.Finished)
				{
					Mode = MovieMode.Finished;
					return true;
				}

				return false;
			}

			// RetroEdit: If the MovieMode was Finished before, there's probably a reason.
			// It shouldn't just be reverted to Play mode without other checks.
			// If this *is* even necessary, the logic should be made more explicit.
			if (Mode == MovieMode.Finished)
			{
				Mode = MovieMode.Play;
			}

			return true;
		}
	}
}
