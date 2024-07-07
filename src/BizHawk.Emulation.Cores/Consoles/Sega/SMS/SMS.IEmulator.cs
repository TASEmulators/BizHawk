using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		// not savestated variables
		private int s_L, s_R;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;
			_lagged = true;

			if (!IsGameGear)
			{
				PSG.Set_Panning(Settings.ForceStereoSeparation ? ForceStereoByte : (byte)0xFF);
			}

			if (Tracer.IsEnabled())
			{
				Cpu.TraceCallback = s => Tracer.Put(s);
			}
			else
			{
				Cpu.TraceCallback = null;
			}

			if (!IsGameGear_C)
			{
				Cpu.NonMaskableInterrupt = controller.IsPressed("Pause");
			}
			else if (!IsGameGear && IsGameGear_C)
			{
				Cpu.NonMaskableInterrupt = controller.IsPressed("P1 Start");
			}

			if (IsGame3D && Settings.Fix3D)
			{
				render = ((Frame & 1) == 0) & render;
			}

			int scanlinesPerFrame = Vdp.DisplayType == DisplayType.NTSC ? 262 : 313;
			Vdp.SpriteLimit = Settings.SpriteLimit;
			for (int i = 0; i < scanlinesPerFrame; i++)
			{
				Vdp.ScanLine = i;

				Vdp.RenderCurrentScanline(render);

				Vdp.ProcessFrameInterrupt();
				Vdp.ProcessLineInterrupt();
				ProcessLineControls();

				for (int j = 0; j < Vdp.IPeriod; j++)
				{
					Cpu.ExecuteOne();

					PSG.generate_sound();

					s_L = PSG.current_sample_L;
					s_R = PSG.current_sample_R;

					if (s_L != OldSl)
					{
						BlipL.AddDelta(SampleClock, s_L - OldSl);
						OldSl = s_L;
					}

					if (s_R != OldSr)
					{
						BlipR.AddDelta(SampleClock, s_R - OldSr);
						OldSr = s_R;
					}

					SampleClock++;
				}

				if (Vdp.ScanLine == scanlinesPerFrame - 1)
				{
					Vdp.ProcessGGScreen();
					Vdp.ProcessOverscan();
				}
			}

			if (_lagged)
			{
				_lagCount++;
				_isLag = true;
			}
			else
			{
				_isLag = false;
			}

			_frame++;

			return true;
		}

		// Used for GG linked play
		public void FrameAdvancePrep()
		{
			_lagged = true;

			if (!IsGameGear && IsGameGear_C)
			{
				Cpu.NonMaskableInterrupt = start_pressed;
			}
		}

		// Used for GG linked play
		public void FrameAdvancePost()
		{
			if (_lagged)
			{
				_lagCount++;
				_isLag = true;
			}
			else
			{
				_isLag = false;
			}

			_frame++;
		}

		public int Frame => _frame;

		public string SystemId { get; }

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose()
		{
			if (BlipL != null)
			{
				BlipL.Dispose();
				BlipL = null;
			}

			if (BlipR != null)
			{
				BlipR.Dispose();
				BlipR = null;
			}
		}
	}
}
