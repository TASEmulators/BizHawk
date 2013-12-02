using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Movie : IMovie
	{
		#region Constructors

		public Movie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Header.Rerecords = 0;
			Filename = filename;
			Loaded = !String.IsNullOrWhiteSpace(filename);
		}

		public Movie(bool startsFromSavestate = false)
		{
			Header = new MovieHeader();
			Filename = String.Empty;
			_preloadFramecount = 0;
			Header.StartsFromSavestate = startsFromSavestate;
			
			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			IsText = true;
			MakeBackup = true;
		}

		#endregion

		#region Properties

		public IMovieHeader Header { get; private set; }

		public bool MakeBackup { get; set; }
		public string Filename { get; set; }
		public bool IsCountingRerecords { get; set; }
		
		public bool Loaded { get; private set; }
		public bool IsText { get; private set; }

		public int InputLogLength
		{
			get { return Loaded ? _log.Length : _preloadFramecount; }
		}

		public double FrameCount
		{
			get
			{
				if (_loopOffset.HasValue)
				{
					return double.PositiveInfinity;
				}
				else if (Loaded)
				{
					
					return _log.Length;
				}
				else
				{
					return _preloadFramecount;
				}
			}
		}

		#endregion

		#region Mode API

		public bool IsPlaying
		{
			get { return _mode == Moviemode.Play || _mode == Moviemode.Finished; }
		}

		public bool IsRecording
		{
			get { return _mode == Moviemode.Record; }
		}

		public bool IsActive
		{
			get { return _mode != Moviemode.Inactive; }
		}

		public bool IsFinished
		{
			get { return _mode == Moviemode.Finished; }
		}

		public bool Changes
		{
			get { return _changes; }
		}

		public void StartNewRecording()
		{
			_mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && MakeBackup && _log.Length > 0)
			{
				SaveAs();
				MakeBackup = false;
			}

			_log.Clear();
		}

		public void StartNewPlayback()
		{
			_mode = Moviemode.Play;
			Global.Emulator.ClearSaveRam();
		}

		public void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		public void SwitchToPlay()
		{
			_mode = Moviemode.Play;
			Save();
		}

		public void Stop(bool saveChanges = true)
		{
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || _changes)
				{
					Save();
				}
			}

			_changes = false;
			_mode = Moviemode.Inactive;
		}

		/// <summary>
		/// If a movie is in playback mode, this will set it to movie finished
		/// </summary>
		private void Finish()
		{
			if (_mode == Moviemode.Play)
			{
				_mode = Moviemode.Finished;
			}
		}
		
		#endregion

		#region Public File Handling

		public void SaveAs(string path)
		{
			if (!Loaded)
			{
				return;
			}

			var directory_info = new FileInfo(Filename).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			if (IsText)
			{
				WriteText(Filename);
			}
			else
			{
				WriteBinary(Filename);
			}
		}

		public void Save()
		{
			if (!Loaded || String.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			SaveAs(Filename);
			_changes = false;
		}

		public void SaveAs()
		{
			if (!Loaded || String.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			var backupName = Filename;
			backupName = backupName.Insert(Filename.LastIndexOf("."), String.Format(".{0:yyyy-MM-dd HH.mm.ss}", DateTime.Now));
			backupName = Path.Combine(Global.Config.PathEntries["Global", "Movie backups"].Path, Path.GetFileName(backupName) ?? String.Empty);

			var directory_info = new FileInfo(backupName).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			if (IsText)
			{
				WriteText(backupName);
			}
			else
			{
				WriteBinary(backupName);
			}
		}

		/// <summary>
		/// Load Header information only for displaying file information in dialogs such as play movie
		/// </summary>
		public bool PreLoadText(HawkFile hawkFile)
		{
			Loaded = false;
			var file = new FileInfo(hawkFile.CanonicalFullPath);

			if (file.Exists == false)
			{
				return false;
			}
			else
			{
				Header.Clear();
				_log.Clear();
			}

			var origStreamPosn = hawkFile.GetStream().Position; 
			hawkFile.GetStream().Position = 0; // Reset to start
			var sr = new StreamReader(hawkFile.GetStream());
			
			// No using block because we're sharing the stream and need to give it back undisposed.
			if (!sr.EndOfStream)
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					if (line.Contains("LoopOffset"))
					{
						try
						{
							_loopOffset = int.Parse(line.Split(new[] { ' ' }, 2)[1]);
						}
						catch (Exception)
						{
							continue;
						}
					}
					else if (String.IsNullOrWhiteSpace(line) || Header.ParseLineFromFile(line))
					{
						continue;
					}
					else if (line.StartsWith("|"))
					{
						var frames = sr.ReadToEnd();
						var length = line.Length;

						// Account for line breaks of either size.
						if (frames.IndexOf("\r\n") != -1)
						{
							length++;
						}

						length++;
						_preloadFramecount = (frames.Length / length) + 1; // Count the remaining frames and the current one.
						break;
					}
					else
					{
						Header.Comments.Add(line);
					}
				}
			}

			hawkFile.GetStream().Position = origStreamPosn;

			return true;
		}

		public bool Load()
		{
			var file = new FileInfo(Filename);
			if (file.Exists == false)
			{
				Loaded = false;
				return false;
			}

			return LoadText();
		}

		#endregion

		#region Public Log Editing

		public string GetInput(int frame)
		{
			if (frame < _log.Length)
			{
				if (frame >= 0)
				{
					int getframe;

					if (_loopOffset.HasValue)
					{
						if (frame < _log.Length)
						{
							getframe = frame;
						}
						else
						{
							getframe = ((frame - _loopOffset.Value) % (_log.Length - _loopOffset.Value)) + _loopOffset.Value;
						}
					}
					else
					{
						getframe = frame;
					}

					return _log[getframe];
				}
				else
				{
					return String.Empty;
				}
			}
			else
			{
				Finish();
				return String.Empty;
			}
		}

		public void ClearFrame(int frame)
		{
			_log.SetFrameAt(frame, MnemonicsGenerator.GetEmptyMnemonic);
			_changes = true;
		}

		public void AppendFrame(string record)
		{
			_log.AppendFrame(record);
			_changes = true;
		}

		public void TruncateMovie(int frame)
		{
			_log.TruncateMovie(frame);
			_log.TruncateStates(frame);
			_changes = true;
		}

		#endregion

		#region Public Misc Methods

		public void PokeFrame(int frameNum, string input)
		{
			_changes = true;
			_log.SetFrameAt(frameNum, input);
		}

		public void CommitFrame(int frameNum, IController source)
		{
			// Note: Truncation here instead of loadstate will make VBA style loadstates
			// (Where an entire movie is loaded then truncated on the next frame
			// this allows users to restore a movie with any savestate from that "timeline"
			if (Global.Config.VBAStyleMovieLoadState)
			{
				if (Global.Emulator.Frame < _log.Length)
				{
					_log.TruncateMovie(Global.Emulator.Frame);
				}
			}

			_changes = true;
			var mg = new MnemonicsGenerator();
			mg.SetSource(source);
			_log.SetFrameAt(frameNum, mg.GetControllersAsMnemonic());
		}

		public string GetInputLog()
		{
			var sb = new StringBuilder();

			sb
				.AppendLine("[Input]")
				.AppendLine(HeaderKeys.GUID + " " + Header[HeaderKeys.GUID]);

			foreach (var record in _log)
			{
				sb.AppendLine(record);
			}

			sb.AppendLine("[/Input]");

			return sb.ToString();
		}

		public void ExtractInputLog(TextReader reader, bool isMultitracking)
		{
			int? stateFrame = null;
			
			// We are in record mode so replace the movie log with the one from the savestate
			if (!isMultitracking)
			{
				if (Global.Config.EnableBackupMovies && MakeBackup && _log.Length > 0)
				{
					SaveAs();
					MakeBackup = false;
				}

				_log.Clear();
				while (true)
				{
					var line = reader.ReadLine();
					if (line == null)
					{
						break;
					}
					else if (line.Trim() == String.Empty)
					{
						continue;
					}
					else if (line == "[Input]")
					{
						continue;
					}
					else if (line == "[/Input]")
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
						catch { } // TODO: message?
					}
					else if (line.Contains("Frame "))
					{
						var strs = line.Split(' ');
						try
						{
							stateFrame = int.Parse(strs[1]);
						}
						catch { } //TODO: message?
					}
					if (line[0] == '|')
					{
						_log.AppendFrame(line);
					}
				}
			}
			else
			{
				int i = 0;
				while (true)
				{
					var line = reader.ReadLine();
					if (line == null)
					{
						break;
					}
					else if (line.Trim() == string.Empty)
					{
						continue;
					}
					else if (line == "[Input]")
					{
						continue;
					}
					else if (line == "[/Input]")
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
						catch { } // TODO: message?
					}
					else if (line.Contains("Frame "))
					{
						var strs = line.Split(' ');
						try
						{
							stateFrame = int.Parse(strs[1]);
						}
						catch { } // TODO: message?
					}
					else if (line.StartsWith("|"))
					{
						_log.SetFrameAt(i, line);
						i++;
					}
				}
			}

			if (stateFrame == null)
			{
				throw new Exception("Couldn't find stateFrame");
			}

			var stateFramei = (int)stateFrame;

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
				Header.Rerecords++;
			}
		}

		public TimeSpan Time
		{
			get
			{
				double dblseconds = GetSeconds(Loaded ? _log.Length : _preloadFramecount);
				int seconds = (int)(dblseconds % 60);
				int days = seconds / 86400;
				int hours = seconds / 3600;
				int minutes = (seconds / 60) % 60;
				int milliseconds = (int)((dblseconds - (double)seconds) * 1000);
				return new TimeSpan(days, hours, minutes, seconds, milliseconds);
			}
		}

		public LoadStateResult CheckTimeLines(TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage)
		{
			// This function will compare the movie data to the savestate movie data to see if they match
			errorMessage = String.Empty;
			var log = new MovieLog();
			int stateFrame = 0;
			while (true)
			{
				var line = reader.ReadLine();
				if (line == null)
				{
					return LoadStateResult.EmptyLog;
				}
				else if (line.Trim() == string.Empty)
				{
					continue;
				}
				else if (line.Contains("GUID"))
				{
					var guid = line.Split(new[] { ' ' }, 2)[1];
					if (Header[HeaderKeys.GUID] != guid)
					{
						if (!ignoreGuidMismatch)
						{
							return LoadStateResult.GuidMismatch;
						}
					}
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
						return LoadStateResult.MissingFrameNumber;
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
						return LoadStateResult.MissingFrameNumber;
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

			if (onlyGuid)
			{
				return LoadStateResult.Pass;
			}

			if (stateFrame == 0)
			{
				stateFrame = log.Length;  // In case the frame count failed to parse, revert to using the entire state input log
			}

			if (_log.Length < stateFrame)
			{
				if (IsFinished)
				{
					return LoadStateResult.Pass;
				}
				else
				{
					errorMessage = "The savestate is from frame "
						+ log.Length
						+ " which is greater than the current movie length of "
						+ _log.Length;
					return LoadStateResult.FutureEventError;
				}
			}

			for (var i = 0; i < stateFrame; i++)
			{
				if (_log[i] != log[i])
				{
					errorMessage = "The savestate input does not match the movie input at frame "
						+ (i + 1)
						+ ".";
					return LoadStateResult.TimeLineError;
				}
			}

			if (stateFrame > log.Length) // stateFrame is greater than state input log, so movie finished mode
			{
				if (_mode == Moviemode.Play || _mode == Moviemode.Finished)
				{
					_mode = Moviemode.Finished;
					return LoadStateResult.Pass;
				}
				else
				{
					return LoadStateResult.NotInRecording; // TODO: For now throw an error if recording, ideally what should happen is that the state gets loaded, and the movie set to movie finished, the movie at its current state is preserved and the state is loaded just fine.  This should probably also only happen if checktimelines passes
				}
			}
			else if (_mode == Moviemode.Finished)
			{
				_mode = Moviemode.Play;
			}

			return LoadStateResult.Pass;
		}

		#endregion

		#region Private Vars

		private readonly MovieLog _log = new MovieLog();
		private enum Moviemode { Inactive, Play, Record, Finished };
		private Moviemode _mode = Moviemode.Inactive;
		private int _preloadFramecount; // Not a a reliable number, used for preloading (when no log has yet been loaded), this is only for quick stat compilation for dialogs such as play movie
		private bool _changes;
		private int? _loopOffset;

		#endregion

		#region Helpers

		private void WriteText(string fn)
		{
			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				WriteText(fs);
			}
		}

		private void WriteBinary(string fn)
		{
			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				WriteBinary(fs);
			}
		}

		private void WriteText(Stream stream)
		{
			using (var sw = new StreamWriter(stream))
			{
				sw.Write(Header.ToString());

				// TODO: clean this up
				if (_loopOffset.HasValue)
				{
					sw.WriteLine("LoopOffset " + _loopOffset);
				}

				for (int i = 0; i < _log.Length; i++)
				{
					sw.WriteLine(_log[i]);
				}
			}
		}

		private void WriteBinary(Stream stream)
		{
		}

		private bool LoadText()
		{
			var file = new FileInfo(Filename);

			if (file.Exists == false)
			{
				Loaded = false;
				return false;
			}
			else
			{
				Header.Clear();
				_log.Clear();
			}

			using (var sr = file.OpenText())
			{
				string line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line == String.Empty)
					{
						continue;
					}

					if (line.Contains("LoopOffset"))
					{
						try
						{
							_loopOffset = int.Parse(line.Split(new[] { ' ' }, 2)[1]);
						}
						catch (Exception)
						{
							continue;
						}
					}
					else if (Header.ParseLineFromFile(line))
					{
						continue;
					}
					else if (line.StartsWith("|"))
					{
						_log.AppendFrame(line);
					}
					else
					{
						Header.Comments.Add(line);
					}
				}
			}

			Loaded = true;
			return true;
		}

		private bool LoadBinary()
		{
			return true;
		}

		private static string MakeDigits(int num)
		{
			return num < 10 ? "0" + num : num.ToString();
		}

		private double GetSeconds(int frameCount)
		{
			double frames = frameCount;
			
			if (frames < 1)
			{
				return 0;
			}

			var system = Header[HeaderKeys.PLATFORM];
			var pal = Header.ContainsKey(HeaderKeys.PAL) &&
				Header[HeaderKeys.PAL] == "1";

			return frames / this.FrameRates[system, pal];
		}

		public double Fps
		{
			get
			{
				var system = Header[HeaderKeys.PLATFORM];
				var pal = Header.ContainsKey(HeaderKeys.PAL) &&
					Header[HeaderKeys.PAL] == "1";

				return FrameRates[system, pal];
			}
		}

		#endregion

		private readonly PlatformFrameRates _frameRates = new PlatformFrameRates();
		public PlatformFrameRates FrameRates
		{
			get { return _frameRates; }
		}
	}
}