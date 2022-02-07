using System;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public enum SMSControllerTypes
	{
		Standard,
		Paddle,
		SportsPad,
		Phaser
	}

	/// <summary>
	/// Represents a SMS controller
	/// </summary>
	public interface IPort
	{
		byte Read_p1_c1(IController c);

		byte Read_p1_c2(IController c);

		byte Read_p2_c1(IController c);

		byte Read_p2_c2(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }

		bool PinStateGet(IController c);

		void PinStateSet(IController c, bool val);

		void CounterSet(IController c, int val);

		void RegionSet(IController c, bool val);
	}

	[DisplayName("Standarad Controller")]
	public class SmsController : IPort
	{
		public SmsController(int portNum)
		{
			PortNum = portNum;
			Definition = new("SMS Controller")
			{
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read_p1_c1(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0])) result &= 0xFE;
			if (c.IsPressed(Definition.BoolButtons[1])) result &= 0xFD;
			if (c.IsPressed(Definition.BoolButtons[2])) result &= 0xFB;
			if (c.IsPressed(Definition.BoolButtons[3])) result &= 0xF7;
			if (c.IsPressed(Definition.BoolButtons[4])) result &= 0xEF;
			if (c.IsPressed(Definition.BoolButtons[5])) result &= 0xDF;

			return result;
		}

		public byte Read_p1_c2(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0])) result &= 0xBF;
			if (c.IsPressed(Definition.BoolButtons[1])) result &= 0x7F;

			return result;
		}

		public byte Read_p2_c1(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c2(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[2])) result &= 0xFE;
			if (c.IsPressed(Definition.BoolButtons[3])) result &= 0xFD;
			if (c.IsPressed(Definition.BoolButtons[4])) result &= 0xFB;
			if (c.IsPressed(Definition.BoolButtons[5])) result &= 0xF7;

			return result;
		}

		public bool PinStateGet(IController c) { return false; }

		public void PinStateSet(IController c, bool val) { }

		public void CounterSet(IController c, int val) { }

		public void RegionSet(IController c, bool val) { }

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "B1", "B2"
		};

		public void SyncState(Serializer ser)
		{
			// nothing
		}
	}

	[DisplayName("Game Gear Controller")]
	public class GGController : IPort
	{
		public GGController(int portNum)
		{
			PortNum = portNum;
			Definition = new("GG Controller")
			{
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read_p1_c1(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0])) result &= 0xFE;
			if (c.IsPressed(Definition.BoolButtons[1])) result &= 0xFD;
			if (c.IsPressed(Definition.BoolButtons[2])) result &= 0xFB;
			if (c.IsPressed(Definition.BoolButtons[3])) result &= 0xF7;
			if (c.IsPressed(Definition.BoolButtons[4])) result &= 0xEF;
			if (c.IsPressed(Definition.BoolButtons[5])) result &= 0xDF;

			return result;
		}

		public byte Read_p1_c2(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c1(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c2(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public bool PinStateGet(IController c) { return false; }

		public void PinStateSet(IController c, bool val) { }

		public void CounterSet(IController c, int val) { }

		public void RegionSet(IController c, bool val) { }

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "B1", "B2", "Start"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}

	[DisplayName("SMS Paddle Controller")]
	public class SMSPaddleController : IPort
	{
		public SMSPaddleController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("SMS Paddle Controller")
			{
				BoolButtons = BaseDefinition.Select(b => $"P{PortNum} {b}").ToList()
			}.AddAxis($"P{PortNum} Paddle", 0.RangeTo(255), 128);
		}

		public int PortNum { get; }

		public bool pin;

		public ControllerDefinition Definition { get; }

		public byte Read_p1_c1(IController c)
		{
			byte result = 0xFF;

			int paddle1Pos;
			if (c.IsPressed("P1 Left"))
				paddle1Pos = 0;
			else if (c.IsPressed("P1 Right"))
				paddle1Pos = 255;
			else
				paddle1Pos = (int)c.AxisValue("P1 Paddle");

			if (pin)
			{
				if ((paddle1Pos & 0x10) == 0) result &= 0xFE;
				if ((paddle1Pos & 0x20) == 0) result &= 0xFD;
				if ((paddle1Pos & 0x40) == 0) result &= 0xFB;
				if ((paddle1Pos & 0x80) == 0) result &= 0xF7;
			}
			else
			{
				if ((paddle1Pos & 0x01) == 0) result &= 0xFE;
				if ((paddle1Pos & 0x02) == 0) result &= 0xFD;
				if ((paddle1Pos & 0x04) == 0) result &= 0xFB;
				if ((paddle1Pos & 0x08) == 0) result &= 0xF7;
			}

			if (c.IsPressed("P1 B1")) result &= 0xEF;
			if (!pin) result &= 0xDF;

			return result;
		}

		public byte Read_p1_c2(IController c)
		{
			byte result = 0xFF;

			int paddle2Pos;
			if (c.IsPressed("P2 Left"))
				paddle2Pos = 0;
			else if (c.IsPressed("P2 Right"))
				paddle2Pos = 255;
			else
				paddle2Pos = (int)c.AxisValue("P2 Paddle");

			if (pin)
			{
				if ((paddle2Pos & 0x10) == 0) result &= 0xBF;
				if ((paddle2Pos & 0x20) == 0) result &= 0x7F;
			}
			else
			{
				if ((paddle2Pos & 0x01) == 0) result &= 0xBF;
				if ((paddle2Pos & 0x02) == 0) result &= 0x7F;
			}

			return result;
		}

		public byte Read_p2_c1(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c2(IController c)
		{
			byte result = 0xFF;

			int paddle2Pos;
			if (c.IsPressed("P2 Left"))
				paddle2Pos = 0;
			else if (c.IsPressed("P2 Right"))
				paddle2Pos = 255;
			else
				paddle2Pos = (int)c.AxisValue("P2 Paddle");

			if (pin)
			{
				if ((paddle2Pos & 0x40) == 0) result &= 0xFE;
				if ((paddle2Pos & 0x80) == 0) result &= 0xFD;
			}
			else
			{
				if ((paddle2Pos & 0x04) == 0) result &= 0xFE;
				if ((paddle2Pos & 0x08) == 0) result &= 0xFD;
			}

			if (c.IsPressed("P2 B1")) result &= 0xFB;
			if (!pin) result &= 0xF7;

			return result;
		}

		public bool PinStateGet(IController c) { return pin; }

		public void PinStateSet(IController c, bool val) { pin = val;}

		public void CounterSet(IController c, int val) { }

		public void RegionSet(IController c, bool val) { }

		private static readonly string[] BaseDefinition =
		{
			"Left", "Right", "B1"
		};

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(pin), ref pin);
		}
	}

	[DisplayName("SMS Sports Pad Controller")]
	public class SMSSportsPadController : IPort
	{
		public SMSSportsPadController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("SMS Sports Pad Controller")
			{
				BoolButtons = BaseDefinition.Select(b => $"P{PortNum} {b}").ToList()
			}.AddXYPair($"P{PortNum} {{0}}", AxisPairOrientation.RightAndUp, (-64).RangeTo(63), 0); //TODO verify direction against hardware
		}

		public int PortNum { get; }

		private const int SportsPadMin = -64;
		private const int SportsPadMax = 63;

		public bool pin;
		// further state value for sports pad, may be useful for other controllers in future
		private int ControllerCounter = 3;
		private bool is_JPN;

		public ControllerDefinition Definition { get; }

		public byte Read_p1_c1(IController c)
		{
			byte result = 0xFF;

			int p1X;
			if (c.IsPressed("P1 Left"))
				p1X = SportsPadMin;
			else if (c.IsPressed("P1 Right"))
				p1X = SportsPadMax;
			else
				p1X = (int)c.AxisValue("P1 X");

			int p1Y;
			if (c.IsPressed("P1 Up"))
				p1Y = SportsPadMin;
			else if (c.IsPressed("P1 Down"))
				p1Y = SportsPadMax;
			else
				p1Y = (int)c.AxisValue("P1 Y");

			if (is_JPN)
			{
				p1X += 128;
				p1Y += 128;
			}
			else
			{
				p1X *= -1;
				p1Y *= -1;
			}

			// advance state
			if (pin && (ControllerCounter % 2 == 0))
			{
				++ControllerCounter;
			}
			else if (!pin && (ControllerCounter % 2 == 1))
			{
				if (++ControllerCounter == (is_JPN ? 6 : 4))
					ControllerCounter = 0;
			}

			switch (ControllerCounter)
			{
				case 0:
					if ((p1X & 0x10) == 0) result &= 0xFE;
					if ((p1X & 0x20) == 0) result &= 0xFD;
					if ((p1X & 0x40) == 0) result &= 0xFB;
					if ((p1X & 0x80) == 0) result &= 0xF7;
					break;
				case 1:
					if ((p1X & 0x01) == 0) result &= 0xFE;
					if ((p1X & 0x02) == 0) result &= 0xFD;
					if ((p1X & 0x04) == 0) result &= 0xFB;
					if ((p1X & 0x08) == 0) result &= 0xF7;
					break;
				case 2:
					if ((p1Y & 0x10) == 0) result &= 0xFE;
					if ((p1Y & 0x20) == 0) result &= 0xFD;
					if ((p1Y & 0x40) == 0) result &= 0xFB;
					if ((p1Y & 0x80) == 0) result &= 0xF7;
					break;
				case 3:
					if ((p1Y & 0x01) == 0) result &= 0xFE;
					if ((p1Y & 0x02) == 0) result &= 0xFD;
					if ((p1Y & 0x04) == 0) result &= 0xFB;
					if ((p1Y & 0x08) == 0) result &= 0xF7;
					break;
				case 4:
					// specific to Japan: sync via TR
					result &= 0xDF;
					break;
				case 5:
					// specific to Japan: buttons
					if (c.IsPressed("P1 B1")) result &= 0xFE;
					if (c.IsPressed("P1 B2")) result &= 0xFD;
					break;
			}

			if (is_JPN)
			{
				// Buttons like normal in Export
				if (c.IsPressed("P1 B1")) result &= 0xEF;
				if (c.IsPressed("P1 B2")) result &= 0xDF;
			}
			else
			{
				// In Japan, it contains selectHigh
				if (!pin) result &= 0xEF;
			}

			return result;
		}

		public byte Read_p1_c2(IController c)
		{
			byte result = 0xFF;

			int p2X;
			if (c.IsPressed("P2 Left"))
				p2X = SportsPadMin;
			else if (c.IsPressed("P2 Right"))
				p2X = SportsPadMax;
			else
				p2X = (int)c.AxisValue("P2 X");

			int p2Y;
			if (c.IsPressed("P2 Up"))
				p2Y = SportsPadMin;
			else if (c.IsPressed("P2 Down"))
				p2Y = SportsPadMax;
			else
				p2Y = (int)c.AxisValue("P2 Y");

			if (is_JPN)
			{
				p2X += 128;
				p2Y += 128;
			}
			else
			{
				p2X *= -1;
				p2Y *= -1;
			}

			if (pin && (ControllerCounter % 2 == 0))
			{
				++ControllerCounter;
			}
			else if (!pin && (ControllerCounter % 2 == 1))
			{
				if (++ControllerCounter == (is_JPN ? 6 : 4))
					ControllerCounter = 0;
			}

			switch (ControllerCounter)
			{
				case 0:
					if ((p2X & 0x10) == 0) result &= 0xBF;
					if ((p2X & 0x20) == 0) result &= 0x7F;
					break;
				case 1:
					if ((p2X & 0x01) == 0) result &= 0xBF;
					if ((p2X & 0x02) == 0) result &= 0x7F;
					break;
				case 2:
					if ((p2Y & 0x10) == 0) result &= 0xBF;
					if ((p2Y & 0x20) == 0) result &= 0x7F;
					break;
				case 3:
					if ((p2Y & 0x01) == 0) result &= 0xBF;
					if ((p2Y & 0x02) == 0) result &= 0x7F;
					break;
				case 5:
					// specific to Japan: buttons
					if (c.IsPressed("P2 B1")) result &= 0xBF;
					if (c.IsPressed("P2 B2")) result &= 0x7F;
					break;
			}

			return result;
		}

		public byte Read_p2_c1(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c2(IController c)
		{
			byte result = 0xFF;

			int p2X;
			if (c.IsPressed("P2 Left"))
				p2X = SportsPadMin;
			else if (c.IsPressed("P2 Right"))
				p2X = SportsPadMax;
			else
				p2X = (int)c.AxisValue("P2 X");

			int p2Y;
			if (c.IsPressed("P2 Down"))
				p2Y = SportsPadMin;
			else if (c.IsPressed("P2 Up"))
				p2Y = SportsPadMax;
			else
				p2Y = (int)c.AxisValue("P2 Y");

			if (is_JPN)
			{
				p2X += 128;
				p2Y += 128;
			}
			else
			{
				p2X *= -1;
				p2Y *= -1;
			}

			if (pin && (ControllerCounter % 2 == 0))
			{
				++ControllerCounter;
			}
			else if (!pin && (ControllerCounter % 2 == 1))
			{
				if (++ControllerCounter == (is_JPN ? 6 : 4))
					ControllerCounter = 0;
			}

			switch (ControllerCounter)
			{
				case 0:
					if ((p2X & 0x40) == 0) result &= 0xFE;
					if ((p2X & 0x80) == 0) result &= 0xFD;
					break;
				case 1:
					if ((p2X & 0x04) == 0) result &= 0xFE;
					if ((p2X & 0x08) == 0) result &= 0xFD;
					break;
				case 2:
					if ((p2Y & 0x40) == 0) result &= 0xFE;
					if ((p2Y & 0x80) == 0) result &= 0xFD;
					break;
				case 3:
					if ((p2Y & 0x04) == 0) result &= 0xFE;
					if ((p2Y & 0x08) == 0) result &= 0xFD;
					break;
			}
			if (is_JPN)
			{
				// Buttons like normal in Export
				if (c.IsPressed("P2 B1")) result &= 0xFB;
				if (c.IsPressed("P2 B2")) result &= 0xF7;
			}
			else
			{
				if (!pin) result &= 0xF7;
			}

			return result;
		}

		public bool PinStateGet(IController c) { return pin; }

		public void PinStateSet(IController c, bool val) { pin = val; }

		public void CounterSet(IController c, int val) { ControllerCounter = val; }

		public void RegionSet(IController c, bool val) { is_JPN = val; }

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "B1", "B2"
		};

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(pin), ref pin);
			ser.Sync(nameof(ControllerCounter), ref ControllerCounter);
			ser.Sync(nameof(is_JPN), ref is_JPN);
		}
	}

	[DisplayName("SMS Light Phaser Controller")]
	public class SMSLightPhaserController : IPort
	{
		public SMSLightPhaserController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("SMS Light Phaser Controller")
			{
				BoolButtons = BaseDefinition.Select(b => $"P{PortNum} {b}").ToList()
			}.AddXYPair($"P{PortNum} {{0}}", AxisPairOrientation.RightAndUp, 0.RangeTo(127), 64, 0.RangeTo(192), 96); //TODO verify direction against hardware
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read_p1_c1(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0])) result &= 0xEF;

			return result;
		}

		public byte Read_p1_c2(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c1(IController c)
		{
			byte result = 0xFF;

			return result;
		}

		public byte Read_p2_c2(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0])) result &= 0xFB;

			return result;
		}

		public bool PinStateGet(IController c) { return false; }

		public void PinStateSet(IController c, bool val) { }

		public void CounterSet(IController c, int val) { }

		public void RegionSet(IController c, bool val) { }

		private static readonly string[] BaseDefinition =
		{
			"Trigger"
		};

		public void SyncState(Serializer ser)
		{
			// nothing
		}
	}
}