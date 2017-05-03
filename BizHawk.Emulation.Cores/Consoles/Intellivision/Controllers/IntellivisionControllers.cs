using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	/// <summary>
	/// Represents a controller plugged into a controller port on the intellivision
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[DisplayName("Unplugged Controller")]
	public class UnpluggedController : IPort
	{
		public UnpluggedController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = new List<string>()
			};
		}

		public byte Read(IController c)
		{
			return 0;
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Do nothing
		}

		public int PortNum { get; }
	}

	[DisplayName("Standard Controller")]
	public class StandardController : IPort
	{
		public StandardController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0;
			for (int i = 0; i < 31; i++)
			{
				if (c.IsPressed(Definition.BoolButtons[i]))
				{
					result |= HandControllerButtons[i];
				}
			}

			return result;
		}

		public ControllerDefinition Definition { get; }


		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseDefinition =
		{
			"L", "R", "Top",
			"Key 0", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5",
			"Key 6", "Key 7", "Key 8", "Key 9", "Enter", "Clear",
			"N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
			"S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW",
		};

		private static byte[] HandControllerButtons =
		{
			0x60, // OUTPUT_ACTION_BUTTON_BOTTOM_LEFT
			0xC0, // OUTPUT_ACTION_BUTTON_BOTTOM_RIGHT
			0xA0, // OUTPUT_ACTION_BUTTON_TOP
			0x48, // OUTPUT_KEYPAD_ZERO
			0x81, // OUTPUT_KEYPAD_ONE
			0x41, // OUTPUT_KEYPAD_TWO
			0x21, // OUTPUT_KEYPAD_THREE
			0x82, // OUTPUT_KEYPAD_FOUR
			0x42, // OUTPUT_KEYPAD_FIVE
			0x22, // OUTPUT_KEYPAD_SIX
			0x84, // OUTPUT_KEYPAD_SEVEN
			0x44, // OUTPUT_KEYPAD_EIGHT
			0x24, // OUTPUT_KEYPAD_NINE
			0x28, // OUTPUT_KEYPAD_ENTER
			0x88, // OUTPUT_KEYPAD_CLEAR

			0x04, // OUTPUT_DISC_NORTH
			0x14, // OUTPUT_DISC_NORTH_NORTH_EAST
			0x16, // OUTPUT_DISC_NORTH_EAST
			0x06, // OUTPUT_DISC_EAST_NORTH_EAST
			0x02, // OUTPUT_DISC_EAST
			0x12, // OUTPUT_DISC_EAST_SOUTH_EAST
			0x13, // OUTPUT_DISC_SOUTH_EAST
			0x03, // OUTPUT_DISC_SOUTH_SOUTH_EAST
			0x01, // OUTPUT_DISC_SOUTH
			0x11, // OUTPUT_DISC_SOUTH_SOUTH_WEST
			0x19, // OUTPUT_DISC_SOUTH_WEST
			0x09, // OUTPUT_DISC_WEST_SOUTH_WEST
			0x08, // OUTPUT_DISC_WEST
			0x18, // OUTPUT_DISC_WEST_NORTH_WEST
			0x1C, // OUTPUT_DISC_NORTH_WEST
			0x0C  // OUTPUT_DISC_NORTH_NORTH_WEST
		};
	}

	[DisplayName("Standard (Analog Disc)")]
	public class FakeAnalogController : IPort
	{
		public FakeAnalogController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = BaseBoolDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList(),
				FloatControls = { "P" + PortNum + " Disc X", "P" + PortNum + " Disc Y" },
				FloatRanges = { new[] { -127.0f, 0, 127.0f }, new[] { -127.0f, 0, 127.0f } }
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c)
		{
			byte result = 0;
			for (int i = 0; i < 15; i++)
			{
				if (c.IsPressed(Definition.BoolButtons[i]))
				{
					result |= BoolControllerButtons[i];
				}
			}

			int x = (int)c.GetFloat(Definition.FloatControls[0]);
			int y = (int)c.GetFloat(Definition.FloatControls[1]);
			result |= CalcDirection(x, y);

			return result;
		}

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseBoolDefinition =
		{
			"L", "R", "Top",
			"Key 0", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5",
			"Key 6", "Key 7", "Key 8", "Key 9", "Enter", "Clear"
		};

		private static byte[] BoolControllerButtons =
		{
			0x60, // OUTPUT_ACTION_BUTTON_BOTTOM_LEFT
			0xC0, // OUTPUT_ACTION_BUTTON_BOTTOM_RIGHT
			0xA0, // OUTPUT_ACTION_BUTTON_TOP
			0x48, // OUTPUT_KEYPAD_ZERO
			0x81, // OUTPUT_KEYPAD_ONE
			0x41, // OUTPUT_KEYPAD_TWO
			0x21, // OUTPUT_KEYPAD_THREE
			0x82, // OUTPUT_KEYPAD_FOUR
			0x42, // OUTPUT_KEYPAD_FIVE
			0x22, // OUTPUT_KEYPAD_SIX
			0x84, // OUTPUT_KEYPAD_SEVEN
			0x44, // OUTPUT_KEYPAD_EIGHT
			0x24, // OUTPUT_KEYPAD_NINE
			0x28, // OUTPUT_KEYPAD_ENTER
			0x88, // OUTPUT_KEYPAD_CLEAR
		};

		// x and y are both assumed to be in [-127, 127]
		// x increases from left to right
		// y increases from top to bottom
		private static byte CalcDirection(int x, int y)
		{
			y = -y; // vflip to match the arrangement of FloatControllerButtons

			// deadzone: if we're less than ? units from the origin, return no direction
			if ((x * x) + (y * y) < Deadzone * Deadzone)
			{
				return 0; // nothing pressed
			}

			double t = Math.Atan2(y, x) * 8.0 / Math.PI;
			int i = (int)Math.Round(t);

			return FloatControllerButtons[i & 15];
		}

		private const int Deadzone = 50;

		private static byte[] FloatControllerButtons =
		{
			0x02, // E
			0x06, // ENE
			0x16, // NE
			0x14, // NNE

			0x04, // N
			0x0C, // NNW
			0x1C, // NW
			0x18, // WNW

			0x08, // W
			0x09, // WSW
			0x19, // SW
			0x11, // SSW

			0x01, // S
			0x03, // SSE
			0x13, // SE
			0x12, // ESE
		};
	}
}
