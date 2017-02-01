using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		public static readonly ControllerDefinition SmsController = new ControllerDefinition
			{
				Name = "SMS Controller",
				BoolButtons =
				{
					"Reset", "Pause",
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2"
				}
			};

		public static readonly ControllerDefinition GGController = new ControllerDefinition
		{
			Name = "GG Controller",
			BoolButtons =
				{
					"Reset",
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2", "P1 Start"
				}
		};

		private byte ReadControls1()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			if (Controller.IsPressed("P1 Up")) value &= 0xFE;
			if (Controller.IsPressed("P1 Down")) value &= 0xFD;
			if (Controller.IsPressed("P1 Left")) value &= 0xFB;
			if (Controller.IsPressed("P1 Right")) value &= 0xF7;
			if (Controller.IsPressed("P1 B1")) value &= 0xEF;
			if (Controller.IsPressed("P1 B2")) value &= 0xDF;

			if (Controller.IsPressed("P2 Up")) value &= 0xBF;
			if (Controller.IsPressed("P2 Down")) value &= 0x7F;

			return value;
		}

		private byte ReadControls2()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			if (Controller.IsPressed("P2 Left")) value &= 0xFE;
			if (Controller.IsPressed("P2 Right")) value &= 0xFD;
			if (Controller.IsPressed("P2 B1")) value &= 0xFB;
			if (Controller.IsPressed("P2 B2")) value &= 0xF7;

			if (Controller.IsPressed("Reset")) value &= 0xEF;

			if ((Port3F & 0x0F) == 5)
			{
				if (_region == "Japan")
				{
					value &= 0x3F;
				}
				else // US / Europe
				{
					if (Port3F >> 4 == 0x0F)
						value |= 0xC0;
					else
						value &= 0x3F;
				}
			}

			return value;
		}

		byte ReadPort0()
		{
			if (IsGameGear == false)
			{
				return 0xFF;
			}

			byte value = 0xFF;
			if ((Controller.IsPressed("Pause") && !IsGameGear) ||
				(Controller.IsPressed("P1 Start") && IsGameGear))
			{
				value ^= 0x80;
			}

			if (RegionStr == "Japan")
			{
				value ^= 0x40;
			}

			return value;
		}
	}
}