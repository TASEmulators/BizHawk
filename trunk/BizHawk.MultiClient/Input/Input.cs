using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
#if WINDOWS
using SlimDX.DirectInput;
#endif

namespace BizHawk.MultiClient
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
			UpdateThread = new Thread(UpdateThreadProc) {IsBackground = true};
			UpdateThread.Start();
#endif
		}

		public static void Initialize()
		{
#if WINDOWS
			KeyInput.Initialize();
			GamePad.Initialize();
			GamePad360.Initialize();
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
		private readonly List<Tuple<string, float>> FloatValues = new List<Tuple<string, float>>();

		void HandleButton(string button, bool newState)
		{
			if (EnableIgnoreModifiers && IgnoreKeys.Contains(button)) return;
			if (LastState[button] && newState) return;
			if (!LastState[button] && !newState) return;
			
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
							LogicalButton = (LogicalButton) ModifierState[button],
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
			List<Tuple<string, float>> FloatValuesCopy;
			lock (FloatValues)
			{
				FloatValuesCopy = new List<Tuple<string, float>>(FloatValues);
			}
			return FloatValuesCopy;
		}

#if WINDOWS
		void UpdateThreadProc()
		{
			for (; ; )
			{
				KeyInput.Update();
				GamePad.UpdateAll();
				GamePad360.UpdateAll();

				_Modifiers = KeyInput.GetModifierKeysAsKeys();
				_NewEvents.Clear();

				//analyze keys
				var bleh = new HashSet<Key>();
				foreach (var k in KeyInput.State.PressedKeys)
					bleh.Add(k);
				foreach (var k in KeyInput.State.AllKeys)
					if (bleh.Contains(k))
						HandleButton(k.ToString(), true);
					else
						HandleButton(k.ToString(), false);

				lock (FloatValues)
				{
					FloatValues.Clear();

					//analyze xinput
					for (int i = 0; i < GamePad360.Devices.Count; i++)
					{
						var pad = GamePad360.Devices[i];
						string xname = "X" + (i + 1) + " ";
						for (int b = 0; b < pad.NumButtons; b++)
							HandleButton(xname + pad.ButtonName(b), pad.Pressed(b));
					}

					//analyze joysticks
					for (int i = 0; i < GamePad.Devices.Count; i++)
					{
						var pad = GamePad.Devices[i];
						string jname = "J" + (i + 1) + " ";

						for (int b = 0; b < pad.NumButtons; b++)
							HandleButton(jname + pad.ButtonName(b), pad.Pressed(b));
						foreach (var sv in pad.GetFloats())
							FloatValues.Add(new Tuple<string, float>(jname + sv.Item1, sv.Item2));
					}

				}

				bool swallow = !Global.MainForm.AllowInput;

				foreach (var ie in _NewEvents)
				{
					//events are swallowed in some cases:
					if (ie.EventType == InputEventType.Press && swallow)
					{ }
					else
						EnqueueEvent(ie);
				}

				//arbitrary selection of polling frequency:
				Thread.Sleep(10);
			}
		}
#endif

		public void Update()
		{
			//TODO - for some reason, we may want to control when the next event processing step happens
			//so i will leave this method here for now..
		}

		//returns the next Press event, if available. should be useful
		public string GetNextBindEvent()
		{
			if (InputEvents.Count == 0) return null;
			if (!Global.MainForm.AllowInput) return null;

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

		//controls whether modifier keys will be ignored as key press events
		//this should be used by hotkey binders, but we may want modifier key events
		//to get triggered in the main form
		public bool EnableIgnoreModifiers = false;

	}
}
