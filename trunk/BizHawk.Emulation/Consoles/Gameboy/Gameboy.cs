using System;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.Z80GB;

namespace BizHawk.Emulation.Consoles.Gameboy
{
	public partial class Gameboy : IEmulator, IVideoProvider
	{
		private bool skipBIOS = false;
		private int _lagcount = 0;
		private bool islag = false;

		public interface IDebuggerAPI
		{
			void DoEvents();
		}
		public IDebuggerAPI DebuggerAPI;

		public enum ECartType
		{
			ROM_ONLY = 0x00,
			ROM_MBC1 = 0x01,
			ROM_MBC1_RAM = 0x02,
			ROM_MBC1_RAM_BATT = 0x03,
			ROM_MBC2 = 0x05,
			ROM_MBC2_BATTERY = 0x06,
			ROM_RAM = 0x08,
			ROM_RAM_BATTERY = 0x09,
			ROM_MMM01 = 0x0B,
			ROM_MMM01_SRAM = 0x0C,
			ROM_MMM01_SRAM_BATT = 0x0D,
			ROM_MBC3_TIMER_BATT = 0x0F,
			ROM_MBC3_TIMER_RAM_BATT = 0x10,
			ROM_MBC3 = 0x11,
			ROM_MBC3_RAM = 0x12,
			ROM_MBC3_RAM_BATT = 0x13,
			ROM_MBC5 = 0x19,
			ROM_MBC5_RAM = 0x1A,
			ROM_MBC5_RAM_BATT = 0x1B,
			ROM_MBC5_RUMBLE = 0x1C,
			ROM_MBC5_RUMBLE_SRAM = 0x1D,
			ROM_MBC5_RUMBLE_SRAM_BATT = 0x1E,
			PocketCamera = 0x1F,
			Bandai_TAMA5 = 0xFD,
			Hudson_HuC_3 = 0xFE,
			Hudson_HuC_1 = 0xFF,
		}

		public enum ESystemType
		{
			GB, //original gameboy
			GBP, //gameboy pocket
			GBC, //gameboy color
			SGB, //super gameboy
			GBA, //gameboy advance (emulating old hardware)
		}

		public ECartType CartType;
		public ESystemType SystemType;

		public struct TCartFlags
		{
			public bool GBC; //cart indicates itself as GBC aware
			public bool SGB; //cart indicates itself as SGB aware
		}
		public TCartFlags CartFlags = new TCartFlags();

		static byte SetBit8(byte variable, int bit, bool val)
		{
			int mask = 1 << bit;
			int temp = variable;
			temp &= ~mask;
			if (val) temp |= mask;
			return (byte)temp;
		}

		static byte SetBit8(byte variable, int bit, int val)
		{
			return SetBit8(variable, bit, val != 0);
		}

		static bool GetBit8(byte variable, int bit)
		{
			return (variable & (1 << bit)) != 0;
		}

		public class TRegisters
		{
			Gameboy gb;
			public TRegisters(Gameboy gb)
			{
				this.gb = gb;
				STAT = new TSTAT(gb);
			}

			public bool BiosMapped = true;
			public class TLCDC
			{
				byte val;
				public bool Enabled { get { return GetBit8(val, 7); } set { val = SetBit8(val, 7, value); } }
				public ETileMap WindowTileMap { get { return GetBit8(val, 6) ? ETileMap.Region_9C00_9FFF : ETileMap.Region_9800_9BFF; } set { val = SetBit8(val, 6, (int)value); } }
				public ushort WindowTileMapAddr { get { return GetTileMapAddrFor(WindowTileMap); } }
				public bool WindowDisplay { get { return GetBit8(val, 5); } set { val = SetBit8(val, 5, value); } }
				public ETileData TileData { get { return GetBit8(val, 4) ? ETileData.Region_8000_8FFF : ETileData.Region_8800_97FF; } set { val = SetBit8(val, 4, (int)value); } }
				public ushort TileDataAddr { get { return GetTileDataAddrFor(TileData); } }
				public ETileMap BgTileMap { get { return GetBit8(val, 3) ? ETileMap.Region_9C00_9FFF : ETileMap.Region_9800_9BFF; } set { val = SetBit8(val, 3, (int)value); } }
				public ushort BgTileMapAddr { get { return GetTileMapAddrFor(BgTileMap); } }
				public EObjSize ObjSize { get { return GetBit8(val, 2) ? EObjSize.ObjSize_8x16 : EObjSize.ObjSize_8x8; } set { val = SetBit8(val, 2, (int)value); } }
				public bool ObjEnabled { get { return GetBit8(val, 1); } set { val = SetBit8(val, 1, value); } }
				public bool BgEnabled { get { return GetBit8(val, 0); } set { val = SetBit8(val, 0, value); } }

