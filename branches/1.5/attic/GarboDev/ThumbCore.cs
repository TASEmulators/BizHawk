//#define ARM_DEBUG

namespace GarboDev
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ThumbCore
    {
        private const int COND_EQ = 0;	    // Z set
        private const int COND_NE = 1;	    // Z clear
        private const int COND_CS = 2;	    // C set
        private const int COND_CC = 3;	    // C clear
        private const int COND_MI = 4;	    // N set
        private const int COND_PL = 5;	    // N clear
        private const int COND_VS = 6;	    // V set
        private const int COND_VC = 7;	    // V clear
        private const int COND_HI = 8;	    // C set and Z clear
        private const int COND_LS = 9;	    // C clear or Z set
        private const int COND_GE = 10;	    // N equals V
        private const int COND_LT = 11;	    // N not equal to V
        private const int COND_GT = 12; 	// Z clear AND (N equals V)
        private const int COND_LE = 13; 	// Z set OR (N not equal to V)
        private const int COND_AL = 14; 	// Always
        private const int COND_NV = 15; 	// Never execute

        private const int OP_AND = 0x0;
        private const int OP_EOR = 0x1;
        private const int OP_LSL = 0x2;
        private const int OP_LSR = 0x3;
        private const int OP_ASR = 0x4;
        private const int OP_ADC = 0x5;
        private const int OP_SBC = 0x6;
        private const int OP_ROR = 0x7;
        private const int OP_TST = 0x8;
        private const int OP_NEG = 0x9;
        private const int OP_CMP = 0xA;
        private const int OP_CMN = 0xB;
        private const int OP_ORR = 0xC;
        private const int OP_MUL = 0xD;
        private const int OP_BIC = 0xE;
        private const int OP_MVN = 0xF;

        private Arm7Processor parent;
        private Memory memory;
        private uint[] registers;

        // CPU flags
        private uint zero, carry, negative, overflow;
        private ushort curInstruction, instructionQueue;

        private delegate void ExecuteInstruction();
        private ExecuteInstruction[] NormalOps = null;

        public ThumbCore(Arm7Processor parent, Memory memory)
        {
            this.parent = parent;
            this.memory = memory;
            this.registers = this.parent.Registers;

            this.NormalOps = new ExecuteInstruction[256]
                {
                    OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm,
                    OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm,
                    OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm,
                    OpAddRegReg, OpAddRegReg, OpSubRegReg, OpSubRegReg, OpAddRegImm, OpAddRegImm, OpSubRegImm, OpSubRegImm,
                    OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm,
                    OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm,
                    OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm,
                    OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm,
                    OpArith, OpArith, OpArith, OpArith, OpAddHi, OpCmpHi, OpMovHi, OpBx,
                    OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc,
                    OpStrReg, OpStrReg, OpStrhReg, OpStrhReg, OpStrbReg, OpStrbReg, OpLdrsbReg, OpLdrsbReg,
                    OpLdrReg, OpLdrReg, OpLdrhReg, OpLdrhReg, OpLdrbReg, OpLdrbReg, OpLdrshReg, OpLdrshReg,
                    OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm,
                    OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm,
                    OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm,
                    OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm,
                    OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm,
                    OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm,
                    OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp,
                    OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp,
                    OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, 
                    OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp,
                    OpSubSp, OpUnd, OpUnd, OpUnd, OpPush, OpPushLr, OpUnd, OpUnd,
                    OpUnd, OpUnd, OpUnd, OpUnd, OpPop, OpPopPc, OpUnd, OpUnd,
                    OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, 
                    OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia,
                    OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond,
                    OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpUnd, OpSwi,
                    OpB, OpB, OpB, OpB, OpB, OpB, OpB, OpB,
                    OpUnd, OpUnd, OpUnd, OpUnd, OpUnd, OpUnd, OpUnd, OpUnd,
                    OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, 
                    OpBl2, OpBl2, OpBl2, OpBl2, OpBl2, OpBl2, OpBl2, OpBl2
                };
        }

        public void BeginExecution()
        {
            this.FlushQueue();
        }

        public void Step()
        {
            this.UnpackFlags();

            this.curInstruction = this.instructionQueue;
            this.instructionQueue = this.memory.ReadU16(registers[15]);
            registers[15] += 2;

            // Execute the instruction
            this.NormalOps[this.curInstruction >> 8]();

            this.parent.Cycles -= this.memory.WaitCycles;

            if ((this.parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
            {
                if ((this.curInstruction >> 8) != 0xDF) this.parent.ReloadQueue();
            }

            this.PackFlags();
        }

        public void Execute()
        {
            this.UnpackFlags();

            while (this.parent.Cycles > 0)
            {
                this.curInstruction = this.instructionQueue;
                this.instructionQueue = this.memory.ReadU16(registers[15]);
                registers[15] += 2;

                // Execute the instruction
                this.NormalOps[this.curInstruction >> 8]();

                this.parent.Cycles -= this.memory.WaitCycles;

                if ((this.parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
                {
                    if ((this.curInstruction >> 8) != 0xDF) this.parent.ReloadQueue();
                    break;
                }

                // Check the current PC
#if ARM_DEBUG
                if (this.parent.Breakpoints.ContainsKey(registers[15] - 2U))
                {
                    this.parent.BreakpointHit = true;
                    break;
                }
#endif
            }

            this.PackFlags();
        }

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
        private void OpLslImm()
        {
            // 0x00 - 0x07
            // lsl rd, rm, #immed
            int rd = this.curInstruction & 0x7;
            int rm = (this.curInstruction >> 3) & 0x7;
            int immed = (this.curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                registers[rd] = registers[rm];
            } else
            {
                carry = (registers[rm] >> (32 - immed)) & 0x1;
                registers[rd] = registers[rm] << immed;
            }

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpLsrImm()
        {
            // 0x08 - 0x0F
            // lsr rd, rm, #immed
            int rd = this.curInstruction & 0x7;
            int rm = (this.curInstruction >> 3) & 0x7;
            int immed = (this.curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                carry = registers[rm] >> 31;
                registers[rd] = 0;
            }
            else
            {
                carry = (registers[rm] >> (immed - 1)) & 0x1;
                registers[rd] = registers[rm] >> immed;
            }

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAsrImm()
        {
            // asr rd, rm, #immed
            int rd = this.curInstruction & 0x7;
            int rm = (this.curInstruction >> 3) & 0x7;
            int immed = (this.curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                carry = registers[rm] >> 31;
                if (carry == 1) registers[rd] = 0xFFFFFFFF;
                else registers[rd] = 0;
            }
            else
            {
                carry = (registers[rm] >> (immed - 1)) & 0x1;
                registers[rd] = (uint)(((int)registers[rm]) >> immed);
            }

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAddRegReg()
        {
            // add rd, rn, rm
            int rd = this.curInstruction & 0x7;
            int rn = (this.curInstruction >> 3) & 0x7;
            int rm = (this.curInstruction >> 6) & 0x7;

            uint orn = registers[rn];
            uint orm = registers[rm];

            registers[rd] = orn + orm;

            this.OverflowCarryAdd(orn, orm, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubRegReg()
        {
            // sub rd, rn, rm
            int rd = this.curInstruction & 0x7;
            int rn = (this.curInstruction >> 3) & 0x7;
            int rm = (this.curInstruction >> 6) & 0x7;

            uint orn = registers[rn];
            uint orm = registers[rm];

            registers[rd] = orn - orm;

            this.OverflowCarrySub(orn, orm, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAddRegImm()
        {
            // add rd, rn, #immed
            int rd = this.curInstruction & 0x7;
            int rn = (this.curInstruction >> 3) & 0x7;
            uint immed = (uint)((this.curInstruction >> 6) & 0x7);

            uint orn = registers[rn];

            registers[rd] = orn + immed;

            this.OverflowCarryAdd(orn, immed, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubRegImm()
        {
            // sub rd, rn, #immed
            int rd = this.curInstruction & 0x7;
            int rn = (this.curInstruction >> 3) & 0x7;
            uint immed = (uint)((this.curInstruction >> 6) & 0x7);

            uint orn = registers[rn];

            registers[rd] = orn - immed;

            this.OverflowCarrySub(orn, immed, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpMovImm()
        {
            // mov rd, #immed
            int rd = (this.curInstruction >> 8) & 0x7;

            registers[rd] = (uint)(this.curInstruction & 0xFF);

            negative = 0;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpCmpImm()
        {
            // cmp rn, #immed
            int rn = (this.curInstruction >> 8) & 0x7;

            uint alu = registers[rn] - (uint)(this.curInstruction & 0xFF);

            this.OverflowCarrySub(registers[rn], (uint)(this.curInstruction & 0xFF), alu);
            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
        }

        private void OpAddImm()
        {
            // add rd, #immed
            int rd = (this.curInstruction >> 8) & 0x7;

            uint ord = registers[rd];

            registers[rd] += (uint)(this.curInstruction & 0xFF);

            this.OverflowCarryAdd(ord, (uint)(this.curInstruction & 0xFF), registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubImm()
        {
            // sub rd, #immed
            int rd = (this.curInstruction >> 8) & 0x7;

            uint ord = registers[rd];

            registers[rd] -= (uint)(this.curInstruction & 0xFF);

            this.OverflowCarrySub(ord, (uint)(this.curInstruction & 0xFF), registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpArith()
        {
            int rd = this.curInstruction & 0x7;
            uint rn = registers[(this.curInstruction >> 3) & 0x7];

            uint orig, alu;
            int shiftAmt;

            switch ((this.curInstruction >> 6) & 0xF)
            {
                case OP_ADC:
                    orig = registers[rd];
                    registers[rd] += rn + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(orig, rn, registers[rd]);
                    break;

                case OP_AND:
                    registers[rd] &= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_ASR:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        carry = (registers[rd] >> (shiftAmt - 1)) & 0x1;
                        registers[rd] = (uint)(((int)registers[rd]) >> shiftAmt);
                    }
                    else
                    {
                        carry = (registers[rd] >> 31) & 1;
                        if (carry == 1) registers[rd] = 0xFFFFFFFF;
                        else registers[rd] = 0;
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_BIC:
                    registers[rd] &= ~rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_CMN:
                    alu = registers[rd] + rn;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarryAdd(registers[rd], rn, alu);
                    break;

                case OP_CMP:
                    alu = registers[rd] - rn;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    this.OverflowCarrySub(registers[rd], rn, alu);
                    break;

                case OP_EOR:
                    registers[rd] ^= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_LSL:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        carry = (registers[rd] >> (32 - shiftAmt)) & 0x1;
                        registers[rd] <<= shiftAmt;
                    }
                    else if (shiftAmt == 32)
                    {
                        carry = registers[rd] & 0x1;
                        registers[rd] = 0;
                    }
                    else
                    {
                        carry = 0;
                        registers[rd] = 0;
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_LSR:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        carry = (registers[rd] >> (shiftAmt - 1)) & 0x1;
                        registers[rd] >>= shiftAmt;
                    }
                    else if (shiftAmt == 32)
                    {
                        carry = (registers[rd] >> 31) & 0x1;
                        registers[rd] = 0;
                    }
                    else
                    {
                        carry = 0;
                        registers[rd] = 0;
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_MUL:
                    int mulCycles = 4;
                    // Multiply cycle calculations
                    if ((rn & 0xFFFFFF00) == 0 || (rn & 0xFFFFFF00) == 0xFFFFFF00)
                    {
                        mulCycles = 1;
                    }
                    else if ((rn & 0xFFFF0000) == 0 || (rn & 0xFFFF0000) == 0xFFFF0000)
                    {
                        mulCycles = 2;
                    }
                    else if ((rn & 0xFF000000) == 0 || (rn & 0xFF000000) == 0xFF000000)
                    {
                        mulCycles = 3;
                    }

                    this.parent.Cycles -= mulCycles;

                    registers[rd] *= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_MVN:
                    registers[rd] = ~rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_NEG:
                    registers[rd] = 0 - rn;

                    this.OverflowCarrySub(0, rn, registers[rd]);
                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_ORR:
                    registers[rd] |= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_ROR:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if ((shiftAmt & 0x1F) == 0)
                    {
                        carry = registers[rd] >> 31;
                    }
                    else
                    {
                        shiftAmt &= 0x1F;
                        carry = (registers[rd] >> (shiftAmt - 1)) & 0x1;
                        registers[rd] = (registers[rd] >> shiftAmt) | (registers[rd] << (32 - shiftAmt));
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_SBC:
                    orig = registers[rd];
                    registers[rd] = (registers[rd] - rn) - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    this.OverflowCarrySub(orig, rn, registers[rd]);
                    break;

                case OP_TST:
                    alu = registers[rd] & rn;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    break;

                default:
                    throw new Exception("The coder screwed up on the thumb alu op...");
            }
        }

        private void OpAddHi()
        {
            int rd = ((this.curInstruction & (1 << 7)) >> 4) | (this.curInstruction & 0x7);
            int rm = (this.curInstruction >> 3) & 0xF;

            registers[rd] += registers[rm];

            if (rd == 15)
            {
                registers[rd] &= ~1U;
                this.FlushQueue();
            }
        }

        private void OpCmpHi()
        {
            int rd = ((this.curInstruction & (1 << 7)) >> 4) | (this.curInstruction & 0x7);
            int rm = (this.curInstruction >> 3) & 0xF;

            uint alu = registers[rd] - registers[rm];

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(registers[rd], registers[rm], alu);
        }

        private void OpMovHi()
        {
            int rd = ((this.curInstruction & (1 << 7)) >> 4) | (this.curInstruction & 0x7);
            int rm = (this.curInstruction >> 3) & 0xF;

            registers[rd] = registers[rm];

            if (rd == 15)
            {
                registers[rd] &= ~1U;
                this.FlushQueue();
            }
        }

        private void OpBx()
        {
            int rm = (this.curInstruction >> 3) & 0xf;

            this.PackFlags();

            this.parent.CPSR &= ~Arm7Processor.T_MASK;
            this.parent.CPSR |= (registers[rm] & 1) << Arm7Processor.T_BIT;

            registers[15] = registers[rm] & (~1U);

            this.UnpackFlags();

            // Check for branch back to Arm Mode
            if ((this.parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
            {
                return;
            }

            this.FlushQueue();
        }

        private void OpLdrPc()
        {
            int rd = (this.curInstruction >> 8) & 0x7;

            registers[rd] = this.memory.ReadU32((registers[15] & ~2U) + (uint)((this.curInstruction & 0xFF) * 4));

            this.parent.Cycles--;
        }

        private void OpStrReg()
        {
            this.memory.WriteU32(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7],
                registers[this.curInstruction & 0x7]);
        }

        private void OpStrhReg()
        {
            this.memory.WriteU16(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7],
                (ushort)(registers[this.curInstruction & 0x7] & 0xFFFF));
        }

        private void OpStrbReg()
        {
            this.memory.WriteU8(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7],
                (byte)(registers[this.curInstruction & 0x7] & 0xFF));
        }

        private void OpLdrsbReg()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU8(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7]);

            if ((registers[this.curInstruction & 0x7] & (1 << 7)) != 0)
            {
                registers[this.curInstruction & 0x7] |= 0xFFFFFF00;
            }

            this.parent.Cycles--;
        }

        private void OpLdrReg()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU32(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7]);

            this.parent.Cycles--;
        }

        private void OpLdrhReg()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU16(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7]);

            this.parent.Cycles--;
        }

        private void OpLdrbReg()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU8(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7]);

            this.parent.Cycles--;
        }

        private void OpLdrshReg()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU16(registers[(this.curInstruction >> 3) & 0x7] + registers[(this.curInstruction >> 6) & 0x7]);

            if ((registers[this.curInstruction & 0x7] & (1 << 15)) != 0)
            {
                registers[this.curInstruction & 0x7] |= 0xFFFF0000;
            }

            this.parent.Cycles--;
        }

        private void OpStrImm()
        {
            this.memory.WriteU32(registers[(this.curInstruction >> 3) & 0x7] + (uint)(((this.curInstruction >> 6) & 0x1F) * 4),
                registers[this.curInstruction & 0x7]);
        }

        private void OpLdrImm()
        {
            registers[this.curInstruction & 0x7] = 
                this.memory.ReadU32(registers[(this.curInstruction >> 3) & 0x7] + (uint)(((this.curInstruction >> 6) & 0x1F) * 4));

            this.parent.Cycles--;
        }

        private void OpStrbImm()
        {
            this.memory.WriteU8(registers[(this.curInstruction >> 3) & 0x7] + (uint)((this.curInstruction >> 6) & 0x1F),
                (byte)(registers[this.curInstruction & 0x7] & 0xFF));
        }

        private void OpLdrbImm()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU8(registers[(this.curInstruction >> 3) & 0x7] + (uint)((this.curInstruction >> 6) & 0x1F));

            this.parent.Cycles--;
        }

        private void OpStrhImm()
        {
            this.memory.WriteU16(registers[(this.curInstruction >> 3) & 0x7] + (uint)(((this.curInstruction >> 6) & 0x1F) * 2),
                (ushort)(registers[this.curInstruction & 0x7] & 0xFFFF));
        }

        private void OpLdrhImm()
        {
            registers[this.curInstruction & 0x7] =
                this.memory.ReadU16(registers[(this.curInstruction >> 3) & 0x7] + (uint)(((this.curInstruction >> 6) & 0x1F) * 2));

            this.parent.Cycles--;
        }

        private void OpStrSp()
        {
            this.memory.WriteU32(registers[13] + (uint)((this.curInstruction & 0xFF) * 4),
                registers[(this.curInstruction >> 8) & 0x7]);
        }

        private void OpLdrSp()
        {
            registers[(this.curInstruction >> 8) & 0x7] = 
                this.memory.ReadU32(registers[13] + (uint)((this.curInstruction & 0xFF) * 4));
        }

        private void OpAddPc()
        {
            registers[(this.curInstruction >> 8) & 0x7] =
                (registers[15] & ~2U) + (uint)((this.curInstruction & 0xFF) * 4);
        }

        private void OpAddSp()
        {
            registers[(this.curInstruction >> 8) & 0x7] =
                registers[13] + (uint)((this.curInstruction & 0xFF) * 4);
        }

        private void OpSubSp()
        {
            if ((this.curInstruction & (1 << 7)) != 0)
                registers[13] -= (uint)((this.curInstruction & 0x7F) * 4);
            else
                registers[13] += (uint)((this.curInstruction & 0x7F) * 4);
        }

        private void OpPush()
        {
            for (int i = 7; i >= 0; i--)
            {
                if (((this.curInstruction >> i) & 1) != 0)
                {
                    registers[13] -= 4;
                    this.memory.WriteU32(registers[13], registers[i]);
                }
            }
        }

        private void OpPushLr()
        {
            registers[13] -= 4;
            this.memory.WriteU32(registers[13], registers[14]);

            for (int i = 7; i >= 0; i--)
            {
                if (((this.curInstruction >> i) & 1) != 0)
                {
                    registers[13] -= 4;
                    this.memory.WriteU32(registers[13], registers[i]);
                }
            }
        }

        private void OpPop()
        {
            for (int i = 0; i < 8; i++)
            {
                if (((this.curInstruction >> i) & 1) != 0)
                {
                    registers[i] = this.memory.ReadU32(registers[13]);
                    registers[13] += 4;
                }
            }

            this.parent.Cycles--;
        }

        private void OpPopPc()
        {
            for (int i = 0; i < 8; i++)
            {
                if (((this.curInstruction >> i) & 1) != 0)
                {
                    registers[i] = this.memory.ReadU32(registers[13]);
                    registers[13] += 4;
                }
            }

            registers[15] = this.memory.ReadU32(registers[13]) & (~1U);
            registers[13] += 4;

            // ARM9 check here

            this.FlushQueue();

            this.parent.Cycles--;
        }

        private void OpStmia()
        {
            int rn = (this.curInstruction >> 8) & 0x7;

            for (int i = 0; i < 8; i++)
            {
                if (((this.curInstruction >> i) & 1) != 0)
                {
                    this.memory.WriteU32(registers[rn] & (~3U), registers[i]);
                    registers[rn] += 4;
                }
            }
        }

        private void OpLdmia()
        {
            int rn = (this.curInstruction >> 8) & 0x7;

            uint address = registers[rn];

            for (int i = 0; i < 8; i++)
            {
                if (((this.curInstruction >> i) & 1) != 0)
                {
                    registers[i] = this.memory.ReadU32Aligned(address & (~3U));
                    address += 4;
                }
            }

            if (((this.curInstruction >> rn) & 1) == 0)
            {
                registers[rn] = address;
            }
        }

        private void OpBCond()
        {
            uint cond = 0;
            switch ((this.curInstruction >> 8) & 0xF)
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
                uint offset = (uint)(this.curInstruction & 0xFF);
                if ((offset & (1 << 7)) != 0) offset |= 0xFFFFFF00;

                registers[15] += offset << 1;

                this.FlushQueue();
            }
        }

        private void OpSwi()
        {
            registers[15] -= 4U;
            this.parent.EnterException(Arm7Processor.SVC, 0x8, false, false);
        }

        private void OpB()
        {
            uint offset = (uint)(this.curInstruction & 0x7FF);
            if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;

            registers[15] += offset << 1;

            this.FlushQueue();
        }

        private void OpBl1()
        {
            uint offset = (uint)(this.curInstruction & 0x7FF);
            if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;

            registers[14] = registers[15] + (offset << 12);
        }

        private void OpBl2()
        {
            uint tmp = registers[15];
            registers[15] = registers[14] + (uint)((this.curInstruction & 0x7FF) << 1);
            registers[14] = (tmp - 2U) | 1;

            this.FlushQueue();
        }

        private void OpUnd()
        {
            throw new Exception("Unknown opcode");
        }
        #endregion

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
            this.instructionQueue = this.memory.ReadU16(registers[15]);
            registers[15] += 2;
        }
    }
}
