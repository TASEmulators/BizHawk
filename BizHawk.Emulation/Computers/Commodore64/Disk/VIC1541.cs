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

		public byte via0dirA;
		public byte via0dirB;
		public byte via0portA;
		public byte via0portB;
		public byte via1dirA;
		public byte via1dirB;
		public byte via1portA;
		public byte via1portB;

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

			via0dirA = 0x00;
			via0dirB = 0x00;
			via0portA = 0xFF;
			via0portB = 0xFF;
			via1dirA = 0x00;
			via1dirB = 0x00;
			via1portA = 0xFF;
			via1portB = 0xFF;
		}

		public void Connect(SerialPort newSerPort)
		{
			// TODO: verify polarity
			serPort = newSerPort;
			serPort.SystemReadAtn = (() => { return true; });
			serPort.SystemReadClock = (() => { return ((via0portB & 0x8) != 0); }); // bit 3
			serPort.SystemReadData = (() => { return ((via0portB & 0x2) != 0); }); // bit 1
			serPort.SystemReadSrq = (() => { return true; });
			serPort.SystemWriteAtn = ((bool val) => { via0portB = Port.ExternalWrite(via0portB, (byte)((via0portB & 0x7F) | (val ? 0x80 : 0x00)), via0dirB); });
			serPort.SystemWriteClock = ((bool val) => { via0portB = Port.ExternalWrite(via0portB, (byte)((via0portB & 0xFB) | (val ? 0x04 : 0x00)), via0dirB); });
			serPort.SystemWriteData = ((bool val) => { via0portB = Port.ExternalWrite(via0portB, (byte)((via0portB & 0xFE) | (val ? 0x01 : 0x00)), via0dirB); });
			serPort.SystemWriteReset = ((bool val) => { });
		}

		public void Execute()
		{
			via0.ExecutePhase1();
			via1.ExecutePhase1();

			cpu.ExecuteOne();
			via0.ExecutePhase2();
			via1.ExecutePhase2();
		}

		public void HardReset()
		{
			for (uint i = 0; i < 0x7FF; i++)
				ram[i] = 0x00;
			cpu.PC = (ushort)(cpu.ReadMemory(0xFFFC) | ((ushort)cpu.ReadMemory(0xFFFD) << 8));
		}
	}
}
