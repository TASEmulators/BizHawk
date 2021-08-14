using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.SrcGen.PeripheralOption;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	[PeripheralOptionConsumer(typeof(PeripheralOption), typeof(IPort), PeripheralOption.Standard)]
	public sealed partial class A7800HawkControllerDeck
	{
		public A7800HawkControllerDeck(PeripheralOption port1Option, PeripheralOption port2Option)
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
						"Reset",
						"Select",
						"BW",
						"Toggle Left Difficulty", // better not put P# on these as they might not correspond to player numbers
						"Toggle Right Difficulty",
						"Pause"
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

		public byte ReadFire1(IController c)
		{
			return Port1.ReadFire(c);
		}

		public byte ReadFire2(IController c)
		{
			return Port2.ReadFire(c);
		}

		public byte ReadFire1_2x(IController c)
		{
			return Port1.ReadFire2x(c);
		}

		public byte ReadFire2_2x(IController c)
		{
			return Port2.ReadFire2x(c);
		}

		public bool Is_2_button1(IController c)
		{
			return Port1.Is_2_button(c);
		}

		public bool Is_2_button2(IController c)
		{
			return Port2.Is_2_button(c);
		}

		public bool Is_LightGun1(IController c, out float lightgun_x, out float lightgun_y)
		{
			return Port1.Is_LightGun(c, out lightgun_x, out lightgun_y);
		}

		public bool Is_LightGun2(IController c, out float lightgun_x, out float lightgun_y)
		{
			return Port2.Is_LightGun(c, out lightgun_x, out lightgun_y);
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
