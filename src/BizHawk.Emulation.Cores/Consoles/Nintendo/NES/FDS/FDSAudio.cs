using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/FDS_audio
	public class FDSAudio
	{
		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(waveram), ref waveram, false);
			ser.Sync(nameof(waverampos), ref waverampos);

			ser.Sync(nameof(volumespd), ref volumespd);
			ser.Sync(nameof(r4080_6), ref r4080_6);
			ser.Sync(nameof(r4080_7), ref r4080_7);

			ser.Sync(nameof(frequency), ref frequency);
			ser.Sync(nameof(r4083_6), ref r4083_6);
			ser.Sync(nameof(r4083_7), ref r4083_7);

			ser.Sync(nameof(sweepspd), ref sweepspd);
			ser.Sync(nameof(r4084_6), ref r4084_6);
			ser.Sync(nameof(r4084_7), ref r4084_7);

			ser.Sync(nameof(sweepbias), ref sweepbias);

			ser.Sync(nameof(modfreq), ref modfreq);
			ser.Sync(nameof(r4087_7), ref r4087_7);

			ser.Sync(nameof(modtable), ref modtable, false);
			ser.Sync(nameof(modtablepos), ref modtablepos);

			ser.Sync(nameof(mastervol_num), ref mastervol_num);
			ser.Sync(nameof(mastervol_den), ref mastervol_den);
			ser.Sync(nameof(waveram_writeenable), ref waveram_writeenable);

			ser.Sync(nameof(envspeed), ref envspeed);

			ser.Sync(nameof(volumeclock), ref volumeclock);
			ser.Sync(nameof(sweepclock), ref sweepclock);
			ser.Sync(nameof(modclock), ref modclock);
			ser.Sync(nameof(mainclock), ref mainclock);

			ser.Sync(nameof(modoutput), ref modoutput);

			ser.Sync(nameof(volumegain), ref volumegain);
			ser.Sync(nameof(sweepgain), ref sweepgain);

			ser.Sync(nameof(waveramoutput), ref waveramoutput);

			ser.Sync(nameof(latchedoutput), ref latchedoutput);
		}

		//4040:407f
		private byte[] waveram = new byte[64];
		/// <summary>
		/// playback position, clocked by main unit
		/// </summary>
		private int waverampos;
		//4080
		/// <summary>
		/// volume level or envelope speed, depending on r4080_7
		/// </summary>
		private int volumespd;
		/// <summary>
		/// increase volume with envelope
		/// </summary>
		private bool r4080_6;
		/// <summary>
		/// disable volume envelope
		/// </summary>
		private bool r4080_7;
		//4082:4083
		/// <summary>
		/// speed to clock main unit
		/// </summary>
		private int frequency;
		/// <summary>
		/// disable volume and sweep
		/// </summary>
		private bool r4083_6;
		/// <summary>
		/// silence channel
		/// </summary>
		private bool r4083_7;
		//4084
		/// <summary>
		/// sweep gain or sweep speed, depending on r4084_7
		/// </summary>
		private int sweepspd;
		/// <summary>
		/// increase sweep with envelope
		/// </summary>
		private bool r4084_6;
		/// <summary>
		/// disable sweep unit
		/// </summary>
		private bool r4084_7;
		//4085
		/// <summary>
		/// 7 bit signed
		/// </summary>
		private int sweepbias;
		//4086:4087
		/// <summary>
		/// speed to clock modulation unit
		/// </summary>
		private int modfreq;
		/// <summary>
		/// disable modulation unit
		/// </summary>
		private bool r4087_7;
		//4088
		/// <summary>
		/// ring buffer, only 32 entries on hardware
		/// </summary>
		private byte[] modtable = new byte[64];
		/// <summary>
		/// playback position
		/// </summary>
		private int modtablepos;
		//4089
		private int mastervol_num = 1;
		private int mastervol_den = 1;
		/// <summary>
		/// channel silenced and waveram writable
		/// </summary>
		private bool waveram_writeenable;
		//408a
		private int envspeed;

		private int volumeclock;
		private int sweepclock;
		private int modclock;
		private int mainclock;

		private int modoutput;

		// read at 4090
		private int volumegain;
		// read at 4092
		private int sweepgain;

		private int waveramoutput;

		private int latchedoutput;

		private readonly Action<int> SendDiff;

		public FDSAudio(Action<int> SendDiff)
		{
			this.SendDiff = SendDiff;
		}

		private void CalcMod()
		{
			// http://forums.nesdev.com/viewtopic.php?f=3&t=10233
			int tmp = sweepbias * sweepgain;
			int remainder = tmp & 15;
			tmp >>= 4;
			if (remainder > 0 && (tmp & 0x80) == 0)
			{
				if (sweepbias < 0)
					tmp -= 1;
				else
					tmp += 2;
			}

			// signed with unconventional bias
			if (tmp >= 192)
				tmp -= 256;
			else if (tmp < -64)
				tmp += 256;

			// round to nearest
			tmp *= frequency;
			remainder = tmp & 63;
			tmp >>= 6;
			if (remainder >= 32)
				tmp++;
			modoutput = tmp;
		}

		private void CalcOut()
		{
			int tmp = volumegain < 32 ? volumegain : 32;
			tmp *= waveramoutput;
			tmp *= mastervol_num;
			tmp /= mastervol_den;

			if (latchedoutput != tmp)
			{
				SendDiff((tmp - latchedoutput) * 3);
				latchedoutput = tmp;
			}
		}

		/// <summary>
		///  ~1.7mhz
		/// </summary>
		public void Clock()
		{
			// volume envelope unit
			if (!r4080_7 && envspeed > 0 && !r4083_6)
			{
				volumeclock++;
				if (volumeclock >= 8 * envspeed * (volumespd + 1))
				{
					volumeclock = 0;
					if (r4080_6 && volumegain < 32)
						volumegain++;
					else if (!r4080_6 && volumegain > 0)
						volumegain--;
					CalcOut();
				}
			}
			// sweep unit
			if (!r4084_7 && envspeed > 0 && !r4083_6)
			{
				sweepclock++;
				if (sweepclock >= 8 * envspeed * (sweepspd + 1))
				{
					sweepclock = 0;
					if (r4084_6 && sweepgain < 32)
						sweepgain++;
					else if (!r4084_6 && sweepgain > 0)
						sweepgain--;
					CalcMod();
				}
			}
			// modulation unit
			if (!r4087_7 && modfreq > 0)
			{
				modclock += modfreq;
				if (modclock >= 0x10000)
				{
					modclock -= 0x10000;
					// our modtable is really twice as big (64 entries)
					switch (modtable[modtablepos++])
					{
						case 0: sweepbias += 0; break;
						case 1: sweepbias += 1; break;
						case 2: sweepbias += 2; break;
						case 3: sweepbias += 4; break;
						case 4: sweepbias = 0; break;
						case 5: sweepbias -= 4; break;
						case 6: sweepbias -= 2; break;
						case 7: sweepbias -= 1; break;
					}
					// sign extend
					sweepbias <<= 25;
					sweepbias >>= 25;

					modtablepos &= 63;
					CalcMod();
				}
			}
			// main unit
			if (!r4083_7 && frequency > 0 && frequency + modoutput > 0 && !waveram_writeenable)
			{
				mainclock += frequency + modoutput;
				if (mainclock >= 0x10000)
				{
					mainclock -= 0x10000;
					waveramoutput = waveram[waverampos++];
					waverampos &= 63;
					CalcOut();
				}
			}
		}

		public void WriteReg(int addr, byte value)
		{
			if (addr < 0x4080)
			{
				if (waveram_writeenable)
					waveram[addr - 0x4040] = (byte)(value & 63);
				return;
			}
			switch (addr)
			{
				case 0x4080:
					r4080_6 = (value & 0x40) != 0;
					r4080_7 = (value & 0x80) != 0;
					volumeclock = 0;
					volumespd = value & 63;
					if (r4080_7) // envelope is off, so written value gets sent to gain directly
						volumegain = value & 63;	
					break;
				case 0x4082:
					frequency &= 0xf00;
					frequency |= value;
					break;
				case 0x4083:
					frequency &= 0x0ff;
					frequency |= value << 8 & 0xf00;
					r4083_6 = (value & 0x40) != 0;
					r4083_7 = (value & 0x80) != 0;
					if (r4083_7)
						waverampos = 0;
					if (r4083_6)
					{
						volumeclock = 0;
						sweepclock = 0;
					}
					break;
				case 0x4084:
					sweepspd = value & 63;
					r4084_6 = (value & 0x40) != 0;
					r4084_7 = (value & 0x80) != 0;
					sweepclock = 0;
					if (r4084_7)
						sweepgain = value & 63;
					break;
				case 0x4085:
					sweepbias = value & 0x7f;

					// sign extend
					sweepbias <<= 25;
					sweepbias >>= 25;
					break;
				case 0x4086:
					modfreq &= 0xf00;
					modfreq |= value;
					if (r4087_7 || modfreq == 0) // when mod unit is disabled, mod output is fixed to 0, not hanging
						modoutput = 0;
					break;
				case 0x4087:
					modfreq &= 0x0ff;
					modfreq |= value << 8 & 0xf00;
					r4087_7 = (value & 0x80) != 0;
					if (r4087_7 || modfreq == 0) // when mod unit is disabled, mod output is fixed to 0, not hanging
						modoutput = 0;
					if (r4087_7)
						modclock = 0;
					break;
				case 0x4088:
					// write twice into virtual 64 unit buffer
					if (r4087_7)
					{
						modtable[modtablepos] = (byte)(value & 7);
						modtablepos++;
						modtablepos &= 63;
						modtable[modtablepos] = (byte)(value & 7);
						modtablepos++;
						modtablepos &= 63;
					}					
					break;
				case 0x4089:
					switch (value & 3)
					{
						case 0: mastervol_num = 1; mastervol_den = 1; break;
						case 1: mastervol_num = 2; mastervol_den = 3; break;
						case 2: mastervol_num = 2; mastervol_den = 4; break;
						case 3: mastervol_num = 2; mastervol_den = 5; break;
					}
					waveram_writeenable = (value & 0x80) != 0;
					break;
				case 0x408a:
					envspeed = value;
					break;
			}
		}

		public byte ReadReg(int addr, byte openbus)
		{
			byte ret = openbus;

			if (addr < 0x4080)
			{
				ret &= 0xc0;
				ret |= waveram[addr - 0x4040];
			}
			else if (addr == 0x4090)
			{
				ret &= 0xc0;
				ret |= (byte)volumegain;
			}
			else if (addr == 0x4092)
			{
				ret &= 0xc0;
				ret |= (byte)sweepgain;
			}
			return ret;
		}
	}
}