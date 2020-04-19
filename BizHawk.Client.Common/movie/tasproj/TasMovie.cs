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
		public IStringLog VerificationLog { get; } = StringLogUtil.MakeStringLog(); // For movies that do not begin with power-on, this is the input required to get into the initial state
		public ITasBranchCollection Branches { get; }
		public ITasSession Session { get; private set; } = new TasSession();

		public new const string Extension = "tasproj";
		public string NewBranchText { get; set; } = "";
		public int LastEditedFrame { get; private set; } = -1;
		public bool LastPositionStable { get; set; } = true;
		public TasMovieMarkerList Markers { get; private set; }
		public bool BindMarkersToInput { get; set; }
		public int CurrentBranch { get; set; } = -1;

		public TasLagLog LagLog { get; } = new TasLagLog();

		public int LastStatedFrame => TasStateManager.Last;
		public override string PreferredExtension => Extension;
		public IStateManager TasStateManager { get; }

		public IStringLog CloneInput() => Log.Clone();

		/// <exception cref="InvalidOperationException">loaded core does not implement <see cref="IStatable"/></exception>
		public TasMovie(string path = null, bool startsFromSavestate = false) : base(path)
		{
			if (!Global.Emulator.HasSavestates())
			{
				throw new InvalidOperationException($"Cannot create a {nameof(TasMovie)} against a core that does not implement {nameof(IStatable)}");
			}

			Branches = new TasBranchCollection(this);
			ChangeLog = new TasMovieChangeLog(this);
			TasStateManager = new TasStateManager(this, Global.Config.DefaultTasStateManagerSettings);
			Header[HeaderKeys.MovieVersion] = "BizHawk v2.0 Tasproj v1.0";
			Markers = new TasMovieMarkerList(this);
			Markers.CollectionChanged += Markers_CollectionChanged;
			Markers.Add(0, startsFromSavestate ? "Savestate" : "Power on");
		}

		public ITasMovieRecord this[int index] => new TasMovieRecord
		{
			HasState = TasStateManager.HasState(index),
			LogEntry = GetInputLogEntry(index),
			Lagged = LagLog[index + 1],
			WasLagged = LagLog.History(index + 1)
		};


		public override void StartNewRecording()
		{
			ClearTasprojExtras();
			Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
			ChangeLog = new TasMovieChangeLog(this);

			base.StartNewRecording();
		}

		/// <summary>
		/// Removes lag log and greenzone after this frame
		/// </summary>
		/// <param name="frame">The last frame that can be valid.</param>
		private void InvalidateAfter(int frame)
		{
			var anyInvalidated = LagLog.RemoveFrom(frame);
			TasStateManager.Invalidate(frame + 1);
			Changes = anyInvalidated;
			LastEditedFrame = frame;

			if (anyInvalidated && Global.MovieSession.Movie.IsCountingRerecords)
			{
				Rerecords++;
			}
		}

		/// <summary>
		/// Returns the mnemonic value for boolean buttons, and actual value for floats,
		/// for a given frame and button.
		/// </summary>
		public string DisplayValue(int frame, string buttonName)
		{
			var adapter = GetInputState(frame);
			return CreateDisplayValueForButton(adapter, buttonName);
		}

		private string CreateDisplayValueForButton(IController adapter, string buttonName)
		{
			if (adapter.Definition.BoolButtons.Contains(buttonName))
			{
				return adapter.IsPressed(buttonName)
					? Bk2MnemonicLookup.Lookup(buttonName, Global.Emulator.SystemId).ToString()
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
			if (Global.Emulator.Frame > LastEditedFrame)
			{
				// emulated a new frame, current editing segment may change now. taseditor logic
				LastPositionStable = false;
			}

			LagLog[Global.Emulator.Frame] = Global.Emulator.AsInputPollable().IsLagFrame;

			if (!TasStateManager.HasState(Global.Emulator.Frame))
			{
				TasStateManager.Capture(Global.Emulator.Frame == LastEditedFrame - 1);
			}
		}

		public void ClearLagLog()
		{
			LagLog.Clear();
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

		#region Branches

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

		#endregion

		#region Events and Handlers

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

		#endregion
	}
}
