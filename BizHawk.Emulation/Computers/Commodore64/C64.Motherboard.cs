using BizHawk.Emulation.Computers.Commodore64.MOS;

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

		public void Execute()
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
            cassPort.DeviceReadLevel = CassPort_DeviceReadLevel;
            cassPort.DeviceReadMotor = CassPort_DeviceReadMotor;
            cassPort.DeviceWriteButton = CassPort_DeviceWriteButton;
            cassPort.DeviceWriteLevel = CassPort_DeviceWriteLevel;
            cassPort.SystemReadButton = CassPort_SystemReadLevel;
            cassPort.SystemReadLevel = CassPort_SystemReadLevel;
            cassPort.SystemWriteLevel = CassPort_SystemWriteLevel;
            cassPort.SystemWriteMotor = CassPort_SystemWriteMotor;

            cia0.ReadDirA = Cia0_ReadDirA;
			cia0.ReadDirB = Cia0_ReadDirB;
			cia0.ReadPortA = Cia0_ReadPortA;
			cia0.ReadPortB = Cia0_ReadPortB;
			cia0.WriteDirA = Cia0_WriteDirA;
			cia0.WriteDirB = Cia0_WriteDirB;
			cia0.WritePortA = Cia0_WritePortA;
			cia0.WritePortB = Cia0_WritePortB;

            cia1.ReadDirA = Cia1_ReadDirA;
			cia1.ReadDirB = Cia1_ReadDirB;
			cia1.ReadPortA = Cia1_ReadPortA;
			cia1.ReadPortB = Cia1_ReadPortB;
			cia1.WriteDirA = Cia1_WriteDirA;
			cia1.WriteDirB = Cia1_WriteDirB;
			cia1.WritePortA = Cia1_WritePortA;
			cia1.WritePortB = Cia1_WritePortB;

			cpu.PeekMemory = pla.Peek;
			cpu.PokeMemory = pla.Poke;
            cpu.ReadAEC = Cpu_ReadAEC;
            cpu.ReadIRQ = Cpu_ReadIRQ;
			cpu.ReadNMI = Cpu_ReadNMI;
			cpu.ReadRDY = Cpu_ReadRDY;
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
            pla.ReadBasicRom = Pla_ReadBasicRom;
            pla.ReadCartridgeHi = Pla_ReadCartridgeHi;
            pla.ReadCartridgeLo = Pla_ReadCartridgeLo;
            pla.ReadCharen = Pla_ReadCharen;
            pla.ReadCharRom = Pla_ReadCharRom;
            pla.ReadCia0 = Pla_ReadCia0;
            pla.ReadCia1 = Pla_ReadCia1;
            pla.ReadColorRam = Pla_ReadColorRam;
            pla.ReadExpansionHi = Pla_ReadExpansionHi;
            pla.ReadExpansionLo = Pla_ReadExpansionLo;
            pla.ReadExRom = Pla_ReadExRom;
            pla.ReadGame = Pla_ReadGame;
            pla.ReadHiRam = Pla_ReadHiRam;
            pla.ReadKernalRom = Pla_ReadKernalRom;
            pla.ReadLoRam = Pla_ReadLoRam;
            pla.ReadMemory = Pla_ReadMemory;
            pla.ReadSid = Pla_ReadSid;
            pla.ReadVic = Pla_ReadVic;
            pla.WriteCartridgeHi = Pla_WriteCartridgeHi;
            pla.WriteCartridgeLo = Pla_WriteCartridgeLo;
            pla.WriteCia0 = Pla_WriteCia0;
            pla.WriteCia1 = Pla_WriteCia1;
            pla.WriteColorRam = Pla_WriteColorRam;
            pla.WriteExpansionHi = Pla_WriteExpansionHi;
            pla.WriteExpansionLo = Pla_WriteExpansionLo;
            pla.WriteMemory = Pla_WriteMemory;
            pla.WriteSid = Pla_WriteSid;
            pla.WriteVic = Pla_WriteVic;

			// note: c64 serport lines are inverted
            serPort.DeviceReadAtn = SerPort_DeviceReadAtn;
            serPort.DeviceReadClock = SerPort_DeviceReadClock;
            serPort.DeviceReadData = SerPort_DeviceReadData;
            serPort.DeviceReadReset = SerPort_DeviceReadReset;
            serPort.DeviceWriteAtn = SerPort_DeviceWriteAtn;
            serPort.DeviceWriteClock = SerPort_DeviceWriteClock;
            serPort.DeviceWriteData = SerPort_DeviceWriteData;
            serPort.DeviceWriteSrq = SerPort_DeviceWriteSrq;

            sid.ReadPotX = Sid_ReadPotX;
            sid.ReadPotY = Sid_ReadPotY;

            vic.ReadMemory = Vic_ReadMemory;
            vic.ReadColorRam = Vic_ReadColorRam;
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
