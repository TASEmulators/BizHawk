using System;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IEmulator, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public byte controller_state;
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

			// check if controller state caused interrupt
			do_controller_check();

			// send the image on VBlank
			SendVideoBuffer();

			bool frame_chk = true;

			while (frame_chk)
			{
				ppu.tick();
				ppu.tick();
				ppu.DMA_tick();
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

		public void do_single_step()
		{
			ppu.tick();
			ppu.tick();
			ppu.DMA_tick();
			serialport.serial_transfer_tick();
			ppu.Audio_tick();
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
			for (int j = 0; j < frame_buffer.Length; j++) 
			{ 
				frame_buffer[j] = _vidbuffer[j];
				_vidbuffer[j] = 0;
			}
		}

		public int VirtualWidth => 186;
		public int VirtualHeight => 240;
		public int BufferWidth => 186;
		public int BufferHeight => 240;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		#endregion
	}
}
