using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	/// <summary>
	/// Represents a controller plugged into a controller port on the Colecovision
	/// </summary>
	public interface IPort
	{
		byte Read(IController c, bool leftMode, int wheel);

		int UpdateWheel(IController c, int wheel);

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

		public byte Read(IController c, bool left_mode, int wheel)
		{
			return 0; // needs checking
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Do nothing
		}

		public int PortNum { get; }

		public int UpdateWheel(IController c, int wheel)
		{
			return 0;
		}
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

		public int PortNum { get; }

		public byte Read(IController c, bool leftMode, int wheel)
		{
			if (leftMode)
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

		public ControllerDefinition Definition { get; }


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

		public int UpdateWheel(IController c, int wheel)
		{
			return 0;
		}
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

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c, bool leftMode, int wheel)
		{
			if (leftMode)
			{
				byte retval = 0x4B;
				
				if (c.IsPressed(Definition.BoolButtons[0])) retval &= 0x3F;

				int x = (int)c.GetFloat(Definition.FloatControls[0]);
				int y = (int)c.GetFloat(Definition.FloatControls[1]);
				retval |= CalcDirection(x, y);
				
				return retval;
			}
			else
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

			if (y >= 0 && x > 0)
			{
				return 0x10;
			}

			if (y >= 0 && x <= 0)
			{
				return 0x30;
			}
			if (y < 0 && x <= 0)
			{
				return 0x20;
			}

			if (y < 0 && x > 0)
			{
				return 0x00;
			}

			Console.WriteLine("Error");
			return 0x1F;
		}

		public int UpdateWheel(IController c, int wheel)
		{
			return 0;
		}
	}

	[DisplayName("Super Action Controller")]
	public class ColecoSuperActionController : IPort
	{
		public ColecoSuperActionController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				BoolButtons = BaseBoolDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList(),
				FloatControls = { "P" + PortNum + " Disc X"},
				FloatRanges = { new[] { -360.0f, 0, 360.0f }}
			};
		}

		public int PortNum { get; private set; }

		public ControllerDefinition Definition { get; private set; }

		public byte Read(IController c, bool left_mode, int wheel)
		{
			if (left_mode)
			{
				byte retval = 0x4F;
				if (c.IsPressed(Definition.BoolButtons[0])) retval &= 0xFE;
				if (c.IsPressed(Definition.BoolButtons[1])) retval &= 0xFD;
				if (c.IsPressed(Definition.BoolButtons[2])) retval &= 0xFB;
				if (c.IsPressed(Definition.BoolButtons[3])) retval &= 0xF7;
				if (c.IsPressed(Definition.BoolButtons[4])) retval &= 0x3F;

				retval |= CalcDirection(wheel);

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

				// extra buttons for SAC
				if (c.IsPressed(Definition.BoolButtons[18])) retval = 0x04;
				if (c.IsPressed(Definition.BoolButtons[19])) retval = 0x08;

				if (c.IsPressed(Definition.BoolButtons[5]) == false) retval |= 0x40;
				retval |= 0x30; // always set these bits
				return retval;
			}
		}

		public void SyncState(Serializer ser)
		{
			// nothing to do
		}

		private static readonly string[] BaseBoolDefinition =
		{
			"Up", "Right", "Down", "Left", "Yellow", "Red",
			"Key 0", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5",
			"Key 6", "Key 7", "Key 8", "Key 9", "Pound", "Star",
			"Purple", "Blue"
		};

		// positive x represents spinning to the right, negative spinning to the left
		private static byte CalcDirection(int wheel)
		{
			byte retval = 0;

			if (wheel >= 0 && wheel < 180)
			{
				retval = 0x00;
			}

			if (wheel >= 180 && wheel < 360)
			{
				retval = 0x10;
			}

			if (wheel < 0 && wheel > -180)
			{
				retval = 0x20;
			}

			if (wheel <= -180 && wheel > -360)
			{
				retval = 0x30;
			}

			return retval;
		}

		public int UpdateWheel(IController c, int wheel)
		{
			int x = (int)c.GetFloat(Definition.FloatControls[0]);

			int diff = -x;

			wheel += diff;

			if (wheel >= 360)
			{
				wheel = wheel - 360;
			}

			if (wheel <= -360)
			{
				wheel = wheel + 360;
			}

			return wheel;
		}
	}
}
