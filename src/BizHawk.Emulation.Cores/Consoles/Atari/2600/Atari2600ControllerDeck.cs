using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	[PeripheralOptionConsumer(typeof(Atari2600ControllerTypes), typeof(IPort), Atari2600ControllerTypes.Joystick)]
	public sealed partial class Atari2600ControllerDeck
	{
		public Atari2600ControllerDeck(Atari2600ControllerTypes controller1, Atari2600ControllerTypes controller2)
		{
			Port1 = CtorFor(controller1)(1);
			Port2 = CtorFor(controller2)(2);

			Definition = new("Atari 2600 Basic Controller")
			{
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(new[]
					{
						"Reset", "Select", "Power", "Toggle Left Difficulty", "Toggle Right Difficulty"
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

		public int ReadPot1(IController c, int pot)
		{
			return Port1.Read_Pot(c, pot);
		}

		public int ReadPot2(IController c, int pot)
		{
			return Port2.Read_Pot(c, pot);
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
