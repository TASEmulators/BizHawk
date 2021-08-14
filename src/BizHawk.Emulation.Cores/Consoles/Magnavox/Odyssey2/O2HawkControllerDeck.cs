using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	[PeripheralOptionConsumer(typeof(PeripheralOption), typeof(IPort), PeripheralOption.Standard)]
	public sealed partial class O2HawkControllerDeck
	{
		public O2HawkControllerDeck(PeripheralOption port1Option, PeripheralOption port2Option, bool is_G7400)
		{
			Port1 = CtorFor(port1Option)(1);
			Port2 = CtorFor(port2Option)(2);

			if (is_G7400)
			{
				Definition = new(Port1.Definition.Name)
				{
					BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(new[]
					{
						"0", "1", "2", "3", "4", "5", "6", "7",
						"8", "9",         "SPC", "?", "L", "P",
						"+", "W", "E", "R", "T", "U", "I", "O",
						"Q", "S", "D", "F", "G", "H", "J", "K",
						"A", "Z", "X", "C", "V", "B", "M", "PERIOD",
						"-", "*", "/", "=", "YES", "NO", "CLR", "ENT",
						"Reset","Power", 
						"SHIFT", "LOCK", "CTNL", ":", "|", "]", "..", ",", "<", "ESC", "BREAK", "RET"
					})
					.ToList()
				};
			}
			else
			{
				Definition = new(Port1.Definition.Name)
				{
					BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(new[]
					{
						"0", "1", "2", "3", "4", "5", "6", "7",
						"8", "9",         "SPC", "?", "L", "P",
						"+", "W", "E", "R", "T", "U", "I", "O",
						"Q", "S", "D", "F", "G", "H", "J", "K",
						"A", "Z", "X", "C", "V", "B", "M", "PERIOD",
						"-", "*", "/", "=", "YES", "NO", "CLR", "ENT",
						"Reset","Power"
					})
					.ToList()
				};
			}

			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);

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
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.EndSection();
			ser.BeginSection(nameof(Port2));
			Port2.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1, Port2;
	}
}