				public byte Read() { return val; }
				public void Write(byte value) { val = value; }
				public void Poke(byte value) { val = value; }

				public enum ETileMap
				{
					Region_9800_9BFF = 0,
					Region_9C00_9FFF = 1,
				}

				public enum ETileData
				{
					Region_8800_97FF = 0,
					Region_8000_8FFF = 1,
				}

				public ushort GetTileMapAddrFor(ETileMap tm) { return (ushort)(tm == ETileMap.Region_9800_9BFF ? 0x9800 : 0x9C00); }
				public ushort GetTileDataAddrFor(ETileData tm) { return (ushort)(tm == ETileData.Region_8800_97FF ? 0x8800 : 0x8000); }


				public enum EObjSize
				{
					ObjSize_8x8 = 0,
					ObjSize_8x16 = 1,
				}
			}
			public TLCDC LCDC = new TLCDC();
			public byte SCY, SCX;

			public class TSTAT
			{
				readonly Gameboy gb;
				public TSTAT(Gameboy gb) { this.gb = gb; }

				public byte Read()
				{
					//TODO (not done yet)
					int mode;
					if (gb.Registers.Timing.line >= 160) mode = 1;
					else if (gb.Registers.Timing.dot < 80) mode = 2;
					else if (gb.Registers.Timing.dot < 172 + 80) mode = 3;
					else mode = 0;

					return (byte)mode;
				}
			}
			public TSTAT STAT;

			public class TTiming
			{
				public int frame;
				public int line;
				public int dot;
			}
			public TTiming Timing = new TTiming();

			public class TInput
			{
				public bool up, down, left, right, a, b, select, start;
				int val;
				public void Write(byte value)
				{
					val = value & 0x30;
				}
				public byte Read()
				{
					if ((val & 0x10) == 0)
					{
						int ret = SetBit8(0, 0, right) | SetBit8(0, 1, left) | SetBit8(0, 2, up) | SetBit8(0, 3, down);
						return (byte)(~ret);
					}
					else if ((val & 0x10) == 0)
					{
						int ret = SetBit8(0, 0, a) | SetBit8(0, 1, b) | SetBit8(0, 2, select) | SetBit8(0, 3, start);
						return (byte)(~ret);
					}
					else return 0xFF;

					//TODO return system type???
				}
			}
			public TInput Input = new TInput();

			public byte Read_LY() { return (byte)Timing.line; }

		};
		public TRegisters Registers;

		public void SingleStepInto()
		{
			Cpu.TotalExecutedCycles = 0;
			Cpu.SingleStepInto();
			int elapsed = Cpu.TotalExecutedCycles;
			Timekeeping(elapsed);
		}

		public bool DebugBreak;
		public void RunForever()
		{
			int sanity = 0;

			for (; ; )
			{
				SingleStepInto();

				sanity++;
				if (sanity == 100000)
				{
					if (DebuggerAPI != null) DebuggerAPI.DoEvents();
					if (DebugBreak) break;
					sanity = 0;
				}
			}

			DebugBreak = false;
		}

		public void Timekeeping(int elapsed)
		{
			Registers.Timing.dot += elapsed;
			if (Registers.Timing.dot >= 456)
			{
				Registers.Timing.line++;
				Registers.Timing.dot -= 456;
			}
			if (Registers.Timing.line > 153)
			{
				Registers.Timing.frame++;
				Registers.Timing.line = 0;
			}
		}

		public void DetachBios()
		{
			Registers.BiosMapped = false;
			Cpu.ReadMemory = ReadMemory;
		}

		public class TSound
		{
			public byte[] WavePatternRam = new byte[16];
		}

		public TSound Sound;

		public byte[] Rom;
		public byte[] WRam;
		public byte[] SRam;
		public byte[] VRam;
		public byte[] HRam;
		public byte[] OAM;

		public Z80 Cpu;
		public MemoryMapper Mapper;

