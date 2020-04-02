using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Runtime.InteropServices;

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

			if (_tracer.Enabled)
			{
				cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				cpu.TraceCallback = null;
			}

			_frame++;

			if (controller.IsPressed("P1 Power"))
			{
				HardReset();
			}

			_islag = true;

			do_frame(controller);

			if (_scanlineCallback != null)
			{
				if (_scanlineCallbackLine == -1)
				{
					GetGPU();
					_scanlineCallback(ppu.LCDC);
				}
			}

			if (_islag)
			{
				_lagcount++;
			}

			return true;
		}

		public void do_frame(IController controller)
		{
			for (int i = 0; i < 70224; i++)
			{
				// These things do not change speed in GBC double spped mode
				audio.tick();
				ppu.tick();
				if (Use_MT) { mapper.Mapper_Tick(); }

				if (!HDMA_transfer)
				{
					// These things all tick twice as fast in GBC double speed mode
					// Note that DMA is halted when the CPU is halted
					if (ppu.DMA_start && !cpu.halted) { ppu.DMA_tick(); }
					serialport.serial_transfer_tick();
					cpu.ExecuteOne();
					timer.tick();

					if (double_speed)
					{
						if (ppu.DMA_start && !cpu.halted) { ppu.DMA_tick(); }
						serialport.serial_transfer_tick();
						cpu.ExecuteOne();
						timer.tick();
					}
				}
				else
				{
					cpu.TotalExecutedCycles++;
					timer.tick();					
					if (double_speed)
					{
						cpu.TotalExecutedCycles++;
						timer.tick();						
					}
				}

				if (in_vblank && !in_vblank_old)
				{
					_islag = false;

					// update the controller state on VBlank
					GetControllerState(controller);

					// check if controller state caused interrupt
					do_controller_check();

					// send the image on VBlank
					SendVideoBuffer();
				}

				REG_FF0F_OLD = REG_FF0F;

				in_vblank_old = in_vblank;
			}

			// turn off the screen so the image doesnt persist
			// but don't turn off blank_frame yet, it still needs to be true until the next VBL
			// this doesn't run for GBC, some games, ex MIB the series 2, rely on the screens persistence while off to make video look smooth.
			// But some GB gams, ex Battletoads, turn off the screen for a long time from the middle of the frame, so need to be cleared.
			if (ppu.clear_screen)
			{
				for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = (int)ppu.color_palette[0]; }
				ppu.clear_screen = false;
			}
		}

		public void do_single_step()
		{
			// These things do not change speed in GBC double spped mode
			audio.tick();
			ppu.tick();
			if (Use_MT) { mapper.Mapper_Tick(); }

			if (!HDMA_transfer)
			{
				// These things all tick twice as fast in GBC double speed mode
				if (ppu.DMA_start && !cpu.halted) { ppu.DMA_tick(); }
				serialport.serial_transfer_tick();
				cpu.ExecuteOne();
				timer.tick();

				if (double_speed)
				{
					if (ppu.DMA_start && !cpu.halted) { ppu.DMA_tick(); }
					serialport.serial_transfer_tick();
					cpu.ExecuteOne();
					timer.tick();
				}
			}
			else
			{
				cpu.TotalExecutedCycles++;
				timer.tick();
				
				if (double_speed)
				{
					cpu.TotalExecutedCycles++;
					timer.tick();				
				}
			}

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
			if (((contr_prev & 8) > 0) && ((input_register & 8) == 0) ||
				((contr_prev & 4) > 0) && ((input_register & 4) == 0) ||
				((contr_prev & 2) > 0) && ((input_register & 2) == 0) ||
				((contr_prev & 1) > 0) && ((input_register & 1) == 0))
			{
				if (REG_FFFF.Bit(4)) { cpu.FlagI = true; }
				REG_FF0F |= 0x10;
			}
		}

		// Switch Speed (GBC only)
		public int SpeedFunc(int temp)
		{
			if (is_GBC)
			{
				if (speed_switch)
				{
					speed_switch = false;
					Console.WriteLine("Speed Switch: " + cpu.TotalExecutedCycles);
					int ret = double_speed ? 35112 : 35112; // actual time needs checking
					double_speed = !double_speed;
					return ret;
				}

				// if we are not switching speed, return 0
				return 0;
			}

			// if we are in GB mode, return 0 indicating not switching speed
			return 0;
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			controller_state = _controllerDeck.ReadPort1(controller);

			Acc_X_state = _controllerDeck.ReadAccX1(controller);
			Acc_Y_state = _controllerDeck.ReadAccY1(controller);
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

		public void SetIntRegs(byte r)
		{
			REG_FF0F = r;
		}

		public int Frame => _frame;

		public string SystemId => "GB";

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
			Marshal.FreeHGlobal(iptr0);
			Marshal.FreeHGlobal(iptr1);
			Marshal.FreeHGlobal(iptr2);
			Marshal.FreeHGlobal(iptr3);

			audio.DisposeSound();
		}

		#region Video provider

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

		#endregion
	}
}
