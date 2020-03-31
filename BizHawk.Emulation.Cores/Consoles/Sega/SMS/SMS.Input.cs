using System.Collections.Generic;
using System.Linq;

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
			AxisControls =
			{
				"P1 Paddle",
				"P2 Paddle"
			},
			AxisRanges =
			{
				new ControllerDefinition.AxisRange(0, 128, 255),
				new ControllerDefinition.AxisRange(0, 128, 255)
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
			AxisControls =
			{
				"P1 X", "P1 Y",
			},
			AxisRanges =
			{
				new ControllerDefinition.AxisRange(0, 64, 127),
				new ControllerDefinition.AxisRange(0, 500, 1000)
			}
		};

		/// <remarks>TODO verify direction against hardware</remarks>
		private static readonly List<ControllerDefinition.AxisRange> SportsPadTrackballRanges = ControllerDefinition.CreateAxisRangePair(-64, 0, 63, ControllerDefinition.AxisPairOrientation.RightAndUp);

		public static readonly ControllerDefinition SMSSportsPadController = new ControllerDefinition
		{
			Name = "SMS Sports Pad Controller",
			BoolButtons =
			{
				"Reset", "Pause",
				"P1 Left", "P1 Right", "P1 Up", "P1 Down", "P1 B1", "P1 B2",
				"P2 Left", "P2 Right", "P2 Up", "P2 Down", "P2 B1", "P2 B2"
			},
			AxisControls =
			{
				"P1 X", "P1 Y",
				"P2 X", "P2 Y"
			},
			AxisRanges = SportsPadTrackballRanges.Concat(SportsPadTrackballRanges).ToList()
		};

		public static readonly ControllerDefinition SMSKeyboardController = new ControllerDefinition
		{
			Name = "SMS Keyboard Controller",
			BoolButtons =
			{
				"Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Minus", "Key Caret", "Key Yen", "Key Break",
				"Key Function", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P", "Key At", "Key Left Bracket", "Key Return", "Key Up Arrow",
				"Key Control", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Semicolon", "Key Colon", "Key Right Bracket", "Key Left Arrow", "Key Right Arrow",
				"Key Shift", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Comma", "Key Period", "Key Slash", "Key PI", "Key Down Arrow",
				"Key Graph", "Key Kana", "Key Space", "Key Home/Clear", "Key Insert/Delete",

				"Reset", "Pause",
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2"
			}
		};

		private static readonly string[] KeyboardMap =
		{
			"Key 1", "Key Q", "Key A", "Key Z", "Key Kana", "Key Comma", "Key K", "Key I", "Key 8", null, null, null,
			"Key 2", "Key W", "Key S", "Key X", "Key Space", "Key Period", "Key L", "Key O", "Key 9", null, null, null,
			"Key 3", "Key E", "Key D", "Key C", "Key Home/Clear", "Key Slash", "Key Semicolon", "Key P", "Key 0", null, null, null,
			"Key 4", "Key R", "Key F", "Key V", "Key Insert/Delete", "Key PI", "Key Colon", "Key At", "Key Minus", null, null, null,
			"Key 5", "Key T", "Key G", "Key B", null, "Key Down Arrow", "Key Right Bracket", "Key Left Bracket", "Key Caret", null, null, null,
			"Key 6", "Key Y", "Key H", "Key N", null, "Key Left Arrow", "Key Return", null, "Key Yen", null, null, "Key Function",
			"Key 7", "Key U", "Key J", "Key M", null, "Key Right Arrow", "Key Up Arrow", null, "Key Break", "Key Graph", "Key Control", "Key Shift",
			"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2", "P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2"
		};

		const int PaddleMin = 0;
		const int PaddleMax = 255;
		const int SportsPadMin = -64;
		const int SportsPadMax = 63;

		// The paddles and sports pads have data select states
		bool Controller1SelectHigh = true;
		bool Controller2SelectHigh = true;

		bool LatchLightPhaser = false;

		// further state value for sports pad, may be useful for other controllers in future
		int Controller1State = 3;
		int Controller2State = 3;
		int ControllerTick = 0; // for timing in japan

		private byte ReadControls1()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			switch (SyncSettings.ControllerType)
			{
				case SmsSyncSettings.ControllerTypes.Paddle:
					{
						// use analog values from a controller, see http://www.smspower.org/Development/Paddle

						int paddle1Pos;
						if (_controller.IsPressed("P1 Left"))
							paddle1Pos = PaddleMin;
						else if (_controller.IsPressed("P1 Right"))
							paddle1Pos = PaddleMax;
						else
							paddle1Pos = (int)_controller.AxisValue("P1 Paddle");

						int paddle2Pos;
						if (_controller.IsPressed("P2 Left"))
							paddle2Pos = PaddleMin;
						else if (_controller.IsPressed("P2 Right"))
							paddle2Pos = PaddleMax;
						else
							paddle2Pos = (int)_controller.AxisValue("P2 Paddle");

						PresetControllerState(1);
						// Hard-wired together?
						Controller2SelectHigh = Controller1SelectHigh;

						if (Controller1SelectHigh)
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
						if (!Controller1SelectHigh) value &= 0xDF;

						if (Controller2SelectHigh)
						{
							if ((paddle2Pos & 0x10) == 0) value &= 0xBF;
							if ((paddle2Pos & 0x20) == 0) value &= 0x7F;
						}
						else
						{
							if ((paddle2Pos & 0x01) == 0) value &= 0xBF;
							if ((paddle2Pos & 0x02) == 0) value &= 0x7F;
						}

						PostsetControllerState(1);
					}
					break;

				case SmsSyncSettings.ControllerTypes.LightPhaser:
					if (_controller.IsPressed("P1 Trigger")) value &= 0xEF;
					break;

				case SmsSyncSettings.ControllerTypes.SportsPad:
					{
						int p1X;
						if (_controller.IsPressed("P1 Left"))
							p1X = SportsPadMin;
						else if (_controller.IsPressed("P1 Right"))
							p1X = SportsPadMax;
						else
							p1X = (int)_controller.AxisValue("P1 X");

						int p1Y;
						if (_controller.IsPressed("P1 Up"))
							p1Y = SportsPadMin;
						else if (_controller.IsPressed("P1 Down"))
							p1Y = SportsPadMax;
						else
							p1Y = (int)_controller.AxisValue("P1 Y");

						int p2X;
						if (_controller.IsPressed("P2 Left"))
							p2X = SportsPadMin;
						else if (_controller.IsPressed("P2 Right"))
							p2X = SportsPadMax;
						else
							p2X = (int)_controller.AxisValue("P2 X");

						int p2Y;
						if (_controller.IsPressed("P2 Up"))
							p2Y = SportsPadMin;
						else if (_controller.IsPressed("P2 Down"))
							p2Y = SportsPadMax;
						else
							p2Y = (int)_controller.AxisValue("P2 Y");

						if (_region == SmsSyncSettings.Regions.Japan)
						{
							p1X += 128;
							p1Y += 128;
							p2X += 128;
							p2Y += 128;
						}
						else
						{
							p1X *= -1;
							p1Y *= -1;
							p2X *= -1;
							p2Y *= -1;
						}

						PresetControllerState(1);

						// advance state
						if (Controller1SelectHigh && (Controller1State % 2 == 0))
						{
							++Controller1State;
						}
						else if (!Controller1SelectHigh && (Controller1State % 2 == 1))
						{
							if (++Controller1State == (_region == SmsSyncSettings.Regions.Japan ? 6 : 4))
								Controller1State = 0;
						}
						if (Controller2SelectHigh && (Controller2State % 2 == 0))
						{
							++Controller2State;
						}
						else if (!Controller2SelectHigh && (Controller2State % 2 == 1))
						{
							if (++Controller2State == (_region == SmsSyncSettings.Regions.Japan ? 6 : 4))
								Controller2State = 0;
						}

						switch (Controller1State)
						{
							case 0:
								if ((p1X & 0x10) == 0) value &= 0xFE;
								if ((p1X & 0x20) == 0) value &= 0xFD;
								if ((p1X & 0x40) == 0) value &= 0xFB;
								if ((p1X & 0x80) == 0) value &= 0xF7;
								break;
							case 1:
								if ((p1X & 0x01) == 0) value &= 0xFE;
								if ((p1X & 0x02) == 0) value &= 0xFD;
								if ((p1X & 0x04) == 0) value &= 0xFB;
								if ((p1X & 0x08) == 0) value &= 0xF7;
								break;
							case 2:
								if ((p1Y & 0x10) == 0) value &= 0xFE;
								if ((p1Y & 0x20) == 0) value &= 0xFD;
								if ((p1Y & 0x40) == 0) value &= 0xFB;
								if ((p1Y & 0x80) == 0) value &= 0xF7;
								break;
							case 3:
								if ((p1Y & 0x01) == 0) value &= 0xFE;
								if ((p1Y & 0x02) == 0) value &= 0xFD;
								if ((p1Y & 0x04) == 0) value &= 0xFB;
								if ((p1Y & 0x08) == 0) value &= 0xF7;
								break;
							case 4:
								// specific to Japan: sync via TR
								value &= 0xDF;
								break;
							case 5:
								// specific to Japan: buttons
								if (_controller.IsPressed("P1 B1")) value &= 0xFE;
								if (_controller.IsPressed("P1 B2")) value &= 0xFD;
								break;
						}

						if (_region != SmsSyncSettings.Regions.Japan)
						{
							// Buttons like normal in Export
							if (_controller.IsPressed("P1 B1")) value &= 0xEF;
							if (_controller.IsPressed("P1 B2")) value &= 0xDF;
						}
						else
						{
							// In Japan, it contains selectHigh
							if (!Controller1SelectHigh) value &= 0xEF;
						}

						switch (Controller2State)
						{
							case 0:
								if ((p2X & 0x10) == 0) value &= 0xBF;
								if ((p2X & 0x20) == 0) value &= 0x7F;
								break;
							case 1:
								if ((p2X & 0x01) == 0) value &= 0xBF;
								if ((p2X & 0x02) == 0) value &= 0x7F;
								break;
							case 2:
								if ((p2Y & 0x10) == 0) value &= 0xBF;
								if ((p2Y & 0x20) == 0) value &= 0x7F;
								break;
							case 3:
								if ((p2Y & 0x01) == 0) value &= 0xBF;
								if ((p2Y & 0x02) == 0) value &= 0x7F;
								break;
							case 5:
								// specific to Japan: buttons
								if (_controller.IsPressed("P2 B1")) value &= 0xBF;
								if (_controller.IsPressed("P2 B2")) value &= 0x7F;
								break;
						}

						PostsetControllerState(1);
					}
					break;

				case SmsSyncSettings.ControllerTypes.Keyboard:
					{
						// use keyboard map to get each bit

						for (int bit = 0; bit < 8; ++bit)
						{
							string key = KeyboardMap[(PortDE & 0x07) * 12 + bit];

							if (key != null && _controller.IsPressed(key))
							{
								value &= (byte)~(1 << bit);
							}
						}
					}
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

			switch (SyncSettings.ControllerType)
			{
				case SmsSyncSettings.ControllerTypes.Paddle:
					{
						// use analog values from a controller, see http://www.smspower.org/Development/Paddle

						int paddle2Pos;
						if (_controller.IsPressed("P2 Left"))
							paddle2Pos = PaddleMin;
						else if (_controller.IsPressed("P2 Right"))
							paddle2Pos = PaddleMax;
						else
							paddle2Pos = (int)_controller.AxisValue("P2 Paddle");

						PresetControllerState(2);

						if (Controller2SelectHigh)
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
						if (!Controller2SelectHigh) value &= 0xF7;

						PostsetControllerState(2);
					}
					break;

				case SmsSyncSettings.ControllerTypes.LightPhaser:
					if (LatchLightPhaser)
					{
						value &= 0xBF;
						LatchLightPhaser = false;
					}
					break;

				case SmsSyncSettings.ControllerTypes.SportsPad:
					{
						int p2X;
						if (_controller.IsPressed("P2 Left"))
							p2X = SportsPadMin;
						else if (_controller.IsPressed("P2 Right"))
							p2X = SportsPadMax;
						else
							p2X = (int)_controller.AxisValue("P2 X");

						int p2Y;
						if (_controller.IsPressed("P2 Down"))
							p2Y = SportsPadMin;
						else if (_controller.IsPressed("P2 Up"))
							p2Y = SportsPadMax;
						else
							p2Y = (int)_controller.AxisValue("P2 Y");

						if (_region == SmsSyncSettings.Regions.Japan)
						{
							p2X += 128;
							p2Y += 128;
						}
						else
						{
							p2X *= -1;
							p2Y *= -1;
						}

						PresetControllerState(2);

						if (Controller2SelectHigh && (Controller2State % 2 == 0))
						{
							++Controller2State;
						}
						else if (!Controller2SelectHigh && (Controller2State % 2 == 1))
						{
							if (++Controller2State == (_region == SmsSyncSettings.Regions.Japan ? 6 : 4))
								Controller2State = 0;
						}

						switch (Controller2State)
						{
							case 0:
								if ((p2X & 0x40) == 0) value &= 0xFE;
								if ((p2X & 0x80) == 0) value &= 0xFD;
								break;
							case 1:
								if ((p2X & 0x04) == 0) value &= 0xFE;
								if ((p2X & 0x08) == 0) value &= 0xFD;
								break;
							case 2:
								if ((p2Y & 0x40) == 0) value &= 0xFE;
								if ((p2Y & 0x80) == 0) value &= 0xFD;
								break;
							case 3:
								if ((p2Y & 0x04) == 0) value &= 0xFE;
								if ((p2Y & 0x08) == 0) value &= 0xFD;
								break;
						}
						if (_region != SmsSyncSettings.Regions.Japan)
						{
							// Buttons like normal in Export
							if (_controller.IsPressed("P2 B1")) value &= 0xFB;
							if (_controller.IsPressed("P2 B2")) value &= 0xF7;
						}
						else
						{
							if (!Controller2SelectHigh) value &= 0xF7;
						}

						PostsetControllerState(2);
					}
					break;

				case SmsSyncSettings.ControllerTypes.Keyboard:
					{
						value &= 0x7F;

						// use keyboard map to get each bit

						for (int bit = 0; bit < 4; ++bit)
						{
							string key = KeyboardMap[(PortDE & 0x07) * 12 + bit + 8];

							if (key != null && _controller.IsPressed(key))
							{
								value &= (byte)~(1 << bit);
							}
						}
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
				if (_region == SmsSyncSettings.Regions.Japan)
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
			if (SyncSettings.ControllerType == SmsSyncSettings.ControllerTypes.LightPhaser)
			{
				byte phaserX = (byte)(_controller.AxisValue("P1 X") + 20);
				int phaserY = (int)_controller.AxisValue("P1 Y");
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
			if (IsGameGear_C == false)
			{
				return 0xFF;
			}

			_lagged = false;

			byte value = 0xFF;
			if ((_controller.IsPressed("Pause") && !IsGameGear) ||
				(_controller.IsPressed("P1 Start") && IsGameGear_C))
			{
				value ^= 0x80;
			}

			if (_region == SmsSyncSettings.Regions.Japan)
			{
				value ^= 0x40;
			}

			return value;
		}

		private void PresetControllerState(int pin)
		{
			// The 3F port's TH slot is also used on games in some games in Export BIOS to clock the paddle state
			// Re: the paddle: Yes it's silly considering the paddle was never released outside Japan but the games think otherwise

			if (_region != SmsSyncSettings.Regions.Japan)
			{
				if ((Port3F & 0x02) == 0x00)
				{
					Controller1SelectHigh = (Port3F & 0x20) != 0;

					// resync
					Controller2State = 3;
				}

				if ((Port3F & 0x08) == 0x00)
				{
					Controller2SelectHigh = (Port3F & 0x80) != 0;

					// resync
					Controller1State = 3;
				}
			}
		}

		private void PostsetControllerState(int pin)
		{
			// for the benefit of the Japan region
			if (_region == SmsSyncSettings.Regions.Japan && (++ControllerTick) == 2)
			{
				ControllerTick = 0;

				if (pin == 1)
				{
					Controller1SelectHigh ^= true;
				}
				else
				{
					Controller1SelectHigh = false;
					Controller2SelectHigh ^= true;
				}
			}
		}
	}
} 