//
// Common definitions for the UAE 68000 core
//
// by James Hammons
// (C) 2011 Underground Software
//
// This file is distributed under the GNU Public License, version 3 or at your
// option any later version. Read the file GPLv3 for details.
//

#ifndef __CPUDEFS_H__
#define __CPUDEFS_H__

#include "sysdeps.h"

/* Special flags */
#define SPCFLAG_DEBUGGER      0x001
#define SPCFLAG_STOP          0x002
#define SPCFLAG_BUSERROR      0x004
#define SPCFLAG_INT           0x008
#define SPCFLAG_BRK           0x010
#define SPCFLAG_EXTRA_CYCLES  0x020
#define SPCFLAG_TRACE         0x040
#define SPCFLAG_DOTRACE       0x080
#define SPCFLAG_DOINT         0x100
#define SPCFLAG_MFP           0x200
#define SPCFLAG_EXEC          0x400
#define SPCFLAG_MODE_CHANGE   0x800

struct regstruct
{
	uint32_t regs[16];
	uint32_t usp, isp;
	uint16_t sr;
	uint8_t s;
	uint8_t stopped;
	int intmask;
	int intLevel;

	unsigned int c;
	unsigned int z;
	unsigned int n;
	unsigned int v; 
	unsigned int x;

	uint32_t pc;
	uint8_t * pc_p;
	uint8_t * pc_oldp;

	uint32_t spcflags;

	uint32_t prefetch_pc;
	uint32_t prefetch;

	int32_t remainingCycles;
	uint32_t interruptCycles;
};

extern struct regstruct regs, lastint_regs;

#define m68k_dreg(r, num) ((r).regs[(num)])
#define m68k_areg(r, num) (((r).regs + 8)[(num)])

#define ZFLG (regs.z)
#define NFLG (regs.n)
#define CFLG (regs.c)
#define VFLG (regs.v)
#define XFLG (regs.x)

/* Possible exceptions sources for M68000_Exception() and Exception() */
#define M68000_EXC_SRC_CPU      1  /* Direct CPU exception */
#define M68000_EXC_SRC_AUTOVEC  2  /* Auto-vector exception (e.g. VBL) */
//#define M68000_EXC_SRC_INT_MFP  3  /* MFP interrupt exception */
//#define M68000_EXC_SRC_INT_DSP  4  /* DSP interrupt exception */

#define SET_CFLG(x) (CFLG = (x))
#define SET_NFLG(x) (NFLG = (x))
#define SET_VFLG(x) (VFLG = (x))
#define SET_ZFLG(x) (ZFLG = (x))
#define SET_XFLG(x) (XFLG = (x))

#define GET_CFLG CFLG
#define GET_NFLG NFLG
#define GET_VFLG VFLG
#define GET_ZFLG ZFLG
#define GET_XFLG XFLG

#define CLEAR_CZNV do { \
 SET_CFLG(0); \
 SET_ZFLG(0); \
 SET_NFLG(0); \
 SET_VFLG(0); \
} while (0)

#define COPY_CARRY (SET_XFLG(GET_CFLG))

#endif	// __CPUDEFS_H__
