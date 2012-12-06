using BizHawk.Emulation.Computers.Commodore64.MOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Motherboard
	{
		// chips
		public Chip23XX basicRom;
		public Chip23XX charRom;
		public MOS6526 cia0;
		public MOS6526 cia1;
		public Chip2114 colorRam;
		public MOS6510 cpu;
		public Chip23XX kernalRom;
		public MOSPLA pla;
		public Chip4864 ram;
		public Sid sid;
		public Vic vic;

		// ports
		public CartridgePort cartPort;
		public CassettePort cassPort;
		public IController controller;
		public SerialPort serPort;
		public UserPort userPort;

		// state
		public ushort address;
		public byte bus;
		public byte cia0DataA;
		public byte cia0DataB;
		public byte cia0DirA;
		public byte cia0DirB;
		public bool cia0FlagCassette;
		public bool cia0FlagSerial;
		public byte cia1DataA;
		public byte cia1DataB;
		public byte cia1DirA;
		public byte cia1DirB;
		public bool inputRead;

		// cache
		private ushort vicBank;

		public Motherboard(Region initRegion)
		{
			// note: roms need to be added on their own externally

			cartPort = new CartridgePort();
			cassPort = new CassettePort();
			cia0 = new MOS6526(initRegion);
			cia1 = new MOS6526(initRegion);
			colorRam = new Chip2114();
			cpu = new MOS6510();
			pla = new MOSPLA();
			ram = new Chip4864();
			serPort = new SerialPort();
			sid = new MOS6581(44100, initRegion);
			switch (initRegion)
			{
				case Region.NTSC: vic = new MOS6567(); break;
				case Region.PAL: vic = new MOS6569(); break;
			}
			userPort = new UserPort();
		}

		// -----------------------------------------

		public void Execute(uint count)
		{
			while (count > 0)
			{
				WriteInputPort();

				cia0.ExecutePhase1();
				cia1.ExecutePhase1();
				pla.ExecutePhase1();
				sid.ExecutePhase1();
				vic.ExecutePhase1();
				cpu.ExecutePhase1();

				cia0.ExecutePhase2();
				cia1.ExecutePhase2();
				pla.ExecutePhase2();
				sid.ExecutePhase2();
				vic.ExecutePhase2();
				cpu.ExecutePhase2();
				
				count--;
			}
		}

		// -----------------------------------------

		public void HardReset()
		{
			address = 0xFFFF;
			bus = 0xFF;
			cia0DataA = 0xFF;
			cia0DataB = 0xFF;
			cia0DirA = 0xFF;
			cia0DirB = 0xFF;
			cia0FlagCassette = true;
			cia0FlagSerial = true;
			cia1DataA = 0xFF;
			cia1DataB = 0xFF;
			cia1DirA = 0xFF;
			cia1DirB = 0xFF;
			inputRead = false;

			cpu.HardReset();
			cia0.HardReset();
			cia1.HardReset();
			colorRam.HardReset();
			pla.HardReset();
			ram.HardReset();
			serPort.HardReset();
			sid.HardReset();
			vic.HardReset();
			userPort.HardReset();

			// because of how mapping works, the cpu needs to be hard reset twice
			cpu.HardReset();

			// now reset the cache
			UpdateVicBank();
		}

		public void Init()
		{
			cassPort.DeviceReadLevel = (() => { return cpu.CassetteOutputLevel; });
			cassPort.DeviceReadMotor = (() => { return cpu.CassetteMotor; });
			cassPort.DeviceWriteButton = ((bool val) => { cpu.CassetteButton = val; });
			cassPort.DeviceWriteLevel = ((bool val) => { cia0FlagCassette = val; cia0.FLAG = cia0FlagCassette & cia0FlagSerial; });
			cassPort.SystemReadButton = (() => { return true; });
			cassPort.SystemReadLevel = (() => { return true; });
			cassPort.SystemWriteLevel = ((bool val) => { });
			cassPort.SystemWriteMotor = ((bool val) => { });

			cia0.ReadDirA = (() => { return cia0DirA; });
			cia0.ReadDirB = (() => { return cia0DirB; });
			cia0.ReadPortA = (() => { return cia0DataA; });
			cia0.ReadPortB = (() => { return cia0DataB; });
			cia0.WriteDirA = ((byte val) => { cia0DirA = val; });
			cia0.WriteDirB = ((byte val) => { cia0DirB = val; });
			cia0.WritePortA = ((byte val) => { cia0DataA = Port.CPUWrite(cia0DataA, val, cia0DirA); });
			cia0.WritePortB = ((byte val) => { cia0DataB = Port.CPUWrite(cia0DataB, val, cia0DirB); });

			cia1.ReadDirA = (() => { return cia1DirA; });
			cia1.ReadDirB = (() => { return cia1DirB; });
			cia1.ReadPortA = (() => { return cia1DataA; });
			cia1.ReadPortB = (() => { return cia1DataB; });
			cia1.WriteDirA = ((byte val) => { cia1DirA = val; });
			cia1.WriteDirB = ((byte val) => { cia1DirB = val; });
			cia1.WritePortA = ((byte val) => { cia1DataA = Port.CPUWrite(cia1DataA, val, cia1DirA); UpdateVicBank(); });
			cia1.WritePortB = ((byte val) => { cia1DataB = Port.CPUWrite(cia1DataB, val, cia1DirB); });

			cpu.PeekMemory = pla.Peek;
			cpu.PokeMemory = pla.Poke;
			cpu.ReadAEC = (() => { return vic.AEC; });
			cpu.ReadIRQ = (() => { return cia0.IRQ & vic.IRQ & cartPort.IRQ; });
			cpu.ReadNMI = (() => { return cia1.IRQ; });
			cpu.ReadRDY = (() => { return vic.BA; });
			cpu.ReadMemory = pla.Read;
			cpu.WriteMemory = pla.Write;

			pla.PeekBasicRom = basicRom.Peek;
			pla.PeekCartridgeHi = cartPort.PeekHiRom;
			pla.PeekCartridgeLo = cartPort.PeekLoRom;
			pla.PeekCharRom = charRom.Peek;
			pla.PeekCia0 = cia0.Peek;
			pla.PeekCia1 = cia1.Peek;
			pla.PeekColorRam = colorRam.Peek;
			pla.PeekExpansionHi = cartPort.PeekHiExp;
			pla.PeekExpansionLo = cartPort.PeekLoExp;
			pla.PeekKernalRom = kernalRom.Peek;
			pla.PeekMemory = ram.Peek;
			pla.PeekSid = sid.Peek;
			pla.PeekVic = vic.Peek;
			pla.PokeCartridgeHi = cartPort.PokeHiRom;
			pla.PokeCartridgeLo = cartPort.PokeLoRom;
			pla.PokeCia0 = cia0.Poke;
			pla.PokeCia1 = cia1.Poke;
			pla.PokeColorRam = colorRam.Poke;
			pla.PokeExpansionHi = cartPort.PokeHiExp;
			pla.PokeExpansionLo = cartPort.PokeLoExp;
			pla.PokeMemory = ram.Poke;
			pla.PokeSid = sid.Poke;
			pla.PokeVic = vic.Poke;
			pla.ReadBasicRom = ((ushort addr) => { address = addr; bus = basicRom.Read(addr); return bus; });
			pla.ReadCartridgeHi = ((ushort addr) => { address = addr; bus = cartPort.ReadHiRom(addr); return bus; });
			pla.ReadCartridgeLo = ((ushort addr) => { address = addr; bus = cartPort.ReadLoRom(addr); return bus; });
			pla.ReadCharen = (() => { return cpu.Charen; });
			pla.ReadCharRom = ((ushort addr) => { address = addr; bus = charRom.Read(addr); return bus; });
			pla.ReadCia0 = ((ushort addr) =>
			{
				address = addr;
				bus = cia0.Read(addr);
				if (!inputRead && (addr == 0xDC00 || addr == 0xDC01))
					inputRead = true;
				return bus;
			});
			pla.ReadCia1 = ((ushort addr) => { address = addr; bus = cia1.Read(addr); return bus; });
			pla.ReadColorRam = ((ushort addr) => { address = addr; bus &= 0xF0; bus |= colorRam.Read(addr); return bus; });
			pla.ReadExpansionHi = ((ushort addr) => { address = addr; bus = cartPort.ReadHiExp(addr); return bus; });
			pla.ReadExpansionLo = ((ushort addr) => { address = addr; bus = cartPort.ReadLoExp(addr); return bus; });
			pla.ReadExRom = (() => { return cartPort.ExRom; });
			pla.ReadGame = (() => { return cartPort.Game; });
			pla.ReadHiRam = (() => { return cpu.HiRam; });
			pla.ReadKernalRom = ((ushort addr) => { address = addr; bus = kernalRom.Read(addr); return bus; });
			pla.ReadLoRam = (() => { return cpu.LoRam; });
			pla.ReadMemory = ((ushort addr) => { address = addr; bus = ram.Read(addr); return bus; });
			pla.ReadSid = ((ushort addr) => { address = addr; bus = sid.Read(addr); return bus; });
			pla.ReadVic = ((ushort addr) => { address = addr; bus = vic.Read(addr); return bus; });
			pla.WriteCartridgeHi = ((ushort addr, byte val) => { address = addr; bus = val; cartPort.WriteHiRom(addr, val); });
			pla.WriteCartridgeLo = ((ushort addr, byte val) => { address = addr; bus = val; cartPort.WriteLoRom(addr, val); });
			pla.WriteCia0 = ((ushort addr, byte val) => { address = addr; bus = val; cia0.Write(addr, val); });
			pla.WriteCia1 = ((ushort addr, byte val) => { address = addr; bus = val; cia1.Write(addr, val); });
			pla.WriteColorRam = ((ushort addr, byte val) => { address = addr; bus = val; colorRam.Write(addr, val); });
			pla.WriteExpansionHi = ((ushort addr, byte val) => { address = addr; bus = val; cartPort.WriteHiExp(addr, val); });
			pla.WriteExpansionLo = ((ushort addr, byte val) => { address = addr; bus = val; cartPort.WriteLoExp(addr, val); });
			pla.WriteMemory = ((ushort addr, byte val) => { address = addr; bus = val; ram.Write(addr, val); });
			pla.WriteSid = ((ushort addr, byte val) => { address = addr; bus = val; sid.Write(addr, val); });
			pla.WriteVic = ((ushort addr, byte val) => { address = addr; bus = val; vic.Write(addr, val); });

			serPort.DeviceReadAtn = (() => { return (cia1DataA & 0x08) != 0; });
			serPort.DeviceReadClock = (() => { return (cia1DataA & 0x10) != 0; });
			serPort.DeviceReadData = (() => { return (cia1DataA & 0x20) != 0; });
			serPort.DeviceReadReset = (() => { return true; }); // this triggers hard reset on ext device when low
			serPort.DeviceWriteAtn = ((bool val) => { }); // currently not wired
			serPort.DeviceWriteClock = ((bool val) => { cia1DataA = Port.ExternalWrite(cia1DataA, (byte)(cia1DataA | (val ? 0x40 : 0x00)), cia1DirA); });
			serPort.DeviceWriteData = ((bool val) => { cia1DataA = Port.ExternalWrite(cia1DataA, (byte)(cia1DataA | (val ? 0x80 : 0x00)), cia1DirA); });
			serPort.DeviceWriteSrq = ((bool val) => { cia0FlagSerial = val; cia0.FLAG = cia0FlagCassette & cia0FlagSerial; });

			sid.ReadPotX = (() => { return 0; });
			sid.ReadPotY = (() => { return 0; });

			vic.ReadMemory = ((ushort addr) =>
			{
				addr |= vicBank;
				address = addr;
				if ((addr & 0x7000) == 0x1000)
					bus = charRom.Read(addr);
				else
					bus = ram.Read(addr);
				return bus;
			});
			vic.ReadColorRam = ((ushort addr) =>
			{
				address = addr; 
				bus &= 0xF0; 
				bus |= colorRam.Read(addr); 
				return bus; 
			});
		}

		public void SyncState(Serializer ser)
		{
		}

		private void UpdateVicBank()
		{
			switch (cia1DataA & 0x3)
			{
				case 0: vicBank = 0xC000; break;
				case 1: vicBank = 0x8000; break;
				case 2: vicBank = 0x4000; break;
				default: vicBank = 0x0000; break;
			}
		}
	}
}
