using System;
using System.IO;
using System.Globalization;

using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class Movie : IMovie
	{
		#region Constructors

		public Movie(string filename)
			: this()
		{
			Rerecords = 0;
			Filename = filename;
			Loaded = !String.IsNullOrWhiteSpace(filename);
		}

		public Movie()
		{
			Header = new MovieHeader();
			Subtitles = new SubtitleList();
			Filename = String.Empty;
			_preloadFramecount = 0;
			StartsFromSavestate = false;
			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			IsText = true;
			MakeBackup = true;
		}

		#endregion

		#region Properties
		public MovieHeader Header { get; private set; }
		public SubtitleList Subtitles { get; private set; }
		
		public bool MakeBackup { get; set; }
		public string Filename { get; set; }
		public bool IsCountingRerecords { get; set; }
		
		public bool Loaded { get; private set; }
		public bool IsText { get; private set; }

		public int Rerecords
		{
			get { return _rerecords; }
			set
			{
				_rerecords = value;
				Header.SetHeaderLine(MovieHeader.RERECORDS, Rerecords.ToString());
			}
		}

		public string SysID
		{
			get { return Header.GetHeaderLine(MovieHeader.PLATFORM); }
		}

		public string Guid
		{
			get { return Header.GetHeaderLine(MovieHeader.GUID); }
		}

		public string GameName
		{
			get { return Header.GetHeaderLine(MovieHeader.GAMENAME); }
		}

		public int RawFrames
		{
			get { return Loaded ? _log.Length : _preloadFramecount; }
		}

		public int? Frames
		{
			get
			{
				if (Loaded)
				{
					if (_loopOffset.HasValue)
					{
						return null;
					}
					else
					{
						return _log.Length;
					}
				}
				else
				{
					return _preloadFramecount;
				}
			}
		}

		public bool StartsFromSavestate
		{
			get { return _startsfromsavestate; }
			set
			{
				_startsfromsavestate = value;
				if (value)
				{
					Header.AddHeaderLine(MovieHeader.STARTSFROMSAVESTATE, "1");
				}
				else
				{
					Header.RemoveHeaderLine(MovieHeader.STARTSFROMSAVESTATE);
				}
			}
		}

		//TODO: these are getting too lengthy perhaps the log should just be exposed?
		public int StateFirstIndex
		{
			get { return _log.StateFirstIndex; }
		}

		public int StateLastIndex
		{
			get { return _log.StateLastIndex; }
		}

		public bool StateCapturing
		{
			get { return _statecapturing; }
			set
			{
				_statecapturing = value;
				if (value == false)
				{
					_log.ClearStates();
				}
			}
		}

		public byte[] GetState(int frame)
		{
			return _log.GetState(frame);
		}

		public byte[] InitState
		{
			get { return _log.InitState; }
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

		public bool HasChanges
		{
			get { return _changes; }
		}

		/// <summary>
		/// Tells the movie to start recording from the beginning, this will clear sram, and the movie log
		/// </summary>
		/// <param name="truncate"></param>
		public void StartRecording(bool truncate = true)
		{
			_mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && MakeBackup && _log.Length > 0)
			{
				WriteBackup();
				MakeBackup = false;
			}
			if (truncate)
			{
				_log.Clear();
			}
		}

		public void StartPlayback()
		{
			_mode = Moviemode.Play;
		}

		/// <summary>
		/// Tells the movie to recording mode
		/// </summary>
		public void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		/// <summary>
		/// Tells the movie to go into playback mode
		/// </summary>
		public void SwitchToPlay()
		{
			_mode = Moviemode.Play;
			WriteMovie();
		}

		public void Stop(bool abortchanges = false)
		{
			if (!abortchanges)
			{
				if (_mode == Moviemode.Record || _changes)
				{
					WriteMovie();
				}
			}
			_changes = false;
			_mode = Moviemode.Inactive;
		}

		/// <summary>
		/// If a movie is in playback mode, this will set it to movie finished
		/// </summary>
		public void Finish()
		{
			if (_mode == Moviemode.Play)
			{
				_mode = Moviemode.Finished;
			}
		}
		
		#endregion

		#region Public File Handling

		public void WriteMovie(string path)
		{
			if (!Loaded)
			{
				return;
			}
			var directory_info = new FileInfo(Filename).Directory;
			if (directory_info != null) Directory.CreateDirectory(directory_info.FullName);
			
			if (IsText)
			{
				WriteText(Filename);
			}
			else
			{
				WriteBinary(Filename);
			}
		}

		public void WriteMovie()
		{
			if (!Loaded || String.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			WriteMovie(Filename);
			_changes = false;
		}

		public void WriteBackup()
		{
			if (!Loaded || String.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			string BackupName = Filename;
			BackupName = BackupName.Insert(Filename.LastIndexOf("."), String.Format(".{0:yyyy-MM-dd HH.mm.ss}", DateTime.Now));
			BackupName = Path.Combine(Global.Config.PathEntries["Global", "Movie backups"].Path, Path.GetFileName(BackupName) ?? String.Empty);

			var directory_info = new FileInfo(BackupName).Directory;
			if (directory_info != null) Directory.CreateDirectory(directory_info.FullName);

			if (IsText)
			{
				WriteText(BackupName);
			}
			else
			{
				WriteBinary(BackupName);
			}
		}

		/// <summary>
		/// Load Header information only for displaying file information in dialogs such as play movie
		/// </summary>
		/// <returns></returns>
		public bool PreLoadText()
		{
			Loaded = false;
			var file = new FileInfo(Filename);

			if (file.Exists == false)
				return false;
			else
			{
				Header.Clear();
				_log.Clear();
			}

			using (StreamReader sr = file.OpenText())
			{
				string str;
				while ((str = sr.ReadLine()) != null)
				{
					if (String.IsNullOrWhiteSpace(str) || Header.AddHeaderFromLine(str))
					{
						continue;
					}

					if (str.StartsWith("subtitle") || str.StartsWith("sub"))
					{
						Subtitles.AddFromString(str);
					}
					else if (str[0] == '|')
					{
						string frames = sr.ReadToEnd();
						int length = str.Length;
						// Account for line breaks of either size.
						if (frames.IndexOf("\r\n") != -1)
						{
							length++;
						}

						length++;
						// Count the remaining frames and the current one.
						_preloadFramecount = (frames.Length/length) + 1;
						break;
					}
					else
					{
						Header.Comments.Add(str);
					}
				}
			}

			return true;
		}

		public bool LoadMovie()
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

		public void ModifyFrame(string record, int frame)
		{
			_log.SetFrameAt(frame, record);
			_changes = true;
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

		public void InsertFrame(string record, int frame)
		{
			_log.AddFrameAt(frame, record);
			_changes = true;
		}

		public void InsertBlankFrame(int frame)
		{
			_log.AddFrameAt(frame, MnemonicsGenerator.GetEmptyMnemonic);
			_changes = true;
		}

		public void DeleteFrame(int frame)
		{
			_log.DeleteFrame(frame);
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

		public MovieLog LogDump
		{
			get { return _log; }
		}

		public bool FrameLagged(int frame)
		{
			return _log.FrameLagged(frame);
		}

		public void CaptureState()
		{
			if (StateCapturing)
			{
				_log.AddState(Global.Emulator.SaveStateBinary());
				GC.Collect();
			}
		}

		public void PokeFrame(int frameNum, string input)
		{
			_changes = true;
			_log.SetFrameAt(frameNum, input);
		}

		public void CommitFrame(int frameNum, IController source)
		{
			//Note: Truncation here instead of loadstate will make VBA style loadstates
			//(Where an entire movie is loaded then truncated on the next frame
			//this allows users to restore a movie with any savestate from that "timeline"
			if (Global.Config.VBAStyleMovieLoadState)
			{
				if (Global.Emulator.Frame < _log.Length)
				{
					_log.TruncateMovie(Global.Emulator.Frame);
					_log .TruncateStates(Global.Emulator.Frame);
				}
			}
			_changes = true;
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(source);
			_log.SetFrameAt(frameNum, mg.GetControllersAsMnemonic());
		}

		public void DumpLogIntoSavestateText(TextWriter writer)
		{
			writer.WriteLine("[Input]");
			writer.WriteLine(MovieHeader.GUID + " " + Header.GetHeaderLine(MovieHeader.GUID));

			for (int x = 0; x < _log.Length; x++)
			{
				writer.WriteLine(_log[x]);
			}

			writer.WriteLine("[/Input]");
		}

		public void LoadLogFromSavestateText(TextReader reader, bool isMultitracking)
		{
			int? stateFrame = null;
			//We are in record mode so replace the movie log with the one from the savestate
			if (!isMultitracking)
			{
				if (Global.Config.EnableBackupMovies && MakeBackup && _log.Length > 0)
				{
					WriteBackup();
					MakeBackup = false;
				}
				_log.Clear();
				while (true)
				{
					string line = reader.ReadLine();
					if (line == null) break;
					else if (line.Trim() == "") continue;
					else if (line == "[Input]") continue;
					else if (line == "[/Input]") break;
					else if (line.Contains("Frame 0x")) //NES stores frame count in hex, yay
					{
						string[] strs = line.Split('x');
						try
						{
							stateFrame = int.Parse(strs[1], NumberStyles.HexNumber);
						}
						catch { } //TODO: message?
					}
					else if (line.Contains("Frame "))
					{
						string[] strs = line.Split(' ');
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
					string line = reader.ReadLine();
					if (line == null) break;
					else if (line.Trim() == "") continue;
					else if (line == "[Input]") continue;
					else if (line == "[/Input]") break;
					else if (line.Contains("Frame 0x")) //NES stores frame count in hex, yay
					{
						string[] strs = line.Split('x');
						try
						{
							stateFrame = int.Parse(strs[1], NumberStyles.HexNumber);
						}
						catch { } //TODO: message?
					}
					else if (line.Contains("Frame "))
					{
						string[] strs = line.Split(' ');
						try
						{
							stateFrame = int.Parse(strs[1]);
						}
						catch { } //TODO: message?
					}
					if (line[0] == '|')
					{
						_log.SetFrameAt(i, line);
						i++;
					}
				}
			}
			if (stateFrame == null)
				throw new Exception("Couldn't find stateFrame");
			int stateFramei = (int)stateFrame;

			if (stateFramei > 0 && stateFramei < _log.Length)
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					_log.TruncateStates(stateFramei);
					_log.TruncateMovie(stateFramei);
				}
			}
			else if (stateFramei > _log.Length) //Post movie savestate
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					_log.TruncateStates(_log.Length);
					_log.TruncateMovie(_log.Length);
				}
				_mode = Moviemode.Finished;
			}
			if (IsCountingRerecords)
				Rerecords++;
		}

		public string GetTime(bool preLoad)
		{
			string time = String.Empty;

			double seconds;
			if (preLoad)
			{
				seconds = GetSeconds(_preloadFramecount);
			}
			else
			{
				seconds = GetSeconds(_log.Length);
			}

			int hours = ((int)seconds) / 3600;
			int minutes = (((int)seconds) / 60) % 60;
			double sec = seconds % 60;
			if (hours > 0)
			{
				time += MakeDigits(hours) + ":";
			}
			
			time += MakeDigits(minutes) + ":";

			if (sec < 10) //Kludge
			{
				time += "0";
			}

			time += Math.Round((decimal)sec, 2).ToString();
			
			return time;
		}

		public LoadStateResult CheckTimeLines(TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage)
		{
			//This function will compare the movie data to the savestate movie data to see if they match
			errorMessage = String.Empty;
			var log = new MovieLog();
			int stateFrame = 0;
			while (true)
			{
				string line = reader.ReadLine();
				if (line == null)
				{
					return LoadStateResult.EmptyLog;
				}
				else if (line.Trim() == "")
				{
					continue;
				}
				else if (line.Contains("GUID"))
				{
					string guid = ParseHeader(line, MovieHeader.GUID);
					if (Header.GetHeaderLine(MovieHeader.GUID) != guid)
					{
						if (!ignoreGuidMismatch)
						{
							return LoadStateResult.GuidMismatch;
						}
					}
				}
				else if (line.Contains("Frame 0x")) //NES stores frame count in hex, yay
				{
					string[] strs = line.Split('x');
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
					string[] strs = line.Split(' ');
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
				else if (line == "[Input]") continue;
				else if (line == "[/Input]") break;
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
				stateFrame = log.Length;  //In case the frame count failed to parse, revert to using the entire state input log
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
						+ log.Length.ToString()
						+ " which is greater than the current movie length of "
						+ _log.Length.ToString();
					return LoadStateResult.FutureEventError;
				}
			}
			for (int i = 0; i < stateFrame; i++)
			{
				if (_log[i] != log[i])
				{
					errorMessage = "The savestate input does not match the movie input at frame "
						+ (i + 1).ToString()
						+ ".";
					return LoadStateResult.TimeLineError;
				}
			}

			if (stateFrame > log.Length) //stateFrame is greater than state input log, so movie finished mode
			{
				if (_mode == Moviemode.Play || _mode == Moviemode.Finished)
				{
					_mode = Moviemode.Finished;
					return LoadStateResult.Pass;
				}
				else
				{
					return LoadStateResult.NotInRecording; //TODO: For now throw an error if recording, ideally what should happen is that the state gets loaded, and the movie set to movie finished, the movie at its current state is preserved and the state is loaded just fine.  This should probably also only happen if checktimelines passes
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
		private bool _statecapturing;
		private bool _startsfromsavestate;
		private int _preloadFramecount; //Not a a reliable number, used for preloading (when no log has yet been loaded), this is only for quick stat compilation for dialogs such as play movie
		private int _rerecords;
		private bool _changes;
		private int? _loopOffset;

		#endregion

		#region Helpers

		private void WriteText(string fn)
		{
			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
				WriteText(fs);
		}

		private void WriteBinary(string fn)
		{
			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
				WriteBinary(fs);
		}

		private void WriteText(Stream stream)
		{
			using (StreamWriter sw = new StreamWriter(stream))
			{
				sw.Write(Header.ToString());

				//TODO: clean this up
				if (_loopOffset.HasValue)
				{
					sw.WriteLine("LoopOffset " + _loopOffset.ToString());
				}

				foreach (var subtitle in Subtitles)
				{
					sw.WriteLine(subtitle.ToString());
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

			using (StreamReader sr = file.OpenText())
			{
				string str;

				while ((str = sr.ReadLine()) != null)
				{
					if (str == "")
					{
						continue;
					}

					if (str.Contains(MovieHeader.RERECORDS))
					{
						string rerecordStr = ParseHeader(str, MovieHeader.RERECORDS);
						try
						{
							Rerecords = int.Parse(rerecordStr);
						}
						catch
						{
							Rerecords = 0;
						}
					}
					else if (str.Contains(MovieHeader.STARTSFROMSAVESTATE))
					{
						str = ParseHeader(str, MovieHeader.STARTSFROMSAVESTATE);
						if (str == "1")
							StartsFromSavestate = true;
					}

					else if (str.Contains("LoopOffset"))
					{
						str = ParseHeader(str, "LoopOffset");
						try
						{
							_loopOffset = int.Parse(str);
						}
						catch
						{
							//Do nothing
						}
					}
					else if (str.StartsWith("subtitle") || str.StartsWith("sub"))
					{
						Subtitles.AddFromString(str);
					}
					else if (Header.AddHeaderFromLine(str))
					{
						continue;
					}
					else if (str[0] == '|')
					{
						_log.AppendFrame(str);
					}
					else
					{
						Header.Comments.Add(str);
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

		private string MakeDigits(int num)
		{
			if (num < 10)
			{
				return "0" + num.ToString();
			}
			else
			{
				return num.ToString();
			}
		}

		private double GetSeconds(int frameCount)
		{
			double frames = frameCount;
			
			if (frames < 1)
			{
				return 0;
			}

			string system = Header.GetHeaderLine(MovieHeader.PLATFORM);
			bool pal = Header.HeaderParams.ContainsKey(MovieHeader.PAL) &&
				Header.HeaderParams[MovieHeader.PAL] == "1";

			return frames / _PlatformFrameRates[system, pal];
		}

		private static string ParseHeader(string line, string headerName)
		{
			int x = line.LastIndexOf(headerName) + headerName.Length;
			return line.Substring(x + 1, line.Length - x - 1);
		}

		public double Fps
		{
			get
			{
				string system = Header.GetHeaderLine(MovieHeader.PLATFORM);
				bool pal = Header.HeaderParams.ContainsKey(MovieHeader.PAL) &&
					Header.HeaderParams[MovieHeader.PAL] == "1";

				return _PlatformFrameRates[system, pal];
			}
		}

		#endregion

		private PlatformFrameRates _platformFrameRates = new PlatformFrameRates();
		public PlatformFrameRates _PlatformFrameRates
		{
			get { return _platformFrameRates; }
		}

		public class PlatformFrameRates
		{
			public double this[string systemId, bool pal]
			{
				get
				{
					string key = systemId + (pal ? "_PAL" : String.Empty);
					if (rates.ContainsKey(key))
					{
						return rates[key];
					}
					else
					{
						return 60.0;
					}
				}
			}

			private Dictionary<string, double> rates = new Dictionary<string, double>
			{
				{ "NES", 60.098813897440515532 },
				{ "NES_PAL", 50.006977968268290849 },
				{ "FDS", 60.098813897440515532 },
				{ "FDS_PAL", 50.006977968268290849 },
				{ "SNES", (double)21477272 / (4 * 341 * 262) },
				{ "SNES_PAL", (double)21281370 / (4 * 341 * 312) },
				{ "SGB", (double)21477272 / (4 * 341 * 262) },
				{ "SGB_PAL", (double)21281370 / (4 * 341 * 312) },
				{ "PCE", (7159090.90909090 / 455 / 263) }, //~59.826
				{ "PCECD", (7159090.90909090 / 455 / 263) }, //~59.826
				{ "SMS", (3579545 / 262.0 / 228.0) },
				{ "SMS_PAL", (3546893 / 313.0 / 228.0) },
				{ "GG", (3579545 / 262.0 / 228.0) },
				{ "GG_PAL", (3546893 / 313.0 / 228.0) },
				{ "SG", (3579545 / 262.0 / 228.0) },
				{ "SG_PAL", (3546893 / 313.0 / 228.0) },
				{ "NGP", (6144000.0 / (515 * 198)) },
				{ "VBOY", (20000000 / (259 * 384 * 4)) },  //~50.273
				{ "LYNX", 59.8 },
				{ "WSWAN", (3072000.0 / (159 * 256)) },
				{ "GB", 262144.0 / 4389.0 },
				{ "GBC", 262144.0 / 4389.0 },
				{ "GBA", 262144.0 / 4389.0 },
				{ "A26", 59.9227510135505 },
				{ "A78", 59.9227510135505 },
				{ "Coleco", 59.9227510135505 }
			};
		}
	}
}