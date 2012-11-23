using System;
using System.Globalization;
using System.IO;

// This Z80 emulator is a modified version of Ben Ryves 'Brazil' emulator.
// It is MIT licensed.

namespace BizHawk.Emulation.CPUs.Z80 
{
	/// <summary>
	/// ZiLOG Z80A CPU Emulator
	/// </summary>
	public sealed partial class Z80A 
    {
		/// <summary>
		/// Creates an instance of the <see cref="Z80A"/> emulator class.
		/// </summary>
		public Z80A() 
        {
			InitialiseTables();
			Reset();
		}

		/// <summary>
		/// Reset the Z80 to its initial state
		/// </summary>
		public void Reset() 
        {
			ResetRegisters();
			ResetInterrupts();
			PendingCycles = 0;
			ExpectedExecutedCycles = 0;
			TotalExecutedCycles = 0;
		}

        /// <summary>
        /// Reset the Z80 to its initial state, but don't modify cycle counters. For use where Z80 may be reset 
        /// </summary>
        public void SoftReset()
        {
            ResetRegisters();
            ResetInterrupts();
        }

        // Memory Access 

        public Func<ushort, byte> ReadMemory;
        public Action<ushort, byte> WriteMemory;

        // Utility function, not used by core
        public ushort ReadWord(ushort addr)
        {
            ushort value = ReadMemory(addr++);
            value |= (ushort) (ReadMemory(addr) << 8);
            return value;
        }

        // Hardware I/O Port Access

        public Func<ushort, byte> ReadHardware;
        public Action<ushort, byte> WriteHardware;

        // State Save/Load

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[Z80]");
            writer.WriteLine("AF {0:X4}", RegAF.Word);
            writer.WriteLine("BC {0:X4}", RegBC.Word);
            writer.WriteLine("DE {0:X4}", RegDE.Word);
            writer.WriteLine("HL {0:X4}", RegHL.Word);
            writer.WriteLine("ShadowAF {0:X4}", RegAltAF.Word);
            writer.WriteLine("ShadowBC {0:X4}", RegAltBC.Word);
            writer.WriteLine("ShadowDE {0:X4}", RegAltDE.Word);
            writer.WriteLine("ShadowHL {0:X4}", RegAltHL.Word);
            writer.WriteLine("I {0:X2}", RegI);
            writer.WriteLine("R {0:X2}", RegR);
            writer.WriteLine("IX {0:X4}", RegIX.Word);
            writer.WriteLine("IY {0:X4}", RegIY.Word);
            writer.WriteLine("SP {0:X4}", RegSP.Word);
            writer.WriteLine("PC {0:X4}", RegPC.Word);
            writer.WriteLine("IRQ {0}", interrupt);
            writer.WriteLine("NMI {0}", nonMaskableInterrupt);
            writer.WriteLine("NMIPending {0}", nonMaskableInterruptPending);
            writer.WriteLine("IM {0}", InterruptMode);
            writer.WriteLine("IFF1 {0}", IFF1);
            writer.WriteLine("IFF2 {0}", IFF2);
            writer.WriteLine("Halted {0}", Halted);
            writer.WriteLine("ExecutedCycles {0}", TotalExecutedCycles);
            writer.WriteLine("PendingCycles {0}", PendingCycles);
            writer.WriteLine("[/Z80]");
            writer.WriteLine();
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/Z80]") break;
                if (args[0] == "AF")
                    RegAF.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "BC")
                    RegBC.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "DE")
                    RegDE.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "HL")
                    RegHL.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ShadowAF")
                    RegAltAF.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ShadowBC")
                    RegAltBC.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ShadowDE")
                    RegAltDE.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ShadowHL")
                    RegAltHL.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "I")
                    RegI = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "R")
                    RegR = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "IX")
                    RegIX.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "IY")
                    RegIY.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "SP")
                    RegSP.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "PC")
                    RegPC.Word = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "IRQ")
                    interrupt = bool.Parse(args[1]);
                else if (args[0] == "NMI")
                    nonMaskableInterrupt = bool.Parse(args[1]);
                else if (args[0] == "NMIPending")
                    nonMaskableInterruptPending = bool.Parse(args[1]);
                else if (args[0] == "IM")
                    InterruptMode = int.Parse(args[1]);
                else if (args[0] == "IFF1")
                    IFF1 = bool.Parse(args[1]);
                else if (args[0] == "IFF2")
                    IFF2 = bool.Parse(args[1]);
                else if (args[0] == "Halted")
                    Halted = bool.Parse(args[1]);
                else if (args[0] == "ExecutedCycles")
                    TotalExecutedCycles = int.Parse(args[1]);
                else if (args[0] == "PendingCycles")
                    PendingCycles = int.Parse(args[1]);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(RegAF.Word);
            writer.Write(RegBC.Word);
            writer.Write(RegDE.Word);
            writer.Write(RegHL.Word);
            writer.Write(RegAltAF.Word);
            writer.Write(RegAltBC.Word);
            writer.Write(RegAltDE.Word);
            writer.Write(RegAltHL.Word);
            writer.Write(RegAltAF.Word);
            writer.Write(RegI);
            writer.Write(RegR);
            writer.Write(RegIX.Word);
            writer.Write(RegIY.Word);
            writer.Write(RegSP.Word);
            writer.Write(RegPC.Word);
            writer.Write(interrupt);
            writer.Write(nonMaskableInterrupt);
            writer.Write(nonMaskableInterruptPending);
            writer.Write(InterruptMode);
            writer.Write(IFF1);
            writer.Write(IFF2);
            writer.Write(Halted);
            writer.Write(TotalExecutedCycles);
            writer.Write(PendingCycles);
        }

	    public void LoadStateBinary(BinaryReader reader)
        {
	        RegAF.Word = reader.ReadUInt16();
            RegBC.Word = reader.ReadUInt16();
            RegDE.Word = reader.ReadUInt16();
            RegHL.Word = reader.ReadUInt16();
            RegAltAF.Word = reader.ReadUInt16();
            RegAltBC.Word = reader.ReadUInt16();
            RegAltDE.Word = reader.ReadUInt16();
            RegAltHL.Word = reader.ReadUInt16();
            RegAltAF.Word = reader.ReadUInt16();
	        RegI = reader.ReadByte();
            RegR = reader.ReadByte();
            RegIX.Word = reader.ReadUInt16();
            RegIY.Word = reader.ReadUInt16();
            RegSP.Word = reader.ReadUInt16();
            RegPC.Word = reader.ReadUInt16();
	        interrupt = reader.ReadBoolean();
	        nonMaskableInterrupt = reader.ReadBoolean();
	        nonMaskableInterruptPending = reader.ReadBoolean();
	        InterruptMode = reader.ReadInt32();
	        IFF1 = reader.ReadBoolean();
	        IFF2 = reader.ReadBoolean();
	        Halted = reader.ReadBoolean();
	        TotalExecutedCycles = reader.ReadInt32();
	        PendingCycles = reader.ReadInt32();
        }
    }
}
