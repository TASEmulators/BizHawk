using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	/// <summary>
	/// Represents a controller plugged into a controller port on the intellivision
	/// </summary>
	public interface IPort
	{
		byte Read(IController c, bool left_mode);

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

		public byte Read(IController c, bool left_mode)
		{
			return 0; // needs checking
		}

		public ControllerDefinition Definition { get; private set; }

		public void SyncState(Serializer ser)
		{
			// Do nothing
		}

		public int PortNum { get; private set; }
	}

	[DisplayName("ColecoVision Basic Controller")]
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

		public int PortNum { get; private set; }

		public byte Read(IController c, bool left_mode)
		{
			if (left_mode)
			{
				byte retval = 0x7F;
				if (c.IsPressed(Definition.BoolButtons[0])) retval &= 0xFE;
				if (c.IsPressed(Definition.BoolButtons[1])) retval &= 0xFD;
				if (c.IsPressed(Definition.BoolButtons[2])) retval &= 0xFB;
				if (c.IsPressed(Definition.BoolButtons[3])) retval &= 0xF7;
				if (c.IsPressed(Definition.BoolButtons[4])) retval &= 0x3F;
				return retval;
			}
			else
			{
				byte retval = 0xF;
				//                                   0x00;
				if (c.IsPressed(Definition.BoolButtons[14])) retval = 0x01;
				if (c.IsPressed(Definition.BoolButtons[10])) retval = 0x02;
				if (c.IsPressed(Definition.BoolButtons[11])) retval = 0x03;
				//                                             0x04;
				if (c.IsPressed(Definition.BoolButtons[13])) retval = 0x05;
				if (c.IsPressed(Definition.BoolButtons[16])) retval = 0x06;
				if (c.IsPressed(Definition.BoolButtons[8])) retval = 0x07;
				//                                             0x08;
				if (c.IsPressed(Definition.BoolButtons[17])) retval = 0x09;
				if (c.IsPressed(Definition.BoolButtons[6])) retval = 0x0A;
				if (c.IsPressed(Definition.BoolButtons[15])) retval = 0x0B;
				if (c.IsPressed(Definition.BoolButtons[9])) retval = 0x0C;
				if (c.IsPressed(Definition.BoolButtons[7])) retval = 0x0D;
				if (c.IsPressed(Definition.BoolButtons[12])) retval = 0x0E;

				if (c.IsPressed(Definition.BoolButtons[5]) == false) retval |= 0x40;
				retval |= 0x30; // always set these bits
				return retval;
			}
		}

		public ControllerDefinition Definition { get; private set; }


		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Right", "Down", "Left", "L", "R",
			"Key 0", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5",
			"Key 6", "Key 7", "Key 8", "Key 9", "Pound", "Star"
		};
	}

	[DisplayName("Turbo Controller")]
	public class ColecoTurboController : IPort
	{
		public ColecoTurboController(int portNum)
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

		public int PortNum { get; private set; }

		public ControllerDefinition Definition { get; private set; }

		public byte Read(IController c, bool left_mode)
		{
			if (left_mode)
			{
				byte retval = 0x4B;
				
				if (c.IsPressed(Definition.BoolButtons[0])) retval &= 0x3F;

				int x = (int)c.GetFloat(Definition.FloatControls[0]);
				int y = (int)c.GetFloat(Definition.FloatControls[1]);
				retval |= CalcDirection(x, y);
				
				return retval;
			} else
			{
				byte retval = 0x4B;
				if (c.IsPressed(Definition.BoolButtons[0])) retval &= 0x3F;

				int x = (int)c.GetFloat(Definition.FloatControls[0]);
				int y = (int)c.GetFloat(Definition.FloatControls[1]);
				retval |= CalcDirection(x, y);
				
				return retval;
			}
		}

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseBoolDefinition =
		{
			"Pedal"
		};

		// x and y are both assumed to be in [-127, 127]
		// x increases from left to right
		// y increases from top to bottom
		private static byte CalcDirection(int x, int y)
		{
			y = -y; // vflip to match the arrangement of FloatControllerButtons
			/*
			// deadzone: if we're less than ? units from the origin, return no direction
			if (x * x + y * y < Deadzone * Deadzone)
			{
				return 0x0F; // nothing pressed
			}
			*/
			if ((y >= 0 && y>=Math.Abs(x)))
				return 0x3F;
			if ((y < 0 && Math.Abs(y) >= Math.Abs(x)))
				return 0x1F;
			if ((x > 0 && Math.Abs(y) < x))
				return 0x0F;
			if ((x < 0 && Math.Abs(y) < Math.Abs(x)))
				return 0x2F;

			Console.WriteLine("Error");
			return 0x1F;
		}

		//private const int Deadzone = 50;
	}
}
