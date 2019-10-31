using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	public partial class O2Hawk : IEmulator, IVideoProvider
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
			for (int i = 0; i < 70224; i++)
			{
				audio.tick();
				ppu.tick();
				ppu.DMA_tick();
				serialport.serial_transfer_tick();
				cpu.ExecuteOne();

				if (in_vblank && !in_vblank_old)
				{
					// update the controller state on VBlank
					GetControllerState(controller);

					// check if controller state caused interrupt
					do_controller_check();

					// send the image on VBlank
					SendVideoBuffer();
				}

				in_vblank_old = in_vblank;
			}

			// turn off the screen so the image doesnt persist
			// but dont turn off blank_frame yet, it still needs to be true until the next VBL
			// this doesn't run for GBC, some games, ex MIB the series 2, rely on the screens persistence while off to make video look smooth.
			// But some GB gams, ex Battletoads, turn off the screen for a long time from the middle of the frame, so need to be cleared.
			if (ppu.clear_screen)
			{
				for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = (int)color_palette[0]; }
				ppu.clear_screen = false;
			}
		}

		public void do_single_step()
		{
			audio.tick();
			ppu.tick();
			ppu.DMA_tick();
			serialport.serial_transfer_tick();
			cpu.ExecuteOne();
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
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			controller_state = _controllerDeck.ReadPort1(controller);

			Acc_X_state = _controllerDeck.ReadAccX1(controller);
			Acc_Y_state = _controllerDeck.ReadAccY1(controller);
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

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			audio.DisposeSound();
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
			if (ppu.blank_frame)
			{
				for (int i = 0; i < _vidbuffer.Length; i++)
				{
					_vidbuffer[i] = (int)color_palette[0];
				}
			}

			for (int j = 0; j < frame_buffer.Length; j++) { frame_buffer[j] = _vidbuffer[j]; }

			ppu.blank_frame = false;
		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 144;
		public int BufferWidth => 160;
		public int BufferHeight => 144;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		#endregion
	}
}
