//
// MEMORY.H: Header file
//
// All Jaguar related memory and I/O locations are contained in this file
//

#ifndef __MEMORY_H__
#define __MEMORY_H__

#include <stdint.h>

extern uint8_t jagMemSpace[];

extern uint8_t * jaguarMainRAM;
extern uint8_t * jaguarMainROM;
extern uint8_t * gpuRAM;
extern uint8_t * dspRAM;

#if 1
extern uint32_t & butch, & dscntrl;
extern uint16_t & ds_data;
extern uint32_t & i2cntrl, & sbcntrl, & subdata, & subdatb, & sb_time, & fifo_data, & i2sdat2, & unknown;
#else
extern uint32_t butch, dscntrl, ds_data, i2cntrl, sbcntrl, subdata, subdatb, sb_time, fifo_data, i2sdat2, unknown;
#endif

extern uint16_t & memcon1, & memcon2, & hc, & vc, & lph, & lpv;
extern uint64_t & obData;
extern uint32_t & olp;
extern uint16_t & obf, & vmode, & bord1, & bord2, & hp, & hbb, & hbe, & hs,
	& hvs, & hdb1, & hdb2, & hde, & vp, & vbb, & vbe, & vs, & vdb, & vde,
	& veb, & vee, & vi, & pit0, & pit1, & heq;
extern uint32_t & bg;
extern uint16_t & int1, & int2;
extern uint8_t * clut, * lbuf;
extern uint32_t & g_flags, & g_mtxc, & g_mtxa, & g_end, & g_pc, & g_ctrl,
	& g_hidata, & g_divctrl;
extern uint32_t g_remain;
extern uint32_t & a1_base, & a1_flags, & a1_clip, & a1_pixel, & a1_step,
	& a1_fstep, & a1_fpixel, & a1_inc, & a1_finc, & a2_base, & a2_flags,
	& a2_mask, & a2_pixel, & a2_step, & b_cmd, & b_count;
extern uint64_t & b_srcd, & b_dstd, & b_dstz, & b_srcz1, & b_srcz2, & b_patd;
extern uint32_t & b_iinc, & b_zinc, & b_stop, & b_i3, & b_i2, & b_i1, & b_i0, & b_z3,
	& b_z2, & b_z1, & b_z0;
extern uint16_t & jpit1, & jpit2, & jpit3, & jpit4, & clk1, & clk2, & clk3, & j_int,
	& asidata, & asictrl;
extern uint16_t asistat;
extern uint16_t & asiclk, & joystick, & joybuts;
extern uint32_t & d_flags, & d_mtxc, & d_mtxa, & d_end, & d_pc, & d_ctrl,
	& d_mod, & d_divctrl;
extern uint32_t d_remain;
extern uint32_t & d_machi;
extern uint16_t & ltxd, lrxd, & rtxd, rrxd;
extern uint8_t & sclk, sstat;
extern uint32_t & smode;
/*
uint16_t & ltxd      = *((uint16_t *)&jagMemSpace[0xF1A148]);
uint16_t lrxd;									// Dual register with $F1A148
uint16_t & rtxd      = *((uint16_t *)&jagMemSpace[0xF1A14C]);
uint16_t rrxd;									// Dual register with $F1A14C
uint8_t  & sclk      = *((uint8_t *) &jagMemSpace[0xF1A150]);
uint8_t sstat;									// Dual register with $F1A150
uint32_t & smode     = *((uint32_t *)&jagMemSpace[0xF1A154]);
*/

// Read/write tracing enumeration

enum { UNKNOWN, JAGUAR, DSP, GPU, TOM, JERRY, M68K, BLITTER, OP, DEBUG };
extern const char * whoName[10];

// BIOS identification enum

//enum { BIOS_NORMAL=0x01, BIOS_CD=0x02, BIOS_STUB1=0x04, BIOS_STUB2=0x08, BIOS_DEV_CD=0x10 };
//extern int biosAvailable;

// Some handy macros to help converting native endian to big endian (jaguar native)
// & vice versa

#define SET64(r, a, v) 	r[(a)] = ((v) & 0xFF00000000000000) >> 56, r[(a)+1] = ((v) & 0x00FF000000000000) >> 48, \
						r[(a)+2] = ((v) & 0x0000FF0000000000) >> 40, r[(a)+3] = ((v) & 0x000000FF00000000) >> 32, \
						r[(a)+4] = ((v) & 0xFF000000) >> 24, r[(a)+5] = ((v) & 0x00FF0000) >> 16, \
						r[(a)+6] = ((v) & 0x0000FF00) >> 8, r[(a)+7] = (v) & 0x000000FF
