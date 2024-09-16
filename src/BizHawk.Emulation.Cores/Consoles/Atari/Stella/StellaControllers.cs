using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public enum Atari2600ControllerTypes
	{
		Unplugged,
		Joystick,
		//Paddle,
		//BoostGrip,
		Driving,
		//Keyboard
	}

	/// <summary>
	/// Represents a controller plugged into a controller port on the 2600
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		int Read_Pot(IController c, int pot);

		ControllerDefinition Definition { get; }

		int PortNum { get; }
	}

	public class UnpluggedController : IPort
	{
		public UnpluggedController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(Atari 2600 Basic Controller fragment)");
		}

		public byte Read(IController c)
		{
			return 0xFF;
		}

		public int Read_Pot(IController c, int pot)
		{
			return -1; // indicates not applicable
		}

		public ControllerDefinition Definition { get; }

		public int PortNum { get; }
	}

	public class StandardController : IPort
	{
		public StandardController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(Atari 2600 Basic Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			};
		}

		public ControllerDefinition Definition { get; }

		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} Up")) { result &= 0xEF; }
			if (c.IsPressed($"P{PortNum} Down")) { result &= 0xDF; }
			if (c.IsPressed($"P{PortNum} Left")) { result &= 0xBF; }
			if (c.IsPressed($"P{PortNum} Right")) { result &= 0x7F; }
			if (c.IsPressed($"P{PortNum} Button")) { result &= 0xF7; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			return -1; // indicates not applicable
		}

		private static readonly string[] BaseDefinition =
		[
			"Up", "Down", "Left", "Right", "Button"
		];
	}

	public class PaddleController : IPort
	{
		public PaddleController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("(Atari 2600 Basic Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Paddle X 1", (-127).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Paddle X 2", (-127).RangeTo(127), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Button 1",
			"Button 2"
		];

		public byte Read(IController c)
		{
			byte result = 0xF0;

			if (c.IsPressed($"P{PortNum} Button 1")) { result &= 0x70; }
			if (c.IsPressed($"P{PortNum} Button 2")) { result &= 0xB0; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			int x = c.AxisValue(Definition.Axes[pot]);
			
			x = -x;
			x += 127;

			x = x * 64 + 10;

			return x;
		}
	}

	public class BoostGripController : IPort
	{
		public BoostGripController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(Atari 2600 Basic Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Up", "Down", "Left", "Right", "Button",
			"Button 1",
			"Button 2"
		];

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} Up")) { result &= 0xEF; }
			if (c.IsPressed($"P{PortNum} Down")) { result &= 0xDF; }
			if (c.IsPressed($"P{PortNum} Left")) { result &= 0xBF; }
			if (c.IsPressed($"P{PortNum} Right")) { result &= 0x7F; }
			if (c.IsPressed($"P{PortNum} Button")) { result &= 0xF7; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			bool is_pressed = false;

			if (pot == 0)
			{
				is_pressed = c.IsPressed($"P{PortNum} Button 1");
			}
			else
			{
				is_pressed = c.IsPressed($"P{PortNum} Button 2");
			}
			
			if (is_pressed)
			{
				return 10;
			}

			return 65535;
		}
	}

	public class DrivingController : IPort
	{
		public DrivingController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("(Atari 2600 Basic Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Wheel X 1", (-127).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Wheel X 2", (-127).RangeTo(127), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Button"
		];

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} Button")) { result &= 0xF7; }

			float x = c.AxisValue(Definition.Axes[0]);
			float y = c.AxisValue(Definition.Axes[1]);

			float angle = CalcDirection(x, y);

			byte temp2 = 0;

			int temp1 = (int)Math.Floor(angle / 45);
			temp1 %= 4;

			if (temp1 == 0)
			{
				temp2 = 0xEF;
			}

			if (temp1 == 1)
			{
				temp2 = 0xCF;
			}
			if (temp1 == 2)
			{
				temp2 = 0xDF;
			}

			if (temp1 == 3)
			{
				temp2 = 0xFF;
			}


			result &= temp2;


			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			return -1;  // indicates not applicable
		}

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
	}

	public class KeyboardController : IPort
	{
		public KeyboardController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(Atari 2600 Basic Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			};
		}

		public ControllerDefinition Definition { get; }

		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed($"P{PortNum} 0")) { result = 0x00; }
			if (c.IsPressed($"P{PortNum} 1")) { result = 0x01; }
			if (c.IsPressed($"P{PortNum} 2")) { result = 0x02; }
			if (c.IsPressed($"P{PortNum} 3")) { result = 0x03; }
			if (c.IsPressed($"P{PortNum} 4")) { result = 0x04; }
			if (c.IsPressed($"P{PortNum} 5")) { result = 0x05; }
			if (c.IsPressed($"P{PortNum} 6")) { result = 0x06; }
			if (c.IsPressed($"P{PortNum} 7")) { result = 0x07; }
			if (c.IsPressed($"P{PortNum} 8")) { result = 0x08; }
			if (c.IsPressed($"P{PortNum} 9")) { result = 0x09; }
			if (c.IsPressed($"P{PortNum} *")) { result = 0x0A; }
			if (c.IsPressed($"P{PortNum} #")) { result = 0x0B; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			return -2; // indicates keyboard
		}

		private static readonly string[] BaseDefinition =
		[
			"1", "2", "3",
			"4", "5", "6",
			"7", "8", "9",
			"*", "0", "#"
		];
	}
}
