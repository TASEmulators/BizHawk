using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	[PeripheralOptionConsumer(typeof(PeripheralOption), typeof(IPort), PeripheralOption.Standard)]
	public sealed partial class ColecoVisionControllerDeck
	{
		public ColecoVisionControllerDeck(PeripheralOption port1Option, PeripheralOption port2Option)
		{
			Port1 = CtorFor(port1Option)(1);
			Port2 = CtorFor(port2Option)(2);

			Definition = new("ColecoVision Basic Controller")
			{
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(new[]
					{
						"Power", "Reset"
					})
					.ToList()
			};

			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in Port2.Definition.Axes) Definition.Axes.Add(kvp);

			Definition.MakeImmutable();
		}

		public float wheel1;
		public float wheel2;

		public float temp_wheel1;
		public float temp_wheel2;

		public byte ReadPort1(IController c, bool leftMode, bool updateWheel)
		{
			wheel1 = Port1.UpdateWheel(c);

			return Port1.Read(c, leftMode, updateWheel, temp_wheel1);
		}

		public byte ReadPort2(IController c, bool leftMode, bool updateWheel)
		{
			wheel2 = Port2.UpdateWheel(c);

			return Port2.Read(c, leftMode, updateWheel, temp_wheel2);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			ser.Sync(nameof(temp_wheel1), ref temp_wheel1);
			ser.EndSection();

			ser.BeginSection(nameof(Port2));
			ser.Sync(nameof(temp_wheel2), ref temp_wheel2);
			Port2.SyncState(ser);
			ser.EndSection();
		}

		public IPort Port1 { get; }
		public IPort Port2 { get; }
	}
}
