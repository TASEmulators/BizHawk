using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public class DoomControllerDeck
	{
		public DoomControllerDeck(DoomControllerTypes controller1, DoomControllerTypes controller2)
		{
			Port1 = ControllerCtors[controller1](1);
			Port2 = ControllerCtors[controller2](2);

			Definition = new("Doom Demo LMP 1.9 Input Format")
			{
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat(
					[
						"Reset", "Select", "Power", "Toggle Left Difficulty", "Toggle Right Difficulty"
					])
					.ToList()
			};

			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);
			foreach (var kvp in Port2.Definition.Axes) Definition.Axes.Add(kvp);

			Definition.MakeImmutable();
		}

		public byte ReadPort1(IController c)
			=> Port1.Read(c);

		public byte ReadPort2(IController c)
			=> Port2.Read(c);

		public int ReadPot1(IController c, int pot)
			=> Port1.Read_Pot(c, pot);

		public int ReadPot2(IController c, int pot)
			=> Port2.Read_Pot(c, pot);

		public ControllerDefinition Definition { get; }

		private readonly IPort Port1;
		private readonly IPort Port2;

		private static IReadOnlyDictionary<DoomControllerTypes, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<DoomControllerTypes, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<DoomControllerTypes, Func<int, IPort>>
			{
				[DoomControllerTypes.Unplugged] = portNum => new UnpluggedController(portNum),
				[DoomControllerTypes.Joystick] = portNum => new StandardController(portNum),
				//[DoomControllerTypes.Paddle] = portNum => new PaddleController(portNum),
				//[DoomControllerTypes.BoostGrip] = portNum => new BoostGripController(portNum),
				[DoomControllerTypes.Driving] = portNum => new DrivingController(portNum),
				//[DoomControllerTypes.Keyboard] = portNum => new KeyboardController(portNum)
			};
	}
}
