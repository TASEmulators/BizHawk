using System.Text;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Components.ARM
{
	public abstract class Darm
	{
		public const CallingConvention cc = CallingConvention.Cdecl;

		[StructLayout(LayoutKind.Sequential)]
		public class Darm_T
		{
			public uint w;
			public uint instr;
			public uint instr_type;
			public uint instr_imm_type;
			public uint instr_flag_type;
			public uint cond;
			public uint B;
			public uint S;
			public uint E;
			public uint M;
			public uint N;
			public uint option;
			public uint U;
			public uint H;
			public uint P;
			public uint R;
			public uint T;
			public uint W;
			public uint I;
			public uint rotate;
			public uint Rd;
			public uint Rn;
			public uint Rm;
			public uint Ra;
			public uint Rt;
			public uint Rt2;
			public uint RdHi;
			public uint RdLo;
			public uint imm;
			public uint sat_imm;
			public uint shift_type;
			public uint Rs;
			public uint shift;
			public uint lsb;
			public uint msb;
			public uint width;
			public ushort reglist;
			public byte coproc;
			public byte opc1;
			public byte opc2;
			public uint CRd;
			public uint CRn;
			public uint Crm;
			public uint D;
			public uint firstcond;
			public byte mask;
			// just in case we got something wrong, padding
			public uint Pad1;
			public uint Pad2;
			public uint Pad3;
			public uint Pad4;
			public uint Pad5;
			public uint Pad6;
			public uint Pad7;
			public uint Pad8;
			public uint Pad9;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class Darm_Str_T
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public byte[] mnemonic = new byte[12];
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6 * 32)]
			public byte[] arg = new byte[6 * 32];
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public byte[] shift = new byte[12];
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
			public byte[] total = new byte[64];
		}

		[BizImport(cc, Compatibility = true, EntryPoint = "darm_disasm")]
		public abstract bool Disassemble([Out] Darm_T d, ushort w, ushort w2, uint addr);

		[BizImport(cc, Compatibility = true, EntryPoint = "darm_str2")]
		public abstract bool Str([In] [Out]Darm_T d, [Out]Darm_Str_T s, bool lowercase);

		public string DisassembleStuff(uint addr, uint opcode)
		{
			var d = new Darm_T();
			var s = new Darm_Str_T();
			if (!Disassemble(d, (ushort)opcode, (ushort)(opcode >> 16), addr))
				return null;
			if (Str(d, s, false))
				return null;
			string[] ret = Encoding.ASCII.GetString(s.total, 0, Array.IndexOf(s.total, (byte)0))
				.Split(new string[] {" "}, 2, StringSplitOptions.None);
			return ret[0].PadRight(8) + (ret.Length > 1 ? ret[1] : "");
		}
	}

}

