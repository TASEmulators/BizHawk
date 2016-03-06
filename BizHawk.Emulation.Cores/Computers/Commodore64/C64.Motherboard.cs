using System.Reflection;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Cores.Computers.Commodore64.Cassette;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;
using BizHawk.Emulation.Cores.Computers.Commodore64.Serial;
using BizHawk.Emulation.Cores.Computers.Commodore64.User;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	/// <summary>
	/// Contains the onboard chipset and glue.
	/// </summary>
	public sealed partial class Motherboard
	{
        // chips
        public readonly Chip23128 BasicRom;
        public readonly Chip23128 CharRom;
        public readonly Cia Cia0;
        public readonly Cia Cia1;
        public readonly Chip2114 ColorRam;
        public readonly Chip6510 Cpu;
        public readonly Chip23128 KernalRom;
        public readonly Chip90611401 Pla;
        public readonly Chip4864 Ram;
        public readonly Sid Sid;
        public readonly Vic Vic;

        // ports
        public readonly CartridgePort CartPort;
        public readonly CassettePort Cassette;
        public IController Controller;
        public readonly SerialPort Serial;
	    public readonly TapeDrive TapeDrive;
        public readonly UserPort User;

        // devices
        public readonly Drive1541 DiskDrive;

        // state
        //public int address;
        public int Bus;
        public bool InputRead;
        public bool Irq;
        public bool Nmi;

		private readonly C64 _c64;

		public Motherboard(C64 c64, C64.VicType initRegion, C64.BorderType borderType, C64.SidType sidType, C64.TapeDriveType tapeDriveType, C64.DiskDriveType diskDriveType)
		{
			// note: roms need to be added on their own externally
			_c64 = c64;
			int clockNum, clockDen;
			switch (initRegion)
			{
				case C64.VicType.Pal:
					clockNum = 17734475;
					clockDen = 18;
			        break;
				case C64.VicType.Ntsc:
			        clockNum = 14318181;
			        clockDen = 14;
			        break;
				case C64.VicType.NtscOld:
					clockNum = 11250000;
					clockDen = 11;
			        break;
				case C64.VicType.Drean:
					clockNum = 14328225;
					clockDen = 14;
			        break;
				default:
					throw new System.Exception();
			}
			CartPort = new CartridgePort();
			Cassette = new CassettePort();
            ColorRam = new Chip2114();
			Cpu = new Chip6510();
			Pla = new Chip90611401();
			Ram = new Chip4864();
			Serial = new SerialPort();

		    switch (sidType)
		    {
		        case C64.SidType.OldR2:
		            Sid = Chip6581R2.Create(44100, clockNum, clockDen);
		            break;
                case C64.SidType.OldR3:
                    Sid = Chip6581R3.Create(44100, clockNum, clockDen);
                    break;
                case C64.SidType.OldR4AR:
                    Sid = Chip6581R4AR.Create(44100, clockNum, clockDen);
                    break;
                case C64.SidType.NewR5:
                    Sid = Chip8580R5.Create(44100, clockNum, clockDen);
                    break;
            }

			switch (initRegion)
			{
				case C64.VicType.Ntsc:
                    Vic = Chip6567R8.Create(borderType);
                    Cia0 = Chip6526.Create(C64.CiaType.Ntsc,  Input_ReadKeyboard, Input_ReadJoysticks);
                    Cia1 = Chip6526.Create(C64.CiaType.Ntsc, Cia1_ReadPortA);
                    break;
				case C64.VicType.Pal:
                    Vic = Chip6569.Create(borderType);
                    Cia0 = Chip6526.Create(C64.CiaType.Pal, Input_ReadKeyboard, Input_ReadJoysticks);
                    Cia1 = Chip6526.Create(C64.CiaType.Pal, Cia1_ReadPortA);
                    break;
                case C64.VicType.NtscOld:
                    Vic = Chip6567R56A.Create(borderType);
                    Cia0 = Chip6526.Create(C64.CiaType.NtscRevA, Input_ReadKeyboard, Input_ReadJoysticks);
                    Cia1 = Chip6526.Create(C64.CiaType.NtscRevA, Cia1_ReadPortA);
			        break;
                case C64.VicType.Drean:
                    Vic = Chip6572.Create(borderType);
                    Cia0 = Chip6526.Create(C64.CiaType.Pal, Input_ReadKeyboard, Input_ReadJoysticks);
                    Cia1 = Chip6526.Create(C64.CiaType.Pal, Cia1_ReadPortA);
                    break;
			}
			User = new UserPort();

		    ClockNumerator = clockNum;
		    ClockDenominator = clockDen;

            // Initialize disk drive
		    switch (diskDriveType)
		    {
		        case C64.DiskDriveType.Commodore1541:
                case C64.DiskDriveType.Commodore1541II:
                    DiskDrive = new Drive1541(ClockNumerator, ClockDenominator);
                    Serial.Connect(DiskDrive);
		            break;
		    }

            // Initialize tape drive
		    switch (tapeDriveType)
		    {
		        case C64.TapeDriveType.Commodore1530:
                    TapeDrive = new TapeDrive();
                    Cassette.Connect(TapeDrive);
		            break;
		    }

            BasicRom = new Chip23128();
            CharRom = new Chip23128();
            KernalRom = new Chip23128();
		}

        [SaveState.DoNotSave] public int ClockNumerator { get; private set; }
        [SaveState.DoNotSave] public int ClockDenominator { get; private set; }

		// -----------------------------------------

		public void Execute()
		{
		    _vicBank = (0x3 - ((Cia1.PrA | ~Cia1.DdrA) & 0x3)) << 14;

            Vic.ExecutePhase();
            CartPort.ExecutePhase();
            Cassette.ExecutePhase();
            Serial.ExecutePhase();
            Sid.ExecutePhase();
            Cia0.ExecutePhase();
            Cia1.ExecutePhase();
            Cpu.ExecutePhase();
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

			Cia0.HardReset();
			Cia1.HardReset();
			ColorRam.HardReset();
			Ram.HardReset();
			Serial.HardReset();
			Sid.HardReset();
			Vic.HardReset();
			User.HardReset();
			Cassette.HardReset();
            Serial.HardReset();
            Cpu.HardReset();
            CartPort.HardReset();
        }

        public void Init()
        {
            CartPort.ReadOpenBus = ReadOpenBus;

            Cassette.ReadDataOutput = CassPort_ReadDataOutput;
            Cassette.ReadMotor = CassPort_ReadMotor;

            Cia0.ReadFlag = Cassette.ReadDataInputBuffer;

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
			Pla.ReadAec = Vic.ReadAec;
			Pla.ReadBa = Vic.ReadBa;
			Pla.ReadBasicRom = BasicRom.Read;
			Pla.ReadCartridgeHi = CartPort.ReadHiRom;
			Pla.ReadCartridgeLo = CartPort.ReadLoRom;
			Pla.ReadCharen = Pla_ReadCharen;
			Pla.ReadCharRom = CharRom.Read;
			Pla.ReadCia0 = Pla_ReadCia0;
			Pla.ReadCia1 = Cia1.Read;
			Pla.ReadColorRam = Pla_ReadColorRam;
			Pla.ReadExpansionHi = Pla_ReadExpansion1;
			Pla.ReadExpansionLo = Pla_ReadExpansion0;
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

			Serial.ReadMasterAtn = SerPort_ReadAtnOut;
			Serial.ReadMasterClk = SerPort_ReadClockOut;
			Serial.ReadMasterData = SerPort_ReadDataOut;

			Sid.ReadPotX = Sid_ReadPotX;
			Sid.ReadPotY = Sid_ReadPotY;

            Vic.ReadMemory = Vic_ReadMemory;
			Vic.ReadColorRam = ColorRam.Read;
		}

		public void SyncState(Serializer ser)
		{
            SaveState.SyncObject(ser, this);
        }
    }
}
