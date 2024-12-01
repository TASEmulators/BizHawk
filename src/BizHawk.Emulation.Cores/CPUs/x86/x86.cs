using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Components.x86
{
	public interface x86CpuType { }
	public struct Intel8086 : x86CpuType { }

	public sealed partial class x86<TCpu> where TCpu : struct, x86CpuType
	{
		// Machine State
		public Register16 RegAX;
		public Register16 RegBX;
		public Register16 RegCX;
		public Register16 RegDX;

		public ushort CS;
		public ushort DS;
		public ushort ES;
		public ushort SS;

		public ushort SI;
		public ushort DI;
		public ushort IP;
		public ushort SP;
		public ushort BP;

		public bool CF;
		public bool PF;
		public bool AF;
		public bool ZF;
		public bool SF;
		public bool TP;
		public bool IF;
		public bool DF;
		public bool OF;

		public ushort Flags
		{
			get
			{
				ushort value = 2;
				if (CF) value |= 1;
				if (PF) value |= 4;
				if (AF) value |= 16;
				if (ZF) value |= 64;
				if (SF) value |= 128;
				if (TP) value |= 256;
				if (IF) value |= 512;
				if (DF) value |= 1024;
				if (OF) value |= 2048;
				return value;
			}
		}

		public int PendingCycles;
		public int TotalExecutedCycles;

		// Memory Access
		public Func<int, byte> ReadMemory;
		public Action<int, byte> WriteMemory;

		public x86()
		{
			InitTiming();
		}

		// We expect these properties to get inlined by the CLR -- at some point we should test this assumption
		public ushort AX
		{
			get => RegAX.Word;
			set => RegAX.Word = value;
		}
		public ushort BX
		{
			get => RegBX.Word;
			set => RegBX.Word = value;
		}
		public ushort CX
		{
			get => RegCX.Word;
			set => RegCX.Word = value;
		}
		public ushort DX
		{
			get => RegDX.Word;
			set => RegDX.Word = value;
		}
		public byte AL
		{
			get => RegAX.Low;
			set => RegAX.Low = value;
		}
		public byte BL
		{
			get => RegBX.Low;
			set => RegBX.Low = value;
		}
		public byte CL
		{
			get => RegCX.Low;
			set => RegCX.Low = value;
		}
		public byte DL
		{
			get => RegDX.Low;
			set => RegDX.Low = value;
		}
		public byte AH
		{
			get => RegAX.High;
			set => RegAX.High = value;
		}
		public byte BH
		{
			get => RegBX.High;
			set => RegBX.High = value;
		}
		public byte CH
		{
			get => RegCX.High;
			set => RegCX.High = value;
		}
		public byte DH
		{
			get => RegDX.High;
			set => RegDX.High = value;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Register16
	{
		[FieldOffset(0)]
		public ushort Word;

		[FieldOffset(0)]
		public byte Low;

		[FieldOffset(1)]
		public byte High;

		public override string ToString() => $"{Word:X4}";
	}
}