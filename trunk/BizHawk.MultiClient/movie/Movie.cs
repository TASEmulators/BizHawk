using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
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
			lastlog = 0;
			Rerecords = 0;
			this.Filename = filename;
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
			get
			{
				return rerecords;
			}
			set
			{
				rerecords = value;
				Header.SetHeaderLine(MovieHeader.RERECORDS, Rerecords.ToString());
			}
		}

		public string SysID
		{
			get
			{
				return Header.GetHeaderLine(MovieHeader.PLATFORM);
			}
		}

		public string GUID
		{
			get
			{
				return Header.GetHeaderLine(MovieHeader.GUID);
			}
		}

		public string GameName
		{
			get
			{
				return Header.GetHeaderLine(MovieHeader.GAMENAME);
			}
		}

		public int Frames
		{
			get
			{
				if (Loaded)
				{
					if (Loop)
					{
						return 999999; //TODO: rethink this, maybe return a nullable int, and on the client side check for null and act accordingly
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
			get
			{
				return startsfromsavestate;
			}
			set
			{
				startsfromsavestate = value;
				if (value == true)
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
			get
			{
				return Log.StateFirstIndex;
			}
		}

		public int StateLastIndex
		{
			get
			{
				return Log.StateLastIndex;
			}
		}

		public bool StateCapturing
		{
			get
			{
				return statecapturing;
			}
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

		public void Stop()
		{
			if (Mode == MOVIEMODE.RECORD)
			{
				WriteMovie();
			}

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
			
			Directory.CreateDirectory(new FileInfo(Filename).Directory.FullName);
			if (IsText)
			{
				WriteText(Filename);
			}
			else
			{
				WriteBinary(Filename);
			}
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
			BackupName = Global.Config.MoviesBackupPath + "\\" + Path.GetFileName(BackupName);

			Directory.CreateDirectory(new FileInfo(BackupName).Directory.FullName);

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
		public bool PreLoadText()
		{
			Loaded = false;
			var file = new FileInfo(Filename);

			if (file.Exists == false)
				return false;
			else
			{
				Header.Clear();
				Log.Clear();
			}

			using (StreamReader sr = file.OpenText())
			{
				string str = "";
				while ((str = sr.ReadLine()) != null)
				{
					if (str == "" || Header.AddHeaderFromLine(str))
						continue;
					if (str.StartsWith("subtitle") || str.StartsWith("sub"))
						Subtitles.AddSubtitle(str);
					else if (str[0] == '|')
					{
						string frames = sr.ReadToEnd();
						int length = str.Length;
						// Account for line breaks of either size.
						if (frames.IndexOf("\r\n") != -1)
							length++;
						length++;
						// Count the remaining frames and the current one.
						this.preload_framecount = (frames.Length / length) + 1;
						break;
					}
					else
						Header.Comments.Add(str);
				}
				sr.Close();
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
			lastlog = frame;
			
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
		}

		public void ClearFrame(int frame)
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			Log.SetFrameAt(frame, mg.GetEmptyMnemonic());
		}

		public void AppendFrame(string record)
		{
			Log.AppendFrame(record);
		}

		public void InsertFrame(string record, int frame)
		{
			Log.AddFrameAt(frame, record);
		}

		public void InsertBlankFrame(int frame)
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			Log.AddFrameAt(frame, mg.GetEmptyMnemonic());
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
		}

		public void TruncateMovie(int frame)
		{
			Log.TruncateMovie(frame);
		}

		#endregion

		#region Public Misc Methods

		public bool FrameLagged(int frame)
		{
			return Log.FrameLagged(frame);
		}

		public void CaptureState()
		{
			if (StateCapturing == true)
			{
				byte[] state = Global.Emulator.SaveStateBinary();
				Log.AddState(state);
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
					if (Global.MainForm.EmulatorPaused == true && frame > 0)
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
						if (frame - 1 < Log.StateCount)
						{
							Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Log.GetState(frame - 1))));
							Global.MainForm.UpdateFrame = true;
						}
					}
				}
			}
			else
			{
				Global.MainForm.UnpauseEmulator();
			}
		}

		public void CommitFrame(int frameNum, IController source)
		{
			//if (Global.Emulator.Frame < Log.Length())
			//{
			//    Log.Truncate(Global.Emulator.Frame);
			//}

			//Note: Truncation here instead of loadstate will make VBA style loadstates
			//(Where an entire movie is loaded then truncated on the next frame
			//this allows users to restore a movie with any savestate from that "timeline"

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
			var reader = new StreamReader(path);
			int stateFrame = 0;
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
			if (stateFrame > 0 && stateFrame < Log.Length)
			{
				Log.TruncateStates(stateFrame);
				Log.TruncateMovie(stateFrame);
			}
			else if (stateFrame > Log.Length) //Post movie savestate
			{
				Log.TruncateStates(Log.Length);
				Log.TruncateMovie(Log.Length);
				Mode = MOVIEMODE.FINISHED;
			}
			Rerecords++;
			reader.Close();
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
			time += Math.Round((decimal)sec, 2).ToString();
			
			return time;
		}

		public bool CheckTimeLines(string path, bool OnlyGUID)
		{
			//This function will compare the movie data to the savestate movie data to see if they match
			var reader = new StreamReader(path);

			MovieLog l = new MovieLog();
			string line;
			string GUID;
			int stateFrame = 0;
			while (true)
			{
				line = reader.ReadLine();
				if (line == null)
					return false;
				if (line.Trim() == "") continue;
				else if (line.Contains("GUID"))
				{
					GUID = ParseHeader(line, MovieHeader.GUID);
					if (Header.GetHeaderLine(MovieHeader.GUID) != GUID)
					{
						//GUID Mismatch error
						var result = MessageBox.Show(GUID + " : " + Header.GetHeaderLine(MovieHeader.GUID) + "\n" +
							"The savestate GUID does not match the current movie.  Proceed anyway?", "GUID Mismatch error",
							MessageBoxButtons.YesNo, MessageBoxIcon.Question);

						if (result == DialogResult.No)
						{
							reader.Close();
							return false;
						}
					}
					else if (OnlyGUID)
					{
						reader.Close();
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

			reader.BaseStream.Position = 0; //Reset position because this stream may be read again by other code

			if (OnlyGUID)
			{
				reader.Close();
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
				reader.Close();
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
					reader.Close();
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

			reader.Close();
			return true;
		}

		#endregion

		#region Private Fields

		private MovieLog Log = new MovieLog();
		private enum MOVIEMODE { INACTIVE, PLAY, RECORD, FINISHED };
		private MOVIEMODE Mode = MOVIEMODE.INACTIVE;
		private bool statecapturing;
		private bool startsfromsavestate;
		private int preload_framecount; //Not a a reliable number, used for preloading (when no log has yet been loaded), this is only for quick stat compilation for dialogs such as play movie
		private int lastlog;
		private int rerecords;

		#endregion

		#region Helpers

		private void WriteText(string file)
		{
			if (file.Length > 0)
			{
				int length = Log.Length;

				using (StreamWriter sw = new StreamWriter(file))
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
		}

		private void WriteBinary(string file)
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
				string str = "";
				string rerecordStr = "";

				while ((str = sr.ReadLine()) != null)
				{
					if (str == "")
					{
						continue;
					}

					if (str.Contains(MovieHeader.RERECORDS))
					{
						rerecordStr = ParseHeader(str, MovieHeader.RERECORDS);
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

					if (Header.AddHeaderFromLine(str))
						continue;

					if (str.Contains("LoopOffset"))
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

		private string MakeDigits(decimal num)
		{
			return MakeDigits((int)num);
		}

		private string MakeDigits(int num)
		{
			if (num < 10)
				return "0" + num.ToString();
			else
				return num.ToString();
		}

		private double GetSeconds(int frameCount)
		{
			const double NES_PAL = 50.006977968268290849;
			const double NES_NTSC = (double)60.098813897440515532;
			const double PCE = (7159090.90909090 / 455 / 263); //~59.826
			const double SMS_NTSC = (3579545 / 262.0 / 228.0);
			const double SMS_PAL = (3546893 / 313.0 / 228.0);
			const double NGP = (6144000.0 / (515 * 198));
			const double VBOY = (20000000 / (259 * 384 * 4));  //~50.273
			const double LYNX = 59.8;
			const double WSWAN = (3072000.0 / (159 * 256));
			double seconds = 0;
			double frames = (double)frameCount;
			if (frames < 1)
				return seconds;

			bool pal = false; //TODO: pal flag

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
				case "SNES":
					if (pal)
						return frames / NES_PAL;
					else
						return frames / NES_NTSC;
				case "PCE":
				case "PCECD":
					return frames / PCE;

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

		private bool IsStateFromAMovie(StreamReader reader)
		{
			while (true)
			{
				if (reader.ReadLine().Contains("GUID"))
					break;
				if (reader.EndOfStream)
					return false;
			}
			return true;
		}

		private static string ParseHeader(string line, string headerName)
		{
			string str;
			int x = line.LastIndexOf(headerName) + headerName.Length;
			str = line.Substring(x + 1, line.Length - x - 1);
			return str;
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
			string thisName = Path.GetFileName(this.Filename);

			return thisName.CompareTo(otherName);
		}

		private int CompareSysID(Movie Other)
		{
			string otherSysID = Other.SysID;
			string thisSysID = this.SysID;

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
			string thisGameName = this.GameName;

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
			int thisLength = this.preload_framecount;

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