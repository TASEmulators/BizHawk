using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Client.Common
{
	public sealed partial class TasMovie : Bk2Movie, INotifyPropertyChanged
	{
		private readonly Bk2MnemonicConstants Mnemonics = new Bk2MnemonicConstants();
		private List<bool> LagLog = new List<bool>();
		private readonly TasStateManager StateManager;
		public TasMovieMarkerList Markers { get; set; }

		public TasMovie(string path) : base(path)
		{
			// TODO: how to call the default constructor AND the base(path) constructor?  And is base(path) calling base() ?
			StateManager = new TasStateManager(this);
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0 Tasproj v1.0";
			Markers = new TasMovieMarkerList(this);
			Markers.CollectionChanged += Markers_CollectionChanged;
			Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
		}

		public TasMovie()
			: base()
		{
			StateManager = new TasStateManager(this);
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0 Tasproj v1.0";
			Markers = new TasMovieMarkerList(this);
			Markers.CollectionChanged += Markers_CollectionChanged;
			Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
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
					State = StateManager[index],
					LogEntry = GetInputLogEntry(index),
					Lagged = (index < LagLog.Count) ? LagLog[index] : (bool?)null
				};
			}
		}

		public override bool Stop(bool saveChanges = true)
		{
			return base.Stop(saveChanges);
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

		// This event is Raised ony when Changes is TOGGLED.
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

		public override void StartNewRecording()
		{
			LagLog.Clear();
			StateManager.Clear();
			Markers.Clear();
			base.StartNewRecording();

			Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
		}

		public override void SwitchToPlay()
		{
			_mode = Moviemode.Play;
		}

		/// <summary>
		/// Removes lag log and greenzone after this frame
		/// </summary>
		/// <param name="frame">The last frame that can be valid.</param>
		private void InvalidateAfter(int frame)
		{
			if (frame < LagLog.Count)
			{
				LagLog.RemoveRange(frame + 1, LagLog.Count - frame - 1);
			}

			StateManager.Invalidate(frame + 1);
			Changes = true; // TODO check if this actually removed anything before flagging changes
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

		private readonly Dictionary<int, IController> InputStateCache = new Dictionary<int, IController>();

		public bool UseInputCache { get; set; }
		public void FlushInputCache()
		{
			InputStateCache.Clear();
		}

		public string CreateDisplayValueForButton(IController adapter, string buttonName)
		{
			if (adapter.Type.BoolButtons.Contains(buttonName))
			{
				return adapter.IsPressed(buttonName) ?
					Mnemonics[buttonName].ToString() :
					string.Empty;
			}

			if (adapter.Type.FloatControls.Contains(buttonName))
			{
				return adapter.GetFloat(buttonName).ToString();
			}

			return "!";
		}

		public void ToggleBoolState(int frame, string buttonName)
		{
			if (frame < _log.Count)
			{
				var adapter = GetInputState(frame) as Bk2ControllerAdapter;
				adapter[buttonName] = !adapter.IsPressed(buttonName);

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame] = lg.GenerateLogEntry();
				Changes = true;
				InvalidateAfter(frame);
			}
		}

		public void SetBoolState(int frame, string buttonName, bool val)
		{
			if (frame < _log.Count)
			{
				var adapter = GetInputState(frame) as Bk2ControllerAdapter;
				var old = adapter[buttonName];
				adapter[buttonName] = val;

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame] = lg.GenerateLogEntry();

				if (old != val)
				{
					InvalidateAfter(frame);
					Changes = true;
				}
			}
		}

		public void SetFloatState(int frame, string buttonName, float val)
		{
			if (frame < _log.Count)
			{
				var adapter = GetInputState(frame) as Bk2ControllerAdapter;
				var old = adapter.GetFloat(buttonName);
				adapter.SetFloat(buttonName, val);

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame] = lg.GenerateLogEntry();

				if (old != val)
				{
					InvalidateAfter(frame);
					Changes = true;
				}
			}
		}

		public bool BoolIsPressed(int frame, string buttonName)
		{
			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			return adapter.IsPressed(buttonName);
		}

		public float GetFloatValue(int frame, string buttonName)
		{
			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			return adapter.GetFloat(buttonName);
		}

		// TODO: try not to need this, or at least use GetInputState and then a log entry generator
		public string GetInputLogEntry(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{
				int getframe;

				if (LoopOffset.HasValue)
				{
					if (frame < _log.Count)
					{
						getframe = frame;
					}
					else
					{
						getframe = ((frame - LoopOffset.Value) % (_log.Count - LoopOffset.Value)) + LoopOffset.Value;
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

		public void ClearGreenzone()
		{
			if (StateManager.Any())
			{
				StateManager.ClearGreenzone();
				Changes = true;
			}
		}

		public override IController GetInputState(int frame)
		{
			// TODO: states and lag capture
			if (Global.Emulator.Frame == frame) // Take this opportunity to capture lag and state info if we do not have it
			{
				if (frame == LagLog.Count) // I intentionally did not do >=, if it were >= we missed some entries somewhere, oops, maybe this shoudl be a dictionary<int, bool> with frame values?
				{
					LagLog.Add(Global.Emulator.IsLagFrame);
				}

				if (!StateManager.HasState(frame))
				{
					StateManager.Capture();
				}
			}

			return base.GetInputState(frame);
		}
	}
}
