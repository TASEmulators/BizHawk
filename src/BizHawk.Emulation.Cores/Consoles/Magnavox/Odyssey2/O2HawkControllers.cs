using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	[PeripheralOptionEnum]
	public enum PeripheralOption : int
	{
		[Description("O2 Controller")]
		Standard = 1,
	}

	/// <summary>
	/// Represents a O2 add on
	/// </summary>
	public interface IPort
	{
		byte Read(IController c);

		ControllerDefinition Definition { get; }

		void SyncState(Serializer ser);

		int PortNum { get; }
	}

	[PeripheralOptionImpl(typeof(PeripheralOption), PeripheralOption.Standard)]
	public class StandardControls : IPort
	{
		public StandardControls(int portNum)
		{
			PortNum = portNum;
			Definition = new("O2 Joystick")
			{
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