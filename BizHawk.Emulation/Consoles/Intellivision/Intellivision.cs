using System;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.CP1610;

namespace BizHawk.Emulation.Consoles.Mattel
{
    public sealed partial class Intellivision : IEmulator
    {
        byte[] Rom;
        GameInfo Game;

        CP1610 Cpu ;

        public Intellivision(GameInfo game, byte[] rom)
        {
            Rom = rom;
            Game = game;
            
            Cpu = new CP1610();
            Cpu.ReadMemory = ReadMemory;
            Cpu.WriteMemory = WriteMemory;
            
            CoreOutputComm = new CoreOutputComm();
        }

        public byte ReadMemory(ushort addr)
        {
            return 0xFF; // TODO you need to implement a memory mapper.
        }

        public void WriteMemory(ushort addr, byte value)
        {
            // TODO
        }

        public void FrameAdvance(bool render)
        {
            Cpu.Execute(999); // execute some cycles. this will do nothing useful until a memory mapper is created.
        }



        // This is all crap to worry about later.

        public IVideoProvider VideoProvider { get { return new NullEmulator(); } }
        public ISoundProvider SoundProvider { get { return NullSound.SilenceProvider; } }

        public ControllerDefinition ControllerDefinition
        {
            get { return null; }
        }

        public IController Controller { get; set; }

        
        public int Frame
        {
            get { return 0; }
        }

        public int LagCount
        {
            get { return 0; }
            set { }
        }

        public bool IsLagFrame { get { return false; } }
        public string SystemId
        {
            get { return "INTV"; }
        }

        public bool DeterministicEmulation { get; set; }

        public byte[] SaveRam { get { return null; } }

        public bool SaveRamModified
        {
            get { return false; }
            set { }
        }

        public void ResetFrameCounter()
        {
        }

        public void SaveStateText(TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public void LoadStateText(TextReader reader)
        {
            throw new NotImplementedException();
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public byte[] SaveStateBinary()
        {
            return new byte[0];
        }

        public CoreInputComm CoreInputComm { get; set; }
        public CoreOutputComm CoreOutputComm { get; private set; }

        public IList<MemoryDomain> MemoryDomains
        {
            get { throw new NotImplementedException(); }
        }

        public MemoryDomain MainMemory
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }
    }
}