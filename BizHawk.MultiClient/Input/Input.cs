using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using SlimDX.DirectInput;

//maybe todo - split into an event processor class (which can grab events from the main input class and be used to step through them independently)

namespace BizHawk.MultiClient
{
	public class Input
	{
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
			public LogicalButton(string button, Keys modifiers)
			{
				Button = button;
				Modifiers = modifiers;
			}
			public string Button;
			public Keys Modifiers;

			public override string ToString()
			{
				string ret = "";
				if ((Modifiers & Keys.Control) != 0) ret += "Ctrl+";
				if ((Modifiers & Keys.Alt) != 0) ret += "Alt+";
				if ((Modifiers & Keys.Shift) != 0) ret += "Shift+";
				ret += Button;
				return ret;
			}
		}
		public class InputEvent
		{
			public LogicalButton LogicalButton;
			public InputEventType EventType;
		}

		
		//coalesces events back into instantaneous states
		class InputCoalescer
		{
			public void Receive(InputEvent ie)
			{
				bool state = ie.EventType == InputEventType.Press;
				State[ie.LogicalButton.ToString()] = state;
				LogicalButton unmodified = ie.LogicalButton;
				unmodified.Modifiers = Keys.None;
				UnmodifiedState[unmodified.ToString()] = state;
			}

			public WorkingDictionary<string, bool> State = new WorkingDictionary<string, bool>();

			public WorkingDictionary<string, bool> UnmodifiedState = new WorkingDictionary<string, bool>();
		}

		InputCoalescer Coalescer = new InputCoalescer();


		WorkingDictionary<string, bool> LastState = new WorkingDictionary<string, bool>();


		HashSet<string> Ignore = new HashSet<string>(new[] { "LeftShift", "RightShift" });
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

		Keys _Modifiers;
		List<InputEvent> _NewEvents = new List<InputEvent>();

		//TODO - maybe need clearevents for various purposes? maybe not.
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
				Coalescer.Receive(ie);
			}
		}

		public bool CheckState(string button) { lock (this) return Coalescer.State[button]; }
		public bool CheckStateUnmodified(string button) { lock (this) return Coalescer.UnmodifiedState[button]; }

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

		public bool IsPressed(string control)
		{
			return Instance.CheckState(control);
		}

		public string GetNextPressedButtonOrNull()
		{
			InputEvent ie = Instance.DequeueEvent();
			if (ie == null) return null;
			if (ie.EventType == InputEventType.Release) return null;
			return ie.LogicalButton.ToString();
		}
	}
}
