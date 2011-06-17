using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
	public enum MOVIEMODE { INACTIVE, PLAY, RECORD, FINISHED };
	public class Movie
	{
		private MovieHeader Header = new MovieHeader();
		private MovieLog Log = new MovieLog();
        
		private bool IsText = true;
		private string Filename;

		private MOVIEMODE MovieMode = new MOVIEMODE();

        public MultitrackRecording MultiTrack = new MultitrackRecording();
		public int Frames = 0;
		public int lastLog;
		public int rerecordCount;

		//TODO:
		//Author field, needs to be passed in by a record or play dialog

		public Movie(string filename, MOVIEMODE m)
		{
			Filename = filename;    //TODO: Validate that file is writable
			MovieMode = m;
			lastLog = 0;
			rerecordCount = 0;
		}

		public Movie()
		{
			Filename = ""; //Note: note this must be populated before playing movie
			MovieMode = MOVIEMODE.INACTIVE;
		}

		public string GetFilePath()
		{
			return Filename;
		}

		public string GetSysID()
		{
			return Header.GetHeaderLine(MovieHeader.PLATFORM);
		}

		public string GetGameName()
		{
			return Header.GetHeaderLine(MovieHeader.GAMENAME);
		}
        public int GetLength()
        {
            return Log.Length();
        }

		public void StopMovie()
		{
			if (MovieMode == MOVIEMODE.RECORD)
				WriteMovie();
			MovieMode = MOVIEMODE.INACTIVE;
		}

		public void StartNewRecording()
		{
			MovieMode = MOVIEMODE.RECORD;
			Log.Clear();
			Header = new MovieHeader(MainForm.EMUVERSION, MovieHeader.MovieVersion, Global.Emulator.SystemId, Global.Game.Name, "", 0);
		}

		public void StartPlayback()
		{
			MovieMode = MOVIEMODE.PLAY;
		}

		public MOVIEMODE GetMovieMode()
		{
			return MovieMode;
		}

		public void GetMnemonic()
		{
            if (MultiTrack.isActive)
            {
                if (MovieMode == MOVIEMODE.RECORD)
                {
             
                    if (Global.Emulator.Frame < Log.Length())                                                                
                        Log.ReplaceFrameAt(Global.ActiveController.GetControllersAsMnemonic(),Global.Emulator.Frame);
                    else
                        Log.AddFrame(Global.ActiveController.GetControllersAsMnemonic());
                }
            }
            else
                if (MovieMode == MOVIEMODE.RECORD)
                {
             
                    if (Global.Emulator.Frame < Log.Length())
                    {
                        Log.Truncate(Global.Emulator.Frame);
                    }
                    //				if (Global.MainForm.TAStudio1.Engaged)
                    //					Log.AddFrame(Global.MainForm.TAStudio1.GetMnemonic());
                    //				else
                    Log.AddFrame(Global.ActiveController.GetControllersAsMnemonic());
                }
		}

		public string GetInputFrame(int frame)
		{
			lastLog = frame;
			if (frame < Log.GetMovieLength())
				return Log.GetFrame(frame);
			else
				return "";
		}

		//Movie editing tools may like to have something like this
		public void AppendFrame(string record)
		{
			Log.AddFrame(record);
		}

		public void InsertFrame(string record, int frame)
		{
			Log.AddFrameAt(record, frame);
		}

		public void WriteMovie()
		{
			Directory.CreateDirectory(new FileInfo(Filename).Directory.FullName);
			if (IsText)
				WriteText();
			else
				WriteBinary();
		}

		private void WriteText()
		{
			if (Filename.Length == 0) return;   //Nothing to write
			int length = Log.GetMovieLength();

			using (StreamWriter sw = new StreamWriter(Filename))
			{
				foreach (KeyValuePair<string, string> kvp in Header.HeaderParams)
				{
					sw.WriteLine(kvp.Key + " " + kvp.Value);
				}


				for (int x = 0; x < length; x++)
				{
					sw.WriteLine(Log.GetFrame(x));
				}
			}
		}

		private void WriteBinary()
		{

		}

		private string ParseHeader(string line, string headerName)
		{
			string str;
			int x = line.LastIndexOf(headerName) + headerName.Length;
			str = line.Substring(x + 1, line.Length - x - 1);
			return str;
		}

		private bool LoadText()
		{
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
					if (str == "")
					{
						continue;
					}
					else if (str.Contains(MovieHeader.EMULATIONVERSION))
					{
						str = ParseHeader(str, MovieHeader.EMULATIONVERSION);
						Header.AddHeaderLine(MovieHeader.EMULATIONVERSION, str);
					}
					else if (str.Contains(MovieHeader.MOVIEVERSION))
					{
						str = ParseHeader(str, MovieHeader.MOVIEVERSION);
						Header.AddHeaderLine(MovieHeader.MOVIEVERSION, str);
					}
					else if (str.Contains(MovieHeader.PLATFORM))
					{
						str = ParseHeader(str, MovieHeader.PLATFORM);
						Header.AddHeaderLine(MovieHeader.PLATFORM, str);
					}
					else if (str.Contains(MovieHeader.GAMENAME))
					{
						str = ParseHeader(str, MovieHeader.GAMENAME);
						Header.AddHeaderLine(MovieHeader.GAMENAME, str);
					}
					else if (str.Contains(MovieHeader.RERECORDS))
					{
						str = ParseHeader(str, MovieHeader.RERECORDS);
						Header.AddHeaderLine(MovieHeader.RERECORDS, str);
						try
						{
							rerecordCount = int.Parse(str);
						}
						catch
						{
							rerecordCount = 0;
						}
					}
					else if (str.Contains(MovieHeader.AUTHOR))
					{
						str = ParseHeader(str, MovieHeader.AUTHOR);
						Header.AddHeaderLine(MovieHeader.AUTHOR, str);
					}
					else if (str[0] == '|')
					{
						Log.AddFrame(str);
					}
					else
					{
						Header.Comments.Add(str);
					}
				}
			}

			return true;

		}

		public bool PreLoadText()
		{
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
				int length = 0;
				while ((str = sr.ReadLine()) != null)
				{
					length += str.Length + 1;
					if (str == "")
					{
						continue;
					}
					//TODO: don't reiterate this entire if chain, make a function called by this and loadmovie
					else if (str.Contains(MovieHeader.EMULATIONVERSION))
					{
						str = ParseHeader(str, MovieHeader.EMULATIONVERSION);
						Header.AddHeaderLine(MovieHeader.EMULATIONVERSION, str);
					}
					else if (str.Contains(MovieHeader.MOVIEVERSION))
					{
						str = ParseHeader(str, MovieHeader.MOVIEVERSION);
						Header.AddHeaderLine(MovieHeader.MOVIEVERSION, str);
					}
					else if (str.Contains(MovieHeader.PLATFORM))
					{
						str = ParseHeader(str, MovieHeader.PLATFORM);
						Header.AddHeaderLine(MovieHeader.PLATFORM, str);
					}
					else if (str.Contains(MovieHeader.GAMENAME))
					{
						str = ParseHeader(str, MovieHeader.GAMENAME);
						Header.AddHeaderLine(MovieHeader.GAMENAME, str);
					}
					else if (str.Contains(MovieHeader.RERECORDS))
					{
						str = ParseHeader(str, MovieHeader.RERECORDS);
						Header.AddHeaderLine(MovieHeader.RERECORDS, str);
					}
					else if (str.Contains(MovieHeader.AUTHOR))
					{
						str = ParseHeader(str, MovieHeader.AUTHOR);
						Header.AddHeaderLine(MovieHeader.AUTHOR, str);
					}
					else if (str[0] == '|')
					{
						int line = str.Length + 1;
						length -= line;
						int lines = (int)file.Length - length;
						this.Frames = lines / line;
						break;
					}
					else
					{
						Header.Comments.Add(str);
					}
				}
				sr.Close();
			}

			return true;
		}//Also this method is never called, can delete?  What is purpose?

		private bool LoadBinary()
		{
			return true;
		}

		public bool LoadMovie()
		{
			var file = new FileInfo(Filename);
			if (file.Exists == false) return false; //TODO: methods like writemovie will fail, some internal flag needs to prevent this
			//TODO: must determine if file is text or binary
			return LoadText();
		}

		public int GetMovieLength()
		{
			return Log.GetMovieLength();
		}

		public void DumpLogIntoSavestateText(TextWriter writer)
		{
			writer.WriteLine("[Input]");
			for (int x = 0; x < Log.Length(); x++)
				writer.WriteLine(Log.GetFrame(x));
			writer.WriteLine("[/Input]");
		}

		public void LoadLogFromSavestateText(TextReader reader)
		{
			//We are in record mode so replace the movie log with the one from the savestate
            if (!MultiTrack.isActive)
            {
                Log.Clear();
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    if (line.Trim() == "") continue;
                    if (line == "[Input]") continue;
                    if (line == "[/Input]") break;
                    if (line[0] == '|')
                        Log.AddFrame(line);
                }
            }
            else
            {
                int i = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    if (line.Trim() == "") continue;
                    if (line == "[Input]") continue;
                    if (line == "[/Input]") break;
                    if (line[0] == '|')
                    {
                        Log.ReplaceFrameAt(line, i);
                        i++;
                    }
                }
            }
			//TODO: we can truncate the movie down to the current frame now (in case the savestate has a larger input log)
			//However, VBA will load it all, then truncate on the next frame, do we want that?
			IncrementRerecordCount();
		}

		public void IncrementRerecordCount()
		{
			rerecordCount++;
			Header.UpdateRerecordCount(rerecordCount);
		}

		public int GetRerecordCount()
		{
			return rerecordCount;
		}

		public Dictionary<string, string> GetHeaderInfo()
		{
			return Header.HeaderParams;
		}

		public void SetMovieFinished()
		{
			if (MovieMode == MOVIEMODE.PLAY)
				MovieMode = MOVIEMODE.FINISHED;
		}

		public void SetHeaderLine(string key, string value)
		{
			Header.SetHeaderLine(key, value);
		}

		public string GetTime(bool preLoad)
		{
			string time = "";

			double seconds;
			if (preLoad)
				seconds = GetSeconds(Frames);
			else
				seconds = GetSeconds(Log.Length());
			int hours = ((int)seconds) / 3600;
			int minutes = (((int)seconds) / 60) % 60;
			double sec = seconds % 60;
			if (hours > 0)
				time += MakeDigits(hours) + ":";
			time += MakeDigits(minutes) + ":";
			time += Math.Round((decimal)sec, 2).ToString();
			return time;
		}

		private string MakeDigits(decimal num)
		{
			if (num < 10)
				return "0" + num.ToString();
			else
				return num.ToString();
		}

		private string MakeDigits(int num)
		{
			if (num < 10)
				return "0" + num.ToString();
			else
				return num.ToString();
		}

		private double GetSeconds(int frameCount)
		{   //Should these be placed somewhere more accessible?  Perhaps as a public dictionary object in MainForm?
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

		public int CheckTimeLines(StreamReader reader)
		{
			//This function will compare the movie data to the savestate movie data to see if they match
			//TODO: Will eventually check header data too such as GUI
			MovieLog l = new MovieLog();
			string line;
			while (true)
			{
				line = reader.ReadLine();
				if (line.Trim() == "") continue;
				else if (line == "[Input]") continue;
				else if (line == "[/Input]") break;
				else if (line[0] == '|')
					l.AddFrame(line);
			}

			for (int x = 0; x < Log.Length(); x++)
			{
				string xs = Log.GetFrame(x);
				string ys = l.GetFrame(x);
				//if (Log.GetFrame(x) != l.GetFrame(x))
				if (xs != ys)
					return x;
			}
			return -1;
		}

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
			string otherName = Path.GetFileName(Other.GetFilePath());
			string thisName = Path.GetFileName(this.Filename);

			return thisName.CompareTo(otherName);
		}

		private int CompareSysID(Movie Other)
		{
			string otherSysID = Other.GetSysID();
			string thisSysID = this.GetSysID();

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
			string otherGameName = Other.GetGameName();
			string thisGameName = this.GetGameName();

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
			int otherLength = Other.Frames;
			int thisLength = this.Frames;

			if (thisLength < otherLength)
				return -1;
			else if (thisLength > otherLength)
				return 1;
			else
				return 0;
		}
	}
}
