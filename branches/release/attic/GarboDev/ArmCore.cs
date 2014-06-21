//#define ARM_DEBUG

namespace GarboDev
{
    using System;

    public class ArmCore
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

        private delegate void ExecuteInstruction();
        private ExecuteInstruction[] NormalOps = null;

        private Arm7Processor parent;
        private Memory memory;
        private uint[] registers;

        private uint instructionQueue;
        private uint curInstruction;

        // CPU flags
        private uint zero, carry, negative, overflow;
        private uint shifterCarry;

        private bool thumbMode;

        public ArmCore(Arm7Processor parent, Memory memory)
        {
            this.parent = parent;
            this.memory = memory;
            this.registers = this.parent.Registers;

            this.NormalOps = new ExecuteInstruction[8]
                {
                    this.DataProcessing,
                    this.DataProcessingImmed,
                    this.LoadStoreImmediate,
                    this.LoadStoreRegister,
                    this.LoadStoreMultiple,
                    this.Branch,
                    this.CoprocessorLoadStore,
                    this.SoftwareInterrupt
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
                this.NormalOps[(curInstruction >> 25) & 0x7]();
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
                    this.NormalOps[(curInstruction >> 25) & 0x7]();
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
                        this.NormalOps[(curInstruction >> 25) & 0x7]();
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

                if (((this.curInstruction >> (int)rn) & 1) != 0 && writeback &&
                    (this.curInstruction & ~(0xFFFFFFFF << (int)rn)) == 0)
                {
                    // If the lowest register is also the writeback, we use the original value
                    // Does anybody do this????
                    throw new Exception("Unhandled STM state");
                }
                else
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