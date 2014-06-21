using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HuC6280
{
	public partial class CoreGenerator
	{
		public void GenerateCDL(string file)
		{
			using (TextWriter w = new StreamWriter(file))
			{
				w.WriteLine("using System;");
				w.WriteLine();
				w.WriteLine("// Do not modify this file directly! This is GENERATED code.");
				w.WriteLine("// Please open the CpuCoreGenerator solution and make your modifications there.");

				w.WriteLine();
				w.WriteLine("namespace BizHawk.Emulation.Cores.Components.H6280");
				w.WriteLine("{");
				w.WriteLine("\tpublic partial class HuC6280");
				w.WriteLine("\t{");

				w.WriteLine("\t\tvoid CDLOpcode()");
				w.WriteLine("\t\t{");
				w.WriteLine("\t\t\tbyte tmp8;");
				w.WriteLine("\t\t\tbyte opcode = ReadMemory(PC);");
				w.WriteLine("\t\t\tswitch (opcode)");
				w.WriteLine("\t\t\t{");
				for (int i = 0; i < 256; i++)
					CDLOpcode(w, i);
				w.WriteLine("\t\t\t}");
				w.WriteLine("\t\t}");

				w.WriteLine("\t}");
				w.WriteLine("}");
			}
		}

		void CDLOpcode(TextWriter w, int opcode)
		{
			// todo: T Flag

			var op = Opcodes[opcode];

			if (op == null)
			{
				// nop
				w.WriteLine("\t\t\t\tcase 0x{0:X2}: // {1}", opcode, "??");
				w.WriteLine("\t\t\t\t\tMarkCode(PC, 1);");
				w.WriteLine("\t\t\t\t\tbreak;");
				return;
			}

            w.WriteLine("\t\t\t\tcase 0x{0:X2}: // {1}", opcode, op);

			w.WriteLine("\t\t\t\t\tMarkCode(PC, {0});", op.Size);

			switch (op.AddressMode)
			{
				case AddrMode.Implicit:
					switch (op.Instruction)
					{
						case "PHA": // push
						case "PHP":
						case "PHX":
						case "PHY":
							w.WriteLine("\t\t\t\t\tMarkPush(1);");
							break;
						case "PLA": // pop
						case "PLP":
						case "PLX":
						case "PLY":
							w.WriteLine("\t\t\t\t\tMarkPop(1);");
							break;
						case "RTI":
							w.WriteLine("\t\t\t\t\tMarkPop(3);");
							break;
						case "RTS":
							w.WriteLine("\t\t\t\t\tMarkPop(2);");
							break;
					}
					break;
				case AddrMode.Accumulator:
					break;
				case AddrMode.Immediate:
					break;
				case AddrMode.ZeroPage:
					w.WriteLine("\t\t\t\t\tMarkZP(ReadMemory((ushort)(PC + 1)));");
					break;
				case AddrMode.ZeroPageX:
					w.WriteLine("\t\t\t\t\tMarkZP(ReadMemory((ushort)(PC + 1)) + X);");
					break;
				case AddrMode.ZeroPageY:
					w.WriteLine("\t\t\t\t\tMarkZP(ReadMemory((ushort)(PC + 1)) + Y);");
					break;
				case AddrMode.ZeroPageR:
					w.WriteLine("\t\t\t\t\tMarkZP(ReadMemory((ushort)(PC + 1)));");
					break;
				case AddrMode.Absolute:
					w.WriteLine("\t\t\t\t\tMarkAddr(ReadWord((ushort)(PC + 1)));");
					break;
				case AddrMode.AbsoluteX:
					w.WriteLine("\t\t\t\t\tMarkAddr(ReadWord((ushort)(PC + 1)) + X);");
					break;
				case AddrMode.AbsoluteY:
					w.WriteLine("\t\t\t\t\tMarkAddr(ReadWord((ushort)(PC + 1)) + Y);");
					break;
				case AddrMode.Indirect:
					w.WriteLine("\t\t\t\t\ttmp8 = ReadMemory((ushort)(PC + 1));");
					w.WriteLine("\t\t\t\t\tMarkZPPtr(tmp8);");
					w.WriteLine("\t\t\t\t\tMarkIndirect(GetIndirect(tmp8));");
					break;
				case AddrMode.IndirectX:
					w.WriteLine("\t\t\t\t\ttmp8 = (byte)(ReadMemory((ushort)(PC + 1)) + X);");
					w.WriteLine("\t\t\t\t\tMarkZPPtr(tmp8);");
					w.WriteLine("\t\t\t\t\tMarkIndirect(GetIndirect(tmp8));");
					break;
				case AddrMode.IndirectY:
					w.WriteLine("\t\t\t\t\ttmp8 = ReadMemory((ushort)(PC + 1));");
					w.WriteLine("\t\t\t\t\tMarkZPPtr(tmp8);");
					w.WriteLine("\t\t\t\t\tMarkIndirect(GetIndirect(tmp8) + Y);");
					break;
				case AddrMode.Relative:
					break;
				case AddrMode.BlockMove:
					w.WriteLine("\t\t\t\t\tif (!InBlockTransfer)");
					w.WriteLine("\t\t\t\t\t{");
					w.WriteLine("\t\t\t\t\t\tMarkBTFrom(ReadWord((ushort)(PC + 1)));");
					w.WriteLine("\t\t\t\t\t\tMarkBTTo(ReadWord((ushort)(PC + 3)));");
					w.WriteLine("\t\t\t\t\t}");
					w.WriteLine("\t\t\t\t\telse");
					w.WriteLine("\t\t\t\t\t{");
					switch (op.Instruction)
					{
						case "TII":
						case "TDD":
						case "TIN":
							w.WriteLine("\t\t\t\t\t\tMarkBTFrom(btFrom);");
							w.WriteLine("\t\t\t\t\t\tMarkBTTo(btTo);");
							break;
						case "TIA":
							w.WriteLine("\t\t\t\t\t\tMarkBTFrom(btFrom);");
							w.WriteLine("\t\t\t\t\t\tMarkBTTo(btTo+btAlternator);");
							break;
						case "TAI":
							w.WriteLine("\t\t\t\t\t\tMarkBTFrom(btFrom+btAlternator);");
							w.WriteLine("\t\t\t\t\t\tMarkBTTo(btTo);");
							break;
					}
					w.WriteLine("\t\t\t\t\t\t}");
					break;
				case AddrMode.ImmZeroPage:
					w.WriteLine("\t\t\t\t\tMarkZP(ReadMemory((ushort)(PC + 2)));");
					break;
				case AddrMode.ImmZeroPageX:
					w.WriteLine("\t\t\t\t\tMarkZP(ReadMemory((ushort)(PC + 2)) + X);");
					break;
				case AddrMode.ImmAbsolute:
					w.WriteLine("\t\t\t\t\tMarkAddr(ReadWord((ushort)(PC + 2)));");
					break;
				case AddrMode.ImmAbsoluteX:
					w.WriteLine("\t\t\t\t\tMarkAddr(ReadWord((ushort)(PC + 2)) + X);");
					break;
				case AddrMode.AbsoluteIndirect:
					w.WriteLine("\t\t\t\t\tMarkFptr(ReadWord((ushort)(PC + 1)));");
					break;
				case AddrMode.AbsoluteIndirectX:
					w.WriteLine("\t\t\t\t\tMarkFptr(ReadWord((ushort)(PC + 1)) + X);");
					break;
			}
			w.WriteLine("\t\t\t\t\tbreak;");
		}
	}
}
