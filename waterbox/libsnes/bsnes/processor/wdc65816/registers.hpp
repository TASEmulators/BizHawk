#if !defined(WDC65816_REGISTERS_HPP)
  #define WDC65816_REGISTERS_HPP

  #define PC r.pc
  #define A  r.a
  #define X  r.x
  #define Y  r.y
  #define Z  r.z
  #define S  r.s
  #define D  r.d
  #define B  r.b
  #define P  r.p

  #define CF r.p.c
  #define ZF r.p.z
  #define IF r.p.i
  #define DF r.p.d
  #define XF r.p.x
  #define MF r.p.m
  #define VF r.p.v
  #define NF r.p.n
  #define EF r.e

  #define U r.u
  #define V r.v
  #define W r.w

  #define E if(r.e)
  #define N if(!r.e)
  #define L lastCycle();

  #define alu(...) (this->*op)(__VA_ARGS__)
#else
  #undef WDC65816_REGISTERS_HPP

  #undef PC
  #undef A
  #undef X
  #undef Y
  #undef Z
  #undef S
  #undef D
  #undef B
  #undef P

  #undef CF
  #undef ZF
  #undef IF
  #undef DF
  #undef XF
  #undef MF
  #undef VF
  #undef NF
  #undef EF

  #undef U
  #undef V
  #undef W

  #undef E
  #undef N
  #undef L

  #undef alu
#endif
