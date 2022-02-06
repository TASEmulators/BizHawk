#pragma once

//{
  enum set_flags {
    set_z = SLJIT_SET_Z,
    set_ult = SLJIT_SET_LESS,
    set_uge = SLJIT_SET_GREATER_EQUAL,
    set_ugt = SLJIT_SET_GREATER,
    set_ule = SLJIT_SET_LESS_EQUAL,
    set_slt = SLJIT_SET_SIG_LESS,
    set_sge = SLJIT_SET_SIG_GREATER_EQUAL,
    set_sgt = SLJIT_SET_SIG_GREATER,
    set_sle = SLJIT_SET_SIG_LESS_EQUAL,
    set_o = SLJIT_SET_OVERFLOW,
    set_c = SLJIT_SET_CARRY,
  };

  enum flags {
    flag_eq = SLJIT_EQUAL,
    flag_z = flag_eq,
    flag_ne = SLJIT_NOT_EQUAL,
    flag_nz = flag_ne,
    flag_ult = SLJIT_LESS,
    flag_uge = SLJIT_GREATER_EQUAL,
    flag_ugt = SLJIT_GREATER,
    flag_ule = SLJIT_LESS_EQUAL,
    flag_slt = SLJIT_SIG_LESS,
    flag_sge = SLJIT_SIG_GREATER_EQUAL,
    flag_sgt = SLJIT_SIG_GREATER,
    flag_sle = SLJIT_SIG_LESS_EQUAL,
    flag_o = SLJIT_OVERFLOW,
    flag_no = SLJIT_NOT_OVERFLOW,
  };

  struct op_base {
    op_base(sljit_s32 f, sljit_sw s) : fst(f), snd(s) {}
    sljit_s32 fst;
    sljit_sw snd;
  };

  struct imm : public op_base {
    explicit imm(sljit_sw immediate) : op_base(SLJIT_IMM, immediate) {}
  };

  struct reg : public op_base {
    explicit reg(sljit_s32 index) : op_base(SLJIT_R(index), 0) {}
  };

  struct sreg : public op_base {
    explicit sreg(sljit_s32 index) : op_base(SLJIT_S(index), 0) {}
  };

  struct mem : public op_base {
    mem(sreg base, sljit_sw offset) : op_base(SLJIT_MEM1(base.fst), offset) {}
  };

  struct unused : public op_base {
    unused() : op_base(SLJIT_UNUSED, 0) {}
  };
//};
