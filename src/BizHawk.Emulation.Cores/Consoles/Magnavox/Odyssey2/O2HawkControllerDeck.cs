using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public class O2HawkControllerDeck
	{
		public O2HawkControllerDeck(string controller1Name, string controller2Name, bool is_G7400)
		{
			Port1 = ControllerCtors.TryGetValue(controller1Name, out var ctor1)
				? ctor1(1)
				: throw new InvalidOperationException($"Invalid controller type: {controller1Name}");
			Port2 = ControllerCtors.TryGetValue(controller2Name, out var ctor2)
				? ctor2(2)
				: throw new InvalidOperationException($"Invalid controller type: {controller2Name}");

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

		private static IReadOnlyDictionary<string, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<string, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<string, Func<int, IPort>>
			{
				[typeof(StandardControls).DisplayName()] = portNum => new StandardControls(portNum)
			};

		public static string DefaultControllerName => typeof(StandardControls).DisplayName();
	}
}
