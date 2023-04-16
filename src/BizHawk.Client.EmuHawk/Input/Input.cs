using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;
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

		public static Input Instance;

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

			Adapter = _currentConfig.HostInputMethod switch
			{
				EHostInputMethod.OpenTK => new OpenTKInputAdapter(),
				_ when OSTailoredCode.IsUnixHost => new OpenTKInputAdapter(),
				EHostInputMethod.DirectInput => new DirectInputAdapter(),
				_ => throw new Exception()
			};
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

		private readonly Dictionary<string, LogicalButton> _modifierState = new Dictionary<string, LogicalButton>();
		private readonly WorkingDictionary<string, bool> _lastState = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, int> _axisValues = new WorkingDictionary<string, int>();
		private readonly WorkingDictionary<string, float> _axisDeltas = new WorkingDictionary<string, float>();
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
			if (newState == _lastState[button1]) return;

			if (currentModifier is not 0U)
			{
				if (newState)
					_modifiers |= currentModifier;
				else
					_modifiers &= ~currentModifier;
			}

			// don't generate events for things like Ctrl+LeftControl
			var mods = _modifiers;
			if (currentModifier is not 0U)
				mods &= ~currentModifier;

			var ie = new InputEvent
				{
					EventType = newState ? InputEventType.Press : InputEventType.Release,
					LogicalButton = new(button1, mods, () => _getConfigCallback().ModifierKeysEffective),
					Source = source
				};
			_lastState[button1] = newState;

			// track the pressed events with modifiers that we send so that we can send corresponding unpresses with modifiers
			// this is an interesting idea, which we may need later, but not yet.
			// for example, you may see this series of events: press:ctrl+c, release:ctrl, release:c
			// but you might would rather have press:ctrl+c, release:ctrl+c
			// this code relates the releases to the original presses.
			// UPDATE - this is necessary for the frame advance key, which has a special meaning when it gets stuck down
			// so, i am adding it as of 11-sep-2011
			if (newState)
			{
				_modifierState[button1] = ie.LogicalButton;
			}
			else
			{
				if (_modifierState.TryGetValue(button1, out var buttonModifierState))
				{
					if (buttonModifierState != ie.LogicalButton && !_ignoreEventsNextPoll)
					{
						_newEvents.Add(
							new InputEvent
							{
								LogicalButton = buttonModifierState,
								EventType = InputEventType.Release,
								Source = source
							});
					}
					_modifierState.Remove(button1);
				}
			}

			if (!_ignoreEventsNextPoll)
			{
				_newEvents.Add(ie);
			}
		}

		private void HandleAxis(string axis, int newValue)
		{
			if (_trackDeltas) _axisDeltas[axis] += Math.Abs(newValue - _axisValues[axis]);
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

		public IDictionary<string, int> GetAxisValues()
		{
			lock (_axisValues)
			{
				return _axisValues.ToDictionary(d => d.Key, d => d.Value);
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
								_axisDeltas["WMouse X"] += Math.Abs(mousePos.X - _axisValues["WMouse X"]) * 50;
								_axisDeltas["WMouse Y"] += Math.Abs(mousePos.Y - _axisValues["WMouse Y"]) * 50;
							}
							// coordinate translation happens later
							_axisValues["WMouse X"] = mousePos.X;
							_axisValues["WMouse Y"] = mousePos.Y;

							var mouseBtns = Control.MouseButtons;
							HandleButton("WMouse L", (mouseBtns & MouseButtons.Left) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse C", (mouseBtns & MouseButtons.Middle) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse R", (mouseBtns & MouseButtons.Right) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse 1", (mouseBtns & MouseButtons.XButton1) != 0, ClientInputFocus.Mouse);
							HandleButton("WMouse 2", (mouseBtns & MouseButtons.XButton2) != 0, ClientInputFocus.Mouse);
						}
						else
						{
#if false // don't do this: for now, it will interfere with the virtualpad. don't do something similar for the mouse position either
							// unpress all buttons
							HandleButton("WMouse L", false, ClientInputFocus.Mouse);
							HandleButton("WMouse C", false, ClientInputFocus.Mouse);
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
							if ((ie.LogicalButton.Modifiers & LogicalButton.MASK_ALT) is not 0U && ShouldSwallow(MainFormInputAllowedCallback(true), ie))
								continue;
							if (ie.EventType == InputEventType.Press && ShouldSwallow(allowInput, ie))
								continue;

							EnqueueEvent(ie);
						}
					}

					_ignoreEventsNextPoll = false;
				} //lock(this)

				//arbitrary selection of polling frequency:
				Thread.Sleep(10);
			}
		}

		private static bool ShouldSwallow(AllowInput allowInput, InputEvent inputEvent)
		{
			return allowInput == AllowInput.None || (allowInput == AllowInput.OnlyController && inputEvent.Source != ClientInputFocus.Pad);
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

					if (ShouldSwallow(allowInput, ie)) continue;

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
