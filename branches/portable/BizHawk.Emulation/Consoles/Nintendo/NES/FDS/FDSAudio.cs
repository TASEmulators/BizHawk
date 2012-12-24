using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// http://wiki.nesdev.com/w/index.php/FDS_audio
	public class FDSAudio //: IDisposable
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
		/// <summary>
		/// playback position, clocked by main unit
		/// </summary>
		int waverampos;
		//4080
		/// <summary>
		/// volume level or envelope speed, depending on r4080_7
		/// </summary>
		int volumespd;
		/// <summary>
		/// increase volume with envelope
		/// </summary>
		bool r4080_6;
		/// <summary>
		/// disable volume envelope
		/// </summary>
		bool r4080_7;
		//4082:4083
		/// <summary>
		/// speed to clock main unit
		/// </summary>
		int frequency;
		/// <summary>
		/// disable volume and sweep
		/// </summary>
		bool r4083_6;
		/// <summary>
		/// silence channel
		/// </summary>
		bool r4083_7;
		//4084
		/// <summary>
		/// sweep gain or sweep speed, depending on r4084_7
		/// </summary>
		int sweepspd;
		/// <summary>
		/// increase sweep with envelope
		/// </summary>
		bool r4084_6;
		/// <summary>
		/// disable sweep unit
		/// </summary>
		bool r4084_7;
		//4085
		/// <summary>
		/// 7 bit signed
		/// </summary>
		int sweepbias;
		//4086:4087
		/// <summary>
		/// speed to clock modulation unit
		/// </summary>
		int modfreq;
		/// <summary>
		/// disable modulation unit
		/// </summary>
		bool r4087_7;
		//4088
		/// <summary>
		/// ring buffer, only 32 entries on hardware
		/// </summary>
		byte[] modtable = new byte[64];
		/// <summary>
		/// playback position
		/// </summary>
		int modtablepos;
		//4089
		int mastervol_num;
		int mastervol_den;
		/// <summary>
		/// channel silenced and waveram writable
		/// </summary>
		bool waveram_writeenable;
		//408a
		int envspeed;

		int volumeclock;
		int sweepclock;
		int modclock;
		int mainclock;

		int modoutput;

		// read at 4090
		int volumegain;
		// read at 4092
		int sweepgain;

		int waveramoutput;

		int latchedoutput;

		Action<int> SendDiff;

		public FDSAudio(Action<int> SendDiff)
		{
			this.SendDiff = SendDiff;
			/*
			// minor hack: due to the way the initialization sequence goes, this might get called
			// with m2rate = 0.  such an instance will never be asked for samples, though
			if (m2rate > 0)
			{
				blip = new Sound.Utilities.BlipBuffer(blipsize);
				blip.SetRates(m2rate, 44100);
			}
			*/
		}

		/*
		public void Dispose()
		{
			if (blip != null)
			{
				blip.Dispose();
				blip = null;
			}
		}
		*/

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

			if (latchedoutput != tmp)
			{
				//dlist.Add(new Delta(sampleclock, tmp - latchedoutput));
				SendDiff((tmp - latchedoutput) * 6);
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
					sweepbias &= 0x7f;
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
			//sampleclock++;
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

		/*
		Sound.Utilities.BlipBuffer blip;

		struct Delta
		{
			public uint time;
			public int value;
			public Delta(uint time, int value)
			{
				this.time = time;
				this.value = value;
			}
		}
		List<Delta> dlist = new List<Delta>();

		uint sampleclock = 0;
		const int blipsize = 4096;

		short[] mixout = new short[blipsize];

		public void ApplyCustomAudio(short[] samples)
		{
			int nsamp = samples.Length / 2;
			if (nsamp > blipsize) // oh well.
				nsamp = blipsize;
			uint targetclock = (uint)blip.ClocksNeeded(nsamp);
			foreach (var d in dlist)
			{
				// original deltas are in -2016..2016
				blip.AddDelta(d.time * targetclock / sampleclock, d.value * 6);
			}
			//Console.WriteLine("sclock {0} tclock {1} ndelta {2}", sampleclock, targetclock, dlist.Count);
			dlist.Clear();
			blip.EndFrame(targetclock);
			sampleclock = 0;
			blip.ReadSamples(mixout, nsamp, false);

			for (int i = 0, j = 0; i < nsamp; i++, j += 2)
			{
				int s = mixout[i] + samples[j];
				if (s > 32767)
					samples[j] = 32767;
				else if (s <= -32768)
					samples[j] = -32768;
				else
					samples[j] = (short)s;
				// nes audio is mono, so we can ignore the original value of samples[j+1]
				samples[j + 1] = samples[j];
			}
		}
		*/
	}
}
