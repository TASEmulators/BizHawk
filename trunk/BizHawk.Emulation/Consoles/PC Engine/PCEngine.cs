using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.H6280;
using BizHawk.Emulation.Sound;

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

        // Machine
        public NecSystemType Type;
        public HuC6280 Cpu;
        public VDC VDC1, VDC2;
        public VCE VCE;
        public HuC6280PSG PSG;
        public VPC VPC;

        private bool TurboGrafx { get { return Type == NecSystemType.TurboGrafx; } }
        private bool SuperGrafx { get { return Type == NecSystemType.SuperGrafx; } }
        private bool TurboCD    { get { return Type == NecSystemType.TurboCD; } }

        // Memory system
        public byte[] Ram;

        // PC Engine timings:
        // 21,477,270  Machine clocks / sec
        //  7,159,090  Cpu cycles / sec

        public PCEngine(NecSystemType type)
        {
            Type = type;
            Controller = NullController.GetNullController();
            Cpu = new HuC6280();
            VCE = new VCE();
            VDC1 = new VDC(Cpu, VCE);
            PSG = new HuC6280PSG();

            if (TurboGrafx || TurboCD)
            {
                Ram = new byte[0x2000];
                Cpu.ReadMemory21 = ReadMemory;
                Cpu.WriteMemory21 = WriteMemory;
                Cpu.WriteVDC = VDC1.WriteVDC;
            }

            if (SuperGrafx)
            {
                VDC2 = new VDC(Cpu, VCE);
                VPC = new VPC(VDC1, VDC2, VCE, Cpu);
                Ram = new byte[0x8000];
                Cpu.ReadMemory21 = ReadMemorySGX;
                Cpu.WriteMemory21 = WriteMemorySGX;
                Cpu.WriteVDC = VDC1.WriteVDC;
            }
        }

        public void LoadGame(IGame game)
        {
            if (game.GetRomData().Length == 0x60000)
            {
                // 384k roms require special loading code. Why ;_;
                // In memory, 384k roms look like [1st 256k][Then full 384k]
                RomData = new byte[0xA0000];
                var origRom = game.GetRomData();
                for (int i=0; i<0x40000; i++)
                    RomData[i] = origRom[i];
                for (int i = 0; i < 0x60000; i++)
                    RomData[i+0x40000] = origRom[i];
                RomLength = RomData.Length;
            } else if (game.GetRomData().Length > 1024 * 1024) {
                // If the rom is bigger than 1 megabyte, switch to Street Fighter 2 mapper
                Cpu.ReadMemory21 = ReadMemorySF2;
                Cpu.WriteMemory21 = WriteMemorySF2;
                RomData = game.GetRomData();
                RomLength = RomData.Length;
            } else {
                // normal rom.
                RomData = game.GetRomData();
                RomLength = RomData.Length;
            }
            Cpu.ResetPC();
            SetupMemoryDomains();
        }

        private int _lagcount = 0;
        private bool lagged = true;
        public int Frame { get; set; }
        public int LagCount { get { return _lagcount; } set { _lagcount = value; } } //TODO: Implement this

        public void FrameAdvance(bool render)
        {
            lagged = true;
            Controller.UpdateControls(Frame++);

            PSG.BeginFrame(Cpu.TotalExecutedCycles);

            if (SuperGrafx)
                VPC.ExecFrame(); // TODO supergrafx frameskipping (waiting on a larger update of VPC frame timing, once I get VDC timing correct)
            else
                VDC1.ExecFrame(render);

            PSG.EndFrame(Cpu.TotalExecutedCycles);
            if (lagged)
                _lagcount++;
        }

        public IVideoProvider VideoProvider
        {
            get { return (IVideoProvider) VPC ?? VDC1; }
        }

        public ISoundProvider SoundProvider
        {
            get { return PSG; }
        }
 
        public string SystemId { get { return "PCE"; } }
        public string Region { get; set; }
        public bool DeterministicEmulation { get; set; }

        public byte[] SaveRam
        {
            get { throw new NotImplementedException(); }
        }

        public bool SaveRamModified
        {
            get { return false; }
            set { throw new NotImplementedException(); }
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[PCEngine]");
            writer.Write("RAM ");
            Ram.SaveAsHex(writer);
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
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            if (SuperGrafx == false)
            {
                writer.Write(Ram);
                writer.Write(Frame);
//                writer.Write(_lagcount); //TODO: why does this fail?
                writer.Write(SF2MapperLatch);
                writer.Write(IOBuffer);
                Cpu.SaveStateBinary(writer);
                VCE.SaveStateBinary(writer);
                VDC1.SaveStateBinary(writer);
                PSG.SaveStateBinary(writer);
            } else {
                writer.Write(Ram);
                writer.Write(Frame);
//                writer.Write(_lagcount);
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
                Frame = reader.ReadInt32();
//                _lagcount = reader.ReadInt32();
                SF2MapperLatch = reader.ReadByte();
                IOBuffer = reader.ReadByte();
                Cpu.LoadStateBinary(reader);
                VCE.LoadStateBinary(reader);
                VDC1.LoadStateBinary(reader);
                PSG.LoadStateBinary(reader);
            } else {
                Ram = reader.ReadBytes(0x8000);
                Frame = reader.ReadInt32();
//                _lagcount = reader.ReadInt32();
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
            var buf = new byte[SuperGrafx ? 166552 : 75854];
            var stream = new MemoryStream(buf);
            var writer = new BinaryWriter(stream);
            SaveStateBinary(writer);
            //Console.WriteLine("LENGTH " + stream.Position);
            writer.Close();
            return buf;
        }

        private void SetupMemoryDomains()
        {
            var domains = new List<MemoryDomain>(1);
            var MainMemoryDomain = new MemoryDomain("Main Memory", Ram.Length, Endian.Little,
                addr => Ram[addr & 0x7FFF],
                (addr, value) => Ram[addr & 0x7FFF] = value);
            var SystemBusDomain = new MemoryDomain("System Bus", 0x2F0000, Endian.Little,
                addr => Cpu.ReadMemory21(addr),
                (addr, value) => Cpu.WriteMemory21(addr, value));
            domains.Add(MainMemoryDomain);
            domains.Add(SystemBusDomain);
            memoryDomains = domains.AsReadOnly();
        }

        private IList<MemoryDomain> memoryDomains;
        public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
        public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public object Query(EmulatorQuery query)
		{
			switch (query)
			{
				case EmulatorQuery.VsyncRate:
					return 60.0;
				default:
					return null;
			}
		}
    }
}
