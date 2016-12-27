using BizHawk.Common.NumberExtensions;
using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class PSG : ISoundProvider
	{
		public ushort[] Register = new ushort[16];

		public int total_clock;

		public void Reset()
		{
			sq_per_A = sq_per_B = sq_per_C = clock_A = clock_B = clock_C = 0x1000;
			noise_per = noise_clock = 64;
			env_per = 0x20000;
		}

		public void DiscardSamples()
		{
			
			sample_count = 0;

			for (int i = 0; i < 3733; i++)
			{
				audio_samples[i] = 0;
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			short[] ret = new short[735 * 2];
			GetSamples(ret);
			samples = ret;
			nsamp = 735;
		}

		public void GetSamples(short[] samples)
		{
			for (int i = 0; i < samples.Length / 2; i++)
			{
				samples[i * 2] = (short)(audio_samples[(int)Math.Floor(3.7904 * i)]);
				samples[(i * 2) + 1] = samples[i * 2];
			}
		}

		// There is one audio clock for every 4 cpu clocks, and ~15000 cycles per frame
		public short[] audio_samples = new short[4000];

		public static int[] volume_table = new int[16] {0x0000, 0x0055, 0x0079, 0x00AB, 0x00F1, 0x0155, 0x01E3, 0x02AA,
		0x03C5, 0x0555, 0x078B, 0x0AAB, 0x0F16, 0x1555, 0x1E2B, 0x2AAA};

		public int sample_count;

		public int TotalExecutedCycles;
		public int PendingCycles;

		public int psg_clock;

		public int sq_per_A, sq_per_B, sq_per_C;
		public int clock_A, clock_B, clock_C;
		public int vol_A, vol_B, vol_C;
		public bool A_on, B_on, C_on;
		public bool A_up, B_up, C_up;
		public bool A_noise, B_noise, C_noise;

		public int env_per;
		public int env_clock;
		public int env_shape;
		public int env_E;
		public int E_up_down;
		public int env_vol_A, env_vol_B, env_vol_C;

		public int noise_clock;
		public int noise_per;
		public int noise=0x1;

		public Func<ushort, ushort> ReadMemory;
		public Func<ushort, ushort, bool> WriteMemory;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("PSG");

			ser.Sync("Register", ref Register, false);
			ser.Sync("Toal_executed_cycles", ref TotalExecutedCycles);
			ser.Sync("Pending_Cycles", ref PendingCycles);

			ser.Sync("sample_count", ref sample_count);
			ser.Sync("psg_clock", ref psg_clock);
			ser.Sync("clock_A", ref clock_A);
			ser.Sync("clock_B", ref clock_B);
			ser.Sync("clock_C", ref clock_C);
			ser.Sync("noise_clock", ref noise_clock);
			ser.Sync("env_clock", ref env_clock);
			ser.Sync("A_up", ref A_up);
			ser.Sync("B_up", ref B_up);
			ser.Sync("C_up", ref C_up);
			ser.Sync("noise", ref noise);
			ser.Sync("env_E", ref env_E);
			ser.Sync("E_up_down", ref E_up_down);

			sync_psg_state();

			ser.EndSection();
		}

		public ushort? ReadPSG(ushort addr)
		{
			if (addr >= 0x01F0 && addr <= 0x01FF)
			{
				return (ushort)(Register[addr - 0x01F0]);
			}
			return null;
		}

		public void sync_psg_state()
		{

			sq_per_A = (Register[0] & 0xFF) | (((Register[4] & 0xF) << 8));
			if (sq_per_A == 0)
				sq_per_A = 0x1000;

			sq_per_B = (Register[1] & 0xFF) | (((Register[5] & 0xF) << 8));
			if (sq_per_B == 0)
				sq_per_B = 0x1000;

			sq_per_C = (Register[2] & 0xFF) | (((Register[6] & 0xF) << 8));
			if (sq_per_C == 0)
				sq_per_C = 0x1000;

			env_per = (Register[3] & 0xFF) | (((Register[7] & 0xFF) << 8));
			if (env_per == 0)
				env_per = 0x20000;

			A_on = Register[8].Bit(0);
			B_on = Register[8].Bit(1);
			C_on = Register[8].Bit(2);
			A_noise = Register[8].Bit(3);
			B_noise = Register[8].Bit(4);
			C_noise = Register[8].Bit(5);

			noise_per = Register[9] & 0x1F;
			if (noise_per == 0)
			{
				noise_per = 64;
			}

			var shape_select = Register[10] & 0xF;

			if (shape_select < 4)
				env_shape = 0;
			else if (shape_select < 8)
				env_shape = 1;
			else
				env_shape = 2 + (shape_select - 8);

			vol_A = Register[11] & 0xF;
			env_vol_A = (Register[11] >> 4) & 0x3;

			vol_B = Register[12] & 0xF;
			env_vol_B = (Register[12] >> 4) & 0x3;

			vol_C = Register[13] & 0xF;
			env_vol_C = (Register[13] >> 4) & 0x3;
		}

		public bool WritePSG(ushort addr, ushort value)
		{
			if (addr >= 0x01F0 && addr <= 0x01FF)
			{
				var reg = addr - 0x01F0;

				value &= 0xFF;

				if (reg == 4 || reg == 5 || reg == 6 || reg == 10)
					value &= 0xF;

				if (reg == 9)
					value &= 0x1F;

				if (reg == 11 || reg == 12 || reg == 13)
					value &= 0x3F;

				Register[addr - 0x01F0] = value;

				sync_psg_state();

				if (reg == 10)
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
				return true;
			}
			return false;
		}

		public void generate_sound(int cycles_to_do)
		{
			// there are 4 cpu cycles for every psg cycle
			bool sound_out_A;
			bool sound_out_B;
			bool sound_out_C;

			for (int i=0;i<cycles_to_do;i++)
			{
				psg_clock++;

				if (psg_clock==4)
				{
					psg_clock = 0;

					total_clock++;

					clock_A--;
					clock_B--;
					clock_C--;

					noise_clock--;
					env_clock--;

					//clock noise
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

							//we just completed a period of the envelope, determine what to do now based on the envelope shape
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

					if (clock_B ==0)
					{
						B_up = !B_up;
						clock_B = sq_per_B;
					}

					if (clock_C ==0)
					{
						C_up = !C_up;
						clock_C = sq_per_C;
					}

					sound_out_A = (noise.Bit(0) | A_noise) & (A_on | A_up);
					sound_out_B = (noise.Bit(0) | B_noise) & (B_on | B_up);
					sound_out_C = (noise.Bit(0) | C_noise) & (C_on | C_up);

					//now calculate the volume of each channel and add them together
					if (env_vol_A == 0)
					{
						audio_samples[sample_count] = (short)(sound_out_A ? volume_table[vol_A] : 0);
					}
					else
					{
						int shift_A = 3-env_vol_A;
						if (shift_A < 0)
							shift_A = 0;
						audio_samples[sample_count] = (short)(sound_out_A ? (volume_table[env_E]>>shift_A) : 0);
					}

					if (env_vol_B == 0)
					{
						audio_samples[sample_count] += (short)(sound_out_B ? volume_table[vol_B] : 0);
					}
					else
					{
						int shift_B = 3 - env_vol_B;
						if (shift_B < 0)
							shift_B = 0;
						audio_samples[sample_count] += (short)(sound_out_B ? (volume_table[env_E] >> shift_B) : 0);
					}

					if (env_vol_C == 0)
					{
						audio_samples[sample_count] += (short)(sound_out_C ? volume_table[vol_C] : 0);
					}
					else
					{
						int shift_C = 3 - env_vol_C;
						if (shift_C < 0)
							shift_C = 0;
						audio_samples[sample_count] += (short)(sound_out_C ? (volume_table[env_E] >> shift_C) : 0);
					}
					sample_count++;
				}
			}

		}
	}
}
