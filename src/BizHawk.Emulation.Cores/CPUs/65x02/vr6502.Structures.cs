using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Components.vr6502
{
	public partial class vr6502
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct VrEmu6502State
		{
			public VrEmu6502Model Model;

			public IntPtr ReadFn;
			public IntPtr WriteFn;

			public VrEmu6502Interrupt IntPin;
			public VrEmu6502Interrupt NmiPin;

			public byte Step;
			public byte CurrentOpcode;
			public ushort CurrentOpcodeAddr;

			public bool Wai;
			public bool Stp;

			public ushort Pc;

			public byte Ac;
			public byte Ix;
			public byte Iy;
			public byte Sp;

			public byte Flags;

			public ushort ZpBase;
			public ushort SpBase;
			public ushort TmpAddr;

			public IntPtr Opcodes; // Use IntPtr for pointer to array

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public string[] MnemonicNames;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public VrEmu6502AddrMode[] AddrModes;
		}

		public enum VrEmu6502Model
		{
			CPU_6502,
			CPU_6502U,
			CPU_65C02,
			CPU_W65C02,
			CPU_R65C02,
			CPU_6510 = CPU_6502U,
			CPU_8500 = CPU_6510,
			CPU_8502 = CPU_8500,
			CPU_7501 = CPU_6502,
			CPU_8501 = CPU_6502
		}

		public enum VrEmu6502Interrupt
		{
			IntCleared,
			IntRequested,
			IntLow = IntRequested,
			IntHigh = IntCleared
		}

		public enum VrEmu6502AddrMode
		{
			AddrModeAbs,
			AddrModeAbsX,
			AddrModeAbsY,
			AddrModeImm,
			AddrModeAbsInd,
			AddrModeAbsIndX,
			AddrModeIndX,
			AddrModeIndY,
			AddrModeRel,
			AddrModeZP,
			AddrModeZPI,
			AddrModeZPX,
			AddrModeZPY,
			AddrModeAcc,
			AddrModeImp
		}
	}
}
