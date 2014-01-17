//#define ARM_DEBUG

namespace GarboDev
{
    using System;

    public partial class FastArmCore
    {
        private const uint COND_EQ = 0;	    // Z set
        private const uint COND_NE = 1;	    // Z clear
        private const uint COND_CS = 2;	    // C set
        private const uint COND_CC = 3;	    // C clear
        private const uint COND_MI = 4;	    // N set
        private const uint COND_PL = 5;	    // N clear
        private const uint COND_VS = 6;	    // V set
        private const uint COND_VC = 7;	    // V clear
        private const uint COND_HI = 8;	    // C set and Z clear
        private const uint COND_LS = 9;	    // C clear or Z set
        private const uint COND_GE = 10;	// N equals V
        private const uint COND_LT = 11;	// N not equal to V
        private const uint COND_GT = 12;	// Z clear AND (N equals V)
        private const uint COND_LE = 13;	// Z set OR (N not equal to V)
        private const uint COND_AL = 14;	// Always
        private const uint COND_NV = 15;	// Never execute

        private const uint OP_AND = 0x0;
        private const uint OP_EOR = 0x1;
        private const uint OP_SUB = 0x2;
        private const uint OP_RSB = 0x3;
        private const uint OP_ADD = 0x4;
        private const uint OP_ADC = 0x5;
        private const uint OP_SBC = 0x6;
        private const uint OP_RSC = 0x7;
        private const uint OP_TST = 0x8;
        private const uint OP_TEQ = 0x9;
        private const uint OP_CMP = 0xA;
        private const uint OP_CMN = 0xB;
        private const uint OP_ORR = 0xC;
        private const uint OP_MOV = 0xD;
        private const uint OP_BIC = 0xE;
        private const uint OP_MVN = 0xF;

        private delegate void ExecuteInstructionDelegate();
        private ExecuteInstructionDelegate[] NormalOps = null;

        private Arm7Processor parent;
        private Memory memory;
        private uint[] registers;

        private uint instructionQueue;
        private uint curInstruction;

        // CPU flags
        private uint zero, carry, negative, overflow;
        private uint shifterCarry;

        private bool thumbMode;

        public FastArmCore(Arm7Processor parent, Memory memory)
        {
            this.parent = parent;
            this.memory = memory;
            this.registers = this.parent.Registers;

            this.NormalOps = new ExecuteInstructionDelegate[8]
                {
                    this.UndefinedInstruction,
                    this.UndefinedInstruction,
                    this.UndefinedInstruction,
                    this.UndefinedInstruction,
                    this.LoadStoreMultiple,
                    this.UndefinedInstruction,
                    this.CoprocessorLoadStore,
                    this.UndefinedInstruction
                };

            this.InitializeDispatchFunc();
        }

        public void BeginExecution()
        {
            this.FlushQueue();
        }

        public void Step()
        {
            this.UnpackFlags();

            this.curInstruction = this.instructionQueue;
            this.instructionQueue = this.memory.ReadU32(registers[15]);
            registers[15] += 4;

            uint cond = 0;
            switch (this.curInstruction >> 28)
            {
                case COND_AL: cond = 1; break;
                case COND_EQ: cond = zero; break;
                case COND_NE: cond = 1 - zero; break;
                case COND_CS: cond = carry; break;
                case COND_CC: cond = 1 - carry; break;
                case COND_MI: cond = negative; break;
                case COND_PL: cond = 1 - negative; break;
                case COND_VS: cond = overflow; break;
                case COND_VC: cond = 1 - overflow; break;
                case COND_HI: cond = carry & (1 - zero); break;
                case COND_LS: cond = (1 - carry) | zero; break;
                case COND_GE: cond = (1 - negative) ^ overflow; break;
                case COND_LT: cond = negative ^ overflow; break;
                case COND_GT: cond = (1 - zero) & (negative ^ (1 - overflow)); break;
                case COND_LE: cond = (negative ^ overflow) | zero; break;
            }

            if (cond == 1)
            {
                // Execute the instruction
                this.ExecuteInstruction((ushort)(((this.curInstruction >> 16) & 0xFF0) | ((this.curInstruction >> 4) & 0xF)));
            }

            this.parent.Cycles -= this.memory.WaitCycles;

            if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
            {
                this.parent.ReloadQueue();
            }

            this.PackFlags();
        }

        public void Execute()
        {
            this.UnpackFlags();
            this.thumbMode = false;

            while (this.parent.Cycles > 0)
            {
                this.curInstruction = this.instructionQueue;
                this.instructionQueue = this.memory.ReadU32Aligned(registers[15]);
                registers[15] += 4;

                if ((this.curInstruction >> 28) == COND_AL)
                {
                    this.fastDispatch[(ushort)(((this.curInstruction >> 16) & 0xFF0) | ((this.curInstruction >> 4) & 0xF))]();
//                    this.ExecuteInstruction((ushort)(((this.curInstruction >> 16) & 0xFF0) | ((this.curInstruction >> 4) & 0xF)));
                }
                else
                {
                    uint cond = 0;
                    switch (this.curInstruction >> 28)
                    {
                        case COND_EQ: cond = zero; break;
                        case COND_NE: cond = 1 - zero; break;
                        case COND_CS: cond = carry; break;
                        case COND_CC: cond = 1 - carry; break;
                        case COND_MI: cond = negative; break;
                        case COND_PL: cond = 1 - negative; break;
                        case COND_VS: cond = overflow; break;
                        case COND_VC: cond = 1 - overflow; break;
                        case COND_HI: cond = carry & (1 - zero); break;
                        case COND_LS: cond = (1 - carry) | zero; break;
                        case COND_GE: cond = (1 - negative) ^ overflow; break;
                        case COND_LT: cond = negative ^ overflow; break;
                        case COND_GT: cond = (1 - zero) & (negative ^ (1 - overflow)); break;
                        case COND_LE: cond = (negative ^ overflow) | zero; break;
                    }

                    if (cond == 1)
                    {
                        // Execute the instruction
                        this.fastDispatch[(ushort)(((this.curInstruction >> 16) & 0xFF0) | ((this.curInstruction >> 4) & 0xF))]();
//                        this.ExecuteInstruction((ushort)(((this.curInstruction >> 16) & 0xFF0) | ((this.curInstruction >> 4) & 0xF)));
                    }
                }

                this.parent.Cycles -= this.memory.WaitCycles;

                if (this.thumbMode)
                {
                    this.parent.ReloadQueue();
                    break;
                }

#if ARM_DEBUG
                // Check the current PC
                if (this.parent.Breakpoints.ContainsKey(registers[15] - 4U))
                {
                    this.parent.BreakpointHit = true;
                    break;
                }
#endif
            }

            this.PackFlags();
        }

        private void DataProcessingWriteToR15()
        {
            // Prevent writing if no SPSR exists (this will be true for USER or SYSTEM mode)
            if (this.parent.SPSRExists) this.parent.WriteCpsr(this.parent.SPSR);
            this.UnpackFlags();

            // Check for branch back to Thumb Mode
            if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
            {
                this.thumbMode = true;
                return;
            }

            // Otherwise, flush the instruction queue
            this.FlushQueue();
        }

        private uint BarrelShifterLslImmed()
        {
            // rm lsl immed
            uint rm = registers[this.curInstruction & 0xF];
            int amount = (int)((this.curInstruction >> 7) & 0x1F);

            if (amount == 0)
            {
                this.shifterCarry = this.carry;
                return rm;
            }
            else
            {
                this.shifterCarry = (rm >> (32 - amount)) & 1;
                return rm << amount;
            }
        }

        private uint BarrelShifterLslReg()
        {
            // rm lsl rs
            uint rm = registers[this.curInstruction & 0xF];
            uint rs = (this.curInstruction >> 8) & 0xF;

            int amount;
            if (rs == 15)
            {
                amount = (int)((registers[rs] + 0x4) & 0xFF);
            }
            else
            {
                amount = (int)(registers[rs] & 0xFF);
            }

            if ((this.curInstruction & 0xF) == 15)
            {
                rm += 4;
            }

            if (amount == 0)
            {
                this.shifterCarry = this.carry;
                return rm;
            }

            if (amount < 32)
            {
                this.shifterCarry = (rm >> (32 - amount)) & 1;
                return rm << amount;
            }
            else if (amount == 32)
            {
                this.shifterCarry = rm & 1;
                return 0;
            }
            else
            {
                this.shifterCarry = 0;
                return 0;
            }
        }

        private uint BarrelShifterLsrImmed()
        {
            // rm lsr immed
            uint rm = registers[this.curInstruction & 0xF];
            int amount = (int)((this.curInstruction >> 7) & 0x1F);

            if (amount == 0)
            {
                this.shifterCarry = (rm >> 31) & 1;
                return 0;
            }
            else
            {
                this.shifterCarry = (rm >> (amount - 1)) & 1;
                return rm >> amount;
            }
        }

        private uint BarrelShifterLsrReg()
        {
            // rm lsr rs
            uint rm = registers[this.curInstruction & 0xF];
            uint rs = (this.curInstruction >> 8) & 0xF;

            int amount;
            if (rs == 15)
            {
                amount = (int)((registers[rs] + 0x4) & 0xFF);
            }
            else
            {
                amount = (int)(registers[rs] & 0xFF);
            }

            if ((this.curInstruction & 0xF) == 15)
            {
                rm += 4;
            }

            if (amount == 0)
            {
                this.shifterCarry = this.carry;
                return rm;
            }

            if (amount < 32)
            {
                this.shifterCarry = (rm >> (amount - 1)) & 1;
                return rm >> amount;
            }
            else if (amount == 32)
            {
                this.shifterCarry = (rm >> 31) & 1;
                return 0;
            }
            else
            {
                this.shifterCarry = 0;
                return 0;
            }
        }

        private uint BarrelShifterAsrImmed()
        {
            // rm asr immed
            uint rm = registers[this.curInstruction & 0xF];
            int amount = (int)((this.curInstruction >> 7) & 0x1F);

            if (amount == 0)
            {
                if ((rm & (1 << 31)) == 0)
                {
                    this.shifterCarry = 0;
                    return 0;
                }
                else
                {
                    this.shifterCarry = 1;
                    return 0xFFFFFFFF;
                }
            }
            else
            {
                this.shifterCarry = (rm >> (amount - 1)) & 1;
                return (uint)(((int)rm) >> amount);
            }
        }

        private uint BarrelShifterAsrReg()
        {
            // rm asr rs
            uint rm = registers[this.curInstruction & 0xF];
            uint rs = (this.curInstruction >> 8) & 0xF;

            int amount;
            if (rs == 15)
            {
                amount = (int)((registers[rs] + 0x4) & 0xFF);
            }
            else
            {
                amount = (int)(registers[rs] & 0xFF);
            }

            if ((this.curInstruction & 0xF) == 15)
            {
                rm += 4;
            }

            if (amount == 0)
            {
                this.shifterCarry = this.carry;
                return rm;
            }

            if (amount >= 32)
            {
                if ((rm & (1 << 31)) == 0)
                {
                    this.shifterCarry = 0;
                    return 0;
                }
                else
                {
                    this.shifterCarry = 1;
                    return 0xFFFFFFFF;
                }
            }
            else
            {
                this.shifterCarry = (rm >> (amount - 1)) & 1;
                return (uint)(((int)rm) >> amount);
            }
        }

        private uint BarrelShifterRorImmed()
        {
            // rm ror immed
            uint rm = registers[this.curInstruction & 0xF];
            int amount = (int)((this.curInstruction >> 7) & 0x1F);

            if (amount == 0)
            {
                // Actually an RRX
                this.shifterCarry = rm & 1;
                return (this.carry << 31) | (rm >> 1);
            }
            else
            {
                this.shifterCarry = (rm >> (amount - 1)) & 1;
                return (rm >> amount) | (rm << (32 - amount));
            }
        }

        private uint BarrelShifterRorReg()
        {
            // rm ror rs
            uint rm = registers[this.curInstruction & 0xF];
            uint rs = (this.curInstruction >> 8) & 0xF;

            int amount;
            if (rs == 15)
            {
                amount = (int)((registers[rs] + 0x4) & 0xFF);
            }
            else
            {
                amount = (int)(registers[rs] & 0xFF);
            }

            if ((this.curInstruction & 0xF) == 15)
            {
                rm += 4;
            }

            if (amount == 0)
            {
                this.shifterCarry = this.carry;
                return rm;
            }

            if ((amount & 0x1F) == 0)
            {
                this.shifterCarry = (rm >> 31) & 1;
                return rm;
            }
            else
            {
                amount &= 0x1F;
                this.shifterCarry = (rm >> amount) & 1;
                return (rm >> amount) | (rm << (32 - amount));
            }
        }

        private uint BarrelShifterImmed()
        {
            uint immed = this.curInstruction & 0xFF;
            int rotateAmount = (int)(((this.curInstruction >> 8) & 0xF) * 2);

            if (rotateAmount == 0)
            {
                this.shifterCarry = this.carry;
                return immed;
            }
            else
            {
                immed = (immed >> rotateAmount) | (immed << (32 - rotateAmount));
                this.shifterCarry = (immed >> 31) & 1;
                return immed;
            }
        }

        private int MultiplyCycleCalculation(uint rs)
        {
            // Multiply cycle calculations
            if ((registers[rs] & 0xFFFFFF00) == 0 || (registers[rs] & 0xFFFFFF00) == 0xFFFFFF00)
            {
                return 1;
            }
            else if ((registers[rs] & 0xFFFF0000) == 0 || (registers[rs] & 0xFFFF0000) == 0xFFFF0000)
            {
                return 2;
            }
            else if ((registers[rs] & 0xFF000000) == 0 || (registers[rs] & 0xFF000000) == 0xFF000000)
            {
                return 3;
            }
            return 4;
        }

        private void UndefinedInstruction()
        {
            // Do the undefined instruction dance here
            throw new Exception("Undefined exception");
        }

