using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	[PeripheralOptionConsumer(typeof(PeripheralOption), typeof(IPort), PeripheralOption.Standard)]
	public sealed partial class GBHawkControllerDeck
	{
		public GBHawkControllerDeck(PeripheralOption port1Option)
		{
			Port1 = CtorFor(port1Option)(1);

			Definition = new(Port1.Definition.Name)
			{
				BoolButtons = Port1.Definition.BoolButtons
					.ToList()
			};

			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);

			Definition.MakeImmutable();
		}

		public byte ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public (ushort X, ushort Y) ReadAcc1(IController c)
			=> Port1.ReadAcc(c);

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1;
	}
}
