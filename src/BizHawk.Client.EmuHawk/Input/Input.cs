using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Bizware.Input;
using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public class Input
	{
		/// <summary>
		/// If your form needs this kind of input focus, be sure to say so.
		/// Really, this only makes sense for mouse, but I've started building it out for other things
		/// Why is this receiving a control, but actually using it as a Form (where the WantingMouseFocus is checked?)
		/// Because later we might change it to work off the control, specifically, if a control is supplied (normally actually a Form will be supplied)
		/// </summary>
		public void ControlInputFocus(Control c, ClientInputFocus types, bool wants)
		{
			if (types.HasFlag(ClientInputFocus.Mouse) && wants) _wantingMouseFocus.Add(c);
			if (types.HasFlag(ClientInputFocus.Mouse) && !wants) _wantingMouseFocus.Remove(c);
		}

		private readonly HashSet<Control> _wantingMouseFocus = new HashSet<Control>();

#pragma warning disable CA2211 // public field
		public static Input Instance;
#pragma warning restore CA2211

		private readonly Thread _updateThread;

		public readonly IHostInputAdapter Adapter;

		private Config _currentConfig;

		private readonly Func<Config> _getConfigCallback;

		internal Input(IntPtr mainFormHandle, Func<Config> getConfigCallback, Func<bool, AllowInput> mainFormInputAllowedCallback)
		{
			_getConfigCallback = getConfigCallback;
			_currentConfig = _getConfigCallback();
			UpdateModifierKeysEffective();

			MainFormInputAllowedCallback = mainFormInputAllowedCallback;

			Adapter = new SDL2InputAdapter();
			Console.WriteLine($"Using {Adapter.Desc} for host input (keyboard + gamepads)");
			Adapter.UpdateConfig(_currentConfig);
			Adapter.FirstInitAll(mainFormHandle);
			_updateThread = new Thread(UpdateThreadProc)
			{
				IsBackground = true,
				Priority = ThreadPriority.AboveNormal // why not? this thread shouldn't be very heavy duty, and we want it to be responsive
			};
			_updateThread.Start();
		}

		private readonly Dictionary<string, float> _axisDeltas = new();

		private readonly Dictionary<string, int> _axisValues = new();

		private readonly Dictionary<string, bool> _lastState = new();

		private bool _trackDeltas;
		private bool _ignoreEventsNextPoll;

		private static readonly IReadOnlyList<string> ModifierKeysBase = new[] { "Super", "Ctrl", "Alt", "Shift" };

		private static readonly IReadOnlyList<string> ModifierKeysBaseUnmerged = new[] { "Super", "Ctrl", "Alt", "Shift", "LeftSuper", "RightSuper", "LeftCtrl", "RightCtrl", "LeftAlt", "RightAlt", "LeftShift", "RightShift" };

		public void UpdateModifierKeysEffective()
			=> _currentConfig.ModifierKeysEffective = (_currentConfig.MergeLAndRModifierKeys ? ModifierKeysBase : ModifierKeysBaseUnmerged)
				.Concat(_currentConfig.ModifierKeys)
				.Take(32).ToArray();

		internal static readonly IReadOnlyDictionary<string, string> ModifierKeyPreMap = new Dictionary<string, string>
		{
			["LeftSuper"] = "Win",
			["RightSuper"] = "Win",
			["LeftCtrl"] = "Ctrl",
			["RightCtrl"] = "Ctrl",
			["LeftAlt"] = "Alt",
			["RightAlt"] = "Alt",
			["LeftShift"] = "Shift",
			["RightShift"] = "Shift",
		};

		internal static readonly IReadOnlyDictionary<string, string> ModifierKeyInvPreMap = new Dictionary<string, string>
		{
			["Super"] = "LeftWin",
			["Ctrl"] = "LeftCtrl",
			["Alt"] = "LeftAlt",
			["Shift"] = "LeftShift",
		};

		private void HandleButton(string button, bool newState, ClientInputFocus source)
		{
			if (!(_currentConfig.MergeLAndRModifierKeys && ModifierKeyPreMap.TryGetValue(button, out var button1))) button1 = button;
			var modIndex = _currentConfig.ModifierKeysEffective.IndexOf(button1);
			var currentModifier = modIndex is -1 ? 0U : 1U << modIndex;
			if (EnableIgnoreModifiers && currentModifier is not 0U) return;
			if (newState == _lastState.GetValueOrDefault(button1)) return;

			if (currentModifier is not 0U)
			{
				if (newState)
					_modifiers |= currentModifier;
				else
					_modifiers &= ~currentModifier;
			}

			// don't generate events for things like Ctrl+LeftControl
			var mods = _modifiers;
			if (!newState)
				mods = 0; // don't set mods for release events, handle releasing all corresponding buttons later in InputCoalescer.Receive()
			else if (currentModifier is not 0U)
				mods &= ~currentModifier;

			var ie = new InputEvent
				{
					EventType = newState ? InputEventType.Press : InputEventType.Release,
					LogicalButton = new(button1, mods, () => _getConfigCallback().ModifierKeysEffective),
					Source = source
				};
			_lastState[button1] = newState;

			if (!_ignoreEventsNextPoll)
			{
				_newEvents.Add(ie);
			}
		}

		private void HandleAxis(string axis, int newValue)
		{
			if (ShouldSwallow(MainFormInputAllowedCallback(false), ClientInputFocus.Pad))
				return;

			if (_trackDeltas)
			{
				_axisDeltas[axis] = _axisDeltas.GetValueOrDefault(axis)
					+ Math.Abs(newValue - _axisValues.GetValueOrDefault(axis));
			}
			_axisValues[axis] = newValue;
		}

		private uint _modifiers;
		private readonly List<InputEvent> _newEvents = new List<InputEvent>();

		public void ClearEvents()
		{
			lock (this)
			{
				_inputEvents.Clear();
				// To "clear" anything currently in the input device buffers
				_ignoreEventsNextPoll = true;
			}
		}

		private readonly Queue<InputEvent> _inputEvents = new Queue<InputEvent>();
		public InputEvent DequeueEvent()
		{
			lock (this)
			{
				return _inputEvents.Count == 0 ? null : _inputEvents.Dequeue();
			}
		}

		private void EnqueueEvent(InputEvent ie)
		{
			lock (this)
			{
				_inputEvents.Enqueue(ie);
			}
		}

		public KeyValuePair<string, int>[] GetAxisValues()
		{
			lock (_axisValues)
			{
				return _axisValues.ToArray();
			}
		}

		/// <summary>
		/// Controls whether MainForm generates input events. should be turned off for most modal dialogs
		/// </summary>
		public readonly Func<bool, AllowInput> MainFormInputAllowedCallback;

		private void UpdateThreadProc()
		{
			while (true)
			{
				_currentConfig = _getConfigCallback();
				UpdateModifierKeysEffective();
				Adapter.UpdateConfig(_currentConfig);

				var keyEvents = Adapter.ProcessHostKeyboards();
				Adapter.PreprocessHostGamepads();

				//this block is going to massively modify data structures that the binding method uses, so we have to lock it all
				lock (this)
				{
					_newEvents.Clear();

					//analyze keys
					foreach (var ke in keyEvents)
					{
						HandleButton(DistinctKeyNameOverrides.GetName(in ke.Key), ke.Pressed, ClientInputFocus.Keyboard);
					}

					lock (_axisValues)
					{
						//_axisValues.Clear();
						Adapter.ProcessHostGamepads(HandleButton, HandleAxis);

						// analyze moose
						// other sorts of mouse api (raw input) could easily be added as a separate listing under a different class
						if (_wantingMouseFocus.Contains(Form.ActiveForm))
						{
							var mousePos = Control.MousePosition;
							if (_trackDeltas)
							{
								// these are relative to screen coordinates, but that's not terribly important
								const float MOUSE_DELTA_SCALE = 50.0f;
								_axisDeltas["WMouse X"] = _axisDeltas.GetValueOrDefault("WMouse X")
									+ MOUSE_DELTA_SCALE * Math.Abs(mousePos.X - _axisValues.GetValueOrDefault("WMouse X"));
								_axisDeltas["WMouse Y"] = _axisDeltas.GetValueOrDefault("WMouse Y")
									+ MOUSE_DELTA_SCALE * Math.Abs(mousePos.Y - _axisValues.GetValueOrDefault("WMouse Y"));
							}
							// coordinate translation happens later
							_axisValues["WMouse X"] = mousePos.X;
							_axisValues["WMouse Y"] = mousePos.Y;

							var mouseBtns = Control.MouseButtons;
							HandleButton("WMouse L", (mouseBtns & MouseButtons.Left) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse M", (mouseBtns & MouseButtons.Middle) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse R", (mouseBtns & MouseButtons.Right) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse 1", (mouseBtns & MouseButtons.XButton1) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse 2", (mouseBtns & MouseButtons.XButton2) != 0, ClientInputFocus.Mouse);
						}
						else
						{
#if false // don't do this: for now, it will interfere with the virtualpad. don't do something similar for the mouse position either
							// unpress all buttons
							HandleButton("WMouse L", false, ClientInputFocus.Mouse);
							HandleButton("WMouse M", false, ClientInputFocus.Mouse);
							HandleButton("WMouse R", false, ClientInputFocus.Mouse);
							HandleButton("WMouse 1", false, ClientInputFocus.Mouse);
							HandleButton("WMouse 2", false, ClientInputFocus.Mouse);
#endif
						}
					}

					if (_newEvents.Count != 0)
					{
						//WHAT!? WE SHOULD NOT BE SO NAIVELY TOUCHING MAINFORM FROM THE INPUTTHREAD. ITS BUSY RUNNING.
						AllowInput allowInput = MainFormInputAllowedCallback(false);

						foreach (var ie in _newEvents)
						{
							//events are swallowed in some cases:
							if ((ie.LogicalButton.Modifiers & LogicalButton.MASK_ALT) is not 0U && ShouldSwallow(MainFormInputAllowedCallback(true), ie.Source))
								continue;
							if (ie.EventType == InputEventType.Press && ShouldSwallow(allowInput, ie.Source))
								continue;

							EnqueueEvent(ie);
						}
					}

					_ignoreEventsNextPoll = false;
				} //lock(this)

				//arbitrary selection of polling frequency:
				Thread.Sleep(2);
			}
		}

		private static bool ShouldSwallow(AllowInput allowInput, ClientInputFocus inputFocus)
		{
			return allowInput == AllowInput.None || (allowInput == AllowInput.OnlyController && inputFocus != ClientInputFocus.Pad);
		}

		public void StartListeningForAxisEvents()
		{
			lock (_axisValues)
			{
				_axisDeltas.Clear();
				_trackDeltas = true;
			}
		}

		public string GetNextAxisEvent()
		{
			lock (_axisValues)
			{
				foreach (var (k, v) in _axisDeltas)
				{
					// need to wiggle the stick a bit
					if (v >= 20000.0f) return k;
				}
			}
			return null;
		}

		public void StopListeningForAxisEvents()
		{
			lock (_axisValues)
			{
				_trackDeltas = false;
			}
		}

		public void Update()
		{
			//TODO - for some reason, we may want to control when the next event processing step happens
			//so i will leave this method here for now..
		}

		//returns the next Press event, if available. should be useful
		public string GetNextBindEvent(ref InputEvent lastPress)
		{
			//this whole process is intimately involved with the data structures, which can conflict with the input thread.
			lock (this)
			{
				if (_inputEvents.Count == 0) return null;
				AllowInput allowInput = MainFormInputAllowedCallback(false);

				//wait for the first release after a press to complete input binding, because we need to distinguish pure modifierkeys from modified keys
				//if you just pressed ctrl, wanting to bind ctrl, we'd see: pressed:ctrl, unpressed:ctrl
				//if you just pressed ctrl+c, wanting to bind ctrl+c, we'd see: pressed:ctrl, pressed:ctrl+c, unpressed:ctrl+c, unpressed:ctrl
				//but in the 2nd example the unpresses will be swapped if ctrl is released first, so we'll take the last press before the release

				while (_inputEvents.Count != 0)
				{
					InputEvent ie = DequeueEvent();

					if (ShouldSwallow(allowInput, ie.Source)) continue;

					if (ie.EventType == InputEventType.Press)
					{
						lastPress = ie;
						//don't allow presses to directly complete binding except escape which we'll accept as a special perk
						if (ie.LogicalButton.Button != "Escape")
							continue;
					}
					else if (lastPress == null) continue;

					Console.WriteLine("Bind Event: {0} ", lastPress);

					_inputEvents.Clear();

					return lastPress.LogicalButton.ToString();
				}

				return null;
			}
		}

		//controls whether modifier keys will be ignored as key press events
		//this should be used by hotkey binders, but we may want modifier key events
		//to get triggered in the main form
		public volatile bool EnableIgnoreModifiers = false;
	}
}
