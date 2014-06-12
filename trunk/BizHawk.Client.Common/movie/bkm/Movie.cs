using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Movie : IMovie
	{
		private readonly PlatformFrameRates _frameRates = new PlatformFrameRates();
		private bool _makeBackup = true;
		private bool _changes;
		private int? _loopOffset;

		public Movie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Rerecords = 0;
			Filename = filename;
			Loaded = !string.IsNullOrWhiteSpace(filename);
		}

		public Movie(bool startsFromSavestate = false)
		{
			Header = new MovieHeader();
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v0.0.1";
			Filename = string.Empty;
			_preloadFramecount = 0;
			StartsFromSavestate = startsFromSavestate;

			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			_makeBackup = true;
		}

		#region Properties

		public string PreferredExtension { get { return "bkm"; } }
		public MovieHeader Header { get; private set; }
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

		public bool Changes
		{
			get { return _changes; }
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

		#endregion

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
	}
}