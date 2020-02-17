using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : IEmulator, ISoundProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition
		{
			get
			{
				if (IsGameGear_C)
				{
					return GGController;
				}

				// Sorta a hack but why not
				PortDEEnabled = SyncSettings.ControllerType == SmsSyncSettings.ControllerTypes.Keyboard;

				switch(SyncSettings.ControllerType)
				{
					case SmsSyncSettings.ControllerTypes.Paddle:
						return SMSPaddleController;
					case SmsSyncSettings.ControllerTypes.LightPhaser:
						// scale the vertical to the display mode
						SMSLightPhaserController.FloatRanges[1] = new ControllerDefinition.FloatRange(0, Vdp.FrameHeight / 2, Vdp.FrameHeight - 1);

						return SMSLightPhaserController;
					case SmsSyncSettings.ControllerTypes.SportsPad:
						return SMSSportsPadController;
					case SmsSyncSettings.ControllerTypes.Keyboard:
						return SMSKeyboardController;
					default:
						return SmsController;
				}
			}
		}

		// not savestated variables
		int s_L, s_R;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;
			_lagged = true;
			_frame++;

			if (!IsGameGear)
			{
				PSG.Set_Panning(Settings.ForceStereoSeparation ? ForceStereoByte : (byte)0xFF);
			}

			if (Tracer.Enabled)
			{
				Cpu.TraceCallback = s => Tracer.Put(s);
			}
			else
			{
				Cpu.TraceCallback = null;
			}

			if (IsGameGear_C == false)
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

					if (s_L != old_s_L)
					{
						blip_L.AddDelta(sampleclock, s_L - old_s_L);
						old_s_L = s_L;
					}

					if (s_R != old_s_R)
					{
						blip_R.AddDelta(sampleclock, s_R - old_s_R);
						old_s_R = s_R;
					}

					sampleclock++;
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

			return true;
		}

		// Used for GG linked play
		public void FrameAdvancePrep()
		{
			_lagged = true;
			_frame++;

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
		}

		public int Frame => _frame;

		public string SystemId => "SMS";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			if (blip_L != null)
			{
				blip_L.Dispose();
				blip_L = null;
			}

			if (blip_R != null)
			{
				blip_R.Dispose();
				blip_R = null;
			}
		}

		#region Audio

		public BlipBuffer blip_L = new BlipBuffer(4096);
		public BlipBuffer blip_R = new BlipBuffer(4096);
		const int blipbuffsize = 4096;

		public uint sampleclock;
		public int old_s_L = 0;
		public int old_s_R = 0;

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new NotSupportedException("Only sync mode is supported");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async not supported");
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (!disablePSG)
			{
				blip_L.EndFrame(sampleclock);
				blip_R.EndFrame(sampleclock);

				nsamp = Math.Max(Math.Max(blip_L.SamplesAvailable(), blip_R.SamplesAvailable()), 1);
				samples = new short[nsamp * 2];

				blip_L.ReadSamplesLeft(samples, nsamp);
				blip_R.ReadSamplesRight(samples, nsamp);

				ApplyYMAudio(samples);
			}
			else
			{
				nsamp = 735;
				samples = new short[nsamp * 2];
				ApplyYMAudio(samples);
			}

			sampleclock = 0;
		}

		public void DiscardSamples()
		{
			blip_L.Clear();
			blip_R.Clear();
			sampleclock = 0;
		}

		public void ApplyYMAudio(short[] samples)
		{
			if (HasYM2413)
			{
				short[] fmsamples = new short[samples.Length];
				YM2413.GetSamples(fmsamples);
				//naive mixing. need to study more
				int len = samples.Length;
				for (int i = 0; i < len; i++)
				{
					short fmsamp = fmsamples[i];
					samples[i] = (short)(samples[i] + fmsamp);
				}
			}
		}

		#endregion

	}
}
