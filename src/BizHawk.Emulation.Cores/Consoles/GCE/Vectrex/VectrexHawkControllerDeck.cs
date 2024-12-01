using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class VectrexHawkControllerDeck
	{
		public VectrexHawkControllerDeck(string controller1Name, string controller2Name)
		{
			Port1 = ControllerCtors.TryGetValue(controller1Name, out var ctor1)
				? ctor1(1)
				: throw new InvalidOperationException($"Invalid controller type: {controller1Name}");
			Port2 = ControllerCtors.TryGetValue(controller2Name, out var ctor2)
				? ctor2(2)
				: throw new InvalidOperationException($"Invalid controller type: {controller2Name}");

			Definition = new(Port1.Definition.Name)
			{
				BoolButtons = Port1.Definition.BoolButtons
							.Concat(Port2.Definition.BoolButtons)
							.Concat(new[]
							{
								"Power",
								"Reset"
							})
							.ToList()
			};

			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in Port2.Definition.Axes) Definition.Axes.Add(kvp);

			Definition.MakeImmutable();
		}

		public byte ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public byte ReadPort2(IController c)
		{
			return Port2.Read(c);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Port1");
			Port1.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("Port2");
			Port2.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;
		private readonly IPort Port2;

		private static IReadOnlyDictionary<string, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<string, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<string, Func<int, IPort>>
			{
				[typeof(StandardControls).DisplayName()] = portNum => new StandardControls(portNum),
				[typeof(AnalogControls).DisplayName()] = portNum => new AnalogControls(portNum)
			};

		public static string DefaultControllerName => typeof(StandardControls).DisplayName();
	}
}
