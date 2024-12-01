using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components
{
	public class VRC6Alt
	{
		// http://wiki.nesdev.com/w/index.php/VRC6_audio

		private readonly Pulse pulse1, pulse2;
		private readonly Saw saw;

		private readonly Action<int> enqueuer;

		/// <param name="enqueuer">a place to dump deltas to</param>
		public VRC6Alt(Action<int> enqueuer)
		{
			this.enqueuer = enqueuer;
			pulse1 = new Pulse(PulseAddDiff);
			pulse2 = new Pulse(PulseAddDiff);
			saw = new Saw(SawAddDiff);
		}

		// the two pulse channels are about the same volume as 2a03 pulse channels.
		// everything is flipped, though; but that's taken care of in the classes
		private void PulseAddDiff(int value)
		{
			enqueuer(value * 360);
		}
		// saw ends up being not that loud because of differences in implementation
		private void SawAddDiff(int value)
		{
			enqueuer(value * 360);
		}

		// state
		private bool masterenable;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(VRC6Alt));
			ser.Sync(nameof(masterenable), ref masterenable);
			ser.BeginSection("Pulse1");
			pulse1.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Pulse2");
			pulse2.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Saw");
			saw.SyncState(ser);
			ser.EndSection();
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
		}

		private class Saw
		{
			private readonly Action<int> SendDiff;
			public Saw(Action<int> SendDiff) { this.SendDiff = SendDiff; }

			// set by regs
			/// <summary>rate of increment for accumulator</summary>
			private byte A;
			/// <summary>frequency.  actually a reload value</summary>
			private int F;
			/// <summary>enable</summary>
			private bool E;
			/// <summary>reload shift, from $9003</summary>
			private int RSHIFT;

			// internal state
			/// <summary>frequency counter</summary>
			private int count;
			/// <summary>accumulator</summary>
			private byte accum;
			/// <summary>saw reset counter</summary>
			private int acount;
			/// <summary>latched output, 0..31</summary>
			private int output;

			public void SetRSHIFT(int RSHIFT)
			{
				this.RSHIFT = RSHIFT;
			}

			private void SendNew()
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
				ser.Sync(nameof(A), ref A);
				ser.Sync(nameof(F), ref F);
				ser.Sync(nameof(E), ref E);
				ser.Sync(nameof(RSHIFT), ref RSHIFT);
				ser.Sync(nameof(count), ref count);
				ser.Sync(nameof(accum), ref accum);
				ser.Sync(nameof(acount), ref acount);
				ser.Sync(nameof(output), ref output);
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
				if (count < 0)
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

		private class Pulse
		{
			private readonly Action<int> SendDiff;
			public Pulse(Action<int> SendDiff) { this.SendDiff = SendDiff; }

			// set by regs
			/// <summary>volume, 0..15</summary>
			private int V;
			/// <summary>duty comparison.  forced to max when x000.7 == 1</summary>
			private int D;
			/// <summary>frequency.  actually a reload value</summary>
			private int F;
			/// <summary>enable</summary>
			private bool E;
			/// <summary>reload shift, from $9003</summary>
			private int RSHIFT;

			// internal state
			/// <summary>frequency counter</summary>
			private int count;
			/// <summary>duty counter</summary>
			private int duty;
			/// <summary>latched output, 0..15</summary>
			private int output;

			public void SetRSHIFT(int RSHIFT)
			{
				this.RSHIFT = RSHIFT;
			}

			private void SendNew()
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

			private void SendNewZero()
			{
				if (0 != output)
				{
					SendDiff(output - 0); // intentionally flipped
					output = 0;
				}
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(V), ref V);
				ser.Sync(nameof(D), ref D);
				ser.Sync(nameof(F), ref F);
				ser.Sync(nameof(E), ref E);
				ser.Sync(nameof(RSHIFT), ref RSHIFT);
				ser.Sync(nameof(count), ref count);
				ser.Sync(nameof(duty), ref duty);
				ser.Sync(nameof(output), ref output);
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
				if (count < 0)
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
