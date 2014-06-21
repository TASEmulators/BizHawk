namespace GarboDev
{
    partial class FastArmCore
    {
        #region Delegate Dispatcher
        private void DispatchFunc0()
        {
            uint rn, rd;
            // AND rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc1()
        {
            uint rn, rd;
            // AND rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc2()
        {
            uint rn, rd;
            // AND rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc3()
        {
            uint rn, rd;
            // AND rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc4()
        {
            uint rn, rd;
            // AND rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc5()
        {
            uint rn, rd;
            // AND rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc6()
        {
            uint rn, rd;
            // AND rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc7()
        {
            uint rn, rd;
            // AND rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc8()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc9()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc10()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc11()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc12()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc13()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc14()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc15()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc16()
        {
            UndefinedInstruction();
        }
        private void DispatchFunc17()
        {
            uint rd, rs, rm;
            int cycles;
            // MUL rd, rm, rs
            rd = (this.curInstruction >> 16) & 0xF;
            rs = (this.curInstruction >> 8) & 0xF;
            rm = this.curInstruction & 0xF;

            cycles = this.MultiplyCycleCalculation(rs);

            registers[rd] = registers[rs] * registers[rm];
            this.parent.Cycles -= cycles;
        }
        private void DispatchFunc18()
        {
            uint rd, rs, rm;
            int cycles;
            // MULS rd, rm, rs
            rd = (this.curInstruction >> 16) & 0xF;
            rs = (this.curInstruction >> 8) & 0xF;
            rm = this.curInstruction & 0xF;

            cycles = this.MultiplyCycleCalculation(rs);

            registers[rd] = registers[rs] * registers[rm];

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;

            this.parent.Cycles -= cycles;
        }
        private void DispatchFunc19()
        {
            uint rn, rd, rs, rm;
            int cycles;
            // MLA rd, rm, rs, rn
            rd = (this.curInstruction >> 16) & 0xF;
            rn = registers[(this.curInstruction >> 12) & 0xF];
            rs = (this.curInstruction >> 8) & 0xF;
            rm = this.curInstruction & 0xF;

            cycles = this.MultiplyCycleCalculation(rs);

            registers[rd] = registers[rs] * registers[rm] + rn;
            this.parent.Cycles -= cycles + 1;
        }
        private void DispatchFunc20()
        {
            uint rn, rd, rs, rm;
            int cycles;
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
        }
        private void DispatchFunc21()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc22()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc23()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc24()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc25()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc26()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc27()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc28()
        {
            uint rs, rm;
            int cycles;
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
        }
        private void DispatchFunc29()
        {
            uint rn, rd, address, offset;
            // STRH rd, [rn], -rm
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = this.registers[this.curInstruction & 0xF];
            offset = (uint)-offset;

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc30()
        {
            uint rn, rd, address, offset;
            // Writeback bit set, instruction is unpredictable
            // STRH rd, [rn], -rm
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = this.registers[this.curInstruction & 0xF];
            offset = (uint)-offset;

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc31()
        {
            uint rn, rd, address, offset;
            // STRH rd, [rn], -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
            offset = (uint)-offset;

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc32()
        {
            uint rn, rd, address, offset;
            // Writeback bit set, instruction is unpredictable
            // STRH rd, [rn], -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
            offset = (uint)-offset;

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc33()
        {
            uint rn, rd, address, offset;
            // STRH rd, [rn], rm
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = this.registers[this.curInstruction & 0xF];

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc34()
        {
            uint rn, rd, address, offset;
            // Writeback bit set, instruction is unpredictable
            // STRH rd, [rn], rm
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = this.registers[this.curInstruction & 0xF];

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc35()
        {
            uint rn, rd, address, offset;
            // STRH rd, [rn], immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc36()
        {
            uint rn, rd, address, offset;
            // Writeback bit set, instruction is unpredictable
            // STRH rd, [rn], immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            address = registers[rn];

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

            this.memory.WriteU16(address, (ushort)(registers[rd] & 0xFFFF));
            registers[rn] = address + offset;
        }
        private void DispatchFunc37()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, -rm]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.registers[this.curInstruction & 0xF];
            offset = (uint)-offset;

            this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc38()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, -rm]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.registers[this.curInstruction & 0xF];
            offset = (uint)-offset;

            registers[rn] += offset;
            this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc39()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, -immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
            offset = (uint)-offset;

            this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc40()
        {
            uint rn, rd, offset;
            // STRH rd, [rn], -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);
            offset = (uint)-offset;

            registers[rn] += offset;
            this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc41()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, rm]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.registers[this.curInstruction & 0xF];

            this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc42()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, rm]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.registers[this.curInstruction & 0xF];

            registers[rn] += offset;
            this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc43()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

            this.memory.WriteU16(registers[rn] + offset, (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc44()
        {
            uint rn, rd, offset;
            // STRH rd, [rn, immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = ((this.curInstruction & 0xF00) >> 4) | (this.curInstruction & 0xF);

            registers[rn] += offset;
            this.memory.WriteU16(registers[rn], (ushort)(registers[rd] & 0xFFFF));
        }
        private void DispatchFunc45()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc46()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc47()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc48()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc49()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc50()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc51()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc52()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc53()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc54()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc55()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc56()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc57()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc58()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc59()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc60()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc61()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc62()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc63()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc64()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc65()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc66()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc67()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc68()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc69()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc70()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc71()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc72()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc73()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc74()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc75()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc76()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc77()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc78()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc79()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc80()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc81()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc82()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc83()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc84()
        {
            uint rn, rd, address, offset;
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
        }
        private void DispatchFunc85()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc86()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc87()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc88()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc89()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc90()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc91()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc92()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc93()
        {
            uint rn, rd;
            // EOR rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc94()
        {
            uint rn, rd;
            // EOR rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc95()
        {
            uint rn, rd;
            // EOR rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc96()
        {
            uint rn, rd;
            // EOR rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc97()
        {
            uint rn, rd;
            // EOR rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc98()
        {
            uint rn, rd;
            // EOR rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc99()
        {
            uint rn, rd;
            // EOR rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc100()
        {
            uint rn, rd;
            // EOR rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn ^ BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc101()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc102()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc103()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc104()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc105()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc106()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc107()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc108()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc109()
        {
            uint rn, rd;
            // SUB rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc110()
        {
            uint rn, rd;
            // SUB rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc111()
        {
            uint rn, rd;
            // SUB rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc112()
        {
            uint rn, rd;
            // SUB rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc113()
        {
            uint rn, rd;
            // SUB rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc114()
        {
            uint rn, rd;
            // SUB rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc115()
        {
            uint rn, rd;
            // SUB rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc116()
        {
            uint rn, rd;
            // SUB rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc117()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc118()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc119()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc120()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc121()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc122()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc123()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc124()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc125()
        {
            uint rn, rd;
            // RSB rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLslImmed() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc126()
        {
            uint rn, rd;
            // RSB rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLslReg() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc127()
        {
            uint rn, rd;
            // RSB rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLsrImmed() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc128()
        {
            uint rn, rd;
            // RSB rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLsrReg() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc129()
        {
            uint rn, rd;
            // RSB rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterAsrImmed() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc130()
        {
            uint rn, rd;
            // RSB rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterAsrReg() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc131()
        {
            uint rn, rd;
            // RSB rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterRorImmed() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc132()
        {
            uint rn, rd;
            // RSB rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterRorReg() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc133()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc134()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc135()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc136()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc137()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc138()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc139()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc140()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc141()
        {
            uint rn, rd;
            // ADD rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc142()
        {
            uint rn, rd;
            // ADD rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc143()
        {
            uint rn, rd;
            // ADD rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc144()
        {
            uint rn, rd;
            // ADD rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc145()
        {
            uint rn, rd;
            // ADD rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc146()
        {
            uint rn, rd;
            // ADD rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc147()
        {
            uint rn, rd;
            // ADD rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc148()
        {
            uint rn, rd;
            // ADD rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc149()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc150()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc151()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc152()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc153()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc154()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc155()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc156()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc157()
        {
            uint rn, rd;
            // ADC rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLslImmed() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc158()
        {
            uint rn, rd;
            // ADC rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLslReg() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc159()
        {
            uint rn, rd;
            // ADC rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLsrImmed() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc160()
        {
            uint rn, rd;
            // ADC rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterLsrReg() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc161()
        {
            uint rn, rd;
            // ADC rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterAsrImmed() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc162()
        {
            uint rn, rd;
            // ADC rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterAsrReg() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc163()
        {
            uint rn, rd;
            // ADC rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterRorImmed() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc164()
        {
            uint rn, rd;
            // ADC rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn + BarrelShifterRorReg() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc165()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc166()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc167()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc168()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc169()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc170()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc171()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc172()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc173()
        {
            uint rn, rd;
            // SBC rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLslImmed() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc174()
        {
            uint rn, rd;
            // SBC rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLslReg() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc175()
        {
            uint rn, rd;
            // SBC rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLsrImmed() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc176()
        {
            uint rn, rd;
            // SBC rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterLsrReg() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc177()
        {
            uint rn, rd;
            // SBC rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterAsrImmed() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc178()
        {
            uint rn, rd;
            // SBC rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterAsrReg() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc179()
        {
            uint rn, rd;
            // SBC rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterRorImmed() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc180()
        {
            uint rn, rd;
            // SBC rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn - BarrelShifterRorReg() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc181()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc182()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc183()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc184()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc185()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc186()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc187()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc188()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc189()
        {
            uint rn, rd;
            // RSC rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLslImmed() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc190()
        {
            uint rn, rd;
            // RSC rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLslReg() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc191()
        {
            uint rn, rd;
            // RSC rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLsrImmed() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc192()
        {
            uint rn, rd;
            // RSC rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLsrReg() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc193()
        {
            uint rn, rd;
            // RSC rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterAsrImmed() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc194()
        {
            uint rn, rd;
            // RSC rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterAsrReg() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc195()
        {
            uint rn, rd;
            // RSC rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterRorImmed() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc196()
        {
            uint rn, rd;
            // RSC rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterRorReg() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc197()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc198()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc199()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc200()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc201()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc202()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc203()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc204()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc205()
        {
            uint rn, rd, rm;
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
        }
        private void DispatchFunc206()
        {
            uint rn, rd, rm;
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
        }
        private void DispatchFunc207()
        {
            uint rd;
            // MRS rd, cpsr
            rd = (this.curInstruction >> 12) & 0xF;
            this.PackFlags();
            registers[rd] = this.parent.CPSR;
        }
        private void DispatchFunc208()
        {
            uint rd;
            // MRS rd, spsr
            rd = (this.curInstruction >> 12) & 0xF;
            if (this.parent.SPSRExists) registers[rd] = this.parent.SPSR;
        }
        private void DispatchFunc209()
        {
            uint rm;
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
        }
        private void DispatchFunc210()
        {
            uint rm;
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
        }
        private void DispatchFunc211()
        {
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
        }
        private void DispatchFunc212()
        {
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
        }
        private void DispatchFunc213()
        {
            uint rm;
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
        }
        private void DispatchFunc214()
        {
            uint rn, alu;
            // TSTS rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterLslImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc215()
        {
            uint rn, alu;
            // TSTS rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterLslReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc216()
        {
            uint rn, alu;
            // TSTS rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterLsrImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc217()
        {
            uint rn, alu;
            // TSTS rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterLsrReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc218()
        {
            uint rn, alu;
            // TSTS rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterAsrImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc219()
        {
            uint rn, alu;
            // TSTS rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterAsrReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc220()
        {
            uint rn, alu;
            // TSTS rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterRorImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc221()
        {
            uint rn, rd, alu;
            // TSTS rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            alu = rn & BarrelShifterRorReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc222()
        {
            uint rn, alu;
            // TEQS rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterLslImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc223()
        {
            uint rn, alu;
            // TEQS rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterLslReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc224()
        {
            uint rn, alu;
            // TEQS rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterLsrImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc225()
        {
            uint rn, alu;
            // TEQS rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterLsrReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc226()
        {
            uint rn, alu;
            // TEQS rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterAsrImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc227()
        {
            uint rn, alu;
            // TEQS rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterAsrReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc228()
        {
            uint rn, alu;
            // TEQS rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterRorImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc229()
        {
            uint rn, rd, alu;
            // TEQS rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            alu = rn ^ BarrelShifterRorReg();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc230()
        {
            uint rn, shifterOperand, alu;
            // CMP rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLslImmed();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc231()
        {
            uint rn, shifterOperand, alu;
            // CMP rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLslReg();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc232()
        {
            uint rn, shifterOperand, alu;
            // CMP rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLsrImmed();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc233()
        {
            uint rn, shifterOperand, alu;
            // CMP rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLsrReg();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc234()
        {
            uint rn, shifterOperand, alu;
            // CMP rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterAsrImmed();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc235()
        {
            uint rn, shifterOperand, alu;
            // CMP rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterAsrReg();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc236()
        {
            uint rn, shifterOperand, alu;
            // CMP rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterRorImmed();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc237()
        {
            uint rn, shifterOperand, alu;
            // CMP rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterRorReg();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc238()
        {
            uint rn, shifterOperand, alu;
            // CMN rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLslImmed();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc239()
        {
            uint rn, shifterOperand, alu;
            // CMN rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLslReg();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc240()
        {
            uint rn, shifterOperand, alu;
            // CMN rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLsrImmed();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc241()
        {
            uint rn, shifterOperand, alu;
            // CMN rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterLsrReg();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc242()
        {
            uint rn, shifterOperand, alu;
            // CMN rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterAsrImmed();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc243()
        {
            uint rn, shifterOperand, alu;
            // CMN rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterAsrReg();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc244()
        {
            uint rn, shifterOperand, alu;
            // CMN rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterRorImmed();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc245()
        {
            uint rn, shifterOperand, alu;
            // CMN rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterRorReg();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc246()
        {
            uint rn, rd;
            // ORR rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc247()
        {
            uint rn, rd;
            // ORR rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc248()
        {
            uint rn, rd;
            // ORR rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc249()
        {
            uint rn, rd;
            // ORR rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc250()
        {
            uint rn, rd;
            // ORR rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc251()
        {
            uint rn, rd;
            // ORR rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc252()
        {
            uint rn, rd;
            // ORR rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc253()
        {
            uint rn, rd;
            // ORR rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn | BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc254()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc255()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc256()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc257()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc258()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc259()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc260()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc261()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc262()
        {
            uint rd;
            // MOV rd, rm lsl immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc263()
        {
            uint rd;
            // MOV rd, rm lsl rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc264()
        {
            uint rd;
            // MOV rd, rm lsr immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc265()
        {
            uint rd;
            // MOV rd, rm lsr rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc266()
        {
            uint rd;
            // MOV rd, rm asr immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc267()
        {
            uint rd;
            // MOV rd, rm asr rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc268()
        {
            uint rd;
            // MOV rd, rm ror immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc269()
        {
            uint rd;
            // MOV rd, rm ror rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc270()
        {
            uint rd;
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
        }
        private void DispatchFunc271()
        {
            uint rd;
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
        }
        private void DispatchFunc272()
        {
            uint rd;
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
        }
        private void DispatchFunc273()
        {
            uint rd;
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
        }
        private void DispatchFunc274()
        {
            uint rd;
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
        }
        private void DispatchFunc275()
        {
            uint rd;
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
        }
        private void DispatchFunc276()
        {
            uint rd;
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
        }
        private void DispatchFunc277()
        {
            uint rd;
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
        }
        private void DispatchFunc278()
        {
            uint rn, rd;
            // BIC rd, rn, rm lsl immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc279()
        {
            uint rn, rd;
            // BIC rd, rn, rm lsl rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc280()
        {
            uint rn, rd;
            // BIC rd, rn, rm lsr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc281()
        {
            uint rn, rd;
            // BIC rd, rn, rm lsr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc282()
        {
            uint rn, rd;
            // BIC rd, rn, rm asr immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc283()
        {
            uint rn, rd;
            // BIC rd, rn, rm asr rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc284()
        {
            uint rn, rd;
            // BIC rd, rn, rm ror immed
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc285()
        {
            uint rn, rd;
            // BIC rd, rn, rm ror rs
            rn = registers[(this.curInstruction >> 16) & 0xF];
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = rn & ~BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc286()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc287()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc288()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc289()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc290()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc291()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc292()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc293()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc294()
        {
            uint rd;
            // MVN rd, rm lsl immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterLslImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc295()
        {
            uint rd;
            // MVN rd, rm lsl rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterLslReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc296()
        {
            uint rd;
            // MVN rd, rm lsr immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterLsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc297()
        {
            uint rd;
            // MVN rd, rm lsr rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterLsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc298()
        {
            uint rd;
            // MVN rd, rm asr immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterAsrImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc299()
        {
            uint rd;
            // MVN rd, rm asr rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterAsrReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc300()
        {
            uint rd;
            // MVN rd, rm ror immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterRorImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc301()
        {
            uint rd;
            // MVN rd, rm ror rs
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterRorReg();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc302()
        {
            uint rd;
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
        }
        private void DispatchFunc303()
        {
            uint rd;
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
        }
        private void DispatchFunc304()
        {
            uint rd;
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
        }
        private void DispatchFunc305()
        {
            uint rd;
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
        }
        private void DispatchFunc306()
        {
            uint rd;
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
        }
        private void DispatchFunc307()
        {
            uint rd;
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
        }
        private void DispatchFunc308()
        {
            uint rd;
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
        }
        private void DispatchFunc309()
        {
            uint rd;
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
        }
        private void DispatchFunc310()
        {
            uint rn, rd;
            // AND rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn & BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc311()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc312()
        {
            uint rn, rd;
            // EOR rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn ^ BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc313()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc314()
        {
            uint rn, rd;
            // SUB rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn - BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc315()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc316()
        {
            uint rn, rd;
            // RSB rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = BarrelShifterImmed() - rn;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc317()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc318()
        {
            uint rn, rd;
            // ADD rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn + BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc319()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc320()
        {
            uint rn, rd;
            // ADC rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn + BarrelShifterImmed() + carry;

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc321()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc322()
        {
            uint rn, rd;
            // SBC rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn - BarrelShifterImmed() - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc323()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc324()
        {
            uint rn, rd;
            // RSC rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = BarrelShifterImmed() - rn - (1U - carry);

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc325()
        {
            uint rn, rd, shifterOperand;
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
        }
        private void DispatchFunc326()
        {
            uint rn, alu;
            // TSTS rn, immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn & BarrelShifterImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc327()
        {
            uint rn, alu;
            // TEQS rn, immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            alu = rn ^ BarrelShifterImmed();

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            carry = this.shifterCarry;
        }
        private void DispatchFunc328()
        {
            uint rn, shifterOperand, alu;
            // CMP rn, immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterImmed();
            alu = rn - shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarrySub(rn, shifterOperand, alu);
        }
        private void DispatchFunc329()
        {
            uint rn, shifterOperand, alu;
            // CMN rn, immed
            rn = registers[(this.curInstruction >> 16) & 0xF];

            shifterOperand = BarrelShifterImmed();
            alu = rn + shifterOperand;

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            this.OverflowCarryAdd(rn, shifterOperand, alu);
        }
        private void DispatchFunc330()
        {
            uint rn, rd;
            // ORR rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn | BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc331()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc332()
        {
            uint rd;
            // MOV rd, immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc333()
        {
            uint rd;
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
        }
        private void DispatchFunc334()
        {
            uint rn, rd;
            // BIC rd, rn, immed
            rd = (this.curInstruction >> 12) & 0xF;
            rn = registers[(this.curInstruction >> 16) & 0xF];

            registers[rd] = rn & ~BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc335()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc336()
        {
            uint rd;
            // MVN rd, immed
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = ~BarrelShifterImmed();

            if (rd == 15)
            {
                this.FlushQueue();
            }
        }
        private void DispatchFunc337()
        {
            uint rd;
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
        }
        private void DispatchFunc338()
        {
            uint rn, rd, offset, alu;
            // STR rd, rn, -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc339()
        {
            uint rn, rd, offset, alu;
            // STRT rd, rn, -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc340()
        {
            uint rn, rd, offset, alu;
            // STRB rd, rn, -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc341()
        {
            uint rn, rd, offset, alu;
            // STRBT rd, rn, -immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc342()
        {
            uint rn, rd, alu;
            // STR rd, rn, immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.curInstruction & 0xFFF;
        }
        private void DispatchFunc343()
        {
            uint rn, rd, alu;
            // STRT rd, rn, immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.curInstruction & 0xFFF;
        }
        private void DispatchFunc344()
        {
            uint rn, rd, alu;
            // STRB rd, rn, immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.curInstruction & 0xFFF;
        }
        private void DispatchFunc345()
        {
            uint rn, rd, alu;
            // STRBT rd, rn, immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.curInstruction & 0xFFF;
        }
        private void DispatchFunc346()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + offset, alu);
        }
        private void DispatchFunc347()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc348()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
        }
        private void DispatchFunc349()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.curInstruction & 0xFFF;
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc350()
        {
            uint rn, rd, alu;
            // STR rd, [rn, immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + (this.curInstruction & 0xFFF), alu);
        }
        private void DispatchFunc351()
        {
            uint rn, rd, alu;
            // STRT rd, [rn, immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.curInstruction & 0xFFF;
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc352()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + (this.curInstruction & 0xFFF), (byte)(alu & 0xFF));
        }
        private void DispatchFunc353()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.curInstruction & 0xFFF;
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc354()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc355()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc356()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc357()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc358()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc359()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc360()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc361()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc362()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc363()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc364()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc365()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc366()
        {
            uint rn, rd;
            // LDR rd, [rn, immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU32(registers[rn] + (this.curInstruction & 0xFFF));

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc367()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc368()
        {
            uint rn, rd;
            // LDRB rd, [rn, immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU8(registers[rn] + (this.curInstruction & 0xFFF));

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc369()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc370()
        {
            uint rn, rd, offset, alu;
            // STR rd, rn, -rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc371()
        {
            uint rn, rd, offset, alu;
            // STR rd, rn, -rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc372()
        {
            uint rn, rd, offset, alu;
            // STR rd, rn, -rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc373()
        {
            uint rn, rd, offset, alu;
            // STR rd, rn, -rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc374()
        {
            uint rn, rd, offset, alu;
            // STRT rd, rn, -rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc375()
        {
            uint rn, rd, offset, alu;
            // STRT rd, rn, -rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc376()
        {
            uint rn, rd, offset, alu;
            // STRT rd, rn, -rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc377()
        {
            uint rn, rd, offset, alu;
            // STRT rd, rn, -rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += offset;
        }
        private void DispatchFunc378()
        {
            uint rn, rd, offset, alu;
            // STRB rd, rn, -rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc379()
        {
            uint rn, rd, offset, alu;
            // STRB rd, rn, -rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc380()
        {
            uint rn, rd, offset, alu;
            // STRB rd, rn, -rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc381()
        {
            uint rn, rd, offset, alu;
            // STRB rd, rn, -rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc382()
        {
            uint rn, rd, offset, alu;
            // STRBT rd, rn, -rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc383()
        {
            uint rn, rd, offset, alu;
            // STRBT rd, rn, -rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc384()
        {
            uint rn, rd, offset, alu;
            // STRBT rd, rn, -rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc385()
        {
            uint rn, rd, offset, alu;
            // STRBT rd, rn, -rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += offset;
        }
        private void DispatchFunc386()
        {
            uint rn, rd, alu;
            // STR rd, rn, rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterLslImmed();
        }
        private void DispatchFunc387()
        {
            uint rn, rd, alu;
            // STR rd, rn, rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterLsrImmed();
        }
        private void DispatchFunc388()
        {
            uint rn, rd, alu;
            // STR rd, rn, rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterAsrImmed();
        }
        private void DispatchFunc389()
        {
            uint rn, rd, alu;
            // STR rd, rn, rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterRorImmed();
        }
        private void DispatchFunc390()
        {
            uint rn, rd, alu;
            // STRT rd, rn, rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterLslImmed();
        }
        private void DispatchFunc391()
        {
            uint rn, rd, alu;
            // STRT rd, rn, rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterLsrImmed();
        }
        private void DispatchFunc392()
        {
            uint rn, rd, alu;
            // STRT rd, rn, rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterAsrImmed();
        }
        private void DispatchFunc393()
        {
            uint rn, rd, alu;
            // STRT rd, rn, rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn], alu);
            registers[rn] += this.BarrelShifterRorImmed();
        }
        private void DispatchFunc394()
        {
            uint rn, rd, alu;
            // STRB rd, rn, rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterLslImmed();
        }
        private void DispatchFunc395()
        {
            uint rn, rd, alu;
            // STRB rd, rn, rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterLsrImmed();
        }
        private void DispatchFunc396()
        {
            uint rn, rd, alu;
            // STRB rd, rn, rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterAsrImmed();
        }
        private void DispatchFunc397()
        {
            uint rn, rd, alu;
            // STRB rd, rn, rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterRorImmed();
        }
        private void DispatchFunc398()
        {
            uint rn, rd, alu;
            // STRBT rd, rn, rm lsl immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterLslImmed();
        }
        private void DispatchFunc399()
        {
            uint rn, rd, alu;
            // STRBT rd, rn, rm lsr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterLsrImmed();
        }
        private void DispatchFunc400()
        {
            uint rn, rd, alu;
            // STRBT rd, rn, rm asr immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterAsrImmed();
        }
        private void DispatchFunc401()
        {
            uint rn, rd, alu;
            // STRBT rd, rn, rm ror immed
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
            registers[rn] += this.BarrelShifterRorImmed();
        }
        private void DispatchFunc402()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm lsl immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + offset, alu);
        }
        private void DispatchFunc403()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm lsr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + offset, alu);
        }
        private void DispatchFunc404()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm asr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + offset, alu);
        }
        private void DispatchFunc405()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm ror immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + offset, alu);
        }
        private void DispatchFunc406()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm lsl immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc407()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm lsr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc408()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm asr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc409()
        {
            uint rn, rd, offset, alu;
            // STR rd, [rn, -rm ror immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc410()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm lsl immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
        }
        private void DispatchFunc411()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm lsr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
        }
        private void DispatchFunc412()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm asr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
        }
        private void DispatchFunc413()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm ror immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + offset, (byte)(alu & 0xFF));
        }
        private void DispatchFunc414()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm lsl immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLslImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc415()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm lsr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterLsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc416()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm asr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterAsrImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc417()
        {
            uint rn, rd, offset, alu;
            // STRB rd, [rn, -rm ror immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            offset = this.BarrelShifterRorImmed();
            offset = (uint)-offset;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += offset;
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc418()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm lsl immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + this.BarrelShifterLslImmed(), alu);
        }
        private void DispatchFunc419()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm lsr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + this.BarrelShifterLsrImmed(), alu);
        }
        private void DispatchFunc420()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm asr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + this.BarrelShifterAsrImmed(), alu);
        }
        private void DispatchFunc421()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm ror immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU32(registers[rn] + this.BarrelShifterRorImmed(), alu);
        }
        private void DispatchFunc422()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm lsl immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterLslImmed();
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc423()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm lsr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterLsrImmed();
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc424()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm asr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterAsrImmed();
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc425()
        {
            uint rn, rd, alu;
            // STR rd, [rn, rm ror immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterRorImmed();
            this.memory.WriteU32(registers[rn], alu);
        }
        private void DispatchFunc426()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm lsl immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + this.BarrelShifterLslImmed(), (byte)(alu & 0xFF));
        }
        private void DispatchFunc427()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm lsr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + this.BarrelShifterLsrImmed(), (byte)(alu & 0xFF));
        }
        private void DispatchFunc428()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm asr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + this.BarrelShifterAsrImmed(), (byte)(alu & 0xFF));
        }
        private void DispatchFunc429()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm ror immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            this.memory.WriteU8(registers[rn] + this.BarrelShifterRorImmed(), (byte)(alu & 0xFF));
        }
        private void DispatchFunc430()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm lsl immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterLslImmed();
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc431()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm lsr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterLsrImmed();
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc432()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm asr immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterAsrImmed();
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc433()
        {
            uint rn, rd, alu;
            // STRB rd, [rn, rm ror immed]!
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            alu = registers[rd];
            if (rd == 15) alu += 4;

            registers[rn] += this.BarrelShifterRorImmed();
            this.memory.WriteU8(registers[rn], (byte)(alu & 0xFF));
        }
        private void DispatchFunc434()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc435()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc436()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc437()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc438()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc439()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc440()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc441()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc442()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc443()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc444()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc445()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc446()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc447()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc448()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc449()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc450()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc451()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc452()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc453()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc454()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc455()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc456()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc457()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc458()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc459()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc460()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc461()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc462()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc463()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc464()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc465()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc466()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc467()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc468()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc469()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc470()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc471()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc472()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc473()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc474()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc475()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc476()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc477()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc478()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc479()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc480()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc481()
        {
            uint rn, rd, offset;
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
        }
        private void DispatchFunc482()
        {
            uint rn, rd;
            // LDR rd, [rn, rm lsl immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterLslImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc483()
        {
            uint rn, rd;
            // LDR rd, [rn, rm lsr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterLsrImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc484()
        {
            uint rn, rd;
            // LDR rd, [rn, rm asr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterAsrImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc485()
        {
            uint rn, rd;
            // LDR rd, [rn, rm ror immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU32(registers[rn] + this.BarrelShifterRorImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc486()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc487()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc488()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc489()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc490()
        {
            uint rn, rd;
            // LDRB rd, [rn, rm lsl immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterLslImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc491()
        {
            uint rn, rd;
            // LDRB rd, [rn, rm lsr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterLsrImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc492()
        {
            uint rn, rd;
            // LDRB rd, [rn, rm asr immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterAsrImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc493()
        {
            uint rn, rd;
            // LDRB rd, [rn, rm ror immed]
            rn = (this.curInstruction >> 16) & 0xF;
            rd = (this.curInstruction >> 12) & 0xF;

            registers[rd] = this.memory.ReadU8(registers[rn] + this.BarrelShifterRorImmed());

            if (rd == 15)
            {
                registers[rd] &= ~3U;
                this.FlushQueue();
            }
        }
        private void DispatchFunc494()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc495()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc496()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc497()
        {
            uint rn, rd;
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
        }
        private void DispatchFunc498()
        {
            {
                uint branchOffset = this.curInstruction & 0x00FFFFFF;
                if (branchOffset >> 23 == 1) branchOffset |= 0xFF000000;

                this.registers[15] += branchOffset << 2;

                this.FlushQueue();
            }
        }
        private void DispatchFunc499()
        {
            {
                uint branchOffset = this.curInstruction & 0x00FFFFFF;
                if (branchOffset >> 23 == 1) branchOffset |= 0xFF000000;

                this.registers[14] = this.registers[15] - 4U;
                this.registers[15] += branchOffset << 2;

                this.FlushQueue();
            }
        }
        private void DispatchFunc500()
        {
            this.registers[15] -= 4U;
            this.parent.EnterException(Arm7Processor.SVC, 0x8, false, false);
        }
        private void DefaultDispatchFunc() { this.NormalOps[(curInstruction >> 25) & 0x7](); }
        private ExecuteInstructionDelegate[] fastDispatch = null;
        private void InitializeDispatchFunc()
        {
            this.fastDispatch = new ExecuteInstructionDelegate[] {
DispatchFunc0,DispatchFunc1,DispatchFunc2,DispatchFunc3,DispatchFunc4,DispatchFunc5,DispatchFunc6,DispatchFunc7,DispatchFunc0,DispatchFunc17,DispatchFunc2,DispatchFunc29,DispatchFunc4,DispatchFunc16,DispatchFunc6,DispatchFunc16,DispatchFunc8,DispatchFunc9,DispatchFunc10,DispatchFunc11,DispatchFunc12
,DispatchFunc13,DispatchFunc14,DispatchFunc15,DispatchFunc8,DispatchFunc18,DispatchFunc10,DispatchFunc45,DispatchFunc12,DispatchFunc61,DispatchFunc14,DispatchFunc77,DispatchFunc93,DispatchFunc94,DispatchFunc95,DispatchFunc96,DispatchFunc97,DispatchFunc98,DispatchFunc99,DispatchFunc100,DispatchFunc93
,DispatchFunc19,DispatchFunc95,DispatchFunc30,DispatchFunc97,DispatchFunc16,DispatchFunc99,DispatchFunc16,DispatchFunc101,DispatchFunc102,DispatchFunc103,DispatchFunc104,DispatchFunc105,DispatchFunc106,DispatchFunc107,DispatchFunc108,DispatchFunc101,DispatchFunc20,DispatchFunc103,DispatchFunc46,DispatchFunc105
,DispatchFunc62,DispatchFunc107,DispatchFunc78,DispatchFunc109,DispatchFunc110,DispatchFunc111,DispatchFunc112,DispatchFunc113,DispatchFunc114,DispatchFunc115,DispatchFunc116,DispatchFunc109,DefaultDispatchFunc,DispatchFunc111,DispatchFunc31,DispatchFunc113,DispatchFunc16,DispatchFunc115,DispatchFunc16,DispatchFunc117
,DispatchFunc118,DispatchFunc119,DispatchFunc120,DispatchFunc121,DispatchFunc122,DispatchFunc123,DispatchFunc124,DispatchFunc117,DefaultDispatchFunc,DispatchFunc119,DispatchFunc47,DispatchFunc121,DispatchFunc63,DispatchFunc123,DispatchFunc79,DispatchFunc125,DispatchFunc126,DispatchFunc127,DispatchFunc128,DispatchFunc129
,DispatchFunc130,DispatchFunc131,DispatchFunc132,DispatchFunc125,DefaultDispatchFunc,DispatchFunc127,DispatchFunc32,DispatchFunc129,DispatchFunc16,DispatchFunc131,DispatchFunc16,DispatchFunc133,DispatchFunc134,DispatchFunc135,DispatchFunc136,DispatchFunc137,DispatchFunc138,DispatchFunc139,DispatchFunc140,DispatchFunc133
,DefaultDispatchFunc,DispatchFunc135,DispatchFunc48,DispatchFunc137,DispatchFunc64,DispatchFunc139,DispatchFunc80,DispatchFunc141,DispatchFunc142,DispatchFunc143,DispatchFunc144,DispatchFunc145,DispatchFunc146,DispatchFunc147,DispatchFunc148,DispatchFunc141,DispatchFunc21,DispatchFunc143,DispatchFunc33,DispatchFunc145
,DispatchFunc16,DispatchFunc147,DispatchFunc16,DispatchFunc149,DispatchFunc150,DispatchFunc151,DispatchFunc152,DispatchFunc153,DispatchFunc154,DispatchFunc155,DispatchFunc156,DispatchFunc149,DispatchFunc22,DispatchFunc151,DispatchFunc49,DispatchFunc153,DispatchFunc65,DispatchFunc155,DispatchFunc81,DispatchFunc157
,DispatchFunc158,DispatchFunc159,DispatchFunc160,DispatchFunc161,DispatchFunc162,DispatchFunc163,DispatchFunc164,DispatchFunc157,DispatchFunc23,DispatchFunc159,DispatchFunc34,DispatchFunc161,DispatchFunc16,DispatchFunc163,DispatchFunc16,DispatchFunc165,DispatchFunc166,DispatchFunc167,DispatchFunc168,DispatchFunc169
,DispatchFunc170,DispatchFunc171,DispatchFunc172,DispatchFunc165,DispatchFunc24,DispatchFunc167,DispatchFunc50,DispatchFunc169,DispatchFunc66,DispatchFunc171,DispatchFunc82,DispatchFunc173,DispatchFunc174,DispatchFunc175,DispatchFunc176,DispatchFunc177,DispatchFunc178,DispatchFunc179,DispatchFunc180,DispatchFunc173
,DispatchFunc25,DispatchFunc175,DispatchFunc35,DispatchFunc177,DispatchFunc16,DispatchFunc179,DispatchFunc16,DispatchFunc181,DispatchFunc182,DispatchFunc183,DispatchFunc184,DispatchFunc185,DispatchFunc186,DispatchFunc187,DispatchFunc188,DispatchFunc181,DispatchFunc26,DispatchFunc183,DispatchFunc51,DispatchFunc185
,DispatchFunc67,DispatchFunc187,DispatchFunc83,DispatchFunc189,DispatchFunc190,DispatchFunc191,DispatchFunc192,DispatchFunc193,DispatchFunc194,DispatchFunc195,DispatchFunc196,DispatchFunc189,DispatchFunc27,DispatchFunc191,DispatchFunc36,DispatchFunc193,DispatchFunc16,DispatchFunc195,DispatchFunc16,DispatchFunc197
,DispatchFunc198,DispatchFunc199,DispatchFunc200,DispatchFunc201,DispatchFunc202,DispatchFunc203,DispatchFunc204,DispatchFunc197,DispatchFunc28,DispatchFunc199,DispatchFunc52,DispatchFunc201,DispatchFunc68,DispatchFunc203,DispatchFunc84,DispatchFunc207,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc205,DefaultDispatchFunc,DispatchFunc37,DefaultDispatchFunc,DispatchFunc16,DefaultDispatchFunc,DispatchFunc16,DispatchFunc214,DispatchFunc215,DispatchFunc216,DispatchFunc217,DispatchFunc218,DispatchFunc219,DispatchFunc220,DispatchFunc221,DispatchFunc214
,DefaultDispatchFunc,DispatchFunc216,DispatchFunc53,DispatchFunc218,DispatchFunc69,DispatchFunc220,DispatchFunc85,DispatchFunc209,DispatchFunc213,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc38,DefaultDispatchFunc
,DispatchFunc16,DefaultDispatchFunc,DispatchFunc16,DispatchFunc222,DispatchFunc223,DispatchFunc224,DispatchFunc225,DispatchFunc226,DispatchFunc227,DispatchFunc228,DispatchFunc229,DispatchFunc222,DefaultDispatchFunc,DispatchFunc224,DispatchFunc54,DispatchFunc226,DispatchFunc70,DispatchFunc228,DispatchFunc86,DispatchFunc208
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc206,DefaultDispatchFunc,DispatchFunc39,DefaultDispatchFunc,DispatchFunc16,DefaultDispatchFunc,DispatchFunc16,DispatchFunc230,DispatchFunc231,DispatchFunc232,DispatchFunc233,DispatchFunc234
,DispatchFunc235,DispatchFunc236,DispatchFunc237,DispatchFunc230,DefaultDispatchFunc,DispatchFunc232,DispatchFunc55,DispatchFunc234,DispatchFunc71,DispatchFunc236,DispatchFunc87,DispatchFunc210,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc40,DefaultDispatchFunc,DispatchFunc16,DefaultDispatchFunc,DispatchFunc16,DispatchFunc238,DispatchFunc239,DispatchFunc240,DispatchFunc241,DispatchFunc242,DispatchFunc243,DispatchFunc244,DispatchFunc245,DispatchFunc238,DefaultDispatchFunc,DispatchFunc240,DispatchFunc56,DispatchFunc242
,DispatchFunc72,DispatchFunc244,DispatchFunc88,DispatchFunc246,DispatchFunc247,DispatchFunc248,DispatchFunc249,DispatchFunc250,DispatchFunc251,DispatchFunc252,DispatchFunc253,DispatchFunc246,DefaultDispatchFunc,DispatchFunc248,DispatchFunc41,DispatchFunc250,DispatchFunc16,DispatchFunc252,DispatchFunc16,DispatchFunc254
,DispatchFunc255,DispatchFunc256,DispatchFunc257,DispatchFunc258,DispatchFunc259,DispatchFunc260,DispatchFunc261,DispatchFunc254,DefaultDispatchFunc,DispatchFunc256,DispatchFunc57,DispatchFunc258,DispatchFunc73,DispatchFunc260,DispatchFunc89,DispatchFunc262,DispatchFunc263,DispatchFunc264,DispatchFunc265,DispatchFunc266
,DispatchFunc267,DispatchFunc268,DispatchFunc269,DispatchFunc262,DefaultDispatchFunc,DispatchFunc264,DispatchFunc42,DispatchFunc266,DispatchFunc16,DispatchFunc268,DispatchFunc16,DispatchFunc270,DispatchFunc271,DispatchFunc272,DispatchFunc273,DispatchFunc274,DispatchFunc275,DispatchFunc276,DispatchFunc277,DispatchFunc270
,DefaultDispatchFunc,DispatchFunc272,DispatchFunc58,DispatchFunc274,DispatchFunc74,DispatchFunc276,DispatchFunc90,DispatchFunc278,DispatchFunc279,DispatchFunc280,DispatchFunc281,DispatchFunc282,DispatchFunc283,DispatchFunc284,DispatchFunc285,DispatchFunc278,DefaultDispatchFunc,DispatchFunc280,DispatchFunc43,DispatchFunc282
,DispatchFunc16,DispatchFunc284,DispatchFunc16,DispatchFunc286,DispatchFunc287,DispatchFunc288,DispatchFunc289,DispatchFunc290,DispatchFunc291,DispatchFunc292,DispatchFunc293,DispatchFunc286,DefaultDispatchFunc,DispatchFunc288,DispatchFunc59,DispatchFunc290,DispatchFunc75,DispatchFunc292,DispatchFunc91,DispatchFunc294
,DispatchFunc295,DispatchFunc296,DispatchFunc297,DispatchFunc298,DispatchFunc299,DispatchFunc300,DispatchFunc301,DispatchFunc294,DefaultDispatchFunc,DispatchFunc296,DispatchFunc44,DispatchFunc298,DispatchFunc16,DispatchFunc300,DispatchFunc16,DispatchFunc302,DispatchFunc303,DispatchFunc304,DispatchFunc305,DispatchFunc306
,DispatchFunc307,DispatchFunc308,DispatchFunc309,DispatchFunc302,DefaultDispatchFunc,DispatchFunc304,DispatchFunc60,DispatchFunc306,DispatchFunc76,DispatchFunc308,DispatchFunc92,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310
,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc310,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc311
,DispatchFunc311,DispatchFunc311,DispatchFunc311,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc312,DispatchFunc313
,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc313,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314
,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc314,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315
,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc315,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc316
,DispatchFunc316,DispatchFunc316,DispatchFunc316,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc317,DispatchFunc318
,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc318,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319
,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc319,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320
,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc320,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc321
,DispatchFunc321,DispatchFunc321,DispatchFunc321,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc322,DispatchFunc323
,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc323,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324
,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc324,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325
,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DispatchFunc325,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc326,DispatchFunc211
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327
,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DispatchFunc327,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc328
,DispatchFunc328,DispatchFunc328,DispatchFunc328,DispatchFunc212,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc329
,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc329,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330
,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc330,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331
,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc331,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc332
,DispatchFunc332,DispatchFunc332,DispatchFunc332,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc333,DispatchFunc334
,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc334,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335
,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc335,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336
,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc336,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc337
,DispatchFunc337,DispatchFunc337,DispatchFunc337,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc338,DispatchFunc354
,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc354,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339
,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc339,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355
,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc355,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc340
,DispatchFunc340,DispatchFunc340,DispatchFunc340,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc356,DispatchFunc341
,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc341,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357
,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc357,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342
,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc342,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc358
,DispatchFunc358,DispatchFunc358,DispatchFunc358,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc343,DispatchFunc359
,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc359,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344
,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc344,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360
,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc360,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc345
,DispatchFunc345,DispatchFunc345,DispatchFunc345,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc361,DispatchFunc346
,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc346,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362
,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc362,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347
,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc347,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc363
,DispatchFunc363,DispatchFunc363,DispatchFunc363,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc348,DispatchFunc364
,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc364,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349
,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc349,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365
,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc365,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc350
,DispatchFunc350,DispatchFunc350,DispatchFunc350,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc366,DispatchFunc351
,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc351,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367
,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc367,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352
,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc352,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc368
,DispatchFunc368,DispatchFunc368,DispatchFunc368,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc353,DispatchFunc369
,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc369,DispatchFunc370,DefaultDispatchFunc,DispatchFunc371,DefaultDispatchFunc,DispatchFunc372
,DefaultDispatchFunc,DispatchFunc373,DefaultDispatchFunc,DispatchFunc370,DefaultDispatchFunc,DispatchFunc371,DefaultDispatchFunc,DispatchFunc372,DefaultDispatchFunc,DispatchFunc373,DefaultDispatchFunc,DispatchFunc434,DefaultDispatchFunc,DispatchFunc435,DefaultDispatchFunc,DispatchFunc436,DefaultDispatchFunc,DispatchFunc437,DefaultDispatchFunc,DispatchFunc434
,DefaultDispatchFunc,DispatchFunc435,DefaultDispatchFunc,DispatchFunc436,DefaultDispatchFunc,DispatchFunc437,DefaultDispatchFunc,DispatchFunc374,DefaultDispatchFunc,DispatchFunc375,DefaultDispatchFunc,DispatchFunc376,DefaultDispatchFunc,DispatchFunc377,DefaultDispatchFunc,DispatchFunc374,DefaultDispatchFunc,DispatchFunc375,DefaultDispatchFunc,DispatchFunc376
,DefaultDispatchFunc,DispatchFunc377,DefaultDispatchFunc,DispatchFunc438,DefaultDispatchFunc,DispatchFunc439,DefaultDispatchFunc,DispatchFunc440,DefaultDispatchFunc,DispatchFunc441,DefaultDispatchFunc,DispatchFunc438,DefaultDispatchFunc,DispatchFunc439,DefaultDispatchFunc,DispatchFunc440,DefaultDispatchFunc,DispatchFunc441,DefaultDispatchFunc,DispatchFunc378
,DefaultDispatchFunc,DispatchFunc379,DefaultDispatchFunc,DispatchFunc380,DefaultDispatchFunc,DispatchFunc381,DefaultDispatchFunc,DispatchFunc378,DefaultDispatchFunc,DispatchFunc379,DefaultDispatchFunc,DispatchFunc380,DefaultDispatchFunc,DispatchFunc381,DefaultDispatchFunc,DispatchFunc442,DefaultDispatchFunc,DispatchFunc443,DefaultDispatchFunc,DispatchFunc444
,DefaultDispatchFunc,DispatchFunc445,DefaultDispatchFunc,DispatchFunc442,DefaultDispatchFunc,DispatchFunc443,DefaultDispatchFunc,DispatchFunc444,DefaultDispatchFunc,DispatchFunc445,DefaultDispatchFunc,DispatchFunc382,DefaultDispatchFunc,DispatchFunc383,DefaultDispatchFunc,DispatchFunc384,DefaultDispatchFunc,DispatchFunc385,DefaultDispatchFunc,DispatchFunc382
,DefaultDispatchFunc,DispatchFunc383,DefaultDispatchFunc,DispatchFunc384,DefaultDispatchFunc,DispatchFunc385,DefaultDispatchFunc,DispatchFunc446,DefaultDispatchFunc,DispatchFunc447,DefaultDispatchFunc,DispatchFunc448,DefaultDispatchFunc,DispatchFunc449,DefaultDispatchFunc,DispatchFunc446,DefaultDispatchFunc,DispatchFunc447,DefaultDispatchFunc,DispatchFunc448
,DefaultDispatchFunc,DispatchFunc449,DefaultDispatchFunc,DispatchFunc386,DefaultDispatchFunc,DispatchFunc387,DefaultDispatchFunc,DispatchFunc388,DefaultDispatchFunc,DispatchFunc389,DefaultDispatchFunc,DispatchFunc386,DefaultDispatchFunc,DispatchFunc387,DefaultDispatchFunc,DispatchFunc388,DefaultDispatchFunc,DispatchFunc389,DefaultDispatchFunc,DispatchFunc450
,DefaultDispatchFunc,DispatchFunc451,DefaultDispatchFunc,DispatchFunc452,DefaultDispatchFunc,DispatchFunc453,DefaultDispatchFunc,DispatchFunc450,DefaultDispatchFunc,DispatchFunc451,DefaultDispatchFunc,DispatchFunc452,DefaultDispatchFunc,DispatchFunc453,DefaultDispatchFunc,DispatchFunc390,DefaultDispatchFunc,DispatchFunc390,DefaultDispatchFunc,DispatchFunc391
,DefaultDispatchFunc,DispatchFunc391,DefaultDispatchFunc,DispatchFunc392,DefaultDispatchFunc,DispatchFunc392,DefaultDispatchFunc,DispatchFunc393,DefaultDispatchFunc,DispatchFunc393,DefaultDispatchFunc,DispatchFunc454,DefaultDispatchFunc,DispatchFunc455,DefaultDispatchFunc,DispatchFunc456,DefaultDispatchFunc,DispatchFunc457,DefaultDispatchFunc,DispatchFunc454
,DefaultDispatchFunc,DispatchFunc455,DefaultDispatchFunc,DispatchFunc456,DefaultDispatchFunc,DispatchFunc457,DefaultDispatchFunc,DispatchFunc394,DefaultDispatchFunc,DispatchFunc395,DefaultDispatchFunc,DispatchFunc396,DefaultDispatchFunc,DispatchFunc397,DefaultDispatchFunc,DispatchFunc394,DefaultDispatchFunc,DispatchFunc395,DefaultDispatchFunc,DispatchFunc396
,DefaultDispatchFunc,DispatchFunc397,DefaultDispatchFunc,DispatchFunc458,DefaultDispatchFunc,DispatchFunc459,DefaultDispatchFunc,DispatchFunc460,DefaultDispatchFunc,DispatchFunc461,DefaultDispatchFunc,DispatchFunc458,DefaultDispatchFunc,DispatchFunc459,DefaultDispatchFunc,DispatchFunc460,DefaultDispatchFunc,DispatchFunc461,DefaultDispatchFunc,DispatchFunc398
,DefaultDispatchFunc,DispatchFunc399,DefaultDispatchFunc,DispatchFunc400,DefaultDispatchFunc,DispatchFunc401,DefaultDispatchFunc,DispatchFunc398,DefaultDispatchFunc,DispatchFunc399,DefaultDispatchFunc,DispatchFunc400,DefaultDispatchFunc,DispatchFunc401,DefaultDispatchFunc,DispatchFunc462,DefaultDispatchFunc,DispatchFunc463,DefaultDispatchFunc,DispatchFunc464
,DefaultDispatchFunc,DispatchFunc465,DefaultDispatchFunc,DispatchFunc462,DefaultDispatchFunc,DispatchFunc463,DefaultDispatchFunc,DispatchFunc464,DefaultDispatchFunc,DispatchFunc465,DefaultDispatchFunc,DispatchFunc402,DefaultDispatchFunc,DispatchFunc403,DefaultDispatchFunc,DispatchFunc404,DefaultDispatchFunc,DispatchFunc405,DefaultDispatchFunc,DispatchFunc402
,DefaultDispatchFunc,DispatchFunc403,DefaultDispatchFunc,DispatchFunc404,DefaultDispatchFunc,DispatchFunc405,DefaultDispatchFunc,DispatchFunc466,DefaultDispatchFunc,DispatchFunc467,DefaultDispatchFunc,DispatchFunc468,DefaultDispatchFunc,DispatchFunc469,DefaultDispatchFunc,DispatchFunc466,DefaultDispatchFunc,DispatchFunc467,DefaultDispatchFunc,DispatchFunc468
,DefaultDispatchFunc,DispatchFunc469,DefaultDispatchFunc,DispatchFunc406,DefaultDispatchFunc,DispatchFunc407,DefaultDispatchFunc,DispatchFunc408,DefaultDispatchFunc,DispatchFunc409,DefaultDispatchFunc,DispatchFunc406,DefaultDispatchFunc,DispatchFunc407,DefaultDispatchFunc,DispatchFunc408,DefaultDispatchFunc,DispatchFunc409,DefaultDispatchFunc,DispatchFunc470
,DefaultDispatchFunc,DispatchFunc471,DefaultDispatchFunc,DispatchFunc472,DefaultDispatchFunc,DispatchFunc473,DefaultDispatchFunc,DispatchFunc470,DefaultDispatchFunc,DispatchFunc471,DefaultDispatchFunc,DispatchFunc472,DefaultDispatchFunc,DispatchFunc473,DefaultDispatchFunc,DispatchFunc410,DefaultDispatchFunc,DispatchFunc411,DefaultDispatchFunc,DispatchFunc412
,DefaultDispatchFunc,DispatchFunc413,DefaultDispatchFunc,DispatchFunc410,DefaultDispatchFunc,DispatchFunc411,DefaultDispatchFunc,DispatchFunc412,DefaultDispatchFunc,DispatchFunc413,DefaultDispatchFunc,DispatchFunc474,DefaultDispatchFunc,DispatchFunc475,DefaultDispatchFunc,DispatchFunc476,DefaultDispatchFunc,DispatchFunc477,DefaultDispatchFunc,DispatchFunc474
,DefaultDispatchFunc,DispatchFunc475,DefaultDispatchFunc,DispatchFunc476,DefaultDispatchFunc,DispatchFunc477,DefaultDispatchFunc,DispatchFunc414,DefaultDispatchFunc,DispatchFunc415,DefaultDispatchFunc,DispatchFunc416,DefaultDispatchFunc,DispatchFunc417,DefaultDispatchFunc,DispatchFunc414,DefaultDispatchFunc,DispatchFunc415,DefaultDispatchFunc,DispatchFunc416
,DefaultDispatchFunc,DispatchFunc417,DefaultDispatchFunc,DispatchFunc478,DefaultDispatchFunc,DispatchFunc479,DefaultDispatchFunc,DispatchFunc480,DefaultDispatchFunc,DispatchFunc481,DefaultDispatchFunc,DispatchFunc478,DefaultDispatchFunc,DispatchFunc479,DefaultDispatchFunc,DispatchFunc480,DefaultDispatchFunc,DispatchFunc481,DefaultDispatchFunc,DispatchFunc418
,DefaultDispatchFunc,DispatchFunc419,DefaultDispatchFunc,DispatchFunc420,DefaultDispatchFunc,DispatchFunc421,DefaultDispatchFunc,DispatchFunc418,DefaultDispatchFunc,DispatchFunc419,DefaultDispatchFunc,DispatchFunc420,DefaultDispatchFunc,DispatchFunc421,DefaultDispatchFunc,DispatchFunc482,DefaultDispatchFunc,DispatchFunc483,DefaultDispatchFunc,DispatchFunc484
,DefaultDispatchFunc,DispatchFunc485,DefaultDispatchFunc,DispatchFunc482,DefaultDispatchFunc,DispatchFunc483,DefaultDispatchFunc,DispatchFunc484,DefaultDispatchFunc,DispatchFunc485,DefaultDispatchFunc,DispatchFunc422,DefaultDispatchFunc,DispatchFunc423,DefaultDispatchFunc,DispatchFunc424,DefaultDispatchFunc,DispatchFunc425,DefaultDispatchFunc,DispatchFunc422
,DefaultDispatchFunc,DispatchFunc423,DefaultDispatchFunc,DispatchFunc424,DefaultDispatchFunc,DispatchFunc425,DefaultDispatchFunc,DispatchFunc486,DefaultDispatchFunc,DispatchFunc487,DefaultDispatchFunc,DispatchFunc488,DefaultDispatchFunc,DispatchFunc489,DefaultDispatchFunc,DispatchFunc486,DefaultDispatchFunc,DispatchFunc487,DefaultDispatchFunc,DispatchFunc488
,DefaultDispatchFunc,DispatchFunc489,DefaultDispatchFunc,DispatchFunc426,DefaultDispatchFunc,DispatchFunc427,DefaultDispatchFunc,DispatchFunc428,DefaultDispatchFunc,DispatchFunc429,DefaultDispatchFunc,DispatchFunc426,DefaultDispatchFunc,DispatchFunc427,DefaultDispatchFunc,DispatchFunc428,DefaultDispatchFunc,DispatchFunc429,DefaultDispatchFunc,DispatchFunc490
,DefaultDispatchFunc,DispatchFunc491,DefaultDispatchFunc,DispatchFunc492,DefaultDispatchFunc,DispatchFunc493,DefaultDispatchFunc,DispatchFunc490,DefaultDispatchFunc,DispatchFunc491,DefaultDispatchFunc,DispatchFunc492,DefaultDispatchFunc,DispatchFunc493,DefaultDispatchFunc,DispatchFunc430,DefaultDispatchFunc,DispatchFunc431,DefaultDispatchFunc,DispatchFunc432
,DefaultDispatchFunc,DispatchFunc433,DefaultDispatchFunc,DispatchFunc430,DefaultDispatchFunc,DispatchFunc431,DefaultDispatchFunc,DispatchFunc432,DefaultDispatchFunc,DispatchFunc433,DefaultDispatchFunc,DispatchFunc494,DefaultDispatchFunc,DispatchFunc495,DefaultDispatchFunc,DispatchFunc496,DefaultDispatchFunc,DispatchFunc497,DefaultDispatchFunc,DispatchFunc494
,DefaultDispatchFunc,DispatchFunc495,DefaultDispatchFunc,DispatchFunc496,DefaultDispatchFunc,DispatchFunc497,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498
,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc498,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499
,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DispatchFunc499,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc
,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DefaultDispatchFunc,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500,DispatchFunc500
};
        }
        #endregion Delegate Dispatcher
    }
}
