using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	// An AY_3_8912
	public class Audio : ISoundProvider
	{
		public VectrexHawk Core { get; set; }

		private BlipBuffer _blip = new BlipBuffer(15000);

		public uint master_audio_clock;

		private short current_sample, old_sample;

		// not actually part of the AY chip, the Vectrex schematics show a line directly feeding audio from multiplexer output
		public short pcm_sample;

		public byte[] Register = new byte[16];

		public byte port_sel;

		public short Sample()
		{
			return current_sample;
		}

		private static readonly int[] VolumeTable =
		{
			0x0000, 0x0055, 0x0079, 0x00AB, 0x00F1, 0x0155, 0x01E3, 0x02AA,
			0x03C5, 0x0555, 0x078B, 0x0AAB, 0x0F16, 0x1555, 0x1E2B, 0x2AAA
		};

		private int psg_clock;
		private int sq_per_A, sq_per_B, sq_per_C;
		private int clock_A, clock_B, clock_C;
		private int vol_A, vol_B, vol_C;
		private bool A_on, B_on, C_on;
		private bool A_up, B_up, C_up;
		private bool A_noise, B_noise, C_noise;

		private int env_per;
		private int env_clock;
		private int env_shape;
		private int env_E;
		private int E_up_down;
		private int env_vol_A, env_vol_B, env_vol_C;

		private int noise_clock;
		private int noise_per;
		private int noise = 0x1;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("PSG");

			ser.Sync(nameof(Register), ref Register, false);

			ser.Sync(nameof(psg_clock), ref psg_clock);
			ser.Sync(nameof(clock_A), ref clock_A);
			ser.Sync(nameof(clock_B), ref clock_B);
			ser.Sync(nameof(clock_C), ref clock_C);
			ser.Sync(nameof(noise_clock), ref noise_clock);
			ser.Sync(nameof(env_clock), ref env_clock);
			ser.Sync(nameof(A_up), ref A_up);
			ser.Sync(nameof(B_up), ref B_up);
			ser.Sync(nameof(C_up), ref C_up);
			ser.Sync(nameof(noise), ref noise);
			ser.Sync(nameof(env_E), ref env_E);
			ser.Sync(nameof(E_up_down), ref E_up_down);

			ser.Sync(nameof(port_sel), ref port_sel);
			ser.Sync(nameof(current_sample), ref current_sample);
			ser.Sync(nameof(old_sample), ref old_sample);
			ser.Sync(nameof(master_audio_clock), ref master_audio_clock);

			ser.Sync(nameof(pcm_sample), ref pcm_sample);

			sync_psg_state();

			ser.EndSection();
		}

		public byte ReadReg(int addr)
		{
			return Register[port_sel];
		}

		private void sync_psg_state()
		{
			sq_per_A = (Register[0] & 0xFF) | (((Register[1] & 0xF) << 8));
			if (sq_per_A == 0)
			{
				sq_per_A = 0x1000;
			}

			sq_per_B = (Register[2] & 0xFF) | (((Register[3] & 0xF) << 8));
			if (sq_per_B == 0)
			{
				sq_per_B = 0x1000;
			}

			sq_per_C = (Register[4] & 0xFF) | (((Register[5] & 0xF) << 8));
			if (sq_per_C == 0)
			{
				sq_per_C = 0x1000;
			}

			env_per = (Register[11] & 0xFF) | (((Register[12] & 0xFF) << 8));
			if (env_per == 0)
			{
				env_per = 0x10000;
			}

			env_per *= 2;

			A_on = Register[7].Bit(0);
			B_on = Register[7].Bit(1);
			C_on = Register[7].Bit(2);
			A_noise = Register[7].Bit(3);
			B_noise = Register[7].Bit(4);
			C_noise = Register[7].Bit(5);

			noise_per = Register[6] & 0x1F;
			if (noise_per == 0)
			{
				noise_per = 0x20;
			}

			var shape_select = Register[13] & 0xF;

			if (shape_select < 4)
				env_shape = 0;
			else if (shape_select < 8)
				env_shape = 1;
			else
				env_shape = 2 + (shape_select - 8);

			vol_A = Register[8] & 0xF;
			env_vol_A = (Register[8] >> 4) & 0x1;

			vol_B = Register[9] & 0xF;
			env_vol_B = (Register[9] >> 4) & 0x1;

			vol_C = Register[10] & 0xF;
			env_vol_C = (Register[10] >> 4) & 0x1;
		}

		public void WriteReg(int addr, byte value)
		{
			//Console.WriteLine("PORT: " + port_sel + " value: " + value + " cpu: " + Core.cpu.TotalExecutedCycles);

			value &= 0xFF;

			// do not write to input ports, controller input is directly assigned seperately
			if (port_sel < 14) { Register[port_sel] = value; }
			
			sync_psg_state();

			if (port_sel == 13)
			{
				env_clock = env_per;

				if (env_shape == 0 || env_shape == 2 || env_shape == 3 || env_shape == 4 || env_shape == 5)
				{
					env_E = 15;
					E_up_down = -1;
				}
				else
				{
					env_E = 0;
					E_up_down = 1;
				}
			}
		}

		public void tick()
		{
			// there are 8 cpu cycles for every psg cycle
			bool sound_out_A;
			bool sound_out_B;
			bool sound_out_C;

			psg_clock++;
			master_audio_clock++;

			if (psg_clock == 8)
			{
				psg_clock = 0;

				clock_A--;
				clock_B--;
				clock_C--;

				noise_clock--;
				env_clock--;

				// clock noise
				if (noise_clock == 0)
				{
					noise = (noise >> 1) ^ (noise.Bit(0) ? 0x10004 : 0);
					noise_clock = noise_per;
				}

				if (env_clock == 0)
				{
					env_clock = env_per;

					env_E += E_up_down;

					if (env_E == 16 || env_E == -1)
					{

						// we just completed a period of the envelope, determine what to do now based on the envelope shape
						if (env_shape == 0 || env_shape == 1 || env_shape == 3 || env_shape == 9)
						{
							E_up_down = 0;
							env_E = 0;
						}
						else if (env_shape == 5 || env_shape == 7)
						{
							E_up_down = 0;
							env_E = 15;
						}
						else if (env_shape == 4 || env_shape == 8)
						{
							if (env_E == 16)
							{
								env_E = 15;
								E_up_down = -1;
							}
							else
							{
								env_E = 0;
								E_up_down = 1;
							}
						}
						else if (env_shape == 2)
						{
							env_E = 15;
						}
						else
						{
							env_E = 0;
						}
					}
				}

				if (clock_A == 0)
				{
					A_up = !A_up;
					clock_A = sq_per_A;
				}

				if (clock_B == 0)
				{
					B_up = !B_up;
					clock_B = sq_per_B;
				}

				if (clock_C == 0)
				{
					C_up = !C_up;
					clock_C = sq_per_C;
				}


				sound_out_A = (noise.Bit(0) | A_noise) & (A_on | A_up);
				sound_out_B = (noise.Bit(0) | B_noise) & (B_on | B_up);
				sound_out_C = (noise.Bit(0) | C_noise) & (C_on | C_up);

				// now calculate the volume of each channel and add them together
				int v;

				if (env_vol_A == 0)
				{
					v = (short)(sound_out_A ? VolumeTable[vol_A] : 0);
				}
				else
				{
					v = (short)(sound_out_A ? VolumeTable[env_E] : 0);
				}

				if (env_vol_B == 0)
				{
					v += (short)(sound_out_B ? VolumeTable[vol_B] : 0);

				}
				else
				{
					v += (short)(sound_out_B ? VolumeTable[env_E] : 0);
				}

				if (env_vol_C == 0)
				{
					v += (short)(sound_out_C ? VolumeTable[vol_C] : 0);
				}
				else
				{
					v += (short)(sound_out_C ? VolumeTable[env_E] : 0);
				}

				current_sample = (short)(v + pcm_sample);

				if (current_sample != old_sample)
				{
					_blip.AddDelta(master_audio_clock, current_sample - old_sample);
					old_sample = current_sample;
				}
			}
		}

		public void Reset()
		{
			clock_A = clock_B = clock_C = 0x1000;
			noise_clock = 0x20;
			port_sel = 0;
			current_sample = old_sample = 0;
			master_audio_clock = 0;

			for (int i = 0; i < 16; i++)
			{
				Register[i] = 0x0000;
			}
			sync_psg_state();

			_blip.SetRates(1500000, 44100);
		}

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
			_blip.EndFrame(master_audio_clock);

			nsamp = _blip.SamplesAvailable();

			samples = new short[nsamp * 2];

			_blip.ReadSamples(samples, nsamp, true);

			for (int i = 0; i < nsamp * 2; i += 2)
			{
				samples[i + 1] = samples[i];
			}

			master_audio_clock = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_blip.Clear();
			master_audio_clock = 0;
		}

		private void GetSamples(short[] samples)
		{

		}

		public void DisposeSound()
		{
			_blip.Clear();
			_blip.Dispose();
			_blip = null;
		}
	}
}