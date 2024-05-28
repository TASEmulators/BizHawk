#pragma once

//{
  //0 operand instructions

  auto brk() {
    sljit_emit_op0(compiler, SLJIT_BREAKPOINT);
  }

  //1 operand instructions

#define OP1(name, op) \
  template<typename T, typename U> \
  auto name(T x, U y) { \
    sljit_emit_op1(compiler, \
                   SLJIT_##op, \
                   x.fst, x.snd, \
                   y.fst, y.snd); \
  }

  OP1(mov32, MOV32)
  OP1(mov64, MOV)
  OP1(mov32_u8, MOV32_U8)
  OP1(mov64_u8, MOV_U8)
  OP1(mov32_s8, MOV32_S8)
  OP1(mov64_s8, MOV_S8)
  OP1(mov32_u16, MOV32_U16)
  OP1(mov64_u16, MOV_U16)
  OP1(mov32_s16, MOV32_S16)
  OP1(mov64_s16, MOV_S16)
  OP1(mov64_u32, MOV_U32)
  OP1(mov64_s32, MOV_S32)
#undef OP1

  //2 operand instructions

#define OP2(name, op) \
  template<typename T, typename U, typename V> \
  auto name(T x, U y, V z, sljit_s32 flags = 0) { \
    sljit_emit_op2(compiler, \
                   SLJIT_##op | flags, \
                   x.fst, x.snd, \
                   y.fst, y.snd, \
                   z.fst, z.snd); \
  } \
  template<typename U, typename V> \
  auto name(unused, U y, V z, sljit_s32 flags = 0) { \
    sljit_emit_op2u(compiler, \
                   SLJIT_##op | flags, \
                   y.fst, y.snd, \
                   z.fst, z.snd); \
  }

  OP2(add32, ADD32)
  OP2(add64, ADD)
  OP2(addc32, ADDC32)
  OP2(addc64, ADDC)
  OP2(sub32, SUB32)
  OP2(sub64, SUB)
  OP2(subc32, SUBC32)
  OP2(subc64, SUBC)
  OP2(mul32, MUL32)
  OP2(mul64, MUL)
  OP2(and32, AND32)
  OP2(and64, AND)
  OP2(or32, OR32)
  OP2(or64, OR)
  OP2(xor32, XOR32)
  OP2(xor64, XOR)
  OP2(shl32, SHL32)
  OP2(shl64, SHL)
  OP2(mshl32, MSHL32)
  OP2(mshl64, MSHL)
  OP2(lshr32, LSHR32)
  OP2(lshr64, LSHR)
  OP2(mlshr32, MLSHR32)
  OP2(mlshr64, MLSHR)
  OP2(ashr32, ASHR32)
  OP2(ashr64, ASHR)
  OP2(mashr32, MASHR32)
  OP2(mashr64, MASHR)
  OP2(rotl32, ROTL32)
  OP2(rotl64, ROTL)
  OP2(rotr32, ROTR32)
  OP2(rotr64, ROTR)
#undef OP2

  //compare instructions

#define OPC(name, op) \
  template<typename T, typename U> \
  auto name(T x, U y, sljit_s32 flags) { \
    sljit_emit_op2u(compiler, \
                    SLJIT_##op | flags, \
                    x.fst, x.snd, \
                    y.fst, y.snd); \
  }

  OPC(cmp32, SUB32)
  OPC(cmp64, SUB)
  OPC(test32, AND32)
  OPC(test64, AND)
#undef OPC

  template<typename T, typename U>
  auto cmp32_jump(T x, U y, sljit_s32 flags) -> sljit_jump* {
    return sljit_emit_cmp(compiler,
                          SLJIT_32 | flags,
                          x.fst, x.snd,
                          y.fst, y.snd);
  }

  //flag instructions

#define OPF(name, op) \
  template<typename T> \
  auto name(T x, sljit_s32 flags) { \
    sljit_emit_op_flags(compiler, \
                        SLJIT_##op, \
                        x.fst, x.snd, \
                        flags); \
  }

  OPF(mov32_f, MOV32)
  OPF(mov64_f, MOV)
  OPF(and32_f, AND32)
  OPF(and64_f, AND)
  OPF(or32_f, OR32)
  OPF(or64_f, OR)
  OPF(xor32_f, XOR32)
  OPF(xor64_f, XOR)
#undef OPF

  //meta instructions


  auto lea(reg r, sreg base, sljit_sw offset) {
    add64(r, base, imm(offset));
  }
//};
