//
// Inline functions used by cpuemu.c
//
// by Bernd Schmidt, Thomas Huth, and James Hammons
//
// Since inline functions have to be in a header, we have them all defined
// here, in one place to make finding them easy.
//

#ifndef __INLINES_H__
#define __INLINES_H__

#include "cpudefs.h"

STATIC_INLINE int cctrue(const int cc)
{
	switch (cc)
	{
		case 0:  return 1;                       /* T */
		case 1:  return 0;                       /* F */
		case 2:  return !CFLG && !ZFLG;          /* HI */
		case 3:  return CFLG || ZFLG;            /* LS */
		case 4:  return !CFLG;                   /* CC */
		case 5:  return CFLG;                    /* CS */
		case 6:  return !ZFLG;                   /* NE */
		case 7:  return ZFLG;                    /* EQ */
		case 8:  return !VFLG;                   /* VC */
		case 9:  return VFLG;                    /* VS */
		case 10: return !NFLG;                   /* PL */
		case 11: return NFLG;                    /* MI */
		case 12: return NFLG == VFLG;            /* GE */
		case 13: return NFLG != VFLG;            /* LT */
		case 14: return !ZFLG && (NFLG == VFLG); /* GT */
		case 15: return ZFLG || (NFLG != VFLG);  /* LE */
	}

	abort();
	return 0;
}

//no #define m68k_incpc(o) (regs.pc_p += (o))
#define m68k_incpc(o) (regs.pc += (o))

STATIC_INLINE void m68k_setpc(uint32_t newpc)
{
	//This is only done here... (get_real_address())
//	regs.pc_p = regs.pc_oldp = get_real_address(newpc);
	regs.pc = newpc;
}

#define m68k_setpc_rte  m68k_setpc

STATIC_INLINE uint32_t m68k_getpc(void)
{
//	return regs.pc + ((char *)regs.pc_p - (char *)regs.pc_oldp);
	return regs.pc;
}

#if 0
STATIC_INLINE uint32_t m68k_getpc_p(uint8_t * p)
{
	return regs.pc + ((char *)p - (char *)regs.pc_oldp);
}
#endif

STATIC_INLINE void m68k_setstopped(int stop)
{
	regs.stopped = stop;
	regs.remainingCycles = 0;

//But trace instructions are only on >68000 cpus, so this is bogus.
#if 0
	/* A traced STOP instruction drops through immediately without
	actually stopping.  */
	if (stop && (regs.spcflags & SPCFLAG_DOTRACE) == 0)
	regs.spcflags |= SPCFLAG_STOP;
#endif
}

STATIC_INLINE void m68k_do_rts(void)
{
	m68k_setpc(m68k_read_memory_32(m68k_areg(regs, 7)));
	m68k_areg(regs, 7) += 4;
}

STATIC_INLINE void m68k_do_bsr(uint32_t oldpc, int32_t offset)
{
	m68k_areg(regs, 7) -= 4;
	m68k_write_memory_32(m68k_areg(regs, 7), oldpc);
	m68k_incpc(offset);
}

STATIC_INLINE void m68k_do_jsr(uint32_t oldpc, uint32_t dest)
{
	m68k_areg(regs, 7) -= 4;
	m68k_write_memory_32(m68k_areg(regs, 7), oldpc);
	m68k_setpc(dest);
}

#if 0
//These do_get_mem_* functions are only used in newcpu...
//What it does is use a pointer to make instruction fetching quicker,
//though it probably leads to more problems than it solves. Something to
//decide using a profiler...
#define get_ibyte(o) do_get_mem_byte(regs.pc_p + (o) + 1)
#define get_iword(o) do_get_mem_word(regs.pc_p + (o))
#define get_ilong(o) do_get_mem_long(regs.pc_p + (o))
#else
// For now, we'll punt this crap...
// (Also, notice that the byte read is at address + 1...)
#define get_ibyte(o)	m68k_read_memory_8(regs.pc + (o) + 1)
#define get_iword(o)	m68k_read_memory_16(regs.pc + (o))
#define get_ilong(o)	m68k_read_memory_32(regs.pc + (o))
#endif

// We don't use this crap, so let's comment out for now...
STATIC_INLINE void refill_prefetch(uint32_t currpc, uint32_t offs)
{
#if 0
	uint32_t t = (currpc + offs) & ~1;
	int32_t pc_p_offs = t - currpc;
	uint8_t * ptr = regs.pc_p + pc_p_offs;
	uint32_t r;

#ifdef UNALIGNED_PROFITABLE
	r = *(uint32_t *)ptr;
	regs.prefetch = r;
#else
	r = do_get_mem_long(ptr);
	do_put_mem_long(&regs.prefetch, r);
#endif
	/* printf ("PC %lx T %lx PCPOFFS %d R %lx\n", currpc, t, pc_p_offs, r); */
	regs.prefetch_pc = t;
#endif
}

STATIC_INLINE uint32_t get_ibyte_prefetch(int32_t o)
{
#if 0
	uint32_t currpc = m68k_getpc();
	uint32_t addr = currpc + o + 1;
	uint32_t offs = addr - regs.prefetch_pc;

	if (offs > 3)
	{
		refill_prefetch(currpc, o + 1);
		offs = addr - regs.prefetch_pc;
	}

	uint32_t v = do_get_mem_byte(((uint8_t *)&regs.prefetch) + offs);

	if (offs >= 2)
		refill_prefetch(currpc, 2);

	/* printf ("get_ibyte PC %lx ADDR %lx OFFS %lx V %lx\n", currpc, addr, offs, v); */
	return v;
#else
	return get_ibyte(o);
#endif
}

STATIC_INLINE uint32_t get_iword_prefetch(int32_t o)
{
#if 0
	uint32_t currpc = m68k_getpc();
	uint32_t addr = currpc + o;
	uint32_t offs = addr - regs.prefetch_pc;

	if (offs > 3)
	{
		refill_prefetch(currpc, o);
		offs = addr - regs.prefetch_pc;
	}

	uint32_t v = do_get_mem_word(((uint8_t *)&regs.prefetch) + offs);

	if (offs >= 2)
		refill_prefetch(currpc, 2);

/*	printf ("get_iword PC %lx ADDR %lx OFFS %lx V %lx\n", currpc, addr, offs, v); */
	return v;
#else
	return get_iword(o);
#endif
}

STATIC_INLINE uint32_t get_ilong_prefetch(int32_t o)
{
#if 0
	uint32_t v = get_iword_prefetch(o);
	v <<= 16;
	v |= get_iword_prefetch(o + 2);
	return v;
#else
	return get_ilong(o);
#endif
}

STATIC_INLINE void fill_prefetch_0(void)
{
}

#define fill_prefetch_2 fill_prefetch_0

#endif	// __INLINES_H__
