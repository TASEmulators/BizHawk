using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public class SMSControllerDeck
	{
		public SMSControllerDeck(SMSControllerTypes controller1, SMSControllerTypes controller2, bool is_GG, bool use_keyboard)
		{
			if (is_GG)
			{
				Port1 = new GGController(1);
				// Port 2 is defined, but not used for Game Gear
				Port2 = new GGController(2);

				Definition = new ControllerDefinition(Port1.Definition.Name)
				{
					BoolButtons = new[] { "Reset" }
							.Concat(Port1.Definition.BoolButtons)
							.ToList()
				};
			}
			else
			{
				Port1 = ControllerCtors[controller1](1);
				Port2 = ControllerCtors[controller2](2);

				if (!use_keyboard) 
				{
					Definition = new ControllerDefinition(Port1.Definition.Name)
					{
						BoolButtons = new[] { "Reset", "Pause" }
								.Concat(Port1.Definition.BoolButtons)
								.Concat(Port2.Definition.BoolButtons)
								.ToList()
					};
				}
				else
				{
					Definition = new ControllerDefinition(Port1.Definition.Name)
					{
						BoolButtons = new[] { "Reset", "Pause" }
								.Concat(Port1.Definition.BoolButtons)
								.Concat(Port2.Definition.BoolButtons)
								.Concat(KeyboardMap)
								.ToList()
					};
				}

				foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);
				foreach (var kvp in Port2.Definition.Axes) Definition.Axes.Add(kvp);
			}
			Definition.MakeImmutable();
		}

		public byte ReadPort1_c1(IController c)
		{
			return Port1.Read_p1_c1(c);
		}

		public byte ReadPort1_c2(IController c)
		{
			return Port2.Read_p1_c2(c);
		}

		public byte ReadPort2_c1(IController c)
		{
			return Port1.Read_p2_c1(c);
		}

		public byte ReadPort2_c2(IController c)
		{
			return Port2.Read_p2_c2(c);
		}

		public bool GetPin_c1(IController c)
		{
			return Port1.PinStateGet(c);
		}

		public bool GetPin_c2(IController c)
		{
			return Port2.PinStateGet(c);
		}

		public void SetPin_c1(IController c, bool val)
		{
			Port1.PinStateSet(c, val);
		}

		public void SetPin_c2(IController c, bool val)
		{
			Port2.PinStateSet(c, val);
		}

		public void SetCounter_c1(IController c, int val)
		{
			Port1.CounterSet(c, val);
		}

		public void SetCounter_c2(IController c, int val)
		{
			Port2.CounterSet(c, val);
		}

		public void SetRegion(IController c, bool val)
		{
			Port1.RegionSet(c, val);
			Port2.RegionSet(c, val);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Port1));
			Port1.SyncState(ser);
			Port2.SyncState(ser);
			ser.EndSection();
		}

		private readonly IPort Port1, Port2;

		private static IReadOnlyDictionary<SMSControllerTypes, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<SMSControllerTypes, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<SMSControllerTypes, Func<int, IPort>>
			{
				[SMSControllerTypes.Standard] = portNum => new SmsController(portNum),
				[SMSControllerTypes.Paddle] = portNum => new SMSPaddleController(portNum),
				[SMSControllerTypes.SportsPad] = portNum => new SMSSportsPadController(portNum),
				[SMSControllerTypes.Phaser] = portNum => new SMSLightPhaserController(portNum),
			};

		public static string DefaultControllerName => typeof(SmsController).DisplayName();

		private static readonly string[] KeyboardMap =
		{
			"Key 1", "Key Q", "Key A", "Key Z", "Key Kana", "Key Comma", "Key K", "Key I", "Key 8",
			"Key 2", "Key W", "Key S", "Key X", "Key Space", "Key Period", "Key L", "Key O", "Key 9",
			"Key 3", "Key E", "Key D", "Key C", "Key Home/Clear", "Key Slash", "Key Semicolon", "Key P", "Key 0",
			"Key 4", "Key R", "Key F", "Key V", "Key Insert/Delete", "Key PI", "Key Colon", "Key At", "Key Minus",
			"Key 5", "Key T", "Key G", "Key B", "Key Down Arrow", "Key Right Bracket", "Key Left Bracket", "Key Caret",
			"Key 6", "Key Y", "Key H", "Key N", "Key Left Arrow", "Key Return", "Key Yen", "Key Function",
			"Key 7", "Key U", "Key J", "Key M", "Key Right Arrow", "Key Up Arrow", "Key Break", "Key Graph", "Key Control", "Key Shift",
			"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2", "P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2"
		};
	}
}
