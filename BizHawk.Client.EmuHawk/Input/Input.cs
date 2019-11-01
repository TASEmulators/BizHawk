using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	//coalesces events back into instantaneous states
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
		public void ControlInputFocus(System.Windows.Forms.Control c, InputFocus types, bool wants)
		{
			if (types.HasFlag(InputFocus.Mouse) && wants) WantingMouseFocus.Add(c);
			if (types.HasFlag(InputFocus.Mouse) && !wants) WantingMouseFocus.Remove(c);
		}

		HashSet<System.Windows.Forms.Control> WantingMouseFocus = new HashSet<System.Windows.Forms.Control>();

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
		readonly Thread UpdateThread;

		private Input()
		{
			UpdateThread = new Thread(UpdateThreadProc)
			{
				IsBackground = true, 
				Priority = ThreadPriority.AboveNormal //why not? this thread shouldn't be very heavy duty, and we want it to be responsive
			};
			UpdateThread.Start();
		}

		public static void Initialize()
		{
			if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows)
			{
				KeyInput.Initialize();
				IPCKeyInput.Initialize();
				GamePad.Initialize();
				GamePad360.Initialize();
			}
			else
			{
				OTK_Keyboard.Initialize();
				OTK_GamePad.Initialize();
			}
			Instance = new Input();
		}

		public static void Cleanup()
		{
			if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows)
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

			public bool Alt { get { return ((Modifiers & ModifierKey.Alt) != 0); } }
			public bool Control { get { return ((Modifiers & ModifierKey.Control) != 0); } }
			public bool Shift { get { return ((Modifiers & ModifierKey.Shift) != 0); } }

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

		private readonly Dictionary<string, LogicalButton> ModifierState = new Dictionary<string, LogicalButton>();
		private readonly WorkingDictionary<string, bool> LastState = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> FloatValues = new WorkingDictionary<string, float>();
		private readonly WorkingDictionary<string, float> FloatDeltas = new WorkingDictionary<string, float>();
		private bool trackdeltas = false;
		private bool IgnoreEventsNextPoll = false;

		void HandleButton(string button, bool newState, InputFocus source)
		{
			ModifierKey currentModifier = ButtonToModifierKey(button);
			if (EnableIgnoreModifiers && currentModifier != ModifierKey.None) return;
			if (LastState[button] == newState) return;

			//apply 
			//NOTE: this is not quite right. if someone held leftshift+rightshift it would be broken. seems unlikely, though.
			if (currentModifier != ModifierKey.None)
			{
				if (newState)
					_Modifiers |= currentModifier;
				else
					_Modifiers &= ~currentModifier;
			}

			//dont generate events for things like Ctrl+LeftControl
			ModifierKey mods = _Modifiers;
			if (currentModifier != ModifierKey.None)
				mods &= ~currentModifier;

			var ie = new InputEvent
				{
					EventType = newState ? InputEventType.Press : InputEventType.Release,
					LogicalButton = new LogicalButton(button, mods),
					Source = source
				};
			LastState[button] = newState;

			//track the pressed events with modifiers that we send so that we can send corresponding unpresses with modifiers
			//this is an interesting idea, which we may need later, but not yet.
			//for example, you may see this series of events: press:ctrl+c, release:ctrl, release:c
			//but you might would rather have press:ctrl+c, release:ctrl+c
			//this code relates the releases to the original presses.
			//UPDATE - this is necessary for the frame advance key, which has a special meaning when it gets stuck down
			//so, i am adding it as of 11-sep-2011
			if (newState)
			{
				ModifierState[button] = ie.LogicalButton;
			}
			else
			{
				LogicalButton buttonModifierState;
				if (ModifierState.TryGetValue(button, out buttonModifierState))
				{
					if (buttonModifierState != ie.LogicalButton && !IgnoreEventsNextPoll)
					{
						_NewEvents.Add(
							new InputEvent
							{
								LogicalButton = buttonModifierState,
								EventType = InputEventType.Release,
								Source = source
							});
					}
					ModifierState.Remove(button);
				}
			}

			if (!IgnoreEventsNextPoll)
			{
				_NewEvents.Add(ie);
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

		private ModifierKey _Modifiers;
		private readonly List<InputEvent> _NewEvents = new List<InputEvent>();

		public void ClearEvents()
		{
			lock (this)
			{
				InputEvents.Clear();
				// To "clear" anything currently in the input device buffers
				IgnoreEventsNextPoll = true;
			}
		}

		private readonly Queue<InputEvent> InputEvents = new Queue<InputEvent>();
		public InputEvent DequeueEvent()
		{
			lock (this)
			{
				if (InputEvents.Count == 0) return null;
				else return InputEvents.Dequeue();
			}
		}
		void EnqueueEvent(InputEvent ie)
		{
			lock (this)
			{
				InputEvents.Enqueue(ie);
			}
		}

		public List<Tuple<string, float>> GetFloats()
		{
			List<Tuple<string, float>> FloatValuesCopy = new List<Tuple<string,float>>();
			lock (FloatValues)
			{
				foreach (var kvp in FloatValues)
					FloatValuesCopy.Add(new Tuple<string, float>(kvp.Key, kvp.Value));
			}
			return FloatValuesCopy;
		}

		void UpdateThreadProc()
		{
			while (true)
			{
				var keyEvents = OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows
					? KeyInput.Update().Concat(IPCKeyInput.Update())
					: OTK_Keyboard.Update();
				if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows)
				{
					GamePad.UpdateAll();
					GamePad360.UpdateAll();
				}
				else
				{
					OTK_GamePad.UpdateAll();
				}

				//this block is going to massively modify data structures that the binding method uses, so we have to lock it all
				lock (this)
				{
					_NewEvents.Clear();

					//analyze keys
					foreach (var ke in keyEvents)
						HandleButton(ke.Key.ToString(), ke.Pressed, InputFocus.Keyboard);

					lock (FloatValues)
					{
						//FloatValues.Clear();

						// analyse OpenTK xinput (or is it libinput?)
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
								if (trackdeltas) FloatDeltas[n] += Math.Abs(f - FloatValues[n]);
								FloatValues[n] = f;
							}
						}

						//analyze xinput
						foreach (var pad in GamePad360.EnumerateDevices())
						{
							string xname = $"X{pad.PlayerNumber} ";
							for (int b = 0; b < pad.NumButtons; b++)
								HandleButton(xname + pad.ButtonName(b), pad.Pressed(b), InputFocus.Pad);
							foreach (var sv in pad.GetFloats())
							{
								string n = xname + sv.Item1;
								float f = sv.Item2;
								if (trackdeltas)
									FloatDeltas[n] += Math.Abs(f - FloatValues[n]);
								FloatValues[n] = f;
							}
						}

						//analyze joysticks
						foreach (var pad in GamePad.EnumerateDevices())
						{
							string jname = $"J{pad.PlayerNumber} ";
							for (int b = 0; b < pad.NumButtons; b++)
								HandleButton(jname + pad.ButtonName(b), pad.Pressed(b), InputFocus.Pad);
							foreach (var sv in pad.GetFloats())
							{
								string n = jname + sv.Item1;
								float f = sv.Item2;
								//if (n == "J5 RotationZ")
								//	System.Diagnostics.Debugger.Break();
								if (trackdeltas)
									FloatDeltas[n] += Math.Abs(f - FloatValues[n]);
								FloatValues[n] = f;
							}
						}

						// analyse moose
						// other sorts of mouse api (raw input) could easily be added as a separate listing under a different class
						if (WantingMouseFocus.Contains(System.Windows.Forms.Form.ActiveForm))
						{
							var P = System.Windows.Forms.Control.MousePosition;
							if (trackdeltas)
							{
								// these are relative to screen coordinates, but that's not terribly important
								FloatDeltas["WMouse X"] += Math.Abs(P.X - FloatValues["WMouse X"]) * 50;
								FloatDeltas["WMouse Y"] += Math.Abs(P.Y - FloatValues["WMouse Y"]) * 50;
							}
							// coordinate translation happens later
							FloatValues["WMouse X"] = P.X;
							FloatValues["WMouse Y"] = P.Y;

							var B = System.Windows.Forms.Control.MouseButtons;
							HandleButton("WMouse L", (B & System.Windows.Forms.MouseButtons.Left) != 0, InputFocus.Mouse);
							HandleButton("WMouse C", (B & System.Windows.Forms.MouseButtons.Middle) != 0, InputFocus.Mouse);
							HandleButton("WMouse R", (B & System.Windows.Forms.MouseButtons.Right) != 0, InputFocus.Mouse);
							HandleButton("WMouse 1", (B & System.Windows.Forms.MouseButtons.XButton1) != 0, InputFocus.Mouse);
							HandleButton("WMouse 2", (B & System.Windows.Forms.MouseButtons.XButton2) != 0, InputFocus.Mouse);
						}
						else
						{
							//dont do this: for now, it will interfere with the virtualpad. dont do something similar for the mouse position either
							//unpress all buttons
							//HandleButton("WMouse L", false);
							//HandleButton("WMouse C", false);
							//HandleButton("WMouse R", false);
							//HandleButton("WMouse 1", false);
							//HandleButton("WMouse 2", false);
						}
					}

					if (_NewEvents.Count != 0)
					{
						//WHAT!? WE SHOULD NOT BE SO NAIVELY TOUCHING MAINFORM FROM THE INPUTTHREAD. ITS BUSY RUNNING.
						AllowInput allowInput = GlobalWin.MainForm.AllowInput(false);

						foreach (var ie in _NewEvents)
						{
							//events are swallowed in some cases:
							if (ie.LogicalButton.Alt && ShouldSwallow(GlobalWin.MainForm.AllowInput(true), ie))
								continue;
							if (ie.EventType == InputEventType.Press && ShouldSwallow(allowInput, ie))
								continue;

							EnqueueEvent(ie);
						}
					}

					IgnoreEventsNextPoll = false;
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
			lock (FloatValues)
			{
				FloatDeltas.Clear();
				trackdeltas = true;
			}
		}

		public string GetNextFloatEvent()
		{
			lock (FloatValues)
			{
				foreach (var kvp in FloatDeltas)
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
			lock (FloatValues)
			{
				trackdeltas = false;
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
				if (InputEvents.Count == 0) return null;
				AllowInput allowInput = GlobalWin.MainForm.AllowInput(false);

				//wait for the first release after a press to complete input binding, because we need to distinguish pure modifierkeys from modified keys
				//if you just pressed ctrl, wanting to bind ctrl, we'd see: pressed:ctrl, unpressed:ctrl
				//if you just pressed ctrl+c, wanting to bind ctrl+c, we'd see: pressed:ctrl, pressed:ctrl+c, unpressed:ctrl+c, unpressed:ctrl
				//but in the 2nd example the unpresses will be swapped if ctrl is released first, so we'll take the last press before the release

				while (InputEvents.Count != 0)
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

					InputEvents.Clear();

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
