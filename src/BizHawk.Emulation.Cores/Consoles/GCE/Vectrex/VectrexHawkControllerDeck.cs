using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	[PeripheralOptionConsumer(typeof(ControllerType), typeof(IPort), ControllerType.Digital)]
	public sealed partial class VectrexHawkControllerDeck
	{
		public VectrexHawkControllerDeck(ControllerType port1Option, ControllerType port2Option)
		{
			Port1 = CtorFor(port1Option)(1);
			Port2 = CtorFor(port2Option)(2);

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
	}
}
