/*
 * readcpu.h - UAE CPU core
 *
 * This file is distributed under the GNU Public License, version 3 or at
 * your option any later version. Read the file GPLv3 for details.
 *
 */

#ifndef UAE_READCPU_H
#define UAE_READCPU_H

#include "sysdeps.h"


ENUMDECL {
  Dreg, Areg, Aind, Aipi, Apdi, Ad16, Ad8r,
  absw, absl, PC16, PC8r, imm, imm0, imm1, imm2, immi, am_unknown, am_illg
} ENUMNAME (amodes);

ENUMDECL {
    i_ILLG,

    i_OR, i_AND, i_EOR, i_ORSR, i_ANDSR, i_EORSR,
    i_SUB, i_SUBA, i_SUBX, i_SBCD,
    i_ADD, i_ADDA, i_ADDX, i_ABCD,
    i_NEG, i_NEGX, i_NBCD, i_CLR, i_NOT, i_TST,
    i_BTST, i_BCHG, i_BCLR, i_BSET,
    i_CMP, i_CMPM, i_CMPA,
    i_MVPRM, i_MVPMR, i_MOVE, i_MOVEA, i_MVSR2, i_MV2SR,
    i_SWAP, i_EXG, i_EXT, i_MVMEL, i_MVMLE,
    i_TRAP, i_MVR2USP, i_MVUSP2R, i_RESET, i_NOP, i_STOP, i_RTE, i_RTD,
    i_LINK, i_UNLK,
    i_RTS, i_TRAPV, i_RTR,
    i_JSR, i_JMP, i_BSR, i_Bcc,
    i_LEA, i_PEA, i_DBcc, i_Scc,
    i_DIVU, i_DIVS, i_MULU, i_MULS,
    i_ASR, i_ASL, i_LSR, i_LSL, i_ROL, i_ROR, i_ROXL, i_ROXR,
    i_ASRW, i_ASLW, i_LSRW, i_LSLW, i_ROLW, i_RORW, i_ROXLW, i_ROXRW,
    i_CHK,i_CHK2,
    i_MOVEC2, i_MOVE2C, i_CAS, i_CAS2, i_DIVL, i_MULL,
    i_BFTST,i_BFEXTU,i_BFCHG,i_BFEXTS,i_BFCLR,i_BFFFO,i_BFSET,i_BFINS,
    i_PACK, i_UNPK, i_TAS, i_BKPT, i_CALLM, i_RTM, i_TRAPcc, i_MOVES,
    i_FPP, i_FDBcc, i_FScc, i_FTRAPcc, i_FBcc, i_FSAVE, i_FRESTORE,
    i_CINVL, i_CINVP, i_CINVA, i_CPUSHL, i_CPUSHP, i_CPUSHA, i_MOVE16,
    i_MMUOP,

    MAX_OPCODE_FAMILY				/* should always be last of the list */
} ENUMNAME (instrmnem);

extern const struct mnemolookup {
    instrmnem mnemo;
    const char *name;
} lookuptab[];

ENUMDECL {
    sz_byte, sz_word, sz_long
} ENUMNAME (wordsizes);

ENUMDECL {
    fa_set, fa_unset, fa_zero, fa_one, fa_dontcare, fa_unknown, fa_isjmp,
    fa_isbranch
} ENUMNAME (flagaffect);

ENUMDECL {
    fu_used, fu_unused, fu_maybecc, fu_unknown, fu_isjmp
} ENUMNAME (flaguse);

ENUMDECL {
    bit0, bit1, bitc, bitC, bitf, biti, bitI, bitj, bitJ, bitk, bitK,
    bits, bitS, bitd, bitD, bitr, bitR, bitz, bitp, lastbit
} ENUMNAME (bitvals);

struct instr_def {
    unsigned int bits;
    int n_variable;
    char bitpos[16];
    unsigned int mask;
    int cpulevel;
    int plevel;
    struct {
	unsigned int flaguse:3;
	unsigned int flagset:3;
    } flaginfo[5];
    unsigned char sduse;
    const char *opcstr;
};

extern const struct instr_def defs68k[];
extern int n_defs68k;

extern struct instr {
    long int handler;
    unsigned char dreg;
    unsigned char sreg;
    signed char dpos;
    signed char spos;
    unsigned char sduse;
    int flagdead:8, flaglive:8;
    unsigned int mnemo:8;
    unsigned int cc:4;
    unsigned int plev:2;
    unsigned int size:2;
    unsigned int smode:5;
    unsigned int stype:3;
    unsigned int dmode:5;
    unsigned int suse:1;
    unsigned int duse:1;
    unsigned int unused1:1;
    unsigned int clev:3;
    unsigned int isjmp:1;
    unsigned int unused2:4;
} *table68k;

extern void read_table68k(void);
extern void do_merges(void);
extern int get_no_mismatches(void);
extern int nr_cpuop_funcs;

#endif /* ifndef UAE_READCPU_H */
