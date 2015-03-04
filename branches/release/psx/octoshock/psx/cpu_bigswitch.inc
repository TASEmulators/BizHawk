         switch(opf)
         {
            case 0: /* SLL */
               {
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
               }
               break;
            case 1: /* ILL */
            case 5:
            case 10:
            case 11:
            case 14:
            case 15:
            case 20:
            case 21:
            case 22:
            case 23:
            case 28:
            case 29:
            case 30:
            case 31:
            case 40:
            case 41:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:
            case 84:
            case 85:
            case 86:
            case 87:
            case 88:
            case 89:
            case 90:
            case 91:
            case 92:
            case 93:
            case 94:
            case 95:
            case 103:
            case 108:
            case 109:
            case 111:
            case 116:
            case 117:
            case 118:
            case 119:
            case 124:
            case 125:
            case 126:
            case 127:
               {
                  BEGIN_OPF(ILL, 0, 0);
                  PSX_WARNING("[CPU] Unknown instruction @%08x = %08x, op=%02x, funct=%02x", PC, instr, instr >> 26, (instr & 0x3F));
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_RI, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 2: /* SRL */
               {
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
               }
               break;
            case 3: /* SRA */
               {
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
               }
               break;
            case 4: /* SLLV */
               {
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
               }
               break;
            case 6: /* SRLV */
               {
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
               }
               break;
            case 7: /* SRAV */
               {
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
               }
               break;
            case 8:  /* JR */
               {
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
               }
               break;
            case 9: /* JALR */
               {
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
               }
            case 12: /* SYSCALL */
               {
                  //
                  // SYSCALL
                  //
                  BEGIN_OPF(SYSCALL, 0, 0x0C);
                  DO_LDS();

                  new_PC = Exception(EXCEPTION_SYSCALL, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 13: /* BREAK */
               {
                  //
                  // BREAK - Breakpoint
                  //
                  BEGIN_OPF(BREAK, 0, 0x0D);
                  PSX_WARNING("[CPU] BREAK BREAK BREAK BREAK DAAANCE -- PC=0x%08x", PC);

                  DO_LDS();
                  new_PC = Exception(EXCEPTION_BP, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 16: 
               {
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

               }
               break;
            case 17:
               {
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
               }
               break;
            case 18:
               {
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
               }
               break;
            case 19:
               {
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
               }
               break;
            case 24: /* MULT */
               {
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
               }
               break;
            case 25: /* MULTU */
               {
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
               }
               break;
            case 26: /* DIV */
               {
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
               }
               break;
            case 27: /* DIVU */
               {
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
               }
               break;
            case 32: /* ADD */
               {
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
               }
               break;
            case 33: /* ADDU */
               {
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
               }
               break;
            case 34: /* SUB */
               {
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
               }
               break;
            case 35: /* SUBU */
               {
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
               }
               break;
            case 36: /* AND */
               {
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
               }
               break;

            case 37: /* OR */
               {
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
               }
               break;
            case 38: /* XOR */
               {
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
               }
               break;
            case 39: /* NOR */
               {
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
               }
               break;
            case 42: /* SLT */
               {
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
               }
               break;
            case 43: /* SLTU */
               {
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
               }
               break;
            case 65: /* BCOND */
               {
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
               }
               break;
            case 66: /* JUMP */
               {
                  //
                  // J - Jump
                  //
                  BEGIN_OPF(J, 0x02, 0);
                  JTYPE;

                  DO_LDS();

                  DO_BRANCH(target << 2, 0xF0000000);
                  END_OPF;
               }
               break;
            case 67: /* JAL */
               {
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
               }
               break;
            case 68: /* BEQ */
               {
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
               }
               break;
            case 69: /* BNE */
               {
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
               }
               break;
            case 70: /* BLEZ */
               {
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
               }
               break;
            case 71: /* BGTZ */
               {
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
               }
               break;
            case 72: /* ADDI */
               {
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
               }
               break;
            case 73: /* ADDIU */
               {
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
               }
               break;
            case 74: /* SLTI */
               {
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
               }
               break;
            case 75: /* SLTIU */
               {
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
               }
               break;
            case 76: /* ANDI */
               {
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
               }
               break;
            case 77: /* ORI */
               {
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
               }
               break;
            case 78: /* XORI */
               {
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
               }
               break;
            case 79: /* LUI */
               {
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
               }
               break;
            case 80: /* COP0 */
               {
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
               }
               break;
            case 81: /* COP1 */
               {
                  //
                  // COP1
                  //
                  BEGIN_OPF(COP1, 0x11, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 82: /* COP2 */
            case 210:
               {
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

										 case 0x10: case 0x11: case 0x12: case 0x13: case 0x14: case 0x15: case 0x16: case 0x17: 
										 case 0x18: case 0x19: case 0x1A: case 0x1B: case 0x1C: case 0x1D: case 0x1E: case 0x1F:
                        //printf("%08x\n", PC);
                        if(timestamp < gte_ts_done)
                           timestamp = gte_ts_done;
                        gte_ts_done = timestamp + GTE_Instruction(instr);
                        DO_LDS();
                        break;
                  }
                  END_OPF;
               }
               break;
            case 83: /* COP3 */
               {
                  //
                  // COP3
                  //
                  BEGIN_OPF(COP3, 0x13, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 96: /* LB */
               {
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
               }
               break;
            case 97: /* LH */
               {
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
               }
               break;
            case 98: /* LWL */
               {
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
               }
               break;
            case 99: /* LW */
               {
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
               }
               break;
            case 100: /* LBU */
               {
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
               }
               break;
            case 101: /* LHU */
               {
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
               }
               break;
            case 102: /* LWR */
               {
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
               }
               break;
            case 104: /* SB */
               {
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
               }
               break;
            case 105: /* SH */
               {
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
               }
               break;
            case 106: /* SWL */
               {
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
               }
               break;
            case 107: /* SW */
               {
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
               }
               break;
            case 110: /* SWR */
               {
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
               }
               break;
            case 112: /* LWC0 */
               {
                  //
                  // LWC0
                  //
                  BEGIN_OPF(LWC0, 0x30, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 113: /* LWC1 */
               {
                  //
                  // LWC1
                  //
                  BEGIN_OPF(LWC1, 0x31, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 114: /* LWC2 */
               {
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
               }
               break;
            case 115: /* LWC3 */
               {
                  //
                  // LWC3
                  //
                  BEGIN_OPF(LWC3, 0x33, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 120: /* SWC0 */
               {
                  //
                  // SWC0
                  //
                  BEGIN_OPF(SWC0, 0x38, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 121: /* SWC1 */
               {
                  //
                  // SWC1
                  //
                  BEGIN_OPF(SWC1, 0x39, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_COPU, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 122: /* SWC2 */
               {
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
               }
               break;
            case 123: /* SWC3 */
               {
                  //
                  // SWC3
                  ///
                  BEGIN_OPF(SWC3, 0x3B, 0);
                  DO_LDS();
                  new_PC = Exception(EXCEPTION_RI, PC, new_PC_mask);
                  new_PC_mask = 0;
                  END_OPF;
               }
               break;
            case 128:
            case 129:
            case 130:
            case 131:
            case 132:
            case 133:
            case 134:
            case 135:
            case 136:
            case 137:
            case 138:
            case 139:
            case 140:
            case 141:
            case 142:
            case 143:
            case 144:
            case 145:
            case 146:
            case 147:
            case 148:
            case 149:
            case 150:
            case 151:
            case 152:
            case 153:
            case 154:
            case 155:
            case 156:
            case 157:
            case 158:
            case 159:
            case 160:
            case 161:
            case 162:
            case 163:
            case 164:
            case 165:
            case 166:
            case 167:
            case 168:
            case 169:
            case 170:
            case 171:
            case 172:
            case 173:
            case 174:
            case 175:
            case 176:
            case 177:
            case 178:
            case 179:
            case 180:
            case 181:
            case 182:
            case 183:
            case 184:
            case 185:
            case 186:
            case 187:
            case 188:
            case 189:
            case 190:
            case 191: /* INTERRUPT */
            case 193:
            case 194:
            case 195:
            case 196:
            case 197:
            case 198:
            case 199:
            case 200:
            case 201:
            case 202:
            case 203:
            case 204:
            case 205:
            case 206:
            case 207:
            case 208:
            case 209:
            case 211:
            case 212:
            case 213:
            case 214:
            case 215:
            case 216:
            case 217:
            case 218:
            case 219:
            case 220:
            case 221:
            case 222:
            case 223:
            case 224:
            case 225:
            case 226:
            case 227:
            case 228:
            case 229:
            case 230:
            case 231:
            case 232:
            case 233:
            case 234:
            case 235:
            case 236:
            case 237:
            case 238:
            case 239:
            case 240:
            case 241:
            case 242:
            case 243:
            case 244:
            case 245:
            case 246:
            case 247:
            case 248:
            case 249:
            case 250:
            case 251:
            case 252:
            case 253:
            case 254:
            case 256:
               {
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
               break;
            default:
               break;
         }

