using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound
{
	public class VRC6Alt : IDisposable
	{
		// http://wiki.nesdev.com/w/index.php/VRC6_audio





		#region blip-buf interface

		Sound.Utilities.BlipBuffer blip;
		// yes, some of this is copy+pasted from the FDS, and more or less from the NES
		// as soon as i decide that i like it and i use it a third time, i'll put it in a class

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
				blip.AddDelta(d.time * targetclock / sampleclock, d.value);
			dlist.Clear();
			blip.EndFrame(targetclock);
			sampleclock = 0;
			blip.ReadSamples(mixout, nsamp, false);

			for (int i = 0, j = 0; i < nsamp; i++, j += 2)
			{
				int s = mixout[i] +samples[j];
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

		#endregion

		Pulse pulse1, pulse2;
		Saw saw;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="freq">frequency of the M2 clock in hz</param>
		public VRC6Alt(uint freq)
		{
			if (freq > 0)
			{
				blip = new Utilities.BlipBuffer(blipsize);
				blip.SetRates(freq, 44100);
			}
			pulse1 = new Pulse(PulseAddDiff);
			pulse2 = new Pulse(PulseAddDiff);
			saw = new Saw(SawAddDiff);
		}

		public void Dispose()
		{
			if (blip != null)
			{
				blip.Dispose();
				blip = null;
			}
		}

		// the two pulse channels are about the same volume as 2a03 pulse channels.
		// everything is flipped, though; but that's taken care of in the classes
		void PulseAddDiff(int value)
		{
			dlist.Add(new Delta(sampleclock, value * 360));
		}
		// saw ends up being not that loud because of differences in implementation
		void SawAddDiff(int value)
		{
			dlist.Add(new Delta(sampleclock, value * 360));
		}

		// state
		bool masterenable;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("VRC6Alt");
			ser.Sync("masterenable", ref masterenable);
			pulse1.SyncState(ser);
			pulse2.SyncState(ser);
			saw.SyncState(ser);
			ser.EndSection();
		}

		public void Write9000(byte value) { pulse1.Write0(value); }
		public void Write9001(byte value) { pulse1.Write1(value); }
		public void Write9002(byte value) { pulse1.Write2(value); }

		public void Write9003(byte value)
		{
			masterenable = !value.Bit(0);
			int RSHIFT = 0;
			if (value.Bit(1))
				RSHIFT = 4;
			if (value.Bit(2))
				RSHIFT = 8;
			pulse1.SetRSHIFT(RSHIFT);
			pulse2.SetRSHIFT(RSHIFT);
			saw.SetRSHIFT(RSHIFT);
		}

		public void WriteA000(byte value) { pulse2.Write0(value); }
		public void WriteA001(byte value) { pulse2.Write1(value); }
		public void WriteA002(byte value) { pulse2.Write2(value); }

		public void WriteB000(byte value) { saw.Write0(value); }
		public void WriteB001(byte value) { saw.Write1(value); }
		public void WriteB002(byte value) { saw.Write2(value); }

		public void Clock()
		{
			if (masterenable)
			{
				pulse1.Clock();
				pulse2.Clock();
				saw.Clock();
			}
			sampleclock++;
		}

		class Saw
		{
			Action<int> SendDiff;
			public Saw(Action<int> SendDiff) { this.SendDiff = SendDiff; }

			// set by regs
			/// <summary>rate of increment for accumulator</summary>
			byte A;
			/// <summary>frequency.  actually a reload value</summary>
			int F;
			/// <summary>enable</summary>
			bool E;
			/// <summary>reload shift, from $9003</summary>
			int RSHIFT;

			// internal state
			/// <summary>frequency counter</summary>
			int count;
			/// <summary>accumulator</summary>
			byte accum;
			/// <summary>saw reset counter</summary>
			int acount;
			/// <summary>latched output, 0..31</summary>
			int output;

			public void SetRSHIFT(int RSHIFT)
			{
				this.RSHIFT = RSHIFT;
			}

			void SendNew()
			{
				int newvalue = accum >> 3;
				if (newvalue != output)
				{
					SendDiff(output - newvalue); // intentionally flipped
					output = newvalue;
				}
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("A", ref A);
				ser.Sync("F", ref F);
				ser.Sync("E", ref E);
				ser.Sync("RSHIFT", ref RSHIFT);
				ser.Sync("count", ref count);
				ser.Sync("accum", ref accum);
				ser.Sync("acount", ref acount);
				ser.Sync("output", ref output);
			}

			public void Write0(byte value)
			{
				A = (byte)(value & 63);
			}
			public void Write1(byte value)
			{
				F &= 0xf00;
				F |= value;
			}
			public void Write2(byte value)
			{
				F &= 0x0ff;
				F |= value << 8 & 0xf00;
				E = value.Bit(7);
				if (!E)
				{
					accum = 0;
					SendNew();
				}
			}

			public void Clock()
			{
				if (!E)
					return;
				count--;
				if (count <= 0)
				{
					count = F >> RSHIFT;
					acount++;
					if (acount % 2 == 0)
					{
						if (acount < 14)
						{
							accum += A;
						}
						else
						{
							accum = 0;
							acount = 0;
						}
						SendNew();
					}
				}
			}
		}

		class Pulse
		{
			Action<int> SendDiff;
			public Pulse(Action<int> SendDiff) { this.SendDiff = SendDiff; }

			// set by regs
			/// <summary>volume, 0..15</summary>
			int V;
			/// <summary>duty comparison.  forced to max when x000.7 == 1</summary>
			int D;
			/// <summary>frequency.  actually a reload value</summary>
			int F;
			/// <summary>enable</summary>
			bool E;
			/// <summary>reload shift, from $9003</summary>
			int RSHIFT;

			// internal state
			/// <summary>frequency counter</summary>
			int count;
			/// <summary>duty counter</summary>
			int duty;
			/// <summary>latched output, 0..15</summary>
			int output;

			public void SetRSHIFT(int RSHIFT)
			{
				this.RSHIFT = RSHIFT;
			}

			void SendNew()
			{
				int newvalue;
				if (duty <= D)
					newvalue = V;
				else
					newvalue = 0;
				if (newvalue != output)
				{
					SendDiff(output - newvalue); // intentionally flipped
					output = newvalue;
				}
			}

			void SendNewZero()
			{
				if (0 != output)
				{
					SendDiff(output - 0); // intentionally flipped
					output = 0;
				}
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("V", ref V);
				ser.Sync("D", ref D);
				ser.Sync("F", ref F);
				ser.Sync("E", ref E);
				ser.Sync("RSHIFT", ref RSHIFT);
				ser.Sync("count", ref count);
				ser.Sync("duty", ref duty);
				ser.Sync("output", ref output);
			}

			public void Write0(byte value)
			{
				V = value & 15;
				if (value.Bit(7))
					D = 16;
				else
					D = value >> 4 & 7;
				SendNew(); // this actually happens, right?
			}
			public void Write1(byte value)
			{
				F &= 0xf00;
				F |= value;
			}
			public void Write2(byte value)
			{
				F &= 0x0ff;
				F |= value << 8 & 0xf00;
				E = value.Bit(7);
				if (E)
					SendNew();
				else
					SendNewZero();
			}

			public void Clock()
			{
				if (!E)
					return;
				count--;
				if (count <= 0)
				{
					count = F >> RSHIFT;
					duty--;
					if (duty < 0)
						duty += 16;
					SendNew();
				}
			}
		}

	}
}
