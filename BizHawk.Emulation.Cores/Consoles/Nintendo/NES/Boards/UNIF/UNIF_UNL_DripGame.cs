using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://www.qmtpro.com/~nes/drip/dripmap.txt
	// http://wiki.nesdev.com/w/index.php/UNIF/UNL-DripGame
	public class UNIF_UNL_DripGame : NES.NESBoardBase
	{
		[MapperProp]
		public bool DripGameDipSwitch;

		// cycles the hardware takes to prep; exact value not known
		const int WarmupTime = 1000000;

		// hardware warmup clock
		int warmupclock = WarmupTime;

		// 16k prg bank
		int[] prg = new int[2];
		// 2k chr bank
		int[] chr = new int[4];
		// prg bank mask
		int prgmask;
		// chr bank mask
		int chrmask;

		// 4096 bits of attribute ram (only bottom two bits of each byte are valid)
		byte[] exram = new byte[2048];

		// nt mirroring (we track internally because we have to use it specially)
		int[] nt = new int[4];

		// true if exattr is active
		bool exattr; // 800a.2
		// true if sram is writeable
		bool sramwrite; // 800a.3

		// 8 bit latch of value written to 8008
		byte irqbuffer; // 8008
		// current irq timer value (15 bit)
		int irqvalue;

		// last ppu read in 2xxx that was a nametable read, mapped for nt mirroring to 000:7ff
		// internally, this might be implemented in a different way?
		int lastntread;

		// sound hardware
		SoundChannel sound0;
		SoundChannel sound1;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);

			ser.Sync("DripGameDipSwitch", ref DripGameDipSwitch);

			ser.Sync("warmupclock", ref warmupclock);
			ser.Sync("prg", ref prg, false);
			ser.Sync("chr", ref chr, false);

			ser.Sync("exram", ref exram, false);
			ser.Sync("nt", ref nt, false);
			ser.Sync("exattr", ref exattr);
			ser.Sync("sramwrite", ref sramwrite);
			ser.Sync("irqbuffer", ref irqbuffer);
			ser.Sync("irqvalue", ref irqvalue);

			ser.Sync("lastntread", ref lastntread); // technically not needed if states are always at frame boundary

			ser.BeginSection("sound0");
			sound0.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("sound1");
			sound1.SyncState(ser);
			ser.EndSection();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-DRIPGAME":
					break;
				default:
					return false;
			}
			Cart.wram_size = 8;
			Cart.wram_battery = false;
			AssertPrg(16, 32, 64, 128, 256); // 4 bits x 16
			AssertChr(8, 16, 32); // 4 bits x 2

			AutoMapperProps.Apply(this);

			prgmask = Cart.prg_size / 16 - 1;
			chrmask = Cart.prg_size / 2 - 1;

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

		public override void ClockCPU()
		{
			if (irqvalue > 0)
			{
				irqvalue--;
				if (irqvalue == 0)
					IRQSignal = true;
			}
			if (warmupclock > 0)
			{
				warmupclock--;
			}
			sound0.Clock();
			sound1.Clock();
		}

		public override byte ReadEXP(int addr)
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

		public override void WritePRG(int addr, byte value)
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
						IRQSignal = false; // ack
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

		public override byte ReadPRG(int addr)
		{
			return ROM[addr & 0x3fff | prg[addr >> 14] << 14];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr & 0x7ff | chr[addr >> 11] << 11];
			}
			else
			{
				int mappedaddr = addr & 0x3ff | nt[addr >> 10 & 3] << 10;
				if (exattr && (addr & 0x3ff) >= 0x3c0)
				{
					// pull palette data from exattr instead
					return exram[lastntread];
				}
				else
				{
					lastntread = mappedaddr;
					return NES.CIRAM[mappedaddr];
				}
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr >= 0x2000)
				NES.CIRAM[addr & 0x3ff | nt[addr >> 10 & 3] << 10] = value;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (sramwrite)
				base.WriteWRAM(addr, value);
		}

		private class SoundChannel
		{
			public SoundChannel(Action<int> enqueuer)
			{
				this.enqueuer = enqueuer;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("fifo", ref fifo, false);
				ser.Sync("active", ref active);
				ser.Sync("writecursor", ref writecursor);
				ser.Sync("readcursor", ref readcursor);

				ser.Sync("timer", ref timer);
				ser.Sync("period", ref period);
				ser.Sync("volume", ref volume);
				ser.Sync("sample", ref sample);
				ser.Sync("latched", ref latched); // not needed

				ser.Sync("volumeChangePending", ref volumeChangePending); // very not needed
			}

			// sound data fifo
			byte[] fifo = new byte[256];
			// true if channel is currently running
			bool active = false;

			// where the next byte will be written to
			int writecursor;
			// where the next byte will be read from
			int readcursor;

			// current phase clock
			int timer;
			// phase reload value
			int period;

			// volume register
			int volume;
			// last read sample
			byte sample;
			// latched output
			int latched;

			// communicate with APU
			Action<int> enqueuer;
			// true if V has been written and we need to check to change something
			bool volumeChangePending;

			void CalcLatch()
			{
				int n = volume * sample * 9; // 9 is magic master volume level
				if (n != latched)
				{
					enqueuer(n - latched);
					latched = n;
				}
			}

			bool Empty { get { return writecursor == readcursor; } }
			bool Full { get { return readcursor + 256 == writecursor; } }

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
