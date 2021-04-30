auto GSU::instruction(uint8 opcode) -> void {
  #define op(id, name, ...) \
    case id: return instruction##name(__VA_ARGS__); \

  #define op4(id, name) \
    case id+ 0: return instruction##name((uint4)opcode); \
    case id+ 1: return instruction##name((uint4)opcode); \
    case id+ 2: return instruction##name((uint4)opcode); \
    case id+ 3: return instruction##name((uint4)opcode); \

  #define op6(id, name) \
    op4(id, name) \
    case id+ 4: return instruction##name((uint4)opcode); \
    case id+ 5: return instruction##name((uint4)opcode); \

  #define op12(id, name) \
    op6(id, name) \
    case id+ 6: return instruction##name((uint4)opcode); \
    case id+ 7: return instruction##name((uint4)opcode); \
    case id+ 8: return instruction##name((uint4)opcode); \
    case id+ 9: return instruction##name((uint4)opcode); \
    case id+10: return instruction##name((uint4)opcode); \
    case id+11: return instruction##name((uint4)opcode); \

  #define op15(id, name) \
    op12(id, name) \
    case id+12: return instruction##name((uint4)opcode); \
    case id+13: return instruction##name((uint4)opcode); \
    case id+14: return instruction##name((uint4)opcode); \

  #define op16(id, name) \
    op15(id, name) \
    case id+15: return instruction##name((uint4)opcode); \

  switch(opcode) {
  op  (0x00, STOP)
  op  (0x01, NOP)
  op  (0x02, CACHE)
  op  (0x03, LSR)
  op  (0x04, ROL)
  op  (0x05, Branch, 1)  //bra
  op  (0x06, Branch, (regs.sfr.s ^ regs.sfr.ov) == 0)  //blt
  op  (0x07, Branch, (regs.sfr.s ^ regs.sfr.ov) == 1)  //bge
  op  (0x08, Branch, regs.sfr.z == 0)  //bne
  op  (0x09, Branch, regs.sfr.z == 1)  //beq
  op  (0x0a, Branch, regs.sfr.s == 0)  //bpl
  op  (0x0b, Branch, regs.sfr.s == 1)  //bmi
  op  (0x0c, Branch, regs.sfr.cy == 0)  //bcc
  op  (0x0d, Branch, regs.sfr.cy == 1)  //bcs
  op  (0x0e, Branch, regs.sfr.ov == 0)  //bvc
  op  (0x0f, Branch, regs.sfr.ov == 1)  //bvs
  op16(0x10, TO_MOVE)
  op16(0x20, WITH)
  op12(0x30, Store)
  op  (0x3c, LOOP)
  op  (0x3d, ALT1)
  op  (0x3e, ALT2)
  op  (0x3f, ALT3)
  op12(0x40, Load)
  op  (0x4c, PLOT_RPIX)
  op  (0x4d, SWAP)
  op  (0x4e, COLOR_CMODE)
  op  (0x4f, NOT)
  op16(0x50, ADD_ADC)
  op16(0x60, SUB_SBC_CMP)
  op  (0x70, MERGE)
  op15(0x71, AND_BIC)
  op16(0x80, MULT_UMULT)
  op  (0x90, SBK)
  op4 (0x91, LINK)
  op  (0x95, SEX)
  op  (0x96, ASR_DIV2)
  op  (0x97, ROR)
  op6 (0x98, JMP_LJMP)
  op  (0x9e, LOB)
  op  (0x9f, FMULT_LMULT)
  op16(0xa0, IBT_LMS_SMS)
  op16(0xb0, FROM_MOVES)
  op  (0xc0, HIB)
  op15(0xc1, OR_XOR)
  op15(0xd0, INC)
  op  (0xdf, GETC_RAMB_ROMB)
  op15(0xe0, DEC)
  op  (0xef, GETB)
  op16(0xf0, IWT_LM_SM)
  }

  #undef op
  #undef op4
  #undef op6
  #undef op12
  #undef op15
  #undef op16
}
