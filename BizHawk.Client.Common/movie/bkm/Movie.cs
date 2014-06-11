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
		private readonly MovieLog _log = new MovieLog();
		private readonly PlatformFrameRates _frameRates = new PlatformFrameRates();
		private bool _makeBackup = true;

		private Moviemode _mode = Moviemode.Inactive;
		private int _preloadFramecount; // Not a a reliable number, used for preloading (when no log has yet been loaded), this is only for quick stat compilation for dialogs such as play movie
		private bool _changes;
		private int? _loopOffset;

		public Movie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Header.Rerecords = 0;
			Filename = filename;
			Loaded = !string.IsNullOrWhiteSpace(filename);
		}

		public Movie(bool startsFromSavestate = false)
		{
			Header = new MovieHeader();
			Filename = string.Empty;
			_preloadFramecount = 0;
			Header.StartsFromSavestate = startsFromSavestate;
			
			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			_makeBackup = true;
		}

		private enum Moviemode { Inactive, Play, Record, Finished }

		#region Properties

		public SubtitleList Subtitles
		{
			get { return (Header as MovieHeader).Subtitles; }
		}

		public IList<string> Comments
		{
			get { return (Header as MovieHeader).Comments; }
		}

		public string SyncSettingsJson
		{
			get
			{
				return Header[HeaderKeys.SYNCSETTINGS];
			}

			set
			{
				Header[HeaderKeys.SYNCSETTINGS] = value;
			}
		}

		public string PreferredExtension { get { return "bkm"; } }

		// TODO: delete me
		public static string Extension { get { return "bkm"; } }

		public IMovieHeader Header { get; private set; }

		public string Filename { get; set; }
		public bool IsCountingRerecords { get; set; }
		
		public bool Loaded { get; private set; }

		public int InputLogLength
		{
			get { return _log.Length; }
		}

		public double FrameCount
		{
			get
			{
				if (_loopOffset.HasValue)
				{
					return double.PositiveInfinity;
				}
				
				if (Loaded)
				{
					return _log.Length;
				}

				return _preloadFramecount;
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
			// adelikat: ClearSaveRam shouldn't be here at all most likely, especially considering this is an implementation detail
			// If Starting a new recording requires clearing sram it shoudl be done at a higher layer and not rely on all IMovies doing this
			// Haven't removed it yet because I coudln't guarantee that power-on movies coudl live without it
			// And the immediate fire is that Savestate movies are breaking
			if (!Header.StartsFromSavestate) // && Global.Emulator.SystemId != "WSWAN")
			{
				Global.Emulator.ClearSaveRam();
			}

			_mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && _makeBackup && _log.Length > 0)
			{
				SaveBackup();
				_makeBackup = false;
			}

			_log.Clear();
		}

		public void StartNewPlayback()
		{
			// See StartNewRecording for details as to why this savestate check is here
			if (!Header.StartsFromSavestate) // && Global.Emulator.SystemId != "WSWAN")
			{
				Global.Emulator.ClearSaveRam();
			}

			_mode = Moviemode.Play;
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
			Filename = path;
			if (!Loaded)
			{
				return;
			}

			var directory_info = new FileInfo(Filename).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			Write(Filename);
		}

		public void Save()
		{
			if (!Loaded || string.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			SaveAs(Filename);
			_changes = false;
		}

		public void SaveBackup()
		{
			if (!Loaded || string.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			var backupName = Filename;
			backupName = backupName.Insert(Filename.LastIndexOf("."), string.Format(".{0:yyyy-MM-dd HH.mm.ss}", DateTime.Now));
			backupName = Path.Combine(Global.Config.PathEntries["Global", "Movie backups"].Path, Path.GetFileName(backupName) ?? string.Empty);

			var directory_info = new FileInfo(backupName).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			Write(backupName);
		}

		/// <summary>
		/// Load Header information only for displaying file information in dialogs such as play movie
		/// TODO - consider not loading the SavestateBinaryBase64Blob key?
		/// </summary>
		public bool PreLoadText(HawkFile hawkFile)
		{
			Loaded = false;
			var file = new FileInfo(hawkFile.CanonicalFullPath);

			if (file.Exists == false)
			{
				return false;
			}

			Header.Clear();
			_log.Clear();

			var origStreamPosn = hawkFile.GetStream().Position; 
			hawkFile.GetStream().Position = 0; // Reset to start

			// No using block because we're sharing the stream and need to give it back undisposed.
			var sr = new StreamReader(hawkFile.GetStream());
			
			for(;;)
			{
				//read to first space (key/value delimeter), or pipe, or EOF
				int first = sr.Read();
					
				if (first == -1) break; //EOF
				else if(first == '|') //pipe: begin input log
				{
					//NOTE - this code is a bit convoluted due to its predating the basic outline of the parser which was upgraded in may 2014
					string line = '|' + sr.ReadLine();

					//how many bytes are left, total?
					long remain = sr.BaseStream.Length - sr.BaseStream.Position;

					//try to find out whether we use \r\n or \n
					//but only look for 1K characters.
					bool usesR = false;
					for (int i = 0; i < 1024; i++)
					{
						int c = sr.Read();
						if (c == -1)
							break;
						if (c == '\r')
						{
							usesR = true;
							break;
						}
						if (c == '\n')
							break;
					}

					int lineLen = line.Length + 1; //account for \n
					if (usesR) lineLen++; //account for \r

					_preloadFramecount = (int)(remain / lineLen); //length is remaining bytes / length per line
					_preloadFramecount++; //account for the current line
					break;
				}
				else
				{
					//a header line. finish reading key token, to make sure it isn't one of the FORBIDDEN keys
					StringBuilder sbLine = new StringBuilder();
					sbLine.Append((char)first);
					for (; ; )
					{
						int c = sr.Read();
						if (c == -1) break;
						if (c == '\n') break;
						if (c == ' ') break;
						sbLine.Append((char)c);
					}

					string line = sbLine.ToString();

					//ignore these suckers, theyre way too big for preloading. seriously, we will get out of memory errors.
					bool skip = false;
					if (line == HeaderKeys.SAVESTATEBINARYBASE64BLOB) skip = true;

					if (skip)
					{
						//skip remainder of the line
						sr.DiscardBufferedData();
						var stream = sr.BaseStream;
						for (; ; )
						{
							int c = stream.ReadByte();
							if (c == -1) break;
							if (c == '\n') break;
						}
						//proceed to next line
						continue;
					}
						

					string remainder = sr.ReadLine();
					sbLine.Append(' ');
					sbLine.Append(remainder);
					line = sbLine.ToString();

					if (string.IsNullOrWhiteSpace(line) || Header.ParseLineFromFile(line))
						continue;
					(Header as MovieHeader).Comments.Add(line);
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
			
			Header.Clear();
			_log.Clear();

			using (var sr = file.OpenText())
			{
				string line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line == string.Empty)
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
						(Header as MovieHeader).Comments.Add(line);
					}
				}
			}

			Loaded = true;
			return true;
		}

		#endregion

		#region Public Log Editing

		public string GetInput(int frame)
		{
			if (frame < FrameCount)
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
				
				return string.Empty;
			}
			
			Finish();
			return string.Empty;
		}

		public void ClearFrame(int frame)
		{
			_log.SetFrameAt(frame, new MnemonicsGenerator().EmptyMnemonic);
			_changes = true;
		}

		public void AppendFrame(IController source)
		{
			var mg = new MnemonicsGenerator();
			mg.SetSource(source);
			_log.AppendFrame(mg.GetControllersAsMnemonic());
			_changes = true;
		}

		public void Truncate(int frame)
		{
			_log.TruncateMovie(frame);
			_log.TruncateStates(frame);
			_changes = true;
		}

		#endregion

		#region Public Misc Methods

		public void PokeFrame(int frame, IController source)
		{
			var mg = new MnemonicsGenerator();
			mg.SetSource(source);

			_changes = true;
			_log.SetFrameAt(frame, mg.GetControllersAsMnemonic());
		}

		public void RecordFrame(int frame, IController source)
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

			var mg = new MnemonicsGenerator();
			mg.SetSource(source);

			_changes = true;
			_log.SetFrameAt(frame, mg.GetControllersAsMnemonic());
		}

		public string GetInputLog()
		{
			var sb = new StringBuilder();

			sb.AppendLine("[Input]");

			foreach (var record in _log)
			{
				sb.AppendLine(record);
			}

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
				Header.Rerecords++;
			}

			return true;
		}

		public TimeSpan Time
		{
			get
			{
				var dblseconds = GetSeconds(Loaded ? _log.Length : _preloadFramecount);
				var seconds = (int)(dblseconds % 60);
				var days = seconds / 86400;
				var hours = seconds / 3600;
				var minutes = (seconds / 60) % 60;
				var milliseconds = (int)((dblseconds - seconds) * 1000);
				return new TimeSpan(days, hours, minutes, seconds, milliseconds);
			}
		}

		public bool CheckTimeLines(TextReader reader, out string errorMessage)
		{
			// This function will compare the movie data to the savestate movie data to see if they match
			errorMessage = string.Empty;
			var log = new MovieLog();
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

		#endregion

		#region Helpers

		private void Write(string fn)
		{
			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				using (var sw = new StreamWriter(fs))
				{
					sw.Write(Header.ToString());

					// TODO: clean this up
					if (_loopOffset.HasValue)
					{
						sw.WriteLine("LoopOffset " + _loopOffset);
					}

					foreach (var input in _log)
					{
						sw.WriteLine(input);
					}
				}
			}
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

			return frames / _frameRates[system, pal];
		}

		public double Fps
		{
			get
			{
				var system = Header[HeaderKeys.PLATFORM];
				var pal = Header.ContainsKey(HeaderKeys.PAL) &&
					Header[HeaderKeys.PAL] == "1";

				return _frameRates[system, pal];
			}
		}

		#endregion
	}
}