#define GET64(r, a)		(((uint64_t)r[(a)] << 56) | ((uint64_t)r[(a)+1] << 48) | \
						((uint64_t)r[(a)+2] << 40) | ((uint64_t)r[(a)+3] << 32) | \
						((uint64_t)r[(a)+4] << 24) | ((uint64_t)r[(a)+5] << 16) | \
						((uint64_t)r[(a)+6] << 8) | (uint64_t)r[(a)+7])
#define SET32(r, a, v)	r[(a)] = ((v) & 0xFF000000) >> 24, r[(a)+1] = ((v) & 0x00FF0000) >> 16, \
						r[(a)+2] = ((v) & 0x0000FF00) >> 8, r[(a)+3] = (v) & 0x000000FF
#define GET32(r, a)		((r[(a)] << 24) | (r[(a)+1] << 16) | (r[(a)+2] << 8) | r[(a)+3])
#define SET16(r, a, v)	r[(a)] = ((v) & 0xFF00) >> 8, r[(a)+1] = (v) & 0xFF
#define GET16(r, a)		((r[(a)] << 8) | r[(a)+1])

//This doesn't seem to work on OSX. So have to figure something else out. :-(
//byteswap.h doesn't exist on OSX.
#if 0
// This is GCC specific, but we can fix that if we need to...
// Big plus of this approach is that these compile down to single instructions on little
// endian machines while one big endian machines we don't have any overhead. :-)

#include <byteswap.h>
#include <endian.h>

#if __BYTE_ORDER == __LITTLE_ENDIAN
	#define ESAFE16(x)	bswap_16(x)
	#define ESAFE32(x)	bswap_32(x)
	#define ESAFE64(x)	bswap_64(x)
#else
	#define ESAFE16(x)	(x)
	#define ESAFE32(x)	(x)
	#define ESAFE64(x)	(x)
#endif
#endif

#if 0
Stuff ripped out of Hatari, that may be useful:

/* Can the actual CPU access unaligned memory? */
#ifndef CPU_CAN_ACCESS_UNALIGNED
# if defined(__i386__) || defined(powerpc) || defined(__mc68020__)
#  define CPU_CAN_ACCESS_UNALIGNED 1
# else
#  define CPU_CAN_ACCESS_UNALIGNED 0
# endif
#endif


/* If the CPU can access unaligned memory, use these accelerated functions: */
#if CPU_CAN_ACCESS_UNALIGNED

#include <SDL_endian.h>


static inline uae_u32 do_get_mem_long(void *a)
{
	return SDL_SwapBE32(*(uae_u32 *)a);
}

static inline uae_u16 do_get_mem_word(void *a)
{
	return SDL_SwapBE16(*(uae_u16 *)a);
}


static inline void do_put_mem_long(void *a, uae_u32 v)
{
	*(uae_u32 *)a = SDL_SwapBE32(v);
}

static inline void do_put_mem_word(void *a, uae_u16 v)
{
	*(uae_u16 *)a = SDL_SwapBE16(v);
}


#else  /* Cpu can not access unaligned memory: */


static inline uae_u32 do_get_mem_long(void *a)
{
	uae_u8 *b = (uae_u8 *)a;

	return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
}

static inline uae_u16 do_get_mem_word(void *a)
{
	uae_u8 *b = (uae_u8 *)a;

	return (b[0] << 8) | b[1];
}


static inline void do_put_mem_long(void *a, uae_u32 v)
{
	uae_u8 *b = (uae_u8 *)a;

	b[0] = v >> 24;
	b[1] = v >> 16;    
	b[2] = v >> 8;
	b[3] = v;
}

static inline void do_put_mem_word(void *a, uae_u16 v)
{
	uae_u8 *b = (uae_u8 *)a;

	b[0] = v >> 8;
	b[1] = v;
}


#endif  /* CPU_CAN_ACCESS_UNALIGNED */


/* These are same for all architectures: */

static inline uae_u8 do_get_mem_byte(uae_u8 *a)
{
	return *a;
}

static inline void do_put_mem_byte(uae_u8 *a, uae_u8 v)
{
	*a = v;
}
#endif

#endif	// __MEMORY_H__
