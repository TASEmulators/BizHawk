using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : IEmulator, IVideoProvider, ISoundProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public int L_NMI_CD, R_NMI_CD;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");
			if (_tracer.IsEnabled())
			{
				L.Cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				L.Cpu.TraceCallback = null;
			}

			if (controller.IsPressed("Power"))
			{
				HardReset();
			}

			bool cableDiscoSignalNew = controller.IsPressed("Toggle Cable");
			if (cableDiscoSignalNew && !_cablediscosignal)
			{
				_cableconnected ^= true;
				Console.WriteLine("Cable connect status to {0}", _cableconnected);
			}

			_cablediscosignal = cableDiscoSignalNew;

			_isLag = true;

			GetControllerState(controller);

			DoFrame(controller, render, renderSound);

			_isLag = L._isLag;

			if (_isLag)
			{
				_lagCount++;
			}

			_frame++;

			return true;
		}

		private void DoFrame(IController controller, bool render, bool renderSound)
		{
			L.start_pressed = controller.IsPressed("P1 Start");
			R.start_pressed = controller.IsPressed("P2 Start");

			L.FrameAdvancePrep();
			R.FrameAdvancePrep();

			if (!_cableconnected)
			{
				if ((L.Port05 & 0x38) == 0x38)
				{
					L.Port05 |= 4;
					L.Cpu.NonMaskableInterrupt = true;
				}

				if ((R.Port05 & 0x38) == 0x38)
				{
					R.Port05 |= 4;
					R.Cpu.NonMaskableInterrupt = true;
				}
			}
			else
			{
				if ((L.Port05 & 0x38) == 0x38)
				{
					L.Port05 &= 0xFB;
				}

				if ((R.Port05 & 0x38) == 0x38)
				{
					R.Port05 &= 0xFB;
				}
			}

			int scanlinesPerFrame = 262;

			L.Vdp.ScanLine = 0;
			R.Vdp.ScanLine = 0;

			for (int S = 0; S < scanlinesPerFrame; S++)
			{
				L.Vdp.RenderCurrentScanline(render);
				R.Vdp.RenderCurrentScanline(render);

				L.Vdp.ProcessFrameInterrupt();
				R.Vdp.ProcessFrameInterrupt();

				L.Vdp.ProcessLineInterrupt();
				R.Vdp.ProcessLineInterrupt();

				// 512 cycles per line
				for (int j = 0; j < 228; j++)
				{
					L.Cpu.ExecuteOne();
					R.Cpu.ExecuteOne();

					// linking code
					if (L.p3_write)
					{
						L.p3_write = false;

						if (((L.Port05 & 0x38) == 0x38) && _cableconnected)
						{
							L.Port05 |= 1;

							if ((R.Port05 & 0x38) == 0x38)
							{
								R.Port05 |= 2;
								R.Port04 = L.Port03;
								R_NMI_CD = 256;
								//R.Cpu.NonMaskableInterrupt = true;
							}
						}					
					}

					if (L.p4_read)
					{
						L.p4_read = false;
						L.Cpu.NonMaskableInterrupt = false;

						if (((L.Port05 & 0x38) == 0x38) && _cableconnected)
						{
							L.Port05 &= 0xFD;

							if ((R.Port05 & 0x38) == 0x38)
							{
								R.Port05 &= 0xFE;
							}
						}
					}

					if (R.p3_write)
					{
						R.p3_write = false;

						if (((R.Port05 & 0x38) == 0x38) && _cableconnected)
						{
							R.Port05 |= 1;

							if ((L.Port05 & 0x38) == 0x38)
							{
								L.Port05 |= 2;
								L.Port04 = R.Port03;
								L_NMI_CD = 256;
								//L.Cpu.NonMaskableInterrupt = true;
							}
						}
					}

					if (R.p4_read)
					{
						R.p4_read = false;
						R.Cpu.NonMaskableInterrupt = false;

						if (((R.Port05 & 0x38) == 0x38) && _cableconnected)
						{
							R.Port05 &= 0xFD;

							if ((L.Port05 & 0x38) == 0x38)
							{
								L.Port05 &= 0xFE;
							}
						}
					}

					if (L_NMI_CD > 0)
					{
						L_NMI_CD--;
						if (L_NMI_CD == 0)
						{
							L.Cpu.NonMaskableInterrupt = true;
						}
					}

					if (R_NMI_CD > 0)
					{
						R_NMI_CD--;
						if (R_NMI_CD == 0)
						{
							R.Cpu.NonMaskableInterrupt = true;
						}
					}

					L.PSG.generate_sound();
					R.PSG.generate_sound();

					int s_L = L.PSG.current_sample_L;
					int s_R = L.PSG.current_sample_R;

					if (s_L != L.OldSl)
					{
						L.BlipL.AddDelta(L.SampleClock, s_L - L.OldSl);
						L.OldSl = s_L;
					}

					if (s_R != L.OldSr)
					{
						L.BlipR.AddDelta(L.SampleClock, s_R - L.OldSr);
						L.OldSr = s_R;
					}

					L.SampleClock++;

					s_L = R.PSG.current_sample_L;
					s_R = R.PSG.current_sample_R;

					if (s_L != R.OldSl)
					{
						R.BlipL.AddDelta(R.SampleClock, s_L - R.OldSl);
						R.OldSl = s_L;
					}

					if (s_R != R.OldSr)
					{
						R.BlipR.AddDelta(R.SampleClock, s_R - R.OldSr);
						R.OldSr = s_R;
					}

					R.SampleClock++;
				}

				if (S == scanlinesPerFrame - 1)
				{
					L.Vdp.ProcessGGScreen();
					R.Vdp.ProcessGGScreen();

					L.Vdp.ProcessOverscan();
					R.Vdp.ProcessOverscan();
				}

				L.Vdp.ScanLine++;
				R.Vdp.ScanLine++;
			}

			L.FrameAdvancePost();
			R.FrameAdvancePost();

			buff_L = L.Vdp.GetVideoBuffer();
			buff_R = R.Vdp.GetVideoBuffer();

			FillVideoBuffer();
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			L.cntr_rd_0 = (byte)(controller.IsPressed("P1 Start") ? 0x7F : 0xFF);
			L.cntr_rd_1 = _controllerDeck.ReadPort1(controller);
			L.cntr_rd_2 = 0xFF;
			R.cntr_rd_0 = (byte)(controller.IsPressed("P2 Start") ? 0x7F : 0xFF);
			R.cntr_rd_1 = _controllerDeck.ReadPort2(controller);
			R.cntr_rd_2 = 0xFF;
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.GGL;

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose()
		{
			L.Dispose();
			R.Dispose();
		}

		public int _frameHz = 60;

		public int[] _vidbuffer = new int[160 * 2 * 144];
		public int[] buff_L = new int[160 * 144];
		public int[] buff_R = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;		
		}

		public void FillVideoBuffer()
		{
			// combine the 2 video buffers from the instances
			for (int i = 0; i < 144; i++)
			{
				for (int j = 0; j < 160; j++)
				{
					_vidbuffer[i * 320 + j] = buff_L[i * 160 + j];
					_vidbuffer[i * 320 + j + 160] = buff_R[i * 160 + j];
				}
			}
		}

		public int VirtualWidth => 160 * 2;
		public int VirtualHeight => 144;
		public int BufferWidth => 160 * 2;
		public int BufferHeight => 144;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

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
			short[] temp_samp_L = new short[735 * 2];
			short[] temp_samp_R = new short[735 * 2];

			L.GetSamplesSync(out temp_samp_L, out int nsamp_L);
			R.GetSamplesSync(out temp_samp_R, out int nsamp_R);

			if (linkSettings.AudioSet == GGLinkSettings.AudioSrc.Left)
			{
				samples = temp_samp_L;
				nsamp = nsamp_L;
			}
			else if (linkSettings.AudioSet == GGLinkSettings.AudioSrc.Right)
			{
				samples = temp_samp_R;
				nsamp = nsamp_R;
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
			L.DiscardSamples();
			R.DiscardSamples();
		}

		public void DisposeSound()
		{

		}
	}
}
