using System;
using System.Collections.Generic;
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
			Port1 = (IPort)Activator.CreateInstance(Implementors[(int)controller1], 1);
			Port2 = (IPort)Activator.CreateInstance(Implementors[(int)controller2], 2);
			Port3 = (IPort)Activator.CreateInstance(Implementors[(int)controller3], 3);
			Port4 = (IPort)Activator.CreateInstance(Implementors[(int)controller4], 4);
			Port5 = (IPort)Activator.CreateInstance(Implementors[(int)controller5], 5);

			Definition = new ControllerDefinition
			{
				Name = "PC Engine Controller",
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(Port3.Definition.BoolButtons)
					.Concat(Port4.Definition.BoolButtons)
					.Concat(Port5.Definition.BoolButtons)
					.ToList()
			};

			Definition.FloatControls.AddRange(Port1.Definition.FloatControls);
			Definition.FloatControls.AddRange(Port2.Definition.FloatControls);
			Definition.FloatControls.AddRange(Port3.Definition.FloatControls);
			Definition.FloatControls.AddRange(Port4.Definition.FloatControls);
			Definition.FloatControls.AddRange(Port5.Definition.FloatControls);

			Definition.FloatRanges.AddRange(Port1.Definition.FloatRanges);
			Definition.FloatRanges.AddRange(Port2.Definition.FloatRanges);
			Definition.FloatRanges.AddRange(Port3.Definition.FloatRanges);
			Definition.FloatRanges.AddRange(Port4.Definition.FloatRanges);
			Definition.FloatRanges.AddRange(Port5.Definition.FloatRanges);
		}

		private readonly IPort Port1;
		private readonly IPort Port2;
		private readonly IPort Port3;
		private readonly IPort Port4;
		private readonly IPort Port5;

		public byte Read(int portNum, IController c, bool sel)
		{
			switch (portNum)
			{
				default:
					throw new ArgumentException($"Invalid {nameof(portNum)}: {portNum}");
				case 1:
					return Port1.Read(c, sel);
				case 2:
					return Port2.Read(c, sel);
				case 3:
					return Port3.Read(c, sel);
				case 4:
					return Port4.Read(c, sel);
				case 5:
					return Port5.Read(c, sel);
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
			Definition = new ControllerDefinition
			{
				BoolButtons = new List<string>()
			};
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
			Definition = new ControllerDefinition
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

			if (sel == false)
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
