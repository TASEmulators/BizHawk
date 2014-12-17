         static const void *const op_goto_table[256] =
         {
            &&op_SLL, &&op_ILL, &&op_SRL, &&op_SRA, &&op_SLLV, &&op_ILL, &&op_SRLV, &&op_SRAV,
            &&op_JR, &&op_JALR, &&op_ILL, &&op_ILL, &&op_SYSCALL, &&op_BREAK, &&op_ILL, &&op_ILL,
            &&op_MFHI, &&op_MTHI, &&op_MFLO, &&op_MTLO, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_MULT, &&op_MULTU, &&op_DIV, &&op_DIVU, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_ADD, &&op_ADDU, &&op_SUB, &&op_SUBU, &&op_AND, &&op_OR, &&op_XOR, &&op_NOR,
            &&op_ILL, &&op_ILL, &&op_SLT, &&op_SLTU, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,

            NULL, &&op_BCOND, &&op_J, &&op_JAL, &&op_BEQ, &&op_BNE, &&op_BLEZ, &&op_BGTZ,
            &&op_ADDI, &&op_ADDIU, &&op_SLTI, &&op_SLTIU, &&op_ANDI, &&op_ORI, &&op_XORI, &&op_LUI,
            &&op_COP0, &&op_COP1, &&op_COP2, &&op_COP3, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_LB, &&op_LH, &&op_LWL, &&op_LW, &&op_LBU, &&op_LHU, &&op_LWR, &&op_ILL,
            &&op_SB, &&op_SH, &&op_SWL, &&op_SW, &&op_ILL, &&op_ILL, &&op_SWR, &&op_ILL,
            &&op_LWC0, &&op_LWC1, &&op_LWC2, &&op_LWC3, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,
            &&op_SWC0, &&op_SWC1, &&op_SWC2, &&op_SWC3, &&op_ILL, &&op_ILL, &&op_ILL, &&op_ILL,

            // Interrupt portion of this table is constructed so that an interrupt won't be taken when the PC is pointing to a GTE instruction,
            // to avoid problems caused by pipeline vs coprocessor nuances that aren't emulated.
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,

            NULL, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_COP2, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
            &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT, &&op_INTERRUPT,
         };

         goto *op_goto_table[opf];

         {
            BEGIN_OPF(ILL, 0, 0);
            PSX_WARNING("[CPU] Unknown instruction @%08x = %08x, op=%02x, funct=%02x", PC, instr, instr >> 26, (instr & 0x3F));
            DO_LDS();
            new_PC = Exception(EXCEPTION_RI, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // ADD - Add Word
            //
            BEGIN_OPF(ADD, 0, 0x20);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] + GPR[rt];
            bool ep = ((~(GPR[rs] ^ GPR[rt])) & (GPR[rs] ^ result)) & 0x80000000;

            DO_LDS();

            if(MDFN_UNLIKELY(ep))
            {
               new_PC = Exception(EXCEPTION_OV, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
               GPR[rd] = result;

            END_OPF;

            //
            // ADDI - Add Immediate Word
            //
            BEGIN_OPF(ADDI, 0x08, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] + immediate;
            bool ep = ((~(GPR[rs] ^ immediate)) & (GPR[rs] ^ result)) & 0x80000000;

            DO_LDS();

            if(MDFN_UNLIKELY(ep))
            {
               new_PC = Exception(EXCEPTION_OV, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
               GPR[rt] = result;

            END_OPF;

            //
            // ADDIU - Add Immediate Unsigned Word
            //
            BEGIN_OPF(ADDIU, 0x09, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] + immediate;

            DO_LDS();

            GPR[rt] = result;

            END_OPF;

            //
            // ADDU - Add Unsigned Word
            //
            BEGIN_OPF(ADDU, 0, 0x21);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] + GPR[rt];

            DO_LDS();

            GPR[rd] = result;

            END_OPF;

            //
            // AND - And
            //
            BEGIN_OPF(AND, 0, 0x24);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] & GPR[rt];

            DO_LDS();

            GPR[rd] = result;

            END_OPF;

            //
            // ANDI - And Immediate
            //
            BEGIN_OPF(ANDI, 0x0C, 0);
            ITYPE_ZE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] & immediate;

            DO_LDS();

            GPR[rt] = result;

            END_OPF;

            //
            // BEQ - Branch on Equal
            //
            BEGIN_OPF(BEQ, 0x04, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               bool result = (GPR[rs] == GPR[rt]);

            DO_LDS();

            if(result)
            {
               DO_BRANCH((immediate << 2), ~0U);
            }
            END_OPF;

            // Bah, why does MIPS encoding have to be funky like this. :(
            // Handles BGEZ, BGEZAL, BLTZ, BLTZAL
            BEGIN_OPF(BCOND, 0x01, 0);
            const uint32_t tv = GPR[(instr >> 21) & 0x1F];
            uint32_t riv = (instr >> 16) & 0x1F;
            	uint32 immediate = (int32)(int16)(instr & 0xFFFF);
            bool result = (int32)(tv ^ (riv << 31)) < 0;

            GPR_DEPRES_BEGIN
               GPR_DEP((instr >> 21) & 0x1F);

            if(riv & 0x10)
               GPR_RES(31);

            GPR_DEPRES_END


               DO_LDS();

            if(riv & 0x10)	// Unconditional link reg setting.
               GPR[31] = PC + 8;

            if(result)
            {
               DO_BRANCH((immediate << 2), ~0U);
            }

            END_OPF;


            //
            // BGTZ - Branch on Greater than Zero
            //
            BEGIN_OPF(BGTZ, 0x07, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               bool result = (int32)GPR[rs] > 0;

            DO_LDS();

            if(result)
            {
               DO_BRANCH((immediate << 2), ~0U);
            }
            END_OPF;

            //
            // BLEZ - Branch on Less Than or Equal to Zero
            //
            BEGIN_OPF(BLEZ, 0x06, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               bool result = (int32)GPR[rs] <= 0;

            DO_LDS();

            if(result)
            {
               DO_BRANCH((immediate << 2), ~0U);
            }

            END_OPF;

            //
            // BNE - Branch on Not Equal
            //
            BEGIN_OPF(BNE, 0x05, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               bool result = GPR[rs] != GPR[rt];

            DO_LDS();

            if(result)
            {
               DO_BRANCH((immediate << 2), ~0U);
            }

            END_OPF;

            //
            // BREAK - Breakpoint
            //
            BEGIN_OPF(BREAK, 0, 0x0D);
            PSX_WARNING("[CPU] BREAK BREAK BREAK BREAK DAAANCE -- PC=0x%08x", PC);

            DO_LDS();
            new_PC = Exception(EXCEPTION_BP, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            // Cop "instructions":	CFCz(no CP0), COPz, CTCz(no CP0), LWCz(no CP0), MFCz, MTCz, SWCz(no CP0)
            //
            // COP0 instructions
            BEGIN_OPF(COP0, 0x10, 0);
            uint32_t sub_op = (instr >> 21) & 0x1F;

            if(sub_op & 0x10)
               sub_op = 0x10 + (instr & 0x3F);

            //printf("COP0 thing: %02x\n", sub_op);
            switch(sub_op)
            {
               default:
                  DO_LDS();
                  break;

               case 0x00:		// MFC0	- Move from Coprocessor
                  {
                     uint32_t rt = (instr >> 16) & 0x1F;
                     uint32_t rd = (instr >> 11) & 0x1F;

                     //printf("MFC0: rt=%d <- rd=%d(%08x)\n", rt, rd, CP0.Regs[rd]);
                     DO_LDS();

                     LDAbsorb = 0;
                     LDWhich = rt;
                     LDValue = CP0.Regs[rd];
                  }
                  break;

               case 0x04:		// MTC0	- Move to Coprocessor
                  {
                     uint32_t rt = (instr >> 16) & 0x1F;
                     uint32_t rd = (instr >> 11) & 0x1F;
                     uint32_t val = GPR[rt];

                     if(rd != CP0REG_PRID && rd != CP0REG_CAUSE && rd != CP0REG_SR && val)
                     {
                        PSX_WARNING("[CPU] Unimplemented MTC0: rt=%d(%08x) -> rd=%d", rt, GPR[rt], rd);
                     }

                     switch(rd)
                     {
                        case CP0REG_BPC:
                           CP0.BPC = val;
                           break;

                        case CP0REG_BDA:
                           CP0.BDA = val;
                           break;

                        case CP0REG_TAR:
                           CP0.TAR = val;
                           break;

                        case CP0REG_DCIC:
                           CP0.DCIC = val & 0xFF80003F;
                           break;

                        case CP0REG_BDAM:
                           CP0.BDAM = val;
                           break;

                        case CP0REG_BPCM:
                           CP0.BPCM = val;
                           break;

                        case CP0REG_CAUSE:
                           CP0.CAUSE &= ~(0x3 << 8);
                           CP0.CAUSE |= val & (0x3 << 8);
                           RecalcIPCache();
                           break;

                        case CP0REG_SR:
                           if((CP0.SR ^ val) & 0x10000)
                              PSX_DBG(PSX_DBG_SPARSE, "[CPU] IsC %u->%u\n", (bool)(CP0.SR & (1U << 16)), (bool)(val & (1U << 16)));

                           CP0.SR = val & ~( (0x3 << 26) | (0x3 << 23) | (0x3 << 6));
                           RecalcIPCache();
                           break;
                     }
                  }
                  DO_LDS();
                  break;

               case (0x10 + 0x10):	// RFE
                  // "Pop"
                  DO_LDS();
                  CP0.SR = (CP0.SR & ~0x0F) | ((CP0.SR >> 2) & 0x0F);
                  RecalcIPCache();
                  break;
            }
            END_OPF;

            //
            // COP1
            //
            BEGIN_OPF(COP1, 0x11, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // COP2
            //
            BEGIN_OPF(COP2, 0x12, 0);
            uint32_t sub_op = (instr >> 21) & 0x1F;

            switch(sub_op)
            {
               default:
                  DO_LDS();
                  break;

               case 0x00:		// MFC2	- Move from Coprocessor
                  {
                     uint32_t rt = (instr >> 16) & 0x1F;
                     uint32_t rd = (instr >> 11) & 0x1F;

                     DO_LDS();

                     if(timestamp < gte_ts_done)
                     {
                        LDAbsorb = gte_ts_done - timestamp;
                        timestamp = gte_ts_done;
                     }
                     else
                        LDAbsorb = 0;

                     LDWhich = rt;
                     LDValue = GTE_ReadDR(rd);
                  }
                  break;

               case 0x04:		// MTC2	- Move to Coprocessor
                  {
                     uint32_t rt = (instr >> 16) & 0x1F;
                     uint32_t rd = (instr >> 11) & 0x1F;
                     uint32_t val = GPR[rt];

                     if(timestamp < gte_ts_done)
                        timestamp = gte_ts_done;

                     //printf("GTE WriteDR: %d %d\n", rd, val);
                     GTE_WriteDR(rd, val);
                     DO_LDS();
                  }
                  break;

               case 0x02:		// CFC2
                  {
                     uint32_t rt = (instr >> 16) & 0x1F;
                     uint32_t rd = (instr >> 11) & 0x1F;

                     DO_LDS();

                     if(timestamp < gte_ts_done)
                     {
                        LDAbsorb = gte_ts_done - timestamp;
                        timestamp = gte_ts_done;
                     }
                     else
                        LDAbsorb = 0;

                     LDWhich = rt;
                     LDValue = GTE_ReadCR(rd);

                     //printf("GTE ReadCR: %d %d\n", rd, GPR[rt]);
                  }		
                  break;

               case 0x06:		// CTC2
                  {
                     uint32_t rt = (instr >> 16) & 0x1F;
                     uint32_t rd = (instr >> 11) & 0x1F;
                     uint32_t val = GPR[rt];

                     //printf("GTE WriteCR: %d %d\n", rd, val);

                     if(timestamp < gte_ts_done)
                        timestamp = gte_ts_done;

                     GTE_WriteCR(rd, val);		 
                     DO_LDS();
                  }
                  break;

               case 0x10 ... 0x1F:
                  //printf("%08x\n", PC);
                  if(timestamp < gte_ts_done)
                     timestamp = gte_ts_done;
                  gte_ts_done = timestamp + GTE_Instruction(instr);
                  DO_LDS();
                  break;
            }
            END_OPF;

            //
            // COP3
            //
            BEGIN_OPF(COP3, 0x13, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // LWC0
            //
            BEGIN_OPF(LWC0, 0x30, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // LWC1
            //
            BEGIN_OPF(LWC1, 0x31, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // LWC2
            //
            BEGIN_OPF(LWC2, 0x32, 0);
            ITYPE;
            uint32_t address = GPR[rs] + immediate;

            DO_LDS();

            if(MDFN_UNLIKELY(address & 3))
            {
               new_PC = Exception(EXCEPTION_ADEL, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
            {
               if(timestamp < gte_ts_done)
                  timestamp = gte_ts_done;

               GTE_WriteDR(rt, ReadMemory<uint32>(timestamp, address, false, true));
            }
            // GTE stuff here
            END_OPF;

            //
            // LWC3
            //
            BEGIN_OPF(LWC3, 0x33, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;


            //
            // SWC0
            //
            BEGIN_OPF(SWC0, 0x38, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // SWC1
            //
            BEGIN_OPF(SWC1, 0x39, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;

            //
            // SWC2
            //
            BEGIN_OPF(SWC2, 0x3A, 0);
            ITYPE;
            uint32_t address = GPR[rs] + immediate;

            if(MDFN_UNLIKELY(address & 0x3))
            {
               new_PC = Exception(EXCEPTION_ADES, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
            {
               if(timestamp < gte_ts_done)
                  timestamp = gte_ts_done;

               WriteMemory<uint32>(timestamp, address, GTE_ReadDR(rt));
            }
            DO_LDS();
            END_OPF;

            //
            // SWC3
            ///
            BEGIN_OPF(SWC3, 0x3B, 0);
            DO_LDS();
            new_PC = Exception(EXCEPTION_RI, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;


            //
            // DIV - Divide Word
            //
            BEGIN_OPF(DIV, 0, 0x1A);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               if(!GPR[rt])
               {
                  if(GPR[rs] & 0x80000000)
                     LO = 1;
                  else
                     LO = 0xFFFFFFFF;

                  HI = GPR[rs];
               }
               else if(GPR[rs] == 0x80000000 && GPR[rt] == 0xFFFFFFFF)
               {
                  LO = 0x80000000;
                  HI = 0;
               }
               else
               {
                  LO = (int32)GPR[rs] / (int32)GPR[rt];
                  HI = (int32)GPR[rs] % (int32)GPR[rt];
               }
            muldiv_ts_done = timestamp + 37;

            DO_LDS();

            END_OPF;


            //
            // DIVU - Divide Unsigned Word
            //
            BEGIN_OPF(DIVU, 0, 0x1B);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               if(!GPR[rt])
               {
                  LO = 0xFFFFFFFF;
                  HI = GPR[rs];
               }
               else
               {
                  LO = GPR[rs] / GPR[rt];
                  HI = GPR[rs] % GPR[rt];
               }
            muldiv_ts_done = timestamp + 37;

            DO_LDS();
            END_OPF;

            //
            // J - Jump
            //
            BEGIN_OPF(J, 0x02, 0);
            JTYPE;

            DO_LDS();

            DO_BRANCH(target << 2, 0xF0000000);
            END_OPF;

            //
            // JAL - Jump and Link
            //
            BEGIN_OPF(JAL, 0x03, 0);
            JTYPE;

            //GPR_DEPRES_BEGIN
            GPR_RES(31);
            //GPR_DEPRES_END

            DO_LDS();

            GPR[31] = PC + 8;

            DO_BRANCH(target << 2, 0xF0000000);
            END_OPF;

            //
            // JALR - Jump and Link Register
            //
            BEGIN_OPF(JALR, 0, 0x09);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t tmp = GPR[rs];

            DO_LDS();

            GPR[rd] = PC + 8;

            DO_BRANCH(tmp, 0);

            END_OPF;

            //
            // JR - Jump Register
            //
            BEGIN_OPF(JR, 0, 0x08);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t bt = GPR[rs];

            DO_LDS();

            DO_BRANCH(bt, 0);

            END_OPF;

            //
            // LUI - Load Upper Immediate
            //
            BEGIN_OPF(LUI, 0x0F, 0);
            ITYPE_ZE;		// Actually, probably would be sign-extending...if we were emulating a 64-bit MIPS chip :b

            GPR_DEPRES_BEGIN
               GPR_RES(rt);
            GPR_DEPRES_END

               DO_LDS();

            GPR[rt] = immediate << 16;

            END_OPF;

            //
            // MFHI - Move from HI
            //
            BEGIN_OPF(MFHI, 0, 0x10);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_RES(rd);
            GPR_DEPRES_END

               DO_LDS();

            if(timestamp < muldiv_ts_done)
            {
               if(timestamp == muldiv_ts_done - 1)
                  muldiv_ts_done--;
               else
               {
                  do
                  {
                     if(ReadAbsorb[ReadAbsorbWhich])
                        ReadAbsorb[ReadAbsorbWhich]--;
                     timestamp++;
                  } while(timestamp < muldiv_ts_done);
               }
            }

            GPR[rd] = HI;

            END_OPF;


            //
            // MFLO - Move from LO
            //
            BEGIN_OPF(MFLO, 0, 0x12);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_RES(rd);
            GPR_DEPRES_END

               DO_LDS();

            if(timestamp < muldiv_ts_done)
            {
               if(timestamp == muldiv_ts_done - 1)
                  muldiv_ts_done--;
               else
               {
                  do
                  {
                     if(ReadAbsorb[ReadAbsorbWhich])
                        ReadAbsorb[ReadAbsorbWhich]--;
                     timestamp++;
                  } while(timestamp < muldiv_ts_done);
               }
            }

            GPR[rd] = LO;

            END_OPF;


            //
            // MTHI - Move to HI
            //
            BEGIN_OPF(MTHI, 0, 0x11);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               HI = GPR[rs];

            DO_LDS();

            END_OPF;

            //
            // MTLO - Move to LO
            //
            BEGIN_OPF(MTLO, 0, 0x13);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               LO = GPR[rs];

            DO_LDS();

            END_OPF;


            //
            // MULT - Multiply Word
            //
            BEGIN_OPF(MULT, 0, 0x18);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint64 result;

            result = (int64)(int32)GPR[rs] * (int32)GPR[rt];
            	muldiv_ts_done = timestamp + MULT_Tab24[__builtin_clz((GPR[rs] ^ ((int32)GPR[rs] >> 31)) | 0x400)];

            DO_LDS();

            LO = result;
            HI = result >> 32;

            END_OPF;

            //
            // MULTU - Multiply Unsigned Word
            //
            BEGIN_OPF(MULTU, 0, 0x19);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint64 result;

            result = (uint64)GPR[rs] * GPR[rt];
            muldiv_ts_done = timestamp + MULT_Tab24[__builtin_clz(GPR[rs] | 0x400)];

            DO_LDS();

            LO = result;
            HI = result >> 32;

            END_OPF;


            //
            // NOR - NOR
            //
            BEGIN_OPF(NOR, 0, 0x27);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = ~(GPR[rs] | GPR[rt]);

            DO_LDS();

            GPR[rd] = result;

            END_OPF;

            //
            // OR - OR
            //
            BEGIN_OPF(OR, 0, 0x25);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] | GPR[rt];

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // ORI - OR Immediate
            //
            BEGIN_OPF(ORI, 0x0D, 0);
            ITYPE_ZE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] | immediate;

            DO_LDS();

            GPR[rt] = result;

            END_OPF;


            //
            // SLL - Shift Word Left Logical
            //
            BEGIN_OPF(SLL, 0, 0x00);	// SLL
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rt] << shamt;

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SLLV - Shift Word Left Logical Variable
            //
            BEGIN_OPF(SLLV, 0, 0x04);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rt] << (GPR[rs] & 0x1F);

            DO_LDS();

            GPR[rd] = result;

            END_OPF;

            //
            // SLT - Set on Less Than
            //
            BEGIN_OPF(SLT, 0, 0x2A);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = (bool)((int32)GPR[rs] < (int32)GPR[rt]);

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SLTI - Set on Less Than Immediate
            //
            BEGIN_OPF(SLTI, 0x0A, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

	uint32 result = (bool)((int32)GPR[rs] < (int32)immediate);


            DO_LDS();

            GPR[rt] = result;

            END_OPF;


            //
            // SLTIU - Set on Less Than Immediate, Unsigned
            //
            BEGIN_OPF(SLTIU, 0x0B, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

               uint32_t result = (bool)(GPR[rs] < (uint32)immediate);

            DO_LDS();

            GPR[rt] = result;

            END_OPF;


            //
            // SLTU - Set on Less Than, Unsigned
            //
            BEGIN_OPF(SLTU, 0, 0x2B);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = (bool)(GPR[rs] < GPR[rt]);

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SRA - Shift Word Right Arithmetic
            //
            BEGIN_OPF(SRA, 0, 0x03);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = ((int32)GPR[rt]) >> shamt;

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SRAV - Shift Word Right Arithmetic Variable
            //
            BEGIN_OPF(SRAV, 0, 0x07);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = ((int32)GPR[rt]) >> (GPR[rs] & 0x1F);

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SRL - Shift Word Right Logical
            //
            BEGIN_OPF(SRL, 0, 0x02);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rt] >> shamt;

            DO_LDS();

            GPR[rd] = result;

            END_OPF;

            //
            // SRLV - Shift Word Right Logical Variable
            //
            BEGIN_OPF(SRLV, 0, 0x06);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rt] >> (GPR[rs] & 0x1F);

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SUB - Subtract Word
            //
            BEGIN_OPF(SUB, 0, 0x22);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] - GPR[rt];
            bool ep = (((GPR[rs] ^ GPR[rt])) & (GPR[rs] ^ result)) & 0x80000000;

            DO_LDS();

            if(MDFN_UNLIKELY(ep))
            {
               new_PC = Exception(EXCEPTION_OV, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
               GPR[rd] = result;

            END_OPF;


            //
            // SUBU - Subtract Unsigned Word
            //
            BEGIN_OPF(SUBU, 0, 0x23); // SUBU
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] - GPR[rt];

            DO_LDS();

            GPR[rd] = result;

            END_OPF;


            //
            // SYSCALL
            //
            BEGIN_OPF(SYSCALL, 0, 0x0C);
            DO_LDS();

            new_PC = Exception(EXCEPTION_SYSCALL, PC, new_PC_mask);
            new_PC_mask = 0;
            END_OPF;


            //
            // XOR
            //
            BEGIN_OPF(XOR, 0, 0x26);
            RTYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_RES(rd);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] ^ GPR[rt];

            DO_LDS();

            GPR[rd] = result;

            END_OPF;

            //
            // XORI - Exclusive OR Immediate
            //
            BEGIN_OPF(XORI, 0x0E, 0);
            ITYPE_ZE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_RES(rt);
            GPR_DEPRES_END

               uint32_t result = GPR[rs] ^ immediate;

            DO_LDS();

            GPR[rt] = result;
            END_OPF;

            //
            // Memory access instructions(besides the coprocessor ones) follow:
            //

            //
            // LB - Load Byte
            //
            BEGIN_OPF(LB, 0x20, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            DO_LDS();

            LDWhich = rt;
            LDValue = (int32)ReadMemory<int8>(timestamp, address);
            END_OPF;

            //
            // LBU - Load Byte Unsigned
            //
            BEGIN_OPF(LBU, 0x24, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            DO_LDS();

            LDWhich = rt;
            LDValue = ReadMemory<uint8>(timestamp, address);
            END_OPF;

            //
            // LH - Load Halfword
            //
            BEGIN_OPF(LH, 0x21, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            DO_LDS();

            if(MDFN_UNLIKELY(address & 1))
            {
               new_PC = Exception(EXCEPTION_ADEL, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
            {
               LDWhich = rt;
               LDValue = (int32)ReadMemory<int16>(timestamp, address);
            }
            END_OPF;

            //
            // LHU - Load Halfword Unsigned
            //
            BEGIN_OPF(LHU, 0x25, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            DO_LDS();

            if(MDFN_UNLIKELY(address & 1))
            {
               new_PC = Exception(EXCEPTION_ADEL, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
            {
               LDWhich = rt;
               LDValue = ReadMemory<uint16>(timestamp, address);
            }
            END_OPF;


            //
            // LW - Load Word
            //
            BEGIN_OPF(LW, 0x23, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            DO_LDS();

            if(MDFN_UNLIKELY(address & 3))
            {
               new_PC = Exception(EXCEPTION_ADEL, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
            {
               LDWhich = rt;
               LDValue = ReadMemory<uint32>(timestamp, address);
            }
            END_OPF;

            //
            // SB - Store Byte
            //
            BEGIN_OPF(SB, 0x28, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            WriteMemory<uint8>(timestamp, address, GPR[rt]);

            DO_LDS();
            END_OPF;

            // 
            // SH - Store Halfword
            //
            BEGIN_OPF(SH, 0x29, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            if(MDFN_UNLIKELY(address & 0x1))
            {
               new_PC = Exception(EXCEPTION_ADES, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
               WriteMemory<uint16>(timestamp, address, GPR[rt]);

            DO_LDS();
            END_OPF;

            // 
            // SW - Store Word
            //
            BEGIN_OPF(SW, 0x2B, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            if(MDFN_UNLIKELY(address & 0x3))
            {
               new_PC = Exception(EXCEPTION_ADES, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            else
               WriteMemory<uint32>(timestamp, address, GPR[rt]);

            DO_LDS();
            END_OPF;

            // LWL and LWR load delay slot tomfoolery appears to apply even to MFC0! (and probably MFCn and CFCn as well, though they weren't explicitly tested)

            //
            // LWL - Load Word Left
            //
            BEGIN_OPF(LWL, 0x22, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            //GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;
            uint32_t v = GPR[rt];

            if(LDWhich == rt)
            {
               v = LDValue;
               ReadFudge = 0;
            }
            else
            {
               DO_LDS();
            }

            LDWhich = rt;
            switch(address & 0x3)
            {
               case 0: LDValue = (v & ~(0xFF << 24)) | (ReadMemory<uint8>(timestamp, address & ~3) << 24);
                       break;

               case 1: LDValue = (v & ~(0xFFFF << 16)) | (ReadMemory<uint16>(timestamp, address & ~3) << 16);
                       break;

               case 2: LDValue = (v & ~(0xFFFFFF << 8)) | (ReadMemory<uint32>(timestamp, address & ~3, true) << 8);
                       break;

               case 3: LDValue = (v & ~(0xFFFFFFFF << 0)) | (ReadMemory<uint32>(timestamp, address & ~3) << 0);
                       break;
            }
            END_OPF;

            //
            // SWL - Store Word Left
            //
            BEGIN_OPF(SWL, 0x2A, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            switch(address & 0x3)
            {
               case 0: WriteMemory<uint8>(timestamp, address & ~3, GPR[rt] >> 24);
                       break;

               case 1: WriteMemory<uint16>(timestamp, address & ~3, GPR[rt] >> 16);
                       break;

               case 2: WriteMemory<uint32>(timestamp, address & ~3, GPR[rt] >> 8, true);
                       break;

               case 3: WriteMemory<uint32>(timestamp, address & ~3, GPR[rt] >> 0);
                       break;
            }
            DO_LDS();

            END_OPF;

            //
            // LWR - Load Word Right
            //
            BEGIN_OPF(LWR, 0x26, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            //GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;
            uint32_t v = GPR[rt];

            if(LDWhich == rt)
            {
               v = LDValue;
               ReadFudge = 0;
            }
            else
            {
               DO_LDS();
            }

            LDWhich = rt;
            switch(address & 0x3)
            {
               case 0:
                  LDValue = (v & ~(0xFFFFFFFF)) | ReadMemory<uint32>(timestamp, address);
                  break;
               case 1:
                  LDValue = (v & ~(0xFFFFFF)) | ReadMemory<uint32>(timestamp, address, true);
                  break;
               case 2:
                  LDValue = (v & ~(0xFFFF)) | ReadMemory<uint16>(timestamp, address);
                  break;

               case 3:
                  LDValue = (v & ~(0xFF)) | ReadMemory<uint8>(timestamp, address);
                  break;
            }
            END_OPF;

            //
            // SWR - Store Word Right
            //
            BEGIN_OPF(SWR, 0x2E, 0);
            ITYPE;

            GPR_DEPRES_BEGIN
               GPR_DEP(rs);
            GPR_DEP(rt);
            GPR_DEPRES_END

               uint32_t address = GPR[rs] + immediate;

            switch(address & 0x3)
            {
               case 0:
                  WriteMemory<uint32>(timestamp, address, GPR[rt]);
                  break;

               case 1:
                  WriteMemory<uint32>(timestamp, address, GPR[rt], true);
                  break;

               case 2:
                  WriteMemory<uint16>(timestamp, address, GPR[rt]);
                  break;

               case 3:
                  WriteMemory<uint8>(timestamp, address, GPR[rt]);
                  break;
            }

            DO_LDS();

            END_OPF;

            //
            // Mednafen special instruction
            //
            BEGIN_OPF(INTERRUPT, 0x3F, 0);
            if(Halted)
            {
               goto SkipNPCStuff;
            }
            else
            {
               DO_LDS();

               new_PC = Exception(EXCEPTION_INT, PC, new_PC_mask);
               new_PC_mask = 0;
            }
            END_OPF;
         }