		public Gameboy(GameInfo game, byte[] rom, bool SkipBIOS)
		{
			skipBIOS = SkipBIOS;
			CoreOutputComm = new CoreOutputComm();
			CartType = (ECartType)rom[0x0147];
			Mapper = new MemoryMapper(this);
			Rom = rom;
			CartFlags.GBC = Rom[0x0143] == 0x80;
			CartFlags.SGB = Rom[0x0146] == 0x03;
			HardReset();
		}

		public void HardReset()
		{
			Cpu = new CPUs.Z80GB.Z80();
			if (skipBIOS)
				Cpu.ReadMemory = ReadMemory;
			else
				Cpu.ReadMemory = ReadMemoryBios;
			Cpu.WriteMemory = WriteMemory;
			Cpu.Reset();
			Cpu.LogData();

			//setup initial cpu registers. based on no evidence:
			//registers which may be used to identify system type are judged to be important; and
			//the initial contents of the stack are judged to be unimportant.
			switch (SystemType)
			{
				case ESystemType.GB:
				case ESystemType.SGB:
					Cpu.RegisterA = 0x01;
					break;
				case ESystemType.GBP:
					Cpu.RegisterA = 0xFF;
					break;
				case ESystemType.GBC:
					Cpu.RegisterA = 0x11;
					break;
				case ESystemType.GBA:
					throw new NotImplementedException(); //decide what to do
			}
			Cpu.RegisterF = 0xB0;
			if (SystemType == ESystemType.GBA)
				Cpu.RegisterB = 0x01;
			else Cpu.RegisterB = 0x00;
			Cpu.RegisterC = 0x13;
			Cpu.RegisterDE = 0x00D8;
			Cpu.RegisterHL = 0x014D;
			Cpu.RegisterSP = 0xFFFE;
			if (skipBIOS) Cpu.RegisterPC = 0x0100;
			else Cpu.RegisterPC = 0x0000;

			WRam = new byte[32 * 1024]; //GB has 4KB of WRam; GBC has 32KB of WRam 
			SRam = new byte[8 * 1024]; //different carts may have different amounts of this
			VRam = new byte[0x2000];
			HRam = new byte[128];
			OAM = new byte[0xA0];

			Sound = new TSound();
			Registers = new TRegisters(this);

			Registers.LCDC.Poke(0x91);
			SetupMemoryDomains();
		}

		private IList<MemoryDomain> memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(1);

			var SystemBusDomain = new MemoryDomain("System Bus", 0x10000, Endian.Little,
				addr => Cpu.ReadMemory((ushort)addr),
				(addr, value) => Cpu.WriteMemory((ushort)addr, value));

			var WRAM0Domain = new MemoryDomain("WRAM Bank 0", 0x2000, Endian.Little,
				addr => WRam[addr & 0x1FFF],
				(addr, value) => WRam[addr & 0x1FFF] = value);

			var WRAM1Domain = new MemoryDomain("WRAM Bank 1", 0x2000, Endian.Little,
				addr => WRam[(addr & 0x1FFF) + 0x2000],
				(addr, value) => WRam[addr & 0x1FFF] = value);

			var WRAMADomain = new MemoryDomain("WRAM Bank (All)", 0x8000, Endian.Little,
				addr => WRam[addr & 0x7FFF],
				(addr, value) => WRam[addr & 0x7FFF] = value); //adelikat: Do we want to check for GBC vs GB and limit this domain accordingly?

			var SRAMDomain = new MemoryDomain("SRAM", 0x2000, Endian.Little,
				addr => SRam[addr & 0x1FFF],
				(addr, value) => OAM[addr & 0x1FFF] = value);

			var OAMDomain = new MemoryDomain("OAM", 0x00A0, Endian.Little,
				addr => OAM[addr & 0x9F],
				(addr, value) => OAM[addr & 0x9F] = value);

			var HRAMDomain = new MemoryDomain("HRAM", 0x0080, Endian.Little,
				addr => HRam[addr & 0x007F],
				(addr, value) => HRam[addr & 0x0080] = value);

			var VRAMDomain = new MemoryDomain("VRAM", 0x2000, Endian.Little,
				addr => VRam[addr & 0x1FFF],
				(addr, value) => VRam[addr & 0x1FFF] = value);

