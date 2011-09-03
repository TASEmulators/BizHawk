using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.H6280;
using BizHawk.Emulation.Sound;
using BizHawk.DiscSystem;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public enum NecSystemType
    {
        TurboGrafx,
        TurboCD,
        SuperGrafx
    }

    public sealed partial class PCEngine : IEmulator
    {
        // ROM
        public byte[] RomData;
        public int RomLength;
        private Disc disc;

        // Machine
        public NecSystemType Type;
        public HuC6280 Cpu;
        public VDC VDC1, VDC2;
        public VCE VCE;
        public VPC VPC;
        public ScsiCDBus SCSI;
        
        public HuC6280PSG PSG;
        public CDAudio CDAudio;
        // TODO ADPCM
        public SoundMixer SoundMixer;
        public MetaspuSoundProvider SoundSynchronizer;

        private bool TurboGrafx { get { return Type == NecSystemType.TurboGrafx; } }
        private bool SuperGrafx { get { return Type == NecSystemType.SuperGrafx; } }
        private bool TurboCD    { get { return Type == NecSystemType.TurboCD; } }
        
        // BRAM
        private bool BramEnabled = false;
        private bool BramLocked = true;
        private byte[] BRAM;

        // Memory system
        public byte[] Ram;       // PCE= 8K base ram, SGX= 64k base ram
        public byte[] CDRam;     // TurboCD extra 64k of ram
        public byte[] SuperRam;  // Super System Card 192K of additional RAM
        public byte[] ArcadeRam; // Arcade Card 2048K of additional RAM

        // 21,477,270  Machine clocks / sec
        //  7,159,090  Cpu cycles / sec

        public PCEngine(GameInfo game, byte[] rom)
        {
            CoreOutputComm = new CoreOutputComm();

            switch (game.System)
            {
                case "PCE": Type = NecSystemType.TurboGrafx; break;
                case "SGX": Type = NecSystemType.SuperGrafx; break;
            }
            Init(game, rom);
        }

        public PCEngine(GameInfo game, Disc disc, byte[] rom)
        {
            CoreOutputComm = new CoreOutputComm();
            Type = NecSystemType.TurboCD;
            this.disc = disc;
            Init(game, rom);
        }

        private void Init(GameInfo game, byte[] rom)
        {
            Controller = NullController.GetNullController();
            Cpu = new HuC6280();
            VCE = new VCE();
            VDC1 = new VDC(Cpu, VCE);
            PSG = new HuC6280PSG();
            InitScsiBus();

            if (TurboGrafx)
            {
                Ram = new byte[0x2000];
                Cpu.ReadMemory21 = ReadMemory;
                Cpu.WriteMemory21 = WriteMemory;
                Cpu.WriteVDC = VDC1.WriteVDC;
                soundProvider = PSG;
                CDAudio = new CDAudio(null, 0);
            }

            else if (SuperGrafx)
            {
                VDC2 = new VDC(Cpu, VCE);
                VPC = new VPC(VDC1, VDC2, VCE, Cpu);
                Ram = new byte[0x8000];
                Cpu.ReadMemory21 = ReadMemorySGX;
                Cpu.WriteMemory21 = WriteMemorySGX;
                Cpu.WriteVDC = VDC1.WriteVDC;
                soundProvider = PSG;
                CDAudio = new CDAudio(null, 0);
            }

            else if (TurboCD)
            {
                Ram = new byte[0x2000];
                CDRam = new byte[0x10000];
                ADPCM_RAM = new byte[0x10000];
                Cpu.ReadMemory21 = ReadMemoryCD;
                Cpu.WriteMemory21 = WriteMemoryCD;
                Cpu.WriteVDC = VDC1.WriteVDC;
                CDAudio = new CDAudio(disc, short.MaxValue);
                ADPCM_Provider = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);
                SoundMixer = new SoundMixer(PSG, CDAudio, ADPCM_Provider);
                SoundSynchronizer = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);
                soundProvider = SoundSynchronizer;
                Cpu.ThinkAction = () => { SCSI.Think(); AdpcmThink(); };
            }

            if (rom.Length == 0x60000)
            {
                // 384k roms require special loading code. Why ;_;
                // In memory, 384k roms look like [1st 256k][Then full 384k]
                RomData = new byte[0xA0000];
                var origRom = rom;
                for (int i=0; i<0x40000; i++)
                    RomData[i] = origRom[i];
                for (int i = 0; i < 0x60000; i++)
                    RomData[i+0x40000] = origRom[i];
                RomLength = RomData.Length;
            } else if (rom.Length > 1024 * 1024) {
                // If the rom is bigger than 1 megabyte, switch to Street Fighter 2 mapper
                Cpu.ReadMemory21 = ReadMemorySF2;
                Cpu.WriteMemory21 = WriteMemorySF2;
                RomData = rom;
                RomLength = RomData.Length;
            } else {
                // normal rom.
                RomData = rom;
                RomLength = RomData.Length;
            }

            if (game["BRAM"] || Type == NecSystemType.TurboCD)
            {
                BramEnabled = true;
                BRAM = new byte[2048]; 
                Console.WriteLine("ENABLE BRAM!");

                // pre-format BRAM
                BRAM[0] = 0x48; BRAM[1] = 0x55; BRAM[2] = 0x42; BRAM[3] = 0x4D;
                BRAM[4] = 0x00; BRAM[5] = 0x88; BRAM[6] = 0x10; BRAM[7] = 0x80;
            }

            if (game["SuperSysCard"])
                SuperRam = new byte[0x30000];

            if (game["ArcadeCard"])
                ArcadeRam = new byte[0x200000];

            if (game["PopulousSRAM"])
            {
                PopulousRAM = new byte[0x8000];
                Cpu.ReadMemory21 = ReadMemoryPopulous;
                Cpu.WriteMemory21 = WriteMemoryPopulous;
            }

            if (game["ForceSpriteLimit"] || game.NotInDatabase)
            {
                VDC1.PerformSpriteLimit = true;
                if (VDC2 != null)
                    VDC2.PerformSpriteLimit = true;
            }

            // Ok, yes, HBlankPeriod's only purpose is game-specific hax.
            // 1) At least they're not coded directly into the emulator, but instead data-driven.
            // 2) The games which have custom HBlankPeriods work without it, the override only
            //    serves to clean up minor gfx anomalies.
            // 3) There's no point in haxing the timing with incorrect values in an attempt to avoid this.
            //    The proper fix is cycle-accurate/bus-accurate timing. That isn't coming to the C# 
            //    version of this core. Let's just acknolwedge that the timing is imperfect and fix
            //    it in the least intrusive and most honest way we can.

            if (game["HBlankPeriod"])
                VDC1.HBlankCycles = int.Parse(game.OptionValue("HBlankPeriod"));

            // This is also a hack. Proper multi-res/TV emulation will be a native-code core feature.
            
            if (game["MultiResHack"])
            {
                VDC1.MultiResHack = int.Parse(game.OptionValue("MultiResHack"));
            }

            Cpu.ResetPC();
            SetupMemoryDomains();
        }

        private int _lagcount = 0;
        private bool lagged = true;
        private bool islag = false;
        public int Frame { get; set; }
        public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
        public bool IsLagFrame { get { return islag; } }

        public void ResetFrameCounter()
        {
            // this should just be a public setter instead of a new method.
            Frame = 0;
        }

        public void FrameAdvance(bool render)
        {
            lagged = true;
            Controller.UpdateControls(Frame++);

            PSG.BeginFrame(Cpu.TotalExecutedCycles);

            if (SuperGrafx)
                VPC.ExecFrame(render);
            else
                VDC1.ExecFrame(render);

            PSG.EndFrame(Cpu.TotalExecutedCycles);
            if (TurboCD)
                SoundSynchronizer.PullSamples(SoundMixer);

            if (lagged)
            {
                _lagcount++;
                islag = true;
            }
            else
                islag = false;
        }

		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }

        public IVideoProvider VideoProvider
        {
            get { return (IVideoProvider) VPC ?? VDC1; }
        }

        private ISoundProvider soundProvider;
        public ISoundProvider SoundProvider
        {
            get { return soundProvider; }
        }
 
        public string SystemId { get { return "PCE"; } }
        public string Region { get; set; }
        public bool DeterministicEmulation { get; set; }

        public byte[] SaveRam
        {
            get { return BRAM; }
        }

        public bool SaveRamModified { get; set; }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[PCEngine]");
            writer.Write("RAM ");
            Ram.SaveAsHex(writer);
            if (PopulousRAM != null)
            {
                writer.Write("PopulousRAM ");
                PopulousRAM.SaveAsHex(writer);
            }
            writer.WriteLine("Frame " + Frame);
            writer.WriteLine("Lag " + _lagcount);
            if (Cpu.ReadMemory21 == ReadMemorySF2)
                writer.WriteLine("SF2MapperLatch " + SF2MapperLatch);
            writer.WriteLine("IOBuffer {0:X2}", IOBuffer);
            writer.WriteLine();

            if (SuperGrafx)
            {
                Cpu.SaveStateText(writer);
                VPC.SaveStateText(writer);
                VCE.SaveStateText(writer);
                VDC1.SaveStateText(writer, 1);
                VDC2.SaveStateText(writer, 2);
                PSG.SaveStateText(writer);
            }
            else
            {
                Cpu.SaveStateText(writer);
                VCE.SaveStateText(writer);
                VDC1.SaveStateText(writer, 1);
                PSG.SaveStateText(writer);
            }
            if (TurboCD)
            {
                CDAudio.SaveStateText(writer);
                writer.Write("CDRAM ");
                CDRam.SaveAsHex(writer);
                if (SuperRam != null)
                {
                    writer.Write("SuperRAM ");
                    SuperRam.SaveAsHex(writer);
                }
            }
            writer.WriteLine("[/PCEngine]");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[PCEngine]") continue;
                if (args[0] == "[/PCEngine]") break;
                if (args[0] == "Frame")
                    Frame = int.Parse(args[1]);
                else if (args[0] == "Lag")
                    _lagcount = int.Parse(args[1]);
                else if (args[0] == "SF2MapperLatch")
                    SF2MapperLatch = byte.Parse(args[1]);
                else if (args[0] == "IOBuffer")
                    IOBuffer = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "RAM")
                    Ram.ReadFromHex(args[1]);
                else if (args[0] == "CDRAM")
                    CDRam.ReadFromHex(args[1]);
                else if (args[0] == "SuperRAM")
                    SuperRam.ReadFromHex(args[1]);
                else if (args[0] == "PopulousRAM" && PopulousRAM != null)
                    PopulousRAM.ReadFromHex(args[1]);
                else if (args[0] == "[HuC6280]")
                    Cpu.LoadStateText(reader);
                else if (args[0] == "[PSG]")
                    PSG.LoadStateText(reader);
                else if (args[0] == "[VCE]")
                    VCE.LoadStateText(reader);
                else if (args[0] == "[VPC]")
                    VPC.LoadStateText(reader);
                else if (args[0] == "[VDC1]")
                    VDC1.LoadStateText(reader, 1);
                else if (args[0] == "[VDC2]")
                    VDC2.LoadStateText(reader, 2);
                else if (args[0] == "[CDAudio]")
                    CDAudio.LoadStateText(reader);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            if (SuperGrafx == false)
            {
                writer.Write(Ram);
                if (BRAM != null)
                    writer.Write(BRAM);
                if (PopulousRAM != null)
                    writer.Write(PopulousRAM);
                if (SuperRam != null)
                    writer.Write(SuperRam);
                if (TurboCD)
                {
                    writer.Write(CDRam);
                    writer.Write(ADPCM_RAM);
                }
                writer.Write(Frame);
                writer.Write(_lagcount);
                writer.Write(SF2MapperLatch);
                writer.Write(IOBuffer);
                Cpu.SaveStateBinary(writer);
                VCE.SaveStateBinary(writer);
                VDC1.SaveStateBinary(writer);
                PSG.SaveStateBinary(writer);
                if (TurboCD)
                {
                    CDAudio.SaveStateBinary(writer);
                }
            } else {
                writer.Write(Ram);
                writer.Write(Frame);
                writer.Write(_lagcount);
                writer.Write(IOBuffer);
                Cpu.SaveStateBinary(writer);
                VCE.SaveStateBinary(writer);
                VPC.SaveStateBinary(writer);
                VDC1.SaveStateBinary(writer);
                VDC2.SaveStateBinary(writer);
                PSG.SaveStateBinary(writer);
            }
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            if (SuperGrafx == false)
            {
                Ram = reader.ReadBytes(0x2000);
                if (BRAM != null)
                    BRAM = reader.ReadBytes(0x800);
                if (PopulousRAM != null)
                    PopulousRAM = reader.ReadBytes(0x8000);
                if (SuperRam != null)
                    SuperRam = reader.ReadBytes(0x30000);
                if (TurboCD)
                {
                    CDRam = reader.ReadBytes(0x10000);
                    ADPCM_RAM = reader.ReadBytes(0x10000);
                }
                Frame = reader.ReadInt32();
                _lagcount = reader.ReadInt32();
                SF2MapperLatch = reader.ReadByte();
                IOBuffer = reader.ReadByte();
                Cpu.LoadStateBinary(reader);
                VCE.LoadStateBinary(reader);
                VDC1.LoadStateBinary(reader);
                PSG.LoadStateBinary(reader);
                if (TurboCD)
                {
                    CDAudio.LoadStateBinary(reader);
                }
            } else {
                Ram = reader.ReadBytes(0x8000);
                Frame = reader.ReadInt32();
                _lagcount = reader.ReadInt32();
                IOBuffer = reader.ReadByte();
                Cpu.LoadStateBinary(reader);
                VCE.LoadStateBinary(reader);
                VPC.LoadStateBinary(reader);
                VDC1.LoadStateBinary(reader);
                VDC2.LoadStateBinary(reader);
                PSG.LoadStateBinary(reader);
            }
        }

        public byte[] SaveStateBinary()
        {
            int buflen = 75870;
            if (SuperGrafx) buflen += 90698;
            if (BramEnabled) buflen += 2048;
            if (PopulousRAM != null) buflen += 0x8000;
            if (SuperRam != null) buflen += 0x30000;
            if (TurboCD) buflen += 0x20000 + 30;
            //Console.WriteLine("LENGTH1 " + buflen);

            var buf = new byte[buflen];
            var stream = new MemoryStream(buf);
            var writer = new BinaryWriter(stream);
            SaveStateBinary(writer);
            //Console.WriteLine("LENGTH2 " + stream.Position);
            writer.Close();
            return buf;
        }

        private void SetupMemoryDomains()
        {
            var domains = new List<MemoryDomain>(10);
            var MainMemoryDomain = new MemoryDomain("Main Memory", Ram.Length, Endian.Little,
                addr => Ram[addr & 0x1FFF],
                (addr, value) => Ram[addr & 0x1FFF] = value);
            domains.Add(MainMemoryDomain);

            var SystemBusDomain = new MemoryDomain("System Bus", 0x2F0000, Endian.Little,
                addr => Cpu.ReadMemory21(addr),
                (addr, value) => Cpu.WriteMemory21(addr, value));
            domains.Add(SystemBusDomain);

            if (BRAM != null)
            {
                var BRAMMemoryDomain = new MemoryDomain("Battery RAM", Ram.Length, Endian.Little,
                    addr => BRAM[addr & 0x7FF],
                    (addr, value) => BRAM[addr & 0x7FF] = value);
                domains.Add(BRAMMemoryDomain);
            }

            if (TurboCD)
            {
                var CDRamMemoryDomain = new MemoryDomain("TurboCD RAM", CDRam.Length, Endian.Little,
                    addr => CDRam[addr & 0xFFFF],
                    (addr, value) => CDRam[addr & 0xFFFF] = value);
                domains.Add(CDRamMemoryDomain);

                var AdpcmMemoryDomain = new MemoryDomain("ADPCM RAM", ADPCM_RAM.Length, Endian.Little,
                    addr => ADPCM_RAM[addr & 0xFFFF],
                    (addr, value) => ADPCM_RAM[addr & 0xFFFF] = value);
                domains.Add(AdpcmMemoryDomain);
                
                if (SuperRam != null)
                {
                    var SuperRamMemoryDomain = new MemoryDomain("Super System Card RAM", SuperRam.Length, Endian.Little,
                        addr => SuperRam[addr & 0x3FFFF],
                        (addr, value) => SuperRam[addr & 0x3FFFF] = value);
                    domains.Add(SuperRamMemoryDomain);
                }
            }

            memoryDomains = domains.AsReadOnly();
        }

        private IList<MemoryDomain> memoryDomains;
        public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
        public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

        public void Dispose() {}
    }
}
