using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	/// <summary>
	/// Represents a controller plugged into a controller port on the intellivision
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		byte ReadFire(IController c);

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

		public byte ReadFire(IController c)
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

	[DisplayName("Joystick Controller")]
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

		public ControllerDefinition Definition { get; }


		public void SyncState(Serializer ser)
		{
			// Nothing todo, I think
		}

		private static readonly string[] BaseDefinition =
		{
			"U", "D", "L", "R", "Fire"
		};

		private static byte[] HandControllerButtons =
		{
			0x0, // UP
			0x0, // Down
			0x0, // Left
			0x0, // Right
		};
	}
}
