using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
#if WINDOWS
using SlimDX.DirectInput;
#endif

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.MultiHawk
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
#if WINDOWS
			UpdateThread = new Thread(UpdateThreadProc)
			{
				IsBackground = true, 
				Priority = ThreadPriority.AboveNormal //why not? this thread shouldn't be very heavy duty, and we want it to be responsive
			};
			UpdateThread.Start();
#endif
		}

		public static void Initialize(IntPtr parent)
		{
#if WINDOWS
			KeyInput.Initialize(parent);
			GamePad.Initialize(parent);
			BizHawk.Client.EmuHawk.GamePad360.Initialize();
#endif
			Instance = new Input();
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
			public override string ToString()
			{
				return string.Format("{0}:{1}", EventType.ToString(), LogicalButton.ToString());
			}
		}

		private readonly WorkingDictionary<string, object> ModifierState = new WorkingDictionary<string, object>();
		private readonly WorkingDictionary<string, bool> LastState = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, bool> UnpressState = new WorkingDictionary<string, bool>();
		private readonly HashSet<string> IgnoreKeys = new HashSet<string>(new[] { "LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt" });
		private readonly WorkingDictionary<string, float> FloatValues = new WorkingDictionary<string, float>();
		private readonly WorkingDictionary<string, float> FloatDeltas = new WorkingDictionary<string, float>();
		private bool trackdeltas = false;

		void HandleButton(string button, bool newState)
		{
			bool isModifier = IgnoreKeys.Contains(button);
			if (EnableIgnoreModifiers && isModifier) return;
			if (LastState[button] && newState) return;
			if (!LastState[button] && !newState) return;

			//apply 
			//NOTE: this is not quite right. if someone held leftshift+rightshift it would be broken. seems unlikely, though.
			if (button == "LeftShift")
			{
				_Modifiers &= ~ModifierKey.Shift;
				if (newState)
					_Modifiers |= ModifierKey.Shift;
			}
			if (button == "RightShift") { _Modifiers &= ~ModifierKey.Shift; if (newState) _Modifiers |= ModifierKey.Shift; }
			if (button == "LeftControl") { _Modifiers &= ~ModifierKey.Control; if (newState) _Modifiers |= ModifierKey.Control; }
			if (button == "RightControl") { _Modifiers &= ~ModifierKey.Control; if (newState) _Modifiers |= ModifierKey.Control; }
			if (button == "LeftAlt") { _Modifiers &= ~ModifierKey.Alt; if (newState) _Modifiers |= ModifierKey.Alt; }
			if (button == "RightAlt") { _Modifiers &= ~ModifierKey.Alt; if (newState) _Modifiers |= ModifierKey.Alt; }

			if (UnpressState.ContainsKey(button))
			{
				if (newState) return;
				Console.WriteLine("Removing Unpress {0} with newState {1}", button, newState);
				UnpressState.Remove(button);
				LastState[button] = false;
				return;
			}


			//dont generate events for things like Ctrl+LeftControl
			ModifierKey mods = _Modifiers;
			if (button == "LeftShift") mods &= ~ModifierKey.Shift;
			if (button == "RightShift") mods &= ~ModifierKey.Shift;
			if (button == "LeftControl") mods &= ~ModifierKey.Control;
			if (button == "RightControl") mods &= ~ModifierKey.Control;
			if (button == "LeftAlt") mods &= ~ModifierKey.Alt;
			if (button == "RightAlt") mods &= ~ModifierKey.Alt;

			var ie = new InputEvent
				{
					EventType = newState ? InputEventType.Press : InputEventType.Release,
					LogicalButton = new LogicalButton(button, mods)
				};
			LastState[button] = newState;

			//track the pressed events with modifiers that we send so that we can send corresponding unpresses with modifiers
			//this is an interesting idea, which we may need later, but not yet.
			//for example, you may see this series of events: press:ctrl+c, release:ctrl, release:c
			//but you might would rather have press:ctr+c, release:ctrl+c
			//this code relates the releases to the original presses.
			//UPDATE - this is necessary for the frame advance key, which has a special meaning when it gets stuck down
			//so, i am adding it as of 11-sep-2011
			if (newState)
			{
				ModifierState[button] = ie.LogicalButton;
			}
			else
			{
				if (ModifierState[button] != null)
				{
					LogicalButton alreadyReleased = ie.LogicalButton;
					var ieModified = new InputEvent
						{
							LogicalButton = (LogicalButton)ModifierState[button],
							EventType = InputEventType.Release
						};
					if (ieModified.LogicalButton != alreadyReleased)
						_NewEvents.Add(ieModified);
				}
				ModifierState[button] = null;
			}

			_NewEvents.Add(ie);
		}

		ModifierKey _Modifiers;
		private readonly List<InputEvent> _NewEvents = new List<InputEvent>();

		//do we need this?
		public void ClearEvents()
		{
			lock (this)
			{
				InputEvents.Clear();
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

#if WINDOWS
		void UpdateThreadProc()
		{
			for (; ; )
			{
				var keyEvents = KeyInput.Update();
				GamePad.UpdateAll();
				BizHawk.Client.EmuHawk.GamePad360.UpdateAll();

				//this block is going to massively modify data structures that the binding method uses, so we have to lock it all
				lock (this)
				{
					_NewEvents.Clear();

					//analyze keys
					foreach (var ke in keyEvents)
						HandleButton(ke.Key.ToString(), ke.Pressed);

					lock (FloatValues)
					{
						//FloatValues.Clear();

						//analyze xinput
						for (int i = 0; i < BizHawk.Client.EmuHawk.GamePad360.Devices.Count; i++)
						{
							var pad = BizHawk.Client.EmuHawk.GamePad360.Devices[i];
							string xname = "X" + (i + 1) + " ";
							for (int b = 0; b < pad.NumButtons; b++)
								HandleButton(xname + pad.ButtonName(b), pad.Pressed(b));
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
						for (int i = 0; i < GamePad.Devices.Count; i++)
						{
							var pad = GamePad.Devices[i];
							string jname = "J" + (i + 1) + " ";

							for (int b = 0; b < pad.NumButtons; b++)
								HandleButton(jname + pad.ButtonName(b), pad.Pressed(b));
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
							HandleButton("WMouse L", (B & System.Windows.Forms.MouseButtons.Left) != 0);
							HandleButton("WMouse C", (B & System.Windows.Forms.MouseButtons.Middle) != 0);
							HandleButton("WMouse R", (B & System.Windows.Forms.MouseButtons.Right) != 0);
							HandleButton("WMouse 1", (B & System.Windows.Forms.MouseButtons.XButton1) != 0);
							HandleButton("WMouse 2", (B & System.Windows.Forms.MouseButtons.XButton2) != 0);
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

					bool swallow = !GlobalWin.MainForm.AllowInput;

					foreach (var ie in _NewEvents)
					{
						//events are swallowed in some cases:
						if (ie.EventType == InputEventType.Press && swallow)
						{ }
						else
							EnqueueEvent(ie);
					}
				} //lock(this)

				//arbitrary selection of polling frequency:
				Thread.Sleep(10);
			}
		}
#endif

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
		public string GetNextBindEvent()
		{
			//this whole process is intimately involved with the data structures, which can conflict with the input thread.
			lock (this)
			{
				if (InputEvents.Count == 0) return null;
				if (!GlobalWin.MainForm.AllowInput) return null;

				//we only listen to releases for input binding, because we need to distinguish releases of pure modifierkeys from modified keys
				//if you just pressed ctrl, wanting to bind ctrl, we'd see: pressed:ctrl, unpressed:ctrl
				//if you just pressed ctrl+c, wanting to bind ctrl+c, we'd see: pressed:ctrl, pressed:ctrl+c, unpressed:ctrl+c, unpressed:ctrl
				//so its the first unpress we need to listen for

				while (InputEvents.Count != 0)
				{
					var ie = DequeueEvent();

					//as a special perk, we'll accept escape immediately
					if (ie.EventType == InputEventType.Press && ie.LogicalButton.Button == "Escape")
						goto ACCEPT;

					if (ie.EventType == InputEventType.Press) continue;

				ACCEPT:
					Console.WriteLine("Bind Event: {0} ", ie);

					foreach (var kvp in LastState)
						if (kvp.Value)
						{
							Console.WriteLine("Unpressing " + kvp.Key);
							UnpressState[kvp.Key] = true;
						}

					InputEvents.Clear();

					return ie.LogicalButton.ToString();
				}

				return null;
			}
		}

		//controls whether modifier keys will be ignored as key press events
		//this should be used by hotkey binders, but we may want modifier key events
		//to get triggered in the main form
		public bool EnableIgnoreModifiers = false;

	}
}
