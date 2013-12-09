using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class TasMovie : IMovie
	{
		// TODO: pass source into 
		// TODO: preloading, or benchmark and see how much of a performaance gain it really is
		// TODO: support loop Offset
		// TODO: consider the fileformat of binary and lagged data
		private readonly NewMnemonicsGenerator _mg;
		private readonly IController _source = Global.MovieOutputHardpoint;

		public MovieRecord this[int index]
		{
			get { return _records[index]; }
		}

		public List<string> ActivePlayers { get; set; }

		public Dictionary<string, char> AvailableMnemonics
		{
			get
			{
				var mg = new NewMnemonicsGenerator(_source) { ActivePlayers = this.ActivePlayers };
				return mg.AvailableMnemonics;
			}
		}

		public void ToggleButton(int frame, string buttonName)
		{
			//_records[frame].Buttons[buttonName] ^= true; //TODO: be this clean but still fire the event
			_records[frame].SetButton(buttonName, !_records[frame].Buttons[buttonName]);

		}

		public void SetButton(int frame, string buttonName, bool value)
		{
			//_records[frame].Buttons[buttonName] = value; //TODO: be this clean but still fire the event
			_records[frame].SetButton(buttonName, value);
		}

		public bool IsPressed(int frame, string buttonName)
		{
			return _records[frame].Buttons[buttonName];
		}

		private void InputChanged(object sender, MovieRecord.InputEventArgs e)
		{
			//TODO: manage green zone
			Changes = true;

			if (OnChanged != null)
			{
				OnChanged(sender, e);
			}
		}

		#region Events

		public delegate void MovieEventHandler(object sender, MovieRecord.InputEventArgs e);
		public event MovieEventHandler OnChanged;

		#endregion

		#region Implementation

		public TasMovie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Filename = filename;
		}

		public TasMovie(bool startsFromSavestate = false)
		{
			Filename = String.Empty;
			Header = new MovieHeader { StartsFromSavestate = startsFromSavestate };
			_records = new MovieRecordList();
			_mode = Moviemode.Inactive;
			IsCountingRerecords = true;

			_mg = new NewMnemonicsGenerator(_source);
		}

		public string Filename { get; set; }

		public IMovieHeader Header { get; private set; }

		public bool IsActive
		{
			get { return _mode != Moviemode.Inactive; }
		}

		public bool IsPlaying
		{
			get { return _mode == Moviemode.Play || _mode == Moviemode.Finished; }
		}

		public bool IsRecording
		{
			get { return _mode == Moviemode.Record; }
		}

		public bool IsFinished
		{
			get { return _mode == Moviemode.Finished; }
		}

		public bool IsCountingRerecords { get; set; }

		public bool Changes { get; private set; }

		public bool Loaded
		{
			get { throw new NotImplementedException(); }
		}

		public TimeSpan Time
		{
			get
			{
				double dblseconds = GetSeconds(_records.Count);
				int seconds = (int)(dblseconds % 60);
				int days = seconds / 86400;
				int hours = seconds / 3600;
				int minutes = (seconds / 60) % 60;
				int milliseconds = (int)((dblseconds - seconds) * 1000);
				return new TimeSpan(days, hours, minutes, seconds, milliseconds);
			}
		}

		public double FrameCount
		{
			get { return _records.Count; }
		}

		public int InputLogLength
		{
			get { return _records.Count; }
		}

		public string GetInput(int frame)
		{
			if (frame < _records.Count)
			{
				if (frame >= 0)
				{
					return _mg.GenerateMnemonicString(_records[frame].Buttons);
				}
				else
				{
					return String.Empty;
				}
			}
			else
			{
				_mode = Moviemode.Finished;
				return String.Empty;
			}
		}

		public string GetInputLog()
		{
			return _records.ToString();
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

		public void StartNewPlayback()
		{
			_mode = Moviemode.Play;
			Global.Emulator.ClearSaveRam();
		}

		public void Stop(bool saveChanges = true)
		{
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || Changes)
				{
					Save();
				}
			}

			_mode = Moviemode.Inactive;
		}

		public void Truncate(int frame)
		{
			_records.Truncate(frame);
		}

		public void ClearFrame(int frame)
		{
			if (frame < _records.Count)
			{
				Changes = true;
				_records[frame].ClearInput();
			}
		}

		public void AppendFrame(IController source)
		{
			Changes = true;
			_mg.Source = source;
			var record = new MovieRecord(_mg.GetBoolButtons(), true);
			record.OnChanged += InputChanged;
			_records.Add(record);
		}

		public void RecordFrame(int frame, IController source)
		{
			if (_mode == Moviemode.Record)
			{
				Changes = true;
				if (Global.Config.VBAStyleMovieLoadState)
				{
					if (Global.Emulator.Frame < _records.Count)
					{
						_records.Truncate(Global.Emulator.Frame);
					}
				}

				if (frame < _records.Count)
				{
					PokeFrame(frame, source);
				}
				else
				{
					AppendFrame(source);
				}
			}
		}

		public void PokeFrame(int frame, IController source)
		{
			if (frame < _records.Count)
			{
				Changes = true;
				_mg.Source = source;
				_records[frame].SetInput(_mg.GetBoolButtons());
			}
		}

		// TODO:
		public double Fps
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void StartNewRecording()
		{
			SwitchToRecord();
			if (Global.Config.EnableBackupMovies && true/*TODO*/ && _records.Any())
			{
				// TODO
			}
		}

		public bool Load()
		{
			// there's a lot of common code here with SavestateManager.  refactor?
			using (BinaryStateLoader bw = BinaryStateLoader.LoadAndDetect(Filename))
			{
				if (bw == null)
					return false;

				Header.Clear();
				_records.Clear();

				bw.GetMovieHeaderRequired(
					delegate(Stream s)
					{
						StreamReader sr = new StreamReader(s);
						string line;
						while ((line = sr.ReadLine()) != null)
							if (!Header.ParseLineFromFile(line))
								Header.Comments.Add(line);
					});
				bw.GetInputLogRequired(
					delegate(Stream s)
					{
						StreamReader sr = new StreamReader(s);
						// TODO: deserialize input log here
					});

				if (Header.StartsFromSavestate)
				{
					// should we raise some sort of error if there's a savestate in the archive but Header.StartsFromSavestate is false?
					bw.GetCoreState(
						delegate(Stream s)
						{
							BinaryReader br = new BinaryReader(s);
							Global.Emulator.LoadStateBinary(br);
						},
						delegate(Stream s)
						{
							StreamReader sr = new StreamReader(s);
							Global.Emulator.LoadStateText(sr);
						});
				}
				bw.GetFrameBuffer(
					delegate(Stream s)
					{
						BinaryReader br = new BinaryReader(s);
						int i;
						var buff = Global.Emulator.VideoProvider.GetVideoBuffer();
						try
						{
							for (i = 0; i < buff.Length; i++)
							{
								int j = br.ReadInt32();
								buff[i] = j;
							}
						}
						catch (EndOfStreamException) { }
					});
			}
			return true;
		}

		public void Save()
		{
			// there's a lot of common code here with SavestateManager.  refactor?

			using (FileStream fs = new FileStream(Filename, FileMode.Create, FileAccess.Write))
			using (BinaryStateSaver bs = new BinaryStateSaver(fs))
			{
				bs.PutMovieHeader(
					delegate(Stream s)
					{
						StreamWriter sw = new StreamWriter(s);
						sw.WriteLine(Header.ToString());
						sw.Flush();
					});
				bs.PutInputLog(
					delegate(Stream s)
					{
						StreamWriter sw = new StreamWriter(s);
						sw.WriteLine(GetInputLog());
						sw.Flush();
					});
				if (Header.StartsFromSavestate)
				{
#if true
					bs.PutCoreStateText(
						delegate(Stream s)
						{
							StreamWriter sw = new StreamWriter(s);
							Global.Emulator.SaveStateText(sw);
							sw.Flush();
						});
#else
					bs.PutCoreStateBinary(
						delegate(Stream s)
						{
							BinaryWriter bw = new BinaryWriter(s);
							Global.Emulator.SaveStateBinary(bw);
							bw.Flush();
						});
#endif
				}
			}
			Changes = false;
		}

		public void SaveAs()
		{
			Changes = false;
			throw new NotImplementedException();
		}

		public bool CheckTimeLines(TextReader reader, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		public bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private

		private enum Moviemode { Inactive, Play, Record, Finished }
		private readonly MovieRecordList _records;
		private Moviemode _mode;
		private readonly PlatformFrameRates _frameRates = new PlatformFrameRates();

		private double GetSeconds(int frameCount)
		{
			double frames = frameCount;

			if (frames < 1)
			{
				return 0;
			}

			var system = Header[HeaderKeys.PLATFORM];
			var pal = Header.ContainsKey(HeaderKeys.PAL) && Header[HeaderKeys.PAL] == "1";

			return frames / _frameRates[system, pal];
		}

		#endregion
	}
}
