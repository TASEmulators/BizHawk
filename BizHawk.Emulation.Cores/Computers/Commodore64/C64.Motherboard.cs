using System.Reflection;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cassette;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;
using BizHawk.Emulation.Cores.Computers.Commodore64.User;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	/// <summary>
	/// Contains the onboard chipset and glue.
	/// </summary>
	public sealed partial class Motherboard
	{
		// chips
		public Chip23128 BasicRom;
		public Chip23128 CharRom;
		public readonly Cia Cia0;
		public readonly Cia Cia1;
		public readonly Chip2114 ColorRam;
		public readonly Chip6510 Cpu;
		public Chip23128 KernalRom;
		public readonly Chip90611401 Pla;
		public readonly Chip4864 Ram;
		public readonly Sid Sid;
		public readonly Vic Vic;

		// ports
		public readonly CartridgePort CartPort;
		public readonly CassettePort Cassette;
		public IController Controller;
		public readonly SerialPort Serial;
		public readonly UserPort User;

		// state
		//public int address;
		public int Bus;
		public bool InputRead;
		public bool Irq;
		public bool Nmi;

		private readonly C64 _c64;

		public Motherboard(C64 c64, C64.VicType initRegion)
		{
			// note: roms need to be added on their own externally
			_c64 = c64;
			int clockNum, clockDen, mainsFrq;
			switch (initRegion)
			{
				case C64.VicType.Pal:
					clockNum = 17734475;
					clockDen = 18;
					mainsFrq = 50;
					break;
				case C64.VicType.Ntsc:
				case C64.VicType.NtscOld:
					clockNum = 11250000;
					clockDen = 11;
					mainsFrq = 60;
					break;
				case C64.VicType.Drean:
					clockNum = 14328225;
					clockDen = 14;
					mainsFrq = 50;
					break;
				default:
					throw new System.Exception();
			}
			CartPort = new CartridgePort();
			Cassette = new CassettePort();
			Cia0 = new Cia(clockNum, clockDen * mainsFrq, keyboardPressed, joystickPressed);
            Cia1 = new Cia(clockNum, clockDen * mainsFrq, Cia1_ReadPortA);
            ColorRam = new Chip2114();
			Cpu = new Chip6510();
			Pla = new Chip90611401();
			Ram = new Chip4864();
			Serial = new SerialPort();
			Sid = Chip6581.Create(44100, clockNum, clockDen);
			switch (initRegion)
			{
				case C64.VicType.Ntsc: Vic = Chip6567R8.Create(); break;
				case C64.VicType.Pal: Vic = Chip6569.Create(); break;
				case C64.VicType.NtscOld: Vic = Chip6567R56A.Create(); break;
				case C64.VicType.Drean: Vic = Chip6572.Create(); break;
			}
			User = new UserPort();
		}

		// -----------------------------------------

		public void Execute()
		{
			Vic.ExecutePhase1();
			Cpu.ExecutePhase1();
			Cia0.ExecutePhase1();
			Cia1.ExecutePhase1();

			Vic.ExecutePhase2();
			Cpu.ExecutePhase2();
			Cia0.ExecutePhase2();
			Cia1.ExecutePhase2();
			Sid.ExecutePhase2();
		}

		public void Flush()
		{
			Sid.Flush();
		}

		// -----------------------------------------

		public void HardReset()
		{
			Bus = 0xFF;
			InputRead = false;

			Cpu.HardReset();
			Cia0.HardReset();
			Cia1.HardReset();
			ColorRam.HardReset();
			Ram.HardReset();
			Serial.HardReset();
			Sid.HardReset();
			Vic.HardReset();
			User.HardReset();
			Cassette.HardReset();

			// because of how mapping works, the cpu needs to be hard reset twice
			Cpu.HardReset();
		}

		public void Init()
		{
			Cassette.ReadDataOutput = CassPort_ReadDataOutput;
			Cassette.ReadMotor = CassPort_ReadMotor;

            /*
			Cia0.ReadCnt = Cia0_ReadCnt;
			Cia0.ReadFlag = Cassette.ReadDataInputBuffer;
			Cia0.ReadPortA = Cia0_ReadPortA;
			Cia0.ReadPortB = Cia0_ReadPortB;
			Cia0.ReadSp = Cia0_ReadSP;

			Cia1.ReadCnt = Cia1_ReadCnt;
			Cia1.ReadFlag = User.ReadFlag2;
			Cia1.ReadPortA = Cia1_ReadPortA;
			Cia1.ReadPortB = User.ReadData;
			Cia1.ReadSp = Cia1_ReadSP;
            */

			Cpu.PeekMemory = Pla.Peek;
			Cpu.PokeMemory = Pla.Poke;
			Cpu.ReadAec = Vic.ReadAec;
			Cpu.ReadIrq = Glue_ReadIRQ;
			Cpu.ReadNmi = Glue_ReadNMI;
			Cpu.ReadPort = Cpu_ReadPort;
			Cpu.ReadRdy = Vic.ReadBa;
			Cpu.ReadMemory = Pla.Read;
			Cpu.WriteMemory = Pla.Write;
			Cpu.WriteMemoryPort = Cpu_WriteMemoryPort;

			Pla.PeekBasicRom = BasicRom.Peek;
			Pla.PeekCartridgeHi = CartPort.PeekHiRom;
			Pla.PeekCartridgeLo = CartPort.PeekLoRom;
			Pla.PeekCharRom = CharRom.Peek;
			Pla.PeekCia0 = Cia0.Peek;
			Pla.PeekCia1 = Cia1.Peek;
			Pla.PeekColorRam = ColorRam.Peek;
			Pla.PeekExpansionHi = CartPort.PeekHiExp;
			Pla.PeekExpansionLo = CartPort.PeekLoExp;
			Pla.PeekKernalRom = KernalRom.Peek;
			Pla.PeekMemory = Ram.Peek;
			Pla.PeekSid = Sid.Peek;
			Pla.PeekVic = Vic.Peek;
			Pla.PokeCartridgeHi = CartPort.PokeHiRom;
			Pla.PokeCartridgeLo = CartPort.PokeLoRom;
			Pla.PokeCia0 = Cia0.Poke;
			Pla.PokeCia1 = Cia1.Poke;
			Pla.PokeColorRam = ColorRam.Poke;
			Pla.PokeExpansionHi = CartPort.PokeHiExp;
			Pla.PokeExpansionLo = CartPort.PokeLoExp;
			Pla.PokeMemory = Ram.Poke;
			Pla.PokeSid = Sid.Poke;
			Pla.PokeVic = Vic.Poke;
			Pla.ReadAEC = Vic.ReadAec;
			Pla.ReadBA = Vic.ReadBa;
			Pla.ReadBasicRom = BasicRom.Read;
			Pla.ReadCartridgeHi = CartPort.ReadHiRom;
			Pla.ReadCartridgeLo = CartPort.ReadLoRom;
			Pla.ReadCharen = Pla_ReadCharen;
			Pla.ReadCharRom = CharRom.Read;
			Pla.ReadCia0 = Pla_ReadCia0;
			Pla.ReadCia1 = Cia1.Read;
			Pla.ReadColorRam = Pla_ReadColorRam;
			Pla.ReadExpansionHi = CartPort.ReadHiExp;
			Pla.ReadExpansionLo = CartPort.ReadLoExp;
			Pla.ReadExRom = CartPort.ReadExRom;
			Pla.ReadGame = CartPort.ReadGame;
			Pla.ReadHiRam = Pla_ReadHiRam;
			Pla.ReadKernalRom = KernalRom.Read;
			Pla.ReadLoRam = Pla_ReadLoRam;
			Pla.ReadMemory = Ram.Read;
			Pla.ReadSid = Sid.Read;
			Pla.ReadVic = Vic.Read;
			Pla.WriteCartridgeHi = CartPort.WriteHiRom;
			Pla.WriteCartridgeLo = CartPort.WriteLoRom;
			Pla.WriteCia0 = Cia0.Write;
			Pla.WriteCia1 = Cia1.Write;
			Pla.WriteColorRam = ColorRam.Write;
			Pla.WriteExpansionHi = CartPort.WriteHiExp;
			Pla.WriteExpansionLo = CartPort.WriteLoExp;
			Pla.WriteMemory = Ram.Write;
			Pla.WriteSid = Sid.Write;
			Pla.WriteVic = Vic.Write;

            /*
			Serial.ReadAtnOut = SerPort_ReadAtnOut;
			Serial.ReadClockOut = SerPort_ReadClockOut;
			Serial.ReadDataOut = SerPort_ReadDataOut;
            */

			Sid.ReadPotX = Sid_ReadPotX;
			Sid.ReadPotY = Sid_ReadPotY;

            /*
		    User.ReadCounter1 = Cia0.ReadCntBuffer;
		    User.ReadCounter2 = Cia1.ReadCntBuffer;
		    User.ReadHandshake = Cia1.ReadPcBuffer;
		    User.ReadSerial1 = Cia0.ReadSpBuffer;
		    User.ReadSerial2 = Cia1.ReadSpBuffer;
            */

			Vic.ReadMemory = Vic_ReadMemory;
			Vic.ReadColorRam = ColorRam.Read;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("motherboard");
			SaveState.SyncObject(ser, this);
			ser.EndSection();
		}
	}
}
