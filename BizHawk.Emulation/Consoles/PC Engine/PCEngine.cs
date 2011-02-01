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
        public int RomPages;

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
        
        // At 60.00 FPS
        //    357,954  mclks / frame
        //    119,318  Cpu cycles / frame

        // 263 lines / frame:
        //       1361  mclks / line
        //        454  Cpu cycles / line

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
                Cpu.ReadMemory = ReadMemory;
                Cpu.WriteMemory = WriteMemory;
                Cpu.WriteVDC = VDC1.WriteVDC;
            }

            if (SuperGrafx)
            {
                VDC2 = new VDC(Cpu, VCE);
                VPC = new VPC(VDC1, VDC2, VCE, Cpu);
                Ram = new byte[0x8000];
                Cpu.ReadMemory = ReadMemorySGX;
                Cpu.WriteMemory = WriteMemorySGX;
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
                RomPages = RomData.Length/8192;
            } else if (game.GetRomData().Length > 1024 * 1024) {
                // If the rom is bigger than 1 megabyte, switch to Street Fighter 2 mapper
                Cpu.ReadMemory = ReadMemorySF2;
                Cpu.WriteMemory = WriteMemorySF2;
                RomData = game.GetRomData();
                RomPages = RomData.Length / 8192;
            } else {
                // normal rom.
                RomData = game.GetRomData();
                RomPages = RomData.Length / 8192;
            }
            Cpu.ResetPC();
            SetupMemoryDomains();
        }

        public int Frame { get; set; }

        public void FrameAdvance(bool render)
        {
            Controller.FrameNumber = Frame++;
            //Log.Note("CPU","======== FRAME {0} =========",Frame);
            PSG.BeginFrame(Cpu.TotalExecutedCycles);

            if (SuperGrafx)
                VPC.ExecFrame(); // TODO supergrafx frameskipping (waiting on a larger update of VPC frame timing, once I get VDC timing correct)
            else
                VDC1.ExecFrame(render);

            PSG.EndFrame(Cpu.TotalExecutedCycles);
        }

        public IVideoProvider VideoProvider
        {
            get { return (IVideoProvider) VPC ?? VDC1; }
        }

        public ISoundProvider SoundProvider
        {
            get { return PSG; }
        }

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
            if (Cpu.ReadMemory == ReadMemorySF2)
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
                    VDC1.LoadStateText(reader,1);
                else if (args[0] == "[VDC2]")
                    VDC2.LoadStateText(reader,2);
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
                writer.Write(SF2MapperLatch);
                writer.Write(IOBuffer);
                Cpu.SaveStateBinary(writer);
                VCE.SaveStateBinary(writer);
                VDC1.SaveStateBinary(writer);
                PSG.SaveStateBinary(writer);
            } else {
                writer.Write(Ram);
                writer.Write(Frame);
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
                SF2MapperLatch = reader.ReadByte();
                IOBuffer = reader.ReadByte();
                Cpu.LoadStateBinary(reader);
                VCE.LoadStateBinary(reader);
                VDC1.LoadStateBinary(reader);
                PSG.LoadStateBinary(reader);
            } else {
                Ram = reader.ReadBytes(0x8000);
                Frame = reader.ReadInt32();
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
            var buf = new byte[SuperGrafx ? 166551 : 75853];
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
            domains.Add(MainMemoryDomain);
            memoryDomains = domains.AsReadOnly();
        }

        private IList<MemoryDomain> memoryDomains;
        public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
        public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
    }
}
