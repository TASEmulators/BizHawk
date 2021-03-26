using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;
using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	// coalesces events back into instantaneous states
	public class InputCoalescer : SimpleController
	{
		public void Receive(Input.InputEvent ie)
		{
			bool state = ie.EventType == Input.InputEventType.Press;
			
			string button = ie.LogicalButton.ToString();
			Buttons[button] = state;

			//when a button is released, all modified variants of it are released as well
			if (!state)
			{
				var releases = Buttons.Where(kvp => kvp.Key.Contains("+") && kvp.Key.EndsWith(ie.LogicalButton.Button)).ToArray();
				foreach (var kvp in releases)
					Buttons[kvp.Key] = false;
			}
		}
	}

	public class ControllerInputCoalescer : SimpleController
	{
		public void Receive(Input.InputEvent ie)
		{
			bool state = ie.EventType == Input.InputEventType.Press;

			string button = ie.LogicalButton.ToString();
			Buttons[button] = state;

			//For controller input, we want Shift+X to register as both Shift and X (for Keyboard controllers)
			string[] subgroups = button.Split('+');
			if (subgroups.Length > 0)
			{
				foreach (string s in subgroups)
				{
					Buttons[s] = state;
				}
			}

			//when a button is released, all modified variants of it are released as well
			if (!state)
			{
				var releases = Buttons.Where((kvp) => kvp.Key.Contains("+") && kvp.Key.EndsWith(ie.LogicalButton.Button)).ToArray();
				foreach (var kvp in releases)
					Buttons[kvp.Key] = false;
			}
		}
	}

	public class Input
	{
		public enum AllowInput
		{
			None = 0,
			All = 1,
			OnlyController = 2
		}

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

		[Flags]
		public enum ModifierKey
		{
			// Summary:
			//     The bitmask to extract modifiers from a key value.
			Modifiers = -65536,
			//
			// Summary:
			//     No key pressed.
			None = 0,
			//
			// Summary:
			//     The SHIFT modifier key.
			Shift = 65536,
			//
			// Summary:
			//     The CTRL modifier key.
			Control = 131072,
			//
			// Summary:
			//     The ALT modifier key.
			Alt = 262144
		}

		public static Input Instance;

		private readonly Thread _updateThread;

		public readonly IHostInputAdapter Adapter;

		private readonly Func<Config> _getConfigCallback;

		internal Input(IntPtr mainFormHandle, Func<Config> getConfigCallback, Func<bool, AllowInput> mainFormInputAllowedCallback)
		{
			_getConfigCallback = getConfigCallback;
			MainFormInputAllowedCallback = mainFormInputAllowedCallback;

			var config = _getConfigCallback();
			Adapter = config.HostInputMethod switch
			{
				EHostInputMethod.OpenTK => new OpenTKInputAdapter(),
				_ when OSTailoredCode.IsUnixHost => new OpenTKInputAdapter(),
				EHostInputMethod.DirectInput => new DirectInputAdapter(),
				_ => throw new Exception()
			};
			Adapter.UpdateConfig(config);
			Adapter.FirstInitAll(mainFormHandle);
			_updateThread = new Thread(UpdateThreadProc)
			{
				IsBackground = true, 
				Priority = ThreadPriority.AboveNormal // why not? this thread shouldn't be very heavy duty, and we want it to be responsive
			};
			_updateThread.Start();
		}

		public enum InputEventType
		{
			Press, Release
		}
		public struct LogicalButton
		{
			public LogicalButton(string button, ModifierKey modifiers)
			{
				Button = button;
				Modifiers = modifiers;
			}
			public readonly string Button;
			public readonly ModifierKey Modifiers;

			public bool Alt => (Modifiers & ModifierKey.Alt) != 0;
			public bool Control => (Modifiers & ModifierKey.Control) != 0;
			public bool Shift => (Modifiers & ModifierKey.Shift) != 0;

			public override string ToString()
			{
				string ret = "";
				if (Control) ret += "Ctrl+";
				if (Alt) ret += "Alt+";
				if (Shift) ret += "Shift+";
				ret += Button;
				return ret;
			}
			public override bool Equals(object obj)
			{
				if (obj is null)
				{
					return false;
				}

				var other = (LogicalButton)obj;
				return other == this;
			}
			public override int GetHashCode()
			{
				return Button.GetHashCode() ^ Modifiers.GetHashCode();
			}
			public static bool operator ==(LogicalButton lhs, LogicalButton rhs)
			{
				return lhs.Button == rhs.Button && lhs.Modifiers == rhs.Modifiers;
			}
			public static bool operator !=(LogicalButton lhs, LogicalButton rhs)
			{
				return !(lhs == rhs);
			}
		}
		public class InputEvent
		{
			public LogicalButton LogicalButton;
			public InputEventType EventType;
			public ClientInputFocus Source;
			public override string ToString()
			{
				return $"{EventType}:{LogicalButton}";
			}
		}

		private readonly Dictionary<string, LogicalButton> _modifierState = new Dictionary<string, LogicalButton>();
		private readonly WorkingDictionary<string, bool> _lastState = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, int> _axisValues = new WorkingDictionary<string, int>();
		private readonly WorkingDictionary<string, float> _axisDeltas = new WorkingDictionary<string, float>(); 
		private IReadOnlyCollection<(string name, int strength)> _hapticFeedback = Array.Empty<(string, int)>();

		private bool _trackDeltas;
		private bool _ignoreEventsNextPoll;

		private void HandleButton(string button, bool newState, ClientInputFocus source)
		{
			var currentModifier = button switch
			{
//				"LeftWin" => ModifierKey.Win,
//				"RightWin" => ModifierKey.Win,
				"LeftShift" => ModifierKey.Shift,
				"RightShift" => ModifierKey.Shift,
				"LeftCtrl" => ModifierKey.Control,
				"RightCtrl" => ModifierKey.Control,
				"LeftAlt" => ModifierKey.Alt,
				"RightAlt" => ModifierKey.Alt,
				_ => ModifierKey.None
			};
			if (EnableIgnoreModifiers && currentModifier != ModifierKey.None) return;
			if (_lastState[button] == newState) return;

			// apply 
			// NOTE: this is not quite right. if someone held leftshift+rightshift it would be broken. seems unlikely, though.
			if (currentModifier != ModifierKey.None)
			{
				if (newState)
					_modifiers |= currentModifier;
				else
					_modifiers &= ~currentModifier;
			}

			// don't generate events for things like Ctrl+LeftControl
			ModifierKey mods = _modifiers;
			if (currentModifier != ModifierKey.None)
				mods &= ~currentModifier;

			var ie = new InputEvent
				{
					EventType = newState ? InputEventType.Press : InputEventType.Release,
					LogicalButton = new LogicalButton(button, mods),
					Source = source
				};
			_lastState[button] = newState;

			// track the pressed events with modifiers that we send so that we can send corresponding unpresses with modifiers
			// this is an interesting idea, which we may need later, but not yet.
			// for example, you may see this series of events: press:ctrl+c, release:ctrl, release:c
			// but you might would rather have press:ctrl+c, release:ctrl+c
			// this code relates the releases to the original presses.
			// UPDATE - this is necessary for the frame advance key, which has a special meaning when it gets stuck down
			// so, i am adding it as of 11-sep-2011
			if (newState)
			{
				_modifierState[button] = ie.LogicalButton;
			}
			else
			{
				if (_modifierState.TryGetValue(button, out var buttonModifierState))
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
					_modifierState.Remove(button);
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

		private ModifierKey _modifiers;
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
			static string KeyName(DistinctKey k) => k switch
			{
				DistinctKey.Back => "Backspace",
				DistinctKey.Enter => "Enter",
				DistinctKey.CapsLock => "CapsLock",
				DistinctKey.PageDown => "PageDown",
				DistinctKey.D0 => "Number0",
				DistinctKey.D1 => "Number1",
				DistinctKey.D2 => "Number2",
				DistinctKey.D3 => "Number3",
				DistinctKey.D4 => "Number4",
				DistinctKey.D5 => "Number5",
				DistinctKey.D6 => "Number6",
				DistinctKey.D7 => "Number7",
				DistinctKey.D8 => "Number8",
				DistinctKey.D9 => "Number9",
				DistinctKey.LWin => "LeftWin",
				DistinctKey.RWin => "RightWin",
				DistinctKey.NumPad0 => "Keypad0",
				DistinctKey.NumPad1 => "Keypad1",
				DistinctKey.NumPad2 => "Keypad2",
				DistinctKey.NumPad3 => "Keypad3",
				DistinctKey.NumPad4 => "Keypad4",
				DistinctKey.NumPad5 => "Keypad5",
				DistinctKey.NumPad6 => "Keypad6",
				DistinctKey.NumPad7 => "Keypad7",
				DistinctKey.NumPad8 => "Keypad8",
				DistinctKey.NumPad9 => "Keypad9",
				DistinctKey.Multiply => "KeypadMultiply",
				DistinctKey.Add => "KeypadAdd",
				DistinctKey.Separator => "KeypadComma",
				DistinctKey.Subtract => "KeypadSubtract",
				DistinctKey.Decimal => "KeypadDecimal",
				DistinctKey.Divide => "KeypadDivide",
				DistinctKey.Scroll => "ScrollLock",
				DistinctKey.OemSemicolon => "Semicolon",
				DistinctKey.OemPlus => "Equals",
				DistinctKey.OemComma => "Comma",
				DistinctKey.OemMinus => "Minus",
				DistinctKey.OemPeriod => "Period",
				DistinctKey.OemQuestion => "Slash",
				DistinctKey.OemTilde => "Backtick",
				DistinctKey.OemOpenBrackets => "LeftBracket",
				DistinctKey.OemPipe => "Backslash",
				DistinctKey.OemCloseBrackets => "RightBracket",
				DistinctKey.OemQuotes => "Apostrophe",
				DistinctKey.OemBackslash => "OEM102",
				DistinctKey.NumPadEnter => "KeypadEnter",
				_ => k.ToString()
			};
			while (true)
			{
				Adapter.UpdateConfig(_getConfigCallback());

				var keyEvents = Adapter.ProcessHostKeyboards();
				Adapter.PreprocessHostGamepads();

				//this block is going to massively modify data structures that the binding method uses, so we have to lock it all
				lock (this)
				{
					_newEvents.Clear();

					//analyze keys
					foreach (var ke in keyEvents)
						HandleButton(KeyName(ke.Key), ke.Pressed, ClientInputFocus.Keyboard);
					
					foreach (var pad in OTK_GamePad.EnumerateDevices())
					{
						int leftStrength = 0;
						int rightStrength = 0;
						bool dualHaptic = false;
						foreach (var (name, strength) in _hapticFeedback)
						{
							if (name == $"{pad.InputNamePrefix}Mono Haptic")
							{
								pad.SetVibration(strength, strength);
								break;
							}
							else if (name == $"{pad.InputNamePrefix}Left Haptic")
							{
								dualHaptic = true;
								leftStrength = strength;
							}
							else if (name == $"{pad.InputNamePrefix}Right Haptic")
							{
								dualHaptic = true;
								rightStrength = strength;
							}
						}
						if (dualHaptic)
						{
							pad.SetVibration(leftStrength, rightStrength);
						}
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
							if (ie.LogicalButton.Alt && ShouldSwallow(MainFormInputAllowedCallback(true), ie))
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
				foreach (var kvp in _axisDeltas)
				{
					// need to wiggle the stick a bit
					if (kvp.Value >= 20000.0f)
						return kvp.Key;
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

		public void SetHapticsFromSnapshot(IReadOnlyCollection<(string name, int strength)> snapshot) => 
			_hapticFeedback = snapshot;


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
