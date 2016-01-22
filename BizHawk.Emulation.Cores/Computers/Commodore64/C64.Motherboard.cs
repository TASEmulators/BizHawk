using System.Reflection;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;
using BizHawk.Emulation.Cores.Computers.Commodore64.CassettePort;
using BizHawk.Emulation.Cores.Computers.Commodore64.UserPort;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	/// <summary>
	/// Contains the onboard chipset and glue.
	/// </summary>
	public sealed partial class Motherboard
	{
		// chips
		public Chip23XX basicRom;
		public Chip23XX charRom;
		public readonly MOS6526 cia0;
		public readonly MOS6526 cia1;
		public readonly Chip2114 colorRam;
		public readonly MOS6510 cpu;
		public Chip23XX kernalRom;
		public readonly MOSPLA pla;
		public readonly Chip4864 ram;
		public readonly Sid sid;
		public readonly Vic vic;

		// ports
		public readonly CartridgePort cartPort;
		public readonly CassettePortDevice cassPort;
		public IController controller;
		public readonly SerialPort serPort;
		public readonly UserPortDevice userPort;

		// state
		//public int address;
		public int bus;
		public bool inputRead;
		public bool irq;
		public bool nmi;

		private C64 _c64;

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
			cartPort = new CartridgePort();
			cassPort = new CassettePortDevice();
			cia0 = new MOS6526(clockNum, clockDen*mainsFrq);
			cia1 = new MOS6526(clockNum, clockDen*mainsFrq);
			colorRam = new Chip2114();
			cpu = new MOS6510();
			pla = new MOSPLA();
			ram = new Chip4864();
			serPort = new SerialPort();
			sid = MOS6581.Create(44100, clockNum, clockDen);
			switch (initRegion)
			{
				case C64.VicType.Ntsc: vic = MOS6567R8.Create(); break;
				case C64.VicType.Pal: vic = MOS6569.Create(); break;
				case C64.VicType.NtscOld: vic = MOS6567R56A.Create(); break;
				case C64.VicType.Drean: vic = MOS6572.Create(); break;
			}
			userPort = new UserPortDevice();
		}

		// -----------------------------------------

		public void Execute()
		{
			vic.ExecutePhase1();
			cpu.ExecutePhase1();
			cia0.ExecutePhase1();
			cia1.ExecutePhase1();

			vic.ExecutePhase2();
			cpu.ExecutePhase2();
			cia0.ExecutePhase2();
			cia1.ExecutePhase2();
			sid.ExecutePhase2();
		}

		public void Flush()
		{
			sid.Flush();
		}

		// -----------------------------------------

		public void HardReset()
		{
			bus = 0xFF;
			inputRead = false;

			cpu.HardReset();
			cia0.HardReset();
			cia1.HardReset();
			colorRam.HardReset();
			ram.HardReset();
			serPort.HardReset();
			sid.HardReset();
			vic.HardReset();
			userPort.HardReset();
			cassPort.HardReset();

			// because of how mapping works, the cpu needs to be hard reset twice
			cpu.HardReset();
		}

		public void Init()
		{
			cartPort.ReadIRQ = Glue_ReadIRQ;
			cartPort.ReadNMI = cia1.ReadIRQBuffer;

			cassPort.ReadDataOutput = CassPort_ReadDataOutput;
			cassPort.ReadMotor = CassPort_ReadMotor;

			cia0.ReadCNT = Cia0_ReadCnt;
			cia0.ReadFlag = cassPort.ReadDataInputBuffer;
			cia0.ReadPortA = Cia0_ReadPortA;
			cia0.ReadPortB = Cia0_ReadPortB;
			cia0.ReadSP = Cia0_ReadSP;

			cia1.ReadCNT = Cia1_ReadCnt;
			cia1.ReadFlag = userPort.ReadFlag2;
			cia1.ReadPortA = Cia1_ReadPortA;
			cia1.ReadPortB = userPort.ReadData;
			cia1.ReadSP = Cia1_ReadSP;

			cpu.PeekMemory = pla.Peek;
			cpu.PokeMemory = pla.Poke;
			cpu.ReadAEC = vic.ReadAecBuffer;
			cpu.ReadIRQ = Glue_ReadIRQ;
			cpu.ReadNMI = cia1.ReadIRQBuffer;
			cpu.ReadPort = Cpu_ReadPort;
			cpu.ReadRDY = vic.ReadBaBuffer;
			cpu.ReadMemory = pla.Read;
			cpu.WriteMemory = pla.Write;
			cpu.WriteMemoryPort = Cpu_WriteMemoryPort;

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
			pla.ReadAEC = vic.ReadAecBuffer;
			pla.ReadBA = vic.ReadBaBuffer;
			pla.ReadBasicRom = basicRom.Read;
			pla.ReadCartridgeHi = cartPort.ReadHiRom;
			pla.ReadCartridgeLo = cartPort.ReadLoRom;
			pla.ReadCharen = Pla_ReadCharen;
			pla.ReadCharRom = charRom.Read;
			pla.ReadCia0 = Pla_ReadCia0;
			pla.ReadCia1 = cia1.Read;
			pla.ReadColorRam = Pla_ReadColorRam;
			pla.ReadExpansionHi = cartPort.ReadHiExp;
			pla.ReadExpansionLo = cartPort.ReadLoExp;
			pla.ReadExRom = cartPort.ReadExRom;
			pla.ReadGame = cartPort.ReadGame;
			pla.ReadHiRam = Pla_ReadHiRam;
			pla.ReadKernalRom = kernalRom.Read;
			pla.ReadLoRam = Pla_ReadLoRam;
			pla.ReadMemory = ram.Read;
			pla.ReadSid = sid.Read;
			pla.ReadVic = vic.Read;
			pla.WriteCartridgeHi = cartPort.WriteHiRom;
			pla.WriteCartridgeLo = cartPort.WriteLoRom;
			pla.WriteCia0 = cia0.Write;
			pla.WriteCia1 = cia1.Write;
			pla.WriteColorRam = colorRam.Write;
			pla.WriteExpansionHi = cartPort.WriteHiExp;
			pla.WriteExpansionLo = cartPort.WriteLoExp;
			pla.WriteMemory = ram.Write;
			pla.WriteSid = sid.Write;
			pla.WriteVic = vic.Write;

			serPort.ReadAtnOut = SerPort_ReadAtnOut;
			serPort.ReadClockOut = SerPort_ReadClockOut;
			serPort.ReadDataOut = SerPort_ReadDataOut;

			sid.ReadPotX = Sid_ReadPotX;
			sid.ReadPotY = Sid_ReadPotY;

			vic.ReadMemory = Vic_ReadMemory;
			vic.ReadColorRam = colorRam.Read;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("motherboard");
			SaveState.SyncObject(ser, this);
			ser.EndSection();
		}
	}
}
