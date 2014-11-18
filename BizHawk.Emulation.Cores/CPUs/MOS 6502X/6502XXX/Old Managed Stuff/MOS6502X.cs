using System;


using BizHawk.Common;

using System.Runtime.InteropServices;
using System.Security;

using System.Reflection;
using System.Reflection.Emit;

namespace BizHawk.Emulation.Cores.Components.M6502
{
	[StructLayout(LayoutKind.Explicit)] // LayoutKind.Sequential doesn't work right on the managed form of non-blittable structs
	public sealed partial class MOS6502X
	{
		[FieldOffset(0)]
		private int _anchor;

		[SuppressUnmanagedCodeSecurity]
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate byte ReadDel(ushort addr);
		[SuppressUnmanagedCodeSecurity]
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void WriteDel(ushort addr, byte val);
		[SuppressUnmanagedCodeSecurity]
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void AddrDel(ushort addr);

		// interface
		[FieldOffset(16)]private ReadDel _ReadMemory;
		[FieldOffset(20)]private ReadDel _DummyReadMemory;
		[FieldOffset(24)]private ReadDel _PeekMemory;
		[FieldOffset(28)]private WriteDel _WriteMemory;
		[FieldOffset(32)]private AddrDel _OnExecFetch; // this only calls when the first byte of an instruction is fetched.

		public ReadDel ReadMemory
		{
			set { _rmp = Marshal.GetFunctionPointerForDelegate(value); _ReadMemory = value; }
			get { return _ReadMemory; }
		}
		public ReadDel DummyReadMemory
		{
			set { _dmp = Marshal.GetFunctionPointerForDelegate(value); _DummyReadMemory = value; }
			get { return _DummyReadMemory; }
		}
		public ReadDel PeekMemory
		{ 
			set { _pmp = Marshal.GetFunctionPointerForDelegate(value); _PeekMemory = value; }
			get { return _PeekMemory; }
		}
		public WriteDel WriteMemory
		{
			set { _wmp = Marshal.GetFunctionPointerForDelegate(value); _WriteMemory = value; }
			get { return _WriteMemory; }
		}
		public AddrDel OnExecFetch
		{
			set { _exp = IntPtr.Zero; /* Marshal.GetFunctionPointerForDelegate(value); */ _OnExecFetch = value; }
			get { return _OnExecFetch; }
		}

		[FieldOffset(36)]public Action<string> TraceCallback; // TODOOO

		[FieldOffset(40)]private IntPtr _rmp;
		[FieldOffset(44)]private IntPtr _dmp;
		[FieldOffset(48)]private IntPtr _pmp;
		[FieldOffset(52)]private IntPtr _wmp;
		[FieldOffset(56)]private IntPtr _exp;

		// config
		[FieldOffset(60)]public bool BCD_Enabled = true;
		[FieldOffset(61)]public bool debug = false;

		// state
		[FieldOffset(62)]public byte A;
		[FieldOffset(63)]public byte X;
		[FieldOffset(64)]public byte Y;
		//public byte P;
		/// <summary>Carry Flag</summary>
		[FieldOffset(65)]public bool FlagC;
		/// <summary>Zero Flag</summary>
		[FieldOffset(66)]public bool FlagZ;
		/// <summary>Interrupt Disable Flag</summary>
		[FieldOffset(67)]public bool FlagI;
		/// <summary>Decimal Mode Flag</summary>
		[FieldOffset(68)]public bool FlagD;
		/// <summary>Break Flag</summary>
		[FieldOffset(69)]public bool FlagB;
		/// <summary>T... Flag</summary>
		[FieldOffset(70)]public bool FlagT;
		/// <summary>Overflow Flag</summary>
		[FieldOffset(71)]public bool FlagV;
		/// <summary>Negative Flag</summary>
		[FieldOffset(72)]public bool FlagN;

		[FieldOffset(74)]public ushort PC;
		[FieldOffset(76)]public byte S;

		[FieldOffset(77)]public bool IRQ;
		[FieldOffset(78)]public bool NMI;
		[FieldOffset(79)]public bool RDY;

		[FieldOffset(80)]public int TotalExecutedCycles;

		//opcode bytes.. theoretically redundant with the temp variables? who knows.
		[FieldOffset(84)]int opcode;
		[FieldOffset(88)]byte opcode2;
		[FieldOffset(89)]byte opcode3;

