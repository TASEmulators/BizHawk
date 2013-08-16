using System;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class Movie
	{
		#region Constructors

		public Movie(string filename)
		{
			Mode = MOVIEMODE.INACTIVE;
			Rerecords = 0;
			Filename = filename;
			IsText = true;
			preload_framecount = 0;
			IsCountingRerecords = true;
			StartsFromSavestate = false;
			if (filename.Length > 0)
				Loaded = true;
		}

		public Movie()
		{
			Filename = "";
			Mode = MOVIEMODE.INACTIVE;
			IsText = true;
			preload_framecount = 0;
			StartsFromSavestate = false;
			Loaded = false;
			IsCountingRerecords = true;
		}

		#endregion

		#region Properties
		public MovieHeader Header = new MovieHeader();
		public SubtitleList Subtitles = new SubtitleList();
		
		public bool MakeBackup = true; //make backup before altering movie
		public string Filename;
		public bool IsCountingRerecords;
		
		public bool Loaded { get; private set; }
		public bool IsText { get; private set; }
		public int LoopOffset = -1;
		public bool Loop
		{
			get
			{
				if (LoopOffset >= 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public int Rerecords
		{
			get { return rerecords; }
			set
			{
				rerecords = value;
				Header.SetHeaderLine(MovieHeader.RERECORDS, Rerecords.ToString());
			}
		}

		public string SysID
		{
			get { return Header.GetHeaderLine(MovieHeader.PLATFORM); }
		}

		public string GUID
		{
			get { return Header.GetHeaderLine(MovieHeader.GUID); }
		}

		public string GameName
		{
			get { return Header.GetHeaderLine(MovieHeader.GAMENAME); }
		}

		public int RawFrames
		{
			get
			{
				if (Loaded)
				{
					return Log.Length;
				}
				else
				{
					return preload_framecount;
				}
			}
		}

		public int? Frames
		{
			get
			{
				if (Loaded)
				{
					if (Loop)
					{
						return null;
					}
					else
					{
						return Log.Length;
					}
				}
				else
				{
					return preload_framecount;
				}
			}
		}

		public bool StartsFromSavestate
		{
			get { return startsfromsavestate; }
			set
			{
				startsfromsavestate = value;
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

		public int StateFirstIndex
		{
			get { return Log.StateFirstIndex; }
		}

		public int StateLastIndex
		{
			get { return Log.StateLastIndex; }
		}

		public bool StateCapturing
		{
			get { return statecapturing; }
			set
			{
				statecapturing = value;
				if (value == false)
				{
					Log.ClearStates();
				}
				
			}
		}

		#endregion

		#region Public Mode Methods

		public bool IsPlaying
		{
			get
			{
				if (Mode == MOVIEMODE.PLAY || Mode == MOVIEMODE.FINISHED)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool IsRecording
		{
			get
			{
				if (Mode == MOVIEMODE.RECORD)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool IsActive
		{
			get
			{
				if (Mode == MOVIEMODE.INACTIVE)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		public bool IsFinished
		{
			get
			{
				if (Mode == MOVIEMODE.FINISHED)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool HasChanges
		{
			get { return changes; }
		}

		/// <summary>
		/// Tells the movie to start recording from the beginning, this will clear sram, and the movie log
		/// </summary>
		/// <param name="truncate"></param>
		public void StartRecording(bool truncate = true)
		{
			Global.MainForm.ClearSaveRAM();
			Mode = MOVIEMODE.RECORD;
			if (Global.Config.EnableBackupMovies && MakeBackup && Log.Length > 0)
			{
				WriteBackup();
				MakeBackup = false;
			}
			if (truncate)
			{
				Log.Clear();
			}
		}

		public void StartPlayback()
		{
			Global.MainForm.ClearSaveRAM();
			Mode = MOVIEMODE.PLAY;
		}

		/// <summary>
		/// Tells the movie to recording mode
		/// </summary>
		public void SwitchToRecord()
		{
			Mode = MOVIEMODE.RECORD;
		}

		/// <summary>
		/// Tells the movie to go into playback mode
		/// </summary>
		public void SwitchToPlay()
		{
			Mode = MOVIEMODE.PLAY;
			WriteMovie();
		}

		public void Stop(bool abortchanges = false)
		{
			if (!abortchanges)
			{
				if (Mode == MOVIEMODE.RECORD || changes)
				{
					WriteMovie();
				}
			}
			changes = false;
			Mode = MOVIEMODE.INACTIVE;
		}

		public void Finish()
		{
			if (Mode == MOVIEMODE.PLAY)
			{
				Mode = MOVIEMODE.FINISHED;
			}
		}
		
		#endregion

		#region Public File Handling

		public void WriteMovie(Stream stream)
		{
			if (!Loaded)
			{
				return;
			}

			var directory_info = new FileInfo(Filename).Directory;
			if (directory_info != null) Directory.CreateDirectory(directory_info.FullName);
			
			if (IsText)
			{
				WriteText(stream);
			}
			else
			{
				WriteBinary(stream);
			}
		}

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
			if (!Loaded)
			{
				return;
			}
			else if (Filename == "")
			{
				return;
			}

			WriteMovie(Filename);
			changes = false;
		}

		public void WriteBackup()
		{
			if (!Loaded)
			{
				return;
			}
			else if (Filename == "")
			{
				return;
			}

			string BackupName = Filename;
			BackupName = BackupName.Insert(Filename.LastIndexOf("."), String.Format(".{0:yyyy-MM-dd HH.mm.ss}", DateTime.Now));
			BackupName = Path.Combine(Global.Config.PathEntries["Global", "Movie backups"].Path, Path.GetFileName(BackupName));

			var directory_info = new FileInfo(BackupName).Directory;
			if (directory_info != null) Directory.CreateDirectory(directory_info.FullName);

			Global.OSD.AddMessage("Backup movie saved to " + BackupName);
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
		public bool PreLoadText(HawkFile file)
		{
			Loaded = false;
			Header.Clear();
			Log.Clear();
			
			StreamReader sr = new StreamReader(file.GetStream());
			string str = "";
            while ((str = sr.ReadLine()) != null)
				{
					if (str == "" || Header.AddHeaderFromLine(str))
					{
						continue;
					}

					if (str.StartsWith("subtitle") || str.StartsWith("sub"))
					{
						Subtitles.AddSubtitle(str);
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
						preload_framecount = (frames.Length/length) + 1;
						break;
					}
					else
					{
						Header.Comments.Add(str);
					}
				}

			sr.BaseStream.Position = 0; //Reset stream for others to use
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
			int getframe;

			if (Loop)
			{
				if (frame < Log.Length)
				{
					getframe = frame;
				}
				else
				{
					getframe = ((frame - LoopOffset) % (Log.Length - LoopOffset)) + LoopOffset;
				}
			}
			else
			{
				getframe = frame;
			}

			if (getframe < Log.Length)
			{
				return Log.GetFrame(getframe);
			}
			else
			{
				return "";
			}
		}

		public void ModifyFrame(string record, int frame)
		{
			Log.SetFrameAt(frame, record);
			changes = true;
		}

		public void ClearFrame(int frame)
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			Log.SetFrameAt(frame, mg.GetEmptyMnemonic);
			changes = true;
		}

		public void AppendFrame(string record)
		{
			Log.AppendFrame(record);
			changes = true;
		}

		public void InsertFrame(string record, int frame)
		{
			Log.AddFrameAt(frame, record);
			changes = true;
		}

		public void InsertBlankFrame(int frame)
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			Log.AddFrameAt(frame, mg.GetEmptyMnemonic);
			changes = true;
		}

		public void DeleteFrame(int frame)
		{
			if (frame <= StateLastIndex)
			{
				if (frame <= StateFirstIndex)
				{
					RewindToFrame(0);
				}
				else
				{
					RewindToFrame(frame);
				}
			}
			Log.DeleteFrame(frame);
			changes = true;
		}

		public void TruncateMovie(int frame)
		{
			Log.TruncateMovie(frame);
			Log.TruncateStates(frame);
			changes = true;
		}

		#endregion

		#region Public Misc Methods

		public MovieLog LogDump
		{
			get
			{
				return Log;
			}
		}

		public bool FrameLagged(int frame)
		{
			return Log.FrameLagged(frame);
		}

		public void CaptureState()
		{
			if (StateCapturing)
			{
				byte[] state = Global.Emulator.SaveStateBinary();
				Log.AddState(state);
				GC.Collect();
			}
		}

		public void RewindToFrame(int frame)
		{
			if (Mode == MOVIEMODE.INACTIVE || Mode == MOVIEMODE.FINISHED)
			{
				return;
			}
			if (frame <= Global.Emulator.Frame)
			{
				if (frame <= Log.StateFirstIndex)
				{
					Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Log.InitState)));
					if (Global.MainForm.EmulatorPaused && frame > 0)
					{
						Global.MainForm.UnpauseEmulator();
					}
					if (MOVIEMODE.RECORD == Mode)
					{
						Mode = MOVIEMODE.PLAY;
						Global.MainForm.RestoreReadWriteOnStop = true;
					}
				}
				else
				{
					if (frame == 0)
					{
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Log.InitState)));
					}
					else
					{
						//frame-1 because we need to go back an extra frame and then run a frame, otherwise the display doesn't get updated.
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Log.GetState(frame - 1))));
						Global.MainForm.UpdateFrame = true;
					}
				}
			}
			else if (frame <= Log.StateLastIndex)
			{
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Log.GetState(frame - 1))));
				Global.MainForm.UpdateFrame = true;
			}
			else
			{
				Global.MainForm.UnpauseEmulator();
			}
		}

		public void PokeFrame(int frameNum, string input)
		{
			changes = true;
			Log.SetFrameAt(frameNum, input);
		}

		public void CommitFrame(int frameNum, IController source)
		{
			//Note: Truncation here instead of loadstate will make VBA style loadstates
			//(Where an entire movie is loaded then truncated on the next frame
			//this allows users to restore a movie with any savestate from that "timeline"
			if (Global.Config.VBAStyleMovieLoadState)
			{
				if (Global.Emulator.Frame < Log.Length)
				{
					Log.TruncateMovie(Global.Emulator.Frame);
					Log .TruncateStates(Global.Emulator.Frame);
				}
			}
			changes = true;
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(source);
			Log.SetFrameAt(frameNum, mg.GetControllersAsMnemonic());
		}

		public void DumpLogIntoSavestateText(TextWriter writer)
		{
			writer.WriteLine("[Input]");
			string s = MovieHeader.GUID + " " + Header.GetHeaderLine(MovieHeader.GUID);
			writer.WriteLine(s);
			for (int x = 0; x < Log.Length; x++)
			{
				writer.WriteLine(Log.GetFrame(x));
			}
			writer.WriteLine("[/Input]");
		}

		public void LoadLogFromSavestateText(string path)
		{
			using (var reader = new StreamReader(path))
			{
				LoadLogFromSavestateText(reader);
			}
		}

		public void LoadLogFromSavestateText(TextReader reader)
		{
			int? stateFrame = null;
			//We are in record mode so replace the movie log with the one from the savestate
			if (!Global.MovieSession.MultiTrack.IsActive)
			{
				if (Global.Config.EnableBackupMovies && MakeBackup && Log.Length > 0)
				{
					WriteBackup();
					MakeBackup = false;
				}
				Log.Clear();
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
						catch { Global.OSD.AddMessage("Savestate Frame failed to parse"); } //TODO: message?
					}
					else if (line.Contains("Frame "))
					{
						string[] strs = line.Split(' ');
						try
						{
							stateFrame = int.Parse(strs[1]);
						}
						catch { Global.OSD.AddMessage("Savestate Frame failed to parse"); } //TODO: message?
					}
					if (line[0] == '|')
					{
						Log.AppendFrame(line);
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
						string[] strs = line.Split(' ');
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
						Log.SetFrameAt(i, line);
						i++;
					}
				}
			}
			if (stateFrame == null)
				throw new Exception("Couldn't find stateFrame");
			int stateFramei = (int)stateFrame;

			if (stateFramei > 0 && stateFramei < Log.Length)
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Log.TruncateStates(stateFramei);
					Log.TruncateMovie(stateFramei);
				}
			}
			else if (stateFramei > Log.Length) //Post movie savestate
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Log.TruncateStates(Log.Length);
					Log.TruncateMovie(Log.Length);
				}
				Mode = MOVIEMODE.FINISHED;
			}
			if (IsCountingRerecords)
				Rerecords++;
		}

		public string GetTime(bool preLoad)
		{
			string time = "";

			double seconds;
			if (preLoad)
			{
				seconds = GetSeconds(preload_framecount);
			}
			else
			{
				seconds = GetSeconds(Log.Length);
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

		public bool CheckTimeLines(TextReader reader, bool OnlyGUID)
		{
			//This function will compare the movie data to the savestate movie data to see if they match

			MovieLog l = new MovieLog();
			int stateFrame = 0;
			while (true)
			{
				string line = reader.ReadLine();
				if (line == null)
				{
					return false;
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
						//GUID Mismatch error
						var result = MessageBox.Show(guid + " : " + Header.GetHeaderLine(MovieHeader.GUID) + "\n" +
							"The savestate GUID does not match the current movie.  Proceed anyway?", "GUID Mismatch error",
							MessageBoxButtons.YesNo, MessageBoxIcon.Question);

						if (result == DialogResult.No)
						{
							//reader.Close();
							return false;
						}
					}
					else if (OnlyGUID)
					{
						//reader.Close();
						return true;
					}
				}
				else if (line.Contains("Frame 0x")) //NES stores frame count in hex, yay
				{
					string[] strs = line.Split('x');
					try
					{
						stateFrame = int.Parse(strs[1], NumberStyles.HexNumber);
					}
					catch { Global.OSD.AddMessage("Savestate Frame number failed to parse"); }
				}
				else if (line.Contains("Frame "))
				{
					string[] strs = line.Split(' ');
					try
					{
						stateFrame = int.Parse(strs[1]);
					}
					catch { Global.OSD.AddMessage("Savestate Frame number failed to parse"); }
				}
				else if (line == "[Input]") continue;
				else if (line == "[/Input]") break;
				else if (line[0] == '|')
					l.AppendFrame(line);
			}

			//reader.BaseStream.Position = 0; //Reset position because this stream may be read again by other code

			if (OnlyGUID)
			{
				//reader.Close();
				return true;
			}

			

			if (stateFrame == 0)
			{
				stateFrame = l.Length;  //In case the frame count failed to parse, revert to using the entire state input log
			}
			if (Log.Length < stateFrame)
			{
				//Future event error
				MessageBox.Show("The savestate is from frame " + l.Length.ToString() + " which is greater than the current movie length of " +
					Log.Length.ToString() + ".\nCan not load this savestate.", "Future event Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				//reader.Close();
				return false;
			}
			for (int x = 0; x < stateFrame; x++)
			{
				string xs = Log.GetFrame(x);
				string ys = l.GetFrame(x);
				if (xs != ys)
				{
					//TimeLine Error
					MessageBox.Show("The savestate input does not match the movie input at frame " + (x + 1).ToString() + ".",
						"Timeline Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					//reader.Close();
					return false;
				}
			}



			if (stateFrame > l.Length) //stateFrame is greater than state input log, so movie finished mode
			{
				if (Mode == MOVIEMODE.PLAY || Mode == MOVIEMODE.FINISHED)
				{
					Mode = MOVIEMODE.FINISHED;
					return true;
				}
				else
					return false; //For now throw an error if recording, ideally what should happen is that the state gets loaded, and the movie set to movie finished, the movie at its current state is preserved and the state is loaded just fine.  This should probably also only happen if checktimelines passes
			}
			else if (Mode == MOVIEMODE.FINISHED)
			{
				Mode = MOVIEMODE.PLAY;
			}

			//reader.Close();
			return true;
		}

		#endregion

		#region Private Fields

		private readonly MovieLog Log = new MovieLog();
		private enum MOVIEMODE { INACTIVE, PLAY, RECORD, FINISHED };
		private MOVIEMODE Mode = MOVIEMODE.INACTIVE;
		private bool statecapturing;
		private bool startsfromsavestate;
		private int preload_framecount; //Not a a reliable number, used for preloading (when no log has yet been loaded), this is only for quick stat compilation for dialogs such as play movie
		private int rerecords;
		private bool changes;
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
				Header.WriteText(sw);

				//TODO: clean this up
				if (LoopOffset >= 0)
				{
					sw.WriteLine("LoopOffset " + LoopOffset.ToString());
				}

				Subtitles.WriteText(sw);
				Log.WriteText(sw);
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
				Log.Clear();
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
							LoopOffset = int.Parse(str);
						}
						catch
						{
							//Do nothing
						}
					}
					else if (str.StartsWith("subtitle") || str.StartsWith("sub"))
					{
						Subtitles.AddSubtitle(str);
					}
					else if (Header.AddHeaderFromLine(str))
					{
						continue;
					}
					else if (str[0] == '|')
					{
						Log.AppendFrame(str);
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
			const double NES_PAL = 50.006977968268290849;
			const double NES_NTSC = 60.098813897440515532;
			const double SNES_NTSC = (double)21477272 / (4 * 341 * 262);
			const double SNES_PAL = (double)21281370 / (4 * 341 * 312);
			const double PCE = (7159090.90909090 / 455 / 263); //~59.826
			const double SMS_NTSC = (3579545 / 262.0 / 228.0);
			const double SMS_PAL = (3546893 / 313.0 / 228.0);
			const double NGP = (6144000.0 / (515 * 198));
			const double VBOY = (20000000 / (259 * 384 * 4));  //~50.273
			const double LYNX = 59.8;
			const double WSWAN = (3072000.0 / (159 * 256));
			const double GB = 262144.0 / 4389.0;
			const double A26 = 59.9227510135505;

			double frames = frameCount;
			
			if (frames < 1)
			{
				return 0;
			}

			bool pal = false;
			if (Header.HeaderParams.ContainsKey(MovieHeader.PAL))
				if (Header.HeaderParams[MovieHeader.PAL] == "1")
					pal = true;

			switch (Header.GetHeaderLine(MovieHeader.PLATFORM))
			{
				case "GG":
				case "SG":
				case "SMS":
					if (pal)
						return frames / SMS_PAL;
					else
						return frames / SMS_NTSC;
				case "FDS":
				case "NES":
					if (pal)
						return frames / NES_PAL;
					else
						return frames / NES_NTSC;
				case "SNES":
				case "SGB":
					if (pal)
						return frames / SNES_PAL;
					else
						return frames / SNES_NTSC;
				case "PCE":
				case "PCECD":
					return frames / PCE;
				case "GB":
				case "GBC":
				case "GBA":
					return frames / GB;
				case "A26":
				case "A78":
				case "Coleco":
					return frames / A26;

				//One Day!
				case "VBOY":
					return frames / VBOY;
				case "NGP":
					return frames / NGP;
				case "LYNX":
					return frames / LYNX;
				case "WSWAN":
					return frames / WSWAN;
				//********

				case "":
				default:
					if (pal)
						return frames / 50.0;
					else
						return frames / 60.0;
			}
		}

		private static string ParseHeader(string line, string headerName)
		{
			int x = line.LastIndexOf(headerName) + headerName.Length;
			return line.Substring(x + 1, line.Length - x - 1);
		}

		#endregion

		#region ComparisonLogic

		public int CompareTo(Movie Other, string parameter)
		{
			int compare = 0;
			if (parameter == "File")
			{
				compare = CompareFileName(Other);
				if (compare == 0)
				{
					compare = CompareSysID(Other);
					if (compare == 0)
					{
						compare = CompareGameName(Other);
						if (compare == 0)
							compare = CompareLength(Other);
					}
				}
			}
			else if (parameter == "SysID")
			{
				compare = CompareSysID(Other);
				if (compare == 0)
				{
					compare = CompareFileName(Other);
					if (compare == 0)
					{
						compare = CompareGameName(Other);
						if (compare == 0)
							compare = CompareLength(Other);
					}
				}
			}
			else if (parameter == "Game")
			{
				compare = CompareGameName(Other);
				if (compare == 0)
				{
					compare = CompareFileName(Other);
					if (compare == 0)
					{
						compare = CompareSysID(Other);
						if (compare == 0)
							compare = CompareLength(Other);
					}
				}
			}
			else if (parameter == "Length")
			{
				compare = CompareLength(Other);
				if (compare == 0)
				{
					compare = CompareFileName(Other);
					if (compare == 0)
					{
						compare = CompareSysID(Other);
						if (compare == 0)
							compare = CompareGameName(Other);
					}
				}
			}
			return compare;
		}

		private int CompareFileName(Movie Other)
		{
			string otherName = Path.GetFileName(Other.Filename);
			string thisName = Path.GetFileName(Filename);

			if (thisName != null)
			{
				return thisName.CompareTo(otherName);
			}
			else
			{
				return 0;
			}
		}

		private int CompareSysID(Movie Other)
		{
			string otherSysID = Other.SysID;
			string thisSysID = SysID;

			if (thisSysID == null && otherSysID == null)
				return 0;
			else if (thisSysID == null)
				return -1;
			else if (otherSysID == null)
				return 1;
			else
				return thisSysID.CompareTo(otherSysID);
		}

		private int CompareGameName(Movie Other)
		{
			string otherGameName = Other.GameName;
			string thisGameName = GameName;

			if (thisGameName == null && otherGameName == null)
				return 0;
			else if (thisGameName == null)
				return -1;
			else if (otherGameName == null)
				return 1;
			else
				return thisGameName.CompareTo(otherGameName);
		}

		private int CompareLength(Movie Other)
		{
			int otherLength = Other.preload_framecount;
			int thisLength = preload_framecount;

			if (thisLength < otherLength)
			{
				return -1;
			}
			else if (thisLength > otherLength)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		#endregion
	}
}