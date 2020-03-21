using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IEmulator, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public byte controller_state_1, controller_state_2, kb_state_row, kb_state_col;
		public bool in_vblank_old;
		public bool in_vblank;
		public bool vblank_rise;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");

			if (_tracer.Enabled)
			{
				cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				cpu.TraceCallback = null;
			}

			_frame++;

			if (controller.IsPressed("Power"))
			{
				HardReset();
			}

			_islag = true;

			do_frame(controller);

			if (_islag)
			{
				_lagcount++;
			}

			return true;
		}

		public void do_frame(IController controller)
		{
			// update the controller state on VBlank
			GetControllerState(controller);

			// send the image on VBlank
			SendVideoBuffer();

			bool frame_chk = true;

			if (is_pal)
			{
				// PAL timing is: 17.7 / 5 ppu
				// and 17.7 / 9 for cpu (divide by 3 externally then by 3 again internally)
				int ticker = 0;
				
				while (frame_chk)
				{
					ticker++;

					if ((ticker % 5) == 0)
					{
						ppu.tick();

						if ((ticker % 10) == 0)
						{
							ppu.Audio_tick();
						}
					}

					if ((ticker % 9) == 0)
					{
						serialport.serial_transfer_tick();
						cpu.ExecuteOne();
					}

					if (!in_vblank && in_vblank_old)
					{
						frame_chk = false;
					}

					in_vblank_old = in_vblank;
				}
			}
			else
			{
				// NTSC is 2 to 1 ppu to cpu ticks
				while (frame_chk)
				{
					ppu.tick();
					ppu.tick();
					serialport.serial_transfer_tick();
					ppu.Audio_tick();
					cpu.ExecuteOne();

					if (!in_vblank && in_vblank_old)
					{
						frame_chk = false;
					}

					in_vblank_old = in_vblank;
				}
			}
		}

		public void do_single_step()
		{
			ppu.tick();
			ppu.tick();
			serialport.serial_transfer_tick();
			ppu.Audio_tick();
			cpu.ExecuteOne();
			
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			controller_state_1 = _controllerDeck.ReadPort1(controller);
			controller_state_2 = _controllerDeck.ReadPort2(controller);

			kb_state_row = 8; // nothing pressed
			if (controller.IsPressed("0")) { kb_state_row = 0; kb_state_col = 7; }
			if (controller.IsPressed("1")) { kb_state_row = 0; kb_state_col = 6; }
			if (controller.IsPressed("2")) { kb_state_row = 0; kb_state_col = 5; }
			if (controller.IsPressed("3")) { kb_state_row = 0; kb_state_col = 4; }
			if (controller.IsPressed("4")) { kb_state_row = 0; kb_state_col = 3; }
			if (controller.IsPressed("5")) { kb_state_row = 0; kb_state_col = 2; }
			if (controller.IsPressed("6")) { kb_state_row = 0; kb_state_col = 1; }
			if (controller.IsPressed("7")) { kb_state_row = 0; kb_state_col = 0; }
			if (controller.IsPressed("8")) { kb_state_row = 1; kb_state_col = 7; }
			if (controller.IsPressed("9")) { kb_state_row = 1; kb_state_col = 6; }
			if (controller.IsPressed("SPC")) { kb_state_row = 1; kb_state_col = 3; }
			if (controller.IsPressed("?")) { kb_state_row = 1; kb_state_col = 2; }
			if (controller.IsPressed("L")) { kb_state_row = 1; kb_state_col = 1; }
			if (controller.IsPressed("P")) { kb_state_row = 1; kb_state_col = 0; }
			if (controller.IsPressed("+")) { kb_state_row = 2; kb_state_col = 7; }
			if (controller.IsPressed("W")) { kb_state_row = 2; kb_state_col = 6; }
			if (controller.IsPressed("E")) { kb_state_row = 2; kb_state_col = 5; }
			if (controller.IsPressed("R")) { kb_state_row = 2; kb_state_col = 4; }
			if (controller.IsPressed("T")) { kb_state_row = 2; kb_state_col = 3; }
			if (controller.IsPressed("U")) { kb_state_row = 2; kb_state_col = 2; }
			if (controller.IsPressed("I")) { kb_state_row = 2; kb_state_col = 1; }
			if (controller.IsPressed("O")) { kb_state_row = 2; kb_state_col = 0; }
			if (controller.IsPressed("Q")) { kb_state_row = 3; kb_state_col = 7; }
			if (controller.IsPressed("S")) { kb_state_row = 3; kb_state_col = 6; }
			if (controller.IsPressed("D")) { kb_state_row = 3; kb_state_col = 5; }
			if (controller.IsPressed("F")) { kb_state_row = 3; kb_state_col = 4; }
			if (controller.IsPressed("G")) { kb_state_row = 3; kb_state_col = 3; }
			if (controller.IsPressed("H")) { kb_state_row = 3; kb_state_col = 2; }
			if (controller.IsPressed("J")) { kb_state_row = 3; kb_state_col = 1; }
			if (controller.IsPressed("K")) { kb_state_row = 3; kb_state_col = 0; }
			if (controller.IsPressed("A")) { kb_state_row = 4; kb_state_col = 7; }
			if (controller.IsPressed("Z")) { kb_state_row = 4; kb_state_col = 6; }
			if (controller.IsPressed("X")) { kb_state_row = 4; kb_state_col = 5; }
			if (controller.IsPressed("C")) { kb_state_row = 4; kb_state_col = 4; }
			if (controller.IsPressed("V")) { kb_state_row = 4; kb_state_col = 3; }
			if (controller.IsPressed("B")) { kb_state_row = 4; kb_state_col = 2; }
			if (controller.IsPressed("M")) { kb_state_row = 4; kb_state_col = 1; }
			if (controller.IsPressed(".")) { kb_state_row = 4; kb_state_col = 0; }
			if (controller.IsPressed("-")) { kb_state_row = 5; kb_state_col = 7; }
			if (controller.IsPressed("*")) { kb_state_row = 5; kb_state_col = 6; }
			if (controller.IsPressed("/")) { kb_state_row = 5; kb_state_col = 5; }
			if (controller.IsPressed("=")) { kb_state_row = 5; kb_state_col = 4; }
			if (controller.IsPressed("YES")) { kb_state_row = 5; kb_state_col = 3; }
			if (controller.IsPressed("NO")) { kb_state_row = 5; kb_state_col = 2; }
			if (controller.IsPressed("CLR")) { kb_state_row = 5; kb_state_col = 1; }
			if (controller.IsPressed("ENT")) { kb_state_row = 5; kb_state_col = 0; }

		}

		public void KB_Scan()
		{
			if (kb_byte == kb_state_row)
			{
				kb_byte &= 0xEF;
				kb_byte |= (byte)(kb_state_col << 5);
			}
			else
			{
				kb_byte |= 0x10;
			}
		}

		public int Frame => _frame;

		public string SystemId => "O2";

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
			ppu.DisposeSound();
		}

		#region Video provider

		public int _frameHz = 60;

		public int[] _vidbuffer;

		public int[] frame_buffer;

		public int[] GetVideoBuffer()
		{
			return frame_buffer;
		}

		public void SendVideoBuffer()
		{
			for (int j = 0; j < pic_height; j++) 
			{
				for (int i = 0; i < 320; i++)
				{
					frame_buffer[j * 320 + i] = _vidbuffer[j * 372 + i];
					_vidbuffer[j * 372 + i] = 0;
				}

				for (int k = 320; k < 372; k++)
				{
					_vidbuffer[j * 372 + k] = 0;
				}
			}
		}

		public int pic_height;

		public int VirtualWidth => 320;
		public int VirtualHeight => pic_height;
		public int BufferWidth => 320;
		public int BufferHeight => pic_height;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		#endregion
	}
}
