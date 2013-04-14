using System;
using EMU7800.Core;

namespace BizHawk.Emulation
{
	public class Atari7800Control
	{
		public static ControllerDefinition Joystick = new ControllerDefinition
		{
			Name = "Atari 7800 Joystick Controller",
			BoolButtons =
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"BW", // should be "Color"??
				"Left Difficulty", // better not put P# on these as they might not correspond to player numbers
				"Right Difficulty",
				// ports
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Trigger",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Trigger"
			}
		};
		public static ControllerDefinition Paddles = new ControllerDefinition
		{
			Name = "Atari 7800 Paddle Controller",
			BoolButtons = 
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"BW", // should be "Color"??
				"Left Difficulty", // better not put P# on these as they might not correspond to player numbers
				"Right Difficulty",
				// ports
				"P1 Trigger",
				"P2 Trigger",
				"P3 Trigger",
				"P4 Trigger"
			},
			FloatControls = // should be in [0..700000]
			{
				"P1 Paddle",
				"P2 Paddle",
				"P3 Paddle",
				"P4 Paddle"
			}
		};
		public static ControllerDefinition Keypad = new ControllerDefinition
		{
			Name = "Atari 7800 Keypad Controller",
			BoolButtons = 
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"BW", // should be "Color"??
				"Left Difficulty", // better not put P# on these as they might not correspond to player numbers
				"Right Difficulty",
				// ports
				"P1 Keypad1", "P1 Keypad2", "P1 Keypad3", 
				"P1 Keypad4", "P1 Keypad5", "P1 Keypad6", 
				"P1 Keypad7", "P1 Keypad8", "P1 Keypad9", 
				"P1 KeypadA", "P1 Keypad0", "P1 KeypadP", 
				"P2 Keypad1", "P2 Keypad2", "P2 Keypad3", 
				"P2 Keypad4", "P2 Keypad5", "P2 Keypad6", 
				"P2 Keypad7", "P2 Keypad8", "P2 Keypad9", 
				"P2 KeypadA", "P2 Keypad0", "P2 KeypadP", 
				"P3 Keypad1", "P3 Keypad2", "P3 Keypad3", 
				"P3 Keypad4", "P3 Keypad5", "P3 Keypad6", 
				"P3 Keypad7", "P3 Keypad8", "P3 Keypad9", 
				"P3 KeypadA", "P3 Keypad0", "P3 KeypadP", 
				"P4 Keypad1", "P4 Keypad2", "P4 Keypad3", 
				"P4 Keypad4", "P4 Keypad5", "P4 Keypad6", 
				"P4 Keypad7", "P4 Keypad8", "P4 Keypad9", 
				"P4 KeypadA", "P4 Keypad0", "P4 KeypadP"
			}
		};
		public static ControllerDefinition Driving = new ControllerDefinition
		{
			Name = "Atari 7800 Driving Controller",
			BoolButtons = 
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"BW", // should be "Color"??
				"Left Difficulty", // better not put P# on these as they might not correspond to player numbers
				"Right Difficulty",
				// ports
				"P1 Trigger",
				"P2 Trigger"
			},
			FloatControls = // should be in [0..3]
			{
				"P1 Driving",
				"P2 Driving"
			}
		};
		public static ControllerDefinition BoosterGrip = new ControllerDefinition
		{
			Name = "Atari 7800 Booster Grip Controller",
			BoolButtons = 
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"BW", // should be "Color"??
				"Left Difficulty", // better not put P# on these as they might not correspond to player numbers
				"Right Difficulty",
				// ports
				// NB: as referenced by the emu, p1t2 = p1t2, p1t3 = p2t2, p2t2 = p3t2, p2t3 = p4t2
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Trigger", "P1 Trigger 2", "P1 Trigger 3",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Trigger", "P2 Trigger 2", "P2 Trigger 3"
			}
		};
		public static ControllerDefinition ProLineJoystick = new ControllerDefinition
		{
			Name = "Atari 7800 ProLine Joystick Controller",
			BoolButtons =
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"Pause",
				// ports
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Trigger", "P1 Trigger 2",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Trigger", "P2 Trigger 2"
			}
		};
		public static ControllerDefinition Lightgun = new ControllerDefinition
		{
			Name = "Atari 7800 Light Gun Controller",
			BoolButtons =
			{
				// hard reset, not passed to EMU7800
				"Power",
				// on the console
				"Reset",
				"Select",
				"Pause",
				// ports
				"P1 Trigger",
				"P2 Trigger"
			},
			FloatControls = // vpos should be actual scanline number.  hpos should be in [0..319]??
			{
				"P1 VPos", "P1 HPos",
				"P2 VPos", "P2 HPos"
			}
		};

		struct ControlAdapter
		{
			public ControllerDefinition Type;
			public Controller Left;
			public Controller Right;
			public Action<IController, InputState> Convert;
			public ControlAdapter(ControllerDefinition Type, Controller Left, Controller Right, Action<IController, InputState> Convert)
			{
				this.Type = Type;
				this.Left = Left;
				this.Right = Right;
				this.Convert = Convert;
			}
		}

		static readonly ControlAdapter[] Adapters = new[]
		{
			new ControlAdapter(Joystick, Controller.Joystick, Controller.Joystick, ConvertJoystick),
			new ControlAdapter(Paddles, Controller.Paddles, Controller.Paddles, ConvertPaddles),
			new ControlAdapter(Keypad, Controller.Keypad, Controller.Keypad, ConvertKeypad),
			new ControlAdapter(Driving, Controller.Driving, Controller.Driving, ConvertDriving),
			new ControlAdapter(BoosterGrip, Controller.BoosterGrip, Controller.BoosterGrip, ConvertBoosterGrip),
			new ControlAdapter(ProLineJoystick, Controller.ProLineJoystick, Controller.ProLineJoystick, ConvertProLineJoystick),
			new ControlAdapter(Lightgun, Controller.Lightgun, Controller.Lightgun, ConvertLightgun),
		};

		static void ConvertConsoleButtons(IController c, InputState s)
		{
			s.RaiseInput(0, MachineInput.Reset, c["Reset"]);
			s.RaiseInput(0, MachineInput.Select, c["Select"]);
			s.RaiseInput(0, MachineInput.Color, c["BW"]);
			s.RaiseInput(0, MachineInput.LeftDifficulty, c["Left Difficulty"]);
			s.RaiseInput(0, MachineInput.RightDifficulty, c["Right Difficulty"]);
		}
		static void ConvertConsoleButtons7800(IController c, InputState s)
		{
			s.RaiseInput(0, MachineInput.Reset, c["Reset"]);
			s.RaiseInput(0, MachineInput.Select, c["Select"]);
			s.RaiseInput(0, MachineInput.Color, c["Pause"]);
		}
		static void ConvertDirections(IController c, InputState s, int p)
		{
			string ps = string.Format("P{0} ", p + 1);
			s.RaiseInput(p, MachineInput.Up, c[ps + "Up"]);
			s.RaiseInput(p, MachineInput.Down, c[ps + "Down"]);
			s.RaiseInput(p, MachineInput.Left, c[ps + "Left"]);
			s.RaiseInput(p, MachineInput.Right, c[ps + "Right"]);
		}
		static void ConvertTrigger(IController c, InputState s, int p)
		{
			string ps = string.Format("P{0} ", p + 1);
			s.RaiseInput(p, MachineInput.Fire, c[ps + "Trigger"]);
		}

		static void ConvertJoystick(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons(c, s);
			ConvertDirections(c, s, 0);
			ConvertDirections(c, s, 1);
			ConvertTrigger(c, s, 0);
			ConvertTrigger(c, s, 1);
		}
		static void ConvertPaddles(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons(c, s);
			for (int i = 0; i < 4; i++)
			{
				string ps = string.Format("P{0} ", i + 1);
				ConvertTrigger(c, s, i);
				s.RaisePaddleInput(i, 700000, (int)c.GetFloat(ps + "Trigger"));
			}
		}
		static void ConvertKeypad(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons(c, s);
			for (int i = 0; i < 4; i++)
			{
				string ps = string.Format("P{0} ", i + 1);
				s.RaiseInput(i, MachineInput.NumPad1, c[ps + "Keypad1"]);
				s.RaiseInput(i, MachineInput.NumPad2, c[ps + "Keypad2"]);
				s.RaiseInput(i, MachineInput.NumPad3, c[ps + "Keypad3"]);
				s.RaiseInput(i, MachineInput.NumPad4, c[ps + "Keypad4"]);
				s.RaiseInput(i, MachineInput.NumPad5, c[ps + "Keypad5"]);
				s.RaiseInput(i, MachineInput.NumPad6, c[ps + "Keypad6"]);
				s.RaiseInput(i, MachineInput.NumPad7, c[ps + "Keypad7"]);
				s.RaiseInput(i, MachineInput.NumPad8, c[ps + "Keypad8"]);
				s.RaiseInput(i, MachineInput.NumPad9, c[ps + "Keypad9"]);
				s.RaiseInput(i, MachineInput.NumPadMult, c[ps + "KeypadA"]);
				s.RaiseInput(i, MachineInput.NumPad0, c[ps + "Keypad0"]);
				s.RaiseInput(i, MachineInput.NumPadHash, c[ps + "KeypadP"]);
			}
		}
		static MachineInput[] drvlut = new[]
		{
			MachineInput.Driving0,
			MachineInput.Driving1,
			MachineInput.Driving2,
			MachineInput.Driving3
		};
		static void ConvertDriving(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons(c, s);
			ConvertTrigger(c, s, 0);
			ConvertTrigger(c, s, 1);
			s.RaiseInput(0, drvlut[(int)c.GetFloat("P1 Driving")], true);
			s.RaiseInput(1, drvlut[(int)c.GetFloat("P2 Driving")], true);
		}
		static void ConvertBoosterGrip(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons(c, s);
			ConvertDirections(c, s, 0);
			ConvertDirections(c, s, 1);
			// weird mapping is intentional
			s.RaiseInput(0, MachineInput.Fire, c["P1 Trigger"]);
			s.RaiseInput(0, MachineInput.Fire2, c["P1 Trigger 2"]);
			s.RaiseInput(1, MachineInput.Fire2, c["P1 Trigger 3"]);
			s.RaiseInput(1, MachineInput.Fire, c["P2 Trigger"]);
			s.RaiseInput(2, MachineInput.Fire2, c["P2 Trigger 2"]);
			s.RaiseInput(3, MachineInput.Fire2, c["P2 Trigger 3"]);
		}
		static void ConvertProLineJoystick(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons7800(c, s);
			ConvertDirections(c, s, 0);
			ConvertDirections(c, s, 1);
			s.RaiseInput(0, MachineInput.Fire, c["P1 Trigger"]);
			s.RaiseInput(0, MachineInput.Fire2, c["P1 Trigger 2"]);
			s.RaiseInput(1, MachineInput.Fire, c["P2 Trigger"]);
			s.RaiseInput(1, MachineInput.Fire2, c["P2 Trigger 2"]);
		}
		static void ConvertLightgun(IController c, InputState s)
		{
			s.ClearAllInput();
			ConvertConsoleButtons7800(c, s);
			ConvertTrigger(c, s, 0);
			ConvertTrigger(c, s, 1);
			s.RaiseLightgunPos(0, (int)c.GetFloat("P1 VPos"), (int)c.GetFloat("P1 HPos"));
			s.RaiseLightgunPos(1, (int)c.GetFloat("P2 VPos"), (int)c.GetFloat("P2 HPos"));
		}

		public Action<IController, InputState> Convert { get; private set; }

		public ControllerDefinition ControlType { get; private set; }

		public Atari7800Control(MachineBase mac)
		{
			var l = mac.InputState.LeftControllerJack;
			var r = mac.InputState.RightControllerJack;

			foreach (var a in Adapters)
			{
				if (a.Left == l && a.Right == r)
				{
					Convert = a.Convert;
					ControlType = a.Type;
					return;
				}
			}
			throw new Exception(string.Format("Couldn't connect Atari 7800 controls \"{0}\" and \"{1}\"", l.ToString(), r.ToString()));
		}
	}
}
