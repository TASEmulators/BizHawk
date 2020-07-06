using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.G7400Hawk
{
	/// <summary>
	/// Represents a G7400 add on
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[DisplayName("G7400 Controller")]
	public class StandardControls : IPort
	{
		public StandardControls(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition
			{
				Name = "O2 Joystick",
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
				result -= 1;
			}
			if (c.IsPressed(Definition.BoolButtons[1]))
			{
				result -= 4;
			}
			if (c.IsPressed(Definition.BoolButtons[2]))
			{
				result -= 8;
			}
			if (c.IsPressed(Definition.BoolButtons[3]))
			{
				result -= 2;
			}
			if (c.IsPressed(Definition.BoolButtons[4]))
			{
				result -= 16;
			}

			return result;
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "F"
		};

		public void SyncState(Serializer ser)
		{
			//nothing
		}
	}
}