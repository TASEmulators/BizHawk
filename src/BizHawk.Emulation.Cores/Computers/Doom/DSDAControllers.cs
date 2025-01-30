using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public enum DoomControllerTypes
	{
		Doom,
		Heretic,
		Hexen
	}

	public interface IPort
	{
		byte Read(IController c);

		int Read_Pot(IController c, int pot);

		ControllerDefinition Definition { get; }

		int PortNum { get; }
	}

	public class DoomController : IPort
	{
		public DoomController(int portNum)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("Doom Input Format")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Run Speed", (-50).RangeTo(50), 0)
				.AddAxis($"P{PortNum} Strafing Speed", (-127).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Turning Speed", (-128).RangeTo(128), 0)
				.AddAxis($"P{PortNum} Weapon Select", (0).RangeTo(7), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Fire",
			"Action",
			"Alt Weapon"
		];

		public byte Read(IController c)
		{
			byte result = 0;

			if (c.IsPressed($"P{PortNum} Fire"))   { result |= 0b0001; }
			if (c.IsPressed($"P{PortNum} Action")) { result |= 0b0010; }
			if (c.IsPressed($"P{PortNum} Alt Weapon")) { result |= 0b0100; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			int x = c.AxisValue(Definition.Axes[pot]);

			return x;
		}
	}
}
