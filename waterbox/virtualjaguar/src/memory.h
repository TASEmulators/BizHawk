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

extern uint32_t & butch, & dscntrl;
extern uint16_t & ds_data;
extern uint32_t & i2cntrl, & sbcntrl, & subdata, & subdatb, & sb_time, & fifo_data, & i2sdat2, & unknown;

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

enum { UNKNOWN, JAGUAR, DSP, GPU, TOM, JERRY, M68K, BLITTER, OP, DEBUG };

// Some handy macros to help converting little endian to big endian (jaguar native)
// & vice versa

#define SET64(r, a, v)	*(uint64_t*)&r[(a)] = __builtin_bswap64((v))
#define GET64(r, a)		(__builtin_bswap64(*(uint64_t*)&r[(a)]))
#define SET32(r, a, v)	*(uint32_t*)&r[(a)] = __builtin_bswap32((v))
#define GET32(r, a)		(__builtin_bswap32(*(uint32_t*)&r[(a)]))
#define SET16(r, a, v)	*(uint16_t*)&r[(a)] = __builtin_bswap16((v))
#define GET16(r, a)		(__builtin_bswap16(*(uint16_t*)&r[(a)]))

#endif	// __MEMORY_H__
