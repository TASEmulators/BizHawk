using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using SlimDX.DirectInput;

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
			UpdateThread = new Thread(UpdateThreadProc);
			UpdateThread.IsBackground = true;
			UpdateThread.Start();
		}

		public static void Initialize()
		{
			KeyInput.Initialize();
			GamePad.Initialize();
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

			public override string ToString()
			{
				string ret = "";
				if ((Modifiers & ModifierKey.Control) != 0) ret += "Ctrl+";
				if ((Modifiers & ModifierKey.Alt) != 0) ret += "Alt+";
				if ((Modifiers & ModifierKey.Shift) != 0) ret += "Shift+";
				ret += Button;
				return ret;
			}
		}
		public class InputEvent
		{
			public LogicalButton LogicalButton;
			public InputEventType EventType;
		}

	
		WorkingDictionary<string, bool> LastState = new WorkingDictionary<string, bool>();


		HashSet<string> Ignore = new HashSet<string>(new[] { "LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt" });
		void HandleButton(string button, bool newState)
		{
			if (Ignore.Contains(button)) return;
			if (LastState[button] && newState) return;
			if (!LastState[button] && !newState) return;

			var ie = new InputEvent();
			ie.EventType = newState ? InputEventType.Press : InputEventType.Release;
			ie.LogicalButton = new LogicalButton(button, _Modifiers);
			_NewEvents.Add(ie);
			LastState[button] = newState;
		}

		ModifierKey _Modifiers;
		List<InputEvent> _NewEvents = new List<InputEvent>();

		//TODO - maybe need clearevents for various purposes. perhaps when returning from modal dialogs?

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
				InputEvents.Enqueue(ie);
		}

		void UpdateThreadProc()
		{
			for(;;)
			{
				KeyInput.Update();
				GamePad.UpdateAll();

				_Modifiers = KeyInput.GetModifierKeysAsKeys();
				_NewEvents.Clear();

				//analyze keys
				foreach (var key in KeyInput.State.PressedKeys) HandleButton(key.ToString(), true);
				foreach (var key in KeyInput.State.ReleasedKeys) HandleButton(key.ToString(), false);
				
				//analyze joysticks
				for (int i = 0; i < GamePad.Devices.Count; i++)
				{
					var pad = GamePad.Devices[i];
					string jname = "J" + (i + 1) + " ";
					HandleButton(jname + "Up", pad.Up);
					HandleButton(jname + "Down", pad.Down);
					HandleButton(jname + "Left", pad.Left);
					HandleButton(jname + "Right", pad.Right);

					for (int b = 0; b < pad.Buttons.Length; b++)
						HandleButton(jname + "B" + (b + 1), pad.Buttons[b]);
				}

				bool swallow = (Global.Config.AcceptBackgroundInput == false && Form.ActiveForm == null);

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
	}
}
