using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound
{
	public class MMC5Audio
	{
		class Pulse
		{
			// regs
			int V;
			int T;
			int L;
			int D;
			bool LenCntDisable;
			bool ConstantVolume;
			bool Enable;
			// envelope
			bool estart;
			int etime;
			int ecount;
			// length
			static int[] lenlookup =
			{
				10,254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
				12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
			};
			int length;

			// pulse
			int sequence;
			static int[,] sequencelookup =
			{
				{0,0,0,0,0,0,0,1},
				{0,0,0,0,0,0,1,1},
				{0,0,0,0,1,1,1,1},
				{1,1,1,1,1,1,0,0}
			};
			int clock;
			int output;

			public Action<int> SendDiff;

			public Pulse(Action<int> SendDiff)
			{
				this.SendDiff = SendDiff;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("V", ref V);
				ser.Sync("T", ref T);
				ser.Sync("L", ref L);
				ser.Sync("D", ref D);
				ser.Sync("LenCntDisable", ref LenCntDisable);
				ser.Sync("ConstantVolume", ref ConstantVolume);
				ser.Sync("Enable", ref Enable);
				ser.Sync("estart", ref estart);
				ser.Sync("etime", ref etime);
				ser.Sync("ecount", ref ecount);
				ser.Sync("length", ref length);
				ser.Sync("sequence", ref sequence);
				ser.Sync("clock", ref clock);
				ser.Sync("output", ref output);
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

		Pulse[] pulse = new Pulse[2];
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="addr">0x5000..0x5015</param>
		/// <param name="val"></param>
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

		/// <summary>
		/// call for 8000:bfff reads
		/// </summary>
		/// <param name="val"></param>
		public void ReadROMTrigger(byte val)
		{
			if (PCMRead)
				WritePCM(val);
		}

		void WritePCM(byte val)
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

		Action<bool> RaiseIRQ;

		const int framereload = 7458; // ???
		int frame = 0;
		bool PCMRead;
		bool PCMEnableIRQ;
		bool PCMIRQTriggered;
		byte PCMVal;
		byte PCMNextVal;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MMC5Audio");
			ser.Sync("frame", ref frame);
			pulse[0].SyncState(ser);
			pulse[1].SyncState(ser);
			ser.Sync("PCMRead", ref PCMRead);
			ser.Sync("PCMEnableIRQ", ref PCMEnableIRQ);
			ser.Sync("PCMIRQTriggered", ref PCMIRQTriggered);
			ser.Sync("PCMVal", ref PCMVal);
			ser.Sync("PCMNextVal", ref PCMNextVal);
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
				enqueuer(20 * (int)(PCMVal - PCMNextVal));
				PCMVal = PCMNextVal;
			}
		}

		Action<int> enqueuer;

		void PulseAddDiff(int value)
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
