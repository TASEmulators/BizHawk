using System.Text;

namespace BizHawk.Emulation.Cores.Components.x86
{
	public class DisassemblyInfo
	{
		public int Addr;
		public string Mnemonic;
		public string Args;
		public string RawBytes;
		public int Length;

		public override string ToString() => $"{Addr:X6}:  {RawBytes,-12}  {Mnemonic,-8} {Args}";
	}

	public partial class x86<TCpu> where TCpu : struct, x86CpuType
	{
		private ushort ReadWord(int addr)
		{
			return (ushort)(ReadMemory(addr++) + (ReadMemory(addr) << 8));
		}

		private string DisassembleRM8(ref int addr)
		{
			byte ModRM = ReadMemory(addr++);
			int mod = (ModRM >> 6) & 3;
			int r = (ModRM >> 3) & 7;
			int m = ModRM & 7;

			string reg;
			switch (r)
			{
				case 0: reg = "AL"; break;
				case 1: reg = "CL"; break;
				case 2: reg = "DL"; break;
				case 3: reg = "BL"; break;
				case 4: reg = "AH"; break;
				case 5: reg = "CH"; break;
				case 6: reg = "DH"; break;
				case 7: reg = "BH"; break;
				default: reg = "UNKNOWN"; break;
			}
			return reg + ", " + DisassembleMod(ref addr, mod, m, 1);
		}

		private string DisassembleMod(ref int addr, int mod, int m, int size)
		{
			string ret;
			switch (mod)
			{
				case 0:
					switch (m)
					{
						case 0: return "[BX+SI]";
						case 1: return "[BX+DI]";
						case 2: return "[BP+SI]";
						case 3: return "[BP+DI]";
						case 4: return "[SI]";
						case 5: return "[DI]";
						case 6: ret = $"{ReadWord(addr):X4}h"; addr += 2; return ret;
						case 7: return "[BX]";
					}
					break;
				case 1:
					switch (m)
					{
						case 0: return $"[BX+SI] + {ReadMemory(addr++):X2}h";
						case 1: return $"[BX+DI] + {ReadMemory(addr++):X2}h";
						case 2: return $"[BP+SI] + {ReadMemory(addr++):X2}h";
						case 3: return $"[BP+DI] + {ReadMemory(addr++):X2}h";
						case 4: return $"[SI] + {ReadMemory(addr++):X2}h";
						case 5: return $"[DI] + {ReadMemory(addr++):X2}h";
						case 6: return $"[BP] + {ReadMemory(addr++):X2}h";
						case 7: return $"[BX] + {ReadMemory(addr++):X2}h";
					}
					break;
				case 2:
					switch (m)
					{
						case 0: ret = $"[BX+SI] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 1: ret = $"[BX+DI] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 2: ret = $"[BP+SI] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 3: ret = $"[BP+DI] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 4: ret = $"[SI] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 5: ret = $"[DI] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 6: ret = $"[BP] + {ReadWord(addr):X4}h"; addr += 2; return ret;
						case 7: ret = $"[BX] + {ReadWord(addr):X4}h"; addr += 2; return ret;
					}
					break;
				case 3:
					switch (m)
					{
						case 0: return size == 1 ? "AL" : "AX";
						case 1: return size == 1 ? "CL" : "CX";
						case 2: return size == 1 ? "DL" : "DX";
						case 3: return size == 1 ? "BL" : "BX";
						case 4: return size == 1 ? "AH" : "SP";
						case 5: return size == 1 ? "CH" : "BP";
						case 6: return size == 1 ? "DH" : "SI";
						case 7: return size == 1 ? "BH" : "DI";
					}
					break;
			}
			return "Disassembly Error";
		}

		public DisassemblyInfo Disassemble(int addr)
		{
			var info = new DisassemblyInfo { Addr = addr };
			byte op1 = ReadMemory(addr++);
			switch (op1)
			{
				case 0x02: // ADD r8,r/m8
					info.Mnemonic = "ADD";
					info.Args = DisassembleRM8(ref addr);
					break;
				case 0xB0: // MOV AL, immed
					info.Mnemonic = "MOV";
					info.Args = $"AL, {ReadMemory(addr++):X2}h";
					break;
				case 0xB1: // MOV CL, immed
					info.Mnemonic = "MOV";
					info.Args = $"CL, {ReadMemory(addr++):X2}h";
					break;
				case 0xB2: // MOV DL, immed
					info.Mnemonic = "MOV";
					info.Args = $"DL, {ReadMemory(addr++):X2}h";
					break;
				case 0xB3: // MOV BL, immed
					info.Mnemonic = "MOV";
					info.Args = $"BL, {ReadMemory(addr++):X2}h";
					break;
				case 0xB4: // MOV AH, immed
					info.Mnemonic = "MOV";
					info.Args = $"AH, {ReadMemory(addr++):X2}h";
					break;
				case 0xB5: // MOV CH, immed
					info.Mnemonic = "MOV";
					info.Args = $"CH, {ReadMemory(addr++):X2}h";
					break;
				case 0xB6: // MOV DH, immed
					info.Mnemonic = "MOV";
					info.Args = $"DH, {ReadMemory(addr++):X2}h";
					break;
				case 0xB7: // MOV BH, immed
					info.Mnemonic = "MOV";
					info.Args = $"BH, {ReadMemory(addr++):X2}h";
					break;
				case 0xB8: // MOV AX, immed
					info.Mnemonic = "MOV";
					info.Args = $"AX, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xB9: // MOV CX, immed
					info.Mnemonic = "MOV";
					info.Args = $"CX, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xBA: // MOV DX, immed
					info.Mnemonic = "MOV";
					info.Args = $"DX, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xBB: // MOV BX, immed
					info.Mnemonic = "MOV";
					info.Args = $"BX, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xBC: // MOV SP, immed
					info.Mnemonic = "MOV";
					info.Args = $"SP, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xBD: // MOV BP, immed
					info.Mnemonic = "MOV";
					info.Args = $"BP, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xBE: // MOV SI, immed
					info.Mnemonic = "MOV";
					info.Args = $"SI, {ReadWord(addr):X4}h"; addr += 2;
					break;
				case 0xBF: // MOV DI, immed
					info.Mnemonic = "MOV";
					info.Args = $"DI, {ReadWord(addr):X4}h"; addr += 2;
					break;
				default:
					info.Mnemonic = "DB";
					info.Args = $"{op1:X2}h";
					break;
			}

			info.Length = addr - info.Addr;
			var sb = new StringBuilder();
			for (int p = info.Addr; p < info.Addr + info.Length; p++)
				sb.AppendFormat("{0:X2}", ReadMemory(p));
			info.RawBytes = sb.ToString();

			return info;
		}
	}
}
