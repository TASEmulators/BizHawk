using BizHawk.Emulation.Computers.Commodore64.MOS;
using System.Reflection;

namespace BizHawk.Emulation.Computers.Commodore64
{
    /// <summary>
    /// Contains the onboard chipset and glue.
    /// </summary>
	sealed public partial class Motherboard
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
		//public int address;
		public byte bus;
		public bool inputRead;
        public bool irq;
        public bool nmi;

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
			sid = MOS6581.Create(44100, initRegion);
			switch (initRegion)
			{
				case Region.NTSC: vic = MOS6567.Create(); break;
				case Region.PAL: vic = MOS6569.Create(); break;
			}
			userPort = new UserPort();
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
            cpu.ReadAEC = vic.ReadAECBuffer;
            cpu.ReadIRQ = Glue_ReadIRQ;
            cpu.ReadNMI = cia1.ReadIRQBuffer;
            cpu.ReadPort = Cpu_ReadPort;
            cpu.ReadRDY = vic.ReadBABuffer;
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
            pla.ReadAEC = vic.ReadAECBuffer;
            pla.ReadBA = vic.ReadBABuffer;
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
            Sync.SyncObject(ser, this);
            ser.EndSection();

            ser.BeginSection("cartridge");
            cartPort.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("cassette");
            cassPort.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("cia0");
            cia0.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("cia1");
            cia1.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("colorram");
            colorRam.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("cpu");
            cpu.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("pla");
            pla.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("ram");
            ram.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("sid");
            sid.SyncState(ser);
            ser.EndSection();

            ser.BeginSection("vic");
            vic.SyncState(ser);
            ser.EndSection();
        }
	}
}
