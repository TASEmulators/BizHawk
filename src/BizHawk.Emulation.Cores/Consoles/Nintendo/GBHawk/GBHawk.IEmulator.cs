using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IEmulator, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public byte controller_state;
		public ushort Acc_X_state;
		public ushort Acc_Y_state;
		public bool in_vblank_old;
		public bool in_vblank;
		public bool vblank_rise;
		public bool controller_was_checked;
		public bool delays_to_process;
		public bool DIV_falling_edge, DIV_edge_old;
		public int controller_delay_cd;
		public int cpu_state_hold;
		public int clear_counter;
		public long CycleCount;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");

			//Update the color palette if a setting changed
			if (_settings.Palette == GBSettings.PaletteType.BW)
			{
				ppu.color_palette[0] = color_palette_BW[0];
				ppu.color_palette[1] = color_palette_BW[1];
				ppu.color_palette[2] = color_palette_BW[2];
				ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				ppu.color_palette[0] = color_palette_Gr[0];
				ppu.color_palette[1] = color_palette_Gr[1];
				ppu.color_palette[2] = color_palette_Gr[2];
				ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (_tracer.IsEnabled())
			{
				cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				cpu.TraceCallback = null;
			}

			if (controller.IsPressed("P1 Power"))
			{
				HardReset();
			}

			_islag = true;

			controller_was_checked = false;

			do_frame(controller);

			// if the game is halted but controller interrupts are on, check for interrupts
			// if the game is stopped, any button press will un-stop even if interrupts are off
			if ((cpu.stopped && !controller_was_checked) || (cpu.halted && ((REG_FFFF & 0x10) == 0x10)))
			{
				// update the controller state on VBlank
				GetControllerState(controller);

				do_controller_check();
			}

			if (_scanlineCallback != null)
			{
				if (_scanlineCallbackLine == -1)
				{
					_scanlineCallback(ppu.LCDC);
				}
			}

			if (_islag)
			{
				_lagcount++;
			}

			_frame++;

			return true;
		}

		public void do_frame(IController controller)
		{
			for (int i = 0; i < 70224; i++)
			{
				// These things do not change speed in GBC double speed mode
				audio.tick();
				ppu.tick();
				if (Use_MT) { mapper.Mapper_Tick(); }

				// These things all tick twice as fast in GBC double speed mode
				// Note that DMA is halted when the CPU is halted

				if (double_speed)
				{
					if (ppu.DMA_start && !cpu.halted && !cpu.stopped) { ppu.DMA_tick(); }
					serialport.serial_transfer_tick();

					// check state before changes from cpu writes
					DIV_edge_old = (timer.divider_reg & 0x2000) == 0x2000;

					timer.tick();
					cpu.ExecuteOne(_settings.UseRGBDSSyntax);
					timer.divider_reg++;

					DIV_falling_edge |= DIV_edge_old & ((timer.divider_reg & 0x2000) == 0);

					if (delays_to_process) { process_delays(); }

					REG_FF0F_OLD = REG_FF0F;
				}

				if (ppu.DMA_start && !cpu.halted && !cpu.stopped) { ppu.DMA_tick(); }
				serialport.serial_transfer_tick();

				// check state before changes from cpu writes
				DIV_edge_old = double_speed ? ((timer.divider_reg & 0x2000) == 0x2000) : ((timer.divider_reg & 0x1000) == 0x1000);

				timer.tick();
				cpu.ExecuteOne(_settings.UseRGBDSSyntax);
				timer.divider_reg++;

				DIV_falling_edge |= DIV_edge_old & (double_speed ? ((timer.divider_reg & 0x2000) == 0) : ((timer.divider_reg & 0x1000) == 0));

				if (delays_to_process) { process_delays(); }

				CycleCount++;

				if (in_vblank && !in_vblank_old)
				{
					_islag = false;

					controller_was_checked = true;

					// update the controller state on VBlank
					GetControllerState(controller);

					// check if controller state caused interrupt
					do_controller_check();

					// send the image on VBlank
					SendVideoBuffer();

					if (_settings.VBL_sync)
					{
						for (int j = 0; j < 0x8000; j++) { RAM_vbls[j] = RAM[j]; }
						for (int j = 0; j < 0x4000; j++) { VRAM_vbls[j] = VRAM[j]; }
						for (int j = 0; j < 0x80; j++) { ZP_RAM_vbls[j] = ZP_RAM[j]; }
						for (int j = 0; j < 0xA0; j++) { OAM_vbls[j] = OAM[j]; }

						if (cart_RAM != null)
						{
							for (int j = 0; j < cart_RAM.Length; j++) { cart_RAM_vbls[j] = cart_RAM[j]; }
						}
					}
				}

				REG_FF0F_OLD = REG_FF0F;

				in_vblank_old = in_vblank;
			}

			// turn off the screen so the image doesnt persist
			// but don't turn off blank_frame yet, it still needs to be true until the next VBL
			// GBC games need to clear slow enough that games that turn the screen off briefly for cinematics still look smooth
			if (ppu.clear_screen)
			{
				clear_screen_func();
			}
		}

		public void do_single_step()
		{
			// These things do not change speed in GBC double spped mode
			audio.tick();
			ppu.tick();
			if (Use_MT) { mapper.Mapper_Tick(); }

			// These things all tick twice as fast in GBC double speed mode
			// Note that DMA is halted when the CPU is halted
			if (double_speed)
			{
				if (ppu.DMA_start && !cpu.halted && !cpu.stopped) { ppu.DMA_tick(); }
				serialport.serial_transfer_tick();

				// check state before changes from cpu writes
				DIV_edge_old = (timer.divider_reg & 0x2000) == 0x2000;

				timer.tick();
				cpu.ExecuteOne(_settings.UseRGBDSSyntax);
				timer.divider_reg++;

				DIV_falling_edge |= DIV_edge_old & ((timer.divider_reg & 0x2000) == 0);

				if (delays_to_process) { process_delays(); }

				REG_FF0F_OLD = REG_FF0F;
			}

			if (ppu.DMA_start && !cpu.halted && !cpu.stopped) { ppu.DMA_tick(); }
			serialport.serial_transfer_tick();

			// check state before changes from cpu writes
			DIV_edge_old = double_speed ? ((timer.divider_reg & 0x2000) == 0x2000) : ((timer.divider_reg & 0x1000) == 0x1000);

			timer.tick();
			cpu.ExecuteOne(_settings.UseRGBDSSyntax);
			timer.divider_reg++;

			DIV_falling_edge |= DIV_edge_old & (double_speed ? ((timer.divider_reg & 0x2000) == 0) : ((timer.divider_reg & 0x1000) == 0));

			if (delays_to_process) { process_delays(); }

			if (in_vblank && !in_vblank_old)
			{
				vblank_rise = true;
			}

			in_vblank_old = in_vblank;
			REG_FF0F_OLD = REG_FF0F;
		}

		public void do_controller_check()
		{
			// check if new input changed the input register and triggered IRQ
			byte contr_prev = input_register;

			input_register &= 0xF0;
			if ((input_register & 0x30) == 0x20)
			{
				input_register |= (byte)(controller_state & 0xF);
			}
			else if ((input_register & 0x30) == 0x10)
			{
				input_register |= (byte)((controller_state & 0xF0) >> 4);
			}
			else if ((input_register & 0x30) == 0x00)
			{
				// if both polls are set, then a bit is zero if either or both pins are zero
				byte temp = (byte)((controller_state & 0xF) & ((controller_state & 0xF0) >> 4));
				input_register |= temp;
			}
			else
			{
				input_register |= 0xF;
			}

			// check for interrupts
			if (((contr_prev & 0b1000) is not 0 && (input_register & 0b1000) is 0)
				|| ((contr_prev & 0b100) is not 0 && (input_register & 0b100) is 0)
				|| ((contr_prev & 0b10) is not 0 && (input_register & 0b10) is 0)
				|| ((contr_prev & 0b1) is not 0 && (input_register & 0b1) is 0))
			{
				if (REG_FFFF.Bit(4)) { cpu.FlagI = true; }
				REG_FF0F |= 0x10;
			}
		}

		public void HDMA_start_stop(bool hdma_start)
		{
			// put the cpu into a wait state when HDMA starts
			// restore it when HDMA ends
			HDMA_transfer = hdma_start;

			if (hdma_start)
			{
				cpu_state_hold = cpu.instr_pntr;
				cpu.instr_pntr = 256 * 60 * 2 + 60 * 8;
			}
			else
			{
				cpu.instr_pntr = cpu_state_hold;
			}	
		}

		public void process_delays()
		{
			// triggering an interrupt with a write to the control register takes 4 cycles to trigger interrupt
			controller_delay_cd--;
			if (controller_delay_cd == 0)
			{
				if (REG_FFFF.Bit(4)) { cpu.FlagI = true; }
				REG_FF0F |= 0x10;
				delays_to_process = false;
			}
		}

		// Switch Speed (GBC only)
		public int SpeedFunc(int temp)
		{
			if (temp == 0)
			{
				if (is_GBC)
				{
					if (speed_switch)
					{
						speed_switch = false;
						Console.WriteLine("Speed Switch: " + cpu.TotalExecutedCycles);

						int ret = double_speed ? 32770 : 32770; // actual time needs checking
						return ret;
					}

					// if we are not switching speed, return 0
					return 0;
				}

				// if we are in GB mode, return 0, cannot switch speed
				return 0;
			}
			else if (temp == 1)
			{
				// reset the divider (only way for speed_change_timing_fine.gbc and speed_change_cancel.gbc to both work)
				// Console.WriteLine("at stop " + timer.divider_reg + " " + timer.timer_control);

				// only if the timer mode is 1, an extra tick of the timer is counted before the reset
				// this varies between console revisions
				if ((timer.timer_control & 7) == 5)
				{
					if ((timer.divider_reg & 0x7) == 7)
					{
						timer.old_state = true;
					}
				}

				timer.divider_reg = 0xFFFF;

				double_speed = !double_speed;

				ppu.LYC_offset = double_speed ? 1 : 2;

				ppu.LY_153_change = double_speed ? 8 : 10;

				return 0;
			}
			else
			{

				return 0;
			}
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			controller_state = _controllerDeck.ReadPort1(controller);
			(Acc_X_state, Acc_Y_state) = _controllerDeck.ReadAcc1(controller);
		}

		public byte GetButtons(ushort r)
		{
			return input_register;
		}

		public byte GetIntRegs(ushort r)
		{
			if (r==0)
			{
				return REG_FF0F;
			}
			else
			{
				return REG_FFFF;
			}
		}

		public void clear_screen_func()
		{
			if (is_GBC)
			{
				for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = frame_buffer[j] | (0x30303 << (clear_counter * 2)); }

				clear_counter++;
				if (clear_counter == 4)
				{
					ppu.clear_screen = false;
				}
			}
			else
			{
				for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = (int)ppu.color_palette[0]; }
				ppu.clear_screen = false;
			}
		}

		public void SetIntRegs(byte r)
		{
			// For timer interrupts or serial interrupts that occur on the same cycle as the IRQ clear
			// the clear wins on GB and GBA (tested on GBP.) Assuming true for GBC E too.
			// but only in single speed
			if (((REG_FF0F & 4) == 4) && ((r & 4) == 0) && timer.IRQ_block && !double_speed) { r |= 4; }
			if (((REG_FF0F & 8) == 8) && ((r & 8) == 0) && serialport.IRQ_block && !double_speed) { r |= 8; }
			REG_FF0F = r;
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.GB;

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
			audio.DisposeSound();
		}

		public int[] frame_buffer;


		public uint[] vid_buffer;


		public int[] GetVideoBuffer()
		{
			return frame_buffer;
		}

		public void SendVideoBuffer()
		{
			if (GBC_compat)
			{
				if (!ppu.blank_frame)
				{
					for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = (int)vid_buffer[j]; }
				}

				ppu.blank_frame = false;
			}
			else
			{
				if (ppu.blank_frame)
				{
					for (int i = 0; i < vid_buffer.Length; i++)
					{
						vid_buffer[i] = ppu.color_palette[0];
					}
				}

				for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = (int)vid_buffer[j]; }

				ppu.blank_frame = false;
			}
		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 144;
		public int BufferWidth => 160;
		public int BufferHeight => 144;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => 262144;
		public int VsyncDenominator => 4389;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF, 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };
	}
}
