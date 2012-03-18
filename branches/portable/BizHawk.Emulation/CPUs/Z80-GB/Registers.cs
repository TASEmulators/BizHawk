using System.Runtime.InteropServices;
using System;

namespace BizHawk.Emulation.CPUs.Z80GB
{
	public partial class Z80 
    {
		[StructLayout(LayoutKind.Explicit)]
		[Serializable]
		public struct RegisterPair 
        {
			[FieldOffset(0)]
			public ushort Word;

			[FieldOffset(0)]
			public byte Low;

			[FieldOffset(1)]
			public byte High;

			public RegisterPair(ushort value) 
            {
				Word = value;
				Low = (byte)(Word);
				High = (byte)(Word >> 8);
			}

			public static implicit operator ushort(RegisterPair rp) 
            {
				return rp.Word;
			}

			public static implicit operator RegisterPair(ushort value) 
            {
				return new RegisterPair(value);
			}
		}

		public bool FlagC 
        {
			get { return (RegAF.Low & 0x10) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x10) | (value ? 0x10 : 0x00)); }
		}

		public bool FlagH 
        {
			get { return (RegAF.Low & 0x20) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x20) | (value ? 0x20 : 0x00)); }
		}

		public bool FlagN 
        {
			get { return (RegAF.Low & 0x40) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x40) | (value ? 0x40 : 0x00)); }
		}

		public bool FlagZ 
        {
			get { return (RegAF.Low & 0x80) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x80) | (value ? 0x80 : 0x00)); }
		}

		private RegisterPair RegAF;
		private RegisterPair RegBC;
		private RegisterPair RegDE;
		private RegisterPair RegHL;
		
		private byte RegI; // I (interrupt vector)

		private RegisterPair RegSP; // SP (stack pointer)
		private RegisterPair RegPC; // PC (program counter)

		private void ResetRegisters() 
        {
			RegAF = 0; RegBC = 0; RegDE = 0; RegHL = 0;
			RegI = 0;
			RegSP.Word = 0; RegPC.Word = 0;
		}

		public byte RegisterA 
        {
			get { return RegAF.High; }
			set { RegAF.High = value; }
		}

		public byte RegisterF 
        {
			get { return RegAF.Low; }
			set { RegAF.Low = (byte)(value&0xF0); }
		}

		public ushort RegisterAF 
        {
			get { return RegAF.Word; }
			set { RegAF.Word = (byte)(value&0xFFF0); }
		}

		public byte RegisterB 
        {
			get { return RegBC.High; }
			set { RegBC.High = value; }
		}

		public byte RegisterC 
        {
			get { return RegBC.Low; }
			set { RegBC.Low = value; }
		}

		public ushort RegisterBC 
        {
			get { return RegBC.Word; }
			set { RegBC.Word = value; }
		}

		public byte RegisterD 
        {
			get { return RegDE.High; }
			set { RegDE.High = value; }
		}

		public byte RegisterE 
        {
			get { return RegDE.Low; }
			set { RegDE.Low = value; }
		}
		public ushort RegisterDE 
        {
			get { return RegDE.Word; }
			set { RegDE.Word = value; }
		}

		public byte RegisterH 
        {
			get { return RegHL.High; }
			set { RegHL.High = value; }
		}

		public byte RegisterL 
        {
			get { return RegHL.Low; }
			set { RegHL.Low = value; }
		}
		public ushort RegisterHL 
        {
			get { return RegHL.Word; }
			set { RegHL.Word = value; }
		}

		public ushort RegisterPC 
        {
			get { return RegPC.Word; }
			set { RegPC.Word = value; }
		}
		public ushort RegisterSP 
        {
			get { return RegSP.Word; }
			set { RegSP.Word = value; }
		}
		public byte RegisterI 
        {
			get { return RegI; }
			set { RegI = value; }
		}
	}
}