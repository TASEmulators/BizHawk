using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.M6502
{
	public static class MOS6502X_DLL
	{
		[UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
		public delegate byte ReadMemoryD(ushort addr);
		[UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
		public delegate void WriteMemoryD(ushort addr, byte value);

		[DllImport("MOS6502XNative.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr Create();
		[DllImport("MOS6502XNative.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void Destroy(IntPtr ptr);

		[DllImport("MOS6502XNative.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?Reset@MOS6502X@@QAEXXZ")]
		public static extern void Reset(IntPtr ptr);

		[DllImport("MOS6502XNative.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?NESSoftReset@MOS6502X@@QAEXXZ")]
		public static extern void NESSoftReset(IntPtr ptr);

		[DllImport("MOS6502XNative.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?ExecuteOne@MOS6502X@@QAEXXZ")]
		public static extern void ExecuteOne(IntPtr ptr);

		[DllImport("MOS6502XNative.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetTrampolines@MOS6502X@@QAEXP6AEG@Z0P6AXGE@Z@Z")]
		public static extern void SetTrampolines(IntPtr ptr, ReadMemoryD Read, ReadMemoryD DummyRead, WriteMemoryD Write);
	}

	/// <summary>
	/// MOS6502X core in unmanaged code
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public class MOS6502X_CPP
	{
		/*
		 * In order to get anywhere near usable performance, the class is cleared of all unblittable types,
		 * set up with identical memory order to a C++ class, and then GC pinned for the duration.  A
		 * naive pinvoke attempt will produce 1/7th the performance of this.
		 */

		// these are aliased to a C++ class, so don't move them
		#region c++ alias

		// C# bool is not blittable!
		[FieldOffset(0x00), MarshalAs(UnmanagedType.U1)]
		byte _BCD_Enabled;
		[FieldOffset(0x01), MarshalAs(UnmanagedType.U1)]
		public byte debug;
		[FieldOffset(0x02), MarshalAs(UnmanagedType.U1)]
		public byte throw_unhandled;

		[FieldOffset(0x03)]
		public byte A;
		[FieldOffset(0x04)]
		public byte X;
		[FieldOffset(0x05)]
		public byte Y;
		[FieldOffset(0x06)]
		public byte P;
		[FieldOffset(0x08)]
		public ushort PC;
		[FieldOffset(0x0a)]
		public byte S;

		[FieldOffset(0x0b), MarshalAs(UnmanagedType.U1)]
		byte _IRQ;
		[FieldOffset(0x0c), MarshalAs(UnmanagedType.U1)]
		byte _NMI;

		[FieldOffset(0x10)]
		public int TotalExecutedCycles;

		// delegates are not blittable, so pretend they aren't there
		[FieldOffset(0x14)]
		IntPtr ZZZ000;
		//public MOS6502X_DLL.ReadMemoryD ReadMemory;
		[FieldOffset(0x18)]
		IntPtr ZZZ001;
		//public MOS6502X_DLL.ReadMemoryD DummyReadMemory;
		[FieldOffset(0x1c)]
		IntPtr ZZZ002;
		//public MOS6502X_DLL.WriteMemoryD WriteMemory;

		//opcode bytes.. theoretically redundant with the temp variables? who knows.
		[FieldOffset(0x20)]
		public int opcode;
		[FieldOffset(0x24)]
		public byte opcode2;
		[FieldOffset(0x25)]
		public byte opcode3;

		[FieldOffset(0x28)]
		public int ea;
		[FieldOffset(0x2c)]
		public int alu_temp; //cpu internal temp variables
		[FieldOffset(0x30)]
		public int mi; //microcode index
		[FieldOffset(0x34), MarshalAs(UnmanagedType.U1)]
		public byte iflag_pending; //iflag must be stored after it is checked in some cases (CLI and SEI).

		//tracks whether an interrupt condition has popped up recently.
		//not sure if this is real or not but it helps with the branch_irq_hack
		[FieldOffset(0x35), MarshalAs(UnmanagedType.U1)]
		public byte interrupt_pending;
		[FieldOffset(0x36), MarshalAs(UnmanagedType.U1)]
		public byte branch_irq_hack; //see Uop.RelBranch_Stage3 for more details

		#endregion

		[FieldOffset(0x48)]
		IntPtr pthis;
		
		// for fields which were converted from bool to byte, use props for backwards compatibility
		public bool IRQ { get { return _IRQ != 0; } set { _IRQ = (byte)(value ? 1 : 0); } }
		public bool NMI { get { return _NMI != 0; } set { _NMI = (byte)(value ? 1 : 0); } }
		public bool BCD_Enabled { get { return _BCD_Enabled != 0; } set { _BCD_Enabled = (byte)(value ? 1 : 0); } }

		public MOS6502X_CPP(Action<GCHandle> DisposeBuilder)
		{
			// this bit of foolery is only needed if you actually need to run the native-side constructor
			//IntPtr native = MOS6502X_DLL.Create();
			//if (native == null)
			//	throw new Exception("Native constructor returned null!");

			var h = GCHandle.Alloc(this, GCHandleType.Pinned);
			pthis = h.AddrOfPinnedObject();

			// bad - use memcpy instead
			//Marshal.PtrToStructure(native, this);

			//MOS6502X_DLL.Destroy(native);

			BCD_Enabled = true;

			MOS6502X_DLL.Reset(pthis);

			DisposeBuilder(h);
		}
		
		public void Reset() { MOS6502X_DLL.Reset(pthis); }
		public void NESSoftReset() { MOS6502X_DLL.NESSoftReset(pthis); }
		public void ExecuteOne() { MOS6502X_DLL.ExecuteOne(pthis); }

		public string State() { return "FOOBAR"; } /*
		{
			int notused;
			string a = string.Format("{0:X4}  {1:X2} {2} ", PC, ReadMemory(PC), Disassemble(PC, out notused)).PadRight(30);
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
		}*/

		// to maintain savestate compatibility, we have bytes that we serialize as bool
		static void SyncByteFakeBool(Serializer ser, string name, ref byte loc)
		{
			bool tmp = loc != 0;
			ser.Sync(name, ref tmp);
			loc = (byte)(tmp ? 1 : 0);
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MOS6502X");
			ser.Sync("A", ref A);
			ser.Sync("X", ref X);
			ser.Sync("Y", ref Y);
			ser.Sync("P", ref P);
			ser.Sync("PC", ref PC);
			ser.Sync("S", ref S);
			SyncByteFakeBool(ser, "NMI", ref _NMI);//ser.Sync("NMI", ref _NMI);
			SyncByteFakeBool(ser, "IRQ", ref _IRQ);//ser.Sync("IRQ", ref _IRQ);
			ser.Sync("TotalExecutedCycles", ref TotalExecutedCycles);
			ser.Sync("opcode", ref opcode);
			ser.Sync("opcode2", ref opcode2);
			ser.Sync("opcode3", ref opcode3);
			ser.Sync("ea", ref ea);
			ser.Sync("alu_temp", ref alu_temp);
			ser.Sync("mi", ref mi);
			SyncByteFakeBool(ser, "iflag_pending", ref iflag_pending); //ser.Sync("iflag_pending", ref iflag_pending);
			SyncByteFakeBool(ser, "interrupt_pending", ref interrupt_pending); //ser.Sync("interrupt_pending", ref interrupt_pending);
			SyncByteFakeBool(ser, "branch_irq_hack", ref branch_irq_hack); //ser.Sync("branch_irq_hack", ref branch_irq_hack);
			ser.EndSection();
		}

		public string Disassemble(ushort pc, out int bytesToAdvance) { bytesToAdvance = 1; return "FOOBAR"; }

		public void SetCallbacks
			(Func<ushort, byte> ReadMemory,
			Func<ushort, byte> DummyReadMemory,
			Action<ushort, byte> WriteMemory, Action<GCHandle> DisposeBuilder)
		{
			var d1 = new MOS6502X_DLL.ReadMemoryD(ReadMemory);
			var h1 = GCHandle.Alloc(d1);
			var d2 = new MOS6502X_DLL.ReadMemoryD(DummyReadMemory);
			var h2 = GCHandle.Alloc(d2);
			var d3 = new MOS6502X_DLL.WriteMemoryD(WriteMemory);
			var h3 = GCHandle.Alloc(d3);

			MOS6502X_DLL.SetTrampolines(pthis, d1, d2, d3);
			DisposeBuilder(h1);
			DisposeBuilder(h2);
			DisposeBuilder(h3);
		}


	}

}
