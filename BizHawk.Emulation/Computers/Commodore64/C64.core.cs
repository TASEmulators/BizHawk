using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
	{
		// source
		public Cartridge cart;
		public bool cartInserted;
		public byte[] inputFile;

		// chipset
		public Cia cia0;
		public Cia cia1;
		public MOS6502X cpu;
		public Memory mem;
		public Sid sid;
		public VicII vic;
		public ChipSignals signal;

		private void HardReset()
		{
			cpu = new MOS6502X();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.DummyReadMemory = PeekMemory;

			// initialize cia timers
			cia0 = new Cia(signal);
			cia0.ports[0] = new DirectionalDataPort(0x00, 0x00);
			cia0.ports[1] = new DirectionalDataPort(0x00, 0x00);
			cia1 = new Cia(signal);
			cia1.ports[0] = new DirectionalDataPort(0x00, 0x00);
			cia1.ports[1] = new DirectionalDataPort(0x00, 0x00);

			// initialize vic
			signal = new ChipSignals();
			vic = new VicII(signal, VicIIMode.NTSC);

			// initialize sid
			sid = new Sid();

			// initialize memory (this must be done AFTER all other chips are initialized)
			string romPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "C64Kernal");
			mem = new Memory(romPath, vic, sid, cia0, cia1);
			vic.mem = mem;

			// initialize media
			Cartridge cart = new Cartridge(inputFile);
			if (cart.valid)
			{
				mem.ApplyCartridge(cart);
			}

			// initialize cpu (hard reset vector)
			cpu.PC = (ushort)(ReadMemory(0xFFFC) + (ReadMemory(0xFFFD) << 8));
		}

		public byte PeekMemory(ushort addr)
		{
			return mem.Peek(addr);
		}

		public byte PeekMemoryInt(int addr)
		{
			return mem.Peek((ushort)(addr & 0xFFFF));
		}

		public void PokeMemoryInt(int addr, byte val)
		{
			// todo
		}

		public byte ReadMemory(ushort addr)
		{
			return mem.Read(addr);
		}

		public void WriteMemory(ushort addr, byte value)
		{
			mem.Write(addr, value);
		}
	}

	public class ChipSignals
	{
		private bool[] _CiaSerialInput = new bool[2];
		private bool[] _CiaIRQOutput = new bool[2];
		private bool _VicAECOutput;
		private bool _VicBAOutput;
		private bool _VicIRQOutput;
		private bool _VicLPInput;

		public bool CiaIRQ0 { get { return _CiaIRQOutput[0]; } set { _CiaIRQOutput[0] = value; } }
		public bool CiaIRQ1 { get { return _CiaIRQOutput[1]; } set { _CiaIRQOutput[1] = value; } }
		public bool CiaSerial0 { get { return _CiaSerialInput[0]; } }
		public bool CiaSerial1 { get { return _CiaSerialInput[1]; } }
		public bool CpuAEC { get { return _VicAECOutput; } }
		public bool CpuIRQ { get { return _VicIRQOutput | _CiaIRQOutput[0] | _CiaIRQOutput[1]; } }
		public bool CpuRDY { get { return _VicBAOutput; } }
		public bool LPOutput { get { return _VicLPInput; } set { _VicLPInput = value; } }
		public bool VicAEC { get { return _VicAECOutput; } set { _VicAECOutput = value; } }
		public bool VicBA { get { return _VicBAOutput; } set { _VicBAOutput = value; } }
		public bool VicIRQ { get { return _VicIRQOutput; } set { _VicIRQOutput = value; } }
		public bool VicLP { get { return _VicLPInput; } }
	}
}
