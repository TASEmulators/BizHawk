using System.Collections.Generic;
using System.Linq;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public class DoomControllerDeck
	{
		public DoomControllerDeck(DoomControllerTypes controllerType, bool player1Present, bool player2Present, bool player3Present, bool player4Present, bool longtics)
		{
			Definition = new("Doom Demo LMP 1.9 Input Format") { };

			if (player1Present) _port1 = ControllerCtors[controllerType](1, longtics);
			if (player2Present) _port2 = ControllerCtors[controllerType](2, longtics);
			if (player3Present) _port3 = ControllerCtors[controllerType](3, longtics);
			if (player4Present) _port4 = ControllerCtors[controllerType](4, longtics);

			if (player1Present) Definition.BoolButtons.AddRange(_port1.Definition.BoolButtons.ToList());
			if (player2Present) Definition.BoolButtons.AddRange(_port2.Definition.BoolButtons.ToList());
			if (player3Present) Definition.BoolButtons.AddRange(_port3.Definition.BoolButtons.ToList());
			if (player4Present) Definition.BoolButtons.AddRange(_port4.Definition.BoolButtons.ToList());

			if (player1Present) foreach (var kvp in _port1.Definition.Axes) Definition.Axes.Add(kvp);
			if (player2Present) foreach (var kvp in _port2.Definition.Axes) Definition.Axes.Add(kvp);
			if (player3Present) foreach (var kvp in _port3.Definition.Axes) Definition.Axes.Add(kvp);
			if (player4Present) foreach (var kvp in _port4.Definition.Axes) Definition.Axes.Add(kvp);

			Definition.BoolButtons.AddRange([
				"Change Gamma",
				"Automap Toggle",
				"Automap +",
				"Automap -",
				"Automap Full/Zoom",
				"Automap Follow",
				"Automap Up",
				"Automap Down",
				"Automap Right",
				"Automap Left",
				"Automap Grid",
				"Automap Mark",
				"Automap Clear Marks"
			]);

			Definition.MakeImmutable();
		}

		private readonly IPort _port1;
		private readonly IPort _port2;
		private readonly IPort _port3;
		private readonly IPort _port4;
		private static IReadOnlyDictionary<DoomControllerTypes, Func<int, bool, IPort>> _controllerCtors;
		public ControllerDefinition Definition { get; }
		public int ReadButtons1(IController c) => _port1.ReadButtons(c);
		public int ReadButtons2(IController c) => _port2.ReadButtons(c);
		public int ReadButtons3(IController c) => _port3.ReadButtons(c);
		public int ReadButtons4(IController c) => _port4.ReadButtons(c);
		public int ReadAxis1(IController c, int axis) => _port1.ReadAxis(c, axis);
		public int ReadAxis2(IController c, int axis) => _port2.ReadAxis(c, axis);
		public int ReadAxis3(IController c, int axis) => _port3.ReadAxis(c, axis);
		public int ReadAxis4(IController c, int axis) => _port4.ReadAxis(c, axis);

		public static IReadOnlyDictionary<DoomControllerTypes, Func<int, bool, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<DoomControllerTypes, Func<int, bool, IPort>>
			{
				[DoomControllerTypes.Doom] = (portNum, longtics) => new DoomController(portNum, longtics),
				[DoomControllerTypes.Heretic] = (portNum, longtics) => new HereticController(portNum, longtics),
				[DoomControllerTypes.Hexen] = (portNum, longtics) => new HexenController(portNum, longtics),
			};
	}
}
