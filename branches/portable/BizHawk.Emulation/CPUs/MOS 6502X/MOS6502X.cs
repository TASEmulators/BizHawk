using System;
using System.Globalization;
using System.IO;

namespace BizHawk.Emulation.CPUs.M6502
{
	public sealed partial class MOS6502X
	{
		public MOS6502X(Action<System.Runtime.InteropServices.GCHandle> DisposeBuilder = null)
		{
			InitOpcodeHandlers();
			Reset();
		}

		public bool BCD_Enabled = true;
		public bool debug = false;
		public bool throw_unhandled;

		public void Reset()
		{
			A = 0;
			X = 0;
			Y = 0;
			P = 0;
			S = 0;
			PC = 0;
			TotalExecutedCycles = 0;
			mi = 0;
			opcode = 256;
			iflag_pending = true;
		}

		public void NESSoftReset()
		{
			opcode = VOP_RESET;
			mi = 0;
			iflag_pending = true;
			FlagI = true;
		}

		public string State(bool disassemble = true)
		{
			int notused;
			string a = string.Format("{0:X4}  {1:X2} {2} ", PC, PeekMemory(PC), disassemble ? Disassemble(PC, out notused) : "---").PadRight(30);
			string b = string.Format("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3:X2} SP:{4:X2} Cy:{5}", A, X, Y, P, S, TotalExecutedCycles);
			string val = a + b + "   ";
			if (FlagN) val = val + "N";
			if (FlagV) val = val + "V";
			if (FlagT) val = val + "T";
			if (FlagB) val = val + "B";
			if (FlagD) val = val + "D";
			if (FlagI) val = val + "I";
			if (FlagZ) val = val + "Z";
			if (FlagC) val = val + "C";
			return val;
		}

		public string TraceState()
		{
			// only disassemble when we're at the beginning of an opcode
			return State(opcode == VOP_Fetch1 || Microcode[opcode][mi] >= Uop.End);
		}

		public const ushort NMIVector = 0xFFFA;
		public const ushort ResetVector = 0xFFFC;
		public const ushort BRKVector = 0xFFFE;
		public const ushort IRQVector = 0xFFFE;

		enum ExceptionType
		{
			BRK, NMI, IRQ
		}


		// ==== CPU State ====

		public byte A;
		public byte X;
		public byte Y;
		public byte P;
		public ushort PC;
		public byte S;

		public bool IRQ;
		public bool NMI;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MOS6502X");
			ser.Sync("A", ref A);
			ser.Sync("X", ref X);
			ser.Sync("Y", ref Y);
			ser.Sync("P", ref P);
			ser.Sync("PC", ref PC);
			ser.Sync("S", ref S);
			ser.Sync("NMI", ref NMI);
			ser.Sync("IRQ", ref IRQ);
			ser.Sync("TotalExecutedCycles", ref TotalExecutedCycles);
			ser.Sync("opcode", ref opcode);
			ser.Sync("opcode2", ref opcode2);
			ser.Sync("opcode3", ref opcode3);
			ser.Sync("ea", ref ea);
			ser.Sync("alu_temp", ref alu_temp);
			ser.Sync("mi", ref mi);
			ser.Sync("iflag_pending", ref iflag_pending);
			ser.Sync("interrupt_pending", ref interrupt_pending);
			ser.Sync("branch_irq_hack", ref branch_irq_hack);
			ser.EndSection();
		}

		public void SaveStateBinary(BinaryWriter writer) { SyncState(Serializer.CreateBinaryWriter(writer)); }
		public void LoadStateBinary(BinaryReader reader) { SyncState(Serializer.CreateBinaryReader(reader)); }

		// ==== End State ====

		/// <summary>Carry Flag</summary>
		private bool FlagC
		{
			get { return (P & 0x01) != 0; }
			set { P = (byte)((P & ~0x01) | (value ? 0x01 : 0x00)); }
		}

		/// <summary>Zero Flag</summary>
		private bool FlagZ
		{
			get { return (P & 0x02) != 0; }
			set { P = (byte)((P & ~0x02) | (value ? 0x02 : 0x00)); }
		}

		/// <summary>Interrupt Disable Flag</summary>
		public bool FlagI
		{
			get { return (P & 0x04) != 0; }
			set { P = (byte)((P & ~0x04) | (value ? 0x04 : 0x00)); }
		}

		/// <summary>Decimal Mode Flag</summary>
		private bool FlagD
		{
			get { return (P & 0x08) != 0; }
			set { P = (byte)((P & ~0x08) | (value ? 0x08 : 0x00)); }
		}

		/// <summary>Break Flag</summary>
		private bool FlagB
		{
			get { return (P & 0x10) != 0; }
			set { P = (byte)((P & ~0x10) | (value ? 0x10 : 0x00)); }
		}

		/// <summary>T... Flag</summary>
		private bool FlagT
		{
			get { return (P & 0x20) != 0; }
			set { P = (byte)((P & ~0x20) | (value ? 0x20 : 0x00)); }
		}

		/// <summary>Overflow Flag</summary>
		private bool FlagV
		{
			get { return (P & 0x40) != 0; }
			set { P = (byte)((P & ~0x40) | (value ? 0x40 : 0x00)); }
		}

		/// <summary>Negative Flag</summary>
		private bool FlagN
		{
			get { return (P & 0x80) != 0; }
			set { P = (byte)((P & ~0x80) | (value ? 0x80 : 0x00)); }
		}

		public int TotalExecutedCycles;

		public Func<ushort, byte> ReadMemory;
		public Func<ushort, byte> DummyReadMemory;
		public Func<ushort, byte> PeekMemory;
		public Action<ushort, byte> WriteMemory;

		public void SetCallbacks
		(
			Func<ushort, byte> ReadMemory,
			Func<ushort, byte> DummyReadMemory,
			Func<ushort, byte> PeekMemory,
			Action<ushort, byte> WriteMemory,
			Action<System.Runtime.InteropServices.GCHandle> DisposeBuilder
		)
		{
			this.ReadMemory = ReadMemory;
			this.DummyReadMemory = DummyReadMemory;
			this.PeekMemory = PeekMemory;
			this.WriteMemory = WriteMemory;
		}

		public ushort ReadWord(ushort address)
		{
			byte l = ReadMemory(address);
			byte h = ReadMemory(++address);
			return (ushort)((h << 8) | l);
		}

		private void WriteWord(ushort address, ushort value)
		{
			byte l = (byte)(value & 0xFF);
			byte h = (byte)(value >> 8);
			WriteMemory(address, l);
			WriteMemory(++address, h);
		}

		private ushort ReadWordPageWrap(ushort address)
		{
			ushort highAddress = (ushort)((address & 0xFF00) + ((address + 1) & 0xFF));
			return (ushort)(ReadMemory(address) | (ReadMemory(highAddress) << 8));
		}

		private static readonly byte[] TableNZ = 
        { 
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80
        };
	}
}