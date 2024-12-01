using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M68000;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public class JaguarDisassembler : VerifiedDisassembler
	{
		private readonly MC68000 _m68kDisassembler = new();

		public string DisassembleM68K(MemoryDomain m, uint addr, out int length)
		{
			_m68kDisassembler.ReadByte = a => (sbyte)m.PeekByte(a);
			_m68kDisassembler.ReadWord = a => (short)m.PeekUshort(a, true);
			_m68kDisassembler.ReadLong = a => (int)m.PeekUint(a, true);
			var info = _m68kDisassembler.Disassemble((int)(addr & 0xFFFFFF));
			length = info.Length;
			return $"{info.RawBytes.Substring(0, 4)}  {info.Mnemonic,-7} {info.Args}";
		}

		// TOM and JERRY RISC processors are mostly similar, only 6 instructions differ
		// most of this is taken from virtualjaguar's dasmjag function
		public string DisassembleRISC(bool gpu, MemoryDomain m, uint addr, out int length)
		{
			var opcode = m.PeekUshort(addr & 0xFFFFFF, true);
			var arg1 = (opcode >> 5) & 0x1F;
			var arg2 = opcode & 0x1F;
			length = (opcode >> 10) == 0x26 ? 6 : 2;

			string argRR() => $"r{arg1}, r{arg2}";
			string argCZIR() => $"${(arg1 == 0 ? 32 : arg1):X02}, r{arg2}";
			string argIR() => $"${arg1:X02}, r{arg2}";
			string argR2() => $"r{arg2}";
			string argSR()
			{
				var s1 = (short)(arg1 << 11) >> 11;
				if (s1 < 0)
				{
					return $"-${-s1:X02}, r{arg2}";
				}
				else
				{
					return $"${s1:X02}, r{arg2}";
				}
			}
			string argDRR() => $"(r{arg1}), r{arg2}";
			string argRDR() => $"r{arg2}, (r{arg1})";
			string argDROR(int r) => $"(r{r} + ${(arg1 == 0 ? 128 : arg1 * 4):X02}), r{arg2}";
			string argRDRO(int r) => $"r{arg2}, (r{r} + ${(arg1 == 0 ? 128 : arg1 * 4):X02})";
			string argDRORR(int r) => $"(r{r} + r{arg1}), r{arg2}";
			string argRDROR(int r) => $"r{arg1}, (r{r} + r{arg2})";
			string argCC()
			{
				return arg2 switch
				{
					0x00 => "",
					0x01 => "nz, ",
					0x02 => "z, ",
					0x04 => "nc, ",
					0x05 => "nc nz, ",
					0x06 => "nc z, ",
					0x08 => "c, ",
					0x09 => "c nz, ",
					0x0A => "c z, ",
					0x14 => "nn, ",
					0x15 => "nn nz, ",
					0x16 => "nn z, ",
					0x18 => "n, ",
					0x19 => "n nz, ",
					0x1A => "n z, ",
					0x1F => "never, ",
					_ => "???, ",
				};
			}

			var disasm = (opcode >> 10) switch
			{
				0x00 => $"add {argRR()}",
				0x01 => $"addc {argRR()}",
				0x02 => $"addq {argCZIR()}",
				0x03 => $"addqt {argCZIR()}",
				0x04 => $"sub {argRR()}",
				0x05 => $"subc {argRR()}",
				0x06 => $"subq {argCZIR()}",
				0x07 => $"subqt {argCZIR()}",
				0x08 => $"neg {argR2()}",
				0x09 => $"and {argRR()}",
				0x0A => $"or {argRR()}",
				0x0B => $"xor {argRR()}",
				0x0C => $"not {argR2()}",
				0x0D => $"btet {argIR()}",
				0x0E => $"bset {argIR()}",
				0x0F => $"bclr {argIR()}",
				0x10 => $"mult {argRR()}",
				0x11 => $"imult {argRR()}",
				0x12 => $"imultn {argRR()}",
				0x13 => $"resmac {argR2()}",
				0x14 => $"imacn {argRR()}",
				0x15 => $"div {argRR()}",
				0x16 => $"abs {argR2()}",
				0x17 => $"sh {argRR()}",
				0x18 => $"shlq ${32 - arg1:X02}, {argR2()}",
				0x19 => $"shrq {argCZIR()}",
				0x1A => $"sha {argRR()}",
				0x1B => $"sharq {argCZIR()}",
				0x1C => $"ror {argRR()}",
				0x1D => $"rorq {argCZIR()}",
				0x1E => $"cmp {argRR()}",
				0x1F => $"cmpq {argSR()}",
				0x20 => gpu ? $"sat8 {argR2()}" : $"subqmod {argCZIR()}",
				0x21 => $"{(gpu ? "sat16" : "sub16s")} {argR2()}",
				0x22 => $"move {argRR()}",
				0x23 => $"moveq {argIR()}",
				0x24 => $"moveta {argRR()}",
				0x25 => $"movefa {argRR()}",
				0x26 => $"movei ${m.PeekUshort((addr + 2) & 0xFFFFFF, true) | (m.PeekUshort((addr + 4) & 0xFFFFFF, true) << 16):X06}, {argR2()}",
				0x27 => $"loadb {argDRR()}",
				0x28 => $"loadw {argDRR()}",
				0x29 => $"load {argDRR()}",
				0x2A => gpu ? $"loadp {argDRR()}" : $"sat32s {argR2()}",
				0x2B => $"load {argDROR(14)}",
				0x2C => $"load {argDROR(15)}",
				0x2D => $"storeb {argRDR()}",
				0x2E => $"storew {argRDR()}",
				0x2F => $"store {argRDR()}",
				0x30 => gpu ? $"storep {argRDR()}" : $"mirror {argR2()}",
				0x31 => $"store {argRDRO(14)}",
				0x32 => $"store {argRDRO(15)}",
				0x33 => $"move pc, {argR2()}",
				0x34 => $"jump {argCC()}(r{arg1})",
				0x35 => $"jr {argCC()}${addr + 2 + ((sbyte)(arg1 << 3) >> 2):X06}",
				0x36 => $"mmult {argRR()}",
				0x37 => $"mtoi {argRR()}",
				0x38 => $"normi {argRR()}",
				0x39 => $"nop",
				0x3A => $"load {argDRORR(14)}",
				0x3B => $"load {argDRORR(15)}",
				0x3C => $"store {argRDROR(14)}",
				0x3D => $"store {argRDROR(15)}",
				0x3E => gpu ? $"sat24 {argR2()}" : $"illegal [{arg1}, {arg2}]",
				0x3F => gpu ? $"{(arg1 == 0 ? "pack" : "unpack")} {argR2()}" : $"addqmod {argCZIR()}",
				_ => throw new InvalidOperationException(),
			};

			if (length == 6)
			{
				return $"{opcode:X04} {m.PeekUshort((addr + 2) & 0xFFFFFF, true):X04} {m.PeekUshort((addr + 4) & 0xFFFFFF, true):X04}  {disasm}";
			}
			else
			{
				return $"{opcode:X04}            {disasm}";
			}
		}

		public override string PCRegisterName => Cpu switch
		{
			"M68000" => "M68K PC",
			"TOM" => "GPU PC",
			"JERRY" => "DSP PC",
			_ => throw new InvalidOperationException(),
		};

		public override IEnumerable<string> AvailableCpus => new[]
		{
			"M68000",
			"TOM",
			"JERRY",
		};

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			return Cpu switch
			{
				"M68000" => DisassembleM68K(m, addr, out length),
				"TOM" => DisassembleRISC(true, m, addr, out length),
				"JERRY" => DisassembleRISC(false, m, addr, out length),
				_ => throw new InvalidOperationException(),
			};
		}
	}
}
