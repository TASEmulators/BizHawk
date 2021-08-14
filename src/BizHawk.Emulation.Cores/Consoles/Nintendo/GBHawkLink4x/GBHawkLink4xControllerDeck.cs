using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	[PeripheralOptionConsumer(typeof(PeripheralOption), typeof(IPort), PeripheralOption.Standard)]
	public sealed partial class GBHawkLink4xControllerDeck
	{
		public GBHawkLink4xControllerDeck(PeripheralOption port1Option, PeripheralOption port2Option, PeripheralOption port3Option, PeripheralOption port4Option)
		{
			Port1 = CtorFor(port1Option)(1);
			Port2 = CtorFor(port2Option)(2);
			Port3 = CtorFor(port3Option)(3);
			Port4 = CtorFor(port4Option)(4);

			Definition = new ControllerDefinition(Port1.Definition.Name)
			{
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(Port3.Definition.BoolButtons)
					.Concat(Port4.Definition.BoolButtons)
					.Concat(new[] { "Toggle Cable UD" } )
					.Concat(new[] { "Toggle Cable LR" } )
					.Concat(new[] { "Toggle Cable X" } )
					.Concat(new[] { "Toggle Cable 4x" })
					.ToList()
			}.MakeImmutable();
		}

		public byte ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public byte ReadPort2(IController c)
		{
			return Port2.Read(c);
		}

		public byte ReadPort3(IController c)
		{
			return Port3.Read(c);
		}

		public byte ReadPort4(IController c)
		{
			return Port4.Read(c);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.EndSection();

			ser.BeginSection(nameof(Port2));
			Port2.SyncState(ser);
			ser.EndSection();

			ser.BeginSection(nameof(Port3));
			Port3.SyncState(ser);
			ser.EndSection();

			ser.BeginSection(nameof(Port4));
			Port4.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;
		private readonly IPort Port2;
		private readonly IPort Port3;
		private readonly IPort Port4;
	}
}
