using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
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
			Buttons[ie.LogicalButton.ToString()] = state;
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
		Thread UpdateThread;

		private Input()
		{
#if WINDOWS
			UpdateThread = new Thread(UpdateThreadProc);
			UpdateThread.IsBackground = true;
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
			public string Button;
			public ModifierKey Modifiers;

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
			public static bool operator==(LogicalButton lhs, LogicalButton rhs)
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

		WorkingDictionary<string, object> ModifierState = new WorkingDictionary<string, object>();
		WorkingDictionary<string, bool> LastState = new WorkingDictionary<string, bool>();

		HashSet<string> IgnoreKeys = new HashSet<string>(new[] { "LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt" });
		void HandleButton(string button, bool newState)
		{
			if (EnableIgnoreModifiers && IgnoreKeys.Contains(button)) return;
			if (LastState[button] && newState) return;
			if (!LastState[button] && !newState) return;

			var ie = new InputEvent();
			ie.EventType = newState ? InputEventType.Press : InputEventType.Release;
			ie.LogicalButton = new LogicalButton(button, _Modifiers);
			_NewEvents.Add(ie);
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
			        ie = new InputEvent();
			        ie.LogicalButton = (LogicalButton)ModifierState[button];
			        ie.EventType = InputEventType.Release;
			        if(ie.LogicalButton != alreadyReleased)
			            _NewEvents.Add(ie);
			    }
			    ModifierState[button] = null;
			}
		}

		ModifierKey _Modifiers;
		List<InputEvent> _NewEvents = new List<InputEvent>();

		//do we need this?
		public void ClearEvents()
		{
			lock (this)
			{
				InputEvents.Clear();
			}
		}

		Queue<InputEvent> InputEvents = new Queue<InputEvent>(); 
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
				foreach(var k in KeyInput.State.PressedKeys)
					bleh.Add(k);
				foreach (var k in KeyInput.State.AllKeys)
					if (bleh.Contains(k))
						HandleButton(k.ToString(), true);
					else
						HandleButton(k.ToString(), false);

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
		public string GetNextPressedButtonOrNull()
		{
			InputEvent ie = DequeueEvent();
			if (ie == null) return null;
			if (ie.EventType == InputEventType.Release) return null;
			return ie.LogicalButton.ToString();
		}

		//controls whether modifier keys will be ignored as key press events
		//this should be used by hotkey binders, but we may want modifier key events
		//to get triggered in the main form
		public bool EnableIgnoreModifiers = false;

	}
}