			domains.Add(WRAM0Domain);
			domains.Add(WRAM1Domain);
			domains.Add(WRAMADomain);
			domains.Add(SRAMDomain);
			domains.Add(VRAMDomain);
			domains.Add(OAMDomain);
			domains.Add(HRAMDomain);
			domains.Add(SystemBusDomain);

			memoryDomains = domains.AsReadOnly();
		}

		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public byte ReadMemoryBios(ushort addr)
		{
			//we speculate that the bios unmaps itself after the first read of 0x100
			if (addr < 0x100)
				return Bios[addr];
			else if (addr == 0x100)
				DetachBios();
			return ReadMemory(addr);
		}

		public string DescribeParagraph(ushort addr)
		{
			//todo - later on, this must say "RO1F" for bank 0x1F and etc.
			//and so it must call through to the mapper
			if (addr < 0x4000)
				return "ROM0";
			else if (addr < 0x8000)
				return "ROM1";
			else if (addr < 0xA000)
				return "VRA0";
			else if (addr < 0xC000)
				return "SRA0";
			else if (addr < 0xD000)
				return "WRA0";
			else if (addr < 0xE000)
				return "WRA1";
			else if (addr < 0xFE00)
				return "ECH0";
			else if (addr < 0xFEA0)
				return "OAM ";
			else if (addr < 0xFF00)
				return "----";
			else if (addr < 0xFF80)
				return "I/O ";
			else return "HRAM";
		}

		public byte ReadMemory(ushort addr)
		{
			if (addr < 0x8000)
				return Rom[addr];
			else if (addr < 0xA000)
				return VRam[addr - 0x8000];
			else if (addr < 0xC000)
				return SRam[addr - 0xA000];
			else if (addr < 0xD000)
				return WRam[addr - 0xC000]; //bank 0 of WRam
			else if (addr < 0xE000)
				return WRam[addr - 0xC000]; //bank 1 of WRam (needs to be switchable)
			else if (addr < 0xFE00)
				return ReadMemory((ushort)(addr - 0xE000)); //echo of WRam; unusable; reserved ????
			else if (addr < 0xFEA0)
				return OAM[addr - 0xFE00];
			else if (addr < 0xFF00)
				return 0xFF;  //"unusable memory"
			else if (addr < 0xFF80)
				return ReadRegister(addr);
			else if (addr < 0xFFFF)
				return HRam[addr - 0xFF80];
			else return ReadRegister(addr);
		}

