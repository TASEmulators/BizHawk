using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

using static BizHawk.Emulation.Common.ControllerDefinition;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	/// <summary>
	/// Represents a controller plugged into a controller port on the A7800
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		byte ReadFire(IController c);

		byte ReadFire2x(IController c);

		bool Is_2_button(IController c);

		bool Is_LightGun(IController c, out float x, out float y);

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
				Name = "Unplugged Controller",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public byte Read(IController c)
		{
			return 0;
		}

		public byte ReadFire(IController c)
		{
			return 0x80;
		}

		public byte ReadFire2x(IController c)
		{
			return 0;
		}

		public bool Is_2_button(IController c)
		{
			return false;
		}

		public bool Is_LightGun(IController c, out float x, out float y)
		{
			x = -1;
			y = -1;
			return false;
		}

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		{
			""
		};

		public void SyncState(Serializer ser)
		{
			// Do nothing
		}

		public int PortNum { get; }
	}

	[DisplayName("Joystick Controller")]
	public class StandardController : IPort
	{
		public StandardController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Atari 2600 Basic Controller",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0xF;
			for (int i = 0; i < 4; i++)
			{
				if (c.IsPressed(Definition.BoolButtons[i]))
				{
					result -= (byte)(1 << i);
				}
			}

			if (PortNum==1)
			{
				result = (byte)(result << 4);
			}

			return result;
		}

		public byte ReadFire(IController c)
		{
			byte result = 0x80;
			if (c.IsPressed(Definition.BoolButtons[4]))
			{
				result = 0x00; // zero means fire is pressed
			}
			return result;
		}

		public byte ReadFire2x(IController c)
		{
			return 0; // only applicable for 2 button mode
		}

		public bool Is_2_button(IController c)
		{
			return false;
		}

		public bool Is_LightGun(IController c, out float x, out float y)
		{
			x = -1;
			y = -1;
			return false;
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Button"
		};

		private static byte[] HandControllerButtons =
		{
			0x0, // UP
			0x0, // Down
			0x0, // Left
			0x0, // Right
		};
	}

	[DisplayName("ProLine Controller")]
	public class ProLineController : IPort
	{
		public ProLineController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Atari 7800 ProLine Joystick Controller",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0xF;
			for (int i = 0; i < 4; i++)
			{
				if (c.IsPressed(Definition.BoolButtons[i]))
				{
					result -= (byte)(1 << i);
				}
			}

			if (PortNum == 1)
			{
				result = (byte)(result << 4);
			}

			return result;
		}

		public byte ReadFire(IController c)
		{
			byte result = 0x80;
			if (c.IsPressed(Definition.BoolButtons[4]) || c.IsPressed(Definition.BoolButtons[5]))
			{
				result = 0x00; // zero means fire is pressed
			}
			return result;
		}

		public byte ReadFire2x(IController c)
		{
			byte result = 0;
			if (c.IsPressed(Definition.BoolButtons[4]))
			{
				result = 0x80;
			}
			if (c.IsPressed(Definition.BoolButtons[5]))
			{
				result |= 0x40;
			}
			return result;
		}

		public bool Is_2_button(IController c)
		{
			return true;
		}

		public bool Is_LightGun(IController c, out float x, out float y)
		{
			x = -1;
			y = -1;
			return false;
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Trigger", "Trigger 2"
		};

		private static byte[] HandControllerButtons =
		{
			0x0, // UP
			0x0, // Down
			0x0, // Left
			0x0, // Right
		};
	}


	[DisplayName("Light Gun Controller")]
	public class LightGunController : IPort
	{
		public LightGunController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "Light Gun Controller",
				BoolButtons = BaseDefinition.Select(b => $"P{PortNum} {b}").ToList()
			}.AddXYPair($"P{PortNum} {{0}}", AxisPairOrientation.RightAndUp, 1.RangeTo(320), 160, 1.RangeTo(242), 121); //TODO verify direction against hardware
		}
		
		public int PortNum { get; }

		public byte Read(IController c)
		{
			byte result = 0xE;
			if (c.IsPressed(Definition.BoolButtons[0]))
			{
				result |= 0x1;
			}

			if (PortNum == 1)
			{
				result = (byte)(result << 4);
			}

			return result;
		}

		public byte ReadFire(IController c)
		{
			return 0x80;
		}

		public byte ReadFire2x(IController c)
		{
			return 0; // only applicable for 2 button mode
		}

		public bool Is_2_button(IController c)
		{
			return false;
		}

		public bool Is_LightGun(IController c, out float x, out float y)
		{
			x = c.AxisValue(Definition.Axes[0]);
			y = c.AxisValue(Definition.Axes[1]);
			return true;
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseDefinition =
		{
			"Trigger"
		};
	}
}