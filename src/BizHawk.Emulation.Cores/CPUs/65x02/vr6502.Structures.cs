using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Components.vr6502
{
	public partial class vr6502
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct VrEmu6502State
		{
			public VrEmu6502Model Model;

			public IntPtr readFn;
			public IntPtr writeFn;

			public VrEmu6502Interrupt intPin;
			public VrEmu6502Interrupt nmiPin;

			public byte step;
			public byte currentOpcode;
			public ushort currentOpcodeAddr;

			public byte wai;
			public byte stp;

			public ushort pc;

			public byte ac;
			public byte ix;
			public byte iy;
			public byte sp;

			public byte flags;

			public ushort zpBase;
			public ushort spBase;
			public ushort tmpAddr;

			public IntPtr opcodes;
			public IntPtr mnemonicNames;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public VrEmu6502AddrMode[] addrModes;
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
