using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class TasMovie : IMovie
	{
		// TODO: pass source into 
		// TODO: preloading, or benchmark and see how much of a performaance gain it really is
		// TODO: support loop Offset
		// TODO: consider the fileformat of binary and lagged data
		private readonly IMnemonicPorts _mg;
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
				return _mg.AvailableMnemonics;
			}
		}

		public void ToggleButton(int frame, string buttonName)
		{
			InvalidateGreenzone(frame);
			_records[frame].SetButton(buttonName, !_records[frame].Buttons[buttonName]);

		}

		public void SetButton(int frame, string buttonName, bool value)
		{
			InvalidateGreenzone(frame);
			_records[frame].SetButton(buttonName, value);
		}

		public bool IsPressed(int frame, string buttonName)
		{
			return _records[frame].Buttons[buttonName];
		}

		private void InputChanged(object sender, MovieRecord.InputEventArgs e)
		{
			Changes = true;

			if (OnChanged != null)
			{
				OnChanged(sender, e);
			}
		}

		/// <summary>
		/// Removes the greenzone content after the given frame
		/// </summary>
		/// <param name="frame"></param>
		private void InvalidateGreenzone(int frame)
		{
			for (int i = frame + 1; i < _records.Count; i++)
			{
				_records[i].ClearState();
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
			_mg = MnemonicGeneratorFactory.Generate();
			Filename = String.Empty;
			Header = new MovieHeader { StartsFromSavestate = startsFromSavestate };
			Header[HeaderKeys.MOVIEVERSION] = HeaderKeys.MovieVersion2;
			_records = new MovieRecordList();
			_mode = Moviemode.Inactive;
			IsCountingRerecords = true;
		}

		public string Filename { get; set; }

		public IMovieHeader Header { get; private set; }

		public bool IsActive
		{
			get { return _mode != Moviemode.Inactive; }
		}

		public bool IsPlaying
		{
			get { return _mode == Moviemode.Play; }
		}

		public bool IsRecording
		{
			get { return _mode == Moviemode.Record; }
		}

		public bool IsFinished
		{
			get { return false; } //a TasMovie is never in this mode.
		}

		public bool IsCountingRerecords { get; set; }

		public bool Changes { get; set; }

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
					if (!_records[frame].HasState)
					{
						_records[frame].CaptureSate();
					}
					return _mg.GenerateMnemonicString(_records[frame].Buttons);
				}
				else
				{
					return String.Empty;
				}
			}
			else
			{
				_mode = Moviemode.Record;

				var buttons = _mg.ParseMnemonicString(_mg.EmptyMnemonic);

				_records.Add(new MovieRecord(buttons, true));
				return String.Empty;
			}
		}

		public string GetInputLog()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var record in _records)
			{
				sb.AppendLine(_mg.GenerateMnemonicString(record.Buttons));
			}
			return sb.ToString();
		}

		public void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		public void SwitchToPlay()
		{
			_mode = Moviemode.Play;
		}

		public void StartNewPlayback()
		{
			_mode = Moviemode.Play;
			Global.Emulator.ClearSaveRam();
		}

		public void Stop(bool saveChanges = true)
		{
			// adelikat: I think Tastudio should be in charge of saving, and so we should not attempt to manage any logic like that here
			// EmuHawk client UI assumes someone has already picked a filename ahead of time and that it is in charge of movies
			/*
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || Changes)
				{
					Save();
				}
			}
			*/
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
			InvalidateGreenzone(frame);
			if (frame < _records.Count)
			{
				Changes = true;
				_mg.Source = source;
				_records[frame].SetInput(_mg.GetBoolButtons());
			}
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

		public void StartNewRecording()
		{
			SwitchToRecord();

			// TODO: MakeBackup logic - Tastudio logic should be to always make backups before saving!

			if (Changes && !String.IsNullOrWhiteSpace(Filename))
			{
				Save();
			}

			_records.Clear();
			Header.Clear();
		}

		public bool Load()
		{
			var file = new FileInfo(Filename);
			if (!file.Exists)
			{
				return false;
			}
			// there's a lot of common code here with SavestateManager.  refactor?
			using (BinaryStateLoader bl = BinaryStateLoader.LoadAndDetect(Filename))
			{
				if (bl == null)
					return false;

				Header.Clear();
				_records.Clear();

				bl.GetLump(BinaryStateLump.Movieheader, true,
					delegate(TextReader tr)
					{
						string line;
						while ((line = tr.ReadLine()) != null)
							if (!Header.ParseLineFromFile(line))
								Header.Comments.Add(line);
					});
				bl.GetLump(BinaryStateLump.Input, true,
					delegate(TextReader tr)
					{
						string line = String.Empty;
						while (true)
						{
							line = tr.ReadLine();
							if (line == null)
							{
								break;
							}
							else if (line.StartsWith("|"))
							{
								var parsedButtons = _mg.ParseMnemonicString(line);
								_records.Add(new MovieRecord(parsedButtons, captureState: false));
							}
						}
					});

				if (Header.StartsFromSavestate)
				{
					// should we raise some sort of error if there's a savestate in the archive but Header.StartsFromSavestate is false?
					bl.GetCoreState(
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
				bl.GetLump(BinaryStateLump.Framebuffer, false,
					delegate(BinaryReader br)
					{
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

			_mode = Moviemode.Play;
			return true;
		}

		public void Save()
		{
			// there's a lot of common code here with SavestateManager.  refactor?

			using (FileStream fs = new FileStream(Filename, FileMode.Create, FileAccess.Write))
			using (BinaryStateSaver bs = new BinaryStateSaver(fs))
			{
				bs.PutLump(BinaryStateLump.Movieheader, (tw) => tw.WriteLine(Header.ToString()));
				bs.PutLump(BinaryStateLump.Input, (tw) => tw.WriteLine(GetInputLog()));
				if (Header.StartsFromSavestate)
				{
#if true
					bs.PutLump(BinaryStateLump.CorestateText, (tw) => Global.Emulator.SaveStateText(tw));
#else
					bs.PutLump(BinaryStateLump.Corestate, (bw) => Global.Emulator.SaveStateBinary(bw));
#endif
				}
			}
			Changes = false;
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
