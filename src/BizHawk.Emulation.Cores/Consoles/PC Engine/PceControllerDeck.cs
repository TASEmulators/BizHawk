using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public enum PceControllerType
	{
		Unplugged,
		GamePad
	}

	public class PceControllerDeck
	{
		private static readonly Type[] Implementors =
		{
			typeof(UnpluggedController), // Order must match PceControllerType enum values
			typeof(StandardController)
		};

		public PceControllerDeck(
			PceControllerType controller1,
			PceControllerType controller2,
			PceControllerType controller3,
			PceControllerType controller4,
			PceControllerType controller5)
		{
			_port1 = (IPort)Activator.CreateInstance(Implementors[(int)controller1], 1);
			_port2 = (IPort)Activator.CreateInstance(Implementors[(int)controller2], 2);
			_port3 = (IPort)Activator.CreateInstance(Implementors[(int)controller3], 3);
			_port4 = (IPort)Activator.CreateInstance(Implementors[(int)controller4], 4);
			_port5 = (IPort)Activator.CreateInstance(Implementors[(int)controller5], 5);

			Definition = new("PC Engine Controller")
			{
				BoolButtons = _port1.Definition.BoolButtons
					.Concat(_port2.Definition.BoolButtons)
					.Concat(_port3.Definition.BoolButtons)
					.Concat(_port4.Definition.BoolButtons)
					.Concat(_port5.Definition.BoolButtons)
					.ToList()
			};
			foreach (var kvp in _port1.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in _port2.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in _port3.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in _port4.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in _port5.Definition.Axes) Definition.Axes.Add(kvp);
			Definition.MakeImmutable();
		}

		private readonly IPort _port1;
		private readonly IPort _port2;
		private readonly IPort _port3;
		private readonly IPort _port4;
		private readonly IPort _port5;

		public byte Read(int portNum, IController c, bool sel)
		{
			switch (portNum)
			{
				default:
					throw new ArgumentOutOfRangeException(paramName: nameof(portNum), portNum, "port index out of range");
				case 1:
					return _port1.Read(c, sel);
				case 2:
					return _port2.Read(c, sel);
				case 3:
					return _port3.Read(c, sel);
				case 4:
					return _port4.Read(c, sel);
				case 5:
					return _port5.Read(c, sel);
			}
		}

		public ControllerDefinition Definition { get; }
	}

	public interface IPort
	{
		byte Read(IController c, bool sel);

		ControllerDefinition Definition { get; }

		int PortNum { get; }
	}

	public class UnpluggedController : IPort
	{
		public UnpluggedController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(PC Engine Controller fragment)");
		}

		public byte Read(IController c, bool sel)
		{
			return 0x3F;
		}

		public ControllerDefinition Definition { get; }

		public int PortNum { get; }
	}

	public class StandardController : IPort
	{
		public StandardController(int portNum)
		{
			PortNum = portNum;
			Definition = new("(PC Engine Controller fragment)")
			{
				BoolButtons = BaseDefinition
				.Select(b => $"P{PortNum} " + b)
				.ToList()
			};
		}

		public ControllerDefinition Definition { get; }

		public int PortNum { get; }

		public byte Read(IController c, bool sel)
		{
			byte result = 0x3F;

			if (!sel)
			{
				if (c.IsPressed($"P{PortNum} B1")) result &= 0xFE;
				if (c.IsPressed($"P{PortNum} B2")) result &= 0xFD;
				if (c.IsPressed($"P{PortNum} Select")) result &= 0xFB;
				if (c.IsPressed($"P{PortNum} Run")) result &= 0xF7;
			}
			else
			{
				if (c.IsPressed($"P{PortNum} Up")) { result &= 0xFE; }
				if (c.IsPressed($"P{PortNum} Right")) { result &= 0xFD; }
				if (c.IsPressed($"P{PortNum} Down")) { result &= 0xFB; }
				if (c.IsPressed($"P{PortNum} Left")) { result &= 0xF7; }
			}

			return result;
		}

		private static readonly string[] BaseDefinition =
		{
			"Up", "Down", "Left", "Right", "Select", "Run", "B2", "B1"
		};
	}
}
