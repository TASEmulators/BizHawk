﻿using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public class Atari2600ControllerDeck
	{
		public Atari2600ControllerDeck(Atari2600ControllerTypes controller1, Atari2600ControllerTypes controller2)
		{
			Port1 = ControllerCtors[controller1](1);
			Port2 = ControllerCtors[controller2](2);

			Definition = new("Atari 2600 Basic Controller")
			{
				BoolButtons = Port1.Definition.BoolButtons
					.Concat(Port2.Definition.BoolButtons)
					.Concat([
						"Reset",
						"Select",
						"Power",
						"Toggle Left Difficulty",
						"Toggle Right Difficulty",
						"Toggle TV Type",
					])
					.ToList(),
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

		private static IReadOnlyDictionary<Atari2600ControllerTypes, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<Atari2600ControllerTypes, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<Atari2600ControllerTypes, Func<int, IPort>>
			{
				[Atari2600ControllerTypes.Unplugged] = portNum => new UnpluggedController(portNum),
				[Atari2600ControllerTypes.Joystick] = portNum => new StandardController(portNum),
				//[Atari2600ControllerTypes.Paddle] = portNum => new PaddleController(portNum),
				//[Atari2600ControllerTypes.BoostGrip] = portNum => new BoostGripController(portNum),
				[Atari2600ControllerTypes.Driving] = portNum => new DrivingController(portNum),
				//[Atari2600ControllerTypes.Keyboard] = portNum => new KeyboardController(portNum)
			};
	}
}