		[FieldOffset(92)]int ea;
		[FieldOffset(96)]int alu_temp; //cpu internal temp variables
		[FieldOffset(100)]int mi; //microcode index
		[FieldOffset(104)]bool iflag_pending; //iflag must be stored after it is checked in some cases (CLI and SEI).
		[FieldOffset(105)]bool rdy_freeze; //true if the CPU must be frozen

		//tracks whether an interrupt condition has popped up recently.
		//not sure if this is real or not but it helps with the branch_irq_hack
		[FieldOffset(106)]bool interrupt_pending;
		[FieldOffset(107)]bool branch_irq_hack; //see Uop.RelBranch_Stage3 for more details

		// transient state
		[FieldOffset(108)]byte value8;
		[FieldOffset(109)]byte temp8;
		[FieldOffset(110)]ushort value16;
		[FieldOffset(112)]bool branch_taken = false;
		[FieldOffset(113)]bool my_iflag;
		[FieldOffset(114)]bool booltemp;
		[FieldOffset(116)]int tempint;
		[FieldOffset(120)]int lo;
		[FieldOffset(124)]int hi;

		public byte P
		{
			// NVTB DIZC
			get
			{
				byte ret = 0;
				if (FlagC) ret |= 1;
				if (FlagZ) ret |= 2;
				if (FlagI) ret |= 4;
				if (FlagD) ret |= 8;
				if (FlagB) ret |= 16;
				if (FlagT) ret |= 32;
				if (FlagV) ret |= 64;
				if (FlagN) ret |= 128;
				return ret;
			}
			set
			{
				FlagC = (value & 1) != 0;
				FlagZ = (value & 2) != 0;
				FlagI = (value & 4) != 0;
				FlagD = (value & 8) != 0;
				FlagB = (value & 16) != 0;
				FlagT = (value & 32) != 0;
				FlagV = (value & 64) != 0;
				FlagN = (value & 128) != 0;
			}
		}

		public MOS6502X()
		{
			InitOpcodeHandlers();
			InitNative();
			Reset();
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MOS6502X");
			ser.Sync("A", ref A);
			ser.Sync("X", ref X);
			ser.Sync("Y", ref Y);
			{
				byte tmp = P;
				ser.Sync("P", ref tmp);
				P = tmp;
			}
			ser.Sync("PC", ref PC);
			ser.Sync("S", ref S);
			ser.Sync("NMI", ref NMI);
			ser.Sync("IRQ", ref IRQ);
			ser.Sync("RDY", ref RDY);
			ser.Sync("TotalExecutedCycles", ref TotalExecutedCycles);
			ser.Sync("opcode", ref opcode);
			ser.Sync("opcode2", ref opcode2);
			ser.Sync("opcode3", ref opcode3);
			ser.Sync("ea", ref ea);
			ser.Sync("alu_temp", ref alu_temp);
			ser.Sync("mi", ref mi);
			ser.Sync("iflag_pending", ref iflag_pending);
			ser.Sync("rdy_freeze", ref rdy_freeze);
			ser.Sync("interrupt_pending", ref interrupt_pending);
			ser.Sync("branch_irq_hack", ref branch_irq_hack);
			ser.EndSection();
		}


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
			RDY = true;
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
			string a = string.Format("{0:X4}  {1:X2} {2} ", PC, _PeekMemory(PC), disassemble ? Disassemble(PC, out notused) : "---").PadRight(30);
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


		#region native interop


		private void InitNative()
		{
		}

		[SuppressUnmanagedCodeSecurity]
		[DllImport("6502XXX.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint="_ZN3CPU10ExecuteOneEv")]
		//[DllImport("6502XXX.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint="?ExecuteOne@CPU@@QAEXXZ")]
		private static extern void ExecuteOneNativeInternal(IntPtr o);

		public unsafe void ExecuteOneNative()
		{
			fixed (int* p = &_anchor)
			{
				ExecuteOneNativeInternal((IntPtr)p);
			}
		}

		#endregion

		public void SetCallbacks
		(
			ReadDel ReadMemory,
			ReadDel DummyReadMemory,
			ReadDel PeekMemory,
			WriteDel WriteMemory
		)
		{
			this.ReadMemory = ReadMemory;
			this.DummyReadMemory = DummyReadMemory;
			this.PeekMemory = PeekMemory;
			this.WriteMemory = WriteMemory;
		}

		public ushort ReadWord(ushort address)
		{
			byte l = _ReadMemory(address);
			byte h = _ReadMemory(++address);
			return (ushort)((h << 8) | l);
		}

		public ushort PeekWord(ushort address)
		{
			byte l = _PeekMemory(address);
			byte h = _PeekMemory(++address);
			return (ushort)((h << 8) | l);
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
