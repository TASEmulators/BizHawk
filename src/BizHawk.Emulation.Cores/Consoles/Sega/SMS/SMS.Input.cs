namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
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

		private bool LatchLightPhaser1 = false;
		private bool LatchLightPhaser2 = false;

		private int ControllerTick = 0; // for timing in japan

		private byte ReadControls1()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			PresetControllerState(1);
			// Hard-wired together for paddles?
			_controllerDeck.SetPin_c2(_controller, _controllerDeck.GetPin_c1(_controller));

			value &= _controllerDeck.ReadPort1_c1(_controller);
			value &= _controllerDeck.ReadPort1_c2(_controller);

			PostsetControllerState(1);
			
			if (!IsGameGear_C && SyncSettings.UseKeyboard)
			{
				// 7 represents ordinary controller reads
				if ((PortDE & 7) != 7)
				{
					value = 0xFF;

					for (int bit = 0; bit < 8; ++bit)
					{
						string key = KeyboardMap[(PortDE & 0x07) * 12 + bit];

						if (key != null && _controller.IsPressed(key))
						{
							value &= (byte)~(1 << bit);
						}
					}
				}			
			}

			return value;
		}

		private byte ReadControls2()
		{
			InputCallbacks.Call();
			_lagged = false;
			byte value = 0xFF;

			PresetControllerState(2);

			value &= _controllerDeck.ReadPort2_c1(_controller);
			value &= _controllerDeck.ReadPort2_c2(_controller);

			PostsetControllerState(2);

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

			if (LatchLightPhaser1)
			{
				value &= 0xBF;
				LatchLightPhaser1 = false;
			}

			if (LatchLightPhaser2)
			{
				value &= 0x7F;
				LatchLightPhaser2 = false;
			}

			if (!IsGameGear_C && SyncSettings.UseKeyboard)
			{
				// 7 represents ordinary controller reads
				if ((PortDE & 7) != 7)
				{
					value = 0x7F;

					for (int bit = 0; bit < 4; ++bit)
					{
						string key = KeyboardMap[(PortDE & 0x07) * 12 + bit + 8];

						if (key != null && _controller.IsPressed(key))
						{
							value &= (byte)~(1 << bit);
						}
					}
				}
			}

			return value;
		}

		internal void ProcessLineControls()
		{
			const int phaserRadius = 4;

			// specifically lightgun needs to do things on a per-line basis
			if (!IsGameGear_C) 
			{
				if (SyncSettings.Port1 == SMSControllerTypes.Phaser)
				{
					byte phaserX = (byte)(_controller.AxisValue("P1 X") + 20);
					int phaserY = _controller.AxisValue("P1 Y");
					int scanline = Vdp.ScanLine;

					if (!LatchLightPhaser1 && phaserY >= scanline - phaserRadius && phaserY <= scanline + phaserRadius)
					{
						if (scanline >= Vdp.FrameHeight)
							return;

						// latch HCounter via TH
						Vdp.HCounter = phaserX;
						LatchLightPhaser1 = true;
					}
					else
					{
						LatchLightPhaser1 = false;
					}
				}

				if (SyncSettings.Port2 == SMSControllerTypes.Phaser)
				{
					byte phaserX = (byte)(_controller.AxisValue("P2 X") + 20);
					int phaserY = _controller.AxisValue("P2 Y");
					int scanline = Vdp.ScanLine;

					if (!LatchLightPhaser2 && phaserY >= scanline - phaserRadius && phaserY <= scanline + phaserRadius)
					{
						if (scanline >= Vdp.FrameHeight)
							return;

						// latch HCounter via TH
						Vdp.HCounter = phaserX;
						LatchLightPhaser2 = true;
					}
					else
					{
						LatchLightPhaser2 = false;
					}
				}
			}
		}

		private byte ReadPort0()
		{
			if (!IsGameGear_C)
			{
				return 0xFF;
			}

			_lagged = false;

			byte value = 0xC0;
			if ((!IsGameGear && _controller.IsPressed("Pause"))
				|| (IsGameGear_C && _controller.IsPressed("P1 Start")))
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
					_controllerDeck.SetPin_c1(_controller, (Port3F & 0x20) != 0);

					// resync
					_controllerDeck.SetCounter_c2(_controller, 3);
				}

				if ((Port3F & 0x08) == 0x00)
				{
					_controllerDeck.SetPin_c2(_controller, (Port3F & 0x80) != 0);

					// resync
					_controllerDeck.SetCounter_c1(_controller, 3);
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
					bool temp = _controllerDeck.GetPin_c1(_controller);
					_controllerDeck.SetPin_c1(_controller, temp ^ true);
				}
				else
				{
					_controllerDeck.SetPin_c1(_controller, false);

					bool temp = _controllerDeck.GetPin_c2(_controller);
					_controllerDeck.SetPin_c2(_controller, temp ^ true);
				}
			}
		}
	}
} 
