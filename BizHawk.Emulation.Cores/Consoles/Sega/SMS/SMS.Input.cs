using System;

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

		public static readonly ControllerDefinition SMSPaddleController = new ControllerDefinition
		{
			Name = "SMS Paddle Controller",
			BoolButtons =
			{
				"Reset", "Pause",
				"P1 Left", "P1 Right", "P1 B1",
				"P2 Left", "P2 Right", "P2 B1",
			},
			FloatControls =
			{
				"P1 Paddle",
				"P2 Paddle"
			},
			FloatRanges =
			{
				new ControllerDefinition.FloatRange(0, 128, 255),
				new ControllerDefinition.FloatRange(0, 128, 255)
			}
		};

		public static readonly ControllerDefinition SMSLightPhaserController = new ControllerDefinition
		{
			Name = "SMS Light Phaser Controller",
			BoolButtons =
			{
				"Reset", "Pause",
				"P1 Trigger",
			},
			FloatControls =
			{
				"P1 X", "P1 Y",
			},
			FloatRanges =
			{
				new ControllerDefinition.FloatRange(0, 64, 127),
				new ControllerDefinition.FloatRange(0, 500, 1000)
			}
		};

		// The paddles have a nibble select state
		bool Paddle1High = false;
		bool Paddle2High = false;

		const int PaddleMin = 0;
		const int PaddleMax = 255;

		bool LatchLightPhaser = false;
		
		private byte ReadControls1()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			switch (Settings.ControllerType)
			{
				case "Paddle":
					// use analog values from a controller, see http://www.smspower.org/Development/Paddle

					int paddle1Pos;
					if (_controller.IsPressed("P1 Left"))
						paddle1Pos = PaddleMin;
					else if (_controller.IsPressed("P1 Right"))
						paddle1Pos = PaddleMax;
					else
						paddle1Pos = (int)_controller.GetFloat("P1 Paddle");

					int paddle2Pos;
					if (_controller.IsPressed("P2 Left"))
						paddle2Pos = PaddleMin;
					else if (_controller.IsPressed("P2 Right"))
						paddle2Pos = PaddleMax;
					else
						paddle2Pos = (int)_controller.GetFloat("P2 Paddle");

					// The 3F port's TH slot is also used on games in some games in Export BIOS to clock the paddle state
					// Yes it's silly considering the paddle was never released outside Japan but the games think otherwise
					if (_region != "Japan")
					{
						if ((Port3F & 0x02) == 0x00)
						{
							Paddle1High = (Port3F & 0x20) != 0;
							Paddle2High = Paddle1High;
						}
						if ((Port3F & 0x08) == 0x00)
						{
							Paddle2High = (Port3F & 0x80) != 0;
						}
					}

					if (Paddle1High)
					{
						if ((paddle1Pos & 0x10) == 0) value &= 0xFE;
						if ((paddle1Pos & 0x20) == 0) value &= 0xFD;
						if ((paddle1Pos & 0x40) == 0) value &= 0xFB;
						if ((paddle1Pos & 0x80) == 0) value &= 0xF7;
					}
					else
					{
						if ((paddle1Pos & 0x01) == 0) value &= 0xFE;
						if ((paddle1Pos & 0x02) == 0) value &= 0xFD;
						if ((paddle1Pos & 0x04) == 0) value &= 0xFB;
						if ((paddle1Pos & 0x08) == 0) value &= 0xF7;
					}

					if (_controller.IsPressed("P1 B1")) value &= 0xEF;
					if (!Paddle1High) value &= 0xDF;

					if (Paddle2High)
					{
						if ((paddle2Pos & 0x10) == 0) value &= 0xBF;
						if ((paddle2Pos & 0x20) == 0) value &= 0x7F;
					}
					else
					{
						if ((paddle2Pos & 0x01) == 0) value &= 0xBF;
						if ((paddle2Pos & 0x02) == 0) value &= 0x7F;
					}

					// toggle state for Japanese region controllers
					Paddle1High = !Paddle1High;

					break;

				case "Light Phaser":
					if (_controller.IsPressed("P1 Trigger")) value &= 0xEF;
					break;

				default:
					// Normal controller

					if (_controller.IsPressed("P1 Up")) value &= 0xFE;
					if (_controller.IsPressed("P1 Down")) value &= 0xFD;
					if (_controller.IsPressed("P1 Left")) value &= 0xFB;
					if (_controller.IsPressed("P1 Right")) value &= 0xF7;
					if (_controller.IsPressed("P1 B1")) value &= 0xEF;
					if (_controller.IsPressed("P1 B2")) value &= 0xDF;

					if (_controller.IsPressed("P2 Up")) value &= 0xBF;
					if (_controller.IsPressed("P2 Down")) value &= 0x7F;
					break;
			}

			return value;
		}

		private byte ReadControls2()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			switch (Settings.ControllerType)
			{
				case "Paddle":
					// use analog values from a controller, see http://www.smspower.org/Development/Paddle

					int paddle2Pos;
					if (_controller.IsPressed("P2 Left"))
						paddle2Pos = PaddleMin;
					else if (_controller.IsPressed("P2 Right"))
						paddle2Pos = PaddleMax;
					else
						paddle2Pos = (int)_controller.GetFloat("P2 Paddle");

					if (_region != "Japan")
					{
						if ((Port3F & 0x08) == 0x00)
						{
							Paddle2High = (Port3F & 0x80) != 0;
						}
					}

					if (Paddle2High)
					{
						if ((paddle2Pos & 0x40) == 0) value &= 0xFE;
						if ((paddle2Pos & 0x80) == 0) value &= 0xFD;
					}
					else
					{
						if ((paddle2Pos & 0x04) == 0) value &= 0xFE;
						if ((paddle2Pos & 0x08) == 0) value &= 0xFD;
					}

					if (_controller.IsPressed("P2 B1")) value &= 0xFB;
					if (!Paddle2High) value &= 0xF7;

					Paddle2High = !Paddle2High;

					break;

				case "Light Phaser":
					if (LatchLightPhaser)
					{
						value &= 0xBF;
						LatchLightPhaser = false;
					}
					break;

				default:
					// Normal controller

					if (_controller.IsPressed("P2 Left")) value &= 0xFE;
					if (_controller.IsPressed("P2 Right")) value &= 0xFD;
					if (_controller.IsPressed("P2 B1")) value &= 0xFB;
					if (_controller.IsPressed("P2 B2")) value &= 0xF7;
					break;
			}

			if (_controller.IsPressed("Reset")) value &= 0xEF;

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

		internal void ProcessLineControls()
		{
			const int phaserRadius = 4;

			// specifically lightgun needs to do things on a per-line basis
			if (Settings.ControllerType == "Light Phaser")
			{
				byte phaserX = (byte)(_controller.GetFloat("P1 X") + 20);
				int phaserY = (int)_controller.GetFloat("P1 Y");
				int scanline = Vdp.ScanLine;

				if (!LatchLightPhaser && phaserY >= scanline - phaserRadius && phaserY <= scanline + phaserRadius)
				{
					if (scanline >= Vdp.FrameHeight)
						return;

					// latch HCounter via TH
					Vdp.HCounter = phaserX;
					LatchLightPhaser = true;
				}
				else
				{
					LatchLightPhaser = false;
				}
			}
		}

		byte ReadPort0()
		{
			if (IsGameGear == false)
			{
				return 0xFF;
			}

			byte value = 0xFF;
			if ((_controller.IsPressed("Pause") && !IsGameGear) ||
				(_controller.IsPressed("P1 Start") && IsGameGear))
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