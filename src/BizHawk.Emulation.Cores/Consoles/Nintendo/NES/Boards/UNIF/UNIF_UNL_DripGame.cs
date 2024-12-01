using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://www.qmtpro.com/~nes/drip/dripmap.txt
	// http://wiki.nesdev.com/w/index.php/UNIF/UNL-DripGame
	internal sealed class UNIF_UNL_DripGame : NesBoardBase
	{
		[MapperProp]
		public bool DripGameDipSwitch;

		// cycles the hardware takes to prep; exact value not known
		private const int WarmupTime = 1000000;

		// hardware warmup clock
		private int warmupclock = WarmupTime;

		// 16k prg bank
		private int[] prg = new int[2];
		// 2k chr bank
		private int[] chr = new int[4];
		// prg bank mask
		private int prgmask;
		// chr bank mask
		private int chrmask;

		// 4096 bits of attribute ram (only bottom two bits of each byte are valid)
		private byte[] exram = new byte[2048];

		// nt mirroring (we track internally because we have to use it specially)
		private int[] nt = new int[4];

		// true if exattr is active
		private bool exattr; // 800a.2
		// true if sram is writeable
		private bool sramwrite; // 800a.3

		// 8 bit latch of value written to 8008
		private byte irqbuffer; // 8008
		// current irq timer value (15 bit)
		private int irqvalue;

		// last ppu read in 2xxx that was a nametable read, mapped for nt mirroring to 000:7ff
		// internally, this might be implemented in a different way?
		private int lastntread;

		// sound hardware
		private SoundChannel sound0;
		private SoundChannel sound1;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);

			ser.Sync(nameof(DripGameDipSwitch), ref DripGameDipSwitch);

			ser.Sync(nameof(warmupclock), ref warmupclock);
			ser.Sync(nameof(prg), ref prg, false);
			ser.Sync(nameof(chr), ref chr, false);

			ser.Sync(nameof(exram), ref exram, false);
			ser.Sync(nameof(nt), ref nt, false);
			ser.Sync(nameof(exattr), ref exattr);
			ser.Sync(nameof(sramwrite), ref sramwrite);
			ser.Sync(nameof(irqbuffer), ref irqbuffer);
			ser.Sync(nameof(irqvalue), ref irqvalue);

			ser.Sync(nameof(lastntread), ref lastntread); // technically not needed if states are always at frame boundary

			ser.BeginSection(nameof(sound0));
			sound0.SyncState(ser);
			ser.EndSection();
			ser.BeginSection(nameof(sound1));
			sound1.SyncState(ser);
			ser.EndSection();
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_UNL-DRIPGAME":
					break;
				default:
					return false;
			}
			Cart.WramSize = 8;
			Cart.WramBattery = false;
			AssertPrg(16, 32, 64, 128, 256); // 4 bits x 16
			AssertChr(8, 16, 32); // 4 bits x 2

			AutoMapperProps.Apply(this);

			prgmask = Cart.PrgSize / 16 - 1;
			chrmask = Cart.PrgSize / 2 - 1;

			prg[1] = prgmask;
			SetMirror(0);

			if (NES.apu != null) // don't start up sound when in configurator
			{
				sound0 = new SoundChannel(NES.apu.ExternalQueue);
				sound1 = new SoundChannel(NES.apu.ExternalQueue);
			}

			return true;
		}

		private void SetMirror(int mirr)
		{
			switch (mirr)
			{
				case 0:
					nt[0] = 0; nt[1] = 1; nt[2] = 0; nt[3] = 1; break; // V
				case 1:
					nt[0] = 0; nt[1] = 0; nt[2] = 1; nt[3] = 1; break; // H
				case 2:
					nt[0] = 0; nt[1] = 0; nt[2] = 0; nt[3] = 0; break; // 1a
				case 3:
					nt[0] = 1; nt[1] = 1; nt[2] = 1; nt[3] = 1; break; // 1b
			}
		}

		public override void ClockCpu()
		{
			if (irqvalue > 0)
			{
				irqvalue--;
				if (irqvalue == 0)
					IrqSignal = true;
			}
			if (warmupclock > 0)
			{
				warmupclock--;
			}
			sound0.Clock();
			sound1.Clock();
		}

		public override byte ReadExp(int addr)
		{
			switch (addr & 0xf800)
			{
				case 0x0800: // status
					byte ret = warmupclock == 0 ? (byte)0x64 : (byte)0x7f;
					if (DripGameDipSwitch)
						ret |= 0x80;
					return ret;

				case 0x1000: // sound1 status
					return sound0.Read();
				case 0x1800: // sound2 status
					return sound1.Read();
				default:
					return NES.DB;
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr < 0x4000) // regs
			{
				switch (addr & 15)
				{
					case 0: // sc0 silence
					case 1: // sc0 data
					case 2: // sc0 p low
					case 3: // sc0 p hi
						sound0.Write(addr & 3, value);
						return;
					case 4: // sc0 silence
					case 5: // sc0 data
					case 6: // sc0 p low
					case 7: // sc0 p hi
						sound1.Write(addr & 3, value);
						return;

					case 8: // irql
						irqbuffer = value;
						return;
					case 9: // irqh
						IrqSignal = false; // ack
						if ((value & 0x80) != 0) // enable
						{
							irqvalue = value << 8 & 0x7f00 | irqbuffer;
						}
						else
						{
							irqvalue = 0;
						}
						return;

					case 10: // control
						SetMirror(value & 3);
						exattr = (value & 4) != 0;
						sramwrite = (value & 8) != 0;
						return;

					case 11:
						prg[0] = value & prgmask;
						return;

					case 12:
					case 13:
					case 14:
					case 15:
						chr[addr & 3] = value & chrmask;
						return;
				}
			}
			else // exattr
			{
				// mirror the two bottom bits to make rendering quicker
				value &= 3;
				value |= (byte)(value << 2);
				value |= (byte)(value << 4);
				exram[addr & 0x7ff] = value;
			}
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr & 0x3fff | prg[addr >> 14] << 14];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr & 0x7ff | chr[addr >> 11] << 11];
			}

			int mappedaddr = addr & 0x3ff | nt[addr >> 10 & 3] << 10;
			if (exattr && (addr & 0x3ff) >= 0x3c0)
			{
				// pull palette data from exattr instead
				return exram[lastntread];
			}

			lastntread = mappedaddr;
			return NES.CIRAM[mappedaddr];
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr >= 0x2000)
				NES.CIRAM[addr & 0x3ff | nt[addr >> 10 & 3] << 10] = value;
		}

		public override void WriteWram(int addr, byte value)
		{
			if (sramwrite)
				base.WriteWram(addr, value);
		}

		private class SoundChannel
		{
			public SoundChannel(Action<int> enqueuer)
			{
				this.enqueuer = enqueuer;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(fifo), ref fifo, false);
				ser.Sync(nameof(active), ref active);
				ser.Sync(nameof(writecursor), ref writecursor);
				ser.Sync(nameof(readcursor), ref readcursor);

				ser.Sync(nameof(timer), ref timer);
				ser.Sync(nameof(period), ref period);
				ser.Sync(nameof(volume), ref volume);
				ser.Sync(nameof(sample), ref sample);
				ser.Sync(nameof(latched), ref latched); // not needed

				ser.Sync(nameof(volumeChangePending), ref volumeChangePending); // very not needed
			}

			// sound data fifo
			private byte[] fifo = new byte[256];
			// true if channel is currently running
			private bool active = false;

			// where the next byte will be written to
			private int writecursor;
			// where the next byte will be read from
			private int readcursor;

			// current phase clock
			private int timer;
			// phase reload value
			private int period;

			// volume register
			private int volume;
			// last read sample
			private byte sample;
			// latched output
			private int latched;

			// communicate with APU
			private readonly Action<int> enqueuer;
			// true if V has been written and we need to check to change something
			private bool volumeChangePending;

			private void CalcLatch()
			{
				int n = volume * sample * 9; // 9 is magic master volume level
				if (n != latched)
				{
					enqueuer(n - latched);
					latched = n;
				}
			}

			private bool Empty => writecursor == readcursor;
			private bool Full => readcursor + 256 == writecursor;

			public void Write(int addr, byte value)
			{
				switch (addr)
				{
					case 0: // clear
						writecursor = 0;
						readcursor = 0;
						Array.Clear(fifo, 0, 256);
						active = false;
						break;
					case 1: // write
						// TODO: is a write disallowed if we're full?
						if (!Full)
						{
							fifo[writecursor & 255] = value;
							writecursor++;
							if (!active)
							{
								active = true;
								timer = period;
								//Console.WriteLine("Enter with period {0}", period);
							}
						}
						break;
					case 2: // set period low
						period &= 0xf00;
						period |= value;
						break;
					case 3: // period high + volume
						period &= 0xff;
						period |= value << 8 & 0xf00;
						volume = value >> 4;
						// we can't change the latched value right now because it's illegal
						// to enqueue to the APU when outside clockcpu method
						volumeChangePending = true;
						break;
				}
			}

			public void Clock()
			{
				if (active && timer > 0)
				{
					timer--;
					if (timer == 0)
					{
						if (Empty)
						{
							active = false;
							//Console.WriteLine("Exhaust");
						}
						else
						{
							sample = fifo[readcursor & 255];
							readcursor++;
							timer = period;
							CalcLatch();
						}
					}
				}
				if (volumeChangePending)
				{
					volumeChangePending = false;
					CalcLatch();
				}
			}

			public byte Read()
			{
				byte ret = 0;
				if (Empty)
					ret |= 0x40;
				if (Full)
					ret |= 0x80;
				return ret;
			}
		}
	}
}
