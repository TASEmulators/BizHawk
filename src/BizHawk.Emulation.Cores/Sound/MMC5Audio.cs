using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components
{
	public class MMC5Audio
	{
		private class Pulse
		{
			// regs
			private int V;
			private int T;
			private int L;
			private int D;
			private bool LenCntDisable;
			private bool ConstantVolume;
			private bool Enable;
			// envelope
			private bool estart;
			private int etime;
			private int ecount;
			// length
			private static readonly int[] lenlookup =
			{
				10,254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
				12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
			};

			private int length;

			// pulse
			private int sequence;
			private static readonly int[,] sequencelookup =
			{
				{0,0,0,0,0,0,0,1},
				{0,0,0,0,0,0,1,1},
				{0,0,0,0,1,1,1,1},
				{1,1,1,1,1,1,0,0}
			};
			private int clock;
			private int output;

			private readonly Action<int> SendDiff;

			public Pulse(Action<int> SendDiff)
			{
				this.SendDiff = SendDiff;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(V), ref V);
				ser.Sync(nameof(T), ref T);
				ser.Sync(nameof(L), ref L);
				ser.Sync(nameof(D), ref D);
				ser.Sync(nameof(LenCntDisable), ref LenCntDisable);
				ser.Sync(nameof(ConstantVolume), ref ConstantVolume);
				ser.Sync(nameof(Enable), ref Enable);
				ser.Sync(nameof(estart), ref estart);
				ser.Sync(nameof(etime), ref etime);
				ser.Sync(nameof(ecount), ref ecount);
				ser.Sync(nameof(length), ref length);
				ser.Sync(nameof(sequence), ref sequence);
				ser.Sync(nameof(clock), ref clock);
				ser.Sync(nameof(output), ref output);
			}

			public void Write0(byte val)
			{
				V = val & 15;
				ConstantVolume = val.Bit(4);
				LenCntDisable = val.Bit(5);
				D = val >> 6;
			}
			public void Write2(byte val)
			{
				T &= 0x700;
				T |= val;
			}
			public void Write3(byte val)
			{
				T &= 0xff;
				T |= val << 8 & 0x700;
				L = val >> 3;
				estart = true;
				if (Enable)
					length = lenlookup[L];
				sequence = 0;
			}
			public void SetEnable(bool val)
			{
				Enable = val;
				if (!Enable)
					length = 0;
			}
			public bool ReadLength()
			{
				return length > 0;
			}

			public void ClockFrame()
			{
				// envelope
				if (estart)
				{
					estart = false;
					ecount = 15;
					etime = V;
				}
				else
				{
					etime--;
					if (etime < 0)
					{
						etime = V;
						if (ecount > 0)
						{
							ecount--;
						}
						else if (LenCntDisable)
						{
							ecount = 15;
						}
					}
				}
				// length
				if (Enable && !LenCntDisable && length > 0)
				{
					length--;
				}
			}

			public void Clock()
			{
				clock--;
				if (clock < 0)
				{
					clock = T * 2 + 1;
					sequence--;
					if (sequence < 0)
						sequence += 8;

					int sequenceval = sequencelookup[D, sequence];

					int newvol = 0;

					if (sequenceval > 0 && length > 0)
					{
						if (ConstantVolume)
							newvol = V;
						else
							newvol = ecount;
					}

					if (newvol != output)
					{
						//Console.WriteLine("{0},{1}", newvol, output);
						SendDiff(output - newvol);
						output = newvol;
					}
				}
			}
		}

		private readonly Pulse[] pulse = new Pulse[2];
		
		/// <param name="addr">0x5000..0x5015</param>
		public void WriteExp(int addr, byte val)
		{
			switch (addr)
			{
				case 0x5000: pulse[0].Write0(val); break;
				case 0x5002: pulse[0].Write2(val); break;
				case 0x5003: pulse[0].Write3(val); break;
				case 0x5004: pulse[1].Write0(val); break;
				case 0x5006: pulse[1].Write2(val); break;
				case 0x5007: pulse[1].Write3(val); break;
				case 0x5010: // pcm mode/irq
					PCMRead = val.Bit(0);
					PCMEnableIRQ = val.Bit(7);
					RaiseIRQ(PCMEnableIRQ && PCMIRQTriggered);
					break;
				case 0x5011: // PCM value
					if (!PCMRead)
						WritePCM(val);
					break;
				case 0x5015:
					pulse[0].SetEnable(val.Bit(0));
					pulse[1].SetEnable(val.Bit(1));
					break;
			}
		}

		public byte Read5015()
		{
			byte ret = 0;
			if (pulse[0].ReadLength())
				ret |= 1;
			if (pulse[1].ReadLength())
				ret |= 2;
			return ret;
		}

		public byte Read5010()
		{
			byte ret = 0;
			if (PCMEnableIRQ && PCMIRQTriggered)
			{
				ret |= 0x80;
			}
			PCMIRQTriggered = false; // ack
			RaiseIRQ(PCMEnableIRQ && PCMIRQTriggered);
			return ret;
		}

		public byte Peek5010()
		{
			byte ret = 0;
			if (PCMEnableIRQ && PCMIRQTriggered)
			{
				ret |= 0x80;
			}
			return ret;
		}

		/// <summary>
		/// call for 8000:bfff reads
		/// </summary>
		public void ReadROMTrigger(byte val)
		{
			if (PCMRead)
				WritePCM(val);
		}

		private void WritePCM(byte val)
		{
			if (val == 0)
			{
				PCMIRQTriggered = true;
			}
			else
			{
				PCMIRQTriggered = false;
				// can't set diff here, because APU cycle clock might be wrong
				PCMNextVal = val;
			}
			RaiseIRQ(PCMEnableIRQ && PCMIRQTriggered);
		}

		private readonly Action<bool> RaiseIRQ;

		private const int framereload = 7458; // ???
		private int frame;
		private bool PCMRead;
		private bool PCMEnableIRQ;
		private bool PCMIRQTriggered;
		private byte PCMVal;
		private byte PCMNextVal;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(MMC5Audio));
			ser.Sync(nameof(frame), ref frame);
			ser.BeginSection("Pulse0");
			pulse[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Pulse1");
			pulse[1].SyncState(ser);
			ser.EndSection();
			ser.Sync(nameof(PCMRead), ref PCMRead);
			ser.Sync(nameof(PCMEnableIRQ), ref PCMEnableIRQ);
			ser.Sync(nameof(PCMIRQTriggered), ref PCMIRQTriggered);
			ser.Sync(nameof(PCMVal), ref PCMVal);
			ser.Sync(nameof(PCMNextVal), ref PCMNextVal);
			ser.EndSection();
			if (ser.IsReader)
				RaiseIRQ(PCMEnableIRQ && PCMIRQTriggered);
		}

		public void Clock()
		{
			pulse[0].Clock();
			pulse[1].Clock();
			frame++;
			if (frame == framereload)
			{
				frame = 0;
				pulse[0].ClockFrame();
				pulse[1].ClockFrame();
			}
			if (PCMNextVal != PCMVal)
			{
				enqueuer(20 * (PCMVal - PCMNextVal));
				PCMVal = PCMNextVal;
			}
		}

		private readonly Action<int> enqueuer;

		private void PulseAddDiff(int value)
		{
			enqueuer(value * 370);
			//Console.WriteLine(value);
		}

		public MMC5Audio(Action<int> enqueuer, Action<bool> RaiseIRQ)
		{
			this.enqueuer = enqueuer;
			this.RaiseIRQ = RaiseIRQ;
			for (int i = 0; i < pulse.Length; i++)
				pulse[i] = new Pulse(PulseAddDiff);
		}
	}
}
