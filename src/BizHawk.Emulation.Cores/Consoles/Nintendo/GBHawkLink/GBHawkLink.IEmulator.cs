using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : IEmulator, IVideoProvider, ISoundProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");
			//Update the color palette if a setting changed
			if (linkSettings.Palette_L == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				L.ppu.color_palette[0] = color_palette_BW[0];
				L.ppu.color_palette[1] = color_palette_BW[1];
				L.ppu.color_palette[2] = color_palette_BW[2];
				L.ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				L.ppu.color_palette[0] = color_palette_Gr[0];
				L.ppu.color_palette[1] = color_palette_Gr[1];
				L.ppu.color_palette[2] = color_palette_Gr[2];
				L.ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (linkSettings.Palette_R == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				R.ppu.color_palette[0] = color_palette_BW[0];
				R.ppu.color_palette[1] = color_palette_BW[1];
				R.ppu.color_palette[2] = color_palette_BW[2];
				R.ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				R.ppu.color_palette[0] = color_palette_Gr[0];
				R.ppu.color_palette[1] = color_palette_Gr[1];
				R.ppu.color_palette[2] = color_palette_Gr[2];
				R.ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (_tracer.IsEnabled())
			{
				L.cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				L.cpu.TraceCallback = null;
			}

			if (controller.IsPressed("P1 Power"))
			{
				L.HardReset();
			}
			if (controller.IsPressed("P2 Power"))
			{
				R.HardReset();
			}

			bool cablediscosignalNew = controller.IsPressed("Toggle Cable");
			if (cablediscosignalNew && !_cablediscosignal)
			{
				_cableconnected ^= true;
				Console.WriteLine("Cable connect status to {0}", _cableconnected);
			}

			_cablediscosignal = cablediscosignalNew;

			_islag = true;

			GetControllerState(controller);

			do_frame_fill = false;
			do_frame();
			if (do_frame_fill)
			{
				FillVideoBuffer();
			}

			_islag = L._islag & R._islag;

			if (_islag)
			{
				_lagcount++;
			}

			_frame++;

			return true;
		}

		public void do_frame()
		{			
			// advance one full frame
			for (int i = 0; i < 70224; i++)
			{
				L.do_single_step();
				R.do_single_step();

				if (_cableconnected)
				{
					// the signal to shift out a bit is when serial_clock = 1
					if (((L.serialport.serial_clock == 1) || (L.serialport.serial_clock == 2)) && (L.serialport.clk_rate > 0) && !do_r_next)
					{
						L.serialport.going_out = (byte)(L.serialport.serial_data >> 7);

						if ((R.serialport.clk_rate == -1) && L.serialport.can_pulse)
						{
							R.serialport.serial_clock = L.serialport.serial_clock;
							R.serialport.going_out = (byte)(R.serialport.serial_data >> 7);
							R.serialport.coming_in = L.serialport.going_out;
						}

						L.serialport.coming_in = R.serialport.going_out;
						L.serialport.can_pulse = false;
					}
					else if (((R.serialport.serial_clock == 1) || (R.serialport.serial_clock == 2)) && (R.serialport.clk_rate > 0))
					{
						do_r_next = false;

						R.serialport.going_out = (byte)(R.serialport.serial_data >> 7);

						if ((L.serialport.clk_rate == -1) && R.serialport.can_pulse)
						{
							L.serialport.serial_clock = R.serialport.serial_clock;
							L.serialport.going_out = (byte)(L.serialport.serial_data >> 7);
							L.serialport.coming_in = R.serialport.going_out;
						}

						R.serialport.coming_in = L.serialport.going_out;
						R.serialport.can_pulse = false;

						if (R.serialport.serial_clock == 2) { do_r_next = true; }
					}
					else
					{
						do_r_next = false;
					}

					// do IR transfer
					if (L.IR_write > 0)
					{
						L.IR_write--;
						if (L.IR_write ==0)
						{
							R.IR_receive = L.IR_signal;
							if ((R.IR_self & R.IR_receive) == 2) { R.IR_reg |= 2; }
							else { R.IR_reg &= 0xFD;}
							if ((L.IR_self & L.IR_receive) == 2) { L.IR_reg |= 2; }
							else { L.IR_reg &= 0xFD; }
						}
					}

					if (R.IR_write > 0)
					{
						R.IR_write--;
						if (R.IR_write == 0)
						{
							L.IR_receive = R.IR_signal;
							if ((L.IR_self & L.IR_receive) == 2) { L.IR_reg |= 2; }
							else { L.IR_reg &= 0xFD; }
							if ((R.IR_self & R.IR_receive) == 2) { R.IR_reg |= 2; }
							else { R.IR_reg &= 0xFD; }
						}
					}
				}

				// if we hit a frame boundary, update video
				if (L.vblank_rise)
				{
					// update the controller state on VBlank
					L.controller_state = L_controller;

					// check if controller state caused interrupt
					L.do_controller_check();

					// send the image on VBlank
					L.SendVideoBuffer();

					L.vblank_rise = false;
					do_frame_fill = true;
				}

				if (R.vblank_rise)
				{
					// update the controller state on VBlank
					R.controller_state = R_controller;

					// check if controller state caused interrupt
					R.do_controller_check();

					// send the image on VBlank
					R.SendVideoBuffer();

					R.vblank_rise = false;
					do_frame_fill = true;
				}
			}

			// clear the screens as needed
			if (L.ppu.clear_screen)
			{
				L.clear_screen_func();
				do_frame_fill = true;
			}

			if (R.ppu.clear_screen)
			{
				R.clear_screen_func();
				do_frame_fill = true;
			}
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			L_controller = _controllerDeck.ReadPort1(controller);
			R_controller = _controllerDeck.ReadPort2(controller);
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.GBL;

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
			L.Dispose();
			R.Dispose();
		}

		public int[] _vidbuffer = new int[160 * 2 * 144];

		public int[] GetVideoBuffer() => _vidbuffer;

		public void FillVideoBuffer()
		{
			if (linkSettings.VideoSet == GBLinkSettings.VideoSrc.Both)
			{
				// combine the 2 video buffers from the instances
				for (int i = 0; i < 144; i++)
				{
					for (int j = 0; j < 160; j++)
					{
						_vidbuffer[i * 320 + j] = L.frame_buffer[i * 160 + j];
						_vidbuffer[i * 320 + j + 160] = R.frame_buffer[i * 160 + j];
					}
				}
			}
			else if (linkSettings.VideoSet == GBLinkSettings.VideoSrc.Left)
			{
				// combine the 2 video buffers from the instances
				for (int i = 0; i < 144; i++)
				{
					for (int j = 0; j < 160; j++)
					{
						_vidbuffer[i * 160 + j] = L.frame_buffer[i * 160 + j];
					}
				}
			}
			else
			{
				// combine the 2 video buffers from the instances
				for (int i = 0; i < 144; i++)
				{
					for (int j = 0; j < 160; j++)
					{
						_vidbuffer[i * 160 + j] = R.frame_buffer[i * 160 + j];
					}
				}
			}
		}

		public int VirtualWidth => (linkSettings.VideoSet == GBLinkSettings.VideoSrc.Both) ? 160 * 2 : 160;
		public int VirtualHeight => 144;
		public int BufferWidth => (linkSettings.VideoSet == GBLinkSettings.VideoSrc.Both) ? 160 * 2 : 160;
		public int BufferHeight => 144;

		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => 262144;
		public int VsyncDenominator => 4389;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported_");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			L.audio.GetSamplesSync(out var tempSampL, out var nsampL);
			R.audio.GetSamplesSync(out var tempSampR, out var nsampR);

			if (linkSettings.AudioSet == GBLinkSettings.AudioSrc.Left)
			{
				samples = tempSampL;
				nsamp = nsampL;
			}
			else if (linkSettings.AudioSet == GBLinkSettings.AudioSrc.Right)
			{
				samples = tempSampR;
				nsamp = nsampR;
			}
			else
			{
				samples = new short[0];
				nsamp = 0;
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			L.audio.DiscardSamples();
			R.audio.DiscardSamples();
		}

		public void DisposeSound()
		{
			L.audio.DisposeSound();
			R.audio.DisposeSound();
		}
	}
}
