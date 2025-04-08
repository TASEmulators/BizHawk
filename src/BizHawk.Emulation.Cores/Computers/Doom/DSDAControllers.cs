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

	// must match the order of axes added
	public enum AxisType : int
	{
		RunSpeed,
		StrafingSpeed,
		TurningSpeed,
		WeaponSelect,
		MouseRunning,
		MouseTurning,
		FlyLook,
		UseArtifact
	}

	public interface IPort
	{
		int ReadButtons(IController c);
		int ReadAxis(IController c, int axis);
		ControllerDefinition Definition { get; }
		int PortNum { get; }
	}

	public class DoomController : IPort
	{
		public DoomController(int portNum, bool longtics)
		{
			PortNum = portNum;
			_longtics = longtics;
			Definition = new ControllerDefinition("Doom Input Format")
			{
				BoolButtons = _baseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			}.AddAxis($"P{PortNum} Run Speed", (-50).RangeTo(50), 0)
				.AddAxis($"P{PortNum} Strafing Speed", (-50).RangeTo(50), 0)
				.AddAxis($"P{PortNum} Turning Speed", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Weapon Select", (0).RangeTo(7), 0)
				.AddAxis($"P{PortNum} Mouse Running", (-128).RangeTo(127), 0)
				.AddAxis($"P{PortNum} Mouse Turning", (longtics ? -32768 : -128).RangeTo(longtics ? 32767 : 127), 0)
				.MakeImmutable();
		}

		public int PortNum { get; }
		public ControllerDefinition Definition { get; }
		private bool _longtics;

		private static readonly string[] _baseDefinition =
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
			"Strafe",
			"Change Gamma",
			"Weapon Select 1",
			"Weapon Select 2",
			"Weapon Select 3",
			"Weapon Select 4",
			"Weapon Select 5",
			"Weapon Select 6",
			"Weapon Select 7",
			"Automap Toggle",
			"Automap +",
			"Automap -",
			"Automap Full/Zoom",
			"Automap Follow",
			"Automap Up",
			"Automap Down",
			"Automap Right",
			"Automap Left",
			"Automap Grid",
			"Automap Mark",
			"Automap Clear Marks",
		];

		public int ReadButtons(IController c)
		{
			int result = 0;

			if (c.IsPressed($"P{PortNum} Fire"))                result |= (1 << 0);
			if (c.IsPressed($"P{PortNum} Use"))                 result |= (1 << 1);
			if (c.IsPressed($"P{PortNum} Change Gamma"))        result |= (1 << 2);
			if (c.IsPressed($"P{PortNum} Automap Toggle"))      result |= (1 << 3);
			if (c.IsPressed($"P{PortNum} Automap +"))           result |= (1 << 4);
			if (c.IsPressed($"P{PortNum} Automap -"))           result |= (1 << 5);
			if (c.IsPressed($"P{PortNum} Automap Full/Zoom"))   result |= (1 << 6);
			if (c.IsPressed($"P{PortNum} Automap Follow"))      result |= (1 << 7);
			if (c.IsPressed($"P{PortNum} Automap Up"))          result |= (1 << 8);
			if (c.IsPressed($"P{PortNum} Automap Down"))        result |= (1 << 9);
			if (c.IsPressed($"P{PortNum} Automap Right"))       result |= (1 << 10);
			if (c.IsPressed($"P{PortNum} Automap Left"))        result |= (1 << 11);
			if (c.IsPressed($"P{PortNum} Automap Grid"))        result |= (1 << 12);
			if (c.IsPressed($"P{PortNum} Automap Mark"))        result |= (1 << 13);
			if (c.IsPressed($"P{PortNum} Automap Clear Marks")) result |= (1 << 14);

			return result;
		}

		public int ReadAxis(IController c, int axis)
		{
			int x = c.AxisValue(Definition.Axes[axis]);

			// Handling weapon select keys overriding axes values
			if (Definition.Axes[axis] == $"P{PortNum} Weapon Select")
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
				BoolButtons = _baseDefinition
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

		private static readonly string[] _baseDefinition =
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

		public int ReadButtons(IController c)
		{
			int result = 0;

			if (c.IsPressed($"P{PortNum} Fire")) { result |= 0b0001; }
			if (c.IsPressed($"P{PortNum} Use")) { result |= 0b0010; }

			return result;
		}

		public int ReadAxis(IController c, int axis)
		{
			int x = c.AxisValue(Definition.Axes[axis]);

			// Handling running keys overriding axes values
			if (Definition.Axes[axis] == $"P{PortNum} Run Speed")
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
			if (Definition.Axes[axis] == $"P{PortNum} Strafing Speed")
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
			if (Definition.Axes[axis] == $"P{PortNum} Turning Speed")
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
			if (Definition.Axes[axis] == $"P{PortNum} Weapon Select")
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
				BoolButtons = _baseDefinition
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

		private static readonly string[] _baseDefinition =
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

		public int ReadButtons(IController c)
		{
			int result = 0;

			if (c.IsPressed($"P{PortNum} Fire")) { result |= 0b00001; }
			if (c.IsPressed($"P{PortNum} Use")) { result |= 0b00010; }
			if (c.IsPressed($"P{PortNum} Jump")) { result |= 0b01000; }
			if (c.IsPressed($"P{PortNum} End Player")) { result |= 0b10000; }

			return result;
		}

		public int ReadAxis(IController c, int axis)
		{
			int x = c.AxisValue(Definition.Axes[axis]);

			// Handling running keys overriding axes values
			if (Definition.Axes[axis] == $"P{PortNum} Run Speed")
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
			if (Definition.Axes[axis] == $"P{PortNum} Strafing Speed")
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
			if (Definition.Axes[axis] == $"P{PortNum} Turning Speed")
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
			if (Definition.Axes[axis] == $"P{PortNum} Weapon Select")
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
