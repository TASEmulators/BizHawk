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
		public DoomController(int portNum, bool longtics)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("Doom Input Format")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Run Speed", (-50).RangeTo(50), 0)
				.AddAxis($"P{PortNum} Strafing Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Turning Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Weapon Select", (0).RangeTo(7), 0)
				.AddAxis($"P{PortNum} Mouse Running", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Mouse Turning", (longtics ? -32768 : -128).RangeTo(longtics ? 32767 : 127), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Fire",
			"Use",
			"Forward",
			"Backward",
			"Turn Left",
			"Turn Right",
			"Strafe Left",
			"Strafe Right",
			"Run",
			"Automap",
			"Weapon Select 1",
			"Weapon Select 2",
			"Weapon Select 3",
			"Weapon Select 4",
			"Weapon Select 5",
			"Weapon Select 6",
			"Weapon Select 7",
		];

		public byte Read(IController c)
		{
			byte result = 0;

			if (c.IsPressed($"P{PortNum} Fire")) { result |= 0b0001; }
			if (c.IsPressed($"P{PortNum} Use")) { result |= 0b0010; }
			if (c.IsPressed($"P{PortNum} Automap")) { result |= 0b0100; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			int x = c.AxisValue(Definition.Axes[pot]);

			// Handling weapon select keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Weapon Select")
			{
				if (c.IsPressed($"P{PortNum} Weapon Select 1")) x = 1;
				if (c.IsPressed($"P{PortNum} Weapon Select 2")) x = 2;
				if (c.IsPressed($"P{PortNum} Weapon Select 3")) x = 3;
				if (c.IsPressed($"P{PortNum} Weapon Select 4")) x = 4;
				if (c.IsPressed($"P{PortNum} Weapon Select 5")) x = 5;
				if (c.IsPressed($"P{PortNum} Weapon Select 6")) x = 6;
				if (c.IsPressed($"P{PortNum} Weapon Select 7")) x = 7;
			}

			return x;
		}
	}

	public class HereticController : IPort
	{
		public HereticController(int portNum, bool longtics)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("Heretic Input Format")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Run Speed", (-50).RangeTo(50), 0)
				.AddAxis($"P{PortNum} Strafing Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Turning Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Weapon Select", (0).RangeTo(7), 0)
				.AddAxis($"P{PortNum} Mouse Running", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Mouse Turning", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Fly / Look", (-7).RangeTo(7), 0)
				.AddAxis($"P{PortNum} Use Artifact", (0).RangeTo(10), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Fire",
			"Use",
			"Forward",
			"Backward",
			"Turn Left",
			"Turn Right",
			"Strafe Left",
			"Strafe Right",
			"Run",
			"Weapon Select 1",
			"Weapon Select 2",
			"Weapon Select 3",
			"Weapon Select 4",
			"Weapon Select 5",
			"Weapon Select 6",
			"Weapon Select 7",
		];

		public byte Read(IController c)
		{
			byte result = 0;

			if (c.IsPressed($"P{PortNum} Fire")) { result |= 0b0001; }
			if (c.IsPressed($"P{PortNum} Use")) { result |= 0b0010; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			int x = c.AxisValue(Definition.Axes[pot]);

			// Handling running keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Run Speed")
			{
				if (c.IsPressed($"P{PortNum} Forward"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? 50 : 25;
				}

				if (c.IsPressed($"P{PortNum} Backward"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? -50 : -25;
				}
			}

			// Handling strafing keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Strafing Speed")
			{
				if (c.IsPressed($"P{PortNum} Strafe Right"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? 40 : 24;
				}

				if (c.IsPressed($"P{PortNum} Strafe Left"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? -40 : -24;
				}
			}

			// Handling turning keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Turning Speed")
			{
				if (c.IsPressed($"P{PortNum} Turn Left"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? 5 : 2;
				}

				if (c.IsPressed($"P{PortNum} Turn Right"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? -5 : -2;
				}
			}

			// Handling weapon select keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Weapon Select")
			{
				if (c.IsPressed($"P{PortNum} Weapon Select 1")) x = 1;
				if (c.IsPressed($"P{PortNum} Weapon Select 2")) x = 2;
				if (c.IsPressed($"P{PortNum} Weapon Select 3")) x = 3;
				if (c.IsPressed($"P{PortNum} Weapon Select 4")) x = 4;
				if (c.IsPressed($"P{PortNum} Weapon Select 5")) x = 5;
				if (c.IsPressed($"P{PortNum} Weapon Select 6")) x = 6;
				if (c.IsPressed($"P{PortNum} Weapon Select 7")) x = 7;
			}

			return x;
		}
	}

	public class HexenController : IPort
	{
		public HexenController(int portNum, bool longtics)
		{
			PortNum = portNum;
			Definition = new ControllerDefinition("Hexen Input Format")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Run Speed", (-50).RangeTo(50), 0)
				.AddAxis($"P{PortNum} Strafing Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Turning Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Weapon Select", (1).RangeTo(4), 0)
				.AddAxis($"P{PortNum} Mouse Running", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Mouse Turning", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Fly / Look", (-7).RangeTo(7), 0)
				.AddAxis($"P{PortNum} Use Artifact", (0).RangeTo(33), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }

		public ControllerDefinition Definition { get; }

		private static readonly string[] BaseDefinition =
		[
			"Fire",
			"Use",
			"Alt Weapon",
			"Jump",
			"End Player",
			"Forward",
			"Backward",
			"Turn Left",
			"Turn Right",
			"Strafe Left",
			"Strafe Right",
			"Run",
			"Weapon Select 1",
			"Weapon Select 2",
			"Weapon Select 3",
			"Weapon Select 4"
		];

		public byte Read(IController c)
		{
			byte result = 0;

			if (c.IsPressed($"P{PortNum} Fire")) { result |= 0b00001; }
			if (c.IsPressed($"P{PortNum} Use")) { result |= 0b00010; }
			if (c.IsPressed($"P{PortNum} Jump")) { result |= 0b01000; }
			if (c.IsPressed($"P{PortNum} End Player")) { result |= 0b10000; }

			return result;
		}

		public int Read_Pot(IController c, int pot)
		{
			int x = c.AxisValue(Definition.Axes[pot]);

			// Handling running keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Run Speed")
			{
				if (c.IsPressed($"P{PortNum} Forward"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? 50 : 25;
				}

				if (c.IsPressed($"P{PortNum} Backward"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? -50 : -25;
				}
			}

			// Handling strafing keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Strafing Speed")
			{
				if (c.IsPressed($"P{PortNum} Strafe Right"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? 40 : 24;
				}

				if (c.IsPressed($"P{PortNum} Strafe Left"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? -40 : -24;
				}
			}

			// Handling turning keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Turning Speed")
			{
				if (c.IsPressed($"P{PortNum} Turn Left"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? 5 : 2;
				}

				if (c.IsPressed($"P{PortNum} Turn Right"))
				{
					x = c.IsPressed($"P{PortNum} Run") ? -5 : -2;
				}
			}

			// Handling weapon select keys overriding axes values
			if (Definition.Axes[pot] == $"P{PortNum} Weapon Select")
			{
				if (c.IsPressed($"P{PortNum} Weapon Select 1")) x = 1;
				if (c.IsPressed($"P{PortNum} Weapon Select 2")) x = 2;
				if (c.IsPressed($"P{PortNum} Weapon Select 3")) x = 3;
				if (c.IsPressed($"P{PortNum} Weapon Select 4")) x = 4;
			}

			return x;
		}
	}
}
