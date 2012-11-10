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
		// input
		public Input input;

		// source
		public Cartridge cart;
		public string extension;
		public byte[] inputFile;
		public List<IMedia> mediaAttached = new List<IMedia>();

		// chipset
		public Cia cia0;
		public Cia cia1;
		public MOS6502X cpu;
		public Memory mem;
		public Sid sid;
		public VicII vic;
		public ChipSignals signal;

		public void HardReset()
		{
			// initalize cpu
			cpu = new MOS6502X();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.DummyReadMemory = PeekMemory;

			// initialize cia timers
			cia0 = new Cia(signal);
			cia0.ports[0] = new DirectionalDataPort(0x00, 0x00, 0xFF);
			cia0.ports[1] = new DirectionalDataPort(0x00, 0x00, 0xFF);
			cia1 = new Cia(signal);
			cia1.ports[0] = new DirectionalDataPort(0x00, 0x00, 0xFF);
			cia1.ports[1] = new DirectionalDataPort(0x00, 0x00, 0xFF);

			// initialize vic
			signal = new ChipSignals();
			vic = new VicII(signal, VicIIMode.NTSC);

			// initialize sid
			sid = new Sid();

			// initialize memory (this must be done AFTER all other chips are initialized)
			string romPath = CoreInputComm.C64_FirmwaresPath;
			mem = new Memory(romPath, vic, sid, cia0, cia1);
			vic.mem = mem;

			// initialize cpu hard reset vector
			cpu.PC = (ushort)(ReadMemory(0xFFFC) + (ReadMemory(0xFFFD) << 8));

			// initailize input
			input = new Input(cia0.ports);

			// initialize media
			switch (extension.ToUpper())
			{
				case @".PRG":
					if (inputFile.Length > 2)
						mediaAttached.Add(new PRGFile(inputFile, mem, cpu));
					break;
				case @".CRT":
					Cartridge newCart = new Cartridge(inputFile, mem);
					if (newCart.valid)
					{
						cart = newCart;
						mediaAttached.Add(cart);
					}
					else
					{
						cart = null;
					}
					break;
			}
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

		public void PollInput()
		{
			input.Poll();
			signal.KeyboardNMI = input.restorePressed;
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
		private bool _KeyboardNMIOutput;
		private bool _VicAECOutput;
		private bool _VicIRQOutput;
		private bool _VicLPInput;

		public bool CiaIRQ0 { get { return _CiaIRQOutput[0]; } set { _CiaIRQOutput[0] = value; } }
		public bool CiaIRQ1 { get { return _CiaIRQOutput[1]; } set { _CiaIRQOutput[1] = value; } }
		public bool CiaSerial0 { get { return _CiaSerialInput[0]; } }
		public bool CiaSerial1 { get { return _CiaSerialInput[1]; } }
		public bool CpuAEC { get { return _VicAECOutput; } }
		public bool CpuIRQ { get { return _VicIRQOutput | _CiaIRQOutput[0]; } }
		public bool CpuNMI { get { return _CiaIRQOutput[1] | _KeyboardNMIOutput; } }
		public bool KeyboardNMI { get { return _KeyboardNMIOutput; } set { _KeyboardNMIOutput = value; } }
		public bool LPOutput { get { return _VicLPInput; } set { _VicLPInput = value; } }
		public bool VicAEC { get { return _VicAECOutput; } set { _VicAECOutput = value; } }
		public bool VicIRQ { get { return _VicIRQOutput; } set { _VicIRQOutput = value; } }
		public bool VicLP { get { return _VicLPInput; } }
	}
}
