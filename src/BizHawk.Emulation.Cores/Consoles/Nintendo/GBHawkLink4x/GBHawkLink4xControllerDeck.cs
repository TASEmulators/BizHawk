using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public class GBHawkLink4xControllerDeck
	{
		public GBHawkLink4xControllerDeck(string controller1Name, string controller2Name, string controller3Name, string controller4Name)
		{
			Port1 = GBHawkControllerDeck.ControllerCtors.TryGetValue(controller1Name, out var ctor1)
				? ctor1(1)
				: throw new InvalidOperationException($"Invalid controller type: {controller1Name}");
			Port2 = GBHawkControllerDeck.ControllerCtors.TryGetValue(controller2Name, out var ctor2)
				? ctor2(2)
				: throw new InvalidOperationException($"Invalid controller type: {controller2Name}");
			Port3 = GBHawkControllerDeck.ControllerCtors.TryGetValue(controller3Name, out var ctor3)
				? ctor3(3)
				: throw new InvalidOperationException($"Invalid controller type: {controller3Name}");
			Port4 = GBHawkControllerDeck.ControllerCtors.TryGetValue(controller4Name, out var ctor4)
				? ctor4(4)
				: throw new InvalidOperationException($"Invalid controller type: {controller4Name}");

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
