using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

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
		[Flags]
		public enum InputFocus
		{
			None = 0,
			Mouse = 1,
			Keyboard = 2,
			Pad = 4
		}

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
		public void ControlInputFocus(Control c, InputFocus types, bool wants)
		{
			if (types.HasFlag(InputFocus.Mouse) && wants) _wantingMouseFocus.Add(c);
			if (types.HasFlag(InputFocus.Mouse) && !wants) _wantingMouseFocus.Remove(c);
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
			Alt = 262144,
		}

		public static Input Instance { get; private set; }
		private readonly Thread UpdateThread;

		private Input()
		{
			UpdateThread = new Thread(UpdateThreadProc)
			{
				IsBackground = true, 
				Priority = ThreadPriority.AboveNormal // why not? this thread shouldn't be very heavy duty, and we want it to be responsive
			};
			UpdateThread.Start();
		}

		public static void Initialize(Control parent)
		{
			if (OSTailoredCode.IsUnixHost)
			{
				OTK_Keyboard.Initialize();
				OTK_GamePad.Initialize();
			}
			else
			{
				KeyInput.Initialize(parent);
				IPCKeyInput.Initialize();
				GamePad.Initialize();
				GamePad360.Initialize();
			}
			Instance = new Input();
		}

		public static void Cleanup()
		{
			if (!OSTailoredCode.IsUnixHost)
			{
				KeyInput.Cleanup();
				GamePad.Cleanup();
			}
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
			public InputFocus Source;
			public override string ToString()
			{
				return $"{EventType.ToString()}:{LogicalButton.ToString()}";
			}
		}

		private readonly Dictionary<string, LogicalButton> _modifierState = new Dictionary<string, LogicalButton>();
		private readonly WorkingDictionary<string, bool> _lastState = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> _floatValues = new WorkingDictionary<string, float>();
		private readonly WorkingDictionary<string, float> _floatDeltas = new WorkingDictionary<string, float>();
		private bool _trackDeltas;
		private bool _ignoreEventsNextPoll;

		private void HandleButton(string button, bool newState, InputFocus source)
		{
			ModifierKey currentModifier = ButtonToModifierKey(button);
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

		private static ModifierKey ButtonToModifierKey(string button)
		{
			switch (button)
			{
				case "LeftShift":  return ModifierKey.Shift;
				case "RightShift": return ModifierKey.Shift;
				case "LeftControl":  return ModifierKey.Control;
				case "RightControl": return ModifierKey.Control;
				case "LeftAlt":  return ModifierKey.Alt;
				case "RightAlt": return ModifierKey.Alt;
			}
			return ModifierKey.None;
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
		void EnqueueEvent(InputEvent ie)
		{
			lock (this)
			{
				_inputEvents.Enqueue(ie);
			}
		}

		public List<Tuple<string, float>> GetFloats()
		{
			var floatValuesCopy = new List<Tuple<string,float>>();
			lock (_floatValues)
			{
				foreach (var kvp in _floatValues)
				{
					floatValuesCopy.Add(new Tuple<string, float>(kvp.Key, kvp.Value));
				}
			}

			return floatValuesCopy;
		}

		private void UpdateThreadProc()
		{
			while (true)
			{
				var keyEvents = OSTailoredCode.IsUnixHost
					? OTK_Keyboard.Update()
					: KeyInput.Update().Concat(IPCKeyInput.Update());
				if (OSTailoredCode.IsUnixHost)
				{
					OTK_GamePad.UpdateAll();
				}
				else
				{
					GamePad.UpdateAll();
					GamePad360.UpdateAll();
				}

				//this block is going to massively modify data structures that the binding method uses, so we have to lock it all
				lock (this)
				{
					_newEvents.Clear();

					//analyze keys
					foreach (var ke in keyEvents)
						HandleButton(ke.Key.ToString(), ke.Pressed, InputFocus.Keyboard);

					lock (_floatValues)
					{
						//FloatValues.Clear();

						// analyze OpenTK xinput (or is it libinput?)
						foreach (var pad in OTK_GamePad.EnumerateDevices())
						{
							foreach (var but in pad.buttonObjects)
							{
								HandleButton(pad.InputNamePrefix + but.ButtonName, but.ButtonAction(), InputFocus.Pad);
							}
							foreach (var sv in pad.GetFloats())
							{
								var n = $"{pad.InputNamePrefix}{sv.Item1} Axis";
								var f = sv.Item2;
								if (_trackDeltas) _floatDeltas[n] += Math.Abs(f - _floatValues[n]);
								_floatValues[n] = f;
							}
						}

						// analyze xinput
						foreach (var pad in GamePad360.EnumerateDevices())
						{
							string xName = $"X{pad.PlayerNumber} ";
							for (int b = 0; b < pad.NumButtons; b++)
								HandleButton(xName + pad.ButtonName(b), pad.Pressed(b), InputFocus.Pad);
							foreach (var sv in pad.GetFloats())
							{
								string n = xName + sv.Item1;
								float f = sv.Item2;
								if (_trackDeltas)
									_floatDeltas[n] += Math.Abs(f - _floatValues[n]);
								_floatValues[n] = f;
							}
						}

						// analyze joysticks
						foreach (var pad in GamePad.EnumerateDevices())
						{
							string jName = $"J{pad.PlayerNumber} ";
							for (int b = 0; b < pad.NumButtons; b++)
								HandleButton(jName + pad.ButtonName(b), pad.Pressed(b), InputFocus.Pad);
							foreach (var sv in pad.GetFloats())
							{
								string n = jName + sv.Item1;
								float f = sv.Item2;
								//if (n == "J5 RotationZ")
								//	System.Diagnostics.Debugger.Break();
								if (_trackDeltas)
									_floatDeltas[n] += Math.Abs(f - _floatValues[n]);
								_floatValues[n] = f;
							}
						}

						// analyze moose
						// other sorts of mouse api (raw input) could easily be added as a separate listing under a different class
						if (_wantingMouseFocus.Contains(Form.ActiveForm))
						{
							var mousePos = Control.MousePosition;
							if (_trackDeltas)
							{
								// these are relative to screen coordinates, but that's not terribly important
								_floatDeltas["WMouse X"] += Math.Abs(mousePos.X - _floatValues["WMouse X"]) * 50;
								_floatDeltas["WMouse Y"] += Math.Abs(mousePos.Y - _floatValues["WMouse Y"]) * 50;
							}
							// coordinate translation happens later
							_floatValues["WMouse X"] = mousePos.X;
							_floatValues["WMouse Y"] = mousePos.Y;

							var mouseBtns = Control.MouseButtons;
							HandleButton("WMouse L", (mouseBtns & MouseButtons.Left) != 0, InputFocus.Mouse);
							HandleButton("WMouse C", (mouseBtns & MouseButtons.Middle) != 0, InputFocus.Mouse);
							HandleButton("WMouse R", (mouseBtns & MouseButtons.Right) != 0, InputFocus.Mouse);
							HandleButton("WMouse 1", (mouseBtns & MouseButtons.XButton1) != 0, InputFocus.Mouse);
							HandleButton("WMouse 2", (mouseBtns & MouseButtons.XButton2) != 0, InputFocus.Mouse);
						}
						else
						{
							// don't do this: for now, it will interfere with the virtualpad. don't do something similar for the mouse position either
							// unpress all buttons
							//HandleButton("WMouse L", false);
							//HandleButton("WMouse C", false);
							//HandleButton("WMouse R", false);
							//HandleButton("WMouse 1", false);
							//HandleButton("WMouse 2", false);
						}
					}

					if (_newEvents.Count != 0)
					{
						//WHAT!? WE SHOULD NOT BE SO NAIVELY TOUCHING MAINFORM FROM THE INPUTTHREAD. ITS BUSY RUNNING.
						AllowInput allowInput = GlobalWin.MainForm.AllowInput(false);

						foreach (var ie in _newEvents)
						{
							//events are swallowed in some cases:
							if (ie.LogicalButton.Alt && ShouldSwallow(GlobalWin.MainForm.AllowInput(true), ie))
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
			return allowInput == AllowInput.None || (allowInput == AllowInput.OnlyController && inputEvent.Source != InputFocus.Pad);
		}

		public void StartListeningForFloatEvents()
		{
			lock (_floatValues)
			{
				_floatDeltas.Clear();
				_trackDeltas = true;
			}
		}

		public string GetNextFloatEvent()
		{
			lock (_floatValues)
			{
				foreach (var kvp in _floatDeltas)
				{
					// need to wiggle the stick a bit
					if (kvp.Value >= 20000.0f)
						return kvp.Key;
				}
			}
			return null;
		}

		public void StopListeningForFloatEvents()
		{
			lock (_floatValues)
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
				AllowInput allowInput = GlobalWin.MainForm.AllowInput(false);

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
