using BizHawk.Common.NumberExtensions;
using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class PSG : ISoundProvider
	{
		public ushort[] Register = new ushort[16];

		public void DiscardSamples()
		{
			
		}

		public void GetSamples(short[] samples)
		{
		}

		public int TotalExecutedCycles;
		public int PendingCycles;

		public int psg_clock;
		public int psg_noise;

		public int sq_per_A, sq_per_B, sq_per_C;
		public int clock_A, clock_B, clock_C;
		public int vol_A, vol_B, vol_C;
		public bool A_on, B_on, C_on;
		public bool A_up, B_up, C_up;
		public bool A_noise, B_noise, C_noise;

		public int env_per;
		public int env_clock;
		public int env_shape;
		public int env_vol_A, env_vol_B, env_vol_C;

		public int noise_per;
		public int noise=0x1FFF;

		public Func<ushort, ushort> ReadMemory;
		public Func<ushort, ushort, bool> WriteMemory;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("PSG");

			ser.Sync("Register", ref Register, false);
			ser.Sync("Toal_executed_cycles", ref TotalExecutedCycles);
			ser.Sync("Pending Cycles", ref PendingCycles);

			ser.EndSection();
		}

		public ushort? ReadPSG(ushort addr)
		{
			if (addr >= 0x01F0 && addr <= 0x01FF)
			{
				return (ushort)(0xFF00 | Register[addr - 0x01F0]);
			}
			return null;
		}

		public bool WritePSG(ushort addr, ushort value)
		{
			if (addr >= 0x01F0 && addr <= 0x01FF)
			{
				Register[addr - 0x01F0] = value;
				
				var reg = addr - 0x01F0;
				if (reg==0 || reg==4)
				{
					sq_per_A = (Register[0] & 0xFF) | (((Register[4] & 0xF) << 8));
					if (sq_per_A == 0)
						sq_per_A = 0x1000;
					else
						sq_per_A *= 2;
					//clock_A = 0;
				}
				if (reg == 1 || reg == 5)
				{
					sq_per_B = (Register[1] & 0xFF) | (((Register[5] & 0xF) << 8));
					if (sq_per_B == 0)
						sq_per_B = 0x1000;
					else
						sq_per_B *= 2;
					//clock_B = 0;
				}
				if (reg == 2 || reg == 6)
				{
					sq_per_C = (Register[2] & 0xFF) | (((Register[6] & 0xF) << 8));
					if (sq_per_C == 0)
						sq_per_C = 0x1000;
					else
						sq_per_C *= 2;
					//clock_C = 0;
				}
				if (reg == 3 || reg == 7)
				{
					env_per = (Register[3] & 0xFF) | (((Register[7] & 0xFF) << 8));
					if (env_per == 0)
						env_per = 0x20000;
					else
						env_per *= 2;
				}

				if (reg==8)
				{
					A_on = Register[8].Bit(0);
					B_on = Register[8].Bit(1);
					C_on = Register[8].Bit(2);
					A_noise = Register[8].Bit(3);
					B_noise = Register[8].Bit(4);
					C_noise = Register[8].Bit(5);
				}

				if (reg==9)
				{
					noise_per = Register[9] & 0x1F;
				}

				if (reg==10)
				{
					//writing to register 10 resets the envelope
					env_clock = 0;

					var shape_select = Register[10] & 0xF;

					if (shape_select < 4)
						env_shape = 0;
					else if (shape_select < 8)
						env_shape = 1;
					else
						env_shape = 2 + (shape_select - 8);
				}

				if (reg==11)
				{
					vol_A = Register[11] & 0xF;
					env_vol_A = (Register[11] >> 4) & 0x3;
				}

				if (reg == 12)
				{
					vol_B = Register[12] & 0xF;
					env_vol_B = (Register[12] >> 4) & 0x3;
				}

				if (reg == 13)
				{
					vol_C = Register[13] & 0xF;
					env_vol_C = (Register[13] >> 4) & 0x3;
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
					clock_A++;
					clock_B++;
					clock_C++;
					env_clock++;

					//clock noise
					noise = (noise >> 1) ^ (noise.Bit(0) ? 0x14000 : 0);

					if (clock_A == sq_per_A)
					{
						A_up = !A_up;
						clock_A = 0;
					}

					if (clock_B == sq_per_B)
					{
						B_up = !B_up;
						clock_B = 0;
					}

					if (clock_C == sq_per_C)
					{
						C_up = !C_up;
						clock_C = 0;
					}


					sound_out_A = (noise.Bit(0) & A_noise) & (A_on & A_up);
					sound_out_B = (noise.Bit(0) & B_noise) & (B_on & B_up);
					sound_out_C = (noise.Bit(0) & C_noise) & (C_on & C_up);

				}

				

			}

		}
	}
}
