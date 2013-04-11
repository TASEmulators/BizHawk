using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BizHawk.Emulation.CPUs.M6502;

#pragma warning disable 162

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public partial class NES : IEmulator
	{
		//hardware/state
		// any of the 3 cpus are drop in replacements
		public MOS6502X cpu;
		//public MOS6502X_CPP cpu;
		//public MOS6502XDouble cpu;
		// dispose list as the native core can't keep track of its own stuff
		List<System.Runtime.InteropServices.GCHandle> DisposeList = new List<System.Runtime.InteropServices.GCHandle>();
		int cpu_accumulate; //cpu timekeeper
		public PPU ppu;
		public APU apu;
		byte[] ram;
		NESWatch[] sysbus_watch = new NESWatch[65536];
		public byte[] CIRAM; //AKA nametables
		string game_name; //friendly name exposed to user and used as filename base
		CartInfo cart; //the current cart prototype. should be moved into the board, perhaps
		INESBoard board; //the board hardware that is currently driving things
		EDetectionOrigin origin = EDetectionOrigin.None;
		public bool SoundOn = true;
		int sprdma_countdown;
		bool _irq_apu; //various irq signals that get merged to the cpu irq pin
		/// <summary>if true, use VS. system arrangement of $4016..$4020</summary>
		bool vs_io = false;
		bool vs_coin1;
		bool vs_coin2;
		/// <summary>clock speed of the main cpu in hz</summary>
		public int cpuclockrate { get; private set; }

		//irq state management
		public bool irq_apu { get { return _irq_apu; } set { _irq_apu = value; } }

		//user configuration 
		int[,] palette = new int[64,3];
		int[] palette_compiled = new int[64*8];
		IPortDevice[] ports;

		private DisplayType _display_type = DisplayType.NTSC;

		//Sound config
		public void SetSquare1(bool enabled) { apu.EnableSquare1 = enabled; }
		public void SetSquare2(bool enabled) { apu.EnableSquare2 = enabled; }
		public void SetTriangle(bool enabled) { apu.EnableTriangle = enabled; }
		public void SetNoise(bool enabled) { apu.EnableNoise = enabled; }
		public void SetDMC(bool enabled) { apu.EnableDMC = enabled; }

		public void Dispose()
		{
			if (magicSoundProvider != null) magicSoundProvider.Dispose();
			magicSoundProvider = null;
			if (DisposeList != null)
			{
				foreach (var h in DisposeList)
					h.Free();
				DisposeList = null;
			}
		}

		class MagicSoundProvider : ISoundProvider, ISyncSoundProvider, IDisposable
		{
			Sound.Utilities.BlipBuffer blip;
			NES nes;

			const int blipbuffsize = 4096;

			public MagicSoundProvider(NES nes, uint infreq)
			{
				this.nes = nes;

				blip = new Sound.Utilities.BlipBuffer(blipbuffsize);
				blip.SetRates(infreq, 44100);

				//var actualMetaspu = new Sound.MetaspuSoundProvider(Sound.ESynchMethod.ESynchMethod_V);
				//1.789773mhz NTSC
				//resampler = new Sound.Utilities.SpeexResampler(2, infreq, 44100 * APU.DECIMATIONFACTOR, infreq, 44100, actualMetaspu.buffer.enqueue_samples);
				//output = new Sound.Utilities.DCFilter(actualMetaspu);
			}

			public void GetSamples(short[] samples)
			{
				//Console.WriteLine("Sync: {0}", nes.apu.dlist.Count);
				int nsamp = samples.Length / 2;
				if (nsamp > blipbuffsize) // oh well.
					nsamp = blipbuffsize;
				uint targetclock = (uint)blip.ClocksNeeded(nsamp);
				uint actualclock = nes.apu.sampleclock;
				foreach (var d in nes.apu.dlist)
					blip.AddDelta(d.time * targetclock / actualclock, d.value);
				nes.apu.dlist.Clear();
				blip.EndFrame(targetclock);
				nes.apu.sampleclock = 0;

				blip.ReadSamples(samples, nsamp, true);
				// duplicate to stereo
				for (int i = 0; i < nsamp * 2; i += 2)
					samples[i + 1] = samples[i];

				//mix in the cart's extra sound circuit
				nes.board.ApplyCustomAudio(samples);
			}

			public void GetSamples(out short[] samples, out int nsamp)
			{
				//Console.WriteLine("ASync: {0}", nes.apu.dlist.Count);
				foreach (var d in nes.apu.dlist)
					blip.AddDelta(d.time, d.value);
				nes.apu.dlist.Clear();
				blip.EndFrame(nes.apu.sampleclock);
				nes.apu.sampleclock = 0;

				nsamp = blip.SamplesAvailable();
				samples = new short[nsamp * 2];

				blip.ReadSamples(samples, nsamp, true);
				// duplicate to stereo
				for (int i = 0; i < nsamp * 2; i += 2)
					samples[i + 1] = samples[i];

				nes.board.ApplyCustomAudio(samples);
			}

			public void DiscardSamples()
			{
				nes.apu.dlist.Clear();
				nes.apu.sampleclock = 0;
			}

			public int MaxVolume { get; set; }

			public void Dispose()
			{
				if (blip != null)
				{
					blip.Dispose();
					blip = null;
				}
			}
		}
		MagicSoundProvider magicSoundProvider;

		public void HardReset()
		{
			cpu = new MOS6502X((h) => DisposeList.Add(h));
			//cpu = new MOS6502X_CPP((h) => DisposeList.Add(h));
			//cpu = new MOS6502XDouble((h) => DisposeList.Add(h));
			cpu.SetCallbacks(ReadMemory, ReadMemory, PeekMemory, WriteMemory, (h) => DisposeList.Add(h));
			cpu.FetchCallback = () =>
				{
					if (CoreComm.Tracer.Enabled)
					{
						CoreComm.Tracer.Put(cpu.TraceState());
					}
				};
			cpu.BCD_Enabled = false;
			ppu = new PPU(this);
			ram = new byte[0x800];
			CIRAM = new byte[0x800];
			ports = new IPortDevice[2];
			ports[0] = new JoypadPortDevice(this, 0);
			ports[1] = new JoypadPortDevice(this, 1);

			// don't replace the magicSoundProvider on reset, as it's not needed
			// if (magicSoundProvider != null) magicSoundProvider.Dispose();

			// set up region
			switch (cart.system)
			{
				case "NES-PAL":
				case "NES-PAL-A":
				case "NES-PAL-B":
					apu = new APU(this, apu, true);
					ppu.region = PPU.Region.PAL;
					CoreComm.VsyncNum = 50;
					CoreComm.VsyncDen = 1;
					cpuclockrate = 1662607;
					cpu_sequence = cpu_sequence_PAL;
					_display_type = DisplayType.PAL;
					break;
				case "NES-NTSC":
				case "Famicom":
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.NTSC;
					cpuclockrate = 1789773;
					cpu_sequence = cpu_sequence_NTSC;
					break;
				// there's no official name for these in bootgod, not sure what we should use
				//case "PC10"://TODO
				case "VS":
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.RGB;
					cpuclockrate = 1789773;
					cpu_sequence = cpu_sequence_NTSC;
					vs_io = true;
					break;
				// this is in bootgod, but not used at all
				case "Dendy":
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.Dendy;
					CoreComm.VsyncNum = 50;
					CoreComm.VsyncDen = 1;
					cpuclockrate = 1773448;
					cpu_sequence = cpu_sequence_NTSC;
					_display_type = DisplayType.DENDY;
					break;
				case null:
					Console.WriteLine("Unknown NES system!  Defaulting to NTSC.");
					goto case "NES-NTSC";
				default:
					Console.WriteLine("Unrecognized NES system \"{0}\"!  Defaulting to NTSC.", cart.system);
					goto case "NES-NTSC";
			}
			if (magicSoundProvider == null)
				magicSoundProvider = new MagicSoundProvider(this, (uint)cpuclockrate);

			BoardSystemHardReset();

			//check fceux's PowerNES and FCEU_MemoryRand function for more information:
			//relevant games: Cybernoid; Minna no Taabou no Nakayoshi Daisakusen; Huang Di; and maybe mechanized attack
			for(int i=0;i<0x800;i++) if((i&4)!=0) ram[i] = 0xFF; else ram[i] = 0x00;

			SetupMemoryDomains();

			//in this emulator, reset takes place instantaneously
			cpu.PC = (ushort)(ReadMemory(0xFFFC) | (ReadMemory(0xFFFD) << 8));
			cpu.P = 0x34;
			cpu.S = 0xFD;
		}

		bool resetSignal;
		bool hardResetSignal;
		public void FrameAdvance(bool render, bool rendersound)
		{
			lagged = true;
			if (resetSignal)
			{
				board.NESSoftReset();
				cpu.NESSoftReset();
				apu.NESSoftReset();
				//need to study what happens to ppu and apu and stuff..
			}
			else if (hardResetSignal)
			{
				HardReset();
			}

			Controller.UpdateControls(Frame++);
			//if (resetSignal)
				//Controller.UnpressButton("Reset");   TODO fix this
			resetSignal = Controller["Reset"];
			hardResetSignal = Controller["Power"];

			if (board is FDS)
			{
				var b = board as FDS;
				if (Controller["FDS Eject"])
					b.Eject();
				for (int i = 0; i < b.NumSides; i++)
					if (Controller["FDS Insert " + i])
						b.InsertSide(i);
			}
			if (vs_io)
			{
				if (Controller["VS Coin 1"])
					vs_coin1 = true;
				if (Controller["VS Coin 2"])
					vs_coin2 = true;
			}

			ppu.FrameAdvance();
			if (lagged)
			{
				_lagcount++;
				islag = true;
			}
			else
				islag = false;

			videoProvider.FillFrameBuffer();
		}

		//PAL:
		//0 15 30 45 60 -> 12 27 42 57 -> 9 24 39 54 -> 6 21 36 51 -> 3 18 33 48 -> 0
		//sequence of ppu clocks per cpu clock: 4,3,3,3,3
		//NTSC:
		//sequence of ppu clocks per cpu clock: 3
		ByteBuffer cpu_sequence;
		static ByteBuffer cpu_sequence_NTSC = new ByteBuffer(new byte[]{3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3});
		static ByteBuffer cpu_sequence_PAL = new ByteBuffer(new byte[]{4,3,3,3,3,4,3,3,3,3,4,3,3,3,3,4,3,3,3,3,4,3,3,3,3,4,3,3,3,3,4,3,3,3,3,4,3,3,3,3});
		public int cpu_step, cpu_stepcounter, cpu_deadcounter;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void RunCpuOne()
		{
			cpu_stepcounter++;
			if (cpu_stepcounter == cpu_sequence[cpu_step])
			{
				cpu_step++;
				cpu_step &= 31;
				cpu_stepcounter = 0;

				if (sprdma_countdown > 0)
				{
					sprdma_countdown--;
					if (sprdma_countdown == 0)
					{
						//its weird that this is 514.. normally itd be 512 (and people would say its wrong) or 513 (and people would say its right)
						//but 514 passes test 4-irq_and_dma
						cpu_deadcounter += 514;
					}
				}

				if (cpu_deadcounter > 0)
					cpu_deadcounter--;
				else
				{
					cpu.IRQ = _irq_apu || board.IRQSignal;
					cpu.ExecuteOne();
				}

				apu.RunOne();
				board.ClockCPU();
				ppu.PostCpuInstructionOne();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte ReadReg(int addr)
		{
			switch (addr)
			{
				case 0x4000: case 0x4001: case 0x4002: case 0x4003:
				case 0x4004: case 0x4005: case 0x4006: case 0x4007:
				case 0x4008: case 0x4009: case 0x400A: case 0x400B:
				case 0x400C: case 0x400D: case 0x400E: case 0x400F:
				case 0x4010: case 0x4011: case 0x4012: case 0x4013:
					return apu.ReadReg(addr);
				case 0x4014: /*OAM DMA*/ break;
				case 0x4015: return apu.ReadReg(addr); 
				case 0x4016:
				case 0x4017:
					return read_joyport(addr);
				default:
					//Console.WriteLine("read register: {0:x4}", addr);
					break;

			}
			return 0xFF;
		}

		public byte PeekReg(int addr)
		{
			switch (addr)
			{
				case 0x4000: case 0x4001: case 0x4002: case 0x4003:
				case 0x4004: case 0x4005: case 0x4006: case 0x4007:
				case 0x4008: case 0x4009: case 0x400A: case 0x400B:
				case 0x400C: case 0x400D: case 0x400E: case 0x400F:
				case 0x4010: case 0x4011: case 0x4012: case 0x4013:
					return apu.PeekReg(addr);
				case 0x4014: /*OAM DMA*/ break;
				case 0x4015: return apu.PeekReg(addr); 
				case 0x4016:
				case 0x4017:
					return peek_joyport(addr);
				default:
					//Console.WriteLine("read register: {0:x4}", addr);
					break;

			}
			return 0xFF;
		}

		void WriteReg(int addr, byte val)
		{
			switch (addr)
			{
				case 0x4000: case 0x4001: case 0x4002: case 0x4003:
				case 0x4004: case 0x4005: case 0x4006: case 0x4007:
				case 0x4008: case 0x4009: case 0x400A: case 0x400B:
				case 0x400C: case 0x400D: case 0x400E: case 0x400F:
				case 0x4010: case 0x4011: case 0x4012: case 0x4013:
					apu.WriteReg(addr, val);
					break;
				case 0x4014: Exec_OAMDma(val); break;
				case 0x4015: apu.WriteReg(addr, val); break;
				case 0x4016:
					ports[0].Write(val & 1);
					ports[1].Write(val & 1);
					if (vs_io && board is Mapper099)
					{
						// happily, there aren't any other "VS exceptions" like this
						var b = board as Mapper099;
						b.Signal4016(val >> 2 & 1);
					}
					break;
				case 0x4017: apu.WriteReg(addr, val); break;
				default:
					//Console.WriteLine("wrote register: {0:x4} = {1:x2}", addr, val);
					break;
			}
		}

		byte read_joyport(int addr)
		{
			if (CoreComm.InputCallback != null) CoreComm.InputCallback();
			return handle_read_joyport(addr, false);
		}

		byte peek_joyport(int addr)
		{
			return handle_read_joyport(addr, true);
		}

		byte handle_read_joyport(int addr, bool peek)
		{
			//read joystick port
			//many todos here
			lagged = false;
			byte ret;
			if (addr == 0x4016)
				ret = ports[vs_io ? 1 : 0].Read(peek);
			else
				ret = ports[vs_io ? 0 : 1].Read(peek);
			if (vs_io)
			{
				if (addr == 0x4016)
				{
					// clear bits 2-6
					ret &= 0x83;
					if (false) // service switch
						ret |= 0x04;
					if (false) // DIP1
						ret |= 0x08;
					if (false) // DIP2
						ret |= 0x10;
					if (vs_coin1)
						ret |= 0x20;
					if (vs_coin2)
						ret |= 0x40;
				}
				else
				{
					// clear bits 2-7
					ret &= 0x03;
					if (false) // DIP3
						ret |= 0x04;
					if (false) // DIP4
						ret |= 0x08;
					if (false) // DIP5
						ret |= 0x10;
					if (false) // DIP6
						ret |= 0x20;
					if (false) // DIP7
						ret |= 0x40;
					if (false) // DIP8
						ret |= 0x80;
				}
			}
			return ret;
		}

		void Exec_OAMDma(byte val)
		{
			ushort addr = (ushort)(val << 8);
			for (int i = 0; i < 256; i++)
			{
				byte db = ReadMemory((ushort)addr);
				WriteMemory(0x2004, db);
				addr++;
			}
			//schedule a sprite dma event for beginning 1 cycle in the future.
			//this receives 2 because thats just the way it works out.
			sprdma_countdown = 2;
		}

		/// <summary>
		/// sets the provided palette as current
		/// </summary>
		public void SetPalette(int[,] pal)
		{
			Array.Copy(pal,palette,64*3);
			for(int i=0;i<64*8;i++)
			{
				int d = i >> 6;
				int c = i & 63;
				int r = palette[c, 0];
				int g = palette[c, 1];
				int b = palette[c, 2];
				Palettes.ApplyDeemphasis(ref r, ref g, ref b, d);
				palette_compiled[i] = (int)unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
			}
		}

		/// <summary>
		/// looks up an internal NES pixel value to an rgb int (applying the core's current palette and assuming no deemph)
		/// </summary>
		public int LookupColor(int pixel)
		{
			return palette_compiled[pixel];
		}

		public byte DummyReadMemory(ushort addr) { return 0; }

		private void ApplySystemBusPoke(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				ram[(addr & 0x7FF)] = value;
			}
			else if (addr < 0x4000)
			{
				ppu.WriteReg((addr & 0x07), value);
			}
			else if (addr < 0x4020)
			{
				WriteReg(addr, value);
			}
			else
			{
				ApplyGameGenie(addr, value, null); //Apply a cheat to the remaining regions since they have no direct access, this may not be the best way to handle this situation
			}
		}

		public byte PeekMemory(ushort addr)
		{
			byte ret;

			if (addr >= 0x4020)
			{
				ret = board.PeekCart(addr); //easy optimization, since rom reads are so common, move this up (reordering the rest of these elseifs is not easy)
			}
			else if (addr < 0x0800)
			{
				ret = ram[addr];
			}
			else if (addr < 0x2000)
			{
				ret = ram[addr & 0x7FF];
			}
			else if (addr < 0x4000)
			{
				ret = ppu.ReadReg(addr & 7);
			}
			else if (addr < 0x4020)
			{
				ret = ReadReg(addr); //we're not rebasing the register just to keep register names canonical
			}
			else
			{
				throw new Exception("Woopsie-doodle!");
				ret = 0xFF;
			}

			return ret;
		}

		//old data bus values from previous reads
		public byte DB;

		public byte ReadMemory(ushort addr)
		{
			byte ret;
			
			if (addr >= 0x8000)
			{
				ret = board.ReadPRG(addr - 0x8000); //easy optimization, since rom reads are so common, move this up (reordering the rest of these elseifs is not easy)
			}
			else if (addr < 0x0800)
			{
				ret = ram[addr];
			}
			else if (addr < 0x2000)
			{
				ret = ram[addr & 0x7FF];
			}
			else if (addr < 0x4000)
			{
				ret = ppu.ReadReg(addr & 7);
			}
			else if (addr < 0x4020)
			{
				ret = ReadReg(addr); //we're not rebasing the register just to keep register names canonical
			}
			else if (addr < 0x6000)
			{
				ret = board.ReadEXP(addr - 0x4000);
			}
			else
			{
				ret = board.ReadWRAM(addr - 0x6000);
			}
			
			//handle breakpoints and stuff.
			//the idea is that each core can implement its own watch class on an address which will track all the different kinds of monitors and breakpoints and etc.
			//but since freeze is a common case, it was implemented through its own mechanisms
			if (sysbus_watch[addr] != null)
			{
				sysbus_watch[addr].Sync();
				ret = sysbus_watch[addr].ApplyGameGenie(ret);
			}

			if (CoreComm.MemoryCallbackSystem.HasRead)
			{
				CoreComm.MemoryCallbackSystem.TriggerRead(addr);
			}

			DB = ret;

			return ret;
		}

		public void ApplyGameGenie(int addr, byte value, byte? compare)
		{
			if (addr < sysbus_watch.Length)
			{
				GetWatch(NESWatch.EDomain.Sysbus, addr).SetGameGenie(compare, value);
			}
		}

		public void RemoveGameGenie(int addr)
		{
			if (addr < sysbus_watch.Length)
			{
				GetWatch(NESWatch.EDomain.Sysbus, addr).RemoveGameGenie();
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x0800)
			{
				ram[addr] = value;
			}
			else if (addr < 0x2000)
			{
				ram[addr & 0x7FF] = value;
			}
			else if (addr < 0x4000)
			{
				ppu.WriteReg(addr & 7, value);
			}
			else if (addr < 0x4020)
			{
				WriteReg(addr, value);  //we're not rebasing the register just to keep register names canonical
			}
			else if (addr < 0x6000)
			{
				if (vs_io && addr == 0x4020 && (value & 1) != 0)
				{
					// acknowledge coin insertion
					vs_coin1 = false;
					vs_coin2 = false;
				}
				board.WriteEXP(addr - 0x4000, value);
			}
			else if (addr < 0x8000)
			{
				board.WriteWRAM(addr - 0x6000, value);
			}
			else
			{
				board.WritePRG(addr - 0x8000, value);
			}

			if (CoreComm.MemoryCallbackSystem.HasWrite)
			{
				CoreComm.MemoryCallbackSystem.TriggerWrite(addr);
			}
		}

	}
}