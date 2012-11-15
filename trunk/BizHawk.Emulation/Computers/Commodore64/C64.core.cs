using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public enum Region
	{
		NTSC,
		PAL
	}

	public partial class  C64 : IEmulator
	{
		// input
		public Input input;

		// source
		public Cartridge cart = null;
		public Drive1541 diskDrive = null;
		public bool diskDriveAttached = false;
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

		// sid stuff
		//private Emulation.Sound.Utilities.DCFilter sidDCFilter;
		private SidSyncSoundProvider syncSid;

		public bool DriveLED
		{
			get
			{
				if (diskDriveAttached)
				{
					return (diskDrive.Peek(0x1C00) & 0x8) != 0;
				}
				else
				{
					return false;
				}
			}
		}

		public void HardReset()
		{
			// initalize cpu
			cpu = new MOS6502X();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.DummyReadMemory = PeekMemory;

			// initialize cia timers
			cia0 = new Cia(signal, Region.NTSC);
			cia1 = new Cia(signal, Region.NTSC);

			// initialize vic
			signal = new ChipSignals();
			vic = new VicII(signal, Region.NTSC);

			// assume NTSC for now
			CoreOutputComm.VsyncDen = vic.cyclesPerFrame;
			CoreOutputComm.VsyncNum = (14318181 / 14);

			// initialize sid
			sid = new Sid(Region.NTSC, 44100); // we'll assume 44.1k for now until there's a better way
			syncSid = new SidSyncSoundProvider(sid);
			//sidDCFilter = new Emulation.Sound.Utilities.DCFilter(sid, 2205);

			// initialize memory (this must be done AFTER all other chips are initialized)
			string romPath = CoreInputComm.C64_FirmwaresPath;
			mem = new Memory(romPath, vic, sid, cia0, cia1);
			vic.mem = mem;

			// initialize cpu hard reset vector
			cpu.PC = (ushort)(ReadMemory(0xFFFC) + (ReadMemory(0xFFFD) << 8));
			cpu.BCD_Enabled = true;

			// initailize input
			input = new Input( new DataPortConnector[] { cia0.ConnectPort(0), cia0.ConnectPort(1) } );
			cia0.AttachWriteHook(0, input.WritePortA);
			cia0.AttachWriteHook(1, input.WritePortB);

			// initialize media
			switch (extension.ToUpper())
			{
				case @".G64":
					diskDrive = new Drive1541(File.ReadAllBytes(Path.Combine(romPath, @"dos1541")), Region.NTSC);
					diskDrive.Insert(G64.Read(inputFile));
					break;
				case @".D64":
					diskDrive = new Drive1541(File.ReadAllBytes(Path.Combine(romPath, @"dos1541")), Region.NTSC);
					diskDrive.Insert(D64.Read(inputFile));
					break;
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
					break;
			}

			diskDriveAttached = (diskDrive != null);
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
