using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		public static ControllerDefinition Keypads = new ControllerDefinition
		{
			Name = "Atari 7800 Keypad Controller",
			BoolButtons = 
			{
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
				// on the console
				"Reset",
				"Select",
				"BW", // should be "Color"??
				"Left Difficulty", // better not put P# on these as they might not correspond to player numbers
				"Right Difficulty",
				// ports
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Trigger", "P1 Trigger 2",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Trigger", "P2 Trigger 2"
			}
		};
		public static ControllerDefinition LightGunController = new ControllerDefinition
		{
			Name = "Atari 7800 Light Gun Controller",
			BoolButtons =
			{
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
			FloatControls = // vpos should be actual scanline number.  hpos should be in [0..319]??
			{
				"P1 VPos", "P1 HPos",
				"P2 VPos", "P2 HPos"
			}
		};
	}
}