        private void ExecuteInstruction(ushort op)
        {
            // Not emulating rn += 4 when register shift, in data operand when rn == pc

            uint rn, rd, rs, rm, address, offset, shifterOperand, alu;
            int cycles;

            switch (op)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // AND implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x000:
                case 0x008:
                    // AND rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x001:
                    // AND rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x002:
                case 0x00A:
                    // AND rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x003:
                    // AND rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x004:
                case 0x00C:
                    // AND rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x005:
                    // AND rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x006:
                case 0x00E:
                    // AND rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x007:
                    // AND rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;
                
                case 0x010:
                case 0x018:
                    // ANDS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLslImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x011:
                    // ANDS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLslReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x012:
                case 0x01A:
                    // ANDS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x013:
                    // ANDS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterLsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x014:
                case 0x01C:
                    // ANDS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterAsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x015:
                    // ANDS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterAsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x016:
                case 0x01E:
                    // ANDS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterRorImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x017:
                    // ANDS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & BarrelShifterRorReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // Undefined instruction implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x00D: case 0x02D: case 0x04D: case 0x06D:
                case 0x08D: case 0x0AD: case 0x0CD: case 0x0ED:
                case 0x10D: case 0x12D: case 0x14D: case 0x16D:
                case 0x18D: case 0x1AD: case 0x1CD: case 0x1ED:
                case 0x00F: case 0x02F: case 0x04F: case 0x06F:
                case 0x08F: case 0x0AF: case 0x0CF: case 0x0EF:
                case 0x10F: case 0x12F: case 0x14F: case 0x16F:
                case 0x18F: case 0x1AF: case 0x1CF: case 0x1EF:
                    UndefinedInstruction();
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // MUL implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x009:
                    // MUL rd, rm, rs
                    rd = (this.curInstruction >> 16) & 0xF;
                    rs = (this.curInstruction >> 8) & 0xF;
                    rm = this.curInstruction & 0xF;

                    cycles = this.MultiplyCycleCalculation(rs);

                    registers[rd] = registers[rs] * registers[rm];
                    this.parent.Cycles -= cycles;
                    break;

                case 0x019:
                    // MULS rd, rm, rs
                    rd = (this.curInstruction >> 16) & 0xF;
                    rs = (this.curInstruction >> 8) & 0xF;
                    rm = this.curInstruction & 0xF;

                    cycles = this.MultiplyCycleCalculation(rs);

                    registers[rd] = registers[rs] * registers[rm];

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;

                    this.parent.Cycles -= cycles;
                    break;

                case 0x029:
                    // MLA rd, rm, rs, rn
                    rd = (this.curInstruction >> 16) & 0xF;
                    rn = registers[(this.curInstruction >> 12) & 0xF];
                    rs = (this.curInstruction >> 8) & 0xF;
                    rm = this.curInstruction & 0xF;

                    cycles = this.MultiplyCycleCalculation(rs);

                    registers[rd] = registers[rs] * registers[rm] + rn;
                    this.parent.Cycles -= cycles + 1;
                    break;

                case 0x039:
                    // MLAS rd, rm, rs
                    rd = (this.curInstruction >> 16) & 0xF;
                    rn = registers[(this.curInstruction >> 12) & 0xF];
                    rs = (this.curInstruction >> 8) & 0xF;
                    rm = this.curInstruction & 0xF;

                    cycles = this.MultiplyCycleCalculation(rs);

                    registers[rd] = registers[rs] * registers[rm] + rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;

                    this.parent.Cycles -= cycles + 1;
                    break;

                case 0x089:
                    // UMULL rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 1;

                        ulong result = ((ulong)registers[rm]) * registers[rs];
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x099:
                    // UMULLS rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 1;

                        ulong result = ((ulong)registers[rm]) * registers[rs];
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        negative = registers[rdhi] >> 31;
                        zero = (registers[rdhi] == 0 && registers[rdlo] == 0) ? 1U : 0U;

                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x0A9:
                    // UMLAL rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 2;

                        ulong accum = (((ulong)registers[rdhi]) << 32) | registers[rdlo];
                        ulong result = ((ulong)registers[rm]) * registers[rs];
                        result += accum;
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x0B9:
                    // UMLALS rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 2;

                        ulong accum = (((ulong)registers[rdhi]) << 32) | registers[rdlo];
                        ulong result = ((ulong)registers[rm]) * registers[rs];
                        result += accum;
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        negative = registers[rdhi] >> 31;
                        zero = (registers[rdhi] == 0 && registers[rdlo] == 0) ? 1U : 0U;

                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x0C9:
                    // SMULL rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 1;

                        long result = ((long)((int)registers[rm])) * ((long)((int)registers[rs]));
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);
                        
                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x0D9:
                    // SMULLS rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 1;

                        long result = ((long)((int)registers[rm])) * ((long)((int)registers[rs]));
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        negative = registers[rdhi] >> 31;
                        zero = (registers[rdhi] == 0 && registers[rdlo] == 0) ? 1U : 0U;

                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x0E9:
                    // SMLAL rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 2;

                        long accum = (((long)((int)registers[rdhi])) << 32) | registers[rdlo];
                        long result = ((long)((int)registers[rm])) * ((long)((int)registers[rs]));
                        result += accum;
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        this.parent.Cycles -= cycles;
                    }
                    break;

                case 0x0F9:
                    // SMLALS rdlo, rdhi, rm, rs
                    {
                        uint rdhi = (this.curInstruction >> 16) & 0xF;
                        uint rdlo = (this.curInstruction >> 12) & 0xF;
                        rs = (this.curInstruction >> 8) & 0xF;
                        rm = this.curInstruction & 0xF;

                        cycles = this.MultiplyCycleCalculation(rs) + 2;

                        long accum = (((long)((int)registers[rdhi])) << 32) | registers[rdlo];
                        long result = ((long)((int)registers[rm])) * ((long)((int)registers[rs]));
                        result += accum;
                        registers[rdhi] = (uint)(result >> 32);
                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);

                        negative = registers[rdhi] >> 31;
                        zero = (registers[rdhi] == 0 && registers[rdlo] == 0) ? 1U : 0U;

                        this.parent.Cycles -= cycles;
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // STRH implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x00B:
                    // STRH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x02B:
                    // Writeback bit set, instruction is unpredictable
                    // STRH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x04B:
                    // STRH rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x06B:
                    // Writeback bit set, instruction is unpredictable
                    // STRH rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x08B:
                    // STRH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x0AB:
                    // Writeback bit set, instruction is unpredictable
                    // STRH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x0CB:
                    // STRH rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x0EB:
                    // Writeback bit set, instruction is unpredictable
                    // STRH rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                    registers[rn] = address + offset;
                    break;

                case 0x10B:
                    // STRH rd, [rn, -rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x12B:
                    // STRH rd, [rn, -rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x14B:
                    // STRH rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x16B:
                    // STRH rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x18B:
                    // STRH rd, [rn, rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x1AB:
                    // STRH rd, [rn, rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rn] += offset;
                    this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x1CB:
                    // STRH rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
                    break;

                case 0x1EB:
                    // STRH rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rn] += offset;
                    this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // LDRH implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x01B:
                    // LDRH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x03B:
                    // Writeback bit set, instruction is unpredictable
                    // LDRH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x05B:
                    // LDRH rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x07B:
                    // Writeback bit set, instruction is unpredictable
                    // LDRH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x09B:
                    // LDRH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0BB:
                    // Writeback bit set, instruction is unpredictable
                    // LDRH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0DB:
                    // LDRH rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0FB:
                    // Writeback bit set, instruction is unpredictable
                    // LDRH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x11B:
                    // LDRH rd, [rn, -rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x13B:
                    // LDRH rd, [rn, -rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rn] += offset;