		public byte ReadRegister(ushort addr)
		{
			switch (addr)
			{
				case 0xFF00: //REG_P1 - Register for reading joy pad info and determining system type.	(R/W)
					return Registers.Input.Read();
				case 0xFF01: //REG_SB - Serial transfer data (R/W)
					return 0xFF;
				case 0xFF02: //REG_SC - SIO control  (R/W)
					return 0xFF;
				case 0xFF04: //REG_DIV - Divider Register (R/W)
					return 0xFF;
				case 0xFF05: //REG_TIMA - Timer counter (R/W)
					return 0xFF;
				case 0xFF06: //REG_TMA - Timer Modulo (R/W)
					return 0xFF;
				case 0xFF07: //REG_TAC - Timer Control (R/W)
					return 0xFF;
				case 0xFF0F: //REG_IF - Interrupt Flag (R/W)
					return 0xFF;
				case 0xFF10: //REG_NR10 - Sound Mode 1 register, Sweep register (R/W)
					return 0xFF;
				case 0xFF11: //REG_NR11 - Sound Mode 1 register, Sound length/Wave pattern duty (R/W)
					return 0xFF;
				case 0xFF12: //REG_NR12 - Sound Mode 1 register, Envelope (R/W)
					return 0xFF;
				case 0xFF13: //REG_NR13 - Sound Mode 1 register, Frequency lo (W)
					return 0xFF;
				case 0xFF14: //REG_NR14 - Sound Mode 1 register, Frequency hi (R/W)
					return 0xFF;

				//0xFF15 ???????????????

				case 0xFF16: //REG_NR21 - Sound Mode 2 register, Sound Length; Wave Pattern Duty (R/W)
					return 0xFF;
				case 0xFF17: //REG_NR22 - Sound Mode 2 register, envelope (R/W)
					return 0xFF;
				case 0xFF18: //REG_NR23 - Sound Mode 2 register, frequency lo data (W)
					return 0xFF;
				case 0xFF19: //REG_NR24 - Sound Mode 2 register, frequency hi data (R/W)
					return 0xFF;
				case 0xFF1A: //REG_NR30 - Sound Mode 3 register, Sound on/off (R/W)
					return 0xFF;
				case 0xFF1B: //REG_NR31 - Sound Mode 3 register, sound length (R/W)
					return 0xFF;
				case 0xFF1C: //REG_NR32 - Sound Mode 3 register, Select output level (R/W)
					return 0xFF;
				case 0xFF1D: //REG_NR33 - Sound Mode 3 register, frequency's lower data (W)
					return 0xFF;
				case 0xFF1E: //REG_NR34 - Sound Mode 3 register, frequency's higher data (R/W)
					return 0xFF;

				//0xFF1F ???????????????

				case 0xFF20: //REG_NR41 - Sound Mode 4 register, sound length (R/W)
					return 0xFF;
				case 0xFF21: //REG_NR42 - Sound Mode 4 register, envelope (R/W)
					return 0xFF;
				case 0xFF22: //REG_NR43 - Sound Mode 4 register, polynomial counter (R/W)
					return 0xFF;
				case 0xFF23: //REG_NR44 - Sound Mode 4 register, counter/consecutive; inital (R/W)
					return 0xFF;
				case 0xFF24: //REG_NR50 - Channel control / ON-OFF / Volume (R/W)
					return 0xFF;
				case 0xFF25: //REG_NR51 - Selection of Sound output terminal (R/W)
					return 0xFF;
				case 0xFF26: //REG_NR52 - Sound on/off (R/W) (Value at reset: $F1-GB, $F0-SGB)
					return 0xFF;
				case 0xFF30: return Sound.WavePatternRam[0x00];
				case 0xFF31: return Sound.WavePatternRam[0x01];
				case 0xFF32: return Sound.WavePatternRam[0x02];
				case 0xFF33: return Sound.WavePatternRam[0x03];
				case 0xFF34: return Sound.WavePatternRam[0x04];
				case 0xFF35: return Sound.WavePatternRam[0x05];
				case 0xFF36: return Sound.WavePatternRam[0x06];
				case 0xFF37: return Sound.WavePatternRam[0x07];
				case 0xFF38: return Sound.WavePatternRam[0x08];
				case 0xFF39: return Sound.WavePatternRam[0x09];
				case 0xFF3A: return Sound.WavePatternRam[0x0A];
				case 0xFF3B: return Sound.WavePatternRam[0x0B];
				case 0xFF3C: return Sound.WavePatternRam[0x0C];
				case 0xFF3D: return Sound.WavePatternRam[0x0D];
				case 0xFF3E: return Sound.WavePatternRam[0x0E];
				case 0xFF3F: return Sound.WavePatternRam[0x0F];
				case 0xFF40: //REG_LCDC - LCD Control (R/W) (value $91 at reset)
					return Registers.LCDC.Read();
				case 0xFF41: //REG_STAT - LCDC Status   (R/W)
					return Registers.STAT.Read();
				case 0xFF42: //REG_SCY - Scroll Y   (R/W)
					return Registers.SCY;
				case 0xFF43: //REG_SCX - Scroll X   (R/W)
					return Registers.SCX;
				case 0xFF44: //REG_LY - LCDC Y-Coordinate (R)
					return Registers.Read_LY();
				case 0xFF45: //REG_LYC - LY Compare  (R/W)
					return 0xFF;
				case 0xFF46: //REG_DMA - DMA Transfer and Start Address (W)
					return 0xFF;
				case 0xFF47: //REG_BGP - BG & Window Palette Data  (R/W)
					return 0xFF;
				case 0xFF48: //REG_OBP0 - Object Palette 0 Data (R/W)
					return 0xFF;
				case 0xFF49: //REG_OBP1 - Object Palette 1 Data (R/W)
					return 0xFF;
				case 0xFF4A: //REG_WY - Window Y Position  (R/W)
					return 0xFF;
				case 0xFF4B: //REG_WX - Window X Position  (R/W)
					return 0xFF;
				case 0xFFFF: //REG_IE
					return 0xFF;
				default:
					return 0xFF;
			}
		}

