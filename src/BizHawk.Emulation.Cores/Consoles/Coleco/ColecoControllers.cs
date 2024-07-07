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
		byte Read(IController c, bool leftMode, bool updateWheel, float wheelAngle);

		float UpdateWheel(IController c);

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
			Definition = new("(ColecoVision Basic Controller fragment)");
		}

		public byte Read(IController c, bool leftMode, bool updateWheel, float wheelAngle)
		{
			return 0x7F; // needs checking
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Do nothing
		}

		public int PortNum { get; }

		public float UpdateWheel(IController c) => 0;
	}

	[DisplayName("ColecoVision Basic Controller")]
	public class StandardController : IPort
	{
		public StandardController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(ColecoVision Basic Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public byte Read(IController c, bool leftMode, bool updateWheel, float wheelAngle)
		{
			if (leftMode)
			{
				byte retVal = 0x7F;
				if (c.IsPressed(Definition.BoolButtons[0])) retVal &= 0xFE;
				if (c.IsPressed(Definition.BoolButtons[1])) retVal &= 0xFD;
				if (c.IsPressed(Definition.BoolButtons[2])) retVal &= 0xFB;
				if (c.IsPressed(Definition.BoolButtons[3])) retVal &= 0xF7;
				if (c.IsPressed(Definition.BoolButtons[4])) retVal &= 0x3F;
				return retVal;
			}
			else
			{
				byte retVal = 0xF;
				//                                   0x00;
				if (c.IsPressed(Definition.BoolButtons[14])) retVal = 0x01;
				if (c.IsPressed(Definition.BoolButtons[10])) retVal = 0x02;
				if (c.IsPressed(Definition.BoolButtons[11])) retVal = 0x03;
				//                                             0x04;
				if (c.IsPressed(Definition.BoolButtons[13])) retVal = 0x05;
				if (c.IsPressed(Definition.BoolButtons[16])) retVal = 0x06;
				if (c.IsPressed(Definition.BoolButtons[8])) retVal = 0x07;
				//                                             0x08;
				if (c.IsPressed(Definition.BoolButtons[17])) retVal = 0x09;
				if (c.IsPressed(Definition.BoolButtons[6])) retVal = 0x0A;
				if (c.IsPressed(Definition.BoolButtons[15])) retVal = 0x0B;
				if (c.IsPressed(Definition.BoolButtons[9])) retVal = 0x0C;
				if (c.IsPressed(Definition.BoolButtons[7])) retVal = 0x0D;
				if (c.IsPressed(Definition.BoolButtons[12])) retVal = 0x0E;

				if (!c.IsPressed(Definition.BoolButtons[5])) retVal |= 0x40;
				retVal |= 0x30; // always set these bits
				return retVal;
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

		public float UpdateWheel(IController c) => 0;
	}

	[DisplayName("Turbo Controller")]
	public class ColecoTurboController : IPort
	{
		public ColecoTurboController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("(ColecoVision Basic Controller fragment)") { BoolButtons = BaseBoolDefinition.Select(b => $"P{PortNum} {b}").ToList() }
				.AddXYPair($"P{PortNum} Disc {{0}}", AxisPairOrientation.RightAndUp, (-127).RangeTo(127), 0); //TODO verify direction against hardware
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c, bool leftMode, bool updateWheel, float wheelAngle)
		{
			if (leftMode)
			{
				byte retVal = 0x4F;
				
				if (c.IsPressed(Definition.BoolButtons[0])) retVal &= 0x3F;
				
				float x = c.AxisValue(Definition.Axes[0]);
				float y = c.AxisValue(Definition.Axes[1]);

				var angle = updateWheel ? wheelAngle : CalcDirection(x, y);
				
				byte temp2 = 0;

				int temp1 = (int)Math.Floor(angle / 1.25);
				temp1 %= 4;

				if (temp1 == 0)
				{
					temp2 = 0x10;
				}

				if (temp1 == 1)
				{
					temp2 = 0x30;
				}
				if (temp1 == 2)
				{
					temp2 = 0x20;
				}

				if (temp1 == 3)
				{
					temp2 = 0x00;
				}


				retVal |= temp2;
				
				return retVal;
			}

			return  0x7F;
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
		private static float CalcDirection(float x, float y)
		{
			y = -y; // vflip to match the arrangement of FloatControllerButtons

			// the wheel is arranged in a grey coded configuration of sensitivity ~2.5 degrees
			// for each signal
			// so overall the value returned changes every 1.25 degrees

			float angle = (float)(Math.Atan2(y, x) * 180.0/Math.PI);

			if (angle < 0)
			{
				angle = 360 + angle;
			}

			return angle;
		}

		public float UpdateWheel(IController c)
		{
			float x = c.AxisValue(Definition.Axes[0]);
			float y = c.AxisValue(Definition.Axes[1]);
			return CalcDirection(x, y);
		}
	}

	[DisplayName("Super Action Controller")]
	public class ColecoSuperActionController : IPort
	{
		public ColecoSuperActionController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("(ColecoVision Basic Controller fragment)") { BoolButtons = BaseBoolDefinition.Select(b => $"P{PortNum} {b}").ToList() }
				.AddXYPair($"P{PortNum} Disc {{0}}", AxisPairOrientation.RightAndUp, (-127).RangeTo(127), 0); //TODO verify direction against hardware
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c, bool left_mode, bool updateWheel, float wheelAngle)
		{
			if (left_mode)
			{
				byte retVal = 0x4F;
				if (c.IsPressed(Definition.BoolButtons[0])) retVal &= 0xFE;
				if (c.IsPressed(Definition.BoolButtons[1])) retVal &= 0xFD;
				if (c.IsPressed(Definition.BoolButtons[2])) retVal &= 0xFB;
				if (c.IsPressed(Definition.BoolButtons[3])) retVal &= 0xF7;
				if (c.IsPressed(Definition.BoolButtons[4])) retVal &= 0x3F;

				float x = c.AxisValue(Definition.Axes[0]);
				float y = c.AxisValue(Definition.Axes[1]);

				var angle = updateWheel ? wheelAngle : CalcDirection(x, y);

				byte temp2 = 0;

				int temp1 = (int)Math.Floor(angle / 1.25);
				temp1 %= 4;

				if (temp1 == 0)
				{
					temp2 = 0x10;
				}

				if (temp1 == 1)
				{
					temp2 = 0x30;
				}
				if (temp1 == 2)
				{
					temp2 = 0x20;
				}

				if (temp1 == 3)
				{
					temp2 = 0x00;
				}

				retVal |= temp2;

				return retVal;
			}
			else
			{
				byte retVal = 0xF;
				//                                   0x00;
				if (c.IsPressed(Definition.BoolButtons[14])) retVal = 0x01;
				if (c.IsPressed(Definition.BoolButtons[10])) retVal = 0x02;
				if (c.IsPressed(Definition.BoolButtons[11])) retVal = 0x03;
				//                                             0x04;
				if (c.IsPressed(Definition.BoolButtons[13])) retVal = 0x05;
				if (c.IsPressed(Definition.BoolButtons[16])) retVal = 0x06;
				if (c.IsPressed(Definition.BoolButtons[8])) retVal = 0x07;
				//                                             0x08;
				if (c.IsPressed(Definition.BoolButtons[17])) retVal = 0x09;
				if (c.IsPressed(Definition.BoolButtons[6])) retVal = 0x0A;
				if (c.IsPressed(Definition.BoolButtons[15])) retVal = 0x0B;
				if (c.IsPressed(Definition.BoolButtons[9])) retVal = 0x0C;
				if (c.IsPressed(Definition.BoolButtons[7])) retVal = 0x0D;
				if (c.IsPressed(Definition.BoolButtons[12])) retVal = 0x0E;

				// extra buttons for SAC
				if (c.IsPressed(Definition.BoolButtons[18])) retVal = 0x04;
				if (c.IsPressed(Definition.BoolButtons[19])) retVal = 0x08;

				if (!c.IsPressed(Definition.BoolButtons[5])) retVal |= 0x40;
				retVal |= 0x30; // always set these bits
				return retVal;
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

		// x and y are both assumed to be in [-127, 127]
		// x increases from left to right
		// y increases from top to bottom
		private static float CalcDirection(float x, float y)
		{
			y = -y; // vflip to match the arrangement of FloatControllerButtons

			// the wheel is arranged in a grey coded configuration of sensitivity ~2.5 degrees
			// for each signal
			// so overall the value returned changes every 1.25 degrees

			float angle = (float)(Math.Atan2(y, x) * 180.0 / Math.PI);

			if (angle < 0)
			{
				angle = 360 + angle;
			}

			return angle;
		}

		public float UpdateWheel(IController c)
		{
			float x = c.AxisValue(Definition.Axes[0]);
			float y = c.AxisValue(Definition.Axes[1]);
			return CalcDirection(x, y);
		}
	}
}