                    registers[rd] = this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x15B:
                    // LDRH rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x17B:
                    // LDRH rd, [rn, -rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x19B:
                    // LDRH rd, [rn, rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1BB:
                    // LDRH rd, [rn, rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1DB:
                    // LDRH rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1FB:
                    // LDRH rd, [rn, rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // LDRSB implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x01D:
                    // LDRSB rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x03D:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSB rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x05D:
                    // LDRSB rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x07D:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSB rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x09D:
                    // LDRSB rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0BD:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSB rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0DD:
                    // LDRSB rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0FD:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSB rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x11D:
                    // LDRSB rd, [rn, -rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x13D:
                    // LDRSB rd, [rn, -rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x15D:
                    // LDRSB rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x17D:
                    // LDRSB rd, [rn, -immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x19D:
                    // LDRSB rd, [rn, rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1BD:
                    // LDRSB rd, [rn, rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1DD:
                    // LDRSB rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1FD:
                    // LDRSB rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rn] += offset;
                    registers[rd] = (uint)(sbyte)this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // LDRSH implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x01F:
                    // LDRSH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x03F:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSH rd, [rn], -rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x05F:
                    // LDRSH rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x07F:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSH rd, [rn], -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x09F:
                    // LDRSH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0BF:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSH rd, [rn], rm
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0DF:
                    // LDRSH rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x0FF:
                    // Writeback bit set, instruction is unpredictable
                    // LDRSH rd, [rn], immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    address = registers[rn];

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = (uint)(short)this.memory.ReadU16(address);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] = address + offset;
                    break;

                case 0x11F:
                    // LDRSH rd, [rn, -rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x13F:
                    // LDRSH rd, [rn, -rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x15F:
                    // LDRSH rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x17F:
                    // LDRSH rd, [rn, -immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x19F:
                    // LDRSH rd, [rn, rm]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1BF:
                    // LDRSH rd, [rn, rm]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.registers[this.curInstruction & 0xF];

                    registers[rn] += offset;
                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1DF:
                    // LDRSH rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x1FF:
                    // LDRSH rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

                    registers[rn] += offset;
                    registers[rd] = (uint)(short)this.memory.ReadU16(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // EOR implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x020:
                case 0x028:
                    // EOR rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x021:
                    // EOR rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x022:
                case 0x02A:
                    // EOR rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x023:
                    // EOR rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x024:
                case 0x02C:
                    // EOR rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x025:
                    // EOR rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x026:
                case 0x02E:
                    // EOR rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x027:
                    // EOR rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x030:
                case 0x038:
                    // EORS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLslImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x031:
                    // EORS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLslReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x032:
                case 0x03A:
                    // EORS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x033:
                    // EORS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterLsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x034:
                case 0x03C:
                    // EORS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterAsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x035:
                    // EORS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterAsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x036:
                case 0x03E:
                    // EORS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterRorImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x037:
                    // EORS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn ^ BarrelShifterRorReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // SUB implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x040:
                case 0x048:
                    // SUB rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x041:
                    // SUB rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x042:
                case 0x04A:
                    // SUB rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x043:
                    // SUB rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x044:
                case 0x04C:
                    // SUB rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x045:
                    // SUB rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x046:
                case 0x04E:
                    // SUB rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x047:
                    // SUB rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x050:
                case 0x058:
                    // SUBS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslImmed();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x051:
                    // SUBS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslReg();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x052:
                case 0x05A:
                    // SUBS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrImmed();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x053:
                    // SUBS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrReg();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x054:
                case 0x05C:
                    // SUBS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrImmed();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x055:
                    // SUBS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrReg();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x056:
                case 0x05E:
                    // SUBS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorImmed();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x057:
                    // SUBS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorReg();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // RSB implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x060:
                case 0x068:
                    // RSB rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslImmed() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x061:
                    // RSB rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslReg() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x062:
                case 0x06A:
                    // RSB rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrImmed() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x063:
                    // RSB rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrReg() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x064:
                case 0x06C:
                    // RSB rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrImmed() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x065:
                    // RSB rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrReg() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x066:
                case 0x06E:
                    // RSB rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorImmed() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x067:
                    // RSB rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorReg() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x070:
                case 0x078:
                    // RSBS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslImmed();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x071:
                    // RSBS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslReg();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x072:
                case 0x07A:
                    // RSBS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrImmed();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x073:
                    // RSBS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrReg();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x074:
                case 0x07C:
                    // RSBS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrImmed();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x075:
                    // RSBS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrReg();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x076:
                case 0x07E:
                    // RSBS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorImmed();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x077:
                    // RSBS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorReg();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // ADD implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x080:
                case 0x088:
                    // ADD rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x081:
                    // ADD rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x082:
                case 0x08A:
                    // ADD rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x083:
                    // ADD rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x084:
                case 0x08C:
                    // ADD rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x085:
                    // ADD rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x086:
                case 0x08E:
                    // ADD rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x087:
                    // ADD rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x090:
                case 0x098:
                    // ADDS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslImmed();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x091:
                    // ADDS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslReg();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x092:
                case 0x09A:
                    // ADDS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrImmed();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x093:
                    // ADDS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrReg();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x094:
                case 0x09C:
                    // ADDS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrImmed();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x095:
                    // ADDS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrReg();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x096:
                case 0x09E:
                    // ADDS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorImmed();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x097:
                    // ADDS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorReg();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // ADC implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x0A0:
                case 0x0A8:
                    // ADC rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLslImmed() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A1:
                    // ADC rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLslReg() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A2:
                case 0x0AA:
                    // ADC rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLsrImmed() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A3:
                    // ADC rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterLsrReg() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A4:
                case 0x0AC:
                    // ADC rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterAsrImmed() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A5:
                    // ADC rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterAsrReg() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A6:
                case 0x0AE:
                    // ADC rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterRorImmed() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0A7:
                    // ADC rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn + BarrelShifterRorReg() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0B0:
                case 0x0B8:
                    // ADCS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslImmed();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B1:
                    // ADCS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslReg();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B2:
                case 0x0BA:
                    // ADCS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrImmed();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B3:
                    // ADCS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrReg();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B4:
                case 0x0BC:
                    // ADCS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrImmed();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B5:
                    // ADCS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrReg();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B6:
                case 0x0BE:
                    // ADCS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorImmed();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0B7:
                    // ADCS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorReg();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // SBC implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x0C0:
                case 0x0C8:
                    // SBC rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLslImmed() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C1:
                    // SBC rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLslReg() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C2:
                case 0x0CA:
                    // SBC rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLsrImmed() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C3:
                    // SBC rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterLsrReg() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C4:
                case 0x0CC:
                    // SBC rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterAsrImmed() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C5:
                    // SBC rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterAsrReg() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C6:
                case 0x0CE:
                    // SBC rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterRorImmed() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0C7:
                    // SBC rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn - BarrelShifterRorReg() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0D0:
                case 0x0D8:
                    // SBCS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslImmed();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D1:
                    // SBCS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslReg();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D2:
                case 0x0DA:
                    // SBCS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrImmed();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D3:
                    // SBCS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrReg();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D4:
                case 0x0DC:
                    // SBCS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrImmed();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D5:
                    // SBCS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrReg();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D6:
                case 0x0DE:
                    // SBCS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorImmed();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0D7:
                    // SBCS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorReg();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // RSC implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x0E0:
                case 0x0E8:
                    // RSC rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslImmed() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E1:
                    // RSC rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslReg() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E2:
                case 0x0EA:
                    // RSC rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrImmed() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E3:
                    // RSC rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrReg() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E4:
                case 0x0EC:
                    // RSC rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrImmed() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E5:
                    // RSC rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrReg() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E6:
                case 0x0EE:
                    // RSC rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorImmed() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0E7:
                    // RSC rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorReg() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x0F0:
                case 0x0F8:
                    // RSCS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslImmed();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F1:
                    // RSCS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLslReg();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F2:
                case 0x0FA:
                    // RSCS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrImmed();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F3:
                    // RSCS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterLsrReg();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F4:
                case 0x0FC:
                    // RSCS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrImmed();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F5:
                    // RSCS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterAsrReg();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F6:
                case 0x0FE:
                    // RSCS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorImmed();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x0F7:
                    // RSCS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    shifterOperand = BarrelShifterRorReg();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // Misc. instructions (MSR, MRS, SWP, BX)
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x109:
                    // SWP rd, rm, [rn]
                    {
                        rn = (this.curInstruction >> 16) & 0xF;
                        rd = (this.curInstruction >> 12) & 0xF;
                        rm = this.curInstruction & 0xF;
                        uint tmp = this.memory.ReadU32(registers[rn]);
                        this.memory.WriteU32(registers[rn], registers[rm]);
                        registers[rd] = tmp;

                        if (rd == 15)
                        {
                            this.FlushQueue();
                        }
                    }
                    break;

                case 0x149:
                    // SWPB rd, rm, [rn]
                    {
                        rn = (this.curInstruction >> 16) & 0xF;
                        rd = (this.curInstruction >> 12) & 0xF;
                        rm = this.curInstruction & 0xF;

                        byte tmp = this.memory.ReadU8(registers[rn]);
                        this.memory.WriteU8(registers[rn], (byte)(registers[rm] & 0xFF));
                        registers[rd] = tmp;
                        
                        if (rd == 15)
                        {
                            this.FlushQueue();
                        }
                    }
                    break;

                case 0x100:
                    // MRS rd, cpsr
                    rd = (this.curInstruction >> 12) & 0xF;
                    this.PackFlags();
                    registers[rd] = this.parent.CPSR;
                    break;

                case 0x140:
                    // MRS rd, spsr
                    rd = (this.curInstruction >> 12) & 0xF;
                    if (this.parent.SPSRExists) registers[rd] = this.parent.SPSR;
                    break;

                case 0x120:
                    // MSR cpsr, rm
                    {
                        rm = registers[this.curInstruction & 0xF];
                        bool userMode = (this.parent.CPSR & 0x1F) == Arm7Processor.USR;

                        this.PackFlags();

                        uint tmpCPSR = this.parent.CPSR;

                        if ((this.curInstruction & (1 << 16)) == 1 << 16 && !userMode)
                        {
                            tmpCPSR &= 0xFFFFFF00;
                            tmpCPSR |= rm & 0x000000FF;
                        }
                        if ((this.curInstruction & (1 << 17)) == 1 << 17 && !userMode)
                        {
                            tmpCPSR &= 0xFFFF00FF;
                            tmpCPSR |= rm & 0x0000FF00;
                        }
                        if ((this.curInstruction & (1 << 18)) == 1 << 18 && !userMode)
                        {
                            tmpCPSR &= 0xFF00FFFF;
                            tmpCPSR |= rm & 0x00FF0000;
                        }
                        if ((this.curInstruction & (1 << 19)) == 1 << 19)
                        {
                            tmpCPSR &= 0x00FFFFFF;
                            tmpCPSR |= rm & 0xFF000000;
                        }

                        this.parent.WriteCpsr(tmpCPSR);

                        this.UnpackFlags();

                        // Check for branch back to Thumb Mode
                        if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                        {
                            this.thumbMode = true;
                            return;
                        }
                    }
                    break;

                case 0x160:
                    // MSR spsr, rm
                    if (this.parent.SPSRExists)
                    {
                        rm = registers[this.curInstruction & 0xF];
                        if ((this.curInstruction & (1 << 16)) == 1 << 16)
                        {
                            this.parent.SPSR &= 0xFFFFFF00;
                            this.parent.SPSR |= rm & 0x000000FF;
                        }
                        if ((this.curInstruction & (1 << 17)) == 1 << 17)
                        {
                            this.parent.SPSR &= 0xFFFF00FF;
                            this.parent.SPSR |= rm & 0x0000FF00;
                        }
                        if ((this.curInstruction & (1 << 18)) == 1 << 18)
                        {
                            this.parent.SPSR &= 0xFF00FFFF;
                            this.parent.SPSR |= rm & 0x00FF0000;
                        }
                        if ((this.curInstruction & (1 << 19)) == 1 << 19)
                        {
                            this.parent.SPSR &= 0x00FFFFFF;
                            this.parent.SPSR |= rm & 0xFF000000;
                        }
                    }
                    break;

                case 0x320:
                    // MSR cpsr, immed
                    {
                        uint immed = this.curInstruction & 0xFF;
                        int rotateAmount = (int)(((this.curInstruction >> 8) & 0xF) * 2);

                        immed = (immed >> rotateAmount) | (immed << (32 - rotateAmount));

                        bool userMode = (this.parent.CPSR & 0x1F) == Arm7Processor.USR;

                        this.PackFlags();

                        uint tmpCPSR = this.parent.CPSR;

                        if ((this.curInstruction & (1 << 16)) == 1 << 16 && !userMode)
                        {
                            tmpCPSR &= 0xFFFFFF00;
                            tmpCPSR |= immed & 0x000000FF;
                        }
                        if ((this.curInstruction & (1 << 17)) == 1 << 17 && !userMode)
                        {
                            tmpCPSR &= 0xFFFF00FF;
                            tmpCPSR |= immed & 0x0000FF00;
                        }
                        if ((this.curInstruction & (1 << 18)) == 1 << 18 && !userMode)
                        {
                            tmpCPSR &= 0xFF00FFFF;
                            tmpCPSR |= immed & 0x00FF0000;
                        }
                        if ((this.curInstruction & (1 << 19)) == 1 << 19)
                        {
                            tmpCPSR &= 0x00FFFFFF;
                            tmpCPSR |= immed & 0xFF000000;
                        }

                        this.parent.WriteCpsr(tmpCPSR);

                        this.UnpackFlags();

                        // Check for branch back to Thumb Mode
                        if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                        {
                            this.thumbMode = true;
                            return;
                        }
                    }
                    break;

                case 0x360:
                    // MSR spsr, immed
                    if (this.parent.SPSRExists)
                    {
                        uint immed = this.curInstruction & 0xFF;
                        int rotateAmount = (int)(((this.curInstruction >> 8) & 0xF) * 2);

                        immed = (immed >> rotateAmount) | (immed << (32 - rotateAmount));

                        if ((this.curInstruction & (1 << 16)) == 1 << 16)
                        {
                            this.parent.SPSR &= 0xFFFFFF00;
                            this.parent.SPSR |= immed & 0x000000FF;
                        }
                        if ((this.curInstruction & (1 << 17)) == 1 << 17)
                        {
                            this.parent.SPSR &= 0xFFFF00FF;
                            this.parent.SPSR |= immed & 0x0000FF00;
                        }
                        if ((this.curInstruction & (1 << 18)) == 1 << 18)
                        {
                            this.parent.SPSR &= 0xFF00FFFF;
                            this.parent.SPSR |= immed & 0x00FF0000;
                        }
                        if ((this.curInstruction & (1 << 19)) == 1 << 19)
                        {
                            this.parent.SPSR &= 0x00FFFFFF;
                            this.parent.SPSR |= immed & 0xFF000000;
                        }
                    }
                    break;

                case 0x121:
                    // BX rm
                    rm = this.curInstruction & 0xf;

                    this.PackFlags();

                    this.parent.CPSR &= ~Arm7Processor.T_MASK;
                    this.parent.CPSR |= (registers[rm] & 1) << Arm7Processor.T_BIT;

                    registers[15] = registers[rm] & (~1U);

                    this.UnpackFlags();

                    // Check for branch back to Thumb Mode
                    if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                    {
                        this.thumbMode = true;
                        return;
                    }

                    this.FlushQueue();
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // TST implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x110:
                case 0x118:
                    // TSTS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterLslImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x111:
                    // TSTS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterLslReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x112:
                case 0x11A:
                    // TSTS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterLsrImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x113:
                    // TSTS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterLsrReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x114:
                case 0x11C:
                    // TSTS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterAsrImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x115:
                    // TSTS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterAsrReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x116:
                case 0x11E:
                    // TSTS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterRorImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x117:
                    // TSTS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = rn & BarrelShifterRorReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // TEQ implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x130:
                case 0x138:
                    // TEQS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterLslImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x131:
                    // TEQS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterLslReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x132:
                case 0x13A:
                    // TEQS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterLsrImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x133:
                    // TEQS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterLsrReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x134:
                case 0x13C:
                    // TEQS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterAsrImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x135:
                    // TEQS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterAsrReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x136:
                case 0x13E:
                    // TEQS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterRorImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x137:
                    // TEQS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = rn ^ BarrelShifterRorReg();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // CMP implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x150:
                case 0x158:
                    // CMP rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLslImmed();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x151:
                    // CMP rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLslReg();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x152:
                case 0x15A:
                    // CMP rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLsrImmed();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x153:
                    // CMP rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLsrReg();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x154:
                case 0x15C:
                    // CMP rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterAsrImmed();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x155:
                    // CMP rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterAsrReg();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x156:
                case 0x15E:
                    // CMP rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterRorImmed();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x157:
                    // CMP rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterRorReg();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // CMN implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x170:
                case 0x178:
                    // CMN rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLslImmed();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x171:
                    // CMN rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLslReg();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x172:
                case 0x17A:
                    // CMN rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLsrImmed();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x173:
                    // CMN rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterLsrReg();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x174:
                case 0x17C:
                    // CMN rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterAsrImmed();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x175:
                    // CMN rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterAsrReg();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x176:
                case 0x17E:
                    // CMN rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterRorImmed();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x177:
                    // CMN rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterRorReg();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // ORR implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x180:
                case 0x188:
                    // ORR rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x181:
                    // ORR rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x182:
                case 0x18A:
                    // ORR rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x183:
                    // ORR rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x184:
                case 0x18C:
                    // ORR rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x185:
                    // ORR rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x186:
                case 0x18E:
                    // ORR rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x187:
                    // ORR rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x190:
                case 0x198:
                    // ORRS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLslImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x191:
                    // ORRS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLslReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x192:
                case 0x19A:
                    // ORRS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x193:
                    // ORRS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterLsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x194:
                case 0x19C:
                    // ORRS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterAsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x195:
                    // ORRS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterAsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x196:
                case 0x19E:
                    // ORRS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterRorImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x197:
                    // ORRS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn | BarrelShifterRorReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // MOV implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x1A0:
                case 0x1A8:
                    // MOV rd, rm lsl immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A1:
                    // MOV rd, rm lsl rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A2:
                case 0x1AA:
                    // MOV rd, rm lsr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A3:
                    // MOV rd, rm lsr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A4:
                case 0x1AC:
                    // MOV rd, rm asr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A5:
                    // MOV rd, rm asr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A6:
                case 0x1AE:
                    // MOV rd, rm ror immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1A7:
                    // MOV rd, rm ror rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1B0:
                case 0x1B8:
                    // MOVS rd, rm lsl immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B1:
                    // MOVS rd, rm lsl rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLslReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B2:
                case 0x1BA:
                    // MOVS rd, rm lsr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B3:
                    // MOVS rd, rn, rm lsr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterLsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B4:
                case 0x1BC:
                    // MOVS rd, rn, rm asr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B5:
                    // MOVS rd, rn, rm asr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterAsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B6:
                case 0x1BE:
                    // MOVS rd, rn, rm ror immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1B7:
                    // MOVS rd, rn, rm ror rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterRorReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // BIC implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x1C0:
                case 0x1C8:
                    // BIC rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C1:
                    // BIC rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C2:
                case 0x1CA:
                    // BIC rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C3:
                    // BIC rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C4:
                case 0x1CC:
                    // BIC rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C5:
                    // BIC rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C6:
                case 0x1CE:
                    // BIC rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1C7:
                    // BIC rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1D0:
                case 0x1D8:
                    // BICS rd, rn, rm lsl immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLslImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D1:
                    // BICS rd, rn, rm lsl rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLslReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D2:
                case 0x1DA:
                    // BICS rd, rn, rm lsr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D3:
                    // BICS rd, rn, rm lsr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterLsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D4:
                case 0x1DC:
                    // BICS rd, rn, rm asr immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterAsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D5:
                    // BICS rd, rn, rm asr rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterAsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D6:
                case 0x1DE:
                    // BICS rd, rn, rm ror immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterRorImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1D7:
                    // BICS rd, rn, rm ror rs
                    rn = registers[(this.curInstruction >> 16) & 0xF];
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = rn & ~BarrelShifterRorReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // MVN implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x1E0:
                case 0x1E8:
                    // MVN rd, rm lsl immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLslImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E1:
                    // MVN rd, rm lsl rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLslReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E2:
                case 0x1EA:
                    // MVN rd, rm lsr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E3:
                    // MVN rd, rm lsr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E4:
                case 0x1EC:
                    // MVN rd, rm asr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterAsrImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E5:
                    // MVN rd, rm asr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterAsrReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E6:
                case 0x1EE:
                    // MVN rd, rm ror immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterRorImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1E7:
                    // MVN rd, rm ror rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterRorReg();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x1F0:
                case 0x1F8:
                    // MVNS rd, rm lsl immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLslImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F1:
                    // MVNS rd, rm lsl rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLslReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F2:
                case 0x1FA:
                    // MVNS rd, rm lsr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F3:
                    // MVNS rd, rn, rm lsr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterLsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F4:
                case 0x1FC:
                    // MVNS rd, rn, rm asr immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterAsrImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F5:
                    // MVNS rd, rn, rm asr rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterAsrReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F6:
                case 0x1FE:
                    // MVNS rd, rn, rm ror immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterRorImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x1F7:
                    // MVNS rd, rn, rm ror rs
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterRorReg();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // Data processing immediate operand implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x200: case 0x201: case 0x202: case 0x203: case 0x204: case 0x205: case 0x206: case 0x207:
                case 0x208: case 0x209: case 0x20A: case 0x20B: case 0x20C: case 0x20D: case 0x20E: case 0x20F:
                    // AND rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn & BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x210: case 0x211: case 0x212: case 0x213: case 0x214: case 0x215: case 0x216: case 0x217:
                case 0x218: case 0x219: case 0x21A: case 0x21B: case 0x21C: case 0x21D: case 0x21E: case 0x21F:
                    // ANDS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn & BarrelShifterImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x220: case 0x221: case 0x222: case 0x223: case 0x224: case 0x225: case 0x226: case 0x227:
                case 0x228: case 0x229: case 0x22A: case 0x22B: case 0x22C: case 0x22D: case 0x22E: case 0x22F:
                    // EOR rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn ^ BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x230: case 0x231: case 0x232: case 0x233: case 0x234: case 0x235: case 0x236: case 0x237:
                case 0x238: case 0x239: case 0x23A: case 0x23B: case 0x23C: case 0x23D: case 0x23E: case 0x23F:
                    // EORS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn ^ BarrelShifterImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x240: case 0x241: case 0x242: case 0x243: case 0x244: case 0x245: case 0x246: case 0x247:
                case 0x248: case 0x249: case 0x24A: case 0x24B: case 0x24C: case 0x24D: case 0x24E: case 0x24F:
                    // SUB rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn - BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x250: case 0x251: case 0x252: case 0x253: case 0x254: case 0x255: case 0x256: case 0x257:
                case 0x258: case 0x259: case 0x25A: case 0x25B: case 0x25C: case 0x25D: case 0x25E: case 0x25F:
                    // SUBS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    registers[rd] = rn - shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x260: case 0x261: case 0x262: case 0x263: case 0x264: case 0x265: case 0x266: case 0x267:
                case 0x268: case 0x269: case 0x26A: case 0x26B: case 0x26C: case 0x26D: case 0x26E: case 0x26F:
                    // RSB rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = BarrelShifterImmed() - rn;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x270: case 0x271: case 0x272: case 0x273: case 0x274: case 0x275: case 0x276: case 0x277:
                case 0x278: case 0x279: case 0x27A: case 0x27B: case 0x27C: case 0x27D: case 0x27E: case 0x27F:
                    // RSBS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    registers[rd] = shifterOperand - rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x280: case 0x281: case 0x282: case 0x283: case 0x284: case 0x285: case 0x286: case 0x287:
                case 0x288: case 0x289: case 0x28A: case 0x28B: case 0x28C: case 0x28D: case 0x28E: case 0x28F:
                    // ADD rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn + BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x290: case 0x291: case 0x292: case 0x293: case 0x294: case 0x295: case 0x296: case 0x297:
                case 0x298: case 0x299: case 0x29A: case 0x29B: case 0x29C: case 0x29D: case 0x29E: case 0x29F:
                    // ADDS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    registers[rd] = rn + shifterOperand;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x2A0: case 0x2A1: case 0x2A2: case 0x2A3: case 0x2A4: case 0x2A5: case 0x2A6: case 0x2A7:
                case 0x2A8: case 0x2A9: case 0x2AA: case 0x2AB: case 0x2AC: case 0x2AD: case 0x2AE: case 0x2AF:
                    // ADC rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn + BarrelShifterImmed() + carry;

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x2B0: case 0x2B1: case 0x2B2: case 0x2B3: case 0x2B4: case 0x2B5: case 0x2B6: case 0x2B7:
                case 0x2B8: case 0x2B9: case 0x2BA: case 0x2BB: case 0x2BC: case 0x2BD: case 0x2BE: case 0x2BF:
                    // ADCS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    registers[rd] = rn + shifterOperand + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x2C0: case 0x2C1: case 0x2C2: case 0x2C3: case 0x2C4: case 0x2C5: case 0x2C6: case 0x2C7:
                case 0x2C8: case 0x2C9: case 0x2CA: case 0x2CB: case 0x2CC: case 0x2CD: case 0x2CE: case 0x2CF:
                    // SBC rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn - BarrelShifterImmed() - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x2D0: case 0x2D1: case 0x2D2: case 0x2D3: case 0x2D4: case 0x2D5: case 0x2D6: case 0x2D7:
                case 0x2D8: case 0x2D9: case 0x2DA: case 0x2DB: case 0x2DC: case 0x2DD: case 0x2DE: case 0x2DF:
                    // SBCS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    registers[rd] = rn - shifterOperand - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x2E0: case 0x2E1: case 0x2E2: case 0x2E3: case 0x2E4: case 0x2E5: case 0x2E6: case 0x2E7:
                case 0x2E8: case 0x2E9: case 0x2EA: case 0x2EB: case 0x2EC: case 0x2ED: case 0x2EE: case 0x2EF:
                    // RSC rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = BarrelShifterImmed() - rn - (1U - carry);

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x2F0: case 0x2F1: case 0x2F2: case 0x2F3: case 0x2F4: case 0x2F5: case 0x2F6: case 0x2F7:
                case 0x2F8: case 0x2F9: case 0x2FA: case 0x2FB: case 0x2FC: case 0x2FD: case 0x2FE: case 0x2FF:
                    // RSCS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    registers[rd] = shifterOperand - rn - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(shifterOperand, rn, registers[rd]);

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x310: case 0x311: case 0x312: case 0x313: case 0x314: case 0x315: case 0x316: case 0x317:
                case 0x318: case 0x319: case 0x31A: case 0x31B: case 0x31C: case 0x31D: case 0x31E: case 0x31F:
                    // TSTS rn, immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn & BarrelShifterImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x330: case 0x331: case 0x332: case 0x333: case 0x334: case 0x335: case 0x336: case 0x337:
                case 0x338: case 0x339: case 0x33A: case 0x33B: case 0x33C: case 0x33D: case 0x33E: case 0x33F:
                    // TEQS rn, immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    alu = rn ^ BarrelShifterImmed();

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    carry = this.shifterCarry;
                    break;

                case 0x350: case 0x351: case 0x352: case 0x353: case 0x354: case 0x355: case 0x356: case 0x357:
                case 0x358: case 0x359: case 0x35A: case 0x35B: case 0x35C: case 0x35D: case 0x35E: case 0x35F:
                    // CMP rn, immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    alu = rn - shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(rn, shifterOperand, alu);
                    break;

                case 0x370: case 0x371: case 0x372: case 0x373: case 0x374: case 0x375: case 0x376: case 0x377:
                case 0x378: case 0x379: case 0x37A: case 0x37B: case 0x37C: case 0x37D: case 0x37E: case 0x37F:
                    // CMN rn, immed
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    shifterOperand = BarrelShifterImmed();
                    alu = rn + shifterOperand;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(rn, shifterOperand, alu);
                    break;

                case 0x380: case 0x381: case 0x382: case 0x383: case 0x384: case 0x385: case 0x386: case 0x387:
                case 0x388: case 0x389: case 0x38A: case 0x38B: case 0x38C: case 0x38D: case 0x38E: case 0x38F:
                    // ORR rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn | BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x390: case 0x391: case 0x392: case 0x393: case 0x394: case 0x395: case 0x396: case 0x397:
                case 0x398: case 0x399: case 0x39A: case 0x39B: case 0x39C: case 0x39D: case 0x39E: case 0x39F:
                    // ORRS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn | BarrelShifterImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x3A0: case 0x3A1: case 0x3A2: case 0x3A3: case 0x3A4: case 0x3A5: case 0x3A6: case 0x3A7:
                case 0x3A8: case 0x3A9: case 0x3AA: case 0x3AB: case 0x3AC: case 0x3AD: case 0x3AE: case 0x3AF:
                    // MOV rd, immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x3B0: case 0x3B1: case 0x3B2: case 0x3B3: case 0x3B4: case 0x3B5: case 0x3B6: case 0x3B7:
                case 0x3B8: case 0x3B9: case 0x3BA: case 0x3BB: case 0x3BC: case 0x3BD: case 0x3BE: case 0x3BF:
                    // MOVS rd, immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = BarrelShifterImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x3C0: case 0x3C1: case 0x3C2: case 0x3C3: case 0x3C4: case 0x3C5: case 0x3C6: case 0x3C7:
                case 0x3C8: case 0x3C9: case 0x3CA: case 0x3CB: case 0x3CC: case 0x3CD: case 0x3CE: case 0x3CF:
                    // BIC rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn & ~BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x3D0: case 0x3D1: case 0x3D2: case 0x3D3: case 0x3D4: case 0x3D5: case 0x3D6: case 0x3D7:
                case 0x3D8: case 0x3D9: case 0x3DA: case 0x3DB: case 0x3DC: case 0x3DD: case 0x3DE: case 0x3DF:
                    // BICS rd, rn, immed
                    rd = (this.curInstruction >> 12) & 0xF;
                    rn = registers[(this.curInstruction >> 16) & 0xF];

                    registers[rd] = rn & ~BarrelShifterImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                case 0x3E0: case 0x3E1: case 0x3E2: case 0x3E3: case 0x3E4: case 0x3E5: case 0x3E6: case 0x3E7:
                case 0x3E8: case 0x3E9: case 0x3EA: case 0x3EB: case 0x3EC: case 0x3ED: case 0x3EE: case 0x3EF:
                    // MVN rd, immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterImmed();

                    if (rd == 15)
                    {
                        this.FlushQueue();
                    }
                    break;

                case 0x3F0: case 0x3F1: case 0x3F2: case 0x3F3: case 0x3F4: case 0x3F5: case 0x3F6: case 0x3F7:
                case 0x3F8: case 0x3F9: case 0x3FA: case 0x3FB: case 0x3FC: case 0x3FD: case 0x3FE: case 0x3FF:
                    // MVNS rd, immed
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = ~BarrelShifterImmed();

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    carry = this.shifterCarry;

                    if (rd == 15)
                    {
                        this.DataProcessingWriteToR15();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // STR immediate implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x400: case 0x401: case 0x402: case 0x403: case 0x404: case 0x405: case 0x406: case 0x407:
                case 0x408: case 0x409: case 0x40A: case 0x40B: case 0x40C: case 0x40D: case 0x40E: case 0x40F:
                    // STR rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x420: case 0x421: case 0x422: case 0x423: case 0x424: case 0x425: case 0x426: case 0x427:
                case 0x428: case 0x429: case 0x42A: case 0x42B: case 0x42C: case 0x42D: case 0x42E: case 0x42F:
                    // STRT rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x440: case 0x441: case 0x442: case 0x443: case 0x444: case 0x445: case 0x446: case 0x447:
                case 0x448: case 0x449: case 0x44A: case 0x44B: case 0x44C: case 0x44D: case 0x44E: case 0x44F:
                    // STRB rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x460: case 0x461: case 0x462: case 0x463: case 0x464: case 0x465: case 0x466: case 0x467:
                case 0x468: case 0x469: case 0x46A: case 0x46B: case 0x46C: case 0x46D: case 0x46E: case 0x46F:
                    // STRBT rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x480: case 0x481: case 0x482: case 0x483: case 0x484: case 0x485: case 0x486: case 0x487:
                case 0x488: case 0x489: case 0x48A: case 0x48B: case 0x48C: case 0x48D: case 0x48E: case 0x48F:
                    // STR rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x4A0: case 0x4A1: case 0x4A2: case 0x4A3: case 0x4A4: case 0x4A5: case 0x4A6: case 0x4A7:
                case 0x4A8: case 0x4A9: case 0x4AA: case 0x4AB: case 0x4AC: case 0x4AD: case 0x4AE: case 0x4AF:
                    // STRT rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x4C0: case 0x4C1: case 0x4C2: case 0x4C3: case 0x4C4: case 0x4C5: case 0x4C6: case 0x4C7:
                case 0x4C8: case 0x4C9: case 0x4CA: case 0x4CB: case 0x4CC: case 0x4CD: case 0x4CE: case 0x4CF:
                    // STRB rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x4E0: case 0x4E1: case 0x4E2: case 0x4E3: case 0x4E4: case 0x4E5: case 0x4E6: case 0x4E7:
                case 0x4E8: case 0x4E9: case 0x4EA: case 0x4EB: case 0x4EC: case 0x4ED: case 0x4EE: case 0x4EF:
                    // STRBT rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x500: case 0x501: case 0x502: case 0x503: case 0x504: case 0x505: case 0x506: case 0x507:
                case 0x508: case 0x509: case 0x50A: case 0x50B: case 0x50C: case 0x50D: case 0x50E: case 0x50F:
                    // STR rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + offset, alu);
                    break;

                case 0x520: case 0x521: case 0x522: case 0x523: case 0x524: case 0x525: case 0x526: case 0x527:
                case 0x528: case 0x529: case 0x52A: case 0x52B: case 0x52C: case 0x52D: case 0x52E: case 0x52F:
                    // STR rd, [rn, -immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x540: case 0x541: case 0x542: case 0x543: case 0x544: case 0x545: case 0x546: case 0x547:
                case 0x548: case 0x549: case 0x54A: case 0x54B: case 0x54C: case 0x54D: case 0x54E: case 0x54F:
                    // STRB rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
                    break;

                case 0x560: case 0x561: case 0x562: case 0x563: case 0x564: case 0x565: case 0x566: case 0x567:
                case 0x568: case 0x569: case 0x56A: case 0x56B: case 0x56C: case 0x56D: case 0x56E: case 0x56F:
                    // STRB rd, [rn, -immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x580: case 0x581: case 0x582: case 0x583: case 0x584: case 0x585: case 0x586: case 0x587:
                case 0x588: case 0x589: case 0x58A: case 0x58B: case 0x58C: case 0x58D: case 0x58E: case 0x58F:
                    // STR rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + (this.curInstruction & 0xFFF), alu);
                    break;

                case 0x5A0: case 0x5A1: case 0x5A2: case 0x5A3: case 0x5A4: case 0x5A5: case 0x5A6: case 0x5A7:
                case 0x5A8: case 0x5A9: case 0x5AA: case 0x5AB: case 0x5AC: case 0x5AD: case 0x5AE: case 0x5AF:
                    // STRT rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.curInstruction & 0xFFF;
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x5C0: case 0x5C1: case 0x5C2: case 0x5C3: case 0x5C4: case 0x5C5: case 0x5C6: case 0x5C7:
                case 0x5C8: case 0x5C9: case 0x5CA: case 0x5CB: case 0x5CC: case 0x5CD: case 0x5CE: case 0x5CF:
                    // STRB rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + (this.curInstruction & 0xFFF), (byte)(alu & 0xFF));
                    break;

                case 0x5E0: case 0x5E1: case 0x5E2: case 0x5E3: case 0x5E4: case 0x5E5: case 0x5E6: case 0x5E7:
                case 0x5E8: case 0x5E9: case 0x5EA: case 0x5EB: case 0x5EC: case 0x5ED: case 0x5EE: case 0x5EF:
                    // STRB rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.curInstruction & 0xFFF;
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // LDR immediate implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x410: case 0x411: case 0x412: case 0x413: case 0x414: case 0x415: case 0x416: case 0x417:
                case 0x418: case 0x419: case 0x41A: case 0x41B: case 0x41C: case 0x41D: case 0x41E: case 0x41F:
                    // LDR rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x430: case 0x431: case 0x432: case 0x433: case 0x434: case 0x435: case 0x436: case 0x437:
                case 0x438: case 0x439: case 0x43A: case 0x43B: case 0x43C: case 0x43D: case 0x43E: case 0x43F:
                    // LDRT rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x450: case 0x451: case 0x452: case 0x453: case 0x454: case 0x455: case 0x456: case 0x457:
                case 0x458: case 0x459: case 0x45A: case 0x45B: case 0x45C: case 0x45D: case 0x45E: case 0x45F:
                    // LDRB rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x470: case 0x471: case 0x472: case 0x473: case 0x474: case 0x475: case 0x476: case 0x477:
                case 0x478: case 0x479: case 0x47A: case 0x47B: case 0x47C: case 0x47D: case 0x47E: case 0x47F:
                    // LDRBT rd, rn, -immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x490: case 0x491: case 0x492: case 0x493: case 0x494: case 0x495: case 0x496: case 0x497:
                case 0x498: case 0x499: case 0x49A: case 0x49B: case 0x49C: case 0x49D: case 0x49E: case 0x49F:
                    // LDR rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x4B0: case 0x4B1: case 0x4B2: case 0x4B3: case 0x4B4: case 0x4B5: case 0x4B6: case 0x4B7:
                case 0x4B8: case 0x4B9: case 0x4BA: case 0x4BB: case 0x4BC: case 0x4BD: case 0x4BE: case 0x4BF:
                    // LDRT rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x4D0: case 0x4D1: case 0x4D2: case 0x4D3: case 0x4D4: case 0x4D5: case 0x4D6: case 0x4D7:
                case 0x4D8: case 0x4D9: case 0x4DA: case 0x4DB: case 0x4DC: case 0x4DD: case 0x4DE: case 0x4DF:
                    // LDRB rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x4F0: case 0x4F1: case 0x4F2: case 0x4F3: case 0x4F4: case 0x4F5: case 0x4F6: case 0x4F7:
                case 0x4F8: case 0x4F9: case 0x4FA: case 0x4FB: case 0x4FC: case 0x4FD: case 0x4FE: case 0x4FF:
                    // LDRBT rd, rn, immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += this.curInstruction & 0xFFF;
                    break;

                case 0x510: case 0x511: case 0x512: case 0x513: case 0x514: case 0x515: case 0x516: case 0x517:
                case 0x518: case 0x519: case 0x51A: case 0x51B: case 0x51C: case 0x51D: case 0x51E: case 0x51F:
                    // LDR rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x530: case 0x531: case 0x532: case 0x533: case 0x534: case 0x535: case 0x536: case 0x537:
                case 0x538: case 0x539: case 0x53A: case 0x53B: case 0x53C: case 0x53D: case 0x53E: case 0x53F:
                    // LDR rd, [rn, -immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x550: case 0x551: case 0x552: case 0x553: case 0x554: case 0x555: case 0x556: case 0x557:
                case 0x558: case 0x559: case 0x55A: case 0x55B: case 0x55C: case 0x55D: case 0x55E: case 0x55F:
                    // LDRB rd, [rn, -immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x570: case 0x571: case 0x572: case 0x573: case 0x574: case 0x575: case 0x576: case 0x577:
                case 0x578: case 0x579: case 0x57A: case 0x57B: case 0x57C: case 0x57D: case 0x57E: case 0x57F:
                    // LDRB rd, [rn, -immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.curInstruction & 0xFFF;
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x590: case 0x591: case 0x592: case 0x593: case 0x594: case 0x595: case 0x596: case 0x597:
                case 0x598: case 0x599: case 0x59A: case 0x59B: case 0x59C: case 0x59D: case 0x59E: case 0x59F:
                    // LDR rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn] + (this.curInstruction & 0xFFF));

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x5B0: case 0x5B1: case 0x5B2: case 0x5B3: case 0x5B4: case 0x5B5: case 0x5B6: case 0x5B7:
                case 0x5B8: case 0x5B9: case 0x5BA: case 0x5BB: case 0x5BC: case 0x5BD: case 0x5BE: case 0x5BF:
                    // LDR rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.curInstruction & 0xFFF;
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x5D0: case 0x5D1: case 0x5D2: case 0x5D3: case 0x5D4: case 0x5D5: case 0x5D6: case 0x5D7:
                case 0x5D8: case 0x5D9: case 0x5DA: case 0x5DB: case 0x5DC: case 0x5DD: case 0x5DE: case 0x5DF:
                    // LDRB rd, [rn, immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn] + (this.curInstruction & 0xFFF));

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x5F0: case 0x5F1: case 0x5F2: case 0x5F3: case 0x5F4: case 0x5F5: case 0x5F6: case 0x5F7:
                case 0x5F8: case 0x5F9: case 0x5FA: case 0x5FB: case 0x5FC: case 0x5FD: case 0x5FE: case 0x5FF:
                    // LDRB rd, [rn, immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.curInstruction & 0xFFF;
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // STR register shift implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x600:
                case 0x608:
                    // STR rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x602:
                case 0x60A:
                    // STR rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x604:
                case 0x60C:
                    // STR rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x606:
                case 0x60E:
                    // STR rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x620:
                case 0x628:
                    // STRT rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x622:
                case 0x62A:
                    // STRT rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x624:
                case 0x62C:
                    // STRT rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x626:
                case 0x62E:
                    // STRT rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += offset;
                    break;

                case 0x640:
                case 0x648:
                    // STRB rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x642:
                case 0x64A:
                    // STRB rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x644:
                case 0x64C:
                    // STRB rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x646:
                case 0x64E:
                    // STRB rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x660:
                case 0x668:
                    // STRBT rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x662:
                case 0x66A:
                    // STRBT rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x664:
                case 0x66C:
                    // STRBT rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x666:
                case 0x66E:
                    // STRBT rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += offset;
                    break;

                case 0x680:
                case 0x688:
                    // STR rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterLslImmed();
                    break;

                case 0x682:
                case 0x68A:
                    // STR rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterLsrImmed();
                    break;

                case 0x684:
                case 0x68C:
                    // STR rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterAsrImmed();
                    break;

                case 0x686:
                case 0x68E:
                    // STR rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterRorImmed();
                    break;

                case 0x6A0:
                case 0x6A2:
                    // STRT rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterLslImmed();
                    break;

                case 0x6A4:
                case 0x6A6:
                    // STRT rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterLsrImmed();
                    break;

                case 0x6A8:
                case 0x6AA:
                    // STRT rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterAsrImmed();
                    break;

                case 0x6AC:
                case 0x6AE:
                    // STRT rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn], alu);
                    registers[rn] += this.BarrelShifterRorImmed();
                    break;

                case 0x6C0:
                case 0x6C8:
                    // STRB rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterLslImmed();
                    break;

                case 0x6C2:
                case 0x6CA:
                    // STRB rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterLsrImmed();
                    break;
                
                case 0x6C4:
                case 0x6CC:
                    // STRB rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterAsrImmed();
                    break;
                
                case 0x6C6:
                case 0x6CE:
                    // STRB rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterRorImmed();
                    break;

                case 0x6E0:
                case 0x6E8:
                    // STRBT rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterLslImmed();
                    break;

                case 0x6E2:
                case 0x6EA:
                    // STRBT rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterLsrImmed();
                    break;

                case 0x6E4:
                case 0x6EC:
                    // STRBT rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterAsrImmed();
                    break;

                case 0x6E6:
                case 0x6EE:
                    // STRBT rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    registers[rn] += this.BarrelShifterRorImmed();
                    break;

                case 0x700:
                case 0x708:
                    // STR rd, [rn, -rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + offset, alu);
                    break;
                
                case 0x702:
                case 0x70A:
                    // STR rd, [rn, -rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + offset, alu);
                    break;

                case 0x704:
                case 0x70C:
                    // STR rd, [rn, -rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + offset, alu);
                    break;

                case 0x706:
                case 0x70E:
                    // STR rd, [rn, -rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + offset, alu);
                    break;

                case 0x720:
                case 0x728:
                    // STR rd, [rn, -rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x722:
                case 0x72A:
                    // STR rd, [rn, -rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x724:
                case 0x72C:
                    // STR rd, [rn, -rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x726:
                case 0x72E:
                    // STR rd, [rn, -rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x740:
                case 0x748:
                    // STRB rd, [rn, -rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
                    break;

                case 0x742:
                case 0x74A:
                    // STRB rd, [rn, -rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
                    break;

                case 0x744:
                case 0x74C:
                    // STRB rd, [rn, -rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
                    break;

                case 0x746:
                case 0x74E:
                    // STRB rd, [rn, -rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
                    break;

                case 0x760:
                case 0x768:
                    // STRB rd, [rn, -rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x762:
                case 0x76A:
                    // STRB rd, [rn, -rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x764:
                case 0x76C:
                    // STRB rd, [rn, -rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x766:
                case 0x76E:
                    // STRB rd, [rn, -rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += offset;
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x780:
                case 0x788:
                    // STR rd, [rn, rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + this.BarrelShifterLslImmed(), alu);
                    break;

                case 0x782:
                case 0x78A:
                    // STR rd, [rn, rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + this.BarrelShifterLsrImmed(), alu);
                    break;

                case 0x784:
                case 0x78C:
                    // STR rd, [rn, rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + this.BarrelShifterAsrImmed(), alu);
                    break;

                case 0x786:
                case 0x78E:
                    // STR rd, [rn, rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU32(registers[rn] + this.BarrelShifterRorImmed(), alu);
                    break;

                case 0x7A0:
                case 0x7A8:
                    // STR rd, [rn, rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterLslImmed();
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x7A2:
                case 0x7AA:
                    // STR rd, [rn, rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterLsrImmed();
                    this.memory.WriteU32(registers[rn], alu);
                    break;
                
                case 0x7A4:
                case 0x7AC:
                    // STR rd, [rn, rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterAsrImmed();
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x7A6:
                case 0x7AE:
                    // STR rd, [rn, rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterRorImmed();
                    this.memory.WriteU32(registers[rn], alu);
                    break;

                case 0x7C0:
                case 0x7C8:
                    // STRB rd, [rn, rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + this.BarrelShifterLslImmed(), (byte)(alu & 0xFF));
                    break;

                case 0x7C2:
                case 0x7CA:
                    // STRB rd, [rn, rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + this.BarrelShifterLsrImmed(), (byte)(alu & 0xFF));
                    break;

                case 0x7C4:
                case 0x7CC:
                    // STRB rd, [rn, rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + this.BarrelShifterAsrImmed(), (byte)(alu & 0xFF));
                    break;

                case 0x7C6:
                case 0x7CE:
                    // STRB rd, [rn, rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    this.memory.WriteU8(registers[rn] + this.BarrelShifterRorImmed(), (byte)(alu & 0xFF));
                    break;

                case 0x7E0:
                case 0x7E8:
                    // STRB rd, [rn, rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterLslImmed();
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x7E2:
                case 0x7EA:
                    // STRB rd, [rn, rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterLsrImmed();
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x7E4:
                case 0x7EC:
                    // STRB rd, [rn, rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterAsrImmed();
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                case 0x7E6:
                case 0x7EE:
                    // STRB rd, [rn, rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    alu = registers[rd];
                    if (rd == 15) alu += 4;

                    registers[rn] += this.BarrelShifterRorImmed();
                    this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // LDR register shift implementations
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0x610:
                case 0x618:
                    // LDR rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x612:
                case 0x61A:
                    // LDR rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x614:
                case 0x61C:
                    // LDR rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x616:
                case 0x61E:
                    // LDR rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x630:
                case 0x638:
                    // LDRT rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x632:
                case 0x63A:
                    // LDRT rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x634:
                case 0x63C:
                    // LDRT rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x636:
                case 0x63E:
                    // LDRT rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x650:
                case 0x658:
                    // LDRB rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x652:
                case 0x65A:
                    // LDRB rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x654:
                case 0x65C:
                    // LDRB rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x656:
                case 0x65E:
                    // LDRB rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x670:
                case 0x678:
                    // LDRBT rd, rn, -rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x672:
                case 0x67A:
                    // LDRBT rd, rn, -rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x674:
                case 0x67C:
                    // LDRBT rd, rn, -rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x676:
                case 0x67E:
                    // LDRBT rd, rn, -rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x690:
                case 0x698:
                    // LDR rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x692:
                case 0x69A:
                    // LDR rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x694:
                case 0x69C:
                    // LDR rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x696:
                case 0x69E:
                    // LDR rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6B0:
                case 0x6B8:
                    // LDRT rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6B2:
                case 0x6BA:
                    // LDRT rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6B4:
                case 0x6BC:
                    // LDRT rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6B6:
                case 0x6BE:
                    // LDRT rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6D0:
                case 0x6D8:
                    // LDRB rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6D2:
                case 0x6DA:
                    // LDRB rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6D4:
                case 0x6DC:
                    // LDRB rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6D6:
                case 0x6DE:
                    // LDRB rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6F0:
                case 0x6F8:
                    // LDRBT rd, rn, rm lsl immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6F2:
                case 0x6FA:
                    // LDRBT rd, rn, rm lsr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6F4:
                case 0x6FC:
                    // LDRBT rd, rn, rm asr immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x6F6:
                case 0x6FE:
                    // LDRBT rd, rn, rm ror immed
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }

                    if (rn != rd)
                        registers[rn] += offset;
                    break;

                case 0x710:
                case 0x718:
                    // LDR rd, [rn, -rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x712:
                case 0x71A:
                    // LDR rd, [rn, -rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x714:
                case 0x71C:
                    // LDR rd, [rn, -rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x716:
                case 0x71E:
                    // LDR rd, [rn, -rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU32(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x730:
                case 0x738:
                    // LDR rd, [rn, -rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x732:
                case 0x73A:
                    // LDR rd, [rn, -rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x734:
                case 0x73C:
                    // LDR rd, [rn, -rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x736:
                case 0x73E:
                    // LDR rd, [rn, -rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x750:
                case 0x758:
                    // LDRB rd, [rn, -rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x752:
                case 0x75A:
                    // LDRB rd, [rn, -rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x754:
                case 0x75C:
                    // LDRB rd, [rn, -rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x756:
                case 0x75E:
                    // LDRB rd, [rn, -rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rd] = this.memory.ReadU8(registers[rn] + offset);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x770:
                case 0x778:
                    // LDRB rd, [rn, -rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLslImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x772:
                case 0x77A:
                    // LDRB rd, [rn, -rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterLsrImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x774:
                case 0x77C:
                    // LDRB rd, [rn, -rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterAsrImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x776:
                case 0x77E:
                    // LDRB rd, [rn, -rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    offset = this.BarrelShifterRorImmed();
                    offset = (uint)-offset;

                    registers[rn] += offset;
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x790:
                case 0x798:
                    // LDR rd, [rn, rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterLslImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x792:
                case 0x79A:
                    // LDR rd, [rn, rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterLsrImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x794:
                case 0x79C:
                    // LDR rd, [rn, rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterAsrImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x796:
                case 0x79E:
                    // LDR rd, [rn, rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterRorImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7B0:
                case 0x7B8:
                    // LDR rd, [rn, rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterLslImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7B2:
                case 0x7BA:
                    // LDR rd, [rn, rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterLsrImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7B4:
                case 0x7BC:
                    // LDR rd, [rn, rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterAsrImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7B6:
                case 0x7BE:
                    // LDR rd, [rn, rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterRorImmed();
                    registers[rd] = this.memory.ReadU32(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7D0:
                case 0x7D8:
                    // LDRB rd, [rn, rm lsl immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterLslImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7D2:
                case 0x7DA:
                    // LDRB rd, [rn, rm lsr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterLsrImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7D4:
                case 0x7DC:
                    // LDRB rd, [rn, rm asr immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterAsrImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7D6:
                case 0x7DE:
                    // LDRB rd, [rn, rm ror immed]
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterRorImmed());

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7F0:
                case 0x7F8:
                    // LDRB rd, [rn, rm lsl immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterLslImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7F2:
                case 0x7FA:
                    // LDRB rd, [rn, rm lsr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterLsrImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7F4:
                case 0x7FC:
                    // LDRB rd, [rn, rm asr immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterAsrImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                case 0x7F6:
                case 0x7FE:
                    // LDRB rd, [rn, rm ror immed]!
                    rn = (this.curInstruction >> 16) & 0xF;
                    rd = (this.curInstruction >> 12) & 0xF;

                    registers[rn] += this.BarrelShifterRorImmed();
                    registers[rd] = this.memory.ReadU8(registers[rn]);

                    if (rd == 15)
                    {
                        registers[rd] &= ~3U;
                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // LDM implementations (TODO)
                //
                ////////////////////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // STM implementations (TODO)
                //
                ////////////////////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // B implementation
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0xA00: case 0xA01: case 0xA02: case 0xA03: case 0xA04: case 0xA05: case 0xA06: case 0xA07:
                case 0xA08: case 0xA09: case 0xA0A: case 0xA0B: case 0xA0C: case 0xA0D: case 0xA0E: case 0xA0F:
                case 0xA10: case 0xA11: case 0xA12: case 0xA13: case 0xA14: case 0xA15: case 0xA16: case 0xA17:
                case 0xA18: case 0xA19: case 0xA1A: case 0xA1B: case 0xA1C: case 0xA1D: case 0xA1E: case 0xA1F:
                case 0xA20: case 0xA21: case 0xA22: case 0xA23: case 0xA24: case 0xA25: case 0xA26: case 0xA27:
                case 0xA28: case 0xA29: case 0xA2A: case 0xA2B: case 0xA2C: case 0xA2D: case 0xA2E: case 0xA2F:
                case 0xA30: case 0xA31: case 0xA32: case 0xA33: case 0xA34: case 0xA35: case 0xA36: case 0xA37:
                case 0xA38: case 0xA39: case 0xA3A: case 0xA3B: case 0xA3C: case 0xA3D: case 0xA3E: case 0xA3F:
                case 0xA40: case 0xA41: case 0xA42: case 0xA43: case 0xA44: case 0xA45: case 0xA46: case 0xA47:
                case 0xA48: case 0xA49: case 0xA4A: case 0xA4B: case 0xA4C: case 0xA4D: case 0xA4E: case 0xA4F:
                case 0xA50: case 0xA51: case 0xA52: case 0xA53: case 0xA54: case 0xA55: case 0xA56: case 0xA57:
                case 0xA58: case 0xA59: case 0xA5A: case 0xA5B: case 0xA5C: case 0xA5D: case 0xA5E: case 0xA5F:
                case 0xA60: case 0xA61: case 0xA62: case 0xA63: case 0xA64: case 0xA65: case 0xA66: case 0xA67:
                case 0xA68: case 0xA69: case 0xA6A: case 0xA6B: case 0xA6C: case 0xA6D: case 0xA6E: case 0xA6F:
                case 0xA70: case 0xA71: case 0xA72: case 0xA73: case 0xA74: case 0xA75: case 0xA76: case 0xA77:
                case 0xA78: case 0xA79: case 0xA7A: case 0xA7B: case 0xA7C: case 0xA7D: case 0xA7E: case 0xA7F:
                case 0xA80: case 0xA81: case 0xA82: case 0xA83: case 0xA84: case 0xA85: case 0xA86: case 0xA87:
                case 0xA88: case 0xA89: case 0xA8A: case 0xA8B: case 0xA8C: case 0xA8D: case 0xA8E: case 0xA8F:
                case 0xA90: case 0xA91: case 0xA92: case 0xA93: case 0xA94: case 0xA95: case 0xA96: case 0xA97:
                case 0xA98: case 0xA99: case 0xA9A: case 0xA9B: case 0xA9C: case 0xA9D: case 0xA9E: case 0xA9F:
                case 0xAA0: case 0xAA1: case 0xAA2: case 0xAA3: case 0xAA4: case 0xAA5: case 0xAA6: case 0xAA7:
                case 0xAA8: case 0xAA9: case 0xAAA: case 0xAAB: case 0xAAC: case 0xAAD: case 0xAAE: case 0xAAF:
                case 0xAB0: case 0xAB1: case 0xAB2: case 0xAB3: case 0xAB4: case 0xAB5: case 0xAB6: case 0xAB7:
                case 0xAB8: case 0xAB9: case 0xABA: case 0xABB: case 0xABC: case 0xABD: case 0xABE: case 0xABF:
                case 0xAC0: case 0xAC1: case 0xAC2: case 0xAC3: case 0xAC4: case 0xAC5: case 0xAC6: case 0xAC7:
                case 0xAC8: case 0xAC9: case 0xACA: case 0xACB: case 0xACC: case 0xACD: case 0xACE: case 0xACF:
                case 0xAD0: case 0xAD1: case 0xAD2: case 0xAD3: case 0xAD4: case 0xAD5: case 0xAD6: case 0xAD7:
                case 0xAD8: case 0xAD9: case 0xADA: case 0xADB: case 0xADC: case 0xADD: case 0xADE: case 0xADF:
                case 0xAE0: case 0xAE1: case 0xAE2: case 0xAE3: case 0xAE4: case 0xAE5: case 0xAE6: case 0xAE7:
                case 0xAE8: case 0xAE9: case 0xAEA: case 0xAEB: case 0xAEC: case 0xAED: case 0xAEE: case 0xAEF:
                case 0xAF0: case 0xAF1: case 0xAF2: case 0xAF3: case 0xAF4: case 0xAF5: case 0xAF6: case 0xAF7:
                case 0xAF8: case 0xAF9: case 0xAFA: case 0xAFB: case 0xAFC: case 0xAFD: case 0xAFE: case 0xAFF:
                    {
                        uint branchOffset = this.curInstruction & 0x00FFFFFF;
                        if (branchOffset >> 23 == 1) branchOffset |= 0xFF000000;

                        this.registers[15] += branchOffset << 2;

                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // BL implementation
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0xB00: case 0xB01: case 0xB02: case 0xB03: case 0xB04: case 0xB05: case 0xB06: case 0xB07:
                case 0xB08: case 0xB09: case 0xB0A: case 0xB0B: case 0xB0C: case 0xB0D: case 0xB0E: case 0xB0F:
                case 0xB10: case 0xB11: case 0xB12: case 0xB13: case 0xB14: case 0xB15: case 0xB16: case 0xB17:
                case 0xB18: case 0xB19: case 0xB1A: case 0xB1B: case 0xB1C: case 0xB1D: case 0xB1E: case 0xB1F:
                case 0xB20: case 0xB21: case 0xB22: case 0xB23: case 0xB24: case 0xB25: case 0xB26: case 0xB27:
                case 0xB28: case 0xB29: case 0xB2A: case 0xB2B: case 0xB2C: case 0xB2D: case 0xB2E: case 0xB2F:
                case 0xB30: case 0xB31: case 0xB32: case 0xB33: case 0xB34: case 0xB35: case 0xB36: case 0xB37:
                case 0xB38: case 0xB39: case 0xB3A: case 0xB3B: case 0xB3C: case 0xB3D: case 0xB3E: case 0xB3F:
                case 0xB40: case 0xB41: case 0xB42: case 0xB43: case 0xB44: case 0xB45: case 0xB46: case 0xB47:
                case 0xB48: case 0xB49: case 0xB4A: case 0xB4B: case 0xB4C: case 0xB4D: case 0xB4E: case 0xB4F:
                case 0xB50: case 0xB51: case 0xB52: case 0xB53: case 0xB54: case 0xB55: case 0xB56: case 0xB57:
                case 0xB58: case 0xB59: case 0xB5A: case 0xB5B: case 0xB5C: case 0xB5D: case 0xB5E: case 0xB5F:
                case 0xB60: case 0xB61: case 0xB62: case 0xB63: case 0xB64: case 0xB65: case 0xB66: case 0xB67:
                case 0xB68: case 0xB69: case 0xB6A: case 0xB6B: case 0xB6C: case 0xB6D: case 0xB6E: case 0xB6F:
                case 0xB70: case 0xB71: case 0xB72: case 0xB73: case 0xB74: case 0xB75: case 0xB76: case 0xB77:
                case 0xB78: case 0xB79: case 0xB7A: case 0xB7B: case 0xB7C: case 0xB7D: case 0xB7E: case 0xB7F:
                case 0xB80: case 0xB81: case 0xB82: case 0xB83: case 0xB84: case 0xB85: case 0xB86: case 0xB87:
                case 0xB88: case 0xB89: case 0xB8A: case 0xB8B: case 0xB8C: case 0xB8D: case 0xB8E: case 0xB8F:
                case 0xB90: case 0xB91: case 0xB92: case 0xB93: case 0xB94: case 0xB95: case 0xB96: case 0xB97:
                case 0xB98: case 0xB99: case 0xB9A: case 0xB9B: case 0xB9C: case 0xB9D: case 0xB9E: case 0xB9F:
                case 0xBA0: case 0xBA1: case 0xBA2: case 0xBA3: case 0xBA4: case 0xBA5: case 0xBA6: case 0xBA7:
                case 0xBA8: case 0xBA9: case 0xBAA: case 0xBAB: case 0xBAC: case 0xBAD: case 0xBAE: case 0xBAF:
                case 0xBB0: case 0xBB1: case 0xBB2: case 0xBB3: case 0xBB4: case 0xBB5: case 0xBB6: case 0xBB7:
                case 0xBB8: case 0xBB9: case 0xBBA: case 0xBBB: case 0xBBC: case 0xBBD: case 0xBBE: case 0xBBF:
                case 0xBC0: case 0xBC1: case 0xBC2: case 0xBC3: case 0xBC4: case 0xBC5: case 0xBC6: case 0xBC7:
                case 0xBC8: case 0xBC9: case 0xBCA: case 0xBCB: case 0xBCC: case 0xBCD: case 0xBCE: case 0xBCF:
                case 0xBD0: case 0xBD1: case 0xBD2: case 0xBD3: case 0xBD4: case 0xBD5: case 0xBD6: case 0xBD7:
                case 0xBD8: case 0xBD9: case 0xBDA: case 0xBDB: case 0xBDC: case 0xBDD: case 0xBDE: case 0xBDF:
                case 0xBE0: case 0xBE1: case 0xBE2: case 0xBE3: case 0xBE4: case 0xBE5: case 0xBE6: case 0xBE7:
                case 0xBE8: case 0xBE9: case 0xBEA: case 0xBEB: case 0xBEC: case 0xBED: case 0xBEE: case 0xBEF:
                case 0xBF0: case 0xBF1: case 0xBF2: case 0xBF3: case 0xBF4: case 0xBF5: case 0xBF6: case 0xBF7:
                case 0xBF8: case 0xBF9: case 0xBFA: case 0xBFB: case 0xBFC: case 0xBFD: case 0xBFE: case 0xBFF:
                    {
                        uint branchOffset = this.curInstruction & 0x00FFFFFF;
                        if (branchOffset >> 23 == 1) branchOffset |= 0xFF000000;

                        this.registers[14] = this.registers[15] - 4U;
                        this.registers[15] += branchOffset << 2;

                        this.FlushQueue();
                    }
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //
                // SWI implementation
                //
                ////////////////////////////////////////////////////////////////////////////////////////////
                case 0xF00: case 0xF01: case 0xF02: case 0xF03: case 0xF04: case 0xF05: case 0xF06: case 0xF07:
                case 0xF08: case 0xF09: case 0xF0A: case 0xF0B: case 0xF0C: case 0xF0D: case 0xF0E: case 0xF0F:
                case 0xF10: case 0xF11: case 0xF12: case 0xF13: case 0xF14: case 0xF15: case 0xF16: case 0xF17:
                case 0xF18: case 0xF19: case 0xF1A: case 0xF1B: case 0xF1C: case 0xF1D: case 0xF1E: case 0xF1F:
                case 0xF20: case 0xF21: case 0xF22: case 0xF23: case 0xF24: case 0xF25: case 0xF26: case 0xF27:
                case 0xF28: case 0xF29: case 0xF2A: case 0xF2B: case 0xF2C: case 0xF2D: case 0xF2E: case 0xF2F:
                case 0xF30: case 0xF31: case 0xF32: case 0xF33: case 0xF34: case 0xF35: case 0xF36: case 0xF37:
                case 0xF38: case 0xF39: case 0xF3A: case 0xF3B: case 0xF3C: case 0xF3D: case 0xF3E: case 0xF3F:
                case 0xF40: case 0xF41: case 0xF42: case 0xF43: case 0xF44: case 0xF45: case 0xF46: case 0xF47:
                case 0xF48: case 0xF49: case 0xF4A: case 0xF4B: case 0xF4C: case 0xF4D: case 0xF4E: case 0xF4F:
                case 0xF50: case 0xF51: case 0xF52: case 0xF53: case 0xF54: case 0xF55: case 0xF56: case 0xF57:
                case 0xF58: case 0xF59: case 0xF5A: case 0xF5B: case 0xF5C: case 0xF5D: case 0xF5E: case 0xF5F:
                case 0xF60: case 0xF61: case 0xF62: case 0xF63: case 0xF64: case 0xF65: case 0xF66: case 0xF67:
                case 0xF68: case 0xF69: case 0xF6A: case 0xF6B: case 0xF6C: case 0xF6D: case 0xF6E: case 0xF6F:
                case 0xF70: case 0xF71: case 0xF72: case 0xF73: case 0xF74: case 0xF75: case 0xF76: case 0xF77:
                case 0xF78: case 0xF79: case 0xF7A: case 0xF7B: case 0xF7C: case 0xF7D: case 0xF7E: case 0xF7F:
                case 0xF80: case 0xF81: case 0xF82: case 0xF83: case 0xF84: case 0xF85: case 0xF86: case 0xF87:
                case 0xF88: case 0xF89: case 0xF8A: case 0xF8B: case 0xF8C: case 0xF8D: case 0xF8E: case 0xF8F:
                case 0xF90: case 0xF91: case 0xF92: case 0xF93: case 0xF94: case 0xF95: case 0xF96: case 0xF97:
                case 0xF98: case 0xF99: case 0xF9A: case 0xF9B: case 0xF9C: case 0xF9D: case 0xF9E: case 0xF9F:
                case 0xFA0: case 0xFA1: case 0xFA2: case 0xFA3: case 0xFA4: case 0xFA5: case 0xFA6: case 0xFA7:
                case 0xFA8: case 0xFA9: case 0xFAA: case 0xFAB: case 0xFAC: case 0xFAD: case 0xFAE: case 0xFAF:
                case 0xFB0: case 0xFB1: case 0xFB2: case 0xFB3: case 0xFB4: case 0xFB5: case 0xFB6: case 0xFB7:
                case 0xFB8: case 0xFB9: case 0xFBA: case 0xFBB: case 0xFBC: case 0xFBD: case 0xFBE: case 0xFBF:
                case 0xFC0: case 0xFC1: case 0xFC2: case 0xFC3: case 0xFC4: case 0xFC5: case 0xFC6: case 0xFC7:
                case 0xFC8: case 0xFC9: case 0xFCA: case 0xFCB: case 0xFCC: case 0xFCD: case 0xFCE: case 0xFCF:
                case 0xFD0: case 0xFD1: case 0xFD2: case 0xFD3: case 0xFD4: case 0xFD5: case 0xFD6: case 0xFD7:
                case 0xFD8: case 0xFD9: case 0xFDA: case 0xFDB: case 0xFDC: case 0xFDD: case 0xFDE: case 0xFDF:
                case 0xFE0: case 0xFE1: case 0xFE2: case 0xFE3: case 0xFE4: case 0xFE5: case 0xFE6: case 0xFE7:
                case 0xFE8: case 0xFE9: case 0xFEA: case 0xFEB: case 0xFEC: case 0xFED: case 0xFEE: case 0xFEF:
                case 0xFF0: case 0xFF1: case 0xFF2: case 0xFF3: case 0xFF4: case 0xFF5: case 0xFF6: case 0xFF7:
                case 0xFF8: case 0xFF9: case 0xFFA: case 0xFFB: case 0xFFC: case 0xFFD: case 0xFFE: case 0xFFF:
                    this.registers[15] -= 4U;
                    this.parent.EnterException(Arm7Processor.SVC, 0x8, false, false);
                    break;

                default:
                    this.NormalOps[(curInstruction >> 25) & 0x7]();
                    break;
            }
        }

        #region Barrel Shifter
        private const uint SHIFT_LSL = 0;
        private const uint SHIFT_LSR = 1;
        private const uint SHIFT_ASR = 2;
        private const uint SHIFT_ROR = 3;

        private uint BarrelShifter(uint shifterOperand)
        {
            uint type = (shifterOperand >> 5) & 0x3;

            bool registerShift = (shifterOperand & (1 << 4)) == (1 << 4);

            uint rm = registers[shifterOperand & 0xF];

            int amount;
            if (registerShift)
            {
                uint rs = (shifterOperand >> 8) & 0xF;
                if (rs == 15)
                {
                    amount = (int)((registers[rs] + 0x4) & 0xFF);
                }
                else
                {
                    amount = (int)(registers[rs] & 0xFF);
                }

                if ((shifterOperand & 0xF) == 15)
                {
                    rm += 4;
                }
            }
            else
            {
                amount = (int)((shifterOperand >> 7) & 0x1F);
            }

            if (registerShift)
            {
                if (amount == 0)
                {
                    this.shifterCarry = this.carry;
                    return rm;
                }

                switch (type)
                {
                    case SHIFT_LSL:
                        if (amount < 32)
                        {
                            this.shifterCarry = (rm >> (32 - amount)) & 1;
                            return rm << amount;
                        }
                        else if (amount == 32)
                        {
                            this.shifterCarry = rm & 1;
                            return 0;
                        }
                        else
                        {
                            this.shifterCarry = 0;
                            return 0;
                        }

                    case SHIFT_LSR:
                        if (amount < 32)
                        {
                            this.shifterCarry = (rm >> (amount - 1)) & 1;
                            return rm >> amount;
                        }
                        else if (amount == 32)
                        {
                            this.shifterCarry = (rm >> 31) & 1;
                            return 0;
                        }
                        else
                        {
                            this.shifterCarry = 0;
                            return 0;
                        }

                    case SHIFT_ASR:
                        if (amount >= 32)
                        {
                            if ((rm & (1 << 31)) == 0)
                            {
                                this.shifterCarry = 0;
                                return 0;
                            }
                            else
                            {
                                this.shifterCarry = 1;
                                return 0xFFFFFFFF;
                            }
                        }
                        else
                        {
                            this.shifterCarry = (rm >> (amount - 1)) & 1;
                            return (uint)(((int)rm) >> amount);
                        }

                    case SHIFT_ROR:
                        if ((amount & 0x1F) == 0)
                        {
                            this.shifterCarry = (rm >> 31) & 1;
                            return rm;
                        }
                        else
                        {
                            amount &= 0x1F;
                            this.shifterCarry = (rm >> amount) & 1;
                            return (rm >> amount) | (rm << (32 - amount));
                        }
                }
            }
            else
            {
                switch (type)
                {
                    case SHIFT_LSL:
                        if (amount == 0)
                        {
                            this.shifterCarry = this.carry;
                            return rm;
                        }
                        else
                        {
                            this.shifterCarry = (rm >> (32 - amount)) & 1;
                            return rm << amount;
                        }

                    case SHIFT_LSR:
                        if (amount == 0)
                        {
                            this.shifterCarry = (rm >> 31) & 1;
                            return 0;
                        }
                        else
                        {
                            this.shifterCarry = (rm >> (amount - 1)) & 1;
                            return rm >> amount;
                        }

                    case SHIFT_ASR:
                        if (amount == 0)
                        {
                            if ((rm & (1 << 31)) == 0)
                            {
                                this.shifterCarry = 0;
                                return 0;
                            }
                            else
                            {
                                this.shifterCarry = 1;
                                return 0xFFFFFFFF;
                            }
                        }
                        else
                        {
                            this.shifterCarry = (rm >> (amount - 1)) & 1;
                            return (uint)(((int)rm) >> amount);
                        }

                    case SHIFT_ROR:
                        if (amount == 0)
                        {
                            // Actually an RRX
                            this.shifterCarry = rm & 1;
                            return (this.carry << 31) | (rm >> 1);
                        }
                        else
                        {
                            this.shifterCarry = (rm >> (amount - 1)) & 1;
                            return (rm >> amount) | (rm << (32 - amount));
                        }
                }
            }

            // Should never happen...
            throw new Exception("Barrel Shifter has messed up.");
        }
        #endregion

        #region Flag helpers
        public void OverflowCarryAdd(uint a, uint b, uint r)
        {
            overflow = ((a & b & ~r) | (~a & ~b & r)) >> 31;
            carry = ((a & b) | (a & ~r) | (b & ~r)) >> 31;
        }

        public void OverflowCarrySub(uint a, uint b, uint r)
        {
            overflow = ((a & ~b & ~r) | (~a & b & r)) >> 31;
            carry = ((a & ~b) | (a & ~r) | (~b & ~r)) >> 31;
        }
        #endregion
        #region Opcodes
        private void DoDataProcessing(uint shifterOperand)
        {
            uint rn = (this.curInstruction >> 16) & 0xF;
            uint rd = (this.curInstruction >> 12) & 0xF;
            uint alu;

            bool registerShift = (this.curInstruction & (1 << 4)) == (1 << 4);
            if (rn == 15 && ((this.curInstruction >> 25) & 0x7) == 0 && registerShift)
            {
                rn = registers[rn] + 4;
            }
            else
            {
                rn = registers[rn];
            }

            uint opcode = (this.curInstruction >> 21) & 0xF;

            if (((this.curInstruction >> 20) & 1) == 1)
            {
                // Set flag bit set
                switch (opcode)
                {
                    case OP_ADC:
                        registers[rd] = rn + shifterOperand + carry;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);
                        break;

                    case OP_ADD:
                        registers[rd] = rn + shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        this.OverflowCarryAdd(rn, shifterOperand, registers[rd]);
                        break;

                    case OP_AND:
                        registers[rd] = rn & shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_BIC:
                        registers[rd] = rn & ~shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_CMN:
                        alu = rn + shifterOperand;

                        negative = alu >> 31;
                        zero = alu == 0 ? 1U : 0U;
                        this.OverflowCarryAdd(rn, shifterOperand, alu);
                        break;

                    case OP_CMP:
                        alu = rn - shifterOperand;

                        negative = alu >> 31;
                        zero = alu == 0 ? 1U : 0U;
                        this.OverflowCarrySub(rn, shifterOperand, alu);
                        break;

                    case OP_EOR:
                        registers[rd] = rn ^ shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_MOV:
                        registers[rd] = shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_MVN:
                        registers[rd] = ~shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_ORR:
                        registers[rd] = rn | shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_RSB:
                        registers[rd] = shifterOperand - rn;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        this.OverflowCarrySub(shifterOperand, rn, registers[rd]);
                        break;

                    case OP_RSC:
                        registers[rd] = shifterOperand - rn - (1U - carry);

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        this.OverflowCarrySub(shifterOperand, rn, registers[rd]);
                        break;

                    case OP_SBC:
                        registers[rd] = rn - shifterOperand - (1U - carry);

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        this.OverflowCarrySub(rn, shifterOperand, registers[rd]);
                        break;

                    case OP_SUB:
                        registers[rd] = rn - shifterOperand;

                        negative = registers[rd] >> 31;
                        zero = registers[rd] == 0 ? 1U : 0U;
                        this.OverflowCarrySub(rn, shifterOperand, registers[rd]);
                        break;

                    case OP_TEQ:
                        alu = rn ^ shifterOperand;

                        negative = alu >> 31;
                        zero = alu == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;

                    case OP_TST:
                        alu = rn & shifterOperand;

                        negative = alu >> 31;
                        zero = alu == 0 ? 1U : 0U;
                        carry = this.shifterCarry;
                        break;
                }

                if (rd == 15)
                {
                    // Prevent writing if no SPSR exists (this will be true for USER or SYSTEM mode)
                    if (this.parent.SPSRExists) this.parent.WriteCpsr(this.parent.SPSR);
                    this.UnpackFlags();

                    // Check for branch back to Thumb Mode
                    if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                    {
                        this.thumbMode = true;
                        return;
                    }

                    // Otherwise, flush the instruction queue
                    this.FlushQueue();
                }
            }
            else
            {
                // Set flag bit not set
                switch (opcode)
                {
                    case OP_ADC: registers[rd] = rn + shifterOperand + carry; break;
                    case OP_ADD: registers[rd] = rn + shifterOperand; break;
                    case OP_AND: registers[rd] = rn & shifterOperand; break;
                    case OP_BIC: registers[rd] = rn & ~shifterOperand; break;
                    case OP_EOR: registers[rd] = rn ^ shifterOperand; break;
                    case OP_MOV: registers[rd] = shifterOperand; break;
                    case OP_MVN: registers[rd] = ~shifterOperand; break;
                    case OP_ORR: registers[rd] = rn | shifterOperand; break;
                    case OP_RSB: registers[rd] = shifterOperand - rn; break;
                    case OP_RSC: registers[rd] = shifterOperand - rn - (1U - carry); break;
                    case OP_SBC: registers[rd] = rn - shifterOperand - (1U - carry); break;
                    case OP_SUB: registers[rd] = rn - shifterOperand; break;

                    case OP_CMN:
                        // MSR SPSR, shifterOperand
                        if ((this.curInstruction & (1 << 16)) == 1 << 16 && this.parent.SPSRExists)
                        {
                            this.parent.SPSR &= 0xFFFFFF00;
                            this.parent.SPSR |= shifterOperand & 0x000000FF;
                        }
                        if ((this.curInstruction & (1 << 17)) == 1 << 17 && this.parent.SPSRExists)
                        {
                            this.parent.SPSR &= 0xFFFF00FF;
                            this.parent.SPSR |= shifterOperand & 0x0000FF00;
                        }
                        if ((this.curInstruction & (1 << 18)) == 1 << 18 && this.parent.SPSRExists)
                        {
                            this.parent.SPSR &= 0xFF00FFFF;
                            this.parent.SPSR |= shifterOperand & 0x00FF0000;
                        }
                        if ((this.curInstruction & (1 << 19)) == 1 << 19 && this.parent.SPSRExists)
                        {
                            this.parent.SPSR &= 0x00FFFFFF;
                            this.parent.SPSR |= shifterOperand & 0xFF000000;
                        }

                        // Queue will be flushed since rd == 15, so adjust the PC
                        registers[15] -= 4;
                        break;

                    case OP_CMP:
                        // MRS rd, SPSR
                        if (this.parent.SPSRExists) registers[rd] = this.parent.SPSR;
                        break;

                    case OP_TEQ:
                        if (((this.curInstruction >> 4) & 0xf) == 1)
                        {
                            // BX
                            uint rm = this.curInstruction & 0xf;

                            this.PackFlags();

                            this.parent.CPSR &= ~Arm7Processor.T_MASK;
                            this.parent.CPSR |= (registers[rm] & 1) << Arm7Processor.T_BIT;

                            registers[15] = registers[rm] & (~1U);

                            this.UnpackFlags();

                            // Check for branch back to Thumb Mode
                            if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                            {
                                this.thumbMode = true;
                                return;
                            }

                            // Queue will be flushed later because rd == 15
                        }
                        else if (((this.curInstruction >> 4) & 0xf) == 0)
                        {
                            // MSR CPSR, shifterOperand
                            bool userMode = (this.parent.CPSR & 0x1F) == Arm7Processor.USR;

                            this.PackFlags();

                            uint tmpCPSR = this.parent.CPSR;

                            if ((this.curInstruction & (1 << 16)) == 1 << 16 && !userMode)
                            {
                                tmpCPSR &= 0xFFFFFF00;
                                tmpCPSR |= shifterOperand & 0x000000FF;
                            }
                            if ((this.curInstruction & (1 << 17)) == 1 << 17 && !userMode)
                            {
                                tmpCPSR &= 0xFFFF00FF;
                                tmpCPSR |= shifterOperand & 0x0000FF00;
                            }
                            if ((this.curInstruction & (1 << 18)) == 1 << 18 && !userMode)
                            {
                                tmpCPSR &= 0xFF00FFFF;
                                tmpCPSR |= shifterOperand & 0x00FF0000;
                            }
                            if ((this.curInstruction & (1 << 19)) == 1 << 19)
                            {
                                tmpCPSR &= 0x00FFFFFF;
                                tmpCPSR |= shifterOperand & 0xFF000000;
                            }

                            this.parent.WriteCpsr(tmpCPSR);

                            this.UnpackFlags();

                            // Check for branch back to Thumb Mode
                            if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                            {
                                this.thumbMode = true;
                                return;
                            }

                            // Queue will be flushed since rd == 15, so adjust the PC
                            registers[15] -= 4;
                        }
                        break;

                    case OP_TST:
                        // MRS rd, CPSR
                        this.PackFlags();
                        registers[rd] = this.parent.CPSR;
                        break;
                }

                if (rd == 15)
                {
                    // Flush the queue
                    this.FlushQueue();
                }
            }
        }

        private void DataProcessing()
        {
            // Special instruction
            switch ((this.curInstruction >> 4) & 0xF)
            {
                case 0x9:
                    // Multiply or swap instructions
                    this.MultiplyOrSwap();
                    return;
                case 0xB:
                    // Load/Store Unsigned halfword
                    this.LoadStoreHalfword();
                    return;
                case 0xD:
                    // Load/Store Signed byte
                    this.LoadStoreHalfword();
                    return;
                case 0xF:
                    // Load/Store Signed halfword
                    this.LoadStoreHalfword();
                    return;
            }

            this.DoDataProcessing(this.BarrelShifter(this.curInstruction));
        }

        private void DataProcessingImmed()
        {
            uint immed = this.curInstruction & 0xFF;
            int rotateAmount = (int)(((this.curInstruction >> 8) & 0xF) * 2);

            immed = (immed >> rotateAmount) | (immed << (32 - rotateAmount));

            if (rotateAmount == 0)
            {
                this.shifterCarry = this.carry;
            }
            else
            {
                this.shifterCarry = (immed >> 31) & 1;
            }

            this.DoDataProcessing(immed);
        }

        private void LoadStore(uint offset)
        {
            uint rn = (this.curInstruction >> 16) & 0xF;
            uint rd = (this.curInstruction >> 12) & 0xF;

            uint address = registers[rn];

            bool preIndexed = (this.curInstruction & (1 << 24)) == 1 << 24;
            bool byteTransfer = (this.curInstruction & (1 << 22)) == 1 << 22;
            bool writeback = (this.curInstruction & (1 << 21)) == 1 << 21;

            // Add or subtract offset
            if ((this.curInstruction & (1 << 23)) != 1 << 23) offset = (uint)-offset;

            if (preIndexed)
            {
                address += offset;

                if (writeback)
                {
                    registers[rn] = address;
                }
            }

            if ((this.curInstruction & (1 << 20)) == 1 << 20)
            {
                // Load
                if (byteTransfer)
                {
                    registers[rd] = this.memory.ReadU8(address);
                }
                else
                {
                    registers[rd] = this.memory.ReadU32(address);
                }

                // ARM9 fix here

                if (rd == 15)
                {
                    registers[rd] &= ~3U;
                    this.FlushQueue();
                }

                if (!preIndexed)
                {
                    if (rn != rd)
                        registers[rn] = address + offset;
                }
            }
            else
            {
                // Store
                uint amount = registers[rd];
                if (rd == 15) amount += 4;

                if (byteTransfer)
                {
                    this.memory.WriteU8(address, (byte)(amount & 0xFF));
                }
                else
                {
                    this.memory.WriteU32(address, amount);
                }

                if (!preIndexed)
                {
                    registers[rn] = address + offset;
                }
            }
        }

        private void LoadStoreImmediate()
        {
            this.LoadStore(this.curInstruction & 0xFFF);
        }

        private void LoadStoreRegister()
        {
            // The barrel shifter expects a 0 in bit 4 for immediate shifts, this is implicit in
            // the meaning of the instruction, so it is fine
            this.LoadStore(this.BarrelShifter(this.curInstruction));
        }

        private void LoadStoreMultiple()
        {
            uint rn = (this.curInstruction >> 16) & 0xF;

            this.PackFlags();
            uint curCpsr = this.parent.CPSR;

            bool preIncrement = (this.curInstruction & (1 << 24)) != 0;
            bool up = (this.curInstruction & (1 << 23)) != 0;
            bool writeback = (this.curInstruction & (1 << 21)) != 0;

            uint address;
            uint bitsSet = 0;
            for (int i = 0; i < 16; i++) if (((this.curInstruction >> i) & 1) != 0) bitsSet++;

            if (preIncrement)
            {
                if (up)
                {
                    // Increment before
                    address = this.registers[rn] + 4;
                    if (writeback) this.registers[rn] += bitsSet * 4;
                }
                else
                {
                    // Decrement before
                    address = this.registers[rn] - (bitsSet * 4);
                    if (writeback) this.registers[rn] -= bitsSet * 4;
                }
            }
            else
            {
                if (up)
                {
                    // Increment after
                    address = this.registers[rn];
                    if (writeback) this.registers[rn] += bitsSet * 4;
                }
                else
                {
                    // Decrement after
                    address = this.registers[rn] - (bitsSet * 4) + 4;
                    if (writeback) this.registers[rn] -= bitsSet * 4;
                }
            }

            if ((this.curInstruction & (1 << 20)) != 0)
            {
                if ((this.curInstruction & (1 << 22)) != 0 && ((this.curInstruction >> 15) & 1) == 0)
                {
                    // Switch to user mode temporarily
                    this.parent.WriteCpsr((curCpsr & ~0x1FU) | Arm7Processor.USR);
                }

                // Load multiple
                for (int i = 0; i < 15; i++)
                {
                    if (((this.curInstruction >> i) & 1) != 1) continue;
                    this.registers[i] = this.memory.ReadU32Aligned(address & (~0x3U));
                    address += 4;
                }

                if (((this.curInstruction >> 15) & 1) == 1)
                {
                    // Arm9 fix here

                    this.registers[15] = this.memory.ReadU32Aligned(address & (~0x3U));

                    if ((this.curInstruction & (1 << 22)) != 0)
                    {
                        // Load the CPSR from the SPSR
                        if (this.parent.SPSRExists)
                        {
                            this.parent.WriteCpsr(this.parent.SPSR);
                            this.UnpackFlags();

                            // Check for branch back to Thumb Mode
                            if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                            {
                                this.thumbMode = true;
                                this.registers[15] &= ~0x1U;
                                return;
                            }
                        }
                    }

                    this.registers[15] &= ~0x3U;
                    this.FlushQueue();
                }
                else
                {
                    if ((this.curInstruction & (1 << 22)) != 0)
                    {
                        // Switch back to the correct mode
                        this.parent.WriteCpsr(curCpsr);
                        this.UnpackFlags();

                        if ((this.parent.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                        {
                            this.thumbMode = true;
                            return;
                        }
                    }
                }
            }
            else
            {
                if ((this.curInstruction & (1 << 22)) != 0)
                {
                    // Switch to user mode temporarily
                    this.parent.WriteCpsr((curCpsr & ~0x1FU) | Arm7Processor.USR);
                }

/*                if (((this.curInstruction >> (int)rn) & 1) != 0 && writeback &&
                    (this.curInstruction & ~(0xFFFFFFFF << (int)rn)) == 0)
                {
                    // If the lowest register is also the writeback, we use the original value
                    // Does anybody do this????
                    throw new Exception("Unhandled STM state");
                }
                else*/
                {
                    // Store multiple
                    for (int i = 0; i < 15; i++)
                    {
                        if (((this.curInstruction >> i) & 1) == 0) continue;
                        this.memory.WriteU32(address, this.registers[i]);
                        address += 4;
                    }

                    if (((this.curInstruction >> 15) & 1) != 0)
                    {
                        this.memory.WriteU32(address, this.registers[15] + 4U);
                    }
                }

                if ((this.curInstruction & (1 << 22)) != 0)
                {
                    // Switch back to the correct mode
                    this.parent.WriteCpsr(curCpsr);
                    this.UnpackFlags();
                }
            }
        }

        private void Branch()
        {
            if ((this.curInstruction & (1 << 24)) != 0)
            {
                this.registers[14] = (this.registers[15] - 4U) & ~3U;
            }

            uint branchOffset = this.curInstruction & 0x00FFFFFF;
            if (branchOffset >> 23 == 1) branchOffset |= 0xFF000000;

            this.registers[15] += branchOffset << 2;

            this.FlushQueue();
        }

        private void CoprocessorLoadStore()
        {
            throw new Exception("Unhandled opcode - coproc load/store");
        }

        private void SoftwareInterrupt()
        {
            // Adjust PC for prefetch
            this.registers[15] -= 4U;
            this.parent.EnterException(Arm7Processor.SVC, 0x8, false, false);
        }

        private void MultiplyOrSwap()
        {
            if ((this.curInstruction & (1 << 24)) == 1 << 24)
            {
                // Swap instruction
                uint rn = (this.curInstruction >> 16) & 0xF;
                uint rd = (this.curInstruction >> 12) & 0xF;
                uint rm = this.curInstruction & 0xF;

                if ((this.curInstruction & (1 << 22)) != 0)
                {
                    // SWPB
                    byte tmp = this.memory.ReadU8(registers[rn]);
                    this.memory.WriteU8(registers[rn], (byte)(registers[rm] & 0xFF));
                    registers[rd] = tmp;
                }
                else
                {
                    // SWP
                    uint tmp = this.memory.ReadU32(registers[rn]);
                    this.memory.WriteU32(registers[rn], registers[rm]);
                    registers[rd] = tmp;
                }
            }
            else
            {
                // Multiply instruction
                switch ((this.curInstruction >> 21) & 0x7)
                {
                    case 0:
                    case 1:
                        {
                            // Multiply/Multiply + Accumulate
                            uint rd = (this.curInstruction >> 16) & 0xF;
                            uint rn = registers[(this.curInstruction >> 12) & 0xF];
                            uint rs = (this.curInstruction >> 8) & 0xF;
                            uint rm = this.curInstruction & 0xF;

                            int cycles = 4;
                            // Multiply cycle calculations
                            if ((registers[rs] & 0xFFFFFF00) == 0 || (registers[rs] & 0xFFFFFF00) == 0xFFFFFF00)
                            {
                                cycles = 1;
                            }
                            else if ((registers[rs] & 0xFFFF0000) == 0 || (registers[rs] & 0xFFFF0000) == 0xFFFF0000)
                            {
                                cycles = 2;
                            }
                            else if ((registers[rs] & 0xFF000000) == 0 || (registers[rs] & 0xFF000000) == 0xFF000000)
                            {
                                cycles = 3;
                            }

                            registers[rd] = registers[rs] * registers[rm];
                            this.parent.Cycles -= cycles;

                            if ((this.curInstruction & (1 << 21)) == 1 << 21)
                            {
                                registers[rd] += rn;
                                this.parent.Cycles -= 1;
                            }

                            if ((this.curInstruction & (1 << 20)) == 1 << 20)
                            {
                                negative = registers[rd] >> 31;
                                zero = registers[rd] == 0 ? 1U : 0U;
                            }
                            break;
                        }

                    case 2:
                    case 3:
                        throw new Exception("Invalid multiply");

                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        {
                            // Multiply/Signed Multiply Long
                            uint rdhi = (this.curInstruction >> 16) & 0xF;
                            uint rdlo = (this.curInstruction >> 12) & 0xF;
                            uint rs = (this.curInstruction >> 8) & 0xF;
                            uint rm = this.curInstruction & 0xF;

                            int cycles = 5;
                            // Multiply cycle calculations
                            if ((registers[rs] & 0xFFFFFF00) == 0 || (registers[rs] & 0xFFFFFF00) == 0xFFFFFF00)
                            {
                                cycles = 2;
                            }
                            else if ((registers[rs] & 0xFFFF0000) == 0 || (registers[rs] & 0xFFFF0000) == 0xFFFF0000)
                            {
                                cycles = 3;
                            }
                            else if ((registers[rs] & 0xFF000000) == 0 || (registers[rs] & 0xFF000000) == 0xFF000000)
                            {
                                cycles = 4;
                            }

                            this.parent.Cycles -= cycles;

                            switch ((this.curInstruction >> 21) & 0x3)
                            {
                                case 0:
                                    {
                                        // UMULL
                                        ulong result = ((ulong)registers[rm]) * registers[rs];
                                        registers[rdhi] = (uint)(result >> 32);
                                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);
                                        break;
                                    }
                                case 1:
                                    {
                                        // UMLAL
                                        ulong accum = (((ulong)registers[rdhi]) << 32) | registers[rdlo];
                                        ulong result = ((ulong)registers[rm]) * registers[rs];
                                        result += accum;
                                        registers[rdhi] = (uint)(result >> 32);
                                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);
                                        break;
                                    }
                                case 2:
                                    {
                                        // SMULL
                                        long result = ((long)((int)registers[rm])) * ((long)((int)registers[rs]));
                                        registers[rdhi] = (uint)(result >> 32);
                                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);
                                        break;
                                    }
                                case 3:
                                    {
                                        // SMLAL
                                        long accum = (((long)((int)registers[rdhi])) << 32) | registers[rdlo];
                                        long result = ((long)((int)registers[rm])) * ((long)((int)registers[rs]));
                                        result += accum;
                                        registers[rdhi] = (uint)(result >> 32);
                                        registers[rdlo] = (uint)(result & 0xFFFFFFFF);
                                        break;
                                    }
                            }

                            if ((this.curInstruction & (1 << 20)) == 1 << 20)
                            {
                                negative = registers[rdhi] >> 31;
                                zero = (registers[rdhi] == 0 && registers[rdlo] == 0) ? 1U : 0U;
                            }
                            break;
                        }
                }
            }
        }

        private void LoadStoreHalfword()
        {
            uint rn = (this.curInstruction >> 16) & 0xF;
            uint rd = (this.curInstruction >> 12) & 0xF;

            uint address = registers[rn];

            bool preIndexed = (this.curInstruction & (1 << 24)) != 0;
            bool byteTransfer = (this.curInstruction & (1 << 5)) == 0;
            bool signedTransfer = (this.curInstruction & (1 << 6)) != 0;
            bool writeback = (this.curInstruction & (1 << 21)) != 0;

            uint offset;
            if ((this.curInstruction & (1 << 22)) != 0)
            {
                // Immediate offset
                offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
            }
            else
            {
                // Register offset
                offset = this.registers[this.curInstruction & 0xF];
            }

            // Add or subtract offset
            if ((this.curInstruction & (1 << 23)) == 0) offset = (uint)-offset;

            if (preIndexed)
            {
                address += offset;

                if (writeback)
                {
                    registers[rn] = address;
                }
            }

            if ((this.curInstruction & (1 << 20)) != 0)
            {
                // Load
                if (byteTransfer)
                {
                    if (signedTransfer)
                    {
                        registers[rd] = this.memory.ReadU8(address);
                        if ((registers[rd] & 0x80) != 0)
                        {
                            registers[rd] |= 0xFFFFFF00;
                        }
                    }
                    else
                    {
                        registers[rd] = this.memory.ReadU8(address);
                    }
                }
                else
                {
                    if (signedTransfer)
                    {
                        registers[rd] = this.memory.ReadU16(address);
                        if ((registers[rd] & 0x8000) != 0)
                        {
                            registers[rd] |= 0xFFFF0000;
                        }
                    }
                    else
                    {
                        registers[rd] = this.memory.ReadU16(address);
                    }
                }

                if (rd == 15)
                {
                    registers[rd] &= ~3U;
                    this.FlushQueue();
                }

                if (!preIndexed)
                {
                    if (rn != rd)
                        registers[rn] = address + offset;
                }
            }
            else
            {
                // Store
                if (byteTransfer)
                {
                    this.memory.WriteU8(address, (byte)(registers[rd] & 0xFF));
                }
                else
                {
                    this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
                }

                if (!preIndexed)
                {
                    registers[rn] = address + offset;
                }
            }
        }
        #endregion Opcodes

        private void PackFlags()
        {
            this.parent.CPSR &= 0x0FFFFFFF;
            this.parent.CPSR |= this.negative << Arm7Processor.N_BIT;
            this.parent.CPSR |= this.zero << Arm7Processor.Z_BIT;
            this.parent.CPSR |= this.carry << Arm7Processor.C_BIT;
            this.parent.CPSR |= this.overflow << Arm7Processor.V_BIT;
        }

        private void UnpackFlags()
        {
            this.negative = (this.parent.CPSR >> Arm7Processor.N_BIT) & 1;
            this.zero = (this.parent.CPSR >> Arm7Processor.Z_BIT) & 1;
            this.carry = (this.parent.CPSR >> Arm7Processor.C_BIT) & 1;
            this.overflow = (this.parent.CPSR >> Arm7Processor.V_BIT) & 1;
        }

        private void FlushQueue()
        {
            this.instructionQueue = this.memory.ReadU32(registers[15]);
            registers[15] += 4;
        }
    }
}