		public void WriteRegister(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0xFF00: //REG_P1 - Register for reading joy pad info and determining system type.	(R/W)
					Registers.Input.Write(value);
					break;
				case 0xFF01: //REG_SB - Serial transfer data (R/W)
					return;
				case 0xFF02: //REG_SC - SIO control  (R/W)
					return;
				case 0xFF04: //REG_DIV - Divider Register (R/W)
					return;
				case 0xFF05: //REG_TIMA - Timer counter (R/W)
					return;
				case 0xFF06: //REG_TMA - Timer Modulo (R/W)
					return;
				case 0xFF07: //REG_TAC - Timer Control (R/W)
					return;
				case 0xFF0F: //REG_IF - Interrupt Flag (R/W)
					return;
				case 0xFF10: //REG_NR10 - Sound Mode 1 register, Sweep register (R/W)
					return;
				case 0xFF11: //REG_NR11 - Sound Mode 1 register, Sound length/Wave pattern duty (R/W)
					return;
				case 0xFF12: //REG_NR12 - Sound Mode 1 register, Envelope (R/W)
					return;
				case 0xFF13: //REG_NR13 - Sound Mode 1 register, Frequency lo (W)
					return;
				case 0xFF14: //REG_NR14 - Sound Mode 1 register, Frequency hi (R/W)
					return;

				//0xFF15 ???????????????

				case 0xFF16: //REG_NR21 - Sound Mode 2 register, Sound Length; Wave Pattern Duty (R/W)
					return;
				case 0xFF17: //REG_NR22 - Sound Mode 2 register, envelope (R/W)
					return;
				case 0xFF18: //REG_NR23 - Sound Mode 2 register, frequency lo data (W)
					return;
				case 0xFF19: //REG_NR24 - Sound Mode 2 register, frequency hi data (R/W)
					return;
				case 0xFF1A: //REG_NR30 - Sound Mode 3 register, Sound on/off (R/W)
					return;
				case 0xFF1B: //REG_NR31 - Sound Mode 3 register, sound length (R/W)
					return;
				case 0xFF1C: //REG_NR32 - Sound Mode 3 register, Select output level (R/W)
					return;
				case 0xFF1D: //REG_NR33 - Sound Mode 3 register, frequency's lower data (W)
					return;
				case 0xFF1E: //REG_NR34 - Sound Mode 3 register, frequency's higher data (R/W)
					return;

				//0xFF1F ???????????????

				case 0xFF20: //REG_NR41 - Sound Mode 4 register, sound length (R/W)
					return;
				case 0xFF21: //REG_NR42 - Sound Mode 4 register, envelope (R/W)
					return;
				case 0xFF22: //REG_NR43 - Sound Mode 4 register, polynomial counter (R/W)
					return;
				case 0xFF23: //REG_NR44 - Sound Mode 4 register, counter/consecutive; inital (R/W)
					return;
				case 0xFF24: //REG_NR50 - Channel control / ON-OFF / Volume (R/W)
					return;
				case 0xFF25: //REG_NR51 - Selection of Sound output terminal (R/W)
					return;
				case 0xFF26: //REG_NR52 - Sound on/off (R/W) (Value at reset: $F1-GB, $F0-SGB)
					return;
				case 0xFF30: Sound.WavePatternRam[0x00] = value; break;
				case 0xFF31: Sound.WavePatternRam[0x01] = value; break;
				case 0xFF32: Sound.WavePatternRam[0x02] = value; break;
				case 0xFF33: Sound.WavePatternRam[0x03] = value; break;
				case 0xFF34: Sound.WavePatternRam[0x04] = value; break;
				case 0xFF35: Sound.WavePatternRam[0x05] = value; break;
				case 0xFF36: Sound.WavePatternRam[0x06] = value; break;
				case 0xFF37: Sound.WavePatternRam[0x07] = value; break;
				case 0xFF38: Sound.WavePatternRam[0x08] = value; break;
				case 0xFF39: Sound.WavePatternRam[0x09] = value; break;
				case 0xFF3A: Sound.WavePatternRam[0x0A] = value; break;
				case 0xFF3B: Sound.WavePatternRam[0x0B] = value; break;
				case 0xFF3C: Sound.WavePatternRam[0x0C] = value; break;
				case 0xFF3D: Sound.WavePatternRam[0x0D] = value; break;
				case 0xFF3E: Sound.WavePatternRam[0x0E] = value; break;
				case 0xFF3F: Sound.WavePatternRam[0x0F] = value; break;
				case 0xFF40: //REG_LCDC - LCD Control (R/W) (value $91 at reset)
					Registers.LCDC.Write(value);
					break;
				case 0xFF41: //REG_STAT - LCDC Status   (R/W)
					return;
				case 0xFF42: //REG_SCY - Scroll Y   (R/W)
					Registers.SCY = value;
					break;
				case 0xFF43: //REG_SCX - Scroll X   (R/W)
					Registers.SCX = value;
					break;
				case 0xFF44: //REG_LY - LCDC Y-Coordinate (R)
					return;
				case 0xFF45: //REG_LYC - LY Compare  (R/W)
					return;
				case 0xFF46: //REG_DMA - DMA Transfer and Start Address (W)
					return;
				case 0xFF47: //REG_BGP - BG & Window Palette Data  (R/W)
					return;
				case 0xFF48: //REG_OBP0 - Object Palette 0 Data (R/W)
					return;
				case 0xFF49: //REG_OBP1 - Object Palette 1 Data (R/W)
					return;
				case 0xFF4A: //REG_WY - Window Y Position  (R/W)
					return;
				case 0xFF4B: //REG_WX - Window X Position  (R/W)
					return;
				case 0xFFFF: //REG_IE
					return;
				default:
					return;
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x8000)
				return;
			else if (addr < 0xA000)
				VRam[addr - 0x8000] = value;
			else if (addr < 0xC000)
				SRam[addr - 0xA000] = value;
			else if (addr < 0xD000)
				WRam[addr - 0xC000] = value;
			else if (addr < 0xE000)
				WRam[addr - 0xC000] = value;
			else if (addr < 0xFE00)
				WriteMemory((ushort)(addr - 0xE000), value);
			else if (addr < 0xFEA0)
				OAM[addr - 0xFE00] = value;
			else if (addr < 0xFF00)
				return;
			else if (addr < 0xFF80)
				WriteRegister(addr, value);
			else if (addr < 0xFFFF)
				HRam[addr - 0xFF80] = value;
			else WriteRegister(addr, value);
		}

		public void FrameAdvance(bool render)
		{
			Controller.UpdateControls(Frame++);

			for (int i = 0; i < 70224; i++)
				SingleStepInto();

			//to make sure input is working
			Console.WriteLine(Controller.IsPressed("Up"));
		}

		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }

		public IVideoProvider VideoProvider
		{
			get { return this; }
		}

		public int[] GetVideoBuffer()
		{
			//TODO - these need to be run once per scanline and accumulated into a 160*144 byte buffer held by the core
			//then, in the call to GetVideoBuffer(), it gets adapted to gray according to the palette and returned
			//(i.e. no real GB logic happens during GetVideoBuffer())
			int[] buf = new int[160 * 144];
			var linebuf = new byte[160];
			int i = 0;
			for (int y = 0; y < 144; y++)
			{
				RenderBGLine(y, linebuf, true);
				RenderOBJLine(y, linebuf, true);
				for (int x = 0; x < 160; x++)
				{
					int gray = 0x000000;
					switch (linebuf[x])
					{
						case 0:
							gray = 0xFFFFFF;
							break;
						case 1:
							gray = 0xC0C0C0;
							break;
						case 2:
							gray = 0x606060;
							break;
					}
					buf[i++] = unchecked(gray | (int)0xFF000000);
				}
			}
			return buf;
		}

        public int VirtualWidth { get { return 160; } }
        public int BufferWidth { get { return 160; } }
		public int BufferHeight { get { return 144; } }
		public int BackgroundColor { get { return 0; } }

		public ISoundProvider SoundProvider
		{
			get { return new NullEmulator(); }
		}

		public int Frame { get; set; }

		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }

		public byte[] SaveRam
		{
			get { throw new NotImplementedException(); }
		}

		public bool SaveRamModified
		{
			get
			{
				return false;
			}
			set
			{

			}
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			throw new NotImplementedException();
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{

		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public void RenderOBJLine(int line, byte[] output, bool limit)
		{
			int height = Registers.LCDC.ObjSize == TRegisters.TLCDC.EObjSize.ObjSize_8x16 ? 16 : 8;
			List<int> sprites = new List<int>();

			//1st pass: select sprites to draw
			for (int s = 0; s < 40; s++)
			{
				int y = OAM[s * 4 + 0];
				y -= 16;
				if (line < y) continue;
				if (line < y + height) continue;

				sprites.Add(s);
				if (sprites.Count == 10 && limit) break; //sprite limit
			}

			//now render from low priority to high
			for (int i = sprites.Count - 1; i >= 0; i--)
			{
				int s = sprites[i];
				int y = OAM[s * 4 + 0];
				int x = OAM[s * 4 + 1];
				int pat = OAM[s * 4 + 2];
				byte flags = OAM[s * 4 + 3];
				bool priority = GetBit8(flags, 7);
				bool yflip = GetBit8(flags, 6);
				bool xflip = GetBit8(flags, 5);
				bool pal = GetBit8(flags, 4);

				y -= 16;
				x -= 8;

				int sprline = line - y;
				if (yflip)
					sprline = height - sprline - 1;

				if (height == 16) pat = ~1;

				ushort patternAddr = (ushort)(0x8000 + (pat << 4));
				patternAddr += (ushort)(sprline << 1);

				int _lobits = ReadMemory(patternAddr);
				patternAddr += 1;
				int _hibits = ReadMemory(patternAddr);

				for (int j = 0; j < 8; j++)
				{
					int px = x + j;
					if (px < 0) continue;
					if (px >= 160) continue;

					int lobits;
					int hibits;

					if (xflip)
					{
						lobits = _lobits >> (j);
						hibits = _hibits >> (j);
					}
					else
					{
						lobits = _lobits >> (7 - j);
						hibits = _hibits >> (7 - j);
					}
					lobits &= 1;
					hibits &= 1;
					int pixel = lobits | (hibits << 1);
					output[x] = (byte)pixel;
				}
			}

		}

		public void RenderTileLine(int line, byte[] output, ushort tiledata)
		{
			int py = line;
			int ty = py >> 3;
			int tyr = py & 7;
			int tileRowOffset = ty << 5;

			for (int x = 0; x < 128; x++)
			{
				int px = x;
				px &= 0xFF;
				int tx = px >> 3;
				int txr = px & 7;
				int tileOffset = tileRowOffset + tx;
				int tileAddr = tileOffset;
				int tileNum = ty * 16 + tx;
				tileNum = (tileNum) & 0xFF;
				ushort patternAddr = (ushort)(tiledata + (tileNum << 4));
				patternAddr += (ushort)(tyr << 1);

				int lobits = ReadMemory(patternAddr);
				patternAddr += 1;
				int hibits = ReadMemory(patternAddr);
				lobits >>= (7 - txr);
				hibits >>= (7 - txr);
				lobits &= 1;
				hibits &= 1;
				int pixel = lobits | (hibits << 1);
				output[x] = (byte)pixel;
			}
		}

		public void RenderBGLine(int line, byte[] output, bool scroll)
		{
			ushort tilemap = Registers.LCDC.BgTileMapAddr;
			ushort tiledata = Registers.LCDC.TileDataAddr;

			int tileAdjust = (Registers.LCDC.TileData == TRegisters.TLCDC.ETileData.Region_8800_97FF ? 128 : 0);

			int py = line;
			if (scroll) line += Registers.SCY;
			py &= 0xFF;
			int ty = py >> 3;
			int tyr = py & 7;
			int tileRowOffset = ty << 5;

			for (int x = 0; x < 160; x++)
			{
				int px = x;
				if (scroll) px += Registers.SCX;
				px &= 0xFF;
				int tx = px >> 3;
				int txr = px & 7;
				int tileOffset = tileRowOffset + tx;
				int tileAddr = tilemap + tileOffset;
				int tileNum = ReadMemory((ushort)tileAddr);
				tileNum = (tileNum + tileAdjust) & 0xFF;
				ushort patternAddr = (ushort)(tiledata + (tileNum << 4));
				patternAddr += (ushort)(tyr << 1);

				int lobits = ReadMemory(patternAddr);
				patternAddr += 1;
				int hibits = ReadMemory(patternAddr);
				lobits >>= (7 - txr);
				hibits >>= (7 - txr);
				lobits &= 1;
				hibits &= 1;
				int pixel = lobits | (hibits << 1);
				output[x] = (byte)pixel;
			}

		}

		public bool DeterministicEmulation { get; set; }
		public string SystemId { get { return "GB"; } }

		public void Dispose() { }
	}
}