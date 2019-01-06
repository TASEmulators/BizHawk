using System;

using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	/// <summary>
	/// Represents a Standard Nintendo Controller
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[DisplayName("NES Controller")]
	public class StandardControls : IPort
	{
		public StandardControls(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "NES Controller",
				BoolButtons = BaseDefinition
				.Select(b => "P" + PortNum + " " + b)
				.ToList()
			};
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		public byte Read(IController c)
		{
			byte result = 0;

			if (c.IsPressed(Definition.BoolButtons[0]))
			{
				result |= 8;
			}
			if (c.IsPressed(Definition.BoolButtons[1]))
			{
				result |= 4;
			}
			if (c.IsPressed(Definition.BoolButtons[2]))
			{
				result |= 2;
			}
			if (c.IsPressed(Definition.BoolButtons[3]))
			{
				result |= 1;
			}
			if (c.IsPressed(Definition.BoolButtons[4]))
			{
				result |= 16;
			}
			if (c.IsPressed(Definition.BoolButtons[5]))
			{
				result |= 32;
			}
			if (c.IsPressed(Definition.BoolButtons[6]))
			{
				result |= 64;
			}
			if (c.IsPressed(Definition.BoolButtons[7]))
			{
				result |= 128;
			}

			return result;
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Start", "Select", "B", "A"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}
}