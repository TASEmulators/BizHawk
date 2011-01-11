using System.Text;

namespace BizHawk.Emulation.CPUs.x86
{
    public class DisassemblyInfo
    {
        public int Addr;
        public string Mnemonic;
        public string Args;
        public string RawBytes;
        public int Length;

        public override string ToString()
        {
            return string.Format("{0:X6}  {3,-12}  {1,-8} {2}", Addr, Mnemonic, Args, RawBytes);
        }
    }

    public partial class x86<CpuType> where CpuType : struct, x86CpuType
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
            return reg+", "+DisassembleMod(ref addr, mod, m, 1);
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
                        case 6: ret = string.Format("{0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 7: return "[BX]";
                    }
                    break;
                case 1:
                    switch (m)
                    {
                        case 0: return string.Format("[BX+SI] + {0:X2}h", ReadMemory(addr++));
                        case 1: return string.Format("[BX+DI] + {0:X2}h", ReadMemory(addr++));
                        case 2: return string.Format("[BP+SI] + {0:X2}h", ReadMemory(addr++));
                        case 3: return string.Format("[BP+DI] + {0:X2}h", ReadMemory(addr++));
                        case 4: return string.Format("[SI] + {0:X2}h", ReadMemory(addr++));
                        case 5: return string.Format("[DI] + {0:X2}h", ReadMemory(addr++));
                        case 6: return string.Format("[BP] + {0:X2}h", ReadMemory(addr++));
                        case 7: return string.Format("[BX] + {0:X2}h", ReadMemory(addr++));
                    }
                    break;
                case 2:
                    switch (m)
                    {
                        case 0: ret = string.Format("[BX+SI] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 1: ret = string.Format("[BX+DI] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 2: ret = string.Format("[BP+SI] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 3: ret = string.Format("[BP+DI] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 4: ret = string.Format("[SI] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 5: ret = string.Format("[DI] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 6: ret = string.Format("[BP] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
                        case 7: ret = string.Format("[BX] + {0:X4}h", ReadWord(addr)); addr += 2; return ret;
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
                    info.Args = string.Format("AL, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB1: // MOV CL, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("CL, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB2: // MOV DL, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("DL, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB3: // MOV BL, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("BL, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB4: // MOV AH, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("AH, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB5: // MOV CH, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("CH, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB6: // MOV DH, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("DH, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB7: // MOV BH, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("BH, {0:X2}h", ReadMemory(addr++));
                    break;
                case 0xB8: // MOV AX, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("AX, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xB9: // MOV CX, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("CX, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xBA: // MOV DX, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("DX, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xBB: // MOV BX, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("BX, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xBC: // MOV SP, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("SP, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xBD: // MOV BP, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("BP, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xBE: // MOV SI, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("SI, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                case 0xBF: // MOV DI, immed
                    info.Mnemonic = "MOV";
                    info.Args = string.Format("DI, {0:X4}h", ReadWord(addr)); addr += 2;
                    break;
                default:
                    info.Mnemonic = "DB";
                    info.Args = string.Format("{0:X2}h", op1);
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
