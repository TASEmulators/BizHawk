using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubGBHawk
{
	public partial class SubGBHawk : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _GBCore.ControllerDefinition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");
			//Update the color palette if a setting changed
			if (GetSettings().Palette == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				_GBCore.ppu.color_palette[0] = GBHawk.GBHawk.color_palette_BW[0];
				_GBCore.ppu.color_palette[1] = GBHawk.GBHawk.color_palette_BW[1];
				_GBCore.ppu.color_palette[2] = GBHawk.GBHawk.color_palette_BW[2];
				_GBCore.ppu.color_palette[3] = GBHawk.GBHawk.color_palette_BW[3];
			}
			else
			{
				_GBCore.ppu.color_palette[0] = GBHawk.GBHawk.color_palette_Gr[0];
				_GBCore.ppu.color_palette[1] = GBHawk.GBHawk.color_palette_Gr[1];
				_GBCore.ppu.color_palette[2] = GBHawk.GBHawk.color_palette_Gr[2];
				_GBCore.ppu.color_palette[3] = GBHawk.GBHawk.color_palette_Gr[3];
			}
			if (_tracer.IsEnabled())
			{
				_GBCore.cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_GBCore.cpu.TraceCallback = null;
			}

			reset_frame = false;
			if (controller.IsPressed("P1 Power"))
			{
				reset_frame = true;
			}

			input_frame_length = controller.AxisValue("Input Cycle");
			input_frame_length_int = (int)Math.Floor(input_frame_length);

			if (input_frame_length_int == 0)
			{
				input_frame_length_int = 70224;
			}

			pass_a_frame = false;
			_GBCore._islag = false;

			InputCallbacks.Call();

			DoFrame(controller);

			bool ret = pass_a_frame;

			if (pass_a_frame) 
			{
				// clear the screen as needed
				if (_GBCore.ppu.clear_screen)
				{
					_GBCore.clear_screen_func();
				}

				// reset the frame cycle counter
				frame_cycle = 0; 
			}
			current_cycle = 0;
			
			_isLag = _GBCore._islag;

			if (_isLag)
			{
				_lagCount++;
			}

			reset_frame = false;

			_frame++;

			return ret;
		}

		private bool stop_cur_frame;
		private bool pass_new_input;
		private bool pass_a_frame;
		private bool reset_frame;
		private int current_cycle;
		private int frame_cycle;
		private float input_frame_length;
		private int input_frame_length_int;

		private void DoFrame(IController controller)
		{
			stop_cur_frame = false;
			_GBCore.GetControllerState(controller);
			_GBCore.do_controller_check();
			while (!stop_cur_frame)
			{
				_GBCore.do_single_step();

				if (reset_frame)
				{
					HardReset();
					reset_frame = false;
					stop_cur_frame |= true;
					pass_a_frame |= true;
				}

				current_cycle++;
				frame_cycle++;
				_cycleCount++;

				if (frame_cycle == 70224)
				{
					stop_cur_frame |= true;
					pass_a_frame |= true;
				}

				if (current_cycle == input_frame_length_int)
				{
					stop_cur_frame |= true;
				}

				if (_GBCore.vblank_rise)
				{
					_GBCore.SendVideoBuffer();
					_GBCore.vblank_rise = false;
				}
			}
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.GB;

		public bool DeterministicEmulation => _GBCore.DeterministicEmulation;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose() => _GBCore.Dispose();
	}
}
