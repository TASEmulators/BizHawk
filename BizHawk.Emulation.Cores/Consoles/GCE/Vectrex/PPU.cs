using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class PPU
	{
		public VectrexHawk Core { get; set; }

		public bool zero_sig, ramp_sig, blank_sig;
		public byte vec_scale, x_vel, y_vel, bright;
		public double x_pos, y_pos;

		public static uint br = 0xFFFFFFFF;

		public void tick()
		{
			if (ramp_sig && !zero_sig)
			{
				x_pos = x_pos + (x_vel - 128.0) / 256.0 * (vec_scale + 2);
				y_pos = y_pos - (y_vel - 128.0) / 256.0 * (vec_scale + 2);

				if (x_pos > 255) { x_pos = 255; }
				if (x_pos < 0) { x_pos = 0; }
				if (y_pos > 383) { y_pos = 383; }
				if (y_pos < 0) { y_pos = 0; }

				if (!blank_sig) { Core._vidbuffer[(int)(Math.Floor(x_pos) + 256 * Math.Floor(y_pos))] = (int)br; }		
			}
			else if (zero_sig)
			{
				x_pos = 128;
				y_pos = 192;
			}
		}

		public void Reset()
		{
			zero_sig = true;
			blank_sig = true;
			ramp_sig = false;

			vec_scale = x_vel = y_vel = bright = 0;
			x_pos = 128;
			y_pos = 192;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(zero_sig), ref zero_sig);
			ser.Sync(nameof(blank_sig), ref blank_sig);
			ser.Sync(nameof(ramp_sig), ref ramp_sig);

			ser.Sync(nameof(vec_scale), ref vec_scale);
			ser.Sync(nameof(x_vel), ref x_vel);
			ser.Sync(nameof(y_vel), ref y_vel);
			ser.Sync(nameof(bright), ref bright);

			ser.Sync(nameof(x_pos), ref x_pos);
			ser.Sync(nameof(y_pos), ref y_pos);
		}
	}
}
