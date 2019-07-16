using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class PPU
	{
		public VectrexHawk Core { get; set; }

		public bool zero_sig, ramp_sig, blank_sig, off_screen;
		public byte vec_scale, x_vel, y_vel, bright;
		public double x_pos, y_pos;

		public int skip;
		public uint bright_int_1, bright_int_2, bright_int_3;

		public static uint br = 0xFFFFFFFF;

		public void tick()
		{
			//Console.WriteLine(ramp_sig + " " + zero_sig + " " + blank_sig + " " + Core.cpu.TotalExecutedCycles + " " + (x_vel - 128.0) + " " + x_pos 
			//+ " " + (y_vel - 128.0) + " " + y_pos + " " + Core.t1_counter + " " + vec_scale);

			if (ramp_sig && !zero_sig)
			{
				if (skip == 0)
				{
					x_pos = x_pos + (x_vel - 128.0) / 256.0 * (vec_scale + 2);
					y_pos = y_pos - (y_vel - 128.0) / 256.0 * (vec_scale + 2);
				}
				else
				{
					skip--;
				}

				off_screen = false;
				
				if (x_pos > 257) { off_screen = true; if (x_pos > (257 + 256)) { x_pos = (257 + 256); } }
				if (x_pos < 2) { off_screen = true; if (x_pos < (2 - 256)) { x_pos = (2 - 256); } }
				if (y_pos > 385) { off_screen = true; if (y_pos > (385 + 256)) { y_pos = (385 + 256); } }
				if (y_pos < 2) { off_screen = true; if (y_pos < (2 - 256)) { y_pos = (2 - 256); } }


			}
			else if (zero_sig)
			{
				x_pos = 128 + 2;
				y_pos = 192 + 2;
			}

			if (!blank_sig && !off_screen)
			{

				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_1);
				
				Core._vidbuffer[(int)(Math.Round(x_pos) + 1 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_2);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 1 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_2);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) + 1))] |= (int)(br & bright_int_2);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) - 1))] |= (int)(br & bright_int_2);
				
				Core._vidbuffer[(int)(Math.Round(x_pos) + 2 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 2 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) + 2))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) - 2))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 1 + 260 * (Math.Round(y_pos) + 1))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 1 + 260 * (Math.Round(y_pos) - 1))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 1 + 260 * (Math.Round(y_pos) + 1))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 1 + 260 * (Math.Round(y_pos) - 1))] |= (int)(br & bright_int_3);
			}
		}

		public void Reset()
		{
			zero_sig = true;
			blank_sig = true;
			ramp_sig = false;

			vec_scale = x_vel = y_vel = bright = 0;
			x_pos = 128 + 2;
			y_pos = 192 + 2;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(zero_sig), ref zero_sig);
			ser.Sync(nameof(blank_sig), ref blank_sig);
			ser.Sync(nameof(ramp_sig), ref ramp_sig);
			ser.Sync(nameof(off_screen), ref off_screen);

			ser.Sync(nameof(vec_scale), ref vec_scale);
			ser.Sync(nameof(x_vel), ref x_vel);
			ser.Sync(nameof(y_vel), ref y_vel);
			ser.Sync(nameof(bright), ref bright);

			ser.Sync(nameof(x_pos), ref x_pos);
			ser.Sync(nameof(y_pos), ref y_pos);

			ser.Sync(nameof(skip), ref skip);
			ser.Sync(nameof(bright_int_1), ref bright_int_1);
			ser.Sync(nameof(bright_int_2), ref bright_int_2);
			ser.Sync(nameof(bright_int_3), ref bright_int_3);
		}
	}
}
