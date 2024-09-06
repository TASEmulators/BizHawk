using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		private static readonly Lazy<ControllerDefinition> _channelFControllerDefinition = new(() =>
		{
			ControllerDefinition definition = new("ChannelF Controller");

			// sticks

			const string P1_PREFIX = "P1 ";
			string[] stickR =
			[
				// P1 (right) stick
				P1_PREFIX + "Forward", P1_PREFIX + "Back", P1_PREFIX + "Left", P1_PREFIX + "Right", P1_PREFIX + "CCW", P1_PREFIX + "CW", P1_PREFIX + "Pull", P1_PREFIX + "Push"
			];

			foreach (var s in stickR)
			{
				definition.BoolButtons.Add(s);
				definition.CategoryLabels[s] = "Right Controller";
			}

			const string P2_PREFIX = "P2 ";
			string[] stickL =
			[
				// P2 (left) stick
				P2_PREFIX + "Forward", P2_PREFIX + "Back", P2_PREFIX + "Left", P2_PREFIX + "Right", P2_PREFIX + "CCW", P2_PREFIX + "CW", P2_PREFIX + "Pull", P2_PREFIX + "Push"
			];

			foreach (var s in stickL)
			{
				definition.BoolButtons.Add(s);
				definition.CategoryLabels[s] = "Left Controller";
			}

			// console
			string[] consoleButtons =
			[
				"TIME", "MODE", "HOLD", "START", "RESET"
			];

			foreach (var s in consoleButtons)
			{
				definition.BoolButtons.Add(s);
				definition.CategoryLabels[s] = "Console";
			}

			return definition.MakeImmutable();
		});

		private readonly string[] _buttonsConsole =
		[
			"TIME", "MODE", "HOLD", "START", "RESET"
		];

		private bool[] _stateConsole = new bool[5];

		private byte DataConsole
		{
			get
			{
				var w = 0;
				for (var i = 0; i < 5; i++)
				{
					var mask = (byte)(1 << i);
					w = _stateConsole[i] ? w | mask : w & ~mask;
				}

				return (byte)(w & 0xFF);
			}
		}

		private bool[] _stateRight = new bool[8];

		private readonly string[] _buttonsRight =
		[
			"P1 Right", "P1 Left", "P1 Back", "P1 Forward", "P1 CCW", "P1 CW", "P1 Pull", "P1 Push"
		];

		private byte DataRight
		{
			get
			{
				var w = 0;
				for (var i = 0; i < 8; i++)
				{
					var mask = (byte)(1 << i);
					w = _stateRight[i] ? w | mask : w & ~mask;
				}

				return (byte)(w & 0xFF);
			}
		}

		private bool[] _stateLeft = new bool[8];

		private readonly string[] _buttonsLeft =
		[
			"P2 Right", "P2 Left", "P2 Back", "P2 Forward", "P2 CCW", "P2 CW", "P2 Pull", "P2 Push"
		];

		private byte DataLeft
		{
			get
			{
				var w = 0;
				for (var i = 0; i < 8; i++)
				{
					var mask = (byte)(1 << i);
					w = _stateLeft[i] ? w | mask : w & ~mask;
				}

				return (byte)(w & 0xFF);
			}
		}
	}
}
