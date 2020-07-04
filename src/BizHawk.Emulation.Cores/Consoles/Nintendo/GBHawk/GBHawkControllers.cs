using System;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	/// <summary>
	/// Represents a GB add on
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ushort ReadAccX(IController c);

		ushort ReadAccY(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[DisplayName("Gameboy Controller")]
	public class StandardControls : IPort
	{
		public StandardControls(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Gameboy Controller H",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0]))
			{
				result -= 4;
			}
			if (c.IsPressed(Definition.BoolButtons[1]))
			{
				result -= 8;
			}
			if (c.IsPressed(Definition.BoolButtons[2]))
			{
				result -= 2;
			}
			if (c.IsPressed(Definition.BoolButtons[3]))
			{
				result -= 1;
			}
			if (c.IsPressed(Definition.BoolButtons[4]))
			{
				result -= 128;
			}
			if (c.IsPressed(Definition.BoolButtons[5]))
			{
				result -= 64;
			}
			if (c.IsPressed(Definition.BoolButtons[6]))
			{
				result -= 32;
			}
			if (c.IsPressed(Definition.BoolButtons[7]))
			{
				result -= 16;
			}

			return result;
		}

		public ushort ReadAccX(IController c)
		{
			return 0;
		}

		public ushort ReadAccY(IController c)
		{
			return 0;
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}

	[DisplayName("Gameboy Controller + Tilt")]
	public class StandardTilt : IPort
	{
		public StandardTilt(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Gameboy Controller + Tilt",
				BoolButtons = BaseDefinition.Select(b => $"P{PortNum} {b}").ToList()
			}.AddXYPair($"P{PortNum} Tilt {{0}}", AxisPairOrientation.RightAndUp, (-90).RangeTo(90), 0); //TODO verify direction against hardware
		}

		public int PortNum { get; }

		public float theta, phi, theta_prev, phi_prev; 

		public ControllerDefinition Definition { get; }

		public byte Read(IController c)
		{
			byte result = 0xFF;

			if (c.IsPressed(Definition.BoolButtons[0]))
			{
				result -= 4;
			}
			if (c.IsPressed(Definition.BoolButtons[1]))
			{
				result -= 8;
			}
			if (c.IsPressed(Definition.BoolButtons[2]))
			{
				result -= 2;
			}
			if (c.IsPressed(Definition.BoolButtons[3]))
			{
				result -= 1;
			}
			if (c.IsPressed(Definition.BoolButtons[4]))
			{
				result -= 128;
			}
			if (c.IsPressed(Definition.BoolButtons[5]))
			{
				result -= 64;
			}
			if (c.IsPressed(Definition.BoolButtons[6]))
			{
				result -= 32;
			}
			if (c.IsPressed(Definition.BoolButtons[7]))
			{
				result -= 16;
			}

			return result;
		}

		// acc x is the result of rotating around body y AFTER rotating around body x
		// therefore this control scheme gives decreasing sensitivity in X as Y rotation inscreases 
		public ushort ReadAccX(IController c)
		{
			theta_prev = theta;
			phi_prev = phi;

			theta = (float)(c.AxisValue(Definition.Axes[1]) * Math.PI / 180.0);
			phi = (float)(c.AxisValue(Definition.Axes[0]) * Math.PI / 180.0);

			float temp = (float)(Math.Cos(theta) * Math.Sin(phi));

			// here we add in rates of change parameters. 
			// a typical rate of change for a fast rotation is guessed at 0.5 rad / frame
			// since rotations about X have less of a moment arm compared to by, we take 1/5 of the effect as a baseline
			float temp2 = (float)((phi - phi_prev) / 0.5 * 25);

			return (ushort)(0x8370 - Math.Floor(temp * 220) - temp2);
		}

		// acc y is just the sine of the angle
		// we assume that ReadAccX is called first, which updates the the states 
		public ushort ReadAccY(IController c)
		{
			float temp = (float)Math.Sin(theta);

			// here we add in rates of change parameters. 
			// a typical rate of change for a fast rotation is guessed at 0.5 rad / frame
			// further it will be assumed that the resulting acceleration is roughly eqvuivalent to gravity
			float temp2 = (float)((theta - theta_prev)/0.5 * 125);

			return (ushort)(0x8370 - Math.Floor(temp * 220) + temp2);			
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power"
		};

		public void SyncState(Serializer ser)
		{
			// since we need rate of change of angle, need to savestate them
			ser.Sync(nameof(theta), ref theta);
		}
	}
}