using BizHawk.Emulation.Common;

using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : IEmulator, IVideoProvider, ISoundProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		//Maria related variables
		public int cycle;
		public int cpu_cycle;
		public bool cpu_is_haltable;
		public bool cpu_is_halted;
		public bool cpu_halt_pending;
		public bool cpu_resume_pending;

		// input state of controllers and console
		public byte p1_state;
		public byte p2_state;
		public byte p1_fire;
		public byte p2_fire;
		public byte p1_fire_2x;
		public byte p2_fire_2x;
		public byte con_state;
		public bool left_toggle;
		public bool right_toggle;
		public bool left_was_pressed;
		public bool right_was_pressed;
		public bool p1_is_2button;
		public bool p2_is_2button;
		public bool p1_is_lightgun;
		public bool p2_is_lightgun;
		public float p1_lightgun_x;
		public float p1_lightgun_y;
		public float p2_lightgun_x;
		public float p2_lightgun_y;
		public int lg_1_counting_down;
		public int lg_1_counting_down_2;
		public int lg_2_counting_down;
		public int lg_2_counting_down_2;
		public byte lg_1_trigger_hit;
		public byte lg_2_trigger_hit;

		public int temp_s_tia;
		public int temp_s_pokey;
		public int samp_l, samp_c;
		public uint master_audio_clock;
		public int temp;

		// there are 4 maria cycles in a CPU cycle (fast access, both NTSC and PAL)
		// if the 6532 or TIA are accessed (PC goes to one of those addresses) the next access will be slower by 1/2 a CPU cycle
		// i.e. it will take 6 Maria cycles instead of 4
		public bool slow_access = false;
		public int slow_countdown;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			if (_tracer.IsEnabled())
			{
				cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				cpu.TraceCallback = null;
			}

			if (controller.IsPressed("Power"))
			{
				// it seems that theMachine.Reset() doesn't clear ram, etc
				// this should leave hsram intact but clear most other things
				HardReset();
			}

			_isLag = true;

			GetControllerState(controller);
			GetConsoleState(controller);

			maria.RunFrame();

			if (_isLag)
			{
				_lagCount++;
			}

			_frame++;

			return true;
		}

		public void RunCPUCycle()
		{
			if (slow_countdown==0)
			{
				cpu_cycle++;
			}
			else
			{
				slow_countdown--;
			}

			if (p1_is_lightgun)
			{
				if (lg_1_counting_down > 0)
				{
					lg_1_counting_down--;
					if (lg_1_counting_down == 0 && lg_1_counting_down_2 > 0)
					{
						lg_1_trigger_hit = 0;
						lg_1_counting_down = 454;
						lg_1_counting_down_2--;
					}

					if (lg_1_counting_down < 424)
					{
						lg_1_trigger_hit = 0x80;
					}
				}

				if ((maria.scanline - 20) == (p1_lightgun_y - 4))
				{
					if (maria.cycle == (132 + p1_lightgun_x))
					{ 
						// return true 64 cycles into the future
						lg_1_counting_down = 64;
						lg_1_counting_down_2 = 9;
					}			
				}
			}

			if (p2_is_lightgun)
			{
				if (lg_2_counting_down > 0)
				{
					lg_2_counting_down--;
					if (lg_2_counting_down == 0 && lg_2_counting_down_2 > 0)
					{
						lg_2_trigger_hit = 0;
						lg_2_counting_down = 454;
						lg_2_counting_down_2--;
					}

					if (lg_2_counting_down < 424)
					{
						lg_2_trigger_hit = 0x80;
					}
				}

				if ((maria.scanline - 20) == (p2_lightgun_y - 4))
				{
					if (maria.cycle == (132 + p2_lightgun_x))
					{
						// return true 64 cycles into the future
						lg_2_counting_down = 64;
						lg_2_counting_down_2 = 9;
					}					
				}
			}

			tia._hsyncCnt++;
			tia._hsyncCnt %= 454;
			// do the audio sampling of TIA audio
			if (tia._hsyncCnt == 113 || tia._hsyncCnt == 340)
			{
				temp_s_tia = tia.Execute();
			}

			// tick the m6532 timer, which is still active although not recommended to use
			// also it runs off of the cpu cycle timer
			// similarly tick the pokey if it is in use
			if (cpu_cycle== 4)
			{
				m6532.Timer.Tick();
			}

			samp_l = temp_s_tia + 3 * temp_s_pokey;

			if (samp_l != samp_c)
			{
				_blip.AddDelta(master_audio_clock, samp_l - samp_c);
				samp_c = samp_l;
			}

			temp++;
			if (temp == 4)
			{
				// the pokey chip ticks at the nominal cpu speed, but is unaffected by cpu slowdown (I think)
				if (is_pokey || is_pokey_450)
				{
					pokey.Tick();
					temp_s_pokey = pokey.sample();
				}

				master_audio_clock++;
				temp = 0;
			}
			

			if (cpu_cycle <= (2 + (slow_access ? 1 : 0)))
			{
				cpu_is_haltable = true;
			}
			else
			{
				cpu_is_haltable = false;
			}

			// the time a cpu cycle takes depends on the status of the address bus
			// any address in range of the TIA or m6532 takes an extra cycle to complete
			if (cpu_cycle == (4 + (slow_access ? 2 : 0)))
			{
				if (!cpu_is_halted)
				{
					cpu.ExecuteOne();

					// we need to stall the next cpu cycle from starting if the current one is a slow access
					if (slow_access)
					{
						slow_access = false;
						slow_countdown = 2;
					}
				}
				else
				{
					// we still want to keep track of CPU time even if it is halted, so increment the counter here
					// The basic 6502 has no halt state, this feature is specific to SALLY
					cpu.TotalExecutedCycles++;
				}

				cpu_cycle = 0;

				if (cpu_halt_pending)
				{
					cpu_halt_pending = false;
					cpu_is_halted = true;
				}

				if (cpu_resume_pending)
				{
					cpu_resume_pending = false;
					cpu_is_halted = false;
				}
			}
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();

			p1_state = _controllerDeck.ReadPort1(controller);
			p2_state = _controllerDeck.ReadPort2(controller);
			p1_fire = _controllerDeck.ReadFire1(controller);
			p2_fire = _controllerDeck.ReadFire2(controller);
			p1_fire_2x = _controllerDeck.ReadFire1_2x(controller);
			p2_fire_2x = _controllerDeck.ReadFire2_2x(controller);
			p1_is_2button = _controllerDeck.Is_2_button1(controller);
			p2_is_2button = _controllerDeck.Is_2_button2(controller);
			p1_is_lightgun = _controllerDeck.Is_LightGun1(controller, out p1_lightgun_x, out p1_lightgun_y);
			p2_is_lightgun = _controllerDeck.Is_LightGun2(controller, out p2_lightgun_x, out p2_lightgun_y);
		}

		public void GetConsoleState(IController controller)
		{
			byte result = 0;

			if (controller.IsPressed("Toggle Right Difficulty"))
			{
				if (!right_was_pressed)
				{
					right_toggle = !right_toggle;
				}
				right_was_pressed = true;
				result |= (byte)((right_toggle ? 1 : 0) << 7);
			}
			else
			{
				right_was_pressed = false;
				result |= (byte)((right_toggle ? 1 : 0) << 7);
			}

			if (controller.IsPressed("Toggle Left Difficulty"))
			{
				if (!left_was_pressed)
				{
					left_toggle = !left_toggle;
				}
				left_was_pressed = true;
				result |= (byte)((left_toggle ? 1 : 0) << 6);
			}
			else
			{
				left_was_pressed = false;
				result |= (byte)((left_toggle ? 1 : 0) << 6);
			}

			if (!controller.IsPressed("Pause"))
			{
				result |= (1 << 3);
			}
			if (!controller.IsPressed("Select"))
			{
				result |= (1 << 1);
			}
			if (!controller.IsPressed("Reset"))
			{
				result |= 1;
			}

			con_state = result;
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.A78;

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose()
		{
			maria = null;
			tia = null;
			m6532 = null;
			DisposeSound();
		}


		public int _frameHz = 60;
		public int _screen_width = 320;
		public int _screen_height = 263;
		public int _vblanklines = 20;

		public int[] _vidbuffer;

		public int[] GetVideoBuffer()
		{
			if (_syncSettings.Filter != "None")
			{
				apply_filter();
			}
			return _vidbuffer;
		}

		public int VirtualWidth => 320;
		public int VirtualHeight => _screen_height - _vblanklines;
		public int BufferWidth => 320;
		public int BufferHeight => _screen_height - _vblanklines;
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public void apply_filter()
		{

		}

		public static readonly Dictionary<string, string> ValidFilterTypes = new Dictionary<string, string>
		{
			{ "None",  "None"},
			{ "NTSC",  "NTSC"},
			{ "Pal",  "Pal"}
		};

		private BlipBuffer _blip = new BlipBuffer(4096);

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_blip.EndFrame(master_audio_clock);
			
			nsamp = _blip.SamplesAvailable();

			samples = new short[nsamp * 2];

			_blip.ReadSamples(samples, nsamp, true);

			for (int i = 0; i < nsamp * 2; i += 2)
			{
				samples[i + 1] = samples[i];
			}

			master_audio_clock = 0;
			temp = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			tia.AudioClocks = 0;
			master_audio_clock = 0;
			_blip.Clear();
		}

		public void DisposeSound()
		{
			_blip.Clear();
			_blip.Dispose();
			_blip = null;
		}
	}
}
