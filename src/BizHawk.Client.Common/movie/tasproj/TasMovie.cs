using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal sealed partial class TasMovie : Bk2Movie, ITasMovie
	{
		public new const string Extension = "tasproj";
		private IInputPollable _inputPollable;

		/// <exception cref="InvalidOperationException">loaded core does not implement <see cref="IStatable"/></exception>
		internal TasMovie(string path, bool startsFromSavestate) : base(path)
		{
			Branches = new TasBranchCollection(this);
			ChangeLog = new TasMovieChangeLog(this);
			TasStateManager = new TasStateManager(this, Global.Config.DefaultTasStateManagerSettings);
			Header[HeaderKeys.MovieVersion] = "BizHawk v2.0 Tasproj v1.0";
			Markers = new TasMovieMarkerList(this);
			Markers.CollectionChanged += Markers_CollectionChanged;
			Markers.Add(0, startsFromSavestate ? "Savestate" : "Power on");
		}

		public override void Attach(IEmulator emulator)
		{
			if (!emulator.HasSavestates())
			{
				throw new InvalidOperationException($"A core must be able to provide an {nameof(IStatable)} service");
			}

			if (!emulator.CanPollInput())
			{
				throw new InvalidOperationException($"A core must be able to provide an {nameof(IInputPollable)} service");
			}

			_inputPollable = emulator.AsInputPollable();
			TasStateManager.Attach(emulator);
			base.Attach(emulator);
		}

		public IStringLog VerificationLog { get; } = StringLogUtil.MakeStringLog(); // For movies that do not begin with power-on, this is the input required to get into the initial state
		public ITasBranchCollection Branches { get; }
		public ITasSession Session { get; private set; } = new TasSession();

		public int LastEditedFrame { get; private set; } = -1;
		public bool LastPositionStable { get; set; } = true;
		public TasMovieMarkerList Markers { get; private set; }
		public bool BindMarkersToInput { get; set; }

		public TasLagLog LagLog { get; } = new TasLagLog();

		public override string PreferredExtension => Extension;
		public IStateManager TasStateManager { get; }

		public ITasMovieRecord this[int index]
		{
			get
			{
				var lagIndex = index + 1;
				var lagged = LagLog[lagIndex];
				if (lagged == null && Emulator.Frame == lagIndex)
				{
					lagged = _inputPollable.IsLagFrame;
				}

				return new TasMovieRecord
				{
					HasState = TasStateManager.HasState(index),
					LogEntry = GetInputLogEntry(index),
					Lagged = lagged,
					WasLagged = LagLog.History(lagIndex)
				};
			}
		}

		public override void StartNewRecording()
		{
			ClearTasprojExtras();
			Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
			ChangeLog = new TasMovieChangeLog(this);

			base.StartNewRecording();
		}

		// Removes lag log and greenzone after this frame
		private void InvalidateAfter(int frame)
		{
			var anyInvalidated = LagLog.RemoveFrom(frame);
			TasStateManager.Invalidate(frame + 1);
			if (anyInvalidated)
			{
				Changes = true;
			}
			LastEditedFrame = frame;

			if (anyInvalidated && Global.MovieSession.Movie.IsCountingRerecords)
			{
				Rerecords++;
			}
		}


		private (int Frame, IMovieController Controller) _displayCache = (-1, new Bk2Controller("", NullController.Instance.Definition));

		/// <summary>
		/// Returns the mnemonic value for boolean buttons, and actual value for floats,
		/// for a given frame and button.
		/// </summary>
		public string DisplayValue(int frame, string buttonName)
		{
			if (_displayCache.Frame != frame)
			{
				_displayCache = (frame, GetInputState(frame));
			}
			
			return CreateDisplayValueForButton(_displayCache.Controller, Emulator.SystemId, buttonName);
		}

		private static string CreateDisplayValueForButton(IController adapter, string systemId, string buttonName)
		{
			if (adapter.Definition.BoolButtons.Contains(buttonName))
			{
				return adapter.IsPressed(buttonName)
					? Bk2MnemonicLookup.Lookup(buttonName, systemId).ToString()
					: "";
			}

			if (adapter.Definition.AxisControls.Contains(buttonName))
			{
				return adapter.AxisValue(buttonName).ToString();
			}

			return "!";
		}

		public void GreenzoneCurrentFrame()
		{
			// todo: this isn't working quite right when autorestore is off and we're editing while seeking
			// but accounting for that requires access to Mainform.IsSeeking
			if (Emulator.Frame > LastEditedFrame)
			{
				// emulated a new frame, current editing segment may change now. taseditor logic
				LastPositionStable = false;
			}

			LagLog[Emulator.Frame] = _inputPollable.IsLagFrame;

			if (!TasStateManager.HasState(Emulator.Frame))
			{
				TasStateManager.Capture(Emulator.Frame == LastEditedFrame - 1);
			}
		}

		public void CopyLog(IEnumerable<string> log)
		{
			Log.Clear();
			foreach (var entry in log)
			{
				Log.Add(entry);
			}
		}

		public void CopyVerificationLog(IEnumerable<string> log)
		{
			foreach (string entry in log)
			{
				VerificationLog.Add(entry);
			}
		}

		public IStringLog GetLogEntries() => Log;

		private int? _timelineBranchFrame;

		// TODO: this is 99% copy pasting of bad code
		public override bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			errorMessage = "";
			int? stateFrame = null;

			var newLog = new List<string>();

			// We are in record mode so replace the movie log with the one from the savestate
			if (!Global.MovieSession.MultiTrack.IsActive)
			{
				_timelineBranchFrame = null;

				if (Global.Config.EnableBackupMovies && MakeBackup && Log.Count != 0)
				{
					SaveBackup();
					MakeBackup = false;
				}

				int counter = 0;
				while (true)
				{
					var line = reader.ReadLine();
					if (string.IsNullOrEmpty(line))
					{
						break;
					}

					if (line.Contains("Frame 0x")) // NES stores frame count in hex, yay
					{
						var split = line.Split('x');
						try
						{
							stateFrame = int.Parse(split[1], NumberStyles.HexNumber);
						}
						catch
						{
							errorMessage = "Savestate Frame number failed to parse";
							return false;
						}
					}
					else if (line.Contains("Frame "))
					{
						var split = line.Split(' ');
						try
						{
							stateFrame = int.Parse(split[1]);
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
					else if (line[0] == '|')
					{
						newLog.Add(line);
						if (!_timelineBranchFrame.HasValue && counter < Log.Count && line != Log[counter])
						{
							_timelineBranchFrame = counter;
						}

						counter++;
					}
				}

				Log.Clear();
				Log.AddRange(newLog);
			}
			else // Multitrack mode
			{
				// TODO: consider TimelineBranchFrame here, my thinking is that there's never a scenario to invalidate state/lag data during multitrack
				var i = 0;
				while (true)
				{
					var line = reader.ReadLine();
					if (line == null)
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
					else if (line.StartsWith("LogKey:"))
					{
						LogKey = line.Replace("LogKey:", "");
					}
					else if (line.StartsWith("|"))
					{
						SetFrame(i, line);
						i++;
					}
				}
			}

			if (!stateFrame.HasValue)
			{
				errorMessage = "Savestate Frame number failed to parse";
			}

			var stateFrameValue = stateFrame ?? 0;

			if (stateFrameValue > 0 && stateFrameValue < Log.Count)
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Truncate(stateFrameValue);
				}
			}
			else if (stateFrameValue > Log.Count) // Post movie savestate
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Truncate(Log.Count);
				}

				Mode = MovieMode.Finished;
			}

			if (IsCountingRerecords)
			{
				Rerecords++;
			}

			if (_timelineBranchFrame.HasValue)
			{
				LagLog.RemoveFrom(_timelineBranchFrame.Value);
				TasStateManager.Invalidate(_timelineBranchFrame.Value);
			}

			return true;
		}

		public void LoadBranch(TasBranch branch)
		{
			int? divergentPoint = Log.DivergentPoint(branch.InputLog);

			Log?.Dispose();
			Log = branch.InputLog.Clone();

			InvalidateAfter(divergentPoint ?? branch.InputLog.Count);

			if (BindMarkersToInput) // pretty critical not to erase them
			{
				Markers = branch.Markers;
			}

			Changes = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private bool _changes;
		public override bool Changes
		{
			get => _changes;
			protected set
			{
				if (_changes != value)
				{
					_changes = value;
					OnPropertyChanged("Changes");
				}
			}
		}

		// This event is Raised only when Changes is TOGGLED.
		private void OnPropertyChanged(string propertyName)
		{
			// Raising the event when FirstName or LastName property value changed
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void Markers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			Changes = true;
		}

		public void ClearChanges() => Changes = false;
		public void FlagChanges() => Changes = true;
	}
}
