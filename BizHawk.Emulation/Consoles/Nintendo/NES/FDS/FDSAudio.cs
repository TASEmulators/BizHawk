using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// http://wiki.nesdev.com/w/index.php/FDS_audio
	public class FDSAudio
	{
		public void SyncState(Serializer ser)
		{
			// no need to sync the DCFilter or the samplebuff
			ser.Sync("waveram", ref waveram, false);
			ser.Sync("waverampos", ref waverampos);

			ser.Sync("volumespd", ref volumespd);
			ser.Sync("r4080_6", ref r4080_6);
			ser.Sync("r4080_7", ref r4080_7);

			ser.Sync("frequency", ref frequency);
			ser.Sync("r4083_6", ref r4083_6);
			ser.Sync("r4083_7", ref r4083_7);

			ser.Sync("sweepspd", ref sweepspd);
			ser.Sync("r4084_6", ref r4084_6);
			ser.Sync("r4084_7", ref r4084_7);

			ser.Sync("sweepbias", ref sweepbias);

			ser.Sync("modfreq", ref modfreq);
			ser.Sync("r4087_7", ref r4087_7);

			ser.Sync("modtable", ref modtable, false);
			ser.Sync("modtablepos", ref modtablepos);

			ser.Sync("mastervol_num", ref mastervol_num);
			ser.Sync("mastervol_den", ref mastervol_den);
			ser.Sync("waveram_writeenable", ref waveram_writeenable);

			ser.Sync("envspeed", ref envspeed);

			ser.Sync("volumeclock", ref volumeclock);
			ser.Sync("sweepclock", ref sweepclock);
			ser.Sync("modclock", ref modclock);
			ser.Sync("mainclock", ref mainclock);

			ser.Sync("modoutput", ref modoutput);

			ser.Sync("volumegain", ref volumegain);
			ser.Sync("sweepgain", ref sweepgain);

			ser.Sync("waveramoutput", ref waveramoutput);

			ser.Sync("latchedoutput", ref latchedoutput);
		}

		//4040:407f
		byte[] waveram = new byte[64];
		int waverampos;
		//4080
		int volumespd;
		bool r4080_6;
		bool r4080_7;
		//4082:4083
		int frequency;
		bool r4083_6;
		bool r4083_7;
		//4084
		int sweepspd;
		bool r4084_6;
		bool r4084_7;
		//4085
		int sweepbias;
		//4086:4087
		int modfreq;
		bool r4087_7;
		//4088
		byte[] modtable = new byte[64];
		int modtablepos;
		//4089
		int mastervol_num;
		int mastervol_den;
		bool waveram_writeenable;
		//408a
		int envspeed;

		int volumeclock;
		int sweepclock;
		int modclock;
		int mainclock;

		int modoutput;

		int volumegain;
		int sweepgain;

		int waveramoutput;

		int latchedoutput;

		/// <summary>
		/// enough room to hold roughly one frame of final output, 0-2047
		/// </summary>
		short[] samplebuff = new short[32768];
		int samplebuffpos = 0;

		void CalcMod()
		{
			// really don't quite get this...
			int tmp = sweepbias * sweepgain;
			if ((tmp & 0xf) != 0)
			{
				tmp /= 16;
				if (sweepbias < 0)
					tmp -= 1;
				else
					tmp += 2;
			}
			else
				tmp /= 16;

			if (tmp > 193)
				tmp -= 258;
			else if (tmp < -64)
				tmp += 256;

			modoutput = frequency * tmp / 64;
		}

		void CalcOut()
		{
			int tmp = volumegain < 32 ? volumegain : 32;
			tmp *= waveramoutput;
			tmp *= mastervol_num;
			tmp /= mastervol_den;
			latchedoutput = tmp;
		}

		/// <summary>
		///  ~1.7mhz
		/// </summary>
		public void Clock()
		{
			// volume envelope unit
			if (!r4080_7 && envspeed > 0 && !r4080_6)
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
					sweepbias &= 0x7f;
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
			samplebuff[samplebuffpos++] = (short)latchedoutput;
			// if for some reason ApplyCustomAudio() is not called, glitch up but don't crash
			samplebuffpos &= 32767;
		}

		public void WriteReg(int addr, byte value)
		{
			if (addr < 0x4080)
			{
				if (waveram_writeenable)
					// can waverampos ever be reset?
					waveram[addr - 0x4040] = (byte)(value & 63);
				return;
			}
			switch (addr)
			{
				case 0x4080:
					r4080_6 = (value & 0x40) != 0;
					r4080_7 = (value & 0x80) != 0;
					if (r4080_7) // envelope is off, so written value gets sent to gain directly
						volumegain = value & 63;
					else // envelope is on; written value is speed of change
						volumespd = value & 63;
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
					break;
				case 0x4084:
					sweepspd = value & 63;
					r4084_6 = (value & 0x40) != 0;
					r4084_7 = (value & 0x80) != 0;
					break;
				case 0x4085:
					modtablepos = 0; // reset
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
					break;
				case 0x4088:
					// write twice into virtual 64 unit buffer
					Buffer.BlockCopy(modtable, 2, modtable, 0, 62);
					modtable[62] = (byte)(value & 7);
					modtable[63] = (byte)(value & 7);
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

		Sound.Utilities.DCFilter dc = Sound.Utilities.DCFilter.DetatchedMode(4096);

		public void ApplyCustomAudio(short[] samples)
		{
			for (int i = 0; i < samples.Length; i += 2)
			{
				// worst imaginable resampling
				int pos = i * samplebuffpos / samples.Length;
				int samp = samplebuff[pos] * 6 - 12096;
				samp += samples[i];
				if (samp > 32767)
					samples[i] = 32767;
				else if (samp < -32768)
					samples[i] = -32768;
				else
					samples[i] = (short)samp;

				// NES audio is mono, so this should be identical anyway
				samples[i + 1] = samples[i];
			}
			//Console.WriteLine("##{0}##", samplebuffpos);
			samplebuffpos = 0;

			dc.PushThroughSamples(samples, samples.Length);
		}
	}
}
