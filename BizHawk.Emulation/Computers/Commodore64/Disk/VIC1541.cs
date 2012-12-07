using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Computers.Commodore64.MOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Disk
{
	public class VIC1541
	{
		public Action<SerialPort> Connect;
		public Action Execute;
		public Action HardReset;

		public Func<int, byte> Peek;
		public Func<int, byte> PeekRom;
		public Func<int, byte> PeekRam;
		public Func<int, byte> PeekVia0;
		public Func<int, byte> PeekVia1;
		public Action<int, byte> Poke;
		public Action<int, byte> PokeRam;
		public Action<int, byte> PokeRom;
		public Action<int, byte> PokeVia0;
		public Action<int, byte> PokeVia1;
		public Func<ushort, byte> Read;
		public Func<ushort, byte> ReadRam;
		public Func<ushort, byte> ReadRom;
		public Func<ushort, byte> ReadVia0;
		public Func<ushort, byte> ReadVia1;
		public Action<ushort, byte> Write;
		public Action<ushort, byte> WriteRam;
		public Action<ushort, byte> WriteRom;
		public Action<ushort, byte> WriteVia0;
		public Action<ushort, byte> WriteVia1;

		public VIC1541Motherboard board;

		public VIC1541(Region initRegion, byte[] rom)
		{
			board = new VIC1541Motherboard(initRegion, rom);
			Connect = board.Connect;
			Execute = board.Execute;
			HardReset = board.HardReset;

			Peek = board.pla.Peek;
			PeekRam = board.pla.PeekRam;
			PeekRom = board.pla.PeekRom;
			PeekVia0 = board.pla.PeekVia0;
			PeekVia1 = board.pla.PeekVia1;
			Poke = board.pla.Poke;
			PokeRam = board.pla.PokeRam;
			PokeRom = board.pla.PokeRom;
			PokeVia0 = board.pla.PokeVia0;
			PokeVia1 = board.pla.PokeVia1;
			Read = board.pla.Read;
			ReadRam = board.pla.ReadRam;
			ReadRom = board.pla.ReadRom;
			ReadVia0 = board.pla.ReadVia0;
			ReadVia1 = board.pla.ReadVia1;
			Write = board.pla.Write;
			WriteRam = board.pla.WriteRam;
			WriteRom = board.pla.WriteRom;
			WriteVia0 = board.pla.WriteVia0;
			WriteVia1 = board.pla.WriteVia1;
		}
	}

	// because the VIC1541 doesn't have bank switching like the system does,
	// we simplify things by processing the rom bytes directly.

	public class VIC1541Motherboard
	{
		public MOS6502X cpu;
		public VIC1541PLA pla;
		public byte[] ram;
		public byte[] rom;
		public SerialPort serPort;
		public MOS6522 via0;
		public MOS6522 via1;

		public bool via0CA0;
		public bool via0CA1;
		public bool via0CB0;
		public bool via0CB1;
		public byte via0DataA;
		public byte via0DataB;
		public byte via0DirA;
		public byte via0DirB;
		public bool via1CA0;
		public bool via1CA1;
		public bool via1CB0;
		public bool via1CB1;
		public byte via1DataA;
		public byte via1DataB;
		public byte via1DirA;
		public byte via1DirB;

		public VIC1541Motherboard(Region initRegion, byte[] initRom)
		{
			cpu = new MOS6502X();
			pla = new VIC1541PLA();
			ram = new byte[0x800];
			rom = initRom;
			serPort = new SerialPort();
			via0 = new MOS6522();
			via1 = new MOS6522();

			cpu.DummyReadMemory = pla.Read;
			cpu.ReadMemory = pla.Read;
			cpu.WriteMemory = pla.Write;

			pla.PeekRam = ((int addr) => { return ram[addr & 0x07FF]; });
			pla.PeekRom = ((int addr) => { return rom[addr & 0x3FFF]; });
			pla.PeekVia0 = via0.Peek;
			pla.PeekVia1 = via1.Peek;
			pla.PokeRam = ((int addr, byte val) => { ram[addr & 0x07FF] = val; });
			pla.PokeRom = ((int addr, byte val) => { });
			pla.PokeVia0 = via0.Poke;
			pla.PokeVia1 = via1.Poke;
			pla.ReadRam = ((ushort addr) => { return ram[addr & 0x07FF]; });
			pla.ReadRom = ((ushort addr) => { return rom[addr & 0x3FFF]; });
			pla.ReadVia0 = via0.Read;
			pla.ReadVia1 = via1.Read;
			pla.WriteRam = ((ushort addr, byte val) => { ram[addr & 0x07FF] = val; });
			pla.WriteRom = ((ushort addr, byte val) => { });
			pla.WriteVia0 = via0.Write;
			pla.WriteVia1 = via1.Write;

			via0CA0 = false;
			via0CA1 = false;
			via0CB0 = false;
			via0CB1 = false;
			via0DirA = 0x00;
			via0DirB = 0x00;
			via0DataA = 0xFF;
			via0DataB = 0xFF;
			via1CA0 = false;
			via1CA1 = false;
			via1CB0 = false;
			via1CB1 = false;
			via1DirA = 0x00;
			via1DirB = 0x00;
			via1DataA = 0xFF;
			via1DataB = 0xFF;

			via0.ReadCA0 = (() => { return via0CA0; });
			via0.ReadCA1 = (() => { return via0CA1; });
			via0.ReadCB0 = (() => { return via0CB0; });
			via0.ReadCB1 = (() => { return via0CB1; });
			via0.ReadDirA = (() => { return via0DirA; });
			via0.ReadDirB = (() => { return via0DirB; });
			via0.ReadPortA = (() => { return via0DataA; });
			via0.ReadPortB = (() => { return via0DataB; });
			via0.WriteCA0 = ((bool val) => { via0CA0 = val; });
			via0.WriteCA1 = ((bool val) => { via0CA1 = val; });
			via0.WriteCB0 = ((bool val) => { via0CB0 = val; });
			via0.WriteCB1 = ((bool val) => { via0CB1 = val; });
			via0.WriteDirA = ((byte val) => { via0DirA = val; });
			via0.WriteDirB = ((byte val) => { via0DirB = val; });
			via0.WritePortA = ((byte val) => {
				via0DataA = Port.CPUWrite(via0DataA, val, via0DirA);
			});
			via0.WritePortB = ((byte val) => {
				via0DataB = Port.CPUWrite(via0DataB, val, via0DirB);
				serPort.DeviceWriteAtn((via0DataB & 0x80) != 0);
				serPort.DeviceWriteClock((via0DataB & 0x08) != 0);
				serPort.DeviceWriteData((via0DataB & 0x02) != 0);
			});

			via1.ReadCA0 = (() => { return via1CA0; });
			via1.ReadCA1 = (() => { return via1CA1; });
			via1.ReadCB0 = (() => { return via1CB0; });
			via1.ReadCB1 = (() => { return via1CB1; });
			via1.ReadDirA = (() => { return via1DirA; });
			via1.ReadDirB = (() => { return via1DirB; });
			via1.ReadPortA = (() => { return via1DataA; });
			via1.ReadPortB = (() => { return via1DataB; });
			via1.WriteCA0 = ((bool val) => { via1CA0 = val; });
			via1.WriteCA1 = ((bool val) => { via1CA1 = val; });
			via1.WriteCB0 = ((bool val) => { via1CB0 = val; });
			via1.WriteCB1 = ((bool val) => { via1CB1 = val; });
			via1.WriteDirA = ((byte val) => { via1DirA = val; });
			via1.WriteDirB = ((byte val) => { via1DirB = val; });
			via1.WritePortA = ((byte val) => { via1DataA = Port.CPUWrite(via1DataA, val, via1DirA); });
			via1.WritePortB = ((byte val) => { via1DataB = Port.CPUWrite(via1DataB, val, via1DirB); });	
		}

		public void Connect(SerialPort newSerPort)
		{
			// TODO: verify polarity
			serPort = newSerPort;
			serPort.SystemReadAtn = (() => { 
				return true;
			});
			serPort.SystemReadClock = (() => { 
				return ((via0DataB & 0x08) != 0); 
			}); // bit 3
			serPort.SystemReadData = (() => { 
				return ((via0DataB & 0x02) != 0); 
			}); // bit 1
			serPort.SystemReadSrq = (() => { 
				return false; 
			}); // device sensing
			serPort.SystemWriteAtn = ((bool val) => {
				via0DataB = Port.ExternalWrite(via0DataB, (byte)((via0DataB & 0x7F) | (val ? 0x80 : 0x00)), via0DataB);
				via0CA0 = val;
				// repeat to DATA OUT if bit 4 enabled on port B
				if ((via0DataB & 0x10) != 0)
					serPort.DeviceWriteData(val);
			});
			serPort.SystemWriteClock = ((bool val) => {
				via0DataB = Port.ExternalWrite(via0DataB, (byte)((via0DataB & 0xFB) | (val ? 0x04 : 0x00)), via0DataB);
			});
			serPort.SystemWriteData = ((bool val) => {
				via0DataB = Port.ExternalWrite(via0DataB, (byte)((via0DataB & 0xFE) | (val ? 0x01 : 0x00)), via0DataB);
			});
			serPort.SystemWriteReset = ((bool val) => { });

			serPort.DeviceWriteSrq(false);
		}

		public void Execute()
		{
			via0.ExecutePhase1();
			via1.ExecutePhase1();

			cpu.IRQ = !(via0.IRQ && via1.IRQ);
			cpu.ExecuteOne();
			via0.ExecutePhase2();
			via1.ExecutePhase2();
		}

		public void HardReset()
		{
			for (uint i = 0; i < 0x7FF; i++)
				ram[i] = 0x00;
			cpu.PC = (ushort)(cpu.ReadMemory(0xFFFC) | ((ushort)cpu.ReadMemory(0xFFFD) << 8));
			via0.HardReset();
			via1.HardReset();
		}
	}
}
