using System.Runtime.InteropServices;
using System;

namespace BizHawk.Emulation.CPUs.Z80 
{
	public partial class Z80A 
    {
		[StructLayout(LayoutKind.Explicit)]
		[Serializable()]
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

		private bool RegFlagC 
        {
			get { return (RegAF.Low & 0x01) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x01) | (value ? 0x01 : 0x00)); }
		}

		private bool RegFlagN 
        {
			get { return (RegAF.Low & 0x02) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x02) | (value ? 0x02 : 0x00)); }
		}

		private bool RegFlagP 
        {
			get { return (RegAF.Low & 0x04) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x04) | (value ? 0x04 : 0x00)); }
		}

		private bool RegFlag3 
        {
			get { return (RegAF.Low & 0x08) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x08) | (value ? 0x08 : 0x00)); }
		}

		private bool RegFlagH 
        {
			get { return (RegAF.Low & 0x10) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x10) | (value ? 0x10 : 0x00)); }
		}

		private bool RegFlag5 
        {
			get { return (RegAF.Low & 0x20) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x20) | (value ? 0x20 : 0x00)); }
		}

		private bool RegFlagZ 
        {
			get { return (RegAF.Low & 0x40) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x40) | (value ? 0x40 : 0x00)); }
		}

		private bool RegFlagS 
        {
			get { return (RegAF.Low & 0x80) != 0; }
			set { RegAF.Low = (byte)((RegAF.Low & ~0x80) | (value ? 0x80 : 0x00)); }
		}

		private RegisterPair RegAF;
		private RegisterPair RegBC;
		private RegisterPair RegDE;
		private RegisterPair RegHL;

		private RegisterPair RegAltAF; // Shadow for A and F
		private RegisterPair RegAltBC; // Shadow for B and C
		private RegisterPair RegAltDE; // Shadow for D and E
		private RegisterPair RegAltHL; // Shadow for H and L
		
		private byte RegI; // I (interrupt vector)
		private byte RegR; // R (memory refresh)

		private RegisterPair RegIX; // IX (index register x)
		private RegisterPair RegIY; // IY (index register y)

		private RegisterPair RegSP; // SP (stack pointer)
		private RegisterPair RegPC; // PC (program counter)

		private void ResetRegisters() 
        {
			// Clear main registers
			RegAF = 0; RegBC = 0; RegDE = 0; RegHL = 0;
			// Clear alternate registers
			RegAltAF = 0; RegAltBC = 0; RegAltDE = 0; RegAltHL = 0;
			// Clear special purpose registers
			RegI = 0; RegR = 0;
			RegIX.Word = 0; RegIY.Word = 0;
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
			set { RegAF.Low = value; }
		}

		public ushort RegisterAF 
        {
			get { return RegAF.Word; }
			set { RegAF.Word = value; }
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
		public ushort RegisterIX 
        {
			get { return RegIX.Word; }
			set { RegIX.Word = value; }
		}
		public ushort RegisterIY 
        {
			get { return RegIY.Word; }
			set { RegIY.Word = value; }
		}
		public byte RegisterI 
        {
			get { return RegI; }
			set { RegI = value; }
		}
		public byte RegisterR 
        {
			get { return RegR; }
			set { RegR = value; }
		}
		public ushort RegisterShadowAF 
        {
			get { return RegAltAF.Word; }
			set { RegAltAF.Word = value; }
		}
		public ushort RegisterShadowBC 
        {
			get { return RegAltBC.Word; }
			set { RegAltBC.Word = value; }
		}
		public ushort RegisterShadowDE 
        {
			get { return RegAltDE.Word; }
			set { RegAltDE.Word = value; }
		}
		public ushort RegisterShadowHL 
        {
			get { return RegAltHL.Word; }
			set { RegAltHL.Word = value; }
		}
	}
}