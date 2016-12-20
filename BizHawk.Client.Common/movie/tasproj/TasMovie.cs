using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public sealed partial class TasMovie : Bk2Movie, INotifyPropertyChanged
	{
		public const string DefaultProjectName = "default";

		private readonly Bk2MnemonicConstants Mnemonics = new Bk2MnemonicConstants();
		private readonly TasStateManager StateManager;
		public readonly TasSession Session;
		private readonly TasLagLog LagLog = new TasLagLog();
		private readonly Dictionary<int, IController> InputStateCache = new Dictionary<int, IController>();
		public readonly IStringLog VerificationLog = StringLogUtil.MakeStringLog(); // For movies that do not begin with power-on, this is the input required to get into the initial state
		public readonly TasBranchCollection Branches = new TasBranchCollection();

		public BackgroundWorker _progressReportWorker = null;
		public void NewBGWorker(BackgroundWorker newWorker)
		{
			_progressReportWorker = newWorker;
		}

		public int LastValidFrame
		{
			get { return LagLog.LastValidFrame; }
		}

		public TasMovie(string path, bool startsFromSavestate = false, BackgroundWorker progressReportWorker = null)
			: base(path)
		{
			// TODO: how to call the default constructor AND the base(path) constructor?  And is base(path) calling base() ?
			_progressReportWorker = progressReportWorker;
			if (!Global.Emulator.HasSavestates())
			{
				throw new InvalidOperationException("Cannot create a TasMovie against a core that does not implement IStatable");
			}

			ChangeLog = new TasMovieChangeLog(this);

			StateManager = new TasStateManager(this);
			Session = new TasSession(this);
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0 Tasproj v1.0";
			Markers = new TasMovieMarkerList(this);
			Markers.CollectionChanged += Markers_CollectionChanged;
			Markers.Add(0, startsFromSavestate ? "Savestate" : "Power on");

			BindMarkersToInput = true;
			CurrentBranch = -1;
		}

		public TasMovie(bool startsFromSavestate = false, BackgroundWorker progressReportWorker = null)
			: base()
		{
			_progressReportWorker = progressReportWorker;
			if (!Global.Emulator.HasSavestates())
			{
				throw new InvalidOperationException("Cannot create a TasMovie against a core that does not implement IStatable");
			}

			ChangeLog = new TasMovieChangeLog(this);

			StateManager = new TasStateManager(this);
			Session = new TasSession(this);
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0 Tasproj v1.0";
			Markers = new TasMovieMarkerList(this);
			Markers.CollectionChanged += Markers_CollectionChanged;
			Markers.Add(0, startsFromSavestate ? "Savestate" : "Power on");

			BindMarkersToInput = true;
			CurrentBranch = -1;
		}

		public TasLagLog TasLagLog { get { return LagLog; } }
		public IStringLog InputLog { get { return _log; } }
		public TasMovieMarkerList Markers { get; set; }
		public bool BindMarkersToInput { get; set; }
		public bool UseInputCache { get; set; }
		public bool LastPositionStable = true;
		public string NewBranchText = "";
		public int CurrentBranch { get; set; }
		public int BranchCount { get { return Branches.Count; } }

		public TasBranch GetBranch(int index)
		{
			if (index >= Branches.Count || index < 0)
				return null;
			else
				return Branches[index];
		}

		public int BranchHashByIndex(int index)
		{
			if (index >= Branches.Count)
				return -1;
			else
				return Branches[index].UniqueIdentifier.GetHashCode();
		}

		public int BranchIndexByHash(int hash)
		{
			TasBranch branch = Branches.Where(b => b.UniqueIdentifier.GetHashCode() == hash).SingleOrDefault();
			if (branch == null)
				return -1;
			return Branches.IndexOf(branch);
		}

		public int BranchIndexByFrame(int frame)
		{
			TasBranch branch = Branches.Where(b => b.Frame == frame)
				.OrderByDescending(b => b.TimeStamp).FirstOrDefault();
			if (branch == null)
				return -1;
			return Branches.IndexOf(branch);
		}

		public override string PreferredExtension
		{
			get { return Extension; }
		}

		public TasStateManager TasStateManager
		{
			get { return StateManager; }
		}

		public new const string Extension = "tasproj";

		public TasMovieRecord this[int index]
		{
			get
			{
				return new TasMovieRecord
				{
					// State = StateManager[index],
					HasState = StateManager.HasState(index),
					LogEntry = GetInputLogEntry(index),
					Lagged = LagLog[index + 1],
					WasLagged = LagLog.History(index + 1)
				};
			}
		}

		public void ReportProgress(double percent)
		{
			if (percent > 100d)
				return;
			if (_progressReportWorker != null)
			{
				_progressReportWorker.ReportProgress((int)percent);
			}
		}

		#region Events and Handlers

		public event PropertyChangedEventHandler PropertyChanged;

		private bool _changes;
		public override bool Changes
		{
			get { return _changes; }
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
			if (PropertyChanged != null)
			{
				// Raising the event when FirstName or LastName property value changed
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		void Markers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			Changes = true;
		}

		#endregion

		public void ClearChanges()
		{
			Changes = false;
		}

		public void FlagChanges()
		{
			Changes = true;
		}

		public override void StartNewRecording()
		{
			ClearTasprojExtras();
			Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
			ChangeLog = new TasMovieChangeLog(this);

			base.StartNewRecording();
		}

		public override void SwitchToPlay()
		{
			_mode = Moviemode.Play;
		}

		public override void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		/// <summary>
		/// Removes lag log and greenzone after this frame
		/// </summary>
		/// <param name="frame">The last frame that can be valid.</param>
		private void InvalidateAfter(int frame)
		{
			var anyInvalidated = LagLog.RemoveFrom(frame);
			StateManager.Invalidate(frame + 1);
			Changes = true; // TODO check if this actually removed anything before flagging changes

            if (anyInvalidated && Global.MovieSession.Movie.IsCountingRerecords)
			{
				base.Rerecords++;
			}
		}

		/// <summary>
		/// Returns the mnemonic value for boolean buttons, and actual value for floats,
		/// for a given frame and button.
		/// </summary>
		public string DisplayValue(int frame, string buttonName)
		{
			if (UseInputCache && InputStateCache.ContainsKey(frame))
			{
				return CreateDisplayValueForButton(InputStateCache[frame], buttonName);
			}

			var adapter = GetInputState(frame);

			if (UseInputCache)
			{
				InputStateCache.Add(frame, adapter);
			}

			return CreateDisplayValueForButton(adapter, buttonName);
		}

		public void FlushInputCache()
		{
			InputStateCache.Clear();
		}

		public string CreateDisplayValueForButton(IController adapter, string buttonName)
		{
			if (adapter.Definition.BoolButtons.Contains(buttonName))
			{
				return adapter.IsPressed(buttonName) ?
					Mnemonics[buttonName].ToString() :
					string.Empty;
			}

			if (adapter.Definition.FloatControls.Contains(buttonName))
			{
				return adapter.GetFloat(buttonName).ToString();
			}

			return "!";
		}

		public bool BoolIsPressed(int frame, string buttonName)
		{
			return ((Bk2ControllerAdapter)GetInputState(frame))
				.IsPressed(buttonName);
		}

		public float GetFloatState(int frame, string buttonName)
		{
			return ((Bk2ControllerAdapter)GetInputState(frame))
				.GetFloat(buttonName);
		}

		public void ClearGreenzone()
		{
			if (StateManager.Any())
			{
				StateManager.ClearStateHistory();
				Changes = true;
			}
		}

		public override IController GetInputState(int frame)
		{
			return base.GetInputState(frame);
		}

		public void GreenzoneCurrentFrame()
		{
			if (Global.Emulator.Frame > LastValidFrame)
			{
				// emulated a new frame, current editing segment may change now. taseditor logic
				LastPositionStable = false;
			}

			LagLog[Global.Emulator.Frame] = Global.Emulator.AsInputPollable().IsLagFrame;

			if (!StateManager.HasState(Global.Emulator.Frame))
			{
				StateManager.Capture();
			}
		}

		public void ClearLagLog()
		{
			LagLog.Clear();
		}

		public void DeleteLogBefore(int frame)
		{
			if (frame < _log.Count)
			{
				_log.RemoveRange(0, frame);
			}
		}

		public void CopyLog(IEnumerable<string> log)
		{
			_log.Clear();
			foreach (var entry in log)
			{
				_log.Add(entry);
			}
		}

		public void CopyVerificationLog(IEnumerable<string> log)
		{
			foreach (string entry in log)
			{
				VerificationLog.Add(entry);
			}
		}

		public IStringLog GetLogEntries()
		{
			return _log;
		}

		private int? TimelineBranchFrame = null;

		// TODO: this is 99% copy pasting of bad code
		public override bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			errorMessage = string.Empty;
			int? stateFrame = null;

			var newLog = new List<string>();
			// We are in record mode so replace the movie log with the one from the savestate
			if (!Global.MovieSession.MultiTrack.IsActive)
			{
				TimelineBranchFrame = null;

				if (Global.Config.EnableBackupMovies && MakeBackup && _log.Count != 0)
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
					else if (line.Contains("Frame 0x")) // NES stores frame count in hex, yay
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
					else if (line[0] == '|')
					{
						newLog.Add(line);
						if (!TimelineBranchFrame.HasValue && counter < _log.Count && line != _log[counter])
						{
							TimelineBranchFrame = counter;
						}
						counter++;
					}
				}

				_log.Clear();
				_log.AddRange(newLog);
			}
			else //Multitrack mode
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

			var stateFramei = stateFrame ?? 0;

			if (stateFramei > 0 && stateFramei < _log.Count)
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Truncate(stateFramei);
				}
			}
			else if (stateFramei > _log.Count) // Post movie savestate
			{
				if (!Global.Config.VBAStyleMovieLoadState)
				{
					Truncate(_log.Count);
				}

				_mode = Moviemode.Finished;
			}

			if (IsCountingRerecords)
			{
				Rerecords++;
			}

			if (TimelineBranchFrame.HasValue)
			{
				LagLog.RemoveFrom(TimelineBranchFrame.Value);
				TasStateManager.Invalidate(TimelineBranchFrame.Value);
			}

			return true;
		}

		public void LoadBranch(TasBranch branch)
		{
			int? divergentPoint = DivergentPoint(_log, branch.InputLog);

			if (_log != null) _log.Dispose();
				_log = branch.InputLog.Clone();
			//_changes = true;

			// if there are branch states, they will be loaded anyway
			// but if there's none, or only *after* divergent point, don't invalidate the entire movie anymore
			if (divergentPoint.HasValue)
			{
				StateManager.Invalidate(divergentPoint.Value);
				LagLog.FromLagLog(branch.LagLog); // don't truncate LagLog if the branch's one is shorter, but input is the same
			}
			else
				StateManager.Invalidate(branch.InputLog.Count);

			StateManager.LoadBranch(Branches.IndexOf(branch));

			StateManager.SetState(branch.Frame, branch.CoreData);

			//ChangeLog = branch.ChangeLog;
			Markers = branch.Markers;
			Changes = true;
		}

		// TODO: use LogGenerators rather than string comparisons
		private int? DivergentPoint(IStringLog currentLog, IStringLog newLog)
		{
			int max = newLog.Count;
			if (currentLog.Count < newLog.Count)
			{
				max = currentLog.Count;
			}

			for (int i = 0; i < max; i++)
			{
				if (newLog[i] != currentLog[i])
				{
					return i;
				}
			}

			return null;
		}

		public void AddBranch(TasBranch branch)
		{
			Branches.Add(branch);
			TasStateManager.AddBranch();
			Changes = true;
		}

		public void RemoveBranch(TasBranch branch)
		{
			TasStateManager.RemoveBranch(Branches.IndexOf(branch));
			Branches.Remove(branch);
			Changes = true;
		}

		public void UpdateBranch(TasBranch old, TasBranch newBranch)
		{
			int index = Branches.IndexOf(old);
			newBranch.UniqueIdentifier = old.UniqueIdentifier;
			if (newBranch.UserText == "")
				newBranch.UserText = old.UserText;
			Branches[index] = newBranch;
			TasStateManager.UpdateBranch(index);
			Changes = true;
		}

		public void SwapBranches(int b1, int b2)
		{
			TasBranch branch = Branches[b1];

			if (b2 >= Branches.Count)
				b2 = Branches.Count - 1;

			Branches.Remove(branch);
			Branches.Insert(b2, branch);
			Changes = true;
		}
	}
}
