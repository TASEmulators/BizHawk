using System;

namespace BizHawk.Emulation.CPUs.x86
{
    public partial class x86<CpuType> where CpuType: struct, x86CpuType
    {
        public void Execute(int cycles)
        {
            Console.WriteLine(Disassemble((CS << 4) + IP));
            byte opcode1 = ReadMemory((CS << 4) + IP);
            IP++;
            
            switch (opcode1)
            {
                case 0xB0: // MOV AL, imm
                    AL = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB1: // MOV CL, immed
                    CL = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB2: // MOV DL, immed
                    DL = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB3: // MOV BL, immed
                    BL = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB4: // MOV AH, immed
                    AH = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB5: // MOV CH, immed
                    CH = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB6: // MOV DH, immed
                    DH = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB7: // MOV BH, immed
                    BH = ReadMemory((CS << 4) + IP++);
                    PendingCycles -= timing_mov_ri8;
                    break;
                case 0xB8: // MOV AX, immed
                    AX = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xB9: // MOV CX, imm
                    CX = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xBA: // MOV DX, immed
                    DX = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xBB: // MOV BX, immed
                    BX = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xBC: // MOV SP, immed
                    SP = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xBD: // MOV BP, immed
                    BP = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xBE: // MOV SI, immed
                    SI = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                case 0xBF: // MOV DI, immed
                    DI = (ushort)(ReadMemory((CS << 4) + IP++) + (ReadMemory((CS << 4) + IP++) << 8));
                    PendingCycles -= timing_mov_ri16;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
