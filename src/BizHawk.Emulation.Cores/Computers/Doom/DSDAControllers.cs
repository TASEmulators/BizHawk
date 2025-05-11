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
		int ReadButtons(IController c);
		int ReadAxis(IController c, string axis);
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
				.AddAxis($"P{PortNum} Turning Speed", (-128).RangeTo(127), 0);

			// editing a short in tastudio would be a nightmare, so we split it:
			// high byte represents shorttics mode and whole angle values
			// low byte is fractional part only available with longtics
			if (longtics)
			{
				Definition.AddAxis($"P{PortNum} Turning Speed Frac.", (-255).RangeTo(255), 0);
			}

			Definition
				.AddAxis($"P{PortNum} Weapon Select", (0).RangeTo(7), 0)
				.AddAxis($"P{PortNum} Mouse Running", (-128).RangeTo(127), 0)
				// current max raw mouse delta is 180
				.AddAxis($"P{PortNum} Mouse Turning", (longtics ? -180 : -128).RangeTo(longtics ? 180 : 127), 0)
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

			if (c.IsPressed($"P{PortNum} Fire")) result |= (1 << 0);
			if (c.IsPressed($"P{PortNum} Use"))  result |= (1 << 1);

			return result;
		}

		public int ReadAxis(IController c, string axis)
		{
			return c.AxisValue(axis);
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

		public int ReadAxis(IController c, string axis)
		{
			return c.AxisValue(axis);
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

		public int ReadAxis(IController c, string axis)
		{
			return c.AxisValue(axis);
		}
	}
}
