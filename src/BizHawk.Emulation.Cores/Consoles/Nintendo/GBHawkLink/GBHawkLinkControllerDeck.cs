using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	[PeripheralOptionConsumer(typeof(PeripheralOption), typeof(IPort), PeripheralOption.Standard)]
	public sealed partial class GBHawkLinkControllerDeck
	{
		public GBHawkLinkControllerDeck(PeripheralOption port1Option, PeripheralOption port2Option)
		{
			Port1 = CtorFor(port1Option)(1);
			Port2 = CtorFor(port2Option)(2);

			Definition = new ControllerDefinition(Port1.Definition.Name)
			{
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(new[] { "Toggle Cable" } )
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

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.EndSection();

			ser.BeginSection(nameof(Port2));
			Port2.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;
		private readonly IPort Port2;
	}
}
