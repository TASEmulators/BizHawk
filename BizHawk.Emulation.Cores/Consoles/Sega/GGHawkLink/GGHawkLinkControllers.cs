using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	/// <summary>
	/// Represents a GG add on
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[DisplayName("Game Gear Controller")]
	public class StandardControls : IPort
	{
		public StandardControls(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "GG Controller",
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

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "B1", "B2", "Start"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}
}