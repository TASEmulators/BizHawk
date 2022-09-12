//
// Blitter core
//
// by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -----------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

//
// I owe a debt of gratitude to Curt Vendel and to John Mathieson--to Curt
// for supplying the Oberon ASIC nets and to John for making them available
// to Curt. ;-) Without that excellent documentation which shows *exactly*
// what's going on inside the TOM chip, we'd all still be guessing as to how
// the wily blitter and other pieces of the Jaguar puzzle actually work.
//

#include "blitter.h"

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "jaguar.h"
#include "log.h"
//#include "memory.h"
#include "settings.h"

// Various conditional compilation goodies...

//#define LOG_BLITS

#define USE_ORIGINAL_BLITTER
//#define USE_MIDSUMMER_BLITTER
#define USE_MIDSUMMER_BLITTER_MKII

#ifdef USE_ORIGINAL_BLITTER
#ifdef USE_MIDSUMMER_BLITTER_MKII
#define USE_BOTH_BLITTERS
#endif
#endif


// External global variables

extern int jaguar_active_memory_dumps;

// Local global variables

int start_logging = 0;
uint8_t blitter_working = 0;
bool startConciseBlitLogging = false;
bool logBlit = false;

// Blitter register RAM (most of it is hidden from the user)

static uint8_t blitter_ram[0x100];

// Other crapola

bool specialLog = false;
extern int effect_start;
extern int blit_start_log;
void BlitterMidsummer(uint32_t cmd);
void BlitterMidsummer2(void);

#define REG(A)	(((uint32_t)blitter_ram[(A)] << 24) | ((uint32_t)blitter_ram[(A)+1] << 16) \
				| ((uint32_t)blitter_ram[(A)+2] << 8) | (uint32_t)blitter_ram[(A)+3])
#define WREG(A,D)	(blitter_ram[(A)] = ((D)>>24)&0xFF, blitter_ram[(A)+1] = ((D)>>16)&0xFF, \
					blitter_ram[(A)+2] = ((D)>>8)&0xFF, blitter_ram[(A)+3] = (D)&0xFF)

// Blitter registers (offsets from F02200)

#define A1_BASE			((uint32_t)0x00)
#define A1_FLAGS		((uint32_t)0x04)
#define A1_CLIP			((uint32_t)0x08)	// Height and width values for clipping
#define A1_PIXEL		((uint32_t)0x0C)	// Integer part of the pixel (Y.i and X.i)
#define A1_STEP			((uint32_t)0x10)	// Integer part of the step
#define A1_FSTEP		((uint32_t)0x14)	// Fractional part of the step
#define A1_FPIXEL		((uint32_t)0x18)	// Fractional part of the pixel (Y.f and X.f)
#define A1_INC			((uint32_t)0x1C)	// Integer part of the increment
#define A1_FINC			((uint32_t)0x20)	// Fractional part of the increment
#define A2_BASE			((uint32_t)0x24)
#define A2_FLAGS		((uint32_t)0x28)
#define A2_MASK			((uint32_t)0x2C)	// Modulo values for x and y (M.y  and M.x)
#define A2_PIXEL		((uint32_t)0x30)	// Integer part of the pixel (no fractional part for A2)
#define A2_STEP			((uint32_t)0x34)	// Integer part of the step (no fractional part for A2)
#define COMMAND			((uint32_t)0x38)
#define PIXLINECOUNTER	((uint32_t)0x3C)	// Inner & outer loop values
#define SRCDATA			((uint32_t)0x40)
#define DSTDATA			((uint32_t)0x48)
#define DSTZ			((uint32_t)0x50)
#define SRCZINT			((uint32_t)0x58)
#define SRCZFRAC		((uint32_t)0x60)
#define PATTERNDATA		((uint32_t)0x68)
#define INTENSITYINC	((uint32_t)0x70)
#define ZINC			((uint32_t)0x74)
#define COLLISIONCTRL	((uint32_t)0x78)
#define PHRASEINT0		((uint32_t)0x7C)
#define PHRASEINT1		((uint32_t)0x80)
#define PHRASEINT2		((uint32_t)0x84)
#define PHRASEINT3		((uint32_t)0x88)
#define PHRASEZ0		((uint32_t)0x8C)
#define PHRASEZ1		((uint32_t)0x90)
#define PHRASEZ2		((uint32_t)0x94)
#define PHRASEZ3		((uint32_t)0x98)

// Blitter command bits

#define SRCEN			(cmd & 0x00000001)
#define SRCENZ			(cmd & 0x00000002)
#define SRCENX			(cmd & 0x00000004)
#define DSTEN			(cmd & 0x00000008)
#define DSTENZ			(cmd & 0x00000010)
#define DSTWRZ			(cmd & 0x00000020)
#define CLIPA1			(cmd & 0x00000040)

#define UPDA1F			(cmd & 0x00000100)
#define UPDA1			(cmd & 0x00000200)
#define UPDA2			(cmd & 0x00000400)

#define DSTA2			(cmd & 0x00000800)

#define Z_OP_INF		(cmd & 0x00040000)
#define Z_OP_EQU		(cmd & 0x00080000)
#define Z_OP_SUP		(cmd & 0x00100000)

#define LFU_NAN			(cmd & 0x00200000)
#define LFU_NA			(cmd & 0x00400000)
#define LFU_AN			(cmd & 0x00800000)
#define LFU_A			(cmd & 0x01000000)

#define CMPDST			(cmd & 0x02000000)
#define BCOMPEN			(cmd & 0x04000000)
#define DCOMPEN			(cmd & 0x08000000)

#define PATDSEL			(cmd & 0x00010000)
#define ADDDSEL			(cmd & 0x00020000)
#define TOPBEN			(cmd & 0x00004000)
#define TOPNEN			(cmd & 0x00008000)
#define BKGWREN			(cmd & 0x10000000)
#define GOURD			(cmd & 0x00001000)
#define GOURZ			(cmd & 0x00002000)
#define SRCSHADE		(cmd & 0x40000000)


#define XADDPHR	 0
#define XADDPIX	 1
#define XADD0	 2
#define XADDINC	 3

#define XSIGNSUB_A1		(REG(A1_FLAGS)&0x080000)
#define XSIGNSUB_A2		(REG(A2_FLAGS)&0x080000)

#define YSIGNSUB_A1		(REG(A1_FLAGS)&0x100000)
#define YSIGNSUB_A2		(REG(A2_FLAGS)&0x100000)

#define YADD1_A1		(REG(A1_FLAGS)&0x040000)
#define YADD1_A2		(REG(A2_FLAGS)&0x040000)

/*******************************************************************************
********************** STUFF CUT BELOW THIS LINE! ******************************
*******************************************************************************/
#ifdef USE_ORIGINAL_BLITTER										// We're ditching this crap for now...

//Put 'em back, once we fix the problem!!! [KO]
// 1 bpp pixel read
#define PIXEL_SHIFT_1(a)      (((~a##_x) >> 16) & 7)
#define PIXEL_OFFSET_1(a)     (((((uint32_t)a##_y >> 16) * a##_width / 8) + (((uint32_t)a##_x >> 19) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 19) & 7))
#define READ_PIXEL_1(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_1(a), BLITTER) >> PIXEL_SHIFT_1(a)) & 0x01)
//#define READ_PIXEL_1(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_1(a)) >> PIXEL_SHIFT_1(a)) & 0x01)

// 2 bpp pixel read
#define PIXEL_SHIFT_2(a)      (((~a##_x) >> 15) & 6)
#define PIXEL_OFFSET_2(a)     (((((uint32_t)a##_y >> 16) * a##_width / 4) + (((uint32_t)a##_x >> 18) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 18) & 7))
#define READ_PIXEL_2(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_2(a), BLITTER) >> PIXEL_SHIFT_2(a)) & 0x03)
//#define READ_PIXEL_2(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_2(a)) >> PIXEL_SHIFT_2(a)) & 0x03)

// 4 bpp pixel read
#define PIXEL_SHIFT_4(a)      (((~a##_x) >> 14) & 4)
#define PIXEL_OFFSET_4(a)     (((((uint32_t)a##_y >> 16) * (a##_width/2)) + (((uint32_t)a##_x >> 17) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 17) & 7))
#define READ_PIXEL_4(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_4(a), BLITTER) >> PIXEL_SHIFT_4(a)) & 0x0f)
//#define READ_PIXEL_4(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_4(a)) >> PIXEL_SHIFT_4(a)) & 0x0f)

// 8 bpp pixel read
#define PIXEL_OFFSET_8(a)     (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 7))
#define READ_PIXEL_8(a)       (JaguarReadByte(a##_addr+PIXEL_OFFSET_8(a), BLITTER))
//#define READ_PIXEL_8(a)       (JaguarReadByte(a##_addr+PIXEL_OFFSET_8(a)))

// 16 bpp pixel read
#define PIXEL_OFFSET_16(a)    (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~3)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 3))
#define READ_PIXEL_16(a)       (JaguarReadWord(a##_addr+(PIXEL_OFFSET_16(a)<<1), BLITTER))
//#define READ_PIXEL_16(a)       (JaguarReadWord(a##_addr+(PIXEL_OFFSET_16(a)<<1)))

// 32 bpp pixel read
#define PIXEL_OFFSET_32(a)    (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~1)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 1))
#define READ_PIXEL_32(a)      (JaguarReadLong(a##_addr+(PIXEL_OFFSET_32(a)<<2), BLITTER))
//#define READ_PIXEL_32(a)      (JaguarReadLong(a##_addr+(PIXEL_OFFSET_32(a)<<2)))

// pixel read
#define READ_PIXEL(a,f) (\
	 (((f>>3)&0x07) == 0) ? (READ_PIXEL_1(a)) : \
	 (((f>>3)&0x07) == 1) ? (READ_PIXEL_2(a)) : \
	 (((f>>3)&0x07) == 2) ? (READ_PIXEL_4(a)) : \
	 (((f>>3)&0x07) == 3) ? (READ_PIXEL_8(a)) : \
	 (((f>>3)&0x07) == 4) ? (READ_PIXEL_16(a)) : \
	 (((f>>3)&0x07) == 5) ? (READ_PIXEL_32(a)) : 0)

// 16 bpp z data read
#define ZDATA_OFFSET_16(a)     (PIXEL_OFFSET_16(a) + a##_zoffs * 4)
#define READ_ZDATA_16(a)       (JaguarReadWord(a##_addr+(ZDATA_OFFSET_16(a)<<1), BLITTER))
//#define READ_ZDATA_16(a)       (JaguarReadWord(a##_addr+(ZDATA_OFFSET_16(a)<<1)))

// z data read
#define READ_ZDATA(a,f) (READ_ZDATA_16(a))

// 16 bpp z data write
#define WRITE_ZDATA_16(a,d)     {  JaguarWriteWord(a##_addr+(ZDATA_OFFSET_16(a)<<1), d, BLITTER); }
//#define WRITE_ZDATA_16(a,d)     {  JaguarWriteWord(a##_addr+(ZDATA_OFFSET_16(a)<<1), d); }

// z data write
#define WRITE_ZDATA(a,f,d) WRITE_ZDATA_16(a,d);

// 1 bpp r data read
#define READ_RDATA_1(r,a,p)  ((p) ?  ((REG(r+(((uint32_t)a##_x >> 19) & 0x04))) >> (((uint32_t)a##_x >> 16) & 0x1F)) & 0x0001 : (REG(r) & 0x0001))

// 2 bpp r data read
#define READ_RDATA_2(r,a,p)  ((p) ?  ((REG(r+(((uint32_t)a##_x >> 18) & 0x04))) >> (((uint32_t)a##_x >> 15) & 0x3E)) & 0x0003 : (REG(r) & 0x0003))

// 4 bpp r data read
#define READ_RDATA_4(r,a,p)  ((p) ?  ((REG(r+(((uint32_t)a##_x >> 17) & 0x04))) >> (((uint32_t)a##_x >> 14) & 0x28)) & 0x000F : (REG(r) & 0x000F))

// 8 bpp r data read
#define READ_RDATA_8(r,a,p)  ((p) ?  ((REG(r+(((uint32_t)a##_x >> 16) & 0x04))) >> (((uint32_t)a##_x >> 13) & 0x18)) & 0x00FF : (REG(r) & 0x00FF))

// 16 bpp r data read
#define READ_RDATA_16(r,a,p)  ((p) ? ((REG(r+(((uint32_t)a##_x >> 15) & 0x04))) >> (((uint32_t)a##_x >> 12) & 0x10)) & 0xFFFF : (REG(r) & 0xFFFF))

// 32 bpp r data read
#define READ_RDATA_32(r,a,p)  ((p) ? REG(r+(((uint32_t)a##_x >> 14) & 0x04)) : REG(r))

// register data read
#define READ_RDATA(r,a,f,p) (\
	 (((f>>3)&0x07) == 0) ? (READ_RDATA_1(r,a,p)) : \
	 (((f>>3)&0x07) == 1) ? (READ_RDATA_2(r,a,p)) : \
	 (((f>>3)&0x07) == 2) ? (READ_RDATA_4(r,a,p)) : \
	 (((f>>3)&0x07) == 3) ? (READ_RDATA_8(r,a,p)) : \
	 (((f>>3)&0x07) == 4) ? (READ_RDATA_16(r,a,p)) : \
	 (((f>>3)&0x07) == 5) ? (READ_RDATA_32(r,a,p)) : 0)

// 1 bpp pixel write
#define WRITE_PIXEL_1(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_1(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_1(a), BLITTER)&(~(0x01 << PIXEL_SHIFT_1(a))))|(d<<PIXEL_SHIFT_1(a)), BLITTER); }
//#define WRITE_PIXEL_1(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_1(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_1(a))&(~(0x01 << PIXEL_SHIFT_1(a))))|(d<<PIXEL_SHIFT_1(a))); }

// 2 bpp pixel write
#define WRITE_PIXEL_2(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_2(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_2(a), BLITTER)&(~(0x03 << PIXEL_SHIFT_2(a))))|(d<<PIXEL_SHIFT_2(a)), BLITTER); }
//#define WRITE_PIXEL_2(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_2(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_2(a))&(~(0x03 << PIXEL_SHIFT_2(a))))|(d<<PIXEL_SHIFT_2(a))); }

// 4 bpp pixel write
#define WRITE_PIXEL_4(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_4(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_4(a), BLITTER)&(~(0x0f << PIXEL_SHIFT_4(a))))|(d<<PIXEL_SHIFT_4(a)), BLITTER); }
//#define WRITE_PIXEL_4(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_4(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_4(a))&(~(0x0f << PIXEL_SHIFT_4(a))))|(d<<PIXEL_SHIFT_4(a))); }

// 8 bpp pixel write
#define WRITE_PIXEL_8(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_8(a), d, BLITTER); }
//#define WRITE_PIXEL_8(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_8(a), d); }

// 16 bpp pixel write
//#define WRITE_PIXEL_16(a,d)     {  JaguarWriteWord(a##_addr+(PIXEL_OFFSET_16(a)<<1),d); }
#define WRITE_PIXEL_16(a,d)     {  JaguarWriteWord(a##_addr+(PIXEL_OFFSET_16(a)<<1), d, BLITTER); if (specialLog) WriteLog("Pixel write address: %08X\n", a##_addr+(PIXEL_OFFSET_16(a)<<1)); }
//#define WRITE_PIXEL_16(a,d)     {  JaguarWriteWord(a##_addr+(PIXEL_OFFSET_16(a)<<1), d); if (specialLog) WriteLog("Pixel write address: %08X\n", a##_addr+(PIXEL_OFFSET_16(a)<<1)); }

// 32 bpp pixel write
#define WRITE_PIXEL_32(a,d)		{ JaguarWriteLong(a##_addr+(PIXEL_OFFSET_32(a)<<2), d, BLITTER); }
//#define WRITE_PIXEL_32(a,d)		{ JaguarWriteLong(a##_addr+(PIXEL_OFFSET_32(a)<<2), d); }

// pixel write
#define WRITE_PIXEL(a,f,d) {\
	switch ((f>>3)&0x07) { \
	case 0: WRITE_PIXEL_1(a,d);  break;  \
	case 1: WRITE_PIXEL_2(a,d);  break;  \
	case 2: WRITE_PIXEL_4(a,d);  break;  \
	case 3: WRITE_PIXEL_8(a,d);  break;  \
	case 4: WRITE_PIXEL_16(a,d); break;  \
	case 5: WRITE_PIXEL_32(a,d); break;  \
	}}

// Width in Pixels of a Scanline
// This is a pretranslation of the value found in the A1 & A2 flags: It's really a floating point value
// of the form EEEEMM where MM is the mantissa with an implied "1." in front of it and the EEEE value is
// the exponent. Valid values for the exponent range from 0 to 11 (decimal). It's easiest to think of it
// as a floating point bit pattern being followed by a number of zeroes. So, e.g., 001101 translates to
// 1.01 (the "1." being implied) x (2 ^ 3) or 1010 -> 10 in base 10 (i.e., 1.01 with the decimal place
// being shifted to the right 3 places).
/*static uint32_t blitter_scanline_width[48] =
{
     0,    0,    0,    0,					// Note: This would really translate to 1, 1, 1, 1
     2,    0,    0,    0,
     4,    0,    6,    0,
     8,   10,   12,   14,
    16,   20,   24,   28,
    32,   40,   48,   56,
    64,   80,   96,  112,
   128,  160,  192,  224,
   256,  320,  384,  448,
   512,  640,  768,  896,
  1024, 1280, 1536, 1792,
  2048, 2560, 3072, 3584
};//*/

//static uint8_t * tom_ram_8;
//static uint8_t * paletteRam;
static uint8_t src;
static uint8_t dst;
static uint8_t misc;
static uint8_t a1ctl;
static uint8_t mode;
static uint8_t ity;
static uint8_t zop;
static uint8_t op;
static uint8_t ctrl;
static uint32_t a1_addr;
static uint32_t a2_addr;
static int32_t a1_zoffs;
static int32_t a2_zoffs;
static uint32_t xadd_a1_control;
static uint32_t xadd_a2_control;
static int32_t a1_pitch;
static int32_t a2_pitch;
static uint32_t n_pixels;
static uint32_t n_lines;
static int32_t a1_x;
static int32_t a1_y;
static int32_t a1_width;
static int32_t a2_x;
static int32_t a2_y;
static int32_t a2_width;
static int32_t a2_mask_x;
static int32_t a2_mask_y;
static int32_t a1_xadd;
static int32_t a1_yadd;
static int32_t a2_xadd;
static int32_t a2_yadd;
static uint8_t a1_phrase_mode;
static uint8_t a2_phrase_mode;
static int32_t a1_step_x = 0;
static int32_t a1_step_y = 0;
static int32_t a2_step_x = 0;
static int32_t a2_step_y = 0;
static uint32_t outer_loop;
static uint32_t inner_loop;
static uint32_t a2_psize;
static uint32_t a1_psize;
static uint32_t gouraud_add;
//static uint32_t gouraud_data;
//static uint16_t gint[4];
//static uint16_t gfrac[4];
//static uint8_t  gcolour[4];
static int gd_i[4];
static int gd_c[4];
static int gd_ia, gd_ca;
static int colour_index = 0;
static int32_t zadd;
static uint32_t z_i[4];

static int32_t a1_clip_x, a1_clip_y;

// In the spirit of "get it right first, *then* optimize" I've taken the liberty
// of removing all the unnecessary code caching. If it turns out to be a good way
// to optimize the blitter, then we may revisit it in the future...

//
// Generic blit handler
//
void blitter_generic(uint32_t cmd)
{
/*
Blit! (0018FA70 <- 008DDC40) count: 2 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -2 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 100/12, A2 x/y: 106/0 Pattern: 000000F300000000
*/
//if (effect_start)
//	specialLog = true;
/*if (cmd == 0x1401060C && blit_start_log)
	specialLog = true;//*/
//Testing only!
//uint32_t logGo = ((cmd == 0x01800E01 && REG(A1_BASE) == 0x898000) ? 1 : 0);
	uint32_t srcdata, srczdata, dstdata, dstzdata, writedata, inhibit;
	uint32_t bppSrc = (DSTA2 ? 1 << ((REG(A1_FLAGS) >> 3) & 0x07) : 1 << ((REG(A2_FLAGS) >> 3) & 0x07));

if (specialLog)
{
	WriteLog("About to do n x m blit (BM width is ? pixels)...\n");
	WriteLog("A1_STEP_X/Y = %08X/%08X, A2_STEP_X/Y = %08X/%08X\n", a1_step_x, a1_step_y, a2_step_x, a2_step_y);
}
/*	if (BCOMPEN)
	{
		if (DSTA2)
			a1_xadd = 0;
		else
			a2_xadd = 0;
	}//*/

	while (outer_loop--)
	{
if (specialLog)
{
	WriteLog("  A1_X/Y = %08X/%08X, A2_X/Y = %08X/%08X\n", a1_x, a1_y, a2_x, a2_y);
}
		uint32_t a1_start = a1_x, a2_start = a2_x, bitPos = 0;

		//Kludge for Hover Strike...
		//I wonder if this kludge is in conjunction with the SRCENX down below...
		// This isn't so much a kludge but the way things work in BCOMPEN mode...!
		if (BCOMPEN && SRCENX)
		{
			if (n_pixels < bppSrc)
				bitPos = bppSrc - n_pixels;
		}

		inner_loop = n_pixels;
		while (inner_loop--)
		{
if (specialLog)
{
	WriteLog("    A1_X/Y = %08X/%08X, A2_X/Y = %08X/%08X\n", a1_x, a1_y, a2_x, a2_y);
}
			srcdata = srczdata = dstdata = dstzdata = writedata = inhibit = 0;

			if (!DSTA2)							// Data movement: A1 <- A2
			{
				// load src data and Z
//				if (SRCEN)
				if (SRCEN || SRCENX)	// Not sure if this is correct... (seems to be...!)
				{
					srcdata = READ_PIXEL(a2, REG(A2_FLAGS));

					if (SRCENZ)
						srczdata = READ_ZDATA(a2, REG(A2_FLAGS));
					else if (cmd & 0x0001C020)	// PATDSEL | TOPBEN | TOPNEN | DSTWRZ
						srczdata = READ_RDATA(SRCZINT, a2, REG(A2_FLAGS), a2_phrase_mode);
				}
				else	// Use SRCDATA register...
				{
					srcdata = READ_RDATA(SRCDATA, a2, REG(A2_FLAGS), a2_phrase_mode);

					if (cmd & 0x0001C020)		// PATDSEL | TOPBEN | TOPNEN | DSTWRZ
						srczdata = READ_RDATA(SRCZINT, a2, REG(A2_FLAGS), a2_phrase_mode);
				}

				// load dst data and Z
				if (DSTEN)
				{
					dstdata = READ_PIXEL(a1, REG(A1_FLAGS));

					if (DSTENZ)
						dstzdata = READ_ZDATA(a1, REG(A1_FLAGS));
					else
						dstzdata = READ_RDATA(DSTZ, a1, REG(A1_FLAGS), a1_phrase_mode);
				}
				else
				{
					dstdata = READ_RDATA(DSTDATA, a1, REG(A1_FLAGS), a1_phrase_mode);

					if (DSTENZ)
						dstzdata = READ_RDATA(DSTZ, a1, REG(A1_FLAGS), a1_phrase_mode);
				}

/*This wasn't working...				// a1 clipping
				if (cmd & 0x00000040)
				{
					if (a1_x < 0 || a1_y < 0 || (a1_x >> 16) >= (REG(A1_CLIP) & 0x7FFF)
						|| (a1_y >> 16) >= ((REG(A1_CLIP) >> 16) & 0x7FFF))
						inhibit = 1;
				}//*/

				if (GOURZ)
					srczdata = z_i[colour_index] >> 16;

				// apply z comparator
				if (Z_OP_INF && srczdata <  dstzdata)	inhibit = 1;
				if (Z_OP_EQU && srczdata == dstzdata)	inhibit = 1;
				if (Z_OP_SUP && srczdata >  dstzdata)	inhibit = 1;

				// apply data comparator
// Note: DCOMPEN only works in 8/16 bpp modes! !!! FIX !!!
// Does BCOMPEN only work in 1 bpp mode???
//   No, but it always does a 1 bit expansion no matter what the BPP of the channel is set to. !!! FIX !!!
//   This is bit tricky... We need to fix the XADD value so that it acts like a 1BPP value while inside
//   an 8BPP space.
				if (DCOMPEN | BCOMPEN)
				{
//Temp, for testing Hover Strike
//Doesn't seem to do it... Why?
//What needs to happen here is twofold. First, the address generator in the outer loop has
//to honor the BPP when calculating the start address (which it kinda does already). Second,
//it has to step bit by bit when using BCOMPEN. How to do this???
	if (BCOMPEN)
//small problem with this approach: it's not accurate... We need a proper address to begin with
//and *then* we can do the bit stepping from there the way it's *supposed* to be done... !!! FIX !!!
//[DONE]
	{
		uint32_t pixShift = (~bitPos) & (bppSrc - 1);
		srcdata = (srcdata >> pixShift) & 0x01;

		bitPos++;
//		if (bitPos % bppSrc == 0)
//			a2_x += 0x00010000;
	}
/*
Interesting (Hover Strike--large letter):

Blit! (0018FA70 <- 008DDC40) count: 2 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -2 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 100/12, A2 x/y: 106/0 Pattern: 000000F300000000

Blit! (0018FA70 <- 008DDC40) count: 8 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -8 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 102/12, A2 x/y: 107/0 Pattern: 000000F300000000

Blit! (0018FA70 <- 008DDC40) count: 1 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -1 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 118/12, A2 x/y: 70/0 Pattern: 000000F300000000

Blit! (0018FA70 <- 008DDC40) count: 8 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -8 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 119/12, A2 x/y: 71/0 Pattern: 000000F300000000

Blit! (0018FA70 <- 008DDC40) count: 1 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -1 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 127/12, A2 x/y: 66/0 Pattern: 000000F300000000

Blit! (0018FA70 <- 008DDC40) count: 8 x 13, A1/2_FLAGS: 00014218/00013C18 [cmd: 1401060C]
 CMD -> src: SRCENX dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl: BCOMPEN BKGWREN
  A1 step values: -8 (X), 1 (Y)
  A2 step values: -1 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 8bpp, z-off: 0, width: 192 (1E), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 128/12, A2 x/y: 67/0 Pattern: 000000F300000000
*/


					if (!CMPDST)
					{
//WriteLog("Blitter: BCOMPEN set on command %08X inhibit prev:%u, now:", cmd, inhibit);
						// compare source pixel with pattern pixel
/*
Blit! (000B8250 <- 0012C3A0) count: 16 x 1, A1/2_FLAGS: 00014420/00012000 [cmd: 05810001]
 CMD -> src: SRCEN  dst:  misc:  a1ctl:  mode:  ity: PATDSEL z-op:  op: LFU_REPLACE ctrl: BCOMPEN
  A1 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 384 (22), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 1bpp, z-off: 0, width: 16 (10), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        x/y: 0/20
...
*/
// AvP is still wrong, could be cuz it's doing A1 -> A2...

// Src is the 1bpp bitmap... DST is the PATTERN!!!
// This seems to solve at least ONE of the problems with MC3D...
// Why should this be inverted???
// Bcuz it is. This is supposed to be used only for a bit -> pixel expansion...
/*						if (srcdata == READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode))
//						if (srcdata != READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode))
							inhibit = 1;//*/
/*						uint32_t A2bpp = 1 << ((REG(A2_FLAGS) >> 3) & 0x07);
						if (A2bpp == 1 || A2bpp == 16 || A2bpp == 8)
							inhibit = (srcdata == 0 ? 1: 0);
//							inhibit = !srcdata;
						else
							WriteLog("Blitter: Bad BPP (%u) selected for BCOMPEN mode!\n", A2bpp);//*/
// What it boils down to is this:

						if (srcdata == 0)
							inhibit = 1;//*/
					}
					else
					{
						// compare destination pixel with pattern pixel
						if (dstdata == READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode))
//						if (dstdata != READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode))
							inhibit = 1;
					}

// This is DEFINITELY WRONG
//					if (a1_phrase_mode || a2_phrase_mode)
//						inhibit = !inhibit;
				}

				if (CLIPA1)
				{
					inhibit |= (((a1_x >> 16) < a1_clip_x && (a1_x >> 16) >= 0
						&& (a1_y >> 16) < a1_clip_y && (a1_y >> 16) >= 0) ? 0 : 1);
				}

				// compute the write data and store
				if (!inhibit)
				{
// Houston, we have a problem...
// Look here, at PATDSEL and GOURD. If both are active (as they are on the BIOS intro), then there's
// a conflict! E.g.:
//Blit! (00100000 <- 000095D0) count: 3 x 1, A1/2_FLAGS: 00014220/00004020 [cmd: 00011008]
// CMD -> src:  dst: DSTEN  misc:  a1ctl:  mode: GOURD  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
//  A1 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 320 (21), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
//  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 256 (20), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
//        A1 x/y: 90/171, A2 x/y: 808/0 Pattern: 776D770077007700

					if (PATDSEL)
					{
						// use pattern data for write data
						writedata = READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode);
					}
					else if (ADDDSEL)
					{
/*if (blit_start_log)
	WriteLog("BLIT: ADDDSEL srcdata: %08X\, dstdata: %08X, ", srcdata, dstdata);//*/

						// intensity addition
//Ok, this is wrong... Or is it? Yes, it's wrong! !!! FIX !!!
/*						writedata = (srcdata & 0xFF) + (dstdata & 0xFF);
						if (!(TOPBEN) && writedata > 0xFF)
//							writedata = 0xFF;
							writedata &= 0xFF;
						writedata |= (srcdata & 0xF00) + (dstdata & 0xF00);
						if (!(TOPNEN) && writedata > 0xFFF)
//							writedata = 0xFFF;
							writedata &= 0xFFF;
						writedata |= (srcdata & 0xF000) + (dstdata & 0xF000);//*/
//notneeded--writedata &= 0xFFFF;
/*if (blit_start_log)
	WriteLog("writedata: %08X\n", writedata);//*/
/*
Hover Strike ADDDSEL blit:

Blit! (00098D90 <- 0081DDC0) count: 320 x 287, A1/2_FLAGS: 00004220/00004020 [cmd: 00020208]
 CMD -> src:  dst: DSTEN  misc:  a1ctl: UPDA1  mode:  ity: ADDDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 step values: -320 (X), 1 (Y)
  A1 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 256 (20), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 0/0, A2 x/y: 3288/0 Pattern: 0000000000000000 SRCDATA: 00FD00FD00FD00FD
*/
						writedata = (srcdata & 0xFF) + (dstdata & 0xFF);

						if (!TOPBEN)
						{
//This is correct now, but slow...
							int16_t s = (srcdata & 0xFF) | (srcdata & 0x80 ? 0xFF00 : 0x0000),
								d = dstdata & 0xFF;
							int16_t sum = s + d;

							if (sum < 0)
								writedata = 0x00;
							else if (sum > 0xFF)
								writedata = 0xFF;
							else
								writedata = (uint32_t)sum;
						}

//This doesn't seem right... Looks like it would muck up the low byte... !!! FIX !!!
						writedata |= (srcdata & 0xF00) + (dstdata & 0xF00);

						if (!TOPNEN && writedata > 0xFFF)
						{
							writedata &= 0xFFF;
						}

						writedata |= (srcdata & 0xF000) + (dstdata & 0xF000);
					}
					else
					{
						if (LFU_NAN) writedata |= ~srcdata & ~dstdata;
						if (LFU_NA)  writedata |= ~srcdata & dstdata;
						if (LFU_AN)  writedata |= srcdata  & ~dstdata;
						if (LFU_A) 	 writedata |= srcdata  & dstdata;
					}

//Although, this looks like it's OK... (even if it is shitty!)
//According to JTRM, this is part of the four things the blitter does with the write data (the other
//three being PATDSEL, ADDDSEL, and LFU (default). I'm not sure which gets precedence, this or PATDSEL
//(see above blit example)...
					if (GOURD)
						writedata = ((gd_c[colour_index]) << 8) | (gd_i[colour_index] >> 16);

					if (SRCSHADE)
					{
						int intensity = srcdata & 0xFF;
						int ia = gd_ia >> 16;
						if (ia & 0x80)
							ia = 0xFFFFFF00 | ia;
						intensity += ia;
						if (intensity < 0)
							intensity = 0;
						if (intensity > 0xFF)
							intensity = 0xFF;
						writedata = (srcdata & 0xFF00) | intensity;
					}
				}
				else
				{
					writedata = dstdata;
					srczdata = dstzdata;
				}

//Tried 2nd below for Hover Strike: No dice.
				if (/*a1_phrase_mode || */BKGWREN || !inhibit)
//				if (/*a1_phrase_mode || BKGWREN ||*/ !inhibit)
				{
/*if (((REG(A1_FLAGS) >> 3) & 0x07) == 5)
{
	uint32_t offset = a1_addr+(PIXEL_OFFSET_32(a1)<<2);
// (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~1)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 1))
	if ((offset >= 0x1FF020 && offset <= 0x1FF03F) || (offset >= 0x1FF820 && offset <= 0x1FF83F))
		WriteLog("32bpp pixel write: A1 Phrase mode --> ");
}//*/
					// write to the destination
					WRITE_PIXEL(a1, REG(A1_FLAGS), writedata);
					if (DSTWRZ)
						WRITE_ZDATA(a1, REG(A1_FLAGS), srczdata);
				}
			}
			else	// if (DSTA2) 							// Data movement: A1 -> A2
			{
				// load src data and Z
				if (SRCEN)
				{
					srcdata = READ_PIXEL(a1, REG(A1_FLAGS));
					if (SRCENZ)
						srczdata = READ_ZDATA(a1, REG(A1_FLAGS));
					else if (cmd & 0x0001C020)	// PATDSEL | TOPBEN | TOPNEN | DSTWRZ
						srczdata = READ_RDATA(SRCZINT, a1, REG(A1_FLAGS), a1_phrase_mode);
				}
				else
				{
					srcdata = READ_RDATA(SRCDATA, a1, REG(A1_FLAGS), a1_phrase_mode);
					if (cmd & 0x001C020)	// PATDSEL | TOPBEN | TOPNEN | DSTWRZ
						srczdata = READ_RDATA(SRCZINT, a1, REG(A1_FLAGS), a1_phrase_mode);
				}

				// load dst data and Z
				if (DSTEN)
				{
					dstdata = READ_PIXEL(a2, REG(A2_FLAGS));
					if (DSTENZ)
						dstzdata = READ_ZDATA(a2, REG(A2_FLAGS));
					else
						dstzdata = READ_RDATA(DSTZ, a2, REG(A2_FLAGS), a2_phrase_mode);
				}
				else
				{
					dstdata = READ_RDATA(DSTDATA, a2, REG(A2_FLAGS), a2_phrase_mode);
					if (DSTENZ)
						dstzdata = READ_RDATA(DSTZ, a2, REG(A2_FLAGS), a2_phrase_mode);
				}

				if (GOURZ)
					srczdata = z_i[colour_index] >> 16;

				// apply z comparator
				if (Z_OP_INF && srczdata < dstzdata)	inhibit = 1;
				if (Z_OP_EQU && srczdata == dstzdata)	inhibit = 1;
				if (Z_OP_SUP && srczdata > dstzdata)	inhibit = 1;

				// apply data comparator
//NOTE: The bit comparator (BCOMPEN) is NOT the same at the data comparator!
				if (DCOMPEN | BCOMPEN)
				{
					if (!CMPDST)
					{
						// compare source pixel with pattern pixel
// AvP: Numbers are correct, but sprites are not!
//This doesn't seem to be a problem... But could still be wrong...
/*						if (srcdata == READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode))
//						if (srcdata != READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode))
							inhibit = 1;//*/
// This is probably not 100% correct... It works in the 1bpp case
// (in A1 <- A2 mode, that is...)
// AvP: This is causing blocks to be written instead of bit patterns...
// Works now...
// NOTE: We really should separate out the BCOMPEN & DCOMPEN stuff!
/*						uint32_t A1bpp = 1 << ((REG(A1_FLAGS) >> 3) & 0x07);
						if (A1bpp == 1 || A1bpp == 16 || A1bpp == 8)
							inhibit = (srcdata == 0 ? 1: 0);
						else
							WriteLog("Blitter: Bad BPP (%u) selected for BCOMPEN mode!\n", A1bpp);//*/
// What it boils down to is this:
						if (srcdata == 0)
							inhibit = 1;//*/
					}
					else
					{
						// compare destination pixel with pattern pixel
						if (dstdata == READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode))
//						if (dstdata != READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode))
							inhibit = 1;
					}

// This is DEFINITELY WRONG
//					if (a1_phrase_mode || a2_phrase_mode)
//						inhibit = !inhibit;
				}

				if (CLIPA1)
				{
					inhibit |= (((a1_x >> 16) < a1_clip_x && (a1_x >> 16) >= 0
						&& (a1_y >> 16) < a1_clip_y && (a1_y >> 16) >= 0) ? 0 : 1);
				}

				// compute the write data and store
				if (!inhibit)
				{
					if (PATDSEL)
					{
						// use pattern data for write data
						writedata = READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode);
					}
					else if (ADDDSEL)
					{
						// intensity addition
						writedata = (srcdata & 0xFF) + (dstdata & 0xFF);
						if (!(TOPBEN) && writedata > 0xFF)
							writedata = 0xFF;
						writedata |= (srcdata & 0xF00) + (dstdata & 0xF00);
						if (!(TOPNEN) && writedata > 0xFFF)
							writedata = 0xFFF;
						writedata |= (srcdata & 0xF000) + (dstdata & 0xF000);
					}
					else
					{
						if (LFU_NAN)
							writedata |= ~srcdata & ~dstdata;
						if (LFU_NA)
							writedata |= ~srcdata & dstdata;
						if (LFU_AN)
							writedata |= srcdata & ~dstdata;
						if (LFU_A)
							writedata |= srcdata & dstdata;
					}

					if (GOURD)
						writedata = ((gd_c[colour_index]) << 8) | (gd_i[colour_index] >> 16);

					if (SRCSHADE)
					{
						int intensity = srcdata & 0xFF;
						int ia = gd_ia >> 16;
						if (ia & 0x80)
							ia = 0xFFFFFF00 | ia;
						intensity += ia;
						if (intensity < 0)
							intensity = 0;
						if (intensity > 0xFF)
							intensity = 0xFF;
						writedata = (srcdata & 0xFF00) | intensity;
					}
				}
				else
				{
					writedata = dstdata;
					srczdata = dstzdata;
				}

				if (/*a2_phrase_mode || */BKGWREN || !inhibit)
				{
/*if (logGo)
{
	uint32_t offset = a2_addr+(PIXEL_OFFSET_16(a2)<<1);
// (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~1)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 1))
	WriteLog("[%08X:%04X] ", offset, writedata);
}//*/
					// write to the destination
					WRITE_PIXEL(a2, REG(A2_FLAGS), writedata);

					if (DSTWRZ)
						WRITE_ZDATA(a2, REG(A2_FLAGS), srczdata);
				}
			}

			// Update x and y (inner loop)
//Now it does! But crappy, crappy, crappy! !!! FIX !!! [DONE]
//This is less than ideal, but it works...
			if (!BCOMPEN)
			{//*/
				a1_x += a1_xadd, a1_y += a1_yadd;
				a2_x = (a2_x + a2_xadd) & a2_mask_x, a2_y = (a2_y + a2_yadd) & a2_mask_y;
			}
			else
			{
				a1_y += a1_yadd, a2_y = (a2_y + a2_yadd) & a2_mask_y;
				if (!DSTA2)
				{
					a1_x += a1_xadd;
					if (bitPos % bppSrc == 0)
						a2_x = (a2_x + a2_xadd) & a2_mask_x;
				}
				else
				{
					a2_x = (a2_x + a2_xadd) & a2_mask_x;
					if (bitPos % bppSrc == 0)
						a1_x += a1_xadd;
				}
			}//*/

			if (GOURZ)
				z_i[colour_index] += zadd;

			if (GOURD || SRCSHADE)
			{
				gd_i[colour_index] += gd_ia;
//Hmm, this doesn't seem to do anything...
//But it is correct according to the JTRM...!
if ((int32_t)gd_i[colour_index] < 0)
	gd_i[colour_index] = 0;
if (gd_i[colour_index] > 0x00FFFFFF)
	gd_i[colour_index] = 0x00FFFFFF;//*/

				gd_c[colour_index] += gd_ca;
if ((int32_t)gd_c[colour_index] < 0)
	gd_c[colour_index] = 0;
if (gd_c[colour_index] > 0x000000FF)
	gd_c[colour_index] = 0x000000FF;//*/
			}

			if (GOURD || SRCSHADE || GOURZ)
			{
				if (a1_phrase_mode)
//This screws things up WORSE (for the BIOS opening screen)
//				if (a1_phrase_mode || a2_phrase_mode)
					colour_index = (colour_index + 1) & 0x03;
			}
		}

/*
Here's the problem... The phrase mode code!
Blit! (00100000 -> 00148000) count: 327 x 267, A1/2_FLAGS: 00004420/00004420 [cmd: 41802E01]
 CMD -> src: SRCEN  dst:  misc:  a1ctl: UPDA1 UPDA2 mode: DSTA2 GOURZ ity:  z-op:  op: LFU_REPLACE ctrl: SRCSHADE
  A1 step values: -327 (X), 1 (Y)
  A2 step values: -327 (X), 1 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 384 (22), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 384 (22), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 28/58, A2 x/y: 28/58 Pattern: 00EA7BEA77EA77EA SRCDATA: 7BFF7BFF7BFF7BFF

Below fixes it, but then borks:
; O

Blit! (00110000 <- 0010B2A8) count: 12 x 12, A1/2_FLAGS: 000042E2/00000020 [cmd: 09800609]
 CMD -> src: SRCEN  dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity:  z-op:  op: LFU_REPLACE ctrl: DCOMPEN
  A1 step values: -15 (X), 1 (Y)
  A2 step values: -4 (X), 0 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 173/144, A2 x/y: 4052/0

Lesse, with pre-add we'd have:

     oooooooooooo
00001111222233334444555566667777
  ^  ^starts here...
  |             ^ends here.
  |rolls back to here. Hmm.

*/
//NOTE: The way to fix the CD BIOS is to uncomment below and comment the stuff after
//      the phrase mode mucking around. But it fucks up everything else...
//#define SCREWY_CD_DEPENDENT
#ifdef SCREWY_CD_DEPENDENT
		a1_x += a1_step_x;
		a1_y += a1_step_y;
		a2_x += a2_step_x;
		a2_y += a2_step_y;//*/
#endif

		//New: Phrase mode taken into account! :-p
/*		if (a1_phrase_mode)			// v1
		{
			// Bump the pointer to the next phrase boundary
			// Even though it works, this is crappy... Clean it up!
			uint32_t size = 64 / a1_psize;

			// Crappy kludge... ('aligning' source to destination)
			if (a2_phrase_mode && DSTA2)
			{
				uint32_t extra = (a2_start >> 16) % size;
				a1_x += extra << 16;
			}

			uint32_t newx = (a1_x >> 16) / size;
			uint32_t newxrem = (a1_x >> 16) % size;
			a1_x &= 0x0000FFFF;
			a1_x |= (((newx + (newxrem == 0 ? 0 : 1)) * size) & 0xFFFF) << 16;
		}//*/
		if (a1_phrase_mode)			// v2
		{
			// Bump the pointer to the next phrase boundary
			// Even though it works, this is crappy... Clean it up!
			uint32_t size = 64 / a1_psize;

			// Crappy kludge... ('aligning' source to destination)
			if (a2_phrase_mode && DSTA2)
			{
				uint32_t extra = (a2_start >> 16) % size;
				a1_x += extra << 16;
			}

			uint32_t pixelSize = (size - 1) << 16;
			a1_x = (a1_x + pixelSize) & ~pixelSize;
		}

/*		if (a2_phrase_mode)			// v1
		{
			// Bump the pointer to the next phrase boundary
			// Even though it works, this is crappy... Clean it up!
			uint32_t size = 64 / a2_psize;

			// Crappy kludge... ('aligning' source to destination)
			// Prolly should do this for A1 channel as well... [DONE]
			if (a1_phrase_mode && !DSTA2)
			{
				uint32_t extra = (a1_start >> 16) % size;
				a2_x += extra << 16;
			}

			uint32_t newx = (a2_x >> 16) / size;
			uint32_t newxrem = (a2_x >> 16) % size;
			a2_x &= 0x0000FFFF;
			a2_x |= (((newx + (newxrem == 0 ? 0 : 1)) * size) & 0xFFFF) << 16;
		}//*/
		if (a2_phrase_mode)			// v1
		{
			// Bump the pointer to the next phrase boundary
			// Even though it works, this is crappy... Clean it up!
			uint32_t size = 64 / a2_psize;

			// Crappy kludge... ('aligning' source to destination)
			// Prolly should do this for A1 channel as well... [DONE]
			if (a1_phrase_mode && !DSTA2)
			{
				uint32_t extra = (a1_start >> 16) % size;
				a2_x += extra << 16;
			}

			uint32_t pixelSize = (size - 1) << 16;
			a2_x = (a2_x + pixelSize) & ~pixelSize;
		}

		//Not entirely: This still mucks things up... !!! FIX !!!
		//Should this go before or after the phrase mode mucking around?
#ifndef SCREWY_CD_DEPENDENT
		a1_x += a1_step_x;
		a1_y += a1_step_y;
		a2_x += a2_step_x;
		a2_y += a2_step_y;//*/
#endif
	}

	// write values back to registers
	WREG(A1_PIXEL,  (a1_y & 0xFFFF0000) | ((a1_x >> 16) & 0xFFFF));
	WREG(A1_FPIXEL, (a1_y << 16) | (a1_x & 0xFFFF));
	WREG(A2_PIXEL,  (a2_y & 0xFFFF0000) | ((a2_x >> 16) & 0xFFFF));
specialLog = false;
}

void blitter_blit(uint32_t cmd)
{
//Apparently this is doing *something*, just not sure exactly what...
/*if (cmd == 0x41802E01)
{
	WriteLog("BLIT: Found our blit. Was: %08X ", cmd);
	cmd = 0x01800E01;
	WriteLog("Is: %08X\n", cmd);
}//*/

	uint32_t pitchValue[4] = { 0, 1, 3, 2 };
	colour_index = 0;
	src = cmd & 0x07;
	dst = (cmd >> 3) & 0x07;
	misc = (cmd >> 6) & 0x03;
	a1ctl = (cmd >> 8) & 0x7;
	mode = (cmd >> 11) & 0x07;
	ity = (cmd >> 14) & 0x0F;
	zop = (cmd >> 18) & 0x07;
	op = (cmd >> 21) & 0x0F;
	ctrl = (cmd >> 25) & 0x3F;

	// Addresses in A1/2_BASE are *phrase* aligned, i.e., bottom three bits are ignored!
	// NOTE: This fixes Rayman's bad collision detection AND keeps T2K working!
	a1_addr = REG(A1_BASE) & 0xFFFFFFF8;
	a2_addr = REG(A2_BASE) & 0xFFFFFFF8;

	a1_zoffs = (REG(A1_FLAGS) >> 6) & 7;
	a2_zoffs = (REG(A2_FLAGS) >> 6) & 7;

	xadd_a1_control = (REG(A1_FLAGS) >> 16) & 0x03;
	xadd_a2_control = (REG(A2_FLAGS) >> 16) & 0x03;

	a1_pitch = pitchValue[(REG(A1_FLAGS) & 0x03)];
	a2_pitch = pitchValue[(REG(A2_FLAGS) & 0x03)];

	n_pixels = REG(PIXLINECOUNTER) & 0xFFFF;
	n_lines = (REG(PIXLINECOUNTER) >> 16) & 0xFFFF;

	a1_x = (REG(A1_PIXEL) << 16) | (REG(A1_FPIXEL) & 0xFFFF);
	a1_y = (REG(A1_PIXEL) & 0xFFFF0000) | (REG(A1_FPIXEL) >> 16);
//According to the JTRM, X is restricted to 15 bits and Y is restricted to 12.
//But it seems to fuck up T2K! !!! FIX !!!
//Could it be sign extended??? Doesn't seem to be so according to JTRM
//	a1_x &= 0x7FFFFFFF, a1_y &= 0x0FFFFFFF;
//Actually, it says that the X is 16 bits. But it still seems to mess with the Y when restricted to 12...
//	a1_y &= 0x0FFFFFFF;

//	a1_width = blitter_scanline_width[((REG(A1_FLAGS) & 0x00007E00) >> 9)];
// According to JTRM, this must give a *whole number* of phrases in the current
// pixel size (this means the lookup above is WRONG)... !!! FIX !!!
	uint32_t m = (REG(A1_FLAGS) >> 9) & 0x03, e = (REG(A1_FLAGS) >> 11) & 0x0F;
	a1_width = ((0x04 | m) << e) >> 2;//*/

	a2_x = (REG(A2_PIXEL) & 0x0000FFFF) << 16;
	a2_y = (REG(A2_PIXEL) & 0xFFFF0000);
//According to the JTRM, X is restricted to 15 bits and Y is restricted to 12.
//But it seems to fuck up T2K! !!! FIX !!!
//	a2_x &= 0x7FFFFFFF, a2_y &= 0x0FFFFFFF;
//Actually, it says that the X is 16 bits. But it still seems to mess with the Y when restricted to 12...
//	a2_y &= 0x0FFFFFFF;

//	a2_width = blitter_scanline_width[((REG(A2_FLAGS) & 0x00007E00) >> 9)];
// According to JTRM, this must give a *whole number* of phrases in the current
// pixel size (this means the lookup above is WRONG)... !!! FIX !!!
	m = (REG(A2_FLAGS) >> 9) & 0x03, e = (REG(A2_FLAGS) >> 11) & 0x0F;
	a2_width = ((0x04 | m) << e) >> 2;//*/
	a2_mask_x = ((REG(A2_MASK) & 0x0000FFFF) << 16) | 0xFFFF;
	a2_mask_y = (REG(A2_MASK) & 0xFFFF0000) | 0xFFFF;

	// Check for "use mask" flag
	if (!(REG(A2_FLAGS) & 0x8000))
	{
		a2_mask_x = 0xFFFFFFFF; // must be 16.16
		a2_mask_y = 0xFFFFFFFF; // must be 16.16
	}

	a1_phrase_mode = 0;

	// According to the official documentation, a hardware bug ties A2's yadd bit to A1's...
	a2_yadd = a1_yadd = (YADD1_A1 ? 1 << 16 : 0);

	if (YSIGNSUB_A1)
		a1_yadd = -a1_yadd;

	// determine a1_xadd
	switch (xadd_a1_control)
	{
	case XADDPHR:
// This is a documented Jaguar bug relating to phrase mode and truncation... Look into it!
		// add phrase offset to X and truncate
		a1_xadd = 1 << 16;
		a1_phrase_mode = 1;
		break;
	case XADDPIX:
		// add pixelsize (1) to X
		a1_xadd = 1 << 16;
		break;
	case XADD0:
		// add zero (for those nice vertical lines)
		a1_xadd = 0;
		break;
	case XADDINC:
		// add the contents of the increment register
		a1_xadd = (REG(A1_INC) << 16)		 | (REG(A1_FINC) & 0x0000FFFF);
		a1_yadd = (REG(A1_INC) & 0xFFFF0000) | (REG(A1_FINC) >> 16);
		break;
	}


//Blit! (0011D000 -> 000B9600) count: 228 x 1, A1/2_FLAGS: 00073820/00064220 [cmd: 41802801]
//  A1 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 128 (1C), addctl: XADDINC YADD1 XSIGNADD YSIGNADD
//  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 320 (21), addctl: XADD0 YADD1 XSIGNADD YSIGNADD
//if (YADD1_A1 && YADD1_A2 && xadd_a2_control == XADD0 && xadd_a1_control == XADDINC)// &&
//	uint32_t a1f = REG(A1_FLAGS), a2f = REG(A2_FLAGS);
//Ok, so this ISN'T it... Prolly the XADDPHR code above that's doing it...
//if (REG(A1_FLAGS) == 0x00073820 && REG(A2_FLAGS) == 0x00064220 && cmd == 0x41802801)
//        A1 x/y: 14368/7, A2 x/y: 150/36
//This is it... The problem...
//if ((a1_x >> 16) == 14368) // 14368 = $3820
//	return; //Lesse what we got...

	if (XSIGNSUB_A1)
		a1_xadd = -a1_xadd;

	if (YSIGNSUB_A2)
		a2_yadd = -a2_yadd;

	a2_phrase_mode = 0;

	// determine a2_xadd
	switch (xadd_a2_control)
	{
	case XADDPHR:
		// add phrase offset to X and truncate
		a2_xadd = 1 << 16;
		a2_phrase_mode = 1;
		break;
	case XADDPIX:
		// add pixelsize (1) to X
		a2_xadd = 1 << 16;
		break;
	case XADD0:
		// add zero (for those nice vertical lines)
		a2_xadd = 0;
		break;
//This really isn't a valid bit combo for A2... Shouldn't this cause the blitter to just say no?
	case XADDINC:
WriteLog("BLIT: Asked to use invalid bit combo (XADDINC) for A2...\n");
		// add the contents of the increment register
		// since there is no register for a2 we just add 1
//Let's do nothing, since it's not listed as a valid bit combo...
//		a2_xadd = 1 << 16;
		break;
	}

	if (XSIGNSUB_A2)
		a2_xadd = -a2_xadd;

	// Modify outer loop steps based on blitter command

	a1_step_x = 0;
	a1_step_y = 0;
	a2_step_x = 0;
	a2_step_y = 0;

	if (UPDA1F)
		a1_step_x = (REG(A1_FSTEP) & 0xFFFF),
		a1_step_y = (REG(A1_FSTEP) >> 16);

	if (UPDA1)
		a1_step_x |= ((REG(A1_STEP) & 0x0000FFFF) << 16),
		a1_step_y |= ((REG(A1_STEP) & 0xFFFF0000));

	if (UPDA2)
		a2_step_x = (REG(A2_STEP) & 0x0000FFFF) << 16,
		a2_step_y = (REG(A2_STEP) & 0xFFFF0000);

	outer_loop = n_lines;

	// Clipping...

	if (CLIPA1)
		a1_clip_x = REG(A1_CLIP) & 0x7FFF,
		a1_clip_y = (REG(A1_CLIP) >> 16) & 0x7FFF;

// This phrase sizing is incorrect as well... !!! FIX !!! [NOTHING TO FIX]
// Err, this is pixel size... (and it's OK)
	a2_psize = 1 << ((REG(A2_FLAGS) >> 3) & 0x07);
	a1_psize = 1 << ((REG(A1_FLAGS) >> 3) & 0x07);

	// Z-buffering
	if (GOURZ)
	{
		zadd = REG(ZINC);

		for(int v=0; v<4; v++)
			z_i[v] = REG(PHRASEZ0 + v*4);
	}

	// Gouraud shading
	if (GOURD || GOURZ || SRCSHADE)
	{
		gd_c[0] = blitter_ram[PATTERNDATA + 6];
		gd_i[0]	= ((uint32_t)blitter_ram[PATTERNDATA + 7] << 16)
			| ((uint32_t)blitter_ram[SRCDATA + 6] << 8) | blitter_ram[SRCDATA + 7];

		gd_c[1] = blitter_ram[PATTERNDATA + 4];
		gd_i[1]	= ((uint32_t)blitter_ram[PATTERNDATA + 5] << 16)
			| ((uint32_t)blitter_ram[SRCDATA + 4] << 8) | blitter_ram[SRCDATA + 5];

		gd_c[2] = blitter_ram[PATTERNDATA + 2];
		gd_i[2]	= ((uint32_t)blitter_ram[PATTERNDATA + 3] << 16)
			| ((uint32_t)blitter_ram[SRCDATA + 2] << 8) | blitter_ram[SRCDATA + 3];

		gd_c[3] = blitter_ram[PATTERNDATA + 0];
		gd_i[3]	= ((uint32_t)blitter_ram[PATTERNDATA + 1] << 16)
			| ((uint32_t)blitter_ram[SRCDATA + 0] << 8) | blitter_ram[SRCDATA + 1];

		gouraud_add = REG(INTENSITYINC);

		gd_ia = gouraud_add & 0x00FFFFFF;
		if (gd_ia & 0x00800000)
			gd_ia = 0xFF000000 | gd_ia;

		gd_ca = (gouraud_add >> 24) & 0xFF;
		if (gd_ca & 0x00000080)
			gd_ca = 0xFFFFFF00 | gd_ca;
	}

	// Bit comparitor fixing...
/*	if (BCOMPEN)
	{
		// Determine the data flow direction...
		if (!DSTA2)
			a2_step_x /= (1 << ((REG(A2_FLAGS) >> 3) & 0x07));
		else
			;//add this later
	}//*/
/*	if (BCOMPEN)//Kludge for Hover Strike... !!! FIX !!!
	{
		// Determine the data flow direction...
		if (!DSTA2)
			a2_x <<= 3;
	}//*/

#ifdef LOG_BLITS
	if (start_logging)
	{
		WriteLog("Blit!\n");
		WriteLog("  cmd      = 0x%.8x\n",cmd);
		WriteLog("  a1_base  = %08X\n", a1_addr);
		WriteLog("  a1_pitch = %d\n", a1_pitch);
		WriteLog("  a1_psize = %d\n", a1_psize);
		WriteLog("  a1_width = %d\n", a1_width);
		WriteLog("  a1_xadd  = %f (phrase=%d)\n", (float)a1_xadd / 65536.0, a1_phrase_mode);
		WriteLog("  a1_yadd  = %f\n", (float)a1_yadd / 65536.0);
		WriteLog("  a1_xstep = %f\n", (float)a1_step_x / 65536.0);
		WriteLog("  a1_ystep = %f\n", (float)a1_step_y / 65536.0);
		WriteLog("  a1_x     = %f\n", (float)a1_x / 65536.0);
		WriteLog("  a1_y     = %f\n", (float)a1_y / 65536.0);
		WriteLog("  a1_zoffs = %i\n",a1_zoffs);

		WriteLog("  a2_base  = %08X\n", a2_addr);
		WriteLog("  a2_pitch = %d\n", a2_pitch);
		WriteLog("  a2_psize = %d\n", a2_psize);
		WriteLog("  a2_width = %d\n", a2_width);
		WriteLog("  a2_xadd  = %f (phrase=%d)\n", (float)a2_xadd / 65536.0, a2_phrase_mode);
		WriteLog("  a2_yadd  = %f\n", (float)a2_yadd / 65536.0);
		WriteLog("  a2_xstep = %f\n", (float)a2_step_x / 65536.0);
		WriteLog("  a2_ystep = %f\n", (float)a2_step_y / 65536.0);
		WriteLog("  a2_x     = %f\n", (float)a2_x / 65536.0);
		WriteLog("  a2_y     = %f\n", (float)a2_y / 65536.0);
		WriteLog("  a2_mask_x= 0x%.4x\n",a2_mask_x);
		WriteLog("  a2_mask_y= 0x%.4x\n",a2_mask_y);
		WriteLog("  a2_zoffs = %i\n",a2_zoffs);

		WriteLog("  count    = %d x %d\n", n_pixels, n_lines);

		WriteLog("  command  = %08X\n", cmd);
		WriteLog("  dsten    = %i\n",DSTEN);
		WriteLog("  srcen    = %i\n",SRCEN);
		WriteLog("  patdsel  = %i\n",PATDSEL);
		WriteLog("  color    = 0x%.8x\n",REG(PATTERNDATA));
		WriteLog("  dcompen  = %i\n",DCOMPEN);
		WriteLog("  bcompen  = %i\n",BCOMPEN);
		WriteLog("  cmpdst   = %i\n",CMPDST);
		WriteLog("  GOURZ   = %i\n",GOURZ);
		WriteLog("  GOURD   = %i\n",GOURD);
		WriteLog("  SRCSHADE= %i\n",SRCSHADE);
	}
#endif

//NOTE: Pitch is ignored!

//This *might* be the altimeter blits (they are)...
//On captured screen, x-pos for black (inner) is 259, for pink is 257
//Black is short by 3, pink is short by 1...
/*
Blit! (00110000 <- 000BF010) count: 9 x 31, A1/2_FLAGS: 000042E2/00010020 [cmd: 00010200]
 CMD -> src:  dst:  misc:  a1ctl: UPDA1  mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 262/124, A2 x/y: 128/0
Blit! (00110000 <- 000BF010) count: 5 x 38, A1/2_FLAGS: 000042E2/00010020 [cmd: 00010200]
 CMD -> src:  dst:  misc:  a1ctl: UPDA1  mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 264/117, A2 x/y: 407/0

Blit! (00110000 <- 000BF010) count: 9 x 23, A1/2_FLAGS: 000042E2/00010020 [cmd: 00010200]
 CMD -> src:  dst:  misc:  a1ctl: UPDA1  mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 step values: -10 (X), 1 (Y)
  A1 -> pitch: 4(2) phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1(0) phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 262/132, A2 x/y: 129/0
Blit! (00110000 <- 000BF010) count: 5 x 27, A1/2_FLAGS: 000042E2/00010020 [cmd: 00010200]
 CMD -> src:  dst:  misc:  a1ctl: UPDA1  mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 step values: -8 (X), 1 (Y)
  A1 -> pitch: 4(2) phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1(0) phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 264/128, A2 x/y: 336/0

  264v       vCursor ends up here...
     xxxxx...`
     111122223333

262v         vCursor ends up here...
   xxxxxxxxx.'
 1111222233334444

Fixed! Now for more:

; This looks like the ship icon in the upper left corner...

Blit! (00110000 <- 0010B2A8) count: 11 x 12, A1/2_FLAGS: 000042E2/00000020 [cmd: 09800609]
 CMD -> src: SRCEN  dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity:  z-op:  op: LFU_REPLACE ctrl: DCOMPEN
  A1 step values: -12 (X), 1 (Y)
  A2 step values: 0 (X), 0 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 20/24, A2 x/y: 5780/0

Also fixed!

More (not sure this is a blitter problem as much as it's a GPU problem):
All but the "M" are trashed...
This does *NOT* look like a blitter problem, as it's rendering properly...
Actually, if you look at the A1 step values, there IS a discrepancy!

; D

Blit! (00110000 <- 0010B2A8) count: 12 x 12, A1/2_FLAGS: 000042E2/00000020 [cmd: 09800609]
 CMD -> src: SRCEN  dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity:  z-op:  op: LFU_REPLACE ctrl: DCOMPEN
  A1 step values: -14 (X), 1 (Y)
  A2 step values: -4 (X), 0 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 134/144, A2 x/y: 2516/0
;129,146: +5,-2

; E

Blit! (00110000 <- 0010B2A8) count: 12 x 12, A1/2_FLAGS: 000042E2/00000020 [cmd: 09800609]
 CMD -> src: SRCEN  dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity:  z-op:  op: LFU_REPLACE ctrl: DCOMPEN
  A1 step values: -13 (X), 1 (Y)
  A2 step values: -4 (X), 0 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 147/144, A2 x/y: 2660/0

; M

Blit! (00110000 <- 0010B2A8) count: 12 x 12, A1/2_FLAGS: 000042E2/00000020 [cmd: 09800609]
 CMD -> src: SRCEN  dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity:  z-op:  op: LFU_REPLACE ctrl: DCOMPEN
  A1 step values: -12 (X), 1 (Y)
  A2 step values: 0 (X), 0 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 160/144, A2 x/y: 3764/0

; O

Blit! (00110000 <- 0010B2A8) count: 12 x 12, A1/2_FLAGS: 000042E2/00000020 [cmd: 09800609]
 CMD -> src: SRCEN  dst: DSTEN  misc:  a1ctl: UPDA1 UPDA2 mode:  ity:  z-op:  op: LFU_REPLACE ctrl: DCOMPEN
  A1 step values: -15 (X), 1 (Y)
  A2 step values: -4 (X), 0 (Y) [mask (unused): 00000000 - FFFFFFFF/FFFFFFFF]
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
        A1 x/y: 173/144, A2 x/y: 4052/0

*/
//extern int op_start_log;
if (blit_start_log)
{
	const char * ctrlStr[4] = { "XADDPHR\0", "XADDPIX\0", "XADD0\0", "XADDINC\0" };
	const char * bppStr[8] = { "1bpp\0", "2bpp\0", "4bpp\0", "8bpp\0", "16bpp\0", "32bpp\0", "???\0", "!!!\0" };
	const char * opStr[16] = { "LFU_CLEAR", "LFU_NSAND", "LFU_NSAD", "LFU_NOTS", "LFU_SAND", "LFU_NOTD", "LFU_N_SXORD", "LFU_NSORND",
		"LFU_SAD", "LFU_XOR", "LFU_D", "LFU_NSORD", "LFU_REPLACE", "LFU_SORND", "LFU_SORD", "LFU_ONE" };
	uint32_t /*src = cmd & 0x07, dst = (cmd >> 3) & 0x07, misc = (cmd >> 6) & 0x03,
		a1ctl = (cmd >> 8) & 0x07,*/ mode = (cmd >> 11) & 0x07/*, ity = (cmd >> 14) & 0x0F,
		zop = (cmd >> 18) & 0x07, op = (cmd >> 21) & 0x0F, ctrl = (cmd >> 25) & 0x3F*/;
	uint32_t a1f = REG(A1_FLAGS), a2f = REG(A2_FLAGS);
	uint32_t p1 = a1f & 0x07, p2 = a2f & 0x07,
		d1 = (a1f >> 3) & 0x07, d2 = (a2f >> 3) & 0x07,
		zo1 = (a1f >> 6) & 0x07, zo2 = (a2f >> 6) & 0x07,
		w1 = (a1f >> 9) & 0x3F, w2 = (a2f >> 9) & 0x3F,
		ac1 = (a1f >> 16) & 0x1F, ac2 = (a2f >> 16) & 0x1F;
	uint32_t iw1 = ((0x04 | (w1 & 0x03)) << ((w1 & 0x3C) >> 2)) >> 2;
	uint32_t iw2 = ((0x04 | (w2 & 0x03)) << ((w2 & 0x3C) >> 2)) >> 2;
	WriteLog("Blit! (%08X %s %08X) count: %d x %d, A1/2_FLAGS: %08X/%08X [cmd: %08X]\n", a1_addr, (mode&0x01 ? "->" : "<-"), a2_addr, n_pixels, n_lines, a1f, a2f, cmd);
//	WriteLog(" CMD -> src: %d, dst: %d, misc: %d, a1ctl: %d, mode: %d, ity: %1X, z-op: %d, op: %1X, ctrl: %02X\n", src, dst, misc, a1ctl, mode, ity, zop, op, ctrl);

	WriteLog(" CMD -> src: %s%s%s ", (cmd & 0x0001 ? "SRCEN " : ""), (cmd & 0x0002 ? "SRCENZ " : ""), (cmd & 0x0004 ? "SRCENX" : ""));
	WriteLog("dst: %s%s%s ", (cmd & 0x0008 ? "DSTEN " : ""), (cmd & 0x0010 ? "DSTENZ " : ""), (cmd & 0x0020 ? "DSTWRZ" : ""));
	WriteLog("misc: %s%s ", (cmd & 0x0040 ? "CLIP_A1 " : ""), (cmd & 0x0080 ? "???" : ""));
	WriteLog("a1ctl: %s%s%s ", (cmd & 0x0100 ? "UPDA1F " : ""), (cmd & 0x0200 ? "UPDA1 " : ""), (cmd & 0x0400 ? "UPDA2" : ""));
	WriteLog("mode: %s%s%s ", (cmd & 0x0800 ? "DSTA2 " : ""), (cmd & 0x1000 ? "GOURD " : ""), (cmd & 0x2000 ? "GOURZ" : ""));
	WriteLog("ity: %s%s%s%s ", (cmd & 0x4000 ? "TOPBEN " : ""), (cmd & 0x8000 ? "TOPNEN " : ""), (cmd & 0x00010000 ? "PATDSEL" : ""), (cmd & 0x00020000 ? "ADDDSEL" : ""));
	WriteLog("z-op: %s%s%s ", (cmd & 0x00040000 ? "ZMODELT " : ""), (cmd & 0x00080000 ? "ZMODEEQ " : ""), (cmd & 0x00100000 ? "ZMODEGT" : ""));
	WriteLog("op: %s ", opStr[(cmd >> 21) & 0x0F]);
	WriteLog("ctrl: %s%s%s%s%s%s\n", (cmd & 0x02000000 ? "CMPDST " : ""), (cmd & 0x04000000 ? "BCOMPEN " : ""), (cmd & 0x08000000 ? "DCOMPEN " : ""), (cmd & 0x10000000 ? "BKGWREN " : ""), (cmd & 0x20000000 ? "BUSHI " : ""), (cmd & 0x40000000 ? "SRCSHADE" : ""));

	if (UPDA1)
		WriteLog("  A1 step values: %d (X), %d (Y)\n", a1_step_x >> 16, a1_step_y >> 16);

	if (UPDA2)
		WriteLog("  A2 step values: %d (X), %d (Y) [mask (%sused): %08X - %08X/%08X]\n", a2_step_x >> 16, a2_step_y >> 16, (a2f & 0x8000 ? "" : "un"), REG(A2_MASK), a2_mask_x, a2_mask_y);

	WriteLog("  A1 -> pitch: %d phrases, depth: %s, z-off: %d, width: %d (%02X), addctl: %s %s %s %s\n", 1 << p1, bppStr[d1], zo1, iw1, w1, ctrlStr[ac1&0x03], (ac1&0x04 ? "YADD1" : "YADD0"), (ac1&0x08 ? "XSIGNSUB" : "XSIGNADD"), (ac1&0x10 ? "YSIGNSUB" : "YSIGNADD"));
	WriteLog("  A2 -> pitch: %d phrases, depth: %s, z-off: %d, width: %d (%02X), addctl: %s %s %s %s\n", 1 << p2, bppStr[d2], zo2, iw2, w2, ctrlStr[ac2&0x03], (ac2&0x04 ? "YADD1" : "YADD0"), (ac2&0x08 ? "XSIGNSUB" : "XSIGNADD"), (ac2&0x10 ? "YSIGNSUB" : "YSIGNADD"));
	WriteLog("        A1 x/y: %d/%d, A2 x/y: %d/%d Pattern: %08X%08X SRCDATA: %08X%08X\n", a1_x >> 16, a1_y >> 16, a2_x >> 16, a2_y >> 16, REG(PATTERNDATA), REG(PATTERNDATA + 4), REG(SRCDATA), REG(SRCDATA + 4));
//	blit_start_log = 0;
//	op_start_log = 1;
}

	blitter_working = 1;
//#ifndef USE_GENERIC_BLITTER
//	if (!blitter_execute_cached_code(blitter_in_cache(cmd)))
//#endif
	blitter_generic(cmd);

/*if (blit_start_log)
{
	if (a1_addr == 0xF03000 && a2_addr == 0x004D58)
	{
		WriteLog("\nBytes at 004D58:\n");
		for(int i=0x004D58; i<0x004D58+(10*127*4); i++)
			WriteLog("%02X ", JaguarReadByte(i));
		WriteLog("\nBytes at F03000:\n");
		for(int i=0xF03000; i<0xF03000+(6*127*4); i++)
			WriteLog("%02X ", JaguarReadByte(i));
		WriteLog("\n\n");
	}
}//*/

	blitter_working = 0;
}
#endif											// of the #if 0 near the top...
/*******************************************************************************
********************** STUFF CUT ABOVE THIS LINE! ******************************
*******************************************************************************/


void BlitterInit(void)
{
	BlitterReset();
}


void BlitterReset(void)
{
	memset(blitter_ram, 0x00, 0xA0);
}


void BlitterDone(void)
{
	WriteLog("BLIT: Done.\n");
}


uint8_t BlitterReadByte(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	offset &= 0xFF;

	// status register
//This isn't cycle accurate--how to fix? !!! FIX !!!
//Probably have to do some multi-threaded implementation or at least a reentrant safe implementation...
//Real hardware returns $00000805, just like the JTRM says.
	if (offset == (0x38 + 0))
		return 0x00;
	if (offset == (0x38 + 1))
		return 0x00;
	if (offset == (0x38 + 2))
		return 0x08;
	if (offset == (0x38 + 3))
		return 0x05;	// always idle/never stopped (collision detection ignored!)

// CHECK HERE ONCE THIS FIX HAS BEEN TESTED: [X]
//Fix for AvP:
	if (offset >= 0x04 && offset <= 0x07)
//This is it. I wonder if it just ignores the lower three bits?
//No, this is a documented Jaguar I bug. It also bites the read at $F02230 as well...
		return blitter_ram[offset + 0x08];		// A1_PIXEL ($F0220C) read at $F02204

	if (offset >= 0x2C && offset <= 0x2F)
		return blitter_ram[offset + 0x04];		// A2_PIXEL ($F02230) read at $F0222C

	return blitter_ram[offset];
}


//Crappy!
uint16_t BlitterReadWord(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	return ((uint16_t)BlitterReadByte(offset, who) << 8) | (uint16_t)BlitterReadByte(offset+1, who);
}


//Crappy!
uint32_t BlitterReadLong(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	return (BlitterReadWord(offset, who) << 16) | BlitterReadWord(offset+2, who);
}


void BlitterWriteByte(uint32_t offset, uint8_t data, uint32_t who/*=UNKNOWN*/)
{
/*if (offset & 0xFF == 0x7B)
	WriteLog("--> Wrote to B_STOP: value -> %02X\n", data);*/
	offset &= 0xFF;
/*if ((offset >= PATTERNDATA) && (offset < PATTERNDATA + 8))
{
	printf("--> %s wrote %02X to byte %u of PATTERNDATA...\n", whoName[who], data, offset - PATTERNDATA);
	fflush(stdout);
}//*/

	// This handles writes to INTENSITY0-3 by also writing them to their proper places in
	// PATTERNDATA & SOURCEDATA (should do the same for the Z registers! !!! FIX !!! [DONE])
	if ((offset >= 0x7C) && (offset <= 0x9B))
	{
		switch (offset)
		{
		// INTENSITY registers 0-3
		case 0x7C: break;
		case 0x7D: blitter_ram[PATTERNDATA + 7] = data; break;
		case 0x7E: blitter_ram[SRCDATA + 6] = data; break;
		case 0x7F: blitter_ram[SRCDATA + 7] = data; break;

		case 0x80: break;
		case 0x81: blitter_ram[PATTERNDATA + 5] = data; break;
		case 0x82: blitter_ram[SRCDATA + 4] = data; break;
		case 0x83: blitter_ram[SRCDATA + 5] = data; break;

		case 0x84: break;
		case 0x85: blitter_ram[PATTERNDATA + 3] = data; break;
		case 0x86: blitter_ram[SRCDATA + 2] = data; break;
		case 0x87: blitter_ram[SRCDATA + 3] = data; break;

		case 0x88: break;
		case 0x89: blitter_ram[PATTERNDATA + 1] = data; break;
		case 0x8A: blitter_ram[SRCDATA + 0] = data; break;
		case 0x8B: blitter_ram[SRCDATA + 1] = data; break;


		// Z registers 0-3
		case 0x8C: blitter_ram[SRCZINT + 6] = data; break;
		case 0x8D: blitter_ram[SRCZINT + 7] = data; break;
		case 0x8E: blitter_ram[SRCZFRAC + 6] = data; break;
		case 0x8F: blitter_ram[SRCZFRAC + 7] = data; break;

		case 0x90: blitter_ram[SRCZINT + 4] = data; break;
		case 0x91: blitter_ram[SRCZINT + 5] = data; break;
		case 0x92: blitter_ram[SRCZFRAC + 4] = data; break;
		case 0x93: blitter_ram[SRCZFRAC + 5] = data; break;

		case 0x94: blitter_ram[SRCZINT + 2] = data; break;
		case 0x95: blitter_ram[SRCZINT + 3] = data; break;
		case 0x96: blitter_ram[SRCZFRAC + 2] = data; break;
		case 0x97: blitter_ram[SRCZFRAC + 3] = data; break;

		case 0x98: blitter_ram[SRCZINT + 0] = data; break;
		case 0x99: blitter_ram[SRCZINT + 1] = data; break;
		case 0x9A: blitter_ram[SRCZFRAC + 0] = data; break;
		case 0x9B: blitter_ram[SRCZFRAC + 1] = data; break;
		}
	}

	// It looks weird, but this is how the 64 bit registers are actually handled...!

	else if ((offset >= SRCDATA + 0) && (offset <= SRCDATA + 3)
		|| (offset >= DSTDATA + 0) && (offset <= DSTDATA + 3)
		|| (offset >= DSTZ + 0) && (offset <= DSTZ + 3)
		|| (offset >= SRCZINT + 0) && (offset <= SRCZINT + 3)
		|| (offset >= SRCZFRAC + 0) && (offset <= SRCZFRAC + 3)
		|| (offset >= PATTERNDATA + 0) && (offset <= PATTERNDATA + 3))
	{
		blitter_ram[offset + 4] = data;
	}
	else if ((offset >= SRCDATA + 4) && (offset <= SRCDATA + 7)
		|| (offset >= DSTDATA + 4) && (offset <= DSTDATA + 7)
		|| (offset >= DSTZ + 4) && (offset <= DSTZ + 7)
		|| (offset >= SRCZINT + 4) && (offset <= SRCZINT + 7)
		|| (offset >= SRCZFRAC + 4) && (offset <= SRCZFRAC + 7)
		|| (offset >= PATTERNDATA + 4) && (offset <= PATTERNDATA + 7))
	{
		blitter_ram[offset - 4] = data;
	}
	else
		blitter_ram[offset] = data;
}


void BlitterWriteWord(uint32_t offset, uint16_t data, uint32_t who/*=UNKNOWN*/)
{
/*if (((offset & 0xFF) >= PATTERNDATA) && ((offset & 0xFF) < PATTERNDATA + 8))
{
	printf("----> %s wrote %04X to byte %u of PATTERNDATA...\n", whoName[who], data, offset - (0xF02200 + PATTERNDATA));
	fflush(stdout);
}*/
//#if 1
/*	if (offset & 0xFF == A1_PIXEL && data == 14368)
	{
		WriteLog("\n1\nA1_PIXEL written by %s (%u)...\n\n\n", whoName[who], data);
extern bool doGPUDis;
doGPUDis = true;
	}
	if ((offset & 0xFF) == (A1_PIXEL + 2) && data == 14368)
	{
		WriteLog("\n2\nA1_PIXEL written by %s (%u)...\n\n\n", whoName[who], data);
extern bool doGPUDis;
doGPUDis = true;
	}//*/
//#endif

	BlitterWriteByte(offset + 0, data >> 8, who);
	BlitterWriteByte(offset + 1, data & 0xFF, who);

	if ((offset & 0xFF) == 0x3A)
	// I.e., the second write of 32-bit value--not convinced this is the best way to do this!
	// But then again, according to the Jaguar docs, this is correct...!
/*extern int blit_start_log;
extern bool doGPUDis;
if (blit_start_log)
{
	WriteLog("BLIT: Blitter started by %s...\n", whoName[who]);
	doGPUDis = true;
}//*/
#ifndef USE_BOTH_BLITTERS
#ifdef USE_ORIGINAL_BLITTER
		blitter_blit(GET32(blitter_ram, 0x38));
#endif
#ifdef USE_MIDSUMMER_BLITTER
		BlitterMidsummer(GET32(blitter_ram, 0x38));
#endif
#ifdef USE_MIDSUMMER_BLITTER_MKII
		BlitterMidsummer2();
#endif
#else
	{
		if (vjs.useFastBlitter)
			blitter_blit(GET32(blitter_ram, 0x38));
		else
			BlitterMidsummer2();
	}
#endif
}
//F02278,9,A,B


void BlitterWriteLong(uint32_t offset, uint32_t data, uint32_t who/*=UNKNOWN*/)
{
/*if (((offset & 0xFF) >= PATTERNDATA) && ((offset & 0xFF) < PATTERNDATA + 8))
{
	printf("------> %s wrote %08X to byte %u of PATTERNDATA...\n", whoName[who], data, offset - (0xF02200 + PATTERNDATA));
	fflush(stdout);
}//*/
//#if 1
/*	if ((offset & 0xFF) == A1_PIXEL && (data & 0xFFFF) == 14368)
	{
		WriteLog("\n3\nA1_PIXEL written by %s (%u)...\n\n\n", whoName[who], data);
extern bool doGPUDis;
doGPUDis = true;
	}//*/
//#endif

	BlitterWriteWord(offset + 0, data >> 16, who);
	BlitterWriteWord(offset + 2, data & 0xFFFF, who);
}


void LogBlit(void)
{
	const char * opStr[16] = { "LFU_CLEAR", "LFU_NSAND", "LFU_NSAD", "LFU_NOTS", "LFU_SAND", "LFU_NOTD", "LFU_N_SXORD", "LFU_NSORND",
		"LFU_SAD", "LFU_XOR", "LFU_D", "LFU_NSORD", "LFU_REPLACE", "LFU_SORND", "LFU_SORD", "LFU_ONE" };
	uint32_t cmd = GET32(blitter_ram, 0x38);
	uint32_t m = (REG(A1_FLAGS) >> 9) & 0x03, e = (REG(A1_FLAGS) >> 11) & 0x0F;
	uint32_t a1_width = ((0x04 | m) << e) >> 2;
	m = (REG(A2_FLAGS) >> 9) & 0x03, e = (REG(A2_FLAGS) >> 11) & 0x0F;
	uint32_t a2_width = ((0x04 | m) << e) >> 2;

	WriteLog("Blit!\n");
	WriteLog("  COMMAND  = %08X\n", cmd);
	WriteLog("  a1_base  = %08X\n", REG(A1_BASE));
	WriteLog("  a1_flags = %08X (%c %c %c %c%c . %c%c%c%c%c%c %c%c%c %c%c%c . %c%c)\n", REG(A1_FLAGS),
		(REG(A1_FLAGS) & 0x100000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x080000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x040000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x020000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x010000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x004000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x002000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x001000 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000800 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000400 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000200 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000100 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000080 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000040 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000020 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000010 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000008 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000002 ? '1' : '0'),
		(REG(A1_FLAGS) & 0x000001 ? '1' : '0'));
	WriteLog("             pitch=%u, pixSz=%u, zOff=%u, width=%u, xCtrl=%u\n",
		REG(A1_FLAGS) & 0x00003, (REG(A1_FLAGS) & 0x00038) >> 3,
		(REG(A1_FLAGS) & 0x001C0) >> 6,  a1_width, (REG(A1_FLAGS) & 0x30000) >> 16);
	WriteLog("  a1_clip  = %u, %u (%08X)\n", GET16(blitter_ram, A1_CLIP + 2), GET16(blitter_ram, A1_CLIP + 0), GET32(blitter_ram, A1_CLIP));
	WriteLog("  a1_pixel = %d, %d (%08X)\n", (int16_t)GET16(blitter_ram, A1_PIXEL + 2), (int16_t)GET16(blitter_ram, A1_PIXEL + 0), GET32(blitter_ram, A1_PIXEL));
	WriteLog("  a1_step  = %d, %d (%08X)\n", (int16_t)GET16(blitter_ram, A1_STEP + 2), (int16_t)GET16(blitter_ram, A1_STEP + 0), GET32(blitter_ram, A1_STEP));
	WriteLog("  a1_fstep = %u, %u (%08X)\n", GET16(blitter_ram, A1_FSTEP + 2), GET16(blitter_ram, A1_FSTEP + 0), GET32(blitter_ram, A1_FSTEP));
	WriteLog("  a1_fpixel= %u, %u (%08X)\n", GET16(blitter_ram, A1_FPIXEL + 2), GET16(blitter_ram, A1_FPIXEL + 0), GET32(blitter_ram, A1_FPIXEL));
	WriteLog("  a1_inc   = %d, %d (%08X)\n", (int16_t)GET16(blitter_ram, A1_INC + 2), (int16_t)GET16(blitter_ram, A1_INC + 0), GET32(blitter_ram, A1_INC));
	WriteLog("  a1_finc  = %u, %u (%08X)\n", GET16(blitter_ram, A1_FINC + 2), GET16(blitter_ram, A1_FINC + 0), GET32(blitter_ram, A1_FINC));

	WriteLog("  a2_base  = %08X\n", REG(A2_BASE));
	WriteLog("  a2_flags = %08X (%c %c %c %c%c %c %c%c%c%c%c%c %c%c%c %c%c%c . %c%c)\n", REG(A2_FLAGS),
		(REG(A2_FLAGS) & 0x100000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x080000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x040000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x020000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x010000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x008000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x004000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x002000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x001000 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000800 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000400 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000200 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000100 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000080 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000040 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000020 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000010 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000008 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000002 ? '1' : '0'),
		(REG(A2_FLAGS) & 0x000001 ? '1' : '0'));
	WriteLog("             pitch=%u, pixSz=%u, zOff=%u, width=%u, xCtrl=%u\n",
		REG(A2_FLAGS) & 0x00003, (REG(A2_FLAGS) & 0x00038) >> 3,
		(REG(A2_FLAGS) & 0x001C0) >> 6,  a2_width, (REG(A2_FLAGS) & 0x30000) >> 16);
	WriteLog("  a2_mask  = %u, %u (%08X)\n", GET16(blitter_ram, A2_MASK + 2), GET16(blitter_ram, A2_MASK + 0), GET32(blitter_ram, A2_MASK));
	WriteLog("  a2_pixel = %d, %d (%08X)\n", (int16_t)GET16(blitter_ram, A2_PIXEL + 2), (int16_t)GET16(blitter_ram, A2_PIXEL + 0), GET32(blitter_ram, A2_PIXEL));
	WriteLog("  a2_step  = %d, %d (%08X)\n", (int16_t)GET16(blitter_ram, A2_STEP + 2), (int16_t)GET16(blitter_ram, A2_STEP + 0), GET32(blitter_ram, A2_STEP));

	WriteLog("  count    = %d x %d\n", GET16(blitter_ram, PIXLINECOUNTER + 2), GET16(blitter_ram, PIXLINECOUNTER));

	WriteLog("  SRCEN    = %s\n", (SRCEN ? "1" : "0"));
	WriteLog("  SRCENZ   = %s\n", (SRCENZ ? "1" : "0"));
	WriteLog("  SRCENX   = %s\n", (SRCENX ? "1" : "0"));
	WriteLog("  DSTEN    = %s\n", (DSTEN ? "1" : "0"));
	WriteLog("  DSTENZ   = %s\n", (DSTENZ ? "1" : "0"));
	WriteLog("  DSTWRZ   = %s\n", (DSTWRZ ? "1" : "0"));
	WriteLog("  CLIPA1   = %s\n", (CLIPA1 ? "1" : "0"));
	WriteLog("  UPDA1F   = %s\n", (UPDA1F ? "1" : "0"));
	WriteLog("  UPDA1    = %s\n", (UPDA1 ? "1" : "0"));
	WriteLog("  UPDA2    = %s\n", (UPDA2 ? "1" : "0"));
	WriteLog("  DSTA2    = %s\n", (DSTA2 ? "1" : "0"));
	WriteLog("  ZOP      = %s %s %s\n", (Z_OP_INF ? "<" : ""), (Z_OP_EQU ? "=" : ""), (Z_OP_SUP ? ">" : ""));
	WriteLog("+-LFUFUNC  = %s\n", opStr[(cmd >> 21) & 0x0F]);
	WriteLog("| PATDSEL  = %s (PD=%08X%08X)\n", (PATDSEL ? "1" : "0"), REG(PATTERNDATA), REG(PATTERNDATA + 4));
	WriteLog("+-ADDDSEL  = %s\n", (ADDDSEL ? "1" : "0"));
	WriteLog("  CMPDST   = %s\n", (CMPDST ? "1" : "0"));
	WriteLog("  BCOMPEN  = %s\n", (BCOMPEN ? "1" : "0"));
	WriteLog("  DCOMPEN  = %s\n", (DCOMPEN ? "1" : "0"));
	WriteLog("  TOPBEN   = %s\n", (TOPBEN ? "1" : "0"));
	WriteLog("  TOPNEN   = %s\n", (TOPNEN ? "1" : "0"));
	WriteLog("  BKGWREN  = %s\n", (BKGWREN ? "1" : "0"));
	WriteLog("  GOURD    = %s (II=%08X, SD=%08X%08X)\n", (GOURD ? "1" : "0"), REG(INTENSITYINC), REG(SRCDATA), REG(SRCDATA + 4));
	WriteLog("  GOURZ    = %s (ZI=%08X, ZD=%08X%08X, SZ1=%08X%08X, SZ2=%08X%08X)\n", (GOURZ ? "1" : "0"), REG(ZINC), REG(DSTZ), REG(DSTZ + 4),
		REG(SRCZINT), REG(SRCZINT + 4), REG(SRCZFRAC), REG(SRCZFRAC + 4));
	WriteLog("  SRCSHADE = %s\n", (SRCSHADE ? "1" : "0"));
}


#ifdef USE_MIDSUMMER_BLITTER
//
// Here's an attempt to write a blitter that conforms to the Midsummer specs--since
// it's supposedly backwards compatible, it should work well...
//
//#define LOG_BLITTER_MEMORY_ACCESSES

#define DATINIT (false)
#define TXTEXT  (false)
#define POLYGON (false)

void BlitterMidsummer(uint32_t cmd)
{
#ifdef LOG_BLITS
	LogBlit();
#endif
uint32_t outer_loop, inner_loop, a1_addr, a2_addr;
int32_t a1_x, a1_y, a2_x, a2_y, a1_width, a2_width;
uint8_t a1_phrase_mode, a2_phrase_mode;

	a1_addr = REG(A1_BASE) & 0xFFFFFFF8;
	a2_addr = REG(A2_BASE) & 0xFFFFFFF8;
	a1_x = (REG(A1_PIXEL) << 16) | (REG(A1_FPIXEL) & 0xFFFF);
	a1_y = (REG(A1_PIXEL) & 0xFFFF0000) | (REG(A1_FPIXEL) >> 16);
	uint32_t m = (REG(A1_FLAGS) >> 9) & 0x03, e = (REG(A1_FLAGS) >> 11) & 0x0F;
	a1_width = ((0x04 | m) << e) >> 2;//*/
	a2_x = (REG(A2_PIXEL) & 0x0000FFFF) << 16;
	a2_y = (REG(A2_PIXEL) & 0xFFFF0000);
	m = (REG(A2_FLAGS) >> 9) & 0x03, e = (REG(A2_FLAGS) >> 11) & 0x0F;
	a2_width = ((0x04 | m) << e) >> 2;//*/

	a1_phrase_mode = a2_phrase_mode = 0;

	if ((blitter_ram[A1_FLAGS + 1] & 0x03) == 0)
		a1_phrase_mode = 1;

	if ((blitter_ram[A2_FLAGS + 1] & 0x03) == 0)
		a2_phrase_mode = 1;

#define INNER0  (inner_loop == 0)
#define OUTER0  (outer_loop == 0)

// $01800005 has SRCENX, may have to investigate further...
// $00011008 has GOURD & DSTEN.
// $41802F41 has SRCSHADE, CLIPA1
/*bool logBlit = false;
if (cmd != 0x00010200 && cmd != 0x01800001 && cmd != 0x01800005
	&& cmd != 0x00011008 && cmd !=0x41802F41)
{
	logBlit = true;
	LogBlit();
}//*/

	uint64_t srcData = GET64(blitter_ram, SRCDATA), srcXtraData,
		dstData = GET64(blitter_ram, DSTDATA), writeData;
	uint32_t srcAddr, dstAddr;
	uint8_t bitCount, a1PixelSize, a2PixelSize;

	// JTRM says phrase mode only works for 8BPP or higher, so let's try this...
	uint32_t phraseOffset[8] = { 8, 8, 8, 8, 4, 2, 0, 0 };
	uint8_t pixelShift[8] = { 3, 2, 1, 0, 1, 2, 0, 0 };

	a1PixelSize = (blitter_ram[A1_FLAGS + 3] >> 3) & 0x07;
	a2PixelSize = (blitter_ram[A2_FLAGS + 3] >> 3) & 0x07;

	outer_loop = GET16(blitter_ram, PIXLINECOUNTER + 0);

	if (outer_loop == 0)
		outer_loop = 0x10000;

	// We just list the states here and jump from state to state in order to
	// keep things somewhat clear. Optimization/cleanups later.

//idle:							// Blitter is idle, and will not perform any bus activity
/*
idle         Blitter is off the bus, and no activity takes place.
if GO    if DATINIT goto init_if
         else       goto inner
*/
	if (DATINIT)
		goto init_if;
	else
		goto inner;

/*
inner        Inner loop is active, read and write cycles are performed
*/
inner:							// Run inner loop state machine (asserts step from its idle state)
	inner_loop = GET16(blitter_ram, PIXLINECOUNTER + 2);

	if (inner_loop == 0)
		inner_loop = 0x10000;

/*
------------------------------
idle:                        Inactive, blitter is idle or passing round outer loop
idle       Another state in the outer loop is active. No bus transfers are performed.
if STEP
    if SRCENX goto sreadx
    else if TXTEXT goto txtread
    else if SRCEN goto sread
    else if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
    if (SRCENX)
		goto sreadx;
    else if (TXTEXT)
		goto txtread;
    else if (SRCEN)
		goto sread;
    else if (DSTEN)
		goto dread;
    else if (DSTENZ)
		goto dzread;
    else
		goto dwrite;

/*
sreadx     Extra source data read at the start of an inner loop pass.
if STEP
    if SRCENZ goto szreadx
    else if TXTEXT goto txtread
    else if SRCEN goto sread
    else if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
sreadx:							// Extra source data read
	if (SRCENZ)
		goto szreadx;
	else if (TXTEXT)
		goto txtread;
	else if (SRCEN)
		goto sread;
	else if (DSTEN)
		goto dread;
	else if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

/*
szreadx    Extra source Z read as the start of an inner loop pass.
if STEP
    if TXTEXT goto txtread
    else goto sread
*/
szreadx:						// Extra source Z read
	if (TXTEXT)
		goto txtread;
	else
		goto sread;

/*
txtread    Read texture data from external memory. This state is only used for external texture.
           TEXTEXT is the condition TEXTMODE=1.
if STEP
    if SRCEN goto sread
    else if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
txtread:						// Read external texture data
	if (SRCEN)
		goto sread;
	else if (DSTEN)
		goto dread;
	else if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

/*
sread      Source data read.
if STEP
    if SRCENZ goto szread
    else if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
sread:							// Source data read
//The JTRM doesn't really specify the internal structure of the source data read, but I would
//imagine that if it's in phrase mode that it starts by reading the phrase that the window is
//pointing at. Likewise, the pixel (if in BPP 1, 2 & 4, chopped) otherwise. It probably still
//transfers an entire phrase even in pixel mode.
//Odd thought: Does it expand, e.g., 1 BPP pixels into 32 BPP internally? Hmm...
//No.
/*
	a1_addr = REG(A1_BASE) & 0xFFFFFFF8;
	a2_addr = REG(A2_BASE) & 0xFFFFFFF8;
	a1_zoffs = (REG(A1_FLAGS) >> 6) & 7;
	a2_zoffs = (REG(A2_FLAGS) >> 6) & 7;
	xadd_a1_control = (REG(A1_FLAGS) >> 16) & 0x03;
	xadd_a2_control = (REG(A2_FLAGS) >> 16) & 0x03;
	a1_pitch = pitchValue[(REG(A1_FLAGS) & 0x03)];
	a2_pitch = pitchValue[(REG(A2_FLAGS) & 0x03)];
	n_pixels = REG(PIXLINECOUNTER) & 0xFFFF;
	n_lines = (REG(PIXLINECOUNTER) >> 16) & 0xFFFF;
	a1_x = (REG(A1_PIXEL) << 16) | (REG(A1_FPIXEL) & 0xFFFF);
	a1_y = (REG(A1_PIXEL) & 0xFFFF0000) | (REG(A1_FPIXEL) >> 16);
	a2_psize = 1 << ((REG(A2_FLAGS) >> 3) & 0x07);
	a1_psize = 1 << ((REG(A1_FLAGS) >> 3) & 0x07);
	a1_phrase_mode = 0;
	a2_phrase_mode = 0;
	a1_width = ((0x04 | m) << e) >> 2;
	a2_width = ((0x04 | m) << e) >> 2;

	// write values back to registers
	WREG(A1_PIXEL,  (a1_y & 0xFFFF0000) | ((a1_x >> 16) & 0xFFFF));
	WREG(A1_FPIXEL, (a1_y << 16) | (a1_x & 0xFFFF));
	WREG(A2_PIXEL,  (a2_y & 0xFFFF0000) | ((a2_x >> 16) & 0xFFFF));
*/
	// Calculate the address to be read...

//Need to fix phrase mode calcs here, since they should *step* by eight, not mulitply.
//Also, need to fix various differing BPP modes here, since offset won't be correct except
//for 8BPP. !!! FIX !!!
	srcAddr = (DSTA2 ? a1_addr : a2_addr);

/*	if ((DSTA2 ? a1_phrase_mode : a2_phrase_mode) == 1)
	{
		srcAddr += (((DSTA2 ? a1_x : a2_x) >> 16)
			+ (((DSTA2 ? a1_y : a2_y) >> 16) * (DSTA2 ? a1_width : a2_width)));
	}
	else*/
	{
//		uint32_t pixAddr = ((DSTA2 ? a1_x : a2_x) >> 16)
//			+ (((DSTA2 ? a1_y : a2_y) >> 16) * (DSTA2 ? a1_width : a2_width));
		int32_t pixAddr = (int16_t)((DSTA2 ? a1_x : a2_x) >> 16)
			+ ((int16_t)((DSTA2 ? a1_y : a2_y) >> 16) * (DSTA2 ? a1_width : a2_width));

		if ((DSTA2 ? a1PixelSize : a2PixelSize) < 3)
			pixAddr >>= pixelShift[(DSTA2 ? a1PixelSize : a2PixelSize)];
		else if ((DSTA2 ? a1PixelSize : a2PixelSize) > 3)
			pixAddr <<= pixelShift[(DSTA2 ? a1PixelSize : a2PixelSize)];

		srcAddr += pixAddr;
	}

	// And read it!

	if ((DSTA2 ? a1_phrase_mode : a2_phrase_mode) == 1)
	{
		srcData = ((uint64_t)JaguarReadLong(srcAddr, BLITTER) << 32)
			| (uint64_t)JaguarReadLong(srcAddr + 4, BLITTER);
	}
	else
	{
//1,2,&4BPP are wrong here... !!! FIX !!!
		if ((DSTA2 ? a1PixelSize : a2PixelSize) == 0)		// 1 BPP
			srcData = JaguarReadByte(srcAddr, BLITTER);
		if ((DSTA2 ? a1PixelSize : a2PixelSize) == 1)		// 2 BPP
			srcData = JaguarReadByte(srcAddr, BLITTER);
		if ((DSTA2 ? a1PixelSize : a2PixelSize) == 2)		// 4 BPP
			srcData = JaguarReadByte(srcAddr, BLITTER);
		if ((DSTA2 ? a1PixelSize : a2PixelSize) == 3)		// 8 BPP
			srcData = JaguarReadByte(srcAddr, BLITTER);
		if ((DSTA2 ? a1PixelSize : a2PixelSize) == 4)		// 16 BPP
			srcData = JaguarReadWord(srcAddr, BLITTER);
		if ((DSTA2 ? a1PixelSize : a2PixelSize) == 5)		// 32 BPP
			srcData = JaguarReadLong(srcAddr, BLITTER);
	}

#ifdef LOG_BLITTER_MEMORY_ACCESSES
if (logBlit)
	WriteLog("BLITTER: srcAddr=%08X,   srcData=%08X %08X\n", srcAddr, (uint32_t)(srcData >> 32), (uint32_t)(srcData & 0xFFFFFFFF));
#endif

	if (SRCENZ)
		goto szread;
	else if (DSTEN)
		goto dread;
	else if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

szread:							// Source Z read
/*
szread     Source Z read.
if STEP
    if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
	if (DSTEN)
		goto dread;
	else if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

dread:							// Destination data read
/*
dread      Destination data read.
if STEP
    if DSTENZ goto dzread
    else goto dwrite
*/
	// Calculate the destination address to be read...

//Need to fix phrase mode calcs here, since they should *step* by eight, not mulitply.
//Also, need to fix various differing BPP modes here, since offset won't be correct except
//for 8BPP. !!! FIX !!!
	dstAddr = (DSTA2 ? a2_addr : a1_addr);

	{
//	uint32_t pixAddr = ((DSTA2 ? a2_x : a1_x) >> 16)
//		+ (((DSTA2 ? a2_y : a1_y) >> 16) * (DSTA2 ? a2_width : a1_width));
	int32_t pixAddr = (int16_t)((DSTA2 ? a2_x : a1_x) >> 16)
		+ ((int16_t)((DSTA2 ? a2_y : a1_y) >> 16) * (DSTA2 ? a2_width : a1_width));

	if ((DSTA2 ? a2PixelSize : a1PixelSize) < 3)
		pixAddr >>= pixelShift[(DSTA2 ? a2PixelSize : a1PixelSize)];
	else if ((DSTA2 ? a2PixelSize : a1PixelSize) > 3)
		pixAddr <<= pixelShift[(DSTA2 ? a2PixelSize : a1PixelSize)];

	dstAddr += pixAddr;
	}

	// And read it!

	if ((DSTA2 ? a2_phrase_mode : a1_phrase_mode) == 1)
	{
		dstData = ((uint64_t)JaguarReadLong(srcAddr, BLITTER) << 32)
			| (uint64_t)JaguarReadLong(srcAddr + 4, BLITTER);
	}
	else
	{
//1,2,&4BPP are wrong here... !!! FIX !!!
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 0)		// 1 BPP
			dstData = JaguarReadByte(dstAddr, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 1)		// 2 BPP
			dstData = JaguarReadByte(dstAddr, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 2)		// 4 BPP
			dstData = JaguarReadByte(dstAddr, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 3)		// 8 BPP
			dstData = JaguarReadByte(dstAddr, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 4)		// 16 BPP
			dstData = JaguarReadWord(dstAddr, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 5)		// 32 BPP
			dstData = JaguarReadLong(dstAddr, BLITTER);
	}

#ifdef LOG_BLITTER_MEMORY_ACCESSES
if (logBlit)
	WriteLog("BLITTER (dread): dstAddr=%08X,   dstData=%08X %08X\n", dstAddr, (uint32_t)(dstData >> 32), (uint32_t)(dstData & 0xFFFFFFFF));
#endif

	if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

dzread:							// Destination Z read
/*
dzread     Destination Z read.
if STEP goto dwrite
*/
	goto dwrite;

dwrite:							// Destination data write
/*
dwrite     Destination write. Every pass round the inner loop must go through this state..
if STEP
    if DSTWRZ goto dzwrite
    else if INNER0 goto idle
    else if TXTEXT goto txtread
    else if SRCEN goto sread
    else if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
/*
Blit!
  a1_base  = 00100000
  a1_pitch = 0
  a1_psize = 16
  a1_width = 320
  a1_xadd  = 1.000000 (phrase=0)
  a1_yadd  = 0.000000
  a1_x     = 159.000000
  a1_y     = 1.000000
  a1_zoffs = 0
  a2_base  = 000095D0
  a2_pitch = 0
  a2_psize = 16
  a2_width = 256
  a2_xadd  = 1.000000 (phrase=1)
  a2_yadd  = 0.000000
  a2_x     = 2.000000
  a2_y     = 0.000000
  a2_mask_x= 0xFFFFFFFF
  a2_mask_y= 0xFFFFFFFF
  a2_zoffs = 0
  count    = 2 x 1
  COMMAND  = 00011008
  SRCEN    = 0
  DSTEN    = 1
  UPDA1F   = 0
  UPDA1    = 0
  UPDA2    = 0
  DSTA2    = 0
--LFUFUNC  = LFU_CLEAR
| PATDSEL  = 1 (PD=77C7 7700 7700 7700)
--ADDDSEL  = 0
  GOURD    = 1 (II=00FC 1A00, SD=FF00 0000 0000 0000)
*/

//Still need to do CLIPA1 and SRCSHADE and GOURD and GOURZ...

	// Check clipping...

	if (CLIPA1)
	{
		uint16_t x = a1_x >> 16, y = a1_y >> 16;

		if (x >= GET16(blitter_ram, A1_CLIP + 2) || y >= GET16(blitter_ram, A1_CLIP))
			goto inhibitWrite;
	}

	// Figure out what gets written...

	if (PATDSEL)
	{
		writeData = GET64(blitter_ram, PATTERNDATA);
//GOURD works properly only in 16BPP mode...
//SRCDATA holds the intensity fractions...
//Does GOURD get calc'ed here or somewhere else???
//Temporary testing kludge...
//if (GOURD)
//   writeData >>= 48;
//	writeData = 0xFF88;
//OK, it's not writing an entire strip of pixels... Why?
//bad incrementing, that's why!
	}
	else if (ADDDSEL)
	{
		// Apparently this only works with 16-bit pixels. Not sure if it works in phrase mode either.
//Also, take TOPBEN & TOPNEN into account here as well...
		writeData = srcData + dstData;
	}
	else	// LFUFUNC is the default...
	{
		writeData = 0;

		if (LFU_NAN)
			writeData |= ~srcData & ~dstData;
		if (LFU_NA)
			writeData |= ~srcData & dstData;
		if (LFU_AN)
			writeData |= srcData & ~dstData;
		if (LFU_A)
			writeData |= srcData & dstData;
	}

	// Calculate the address to be written...

	dstAddr = (DSTA2 ? a2_addr : a1_addr);

/*	if ((DSTA2 ? a2_phrase_mode : a1_phrase_mode) == 1)
	{
//both of these calculate the wrong address because they don't take into account
//pixel sizes...
		dstAddr += ((DSTA2 ? a2_x : a1_x) >> 16)
			+ (((DSTA2 ? a2_y : a1_y) >> 16) * (DSTA2 ? a2_width : a1_width));
	}
	else*/
	{
/*		dstAddr += ((DSTA2 ? a2_x : a1_x) >> 16)
			+ (((DSTA2 ? a2_y : a1_y) >> 16) * (DSTA2 ? a2_width : a1_width));*/
//		uint32_t pixAddr = ((DSTA2 ? a2_x : a1_x) >> 16)
//			+ (((DSTA2 ? a2_y : a1_y) >> 16) * (DSTA2 ? a2_width : a1_width));
		int32_t pixAddr = (int16_t)((DSTA2 ? a2_x : a1_x) >> 16)
			+ ((int16_t)((DSTA2 ? a2_y : a1_y) >> 16) * (DSTA2 ? a2_width : a1_width));

		if ((DSTA2 ? a2PixelSize : a1PixelSize) < 3)
			pixAddr >>= pixelShift[(DSTA2 ? a2PixelSize : a1PixelSize)];
		else if ((DSTA2 ? a2PixelSize : a1PixelSize) > 3)
			pixAddr <<= pixelShift[(DSTA2 ? a2PixelSize : a1PixelSize)];

		dstAddr += pixAddr;
	}

	// And write it!

	if ((DSTA2 ? a2_phrase_mode : a1_phrase_mode) == 1)
	{
		JaguarWriteLong(dstAddr, writeData >> 32, BLITTER);
		JaguarWriteLong(dstAddr + 4, writeData & 0xFFFFFFFF, BLITTER);
	}
	else
	{
//1,2,&4BPP are wrong here... !!! FIX !!!
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 0)		// 1 BPP
			JaguarWriteByte(dstAddr, writeData, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 1)		// 2 BPP
			JaguarWriteByte(dstAddr, writeData, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 2)		// 4 BPP
			JaguarWriteByte(dstAddr, writeData, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 3)		// 8 BPP
			JaguarWriteByte(dstAddr, writeData, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 4)		// 16 BPP
			JaguarWriteWord(dstAddr, writeData, BLITTER);
		if ((DSTA2 ? a2PixelSize : a1PixelSize) == 5)		// 32 BPP
			JaguarWriteLong(dstAddr, writeData, BLITTER);
	}

#ifdef LOG_BLITTER_MEMORY_ACCESSES
if (logBlit)
	WriteLog("BLITTER: dstAddr=%08X, writeData=%08X %08X\n", dstAddr, (uint32_t)(writeData >> 32), (uint32_t)(writeData & 0xFFFFFFFF));
#endif

inhibitWrite://Should this go here? or on the other side of the X/Y incrementing?
//Seems OK here... for now.

// Do funky X/Y incrementation here as well... !!! FIX !!!

	// Handle A1 channel stepping

	if ((blitter_ram[A1_FLAGS + 1] & 0x03) == 0)
		a1_x += phraseOffset[a1PixelSize] << 16;
	else if ((blitter_ram[A1_FLAGS + 1] & 0x03) == 1)
		a1_x += (blitter_ram[A1_FLAGS + 1] & 0x08 ? -1 << 16 : 1 << 16);
/*	else if ((blitter_ram[A1_FLAGS + 1] & 0x03) == 2)
		a1_x += 0 << 16;                              */
	else if ((blitter_ram[A1_FLAGS + 1] & 0x03) == 3)
	{
//Always add the FINC here??? That was the problem with the BIOS screen... So perhaps.
		a1_x += GET16(blitter_ram, A1_FINC + 2);
		a1_y += GET16(blitter_ram, A1_FINC + 0);

		a1_x += GET16(blitter_ram, A1_INC + 2) << 16;
		a1_y += GET16(blitter_ram, A1_INC + 0) << 16;
	}

	if ((blitter_ram[A1_FLAGS + 1] & 0x04) && (blitter_ram[A1_FLAGS + 1] & 0x03 != 3))
		a1_y += (blitter_ram[A1_FLAGS + 1] & 0x10 ? -1 << 16 : 1 << 16);

	// Handle A2 channel stepping

	if ((blitter_ram[A2_FLAGS + 1] & 0x03) == 0)
		a2_x += phraseOffset[a2PixelSize] << 16;
	else if ((blitter_ram[A2_FLAGS + 1] & 0x03) == 1)
		a2_x += (blitter_ram[A2_FLAGS + 1] & 0x08 ? -1 << 16 : 1 << 16);
/*	else if ((blitter_ram[A2_FLAGS + 1] & 0x03) == 2)
		a2_x += 0 << 16;                              */

	if (blitter_ram[A2_FLAGS + 1] & 0x04)
		a2_y += (blitter_ram[A2_FLAGS + 1] & 0x10 ? -1 << 16 : 1 << 16);

//Need to fix this so that it subtracts (saturating, of course) the correct number of pixels
//in phrase mode... !!! FIX !!! [DONE]
//Need to fix this so that it counts down the correct item. Does it count the
//source or the destination phrase mode???
//It shouldn't matter, because we *should* end up processing the same amount
//the same number of pixels... Not sure though.
	if ((DSTA2 ? a2_phrase_mode : a1_phrase_mode) == 1)
	{
		if (inner_loop < phraseOffset[DSTA2 ? a2PixelSize : a1PixelSize])
			inner_loop = 0;
		else
			inner_loop -= phraseOffset[DSTA2 ? a2PixelSize : a1PixelSize];
	}
	else
		inner_loop--;


	if (DSTWRZ)
		goto dzwrite;
	else if (INNER0)
		goto indone;
	else if (TXTEXT)
		goto txtread;
	else if (SRCEN)
		goto sread;
	else if (DSTEN)
		goto dread;
	else if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

dzwrite:						// Destination Z write
/*
dzwrite    Destination Z write.
if STEP
    if INNER0 goto idle
    else if TXTEXT goto txtread
    else if SRCEN goto sread
    else if DSTEN goto dread
    else if DSTENZ goto dzread
    else goto dwrite
*/
	if (INNER0)
		goto indone;
	else if (TXTEXT)
		goto txtread;
	else if (SRCEN)
		goto sread;
	else if (DSTEN)
		goto dread;
	else if (DSTENZ)
		goto dzread;
	else
		goto dwrite;

/*
------------------------------
if INDONE if OUTER0 goto idle
else if UPDA1F        goto a1fupdate
else if UPDA1         goto a1update
else if GOURZ.POLYGON goto zfupdate
else if UPDA2         goto a2update
else if DATINIT       goto init_if
else restart inner
*/
indone:
	outer_loop--;


	if (OUTER0)
		goto blitter_done;
	else if (UPDA1F)
		goto a1fupdate;
	else if (UPDA1)
		goto a1update;
//kill this, for now...
//	else if (GOURZ.POLYGON)
//		goto zfupdate;
	else if (UPDA2)
		goto a2update;
	else if (DATINIT)
		goto init_if;
	else
		goto inner;

a1fupdate:						// Update A1 pointer fractions and more (see below)
/*
a1fupdate    A1 step fraction is added to A1 pointer fraction
             POLYGON true: A1 step delta X and Y fraction parts are added to the A1
			 step X and Y fraction parts (the value prior to this add is used for
			 the step to pointer add).
             POLYGON true: inner count step fraction is added to the inner count
			 fraction part
             POLYGON.GOURD true: the I fraction step is added to the computed
			 intensity fraction parts +
             POLYGON.GOURD true: the I fraction step delta is added to the I
			 fraction step
goto a1update
*/
/*
#define A1_PIXEL		((uint32_t)0x0C)	// Integer part of the pixel (Y.i and X.i)
#define A1_STEP			((uint32_t)0x10)	// Integer part of the step
#define A1_FSTEP		((uint32_t)0x14)	// Fractional part of the step
#define A1_FPIXEL		((uint32_t)0x18)	// Fractional part of the pixel (Y.f and X.f)
*/

// This is all kinda murky. All we have are the Midsummer docs to give us any guidance,
// and it's incomplete or filled with errors (like above). Aarrrgggghhhhh!

//This isn't right. Is it? I don't think the fractional parts are signed...
//	a1_x += (int32_t)((int16_t)GET16(blitter_ram, A1_FSTEP + 2));
//	a1_y += (int32_t)((int16_t)GET16(blitter_ram, A1_FSTEP + 0));
	a1_x += GET16(blitter_ram, A1_FSTEP + 2);
	a1_y += GET16(blitter_ram, A1_FSTEP + 0);

	goto a1update;

a1update:						// Update A1 pointer integers
/*
a1update     A1 step is added to A1 pointer, with carry from the fractional add
             POLYGON true: A1 step delta X and Y integer parts are added to the A1
			 step X and Y integer parts, with carry from the corresponding
			 fractional part add (again, the value prior to this add is used for
			 the step to pointer add).
             POLYGON true: inner count step is added to the inner count, with carry
             POLYGON.GOURD true: the I step is added to the computed intensities,
			 with carry +
             POLYGON.GOURD true: the I step delta is added to the I step, with
			 carry the texture X and Y step delta values are added to the X and Y
			 step values.
if GOURZ.POLYGON goto zfupdate
else if UPDA2 goto a2update
else if DATINIT goto init_if
else restart inner
*/
	a1_x += (int32_t)(GET16(blitter_ram, A1_STEP + 2) << 16);
	a1_y += (int32_t)(GET16(blitter_ram, A1_STEP + 0) << 16);


//kill this, for now...
//	if (GOURZ.POLYGON)
	if (false)
		goto zfupdate;
	else if (UPDA2)
		goto a2update;
	else if (DATINIT)
		goto init_if;
	else
		goto inner;

zfupdate:						// Update computed Z step fractions
/*
zfupdate     the Z fraction step is added to the computed Z fraction parts +
             the Z fraction step delta is added to the Z fraction step
goto zupdate
*/
	goto zupdate;

zupdate:						// Update computed Z step integers
/*
zupdate      the Z step is added to the computed Zs, with carry +
             the Z step delta is added to the Z step, with carry
if UPDA2 goto a2update
else if DATINIT goto init_if
else restart inner
*/
	if (UPDA2)
		goto a2update;
	else if (DATINIT)
		goto init_if;
	else
		goto inner;

a2update:						// Update A2 pointer
/*
a2update     A2 step is added to the A2 pointer
if DATINIT goto init_if
else restart inner
*/
	a2_x += (int32_t)(GET16(blitter_ram, A2_STEP + 2) << 16);
	a2_y += (int32_t)(GET16(blitter_ram, A2_STEP + 0) << 16);


	if (DATINIT)
		goto init_if;
	else
		goto inner;

init_if:						// Initialise intensity fractions and texture X
/*
init_if      Initialise the fractional part of the computed intensity fields, from
             the increment and step registers. The texture X integer and fractional
			 parts can also be initialised.
goto     init_ii
*/
	goto init_ii;

init_ii:						// Initialise intensity integers and texture Y
/*
init_ii      Initialise the integer part of the computed intensity, and texture Y
             integer and fractional parts
if GOURZ goto init_zf
else     goto inner
*/
	if (GOURZ)
		goto init_zf;
	else
	    goto inner;

init_zf:						// Initialise Z fractions
/*
init_zf      Initialise the fractional part of the computed Z fields.
goto init_zi
*/
	goto init_zi;

init_zi:						// Initialise Z integers
/*
init_zi      Initialise the integer part of the computed Z fields.
goto inner
*/
	goto inner;


/*
The outer loop state machine fires off the inner loop, and controls the updating
process between passes through the inner loop.

+ -- these functions are irrelevant if the DATINIT function is enabled, which it
     will normally be.

All these states will complete in one clock cycle, with the exception of the idle
state, which means the blitter is quiescent; and the inner state, which takes as
long as is required to complete one strip of pixels. It is therefore possible for
the blitter to spend a maximum of nine clock cycles of inactivity between passes
through the inner loop.
*/

blitter_done:
	{}
}
#endif


//
// Here's attempt #2--taken from the Oberon chip specs!
//

#ifdef USE_MIDSUMMER_BLITTER_MKII

void ADDRGEN(uint32_t &, uint32_t &, bool, bool,
	uint16_t, uint16_t, uint32_t, uint8_t, uint8_t, uint8_t, uint8_t,
	uint16_t, uint16_t, uint32_t, uint8_t, uint8_t, uint8_t, uint8_t);
void ADDARRAY(uint16_t * addq, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode,
	uint64_t dstd, uint32_t iinc, uint8_t initcin[], uint64_t initinc, uint16_t initpix,
	uint32_t istep, uint64_t patd, uint64_t srcd, uint64_t srcz1, uint64_t srcz2,
	uint32_t zinc, uint32_t zstep);
void ADD16SAT(uint16_t &r, uint8_t &co, uint16_t a, uint16_t b, uint8_t cin, bool sat, bool eightbit, bool hicinh);
void ADDAMUX(int16_t &adda_x, int16_t &adda_y, uint8_t addasel, int16_t a1_step_x, int16_t a1_step_y,
	int16_t a1_stepf_x, int16_t a1_stepf_y, int16_t a2_step_x, int16_t a2_step_y,
	int16_t a1_inc_x, int16_t a1_inc_y, int16_t a1_incf_x, int16_t a1_incf_y, uint8_t adda_xconst,
	bool adda_yconst, bool addareg, bool suba_x, bool suba_y);
void ADDBMUX(int16_t &addb_x, int16_t &addb_y, uint8_t addbsel, int16_t a1_x, int16_t a1_y,
	int16_t a2_x, int16_t a2_y, int16_t a1_frac_x, int16_t a1_frac_y);
void DATAMUX(int16_t &data_x, int16_t &data_y, uint32_t gpu_din, int16_t addq_x, int16_t addq_y, bool addqsel);
void ADDRADD(int16_t &addq_x, int16_t &addq_y, bool a1fracldi,
	uint16_t adda_x, uint16_t adda_y, uint16_t addb_x, uint16_t addb_y, uint8_t modx, bool suba_x, bool suba_y);
void DATA(uint64_t &wdata, uint8_t &dcomp, uint8_t &zcomp, bool &nowrite,
	bool big_pix, bool cmpdst, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode, bool daddq_sel, uint8_t data_sel,
	uint8_t dbinh, uint8_t dend, uint8_t dstart, uint64_t dstd, uint32_t iinc, uint8_t lfu_func, uint64_t &patd, bool patdadd,
	bool phrase_mode, uint64_t srcd, bool srcdread, bool srczread, bool srcz2add, uint8_t zmode,
	bool bcompen, bool bkgwren, bool dcompen, uint8_t icount, uint8_t pixsize,
	uint64_t &srcz, uint64_t dstz, uint32_t zinc);
void COMP_CTRL(uint8_t &dbinh, bool &nowrite,
	bool bcompen, bool big_pix, bool bkgwren, uint8_t dcomp, bool dcompen, uint8_t icount,
	uint8_t pixsize, bool phrase_mode, uint8_t srcd, uint8_t zcomp);
#define VERBOSE_BLITTER_LOGGING

void BlitterMidsummer2(void)
{
#ifdef LOG_BLITS
	LogBlit();
#endif
	if (startConciseBlitLogging)
		LogBlit();

	// Here's what the specs say the state machine does. Note that this can probably be
	// greatly simplified (also, it's different from what John has in his Oberon docs):
//Will remove stuff that isn't in Jaguar I once fully described (stuff like texture won't
//be described here at all)...

	uint32_t cmd = GET32(blitter_ram, COMMAND);

#if 0
logBlit = false;
if (
	cmd != 0x00010200 &&	// PATDSEL
	cmd != 0x01800001		// SRCEN LFUFUNC=C
	&& cmd != 0x01800005
//Boot ROM ATARI letters:
	&& cmd != 0x00011008	// DSTEN GOURD PATDSEL
//Boot ROM spinning cube:
	&& cmd != 0x41802F41	// SRCEN CLIP_A1 UPDA1 UPDA1F UPDA2 DSTA2 GOURZ ZMODE=0 LFUFUNC=C SRCSHADE
//T2K intro screen:
	&& cmd != 0x01800E01	// SRCEN UPDA1 UPDA2 DSTA2 LFUFUNC=C
//T2K TEMPEST letters:
	&& cmd != 0x09800741	// SRCEN CLIP_A1 UPDA1 UPDA1F UPDA2 LFUFUNC=C DCOMPEN
//Static letters on Cybermorph intro screen:
	&& cmd != 0x09800609	// SRCEN DSTEN UPDA1 UPDA2 LFUFUNC=C DCOMPEN
//Static pic on title screen:
	&& cmd != 0x01800601	// SRCEN UPDA1 UPDA2 LFUFUNC=C
//Turning letters on Cybermorph intro screen:
//	&& cmd != 0x09800F41	// SRCEN CLIP_A1 UPDA1 UPDA1F UPDA2 DSTA2 LFUFUNC=C DCOMPEN
	&& cmd != 0x00113078	// DSTEN DSTENZ DSTWRZ CLIP_A1 GOURD GOURZ PATDSEL ZMODE=4
	&& cmd != 0x09900F39	// SRCEN DSTEN DSTENZ DSTWRZ UPDA1 UPDA1F UPDA2 DSTA2 ZMODE=4 LFUFUNC=C DCOMPEN
	&& cmd != 0x09800209	// SRCEN DSTEN UPDA1 LFUFUNC=C DCOMPEN
	&& cmd != 0x00011200	// UPDA1 GOURD PATDSEL
//Start of Hover Strike (clearing screen):
	&& cmd != 0x00010000	// PATDSEL
//Hover Strike text:
	&& cmd != 0x1401060C	// SRCENX DSTEN UPDA1 UPDA2 PATDSEL BCOMPEN BKGWREN
//Hover Strike 3D stuff
	&& cmd != 0x01902839	// SRCEN DSTEN DSTENZ DSTWRZ DSTA2 GOURZ ZMODE=4 LFUFUNC=C
//Hover Strike darkening on intro to play (briefing) screen
	&& cmd != 0x00020208	// DSTEN UPDA1 ADDDSEL
//Trevor McFur stuff:
	&& cmd != 0x05810601	// SRCEN UPDA1 UPDA2 PATDSEL BCOMPEN
	&& cmd != 0x01800201	// SRCEN UPDA1 LFUFUNC=C
//T2K:
	&& cmd != 0x00011000	// GOURD PATDSEL
	&& cmd != 0x00011040	// CLIP_A1 GOURD PATDSEL
//Checkered flag:
	&& cmd != 0x01800000	// LFUFUNC=C
	&& cmd != 0x01800401	//
	&& cmd != 0x01800040	//
	&& cmd != 0x00020008	//
//	&& cmd != 0x09800F41	// SRCEN CLIP_A1 UPDA1 UPDA1F UPDA2 DSTA2 LFUFUNC=C DCOMPEN
	)
	logBlit = true;//*/
#else
logBlit = true;
#endif
if (blit_start_log == 0)	// Wait for the signal...
	logBlit = false;//*/
//temp, for testing...
/*if (cmd != 0x49820609)
	logBlit = false;//*/

/*
Some T2K unique blits:
logBlit = F, cmd = 00010200 *
logBlit = F, cmd = 00011000
logBlit = F, cmd = 00011040
logBlit = F, cmd = 01800005 *
logBlit = F, cmd = 09800741 *

Hover Strike mission selection screen:
Blit! (CMD = 01902839)	// SRCEN DSTEN DSTENZ DSTWRZ DSTA2 GOURZ ZMODE=4 LFUFUNC=C

Checkered Flag blits in the screw up zone:
Blit! (CMD = 01800001)	// SRCEN LFUFUNC=C
Blit! (CMD = 01800000)	// LFUFUNC=C
Blit! (CMD = 00010000)	// PATDSEL

Wolfenstein 3D in the fuckup zone:
Blit! (CMD = 01800000)	// LFUFUNC=C
*/

//printf("logBlit = %s, cmd = %08X\n", (logBlit ? "T" : "F"), cmd);
//fflush(stdout);
//logBlit = true;

/*
Blit! (CMD = 00011040)
Flags: CLIP_A1 GOURD PATDSEL
  count = 18 x 1
  a1_base = 00100000, a2_base = 0081F6A8
  a1_x = 00A7, a1_y = 0014, a1_frac_x = 0000, a1_frac_y = 0000, a2_x = 0001, a2_y = 0000
  a1_step_x = FE80, a1_step_y = 0001, a1_stepf_x = 0000, a1_stepf_y = 0000, a2_step_x = FFF8, a2_step_y = 0001
  a1_inc_x = 0001, a1_inc_y = 0000, a1_incf_x = 0000, a1_incf_y = 0000
  a1_win_x = 0180, a1_win_y = 0118, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+phr/+0 a2add=+phr/+0
  a1_pixsize = 4, a2_pixsize = 4
*/
//Testing T2K...
/*logBlit = false;
if (cmd == 0x00011040
	&& (GET16(blitter_ram, A1_PIXEL + 2) == 0x00A7) && (GET16(blitter_ram, A1_PIXEL + 0) == 0x0014)
	&& (GET16(blitter_ram, A2_PIXEL + 2) == 0x0001) && (GET16(blitter_ram, A2_PIXEL + 0) == 0x0000)
	&& (GET16(blitter_ram, PIXLINECOUNTER + 2) == 18))
	logBlit = true;*/

	// Line states passed in via the command register

	bool srcen = (SRCEN), srcenx = (SRCENX), srcenz = (SRCENZ),
		dsten = (DSTEN), dstenz = (DSTENZ), dstwrz = (DSTWRZ), clip_a1 = (CLIPA1),
		upda1 = (UPDA1), upda1f = (UPDA1F), upda2 = (UPDA2), dsta2 = (DSTA2),
		gourd = (GOURD), gourz = (GOURZ), topben = (TOPBEN), topnen = (TOPNEN),
		patdsel = (PATDSEL), adddsel = (ADDDSEL), cmpdst = (CMPDST), bcompen = (BCOMPEN),
		dcompen = (DCOMPEN), bkgwren = (BKGWREN), srcshade = (SRCSHADE);

	uint8_t zmode = (cmd & 0x01C0000) >> 18, lfufunc = (cmd & 0x1E00000) >> 21;
//Missing: BUSHI
//Where to find various lines:
// clip_a1  -> inner
// gourd    -> dcontrol, inner, outer, state
// gourz    -> dcontrol, inner, outer, state
// cmpdst   -> blit, data, datacomp, state
// bcompen  -> acontrol, inner, mcontrol, state
// dcompen  -> inner, state
// bkgwren  -> inner, state
// srcshade -> dcontrol, inner, state
// adddsel  -> dcontrol
//NOTE: ADDDSEL takes precedence over PATDSEL, PATDSEL over LFU_FUNC
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	char zfs[512], lfus[512];
	zfs[0] = lfus[0] = 0;
	if (dstwrz || dstenz || gourz)
		sprintf(zfs, " ZMODE=%X", zmode);
	if (!(patdsel || adddsel))
		sprintf(lfus, " LFUFUNC=%X", lfufunc);
	WriteLog("\nBlit! (CMD = %08X)\nFlags:%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s\n", cmd,
		(srcen ? " SRCEN" : ""), (srcenx ? " SRCENX" : ""), (srcenz ? " SRCENZ" : ""),
		(dsten ? " DSTEN" : ""), (dstenz ? " DSTENZ" : ""), (dstwrz ? " DSTWRZ" : ""),
		(clip_a1 ? " CLIP_A1" : ""), (upda1 ? " UPDA1" : ""), (upda1f ? " UPDA1F" : ""),
		(upda2 ? " UPDA2" : ""), (dsta2 ? " DSTA2" : ""), (gourd ? " GOURD" : ""),
		(gourz ? " GOURZ" : ""), (topben ? " TOPBEN" : ""), (topnen ? " TOPNEN" : ""),
		(patdsel ? " PATDSEL" : ""), (adddsel ? " ADDDSEL" : ""), zfs, lfus, (cmpdst ? " CMPDST" : ""),
		(bcompen ? " BCOMPEN" : ""), (dcompen ? " DCOMPEN" : ""), (bkgwren ? " BKGWREN" : ""),
		(srcshade ? " SRCSHADE" : ""));
	WriteLog("  count = %d x %d\n", GET16(blitter_ram, PIXLINECOUNTER + 2), GET16(blitter_ram, PIXLINECOUNTER));
}
#endif

	// Lines that don't exist in Jaguar I (and will never be asserted)

	bool polygon = false, datinit = false, a1_stepld = false, a2_stepld = false, ext_int = false;
	bool istepadd = false, istepfadd = false, finneradd = false, inneradd = false;
	bool zstepfadd = false, zstepadd = false;

	// Various state lines (initial state--basically the reset state of the FDSYNCs)

	bool go = true, idle = true, inner = false, a1fupdate = false, a1update = false,
		zfupdate = false, zupdate = false, a2update = false, init_if = false, init_ii = false,
		init_zf = false, init_zi = false;

	bool outer0 = false, indone = false;

	bool idlei, inneri, a1fupdatei, a1updatei, zfupdatei, zupdatei, a2updatei, init_ifi, init_iii,
		init_zfi, init_zii;

	bool notgzandp = !(gourz && polygon);

	// Various registers set up by user

	uint16_t ocount = GET16(blitter_ram, PIXLINECOUNTER);
	uint8_t a1_pitch = blitter_ram[A1_FLAGS + 3] & 0x03;
	uint8_t a2_pitch = blitter_ram[A2_FLAGS + 3] & 0x03;
	uint8_t a1_pixsize = (blitter_ram[A1_FLAGS + 3] & 0x38) >> 3;
	uint8_t a2_pixsize = (blitter_ram[A2_FLAGS + 3] & 0x38) >> 3;
	uint8_t a1_zoffset = (GET16(blitter_ram, A1_FLAGS + 2) >> 6) & 0x07;
	uint8_t a2_zoffset = (GET16(blitter_ram, A2_FLAGS + 2) >> 6) & 0x07;
	uint8_t a1_width = (blitter_ram[A1_FLAGS + 2] >> 1) & 0x3F;
	uint8_t a2_width = (blitter_ram[A2_FLAGS + 2] >> 1) & 0x3F;
	bool a2_mask = blitter_ram[A2_FLAGS + 2] & 0x80;
	uint8_t a1addx = blitter_ram[A1_FLAGS + 1] & 0x03, a2addx = blitter_ram[A2_FLAGS + 1] & 0x03;
	bool a1addy = blitter_ram[A1_FLAGS + 1] & 0x04, a2addy = blitter_ram[A2_FLAGS + 1] & 0x04;
	bool a1xsign = blitter_ram[A1_FLAGS + 1] & 0x08, a2xsign = blitter_ram[A2_FLAGS + 1] & 0x08;
	bool a1ysign = blitter_ram[A1_FLAGS + 1] & 0x10, a2ysign = blitter_ram[A2_FLAGS + 1] & 0x10;
	uint32_t a1_base = GET32(blitter_ram, A1_BASE) & 0xFFFFFFF8;	// Phrase aligned by ignoring bottom 3 bits
	uint32_t a2_base = GET32(blitter_ram, A2_BASE) & 0xFFFFFFF8;

	uint16_t a1_win_x = GET16(blitter_ram, A1_CLIP + 2) & 0x7FFF;
	uint16_t a1_win_y = GET16(blitter_ram, A1_CLIP + 0) & 0x7FFF;
	int16_t a1_x = (int16_t)GET16(blitter_ram, A1_PIXEL + 2);
	int16_t a1_y = (int16_t)GET16(blitter_ram, A1_PIXEL + 0);
	int16_t a1_step_x = (int16_t)GET16(blitter_ram, A1_STEP + 2);
	int16_t a1_step_y = (int16_t)GET16(blitter_ram, A1_STEP + 0);
	uint16_t a1_stepf_x = GET16(blitter_ram, A1_FSTEP + 2);
	uint16_t a1_stepf_y = GET16(blitter_ram, A1_FSTEP + 0);
	uint16_t a1_frac_x = GET16(blitter_ram, A1_FPIXEL + 2);
	uint16_t a1_frac_y = GET16(blitter_ram, A1_FPIXEL + 0);
	int16_t a1_inc_x = (int16_t)GET16(blitter_ram, A1_INC + 2);
	int16_t a1_inc_y = (int16_t)GET16(blitter_ram, A1_INC + 0);
	uint16_t a1_incf_x = GET16(blitter_ram, A1_FINC + 2);
	uint16_t a1_incf_y = GET16(blitter_ram, A1_FINC + 0);

	int16_t a2_x = (int16_t)GET16(blitter_ram, A2_PIXEL + 2);
	int16_t a2_y = (int16_t)GET16(blitter_ram, A2_PIXEL + 0);
	uint16_t a2_mask_x = GET16(blitter_ram, A2_MASK + 2);
	uint16_t a2_mask_y = GET16(blitter_ram, A2_MASK + 0);
	int16_t a2_step_x = (int16_t)GET16(blitter_ram, A2_STEP + 2);
	int16_t a2_step_y = (int16_t)GET16(blitter_ram, A2_STEP + 0);

	uint64_t srcd1 = GET64(blitter_ram, SRCDATA);
	uint64_t srcd2 = 0;
	uint64_t dstd = GET64(blitter_ram, DSTDATA);
	uint64_t patd = GET64(blitter_ram, PATTERNDATA);
	uint32_t iinc = GET32(blitter_ram, INTENSITYINC);
	uint64_t srcz1 = GET64(blitter_ram, SRCZINT);
	uint64_t srcz2 = GET64(blitter_ram, SRCZFRAC);
	uint64_t dstz = GET64(blitter_ram, DSTZ);
	uint32_t zinc = GET32(blitter_ram, ZINC);
	uint32_t collision = GET32(blitter_ram, COLLISIONCTRL);// 0=RESUME, 1=ABORT, 2=STOPEN

	uint8_t pixsize = (dsta2 ? a2_pixsize : a1_pixsize);	// From ACONTROL

//Testing Trevor McFur--I *think* it's the circle on the lower RHS of the screen...
/*logBlit = false;
if (cmd == 0x05810601 && (GET16(blitter_ram, PIXLINECOUNTER + 2) == 96)
	&& (GET16(blitter_ram, PIXLINECOUNTER + 0) == 72))
	logBlit = true;//*/
//Testing...
//if (cmd == 0x1401060C) patd = 0xFFFFFFFFFFFFFFFFLL;
//if (cmd == 0x1401060C) patd = 0x00000000000000FFLL;
//If it's still not working (bcompen-patd) then see who's writing what to patd and where...
//Still not OK. Check to see who's writing what to where in patd!
//It looks like M68K is writing to the top half of patd... Hmm...
/*
----> M68K wrote 0000 to byte 15737344 of PATTERNDATA...
--> M68K wrote 00 to byte 0 of PATTERNDATA...
--> M68K wrote 00 to byte 1 of PATTERNDATA...
----> M68K wrote 00FF to byte 15737346 of PATTERNDATA...
--> M68K wrote 00 to byte 2 of PATTERNDATA...
--> M68K wrote FF to byte 3 of PATTERNDATA...
logBlit = F, cmd = 1401060C

Wren0 := ND6 (wren\[0], gpua\[5], gpua\[6..8], bliten, gpu_memw);
Wren1 := ND6 (wren\[1], gpua[5], gpua\[6..8], bliten, gpu_memw);
Wren2 := ND6 (wren\[2], gpua\[5], gpua[6], gpua\[7..8], bliten, gpu_memw);
Wren3 := ND6 (wren\[3], gpua[5], gpua[6], gpua\[7..8], bliten, gpu_memw);

--> 0 000x xx00
Dec0  := D38GH (a1baseld, a1flagld, a1winld, a1ptrld, a1stepld, a1stepfld, a1fracld, a1incld, gpua[2..4], wren\[0]);
--> 0 001x xx00
Dec1  := D38GH (a1incfld, a2baseld, a2flagld, a2maskld, a2ptrldg, a2stepld, cmdldt, countldt, gpua[2..4], wren\[1]);
--> 0 010x xx00
Dec2  := D38GH (srcd1ldg[0..1], dstdldg[0..1], dstzldg[0..1], srcz1ldg[0..1], gpua[2..4], wren\[2]);
--> 0 011x xx00
Dec3  := D38GH (srcz2ld[0..1], patdld[0..1], iincld, zincld, stopld, intld[0], gpua[2..4], wren\[3]);

wren[3] is asserted when gpu address bus = 0 011x xx00
patdld[0] -> 0 0110 1000 -> $F02268 (lo 32 bits)
patdld[1] -> 0 0110 1100 -> $F0226C (hi 32 bits)

So... It's reversed! The data organization of the patd register is [low 32][high 32]! !!! FIX !!! [DONE]
And fix all the other 64 bit registers [DONE]
*/
/*if (cmd == 0x1401060C)
{
	printf("logBlit = %s, cmd = %08X\n", (logBlit ? "T" : "F"), cmd);
	fflush(stdout);
}*/
/*logBlit = false;
if ((cmd == 0x00010200) && (GET16(blitter_ram, PIXLINECOUNTER + 2) == 9))
	logBlit = true;

; Pink altimeter bar

Blit! (00110000 <- 000BF010) count: 9 x 23, A1/2_FLAGS: 000042E2/00010020 [cmd: 00010200]
 CMD -> src:  dst:  misc:  a1ctl: UPDA1  mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 step values: -10 (X), 1 (Y)
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 262/132, A2 x/y: 129/0
;x-coord is 257 in pic, so add 5
;20 for ship, 33 for #... Let's see if we can find 'em!

; Black altimeter bar

Blit! (00110000 <- 000BF010) count: 5 x 29, A1/2_FLAGS: 000042E2/00010020 [cmd: 00010200]
 CMD -> src:  dst:  misc:  a1ctl: UPDA1  mode:  ity: PATDSEL z-op:  op: LFU_CLEAR ctrl:
  A1 step values: -8 (X), 1 (Y)
  A1 -> pitch: 4 phrases, depth: 16bpp, z-off: 3, width: 320 (21), addctl: XADDPHR YADD0 XSIGNADD YSIGNADD
  A2 -> pitch: 1 phrases, depth: 16bpp, z-off: 0, width: 1 (00), addctl: XADDPIX YADD0 XSIGNADD YSIGNADD
        A1 x/y: 264/126, A2 x/y: 336/0

Here's the pink bar--note that it's phrase mode without dread, so how does this work???
Not sure, but I *think* that somehow it MUXes the data at the write site in on the left or right side
of the write data when masked in phrase mode. I'll have to do some tracing to see if this is the mechanism
it uses or not...

Blit! (CMD = 00010200)
Flags: UPDA1 PATDSEL
  count = 9 x 11
  a1_base = 00110010, a2_base = 000BD7E0
  a1_x = 0106, a1_y = 0090, a1_frac_x = 0000, a1_frac_y = 8000, a2_x = 025A, a2_y = 0000
  a1_step_x = FFF6, a1_step_y = 0001, a1_stepf_x = 5E00, a1_stepf_y = D100, a2_step_x = FFF7, a2_step_y = 0001
  a1_inc_x = 0001, a1_inc_y = FFFF, a1_incf_x = 0000, a1_incf_y = E000
  a1_win_x = 0000, a1_win_y = 0000, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+phr/+0 a2add=+1/+0
  a1_pixsize = 4, a2_pixsize = 4
   srcd=BAC673AC2C92E578  dstd=0000000000000000 patd=74C074C074C074C0 iinc=0002E398
  srcz1=7E127E12000088DA srcz2=DBE06DF000000000 dstz=0000000000000000 zinc=FFFE4840, coll=0
  Phrase mode is ON
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
  Entering DWRITE state...
     Dest write address/pix address: 0016A830/0 [dstart=20 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F] [7400000074C074C0] (icount=0007, inc=2)
  Entering A1_ADD state [a1_x=0106, a1_y=0090, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0016A850/0 [dstart=0 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F] [74C074C074C074C0] (icount=0003, inc=4)
  Entering A1_ADD state [a1_x=0108, a1_y=0090, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0016A870/0 [dstart=0 dend=30 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F] [74C074C074C00000] (icount=FFFF, inc=4)
  Entering A1_ADD state [a1_x=010C, a1_y=0090, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering IDLE_INNER state...
  Leaving INNER state... (ocount=000A)
  [in=F a1f=F a1=T zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering A1UPDATE state... (272/144 -> 262/145)
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
*/

	// Bugs in Jaguar I

	a2addy = a1addy;							// A2 channel Y add bit is tied to A1's

//if (logBlit && (ocount > 20)) logBlit = false;
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	WriteLog("  a1_base = %08X, a2_base = %08X\n", a1_base, a2_base);
	WriteLog("  a1_x = %04X, a1_y = %04X, a1_frac_x = %04X, a1_frac_y = %04X, a2_x = %04X, a2_y = %04X\n", (uint16_t)a1_x, (uint16_t)a1_y, a1_frac_x, a1_frac_y, (uint16_t)a2_x, (uint16_t)a2_y);
	WriteLog("  a1_step_x = %04X, a1_step_y = %04X, a1_stepf_x = %04X, a1_stepf_y = %04X, a2_step_x = %04X, a2_step_y = %04X\n", (uint16_t)a1_step_x, (uint16_t)a1_step_y, a1_stepf_x, a1_stepf_y, (uint16_t)a2_step_x, (uint16_t)a2_step_y);
	WriteLog("  a1_inc_x = %04X, a1_inc_y = %04X, a1_incf_x = %04X, a1_incf_y = %04X\n", (uint16_t)a1_inc_x, (uint16_t)a1_inc_y, a1_incf_x, a1_incf_y);
	WriteLog("  a1_win_x = %04X, a1_win_y = %04X, a2_mask_x = %04X, a2_mask_y = %04X\n", a1_win_x, a1_win_y, a2_mask_x, a2_mask_y);
	char x_add_str[4][4] = { "phr", "1", "0", "inc" };
	WriteLog("  a2_mask=%s a1add=%s%s/%s%s a2add=%s%s/%s%s\n", (a2_mask ? "T" : "F"), (a1xsign ? "-" : "+"), x_add_str[a1addx],
		(a1ysign ? "-" : "+"), (a1addy ? "1" : "0"), (a2xsign ? "-" : "+"), x_add_str[a2addx],
		(a2ysign ? "-" : "+"), (a2addy ? "1" : "0"));
	WriteLog("  a1_pixsize = %u, a2_pixsize = %u\n", a1_pixsize, a2_pixsize);
	WriteLog("   srcd=%08X%08X  dstd=%08X%08X patd=%08X%08X iinc=%08X\n",
		(uint32_t)(srcd1 >> 32), (uint32_t)(srcd1 & 0xFFFFFFFF),
		(uint32_t)(dstd >> 32), (uint32_t)(dstd & 0xFFFFFFFF),
		(uint32_t)(patd >> 32), (uint32_t)(patd & 0xFFFFFFFF), iinc);
	WriteLog("  srcz1=%08X%08X srcz2=%08X%08X dstz=%08X%08X zinc=%08X, coll=%X\n",
		(uint32_t)(srcz1 >> 32), (uint32_t)(srcz1 & 0xFFFFFFFF),
		(uint32_t)(srcz2 >> 32), (uint32_t)(srcz2 & 0xFFFFFFFF),
		(uint32_t)(dstz >> 32), (uint32_t)(dstz & 0xFFFFFFFF), zinc, collision);
}
#endif

	// Various state lines set up by user

	bool phrase_mode = ((!dsta2 && a1addx == 0) || (dsta2 && a2addx == 0) ? true : false);	// From ACONTROL
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Phrase mode is %s\n", (phrase_mode ? "ON" : "off"));
#endif
//logBlit = false;

	// Stopgap vars to simulate various lines

	uint16_t a1FracCInX = 0, a1FracCInY = 0;

	while (true)
	{
		// IDLE

		if ((idle && !go) || (inner && outer0 && indone))
		{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering IDLE state...\n");
#endif
			idlei = true;

//Instead of a return, let's try breaking out of the loop...
break;
//			return;
		}
		else
			idlei = false;

		// INNER LOOP ACTIVE
/*
  Entering DWRITE state... (icount=0000, inc=4)
  Entering IDLE_INNER state...
  Leaving INNER state... (ocount=00EF)
  [in=T a1f=F a1=T zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
Now:
  [in=F a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
*/

		if ((idle && go && !datinit)
			|| (inner && !indone)
			|| (inner && indone && !outer0 && !upda1f && !upda1 && notgzandp && !upda2 && !datinit)
			|| (a1update && !upda2 && notgzandp && !datinit)
			|| (zupdate && !upda2 && !datinit)
			|| (a2update && !datinit)
			|| (init_ii && !gourz)
			|| (init_zi))
		{
			inneri = true;
		}
		else
			inneri = false;

		// A1 FRACTION UPDATE

		if (inner && indone && !outer0 && upda1f)
		{
			a1fupdatei = true;
		}
		else
			a1fupdatei = false;

		// A1 POINTER UPDATE

		if ((a1fupdate)
			|| (inner && indone && !outer0 && !upda1f && upda1))
		{
			a1updatei = true;
		}
		else
			a1updatei = false;

		// Z FRACTION UPDATE

		if ((a1update && gourz && polygon)
			|| (inner && indone && !outer0 && !upda1f && !upda1 && gourz && polygon))
		{
			zfupdatei = true;
		}
		else
			zfupdatei = false;

		// Z INTEGER UPDATE

		if (zfupdate)
		{
			zupdatei = true;
		}
		else
			zupdatei = false;

		// A2 POINTER UPDATE

		if ((a1update && upda2 && notgzandp)
			|| (zupdate && upda2)
			|| (inner && indone && !outer0 && !upda1f && notgzandp && !upda1 && upda2))
		{
			a2updatei = true;
		}
		else
			a2updatei = false;

		// INITIALIZE INTENSITY FRACTION

		if ((zupdate && !upda2 && datinit)
			|| (a1update && !upda2 && datinit && notgzandp)
			|| (inner && indone && !outer0 && !upda1f && !upda1 && notgzandp && !upda2 && datinit)
			|| (a2update && datinit)
			|| (idle && go && datinit))
		{
			init_ifi = true;
		}
		else
			init_ifi = false;

		// INITIALIZE INTENSITY INTEGER

		if (init_if)
		{
			init_iii = true;
		}
		else
			init_iii = false;

		// INITIALIZE Z FRACTION

		if (init_ii && gourz)
		{
			init_zfi = true;
		}
		else
			init_zfi = false;

		// INITIALIZE Z INTEGER

		if (init_zf)
		{
			init_zii = true;
		}
		else
			init_zii = false;

// Here we move the fooi into their foo counterparts in order to simulate the moving
// of data into the various FDSYNCs... Each time we loop we simulate one clock cycle...

		idle = idlei;
		inner = inneri;
		a1fupdate = a1fupdatei;
		a1update = a1updatei;
		zfupdate = zfupdatei;		// *
		zupdate = zupdatei;			// *
		a2update = a2updatei;
		init_if = init_ifi;			// *
		init_ii = init_iii;			// *
		init_zf = init_zfi;			// *
		init_zi = init_zii;			// *
// * denotes states that will never assert for Jaguar I
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  [in=%c a1f=%c a1=%c zf=%c z=%c a2=%c iif=%c iii=%c izf=%c izi=%c]\n",
		(inner ? 'T' : 'F'), (a1fupdate ? 'T' : 'F'), (a1update ? 'T' : 'F'),
		(zfupdate ? 'T' : 'F'), (zupdate ? 'T' : 'F'), (a2update ? 'T' : 'F'),
		(init_if ? 'T' : 'F'), (init_ii ? 'T' : 'F'), (init_zf ? 'T' : 'F'),
		(init_zi ? 'T' : 'F'));
#endif

// Now, depending on how we want to handle things, we could either put the implementation
// of the various pieces up above, or handle them down below here.

// Let's try postprocessing for now...

		if (inner)
		{
			indone = false;
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering INNER state...\n");
#endif
			uint16_t icount = GET16(blitter_ram, PIXLINECOUNTER + 2);
			bool idle_inner = true, step = true, sreadx = false, szreadx = false, sread = false,
				szread = false, dread = false, dzread = false, dwrite = false, dzwrite = false;
			bool inner0 = false;
			bool idle_inneri, sreadxi, szreadxi, sreadi, szreadi, dreadi, dzreadi, dwritei, dzwritei;

			// State lines that will never assert in Jaguar I

			bool textext = false, txtread = false;

//other stuff
uint8_t srcshift = 0;
bool sshftld = true; // D flipflop (D -> Q): instart -> sshftld
//NOTE: sshftld probably is only asserted at the beginning of the inner loop. !!! FIX !!!
/*
Blit! (CMD = 01800005)
Flags: SRCEN SRCENX LFUFUNC=C
  count = 626 x 1
  a1_base = 00037290, a2_base = 000095D0
  a1_x = 0000, a1_y = 0000, a2_x = 0002, a2_y = 0000
  a1_pixsize = 4, a2_pixsize = 4
  srcd=0000000000000000, dstd=0000000000000000, patd=0000000000000000
  Phrase mode is ON
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
  Entering SREADX state... [dstart=0 dend=20 pwidth=8 srcshift=20]
    Source extra read address/pix address: 000095D4/0 [0000001C00540038]
  Entering A2_ADD state [a2_x=0002, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state... [dstart=0 dend=20 pwidth=8 srcshift=0]
    Source read address/pix address: 000095D8/0 [0054003800009814]
  Entering A2_ADD state [a2_x=0004, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 00037290/0 [dstart=0 dend=20 pwidth=8 srcshift=0] (icount=026E, inc=4)
  Entering A1_ADD state [a1_x=0000, a1_y=0000, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state... [dstart=0 dend=20 pwidth=8 srcshift=0]
    Source read address/pix address: 000095E0/0 [00009968000377C7]
  Entering A2_ADD state [a2_x=0008, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 00037298/0 [dstart=0 dend=20 pwidth=8 srcshift=0] (icount=026A, inc=4)
  Entering A1_ADD state [a1_x=0004, a1_y=0000, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
*/

//			while (!idle_inner)
			while (true)
			{
				// IDLE

				if ((idle_inner && !step)
					|| (dzwrite && step && inner0)
					|| (dwrite && step && !dstwrz && inner0))
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering IDLE_INNER state...\n");
#endif
					idle_inneri = true;
break;
				}
				else
					idle_inneri = false;

				// EXTRA SOURCE DATA READ

				if ((idle_inner && step && srcenx)
					|| (sreadx && !step))
				{
					sreadxi = true;
				}
				else
					sreadxi = false;

				// EXTRA SOURCE ZED READ

				if ((sreadx && step && srcenz)
					|| (szreadx && !step))
				{
					szreadxi = true;
				}
				else
					szreadxi = false;

				// TEXTURE DATA READ (not implemented because not in Jaguar I)

				// SOURCE DATA READ

				if ((szreadx && step && !textext)
					|| (sreadx && step && !srcenz && srcen)
					|| (idle_inner && step && !srcenx && !textext && srcen)
					|| (dzwrite && step && !inner0 && !textext && srcen)
					|| (dwrite && step && !dstwrz && !inner0 && !textext && srcen)
					|| (txtread && step && srcen)
					|| (sread && !step))
				{
					sreadi = true;
				}
				else
					sreadi = false;

				// SOURCE ZED READ

				if ((sread && step && srcenz)
					|| (szread && !step))
				{
					szreadi = true;
				}
				else
					szreadi = false;

				// DESTINATION DATA READ

				if ((szread && step && dsten)
					|| (sread && step && !srcenz && dsten)
					|| (sreadx && step && !srcenz && !textext && !srcen && dsten)
					|| (idle_inner && step && !srcenx && !textext && !srcen && dsten)
					|| (dzwrite && step && !inner0 && !textext && !srcen && dsten)
					|| (dwrite && step && !dstwrz && !inner0 && !textext && !srcen && dsten)
					|| (txtread && step && !srcen && dsten)
					|| (dread && !step))
				{
					dreadi = true;
				}
				else
					dreadi = false;

				// DESTINATION ZED READ

				if ((dread && step && dstenz)
					|| (szread && step && !dsten && dstenz)
					|| (sread && step && !srcenz && !dsten && dstenz)
					|| (sreadx && step && !srcenz && !textext && !srcen && !dsten && dstenz)
					|| (idle_inner && step && !srcenx && !textext && !srcen && !dsten && dstenz)
					|| (dzwrite && step && !inner0 && !textext && !srcen && !dsten && dstenz)
					|| (dwrite && step && !dstwrz && !inner0 && !textext && !srcen && !dsten && dstenz)
					|| (txtread && step && !srcen && !dsten && dstenz)
					|| (dzread && !step))
				{
					dzreadi = true;
				}
				else
					dzreadi = false;

				// DESTINATION DATA WRITE

				if ((dzread && step)
					|| (dread && step && !dstenz)
					|| (szread && step && !dsten && !dstenz)
					|| (sread && step && !srcenz && !dsten && !dstenz)
					|| (txtread && step && !srcen && !dsten && !dstenz)
					|| (sreadx && step && !srcenz && !textext && !srcen && !dsten && !dstenz)
					|| (idle_inner && step && !srcenx && !textext && !srcen && !dsten && !dstenz)
					|| (dzwrite && step && !inner0 && !textext && !srcen && !dsten && !dstenz)
					|| (dwrite && step && !dstwrz && !inner0 && !textext && !srcen && !dsten && !dstenz)
					|| (dwrite && !step))
				{
					dwritei = true;
				}
				else
					dwritei = false;

				// DESTINATION ZED WRITE

				if ((dzwrite && !step)
					|| (dwrite && step && dstwrz))
				{
					dzwritei = true;
				}
				else
					dzwritei = false;

//Kludge: A QnD way to make sure that sshftld is asserted only for the first
//        cycle of the inner loop...
sshftld = idle_inner;

// Here we move the fooi into their foo counterparts in order to simulate the moving
// of data into the various FDSYNCs... Each time we loop we simulate one clock cycle...

				idle_inner = idle_inneri;
				sreadx = sreadxi;
				szreadx = szreadxi;
				sread = sreadi;
				szread = szreadi;
				dread = dreadi;
				dzread = dzreadi;
				dwrite = dwritei;
				dzwrite = dzwritei;

// Here's a few more decodes--not sure if they're supposed to go here or not...

				bool srca_addi = (sreadxi && !srcenz) || (sreadi && !srcenz) || szreadxi || szreadi;

				bool dsta_addi = (dwritei && !dstwrz) || dzwritei;

				bool gensrc = sreadxi || szreadxi || sreadi || szreadi;
				bool gendst = dreadi || dzreadi || dwritei || dzwritei;
				bool gena2i = (gensrc && !dsta2) || (gendst && dsta2);

				bool zaddr = szreadx || szread || dzread || dzwrite;

// Some stuff from MCONTROL.NET--not sure if this is the correct use of this decode or not...
/*Fontread\	:= OND1 (fontread\, sread[1], sreadx[1], bcompen);
Fontread	:= INV1 (fontread, fontread\);
Justt		:= NAN3 (justt, fontread\, phrase_mode, tactive\);
Justify		:= TS (justify, justt, busen);*/
bool fontread = (sread || sreadx) && bcompen;
bool justify = !(!fontread && phrase_mode /*&& tactive*/);

/* Generate inner loop update enables */
/*
A1_addi		:= MX2 (a1_addi, dsta_addi, srca_addi, dsta2);
A2_addi		:= MX2 (a2_addi, srca_addi, dsta_addi, dsta2);
A1_add		:= FD1 (a1_add, a1_add\, a1_addi, clk);
A2_add		:= FD1 (a2_add, a2_add\, a2_addi, clk);
A2_addb		:= BUF1 (a2_addb, a2_add);
*/
				bool a1_add = (dsta2 ? srca_addi : dsta_addi);
				bool a2_add = (dsta2 ? dsta_addi : srca_addi);

/* Address adder input A register selection
000	A1 step integer part
001	A1 step fraction part
010	A1 increment integer part
011	A1 increment fraction part
100	A2 step

bit 2 = a2update
bit 1 = /a2update . (a1_add . a1addx[0..1])
bit 0 = /a2update . ( a1fupdate
				    + a1_add . atick[0] . a1addx[0..1])
The /a2update term on bits 0 and 1 is redundant.
Now look-ahead based
*/
				uint8_t addasel = (a1fupdate || (a1_add && a1addx == 3) ? 0x01 : 0x00);
				addasel |= (a1_add && a1addx == 3 ? 0x02 : 0x00);
				addasel |= (a2update ? 0x04 : 0x00);
/* Address adder input A X constant selection
adda_xconst[0..2] generate a power of 2 in the range 1-64 or all
zeroes when they are all 1
Remember - these are pixels, so to add one phrase the pixel size
has to be taken into account to get the appropriate value.
for A1
		if a1addx[0..1] are 00 set 6 - pixel size
		if a1addx[0..1] are 01 set the value 000
		if a1addx[0..1] are 10 set the value 111
similarly for A2
JLH: Also, 11 will likewise set the value to 111
*/
				uint8_t a1_xconst = 6 - a1_pixsize, a2_xconst = 6 - a2_pixsize;

				if (a1addx == 1)
				    a1_xconst = 0;
				else if (a1addx & 0x02)
				    a1_xconst = 7;

				if (a2addx == 1)
				    a2_xconst = 0;
				else if (a2addx & 0x02)
				    a2_xconst = 7;

				uint8_t adda_xconst = (a2_add ? a2_xconst : a1_xconst);
/* Address adder input A Y constant selection
22 June 94 - This was erroneous, because only the a1addy bit was reflected here.
Therefore, the selection has to be controlled by a bug fix bit.
JLH: Bug fix bit in Jaguar II--not in Jaguar I!
*/
				bool adda_yconst = a1addy;
/* Address adder input A register versus constant selection
given by	  a1_add . a1addx[0..1]
				+ a1update
				+ a1fupdate
				+ a2_add . a2addx[0..1]
				+ a2update
*/
				bool addareg = ((a1_add && a1addx == 3) || a1update || a1fupdate
					|| (a2_add && a2addx == 3) || a2update ? true : false);
/* The adders can be put into subtract mode in add pixel size
mode when the corresponding flags are set */
				bool suba_x = ((a1_add && a1xsign && a1addx == 1) || (a2_add && a2xsign && a2addx == 1) ? true : false);
				bool suba_y = ((a1_add && a1addy && a1ysign) || (a2_add && a2addy && a2ysign) ? true : false);
/* Address adder input B selection
00	A1 pointer
01	A2 pointer
10	A1 fraction
11	Zero

Bit 1 =   a1fupdate
		+ (a1_add . atick[0] . a1addx[0..1])
		+ a1fupdate . a1_stepld
		+ a1update . a1_stepld
		+ a2update . a2_stepld
Bit 0 =   a2update + a2_add
		+ a1fupdate . a1_stepld
		+ a1update . a1_stepld
		+ a2update . a2_stepld
*/
				uint8_t addbsel = (a2update || a2_add || (a1fupdate && a1_stepld)
				    || (a1update && a1_stepld) || (a2update && a2_stepld) ? 0x01 : 0x00);
				addbsel |= (a1fupdate || (a1_add && a1addx == 3) || (a1fupdate && a1_stepld)
				    || (a1update && a1_stepld) || (a2update && a2_stepld) ? 0x02 : 0x00);

/* The modulo bits are used to align X onto a phrase boundary when
it is being updated by one phrase
000	no mask
001	mask bit 0
010	mask bits 1-0
..
110  	mask bits 5-0

Masking is enabled for a1 when a1addx[0..1] is 00, and the value
is 6 - the pixel size (again!)
*/
				uint8_t maska1 = (a1_add && a1addx == 0 ? 6 - a1_pixsize : 0);
				uint8_t maska2 = (a2_add && a2addx == 0 ? 6 - a2_pixsize : 0);
				uint8_t modx = (a2_add ? maska2 : maska1);
/* Generate load strobes for the increment updates */

/*A1pldt		:= NAN2 (a1pldt, atick[1], a1_add);
A1ptrldi	:= NAN2 (a1ptrldi, a1update\, a1pldt);

A1fldt		:= NAN4 (a1fldt, atick[0], a1_add, a1addx[0..1]);
A1fracldi	:= NAN2 (a1fracldi, a1fupdate\, a1fldt);

A2pldt		:= NAN2 (a2pldt, atick[1], a2_add);
A2ptrldi	:= NAN2 (a2ptrldi, a2update\, a2pldt);*/
				bool a1fracldi = a1fupdate || (a1_add && a1addx == 3);

// Some more from DCONTROL...
// atick[] just MAY be important here! We're assuming it's true and dropping the term...
// That will probably screw up some of the lower terms that seem to rely on the timing of it...
#warning srcdreadd is not properly initialized!
bool srcdreadd = false;						// Set in INNER.NET
//Shadeadd\	:= NAN2H (shadeadd\, dwrite, srcshade);
//Shadeadd	:= INV2 (shadeadd, shadeadd\);
bool shadeadd = dwrite && srcshade;
/* Data adder control, input A selection
000   Destination data
001   Initialiser pixel value
100   Source data      - computed intensity fraction
101   Pattern data     - computed intensity
110   Source zed 1     - computed zed
111   Source zed 2     - computed zed fraction

Bit 0 =   dwrite  . gourd . atick[1]
	+ dzwrite . gourz . atick[0]
	+ istepadd
	+ zstepfadd
	+ init_if + init_ii + init_zf + init_zi
Bit 1 =   dzwrite . gourz . (atick[0] + atick[1])
	+ zstepadd
	+ zstepfadd
Bit 2 =   (gourd + gourz) . /(init_if + init_ii + init_zf + init_zi)
	+ dwrite  . srcshade
*/
uint8_t daddasel = ((dwrite && gourd) || (dzwrite && gourz) || istepadd || zstepfadd
	|| init_if || init_ii || init_zf || init_zi ? 0x01 : 0x00);
daddasel |= ((dzwrite && gourz) || zstepadd || zstepfadd ? 0x02 : 0x00);
daddasel |= (((gourd || gourz) && !(init_if || init_ii || init_zf || init_zi))
	|| (dwrite && srcshade) ? 0x04 : 0x00);
/* Data adder control, input B selection
0000	Source data
0001	Data initialiser increment
0100	Bottom 16 bits of I increment repeated four times
0101	Top 16 bits of I increment repeated four times
0110	Bottom 16 bits of Z increment repeated four times
0111	Top 16 bits of Z increment repeated four times
1100	Bottom 16 bits of I step repeated four times
1101	Top 16 bits of I step repeated four times
1110	Bottom 16 bits of Z step repeated four times
1111	Top 16 bits of Z step repeated four times

Bit 0 =   dwrite  . gourd . atick[1]
	+ dzwrite . gourz . atick[1]
	+ dwrite  . srcshade
	+ istepadd
	+ zstepadd
	+ init_if + init_ii + init_zf + init_zi
Bit 1 =   dzwrite . gourz . (atick[0] + atick[1])
	+ zstepadd
	+ zstepfadd
Bit 2 =   dwrite  . gourd . (atick[0] + atick[1])
	+ dzwrite . gourz . (atick[0] + atick[1])
	+ dwrite  . srcshade
	+ istepadd + istepfadd + zstepadd + zstepfadd
Bit 3 =   istepadd + istepfadd + zstepadd + zstepfadd
*/
uint8_t daddbsel = ((dwrite && gourd) || (dzwrite && gourz) || (dwrite && srcshade)
	|| istepadd || zstepadd || init_if || init_ii || init_zf || init_zi ? 0x01 : 0x00);
daddbsel |= ((dzwrite && gourz) || zstepadd || zstepfadd ? 0x02 : 0x00);
daddbsel |= ((dwrite && gourd) || (dzwrite && gourz) || (dwrite && srcshade)
	|| istepadd || istepfadd || zstepadd || zstepfadd ? 0x04 : 0x00);
daddbsel |= (istepadd && istepfadd && zstepadd && zstepfadd ? 0x08 : 0x00);
/* Data adder mode control
000	16-bit normal add
001	16-bit saturating add with carry
010	8-bit saturating add with carry, carry into top byte is
	inhibited (YCrCb)
011	8-bit saturating add with carry, carry into top byte and
	between top nybbles is inhibited (CRY)
100	16-bit normal add with carry
101	16-bit saturating add
110	8-bit saturating add, carry into top byte is inhibited
111	8-bit saturating add, carry into top byte and between top
	nybbles is inhibited

The first five are used for Gouraud calculations, the latter three
for adding source and destination data

Bit 0 =   dzwrite . gourz . atick[1]
	+ dwrite  . gourd . atick[1] . /topnen . /topben . /ext_int
	+ dwrite  . gourd . atick[1] .  topnen .  topben . /ext_int
	+ zstepadd
	+ istepadd . /topnen . /topben . /ext_int
	+ istepadd .  topnen .  topben . /ext_int
	+ /gourd . /gourz . /topnen . /topben
	+ /gourd . /gourz .  topnen .  topben
	+ shadeadd . /topnen . /topben
	+ shadeadd .  topnen .  topben
	+ init_ii . /topnen . /topben . /ext_int
	+ init_ii .  topnen .  topben . /ext_int
	+ init_zi

Bit 1 =   dwrite . gourd . atick[1] . /topben . /ext_int
	+ istepadd . /topben . /ext_int
	+ /gourd . /gourz .  /topben
	+ shadeadd .  /topben
	+ init_ii .  /topben . /ext_int

Bit 2 =   /gourd . /gourz
	+ shadeadd
	+ dwrite  . gourd . atick[1] . ext_int
	+ istepadd . ext_int
	+ init_ii . ext_int
*/
uint8_t daddmode = ((dzwrite && gourz) || (dwrite && gourd && !topnen && !topben && !ext_int)
	|| (dwrite && gourd && topnen && topben && !ext_int) || zstepadd
	|| (istepadd && !topnen && !topben && !ext_int)
	|| (istepadd && topnen && topben && !ext_int) || (!gourd && !gourz && !topnen && !topben)
	|| (!gourd && !gourz && topnen && topben) || (shadeadd && !topnen && !topben)
	|| (shadeadd && topnen && topben) || (init_ii && !topnen && !topben && !ext_int)
	|| (init_ii && topnen && topben && !ext_int) || init_zi ? 0x01 : 0x00);
daddmode |= ((dwrite && gourd && !topben && !ext_int) || (istepadd && !topben && !ext_int)
	|| (!gourd && !gourz && !topben) || (shadeadd && !topben)
	|| (init_ii && !topben && !ext_int) ? 0x02 : 0x00);
daddmode |= ((!gourd && !gourz) || shadeadd || (dwrite && gourd && ext_int)
	|| (istepadd && ext_int) || (init_ii && ext_int) ? 0x04 : 0x00);
/* Data add load controls
Pattern fraction (dest data) is loaded on
	  dwrite . gourd . atick[0]
	+ istepfadd . /datinit
	+ init_if
Pattern data is loaded on
	  dwrite . gourd . atick[1]
	+ istepadd . /datinit . /datinit
	+ init_ii
Source z1 is loaded on
	  dzwrite . gourz . atick[1]
	+ zstepadd . /datinit . /datinit
	+ init_zi
Source z2 is loaded on
	  dzwrite . gourz . atick[0]
	+ zstepfadd
	+ init_zf
Texture map shaded data is loaded on
	srcdreadd . srcshade
*/
bool patfadd = (dwrite && gourd) || (istepfadd && !datinit) || init_if;
bool patdadd = (dwrite && gourd) || (istepadd && !datinit) || init_ii;
bool srcz1add = (dzwrite && gourz) || (zstepadd && !datinit) || init_zi;
bool srcz2add = (dzwrite && gourz) || zstepfadd || init_zf;
bool srcshadd = srcdreadd && srcshade;
bool daddq_sel = patfadd || patdadd || srcz1add || srcz2add || srcshadd;
/* Select write data
This has to be controlled from stage 1 of the pipe-line, delayed
by one tick, as the write occurs in the cycle after the ack.

00	pattern data
01	lfu data
10	adder output
11	source zed

Bit 0 =  /patdsel . /adddsel
	+ dzwrite1d
Bit 1 =   adddsel
	+ dzwrite1d
*/
uint8_t data_sel = ((!patdsel && !adddsel) || dzwrite ? 0x01 : 0x00)
	| (adddsel || dzwrite ? 0x02 : 0x00);

uint32_t address, pixAddr;
ADDRGEN(address, pixAddr, gena2i, zaddr,
	a1_x, a1_y, a1_base, a1_pitch, a1_pixsize, a1_width, a1_zoffset,
	a2_x, a2_y, a2_base, a2_pitch, a2_pixsize, a2_width, a2_zoffset);

//Here's my guess as to how the addresses get truncated to phrase boundaries in phrase mode...
if (!justify)
	address &= 0xFFFFF8;

/* Generate source alignment shift
   -------------------------------
The source alignment shift for data move is the difference between
the source and destination X pointers, multiplied by the pixel
size.  Only the low six bits of the pointers are of interest, as
pixel sizes are always a power of 2 and window rows are always
phrase aligned.

When not in phrase mode, the top 3 bits of the shift value are
set to zero (2/26).

Source shifting is also used to extract bits for bit-to-byte
expansion in phrase mode.  This involves only the bottom three
bits of the shift value, and is based on the offset within the
phrase of the destination X pointer, in pixels.

Source shifting is disabled when srcen is not set.
*/
uint8_t dstxp = (dsta2 ? a2_x : a1_x) & 0x3F;
uint8_t srcxp = (dsta2 ? a1_x : a2_x) & 0x3F;
uint8_t shftv = ((dstxp - srcxp) << pixsize) & 0x3F;
/* The phrase mode alignment count is given by the phrase offset
of the first pixel, for bit to byte expansion */
uint8_t pobb = 0;

if (pixsize == 3)
	pobb = dstxp & 0x07;
if (pixsize == 4)
	pobb = dstxp & 0x03;
if (pixsize == 5)
	pobb = dstxp & 0x01;

bool pobbsel = phrase_mode && bcompen;
uint8_t loshd = (pobbsel ? pobb : shftv) & 0x07;
uint8_t shfti = (srcen || pobbsel ? (sshftld ? loshd : srcshift & 0x07) : 0);
/* Enable for high bits is srcen . phrase_mode */
shfti |= (srcen && phrase_mode ? (sshftld ? shftv & 0x38 : srcshift & 0x38) : 0);
srcshift = shfti;

				if (sreadx)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering SREADX state...");
#endif
//uint32_t srcAddr, pixAddr;
//ADDRGEN(srcAddr, pixAddr, gena2i, zaddr,
//	a1_x, a1_y, a1_base, a1_pitch, a1_pixsize, a1_width, a1_zoffset,
//	a2_x, a2_y, a2_base, a2_pitch, a2_pixsize, a2_width, a2_zoffset);
					srcd2 = srcd1;
					srcd1 = ((uint64_t)JaguarReadLong(address + 0, BLITTER) << 32)
						| (uint64_t)JaguarReadLong(address + 4, BLITTER);
//Kludge to take pixel size into account...
//Hmm. If we're not in phrase mode, this is most likely NOT going to be used...
//Actually, it would be--because of BCOMPEN expansion, for example...
if (!phrase_mode)
{
	if (bcompen)
		srcd1 >>= 56;
	else
	{
		if (pixsize == 5)
			srcd1 >>= 32;
		else if (pixsize == 4)
			srcd1 >>= 48;
		else
			srcd1 >>= 56;
	}
}//*/
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("    Source extra read address/pix address: %08X/%1X [%08X%08X]\n",
		address, pixAddr, (uint32_t)(srcd1 >> 32), (uint32_t)(srcd1 & 0xFFFFFFFF));
#endif
				}

				if (szreadx)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering SZREADX state...");
#endif
					srcz2 = srcz1;
					srcz1 = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog(" Src Z extra read address/pix address: %08X/%1X [%08X%08X]\n", address, pixAddr,
		(uint32_t)(dstz >> 32), (uint32_t)(dstz & 0xFFFFFFFF));
#endif
				}

				if (sread)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering SREAD state...");
#endif
//uint32_t srcAddr, pixAddr;
//ADDRGEN(srcAddr, pixAddr, gena2i, zaddr,
//	a1_x, a1_y, a1_base, a1_pitch, a1_pixsize, a1_width, a1_zoffset,
//	a2_x, a2_y, a2_base, a2_pitch, a2_pixsize, a2_width, a2_zoffset);
srcd2 = srcd1;
srcd1 = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);
//Kludge to take pixel size into account...
if (!phrase_mode)
{
	if (bcompen)
		srcd1 >>= 56;
	else
	{
		if (pixsize == 5)
			srcd1 >>= 32;
		else if (pixsize == 4)
			srcd1 >>= 48;
		else
			srcd1 >>= 56;
	}
}
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("     Source read address/pix address: %08X/%1X [%08X%08X]\n", address, pixAddr,
	(uint32_t)(srcd1 >> 32), (uint32_t)(srcd1 & 0xFFFFFFFF));
//fflush(stdout);
}
#endif
				}

				if (szread)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  Entering SZREAD state...");
//fflush(stdout);
}
#endif
					srcz2 = srcz1;
					srcz1 = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);
//Kludge to take pixel size into account... I believe that it only has to take 16BPP mode into account. Not sure tho.
if (!phrase_mode && pixsize == 4)
	srcz1 >>= 48;

#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	WriteLog("     Src Z read address/pix address: %08X/%1X [%08X%08X]\n", address, pixAddr,
		(uint32_t)(dstz >> 32), (uint32_t)(dstz & 0xFFFFFFFF));
}
#endif
				}

				if (dread)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering DREAD state...");
#endif
//uint32_t dstAddr, pixAddr;
//ADDRGEN(dstAddr, pixAddr, gena2i, zaddr,
//	a1_x, a1_y, a1_base, a1_pitch, a1_pixsize, a1_width, a1_zoffset,
//	a2_x, a2_y, a2_base, a2_pitch, a2_pixsize, a2_width, a2_zoffset);
dstd = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);
//Kludge to take pixel size into account...
if (!phrase_mode)
{
	if (pixsize == 5)
		dstd >>= 32;
	else if (pixsize == 4)
		dstd >>= 48;
	else
		dstd >>= 56;
}
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("       Dest read address/pix address: %08X/%1X [%08X%08X]\n", address,
		pixAddr, (uint32_t)(dstd >> 32), (uint32_t)(dstd & 0xFFFFFFFF));
#endif
				}

				if (dzread)
				{
// Is Z always 64 bit read? Or sometimes 16 bit (dependent on phrase_mode)?
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering DZREAD state...");
#endif
					dstz = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);
//Kludge to take pixel size into account... I believe that it only has to take 16BPP mode into account. Not sure tho.
if (!phrase_mode && pixsize == 4)
	dstz >>= 48;

#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("    Dest Z read address/pix address: %08X/%1X [%08X%08X]\n", address,
		pixAddr, (uint32_t)(dstz >> 32), (uint32_t)(dstz & 0xFFFFFFFF));
#endif
				}

// These vars should probably go further up in the code... !!! FIX !!!
// We can't preassign these unless they're static...
//uint64_t srcz = 0;			// These are assigned to shut up stupid compiler warnings--dwrite is ALWAYS asserted
//bool winhibit = false;
uint64_t srcz;
bool winhibit;
//NOTE: SRCSHADE requires GOURZ to be set to work properly--another Jaguar I bug
				if (dwrite)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("  Entering DWRITE state...");
#endif
//Counter is done on the dwrite state...! (We'll do it first, since it affects dstart/dend calculations.)
//Here's the voodoo for figuring the correct amount of pixels in phrase mode (or not):
					int8_t inct = -((dsta2 ? a2_x : a1_x) & 0x07);	// From INNER_CNT
					uint8_t inc = 0;
					inc = (!phrase_mode || (phrase_mode && (inct & 0x01)) ? 0x01 : 0x00);
					inc |= (phrase_mode && (((pixsize == 3 || pixsize == 4) && (inct & 0x02)) || pixsize == 5 && !(inct & 0x01)) ? 0x02 : 0x00);
					inc |= (phrase_mode && ((pixsize == 3 && (inct & 0x04)) || (pixsize == 4 && !(inct & 0x03))) ? 0x04 : 0x00);
					inc |= (phrase_mode && pixsize == 3 && !(inct & 0x07) ? 0x08 : 0x00);

					uint16_t oldicount = icount;	// Save icount to detect underflow...
					icount -= inc;

					if (icount == 0 || ((icount & 0x8000) && !(oldicount & 0x8000)))
						inner0 = true;
// X/Y stepping is also done here, I think...No. It's done when a1_add or a2_add is asserted...

//*********************************************************************************
//Start & end write mask computations...
//*********************************************************************************

uint8_t dstart = 0;

if (pixsize == 3)
	dstart = (dstxp & 0x07) << 3;
if (pixsize == 4)
	dstart = (dstxp & 0x03) << 4;
if (pixsize == 5)
	dstart = (dstxp & 0x01) << 5;

dstart = (phrase_mode ? dstart : pixAddr & 0x07);

//This is the other Jaguar I bug... Normally, should ALWAYS select a1_x here.
uint16_t dstxwr = (dsta2 ? a2_x : a1_x) & 0x7FFE;
uint16_t pseq = dstxwr ^ (a1_win_x & 0x7FFE);
pseq = (pixsize == 5 ? pseq : pseq & 0x7FFC);
pseq = ((pixsize & 0x06) == 4 ? pseq : pseq & 0x7FF8);
bool penden = clip_a1 && (pseq == 0);
uint8_t window_mask = 0;

if (pixsize == 3)
	window_mask = (a1_win_x & 0x07) << 3;
if (pixsize == 4)
	window_mask = (a1_win_x & 0x03) << 4;
if (pixsize == 5)
	window_mask = (a1_win_x & 0x01) << 5;

window_mask = (penden ? window_mask : 0);

/*
  Entering SREADX state... [dstart=0 dend=20 pwidth=8 srcshift=20]
    Source extra read address/pix address: 000095D0/0 [000004E40000001C]
  Entering A2_ADD state [a2_x=0002, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state... [dstart=0 dend=20 pwidth=8 srcshift=20]
    Source read address/pix address: 000095D8/0 [0054003800009814]
  Entering A2_ADD state [a2_x=0004, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 00037290/0 [dstart=0 dend=20 pwidth=8 srcshift=20][daas=0 dabs=0 dam=7 ds=1 daq=F] [0000001C00000000] (icount=026E, inc=4)
  Entering A1_ADD state [a1_x=0000, a1_y=0000, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...

(icount=026E, inc=4)
icount & 0x03 = 0x02
         << 4 = 0x20

window_mask = 0x1000

Therefore, it chooses the inner_mask over the window_mask every time! Argh!
This is because we did this wrong:
Innerm[3-5]	:= AN2 (inner_mask[3-5], imb[3-5], inner0);
NOTE! This doesn't fix the problem because inner0 is asserted too late to help here. !!! FIX !!! [Should be DONE]
*/

/* The mask to be used if within one phrase of the end of the inner
loop, similarly */
uint8_t inner_mask = 0;

if (pixsize == 3)
	inner_mask = (icount & 0x07) << 3;
if (pixsize == 4)
	inner_mask = (icount & 0x03) << 4;
if (pixsize == 5)
	inner_mask = (icount & 0x01) << 5;
if (!inner0)
	inner_mask = 0;
/* The actual mask used should be the lesser of the window masks and
the inner mask, where is all cases 000 means 1000. */
window_mask = (window_mask == 0 ? 0x40 : window_mask);
inner_mask = (inner_mask == 0 ? 0x40 : inner_mask);
uint8_t emask = (window_mask > inner_mask ? inner_mask : window_mask);
/* The mask to be used for the pixel size, to which must be added
the bit offset */
uint8_t pma = pixAddr + (1 << pixsize);
/* Select the mask */
uint8_t dend = (phrase_mode ? emask : pma);

/* The cycle width in phrase mode is normally one phrase.  However,
at the start and end it may be narrower.  The start and end masks
are used to generate this.  The width is given by:

	8 - start mask - (8 - end mask)
=	end mask - start mask

This is only used for writes in phrase mode.
Start and end from the address level of the pipeline are used.
*/
uint8_t pwidth = (((dend | dstart) & 0x07) == 0 ? 0x08 : (dend - dstart) & 0x07);

//uint32_t dstAddr, pixAddr;
//ADDRGEN(dstAddr, pixAddr, gena2i, zaddr,
//	a1_x, a1_y, a1_base, a1_pitch, a1_pixsize, a1_width, a1_zoffset,
//	a2_x, a2_y, a2_base, a2_pitch, a2_pixsize, a2_width, a2_zoffset);
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("     Dest write address/pix address: %08X/%1X", address, pixAddr);
#endif

//More testing... This is almost certainly wrong, but how else does this work???
//Seems to kinda work... But still, this doesn't seem to make any sense!
if (phrase_mode && !dsten)
	dstd = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);

//Testing only... for now...
//This is wrong because the write data is a combination of srcd and dstd--either run
//thru the LFU or in PATDSEL or ADDDSEL mode. [DONE now, thru DATA module]
// Precedence is ADDDSEL > PATDSEL > LFU.
//Also, doesn't take into account the start & end masks, or the phrase width...
//Now it does!

// srcd2 = xxxx xxxx 0123 4567, srcd = 8901 2345 xxxx xxxx, srcshift = $20 (32)
uint64_t srcd = (srcd2 << (64 - srcshift)) | (srcd1 >> srcshift);
//bleh, ugly ugly ugly
if (srcshift == 0)
	srcd = srcd1;

//NOTE: This only works with pixel sizes less than 8BPP...
//DOUBLE NOTE: Still need to do regression testing to ensure that this doesn't break other stuff... !!! CHECK !!!
if (!phrase_mode && srcshift != 0)
	srcd = ((srcd2 & 0xFF) << (8 - srcshift)) | ((srcd1 & 0xFF) >> srcshift);

//Z DATA() stuff done here... And it has to be done before any Z shifting...
//Note that we need to have phrase mode start/end support here... (Not since we moved it from dzwrite...!)
/*
Here are a couple of Cybermorph blits with Z:
$00113078	// DSTEN DSTENZ DSTWRZ CLIP_A1 GOURD GOURZ PATDSEL ZMODE=4
$09900F39	// SRCEN DSTEN DSTENZ DSTWRZ UPDA1 UPDA1F UPDA2 DSTA2 ZMODE=4 LFUFUNC=C DCOMPEN

We're having the same phrase mode overwrite problem we had with the pixels... !!! FIX !!!
Odd. It's equating 0 with 0... Even though ZMODE is $04 (less than)!
*/
if (gourz)
{
/*
void ADDARRAY(uint16_t * addq, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode,
	uint64_t dstd, uint32_t iinc, uint8_t initcin[], uint64_t initinc, uint16_t initpix,
	uint32_t istep, uint64_t patd, uint64_t srcd, uint64_t srcz1, uint64_t srcz2,
	uint32_t zinc, uint32_t zstep)
*/
	uint16_t addq[4];
	uint8_t initcin[4] = { 0, 0, 0, 0 };
	ADDARRAY(addq, 7/*daddasel*/, 6/*daddbsel*/, 0/*daddmode*/, 0, 0, initcin, 0, 0, 0, 0, 0, srcz1, srcz2, zinc, 0);
	srcz2 = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
	ADDARRAY(addq, 6/*daddasel*/, 7/*daddbsel*/, 1/*daddmode*/, 0, 0, initcin, 0, 0, 0, 0, 0, srcz1, srcz2, zinc, 0);
	srcz1 = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];

#if 0//def VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("\n[srcz1=%08X%08X, srcz2=%08X%08X, zinc=%08X",
		(uint32_t)(srcz1 >> 32), (uint32_t)(srcz1 & 0xFFFFFFFF),
		(uint32_t)(srcz2 >> 32), (uint32_t)(srcz2 & 0xFFFFFFFF), zinc);
#endif
}

uint8_t zSrcShift = srcshift & 0x30;
srcz = (srcz2 << (64 - zSrcShift)) | (srcz1 >> zSrcShift);
//bleh, ugly ugly ugly
if (zSrcShift == 0)
	srcz = srcz1;

#if 0//def VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog(" srcz=%08X%08X]\n", (uint32_t)(srcz >> 32), (uint32_t)(srcz & 0xFFFFFFFF));
#endif

//When in SRCSHADE mode, it adds the IINC to the read source (from LFU???)
//According to following line, it gets LFU mode. But does it feed the source into the LFU
//after the add?
//Dest write address/pix address: 0014E83E/0 [dstart=0 dend=10 pwidth=8 srcshift=0][daas=4 dabs=5 dam=7 ds=1 daq=F] [0000000000006505] (icount=003F, inc=1)
//Let's try this:
if (srcshade)
{
//NOTE: This is basically doubling the work done by DATA--since this is what
//      ADDARRAY is loaded with when srschshade is enabled... !!! FIX !!!
//      Also note that it doesn't work properly unless GOURZ is set--there's the clue!
	uint16_t addq[4];
	uint8_t initcin[4] = { 0, 0, 0, 0 };
	ADDARRAY(addq, 4/*daddasel*/, 5/*daddbsel*/, 7/*daddmode*/, dstd, iinc, initcin, 0, 0, 0, patd, srcd, 0, 0, 0, 0);
	srcd = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
}
//Seems to work... Not 100% sure tho.
//end try this

//Temporary kludge, to see if the fractional pattern does anything...
//This works, BTW
//But it seems to mess up in Cybermorph... the shading should be smooth but it isn't...
//Seems the carry out is lost again... !!! FIX !!! [DONE--see below]
if (patfadd)
{
	uint16_t addq[4];
	uint8_t initcin[4] = { 0, 0, 0, 0 };
	ADDARRAY(addq, 4/*daddasel*/, 4/*daddbsel*/, 0/*daddmode*/, dstd, iinc, initcin, 0, 0, 0, patd, srcd, 0, 0, 0, 0);
	srcd1 = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
}

//Note that we still don't take atick[0] & [1] into account here, so this will skip half of the data needed... !!! FIX !!!
//Not yet enumerated: dbinh, srcdread, srczread
//Also, should do srcshift on the z value in phrase mode... !!! FIX !!! [DONE]
//As well as add a srcz variable we can set external to this state... !!! FIX !!! [DONE]

uint64_t wdata;
uint8_t dcomp, zcomp;
DATA(wdata, dcomp, zcomp, winhibit,
	true, cmpdst, daddasel, daddbsel, daddmode, daddq_sel, data_sel, 0/*dbinh*/,
	dend, dstart, dstd, iinc, lfufunc, patd, patdadd,
	phrase_mode, srcd, false/*srcdread*/, false/*srczread*/, srcz2add, zmode,
	bcompen, bkgwren, dcompen, icount & 0x07, pixsize,
	srcz, dstz, zinc);
/*
Seems that the phrase mode writes with DCOMPEN and DSTEN are corrupting inside of DATA: !!! FIX !!!
It's fairly random as well. 7CFE -> 7DFE, 7FCA -> 78CA, 7FA4 -> 78A4, 7F88 -> 8F88
It could be related to an uninitialized variable, like the zmode bug...
[DONE]
It was a bug in the dech38el data--it returned $FF for ungated instead of $00...

Blit! (CMD = 09800609)
Flags: SRCEN DSTEN UPDA1 UPDA2 LFUFUNC=C DCOMPEN
  count = 10 x 12
  a1_base = 00110000, a2_base = 0010B2A8
  a1_x = 004B, a1_y = 00D8, a1_frac_x = 0000, a1_frac_y = 0000, a2_x = 0704, a2_y = 0000
  a1_step_x = FFF3, a1_step_y = 0001, a1_stepf_x = 0000, a1_stepf_y = 0000, a2_step_x = FFFC, a2_step_y = 0000
  a1_inc_x = 0000, a1_inc_y = 0000, a1_incf_x = 0000, a1_incf_y = 0000
  a1_win_x = 0000, a1_win_y = 0000, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+phr/+0 a2add=+phr/+0
  a1_pixsize = 4, a2_pixsize = 4
   srcd=0000000000000000  dstd=0000000000000000 patd=0000000000000000 iinc=00000000
  srcz1=0000000000000000 srcz2=0000000000000000 dstz=0000000000000000 zinc=00000000, coll=0
  Phrase mode is ON
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
  Entering SREAD state...    Source read address/pix address: 0010C0B0/0 [0000000078047804]
  Entering A2_ADD state [a2_x=0704, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DREAD state...
      Dest read address/pix address: 00197240/0 [0000000000000028]
  Entering DWRITE state...
     Dest write address/pix address: 00197240/0 [dstart=30 dend=40 pwidth=8 srcshift=30][daas=0 dabs=0 dam=7 ds=1 daq=F] [0000000000000028] (icount=0009, inc=1)
  Entering A1_ADD state [a1_x=004B, a1_y=00D8, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state...    Source read address/pix address: 0010C0B8/0 [7804780478047804]
  Entering A2_ADD state [a2_x=0708, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DREAD state...
      Dest read address/pix address: 00197260/0 [0028000000200008]
  Entering DWRITE state...
     Dest write address/pix address: 00197260/0 [dstart=0 dend=40 pwidth=8 srcshift=30][daas=0 dabs=0 dam=7 ds=1 daq=F] [0028780478047804] (icount=0005, inc=4)
  Entering A1_ADD state [a1_x=004C, a1_y=00D8, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state...    Source read address/pix address: 0010C0C0/0 [0000000000000000]
  Entering A2_ADD state [a2_x=070C, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DREAD state...
      Dest read address/pix address: 00197280/0 [0008001800180018]
  Entering DWRITE state...
     Dest write address/pix address: 00197280/0 [dstart=0 dend=40 pwidth=8 srcshift=30][daas=0 dabs=0 dam=7 ds=1 daq=F] [7804780478040018] (icount=0001, inc=4)
  Entering A1_ADD state [a1_x=0050, a1_y=00D8, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state...    Source read address/pix address: 0010C0C8/0 [000078047BFE7BFE]
  Entering A2_ADD state [a2_x=0710, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering DREAD state...
      Dest read address/pix address: 001972A0/0 [0008002000000000]
  Entering DWRITE state...
     Dest write address/pix address: 001972A0/0 [dstart=0 dend=10 pwidth=8 srcshift=30][daas=0 dabs=0 dam=7 ds=1 daq=F] [0008002000000000] (icount=FFFD, inc=4)
  Entering A1_ADD state [a1_x=0054, a1_y=00D8, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering IDLE_INNER state...
*/

//Why isn't this taken care of in DATA? Because, DATA is modifying its local copy instead of the one used here.
//!!! FIX !!! [DONE]
//if (patdadd)
//	patd = wdata;

//if (patfadd)
//	srcd1 = wdata;

/*
DEF ADDRCOMP (
	a1_outside	// A1 pointer is outside window bounds
	:OUT;
INT16/	a1_x
INT16/	a1_y
INT15/	a1_win_x
INT15/	a1_win_y
	:IN);
BEGIN

// The address is outside if negative, or if greater than or equal
// to the window size

A1_xcomp	:= MAG_15 (a1xgr, a1xeq, a1xlt, a1_x{0..14}, a1_win_x{0..14});
A1_ycomp	:= MAG_15 (a1ygr, a1yeq, a1ylt, a1_y{0..14}, a1_win_y{0..14});
A1_outside	:= OR6 (a1_outside, a1_x{15}, a1xgr, a1xeq, a1_y{15}, a1ygr, a1yeq);
*/
//NOTE: There seems to be an off-by-one bug here in the clip_a1 section... !!! FIX !!!
//      Actually, seems to be related to phrase mode writes...
//      Or is it? Could be related to non-15-bit compares as above?
if (clip_a1 && ((a1_x & 0x8000) || (a1_y & 0x8000) || (a1_x >= a1_win_x) || (a1_y >= a1_win_y)))
	winhibit = true;

if (!winhibit)
{
	if (phrase_mode)
	{
		JaguarWriteLong(address + 0, wdata >> 32, BLITTER);
		JaguarWriteLong(address + 4, wdata & 0xFFFFFFFF, BLITTER);
	}
	else
	{
		if (pixsize == 5)
			JaguarWriteLong(address, wdata & 0xFFFFFFFF, BLITTER);
		else if (pixsize == 4)
			JaguarWriteWord(address, wdata & 0x0000FFFF, BLITTER);
		else
			JaguarWriteByte(address, wdata & 0x000000FF, BLITTER);
	}
}

#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	WriteLog(" [%08X%08X]%s", (uint32_t)(wdata >> 32), (uint32_t)(wdata & 0xFFFFFFFF), (winhibit ? "[X]" : ""));
	WriteLog(" (icount=%04X, inc=%u)\n", icount, (uint16_t)inc);
	WriteLog("    [dstart=%X dend=%X pwidth=%X srcshift=%X]", dstart, dend, pwidth, srcshift);
	WriteLog("[daas=%X dabs=%X dam=%X ds=%X daq=%s]\n", daddasel, daddbsel, daddmode, data_sel, (daddq_sel ? "T" : "F"));
}
#endif
				}

				if (dzwrite)
				{
// OK, here's the big insight: When NOT in GOURZ mode, srcz1 & 2 function EXACTLY the same way that
// srcd1 & 2 work--there's an implicit shift from srcz1 to srcz2 whenever srcz1 is read.
// OTHERWISE, srcz1 is the integer for the computed Z and srcz2 is the fractional part.
// Writes to srcz1 & 2 follow the same pattern as the other 64-bit registers--low 32 at the low address,
// high 32 at the high address (little endian!).
// NOTE: GOURZ is still not properly supported. Check patd/patf handling...
//       Phrase mode start/end masks are not properly supported either...
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	WriteLog("  Entering DZWRITE state...");
	WriteLog("  Dest Z write address/pix address: %08X/%1X [%08X%08X]\n", address,
		pixAddr, (uint32_t)(srcz >> 32), (uint32_t)(srcz & 0xFFFFFFFF));
}
#endif
//This is not correct... !!! FIX !!!
//Should be OK now... We'll see...
//Nope. Having the same starstep write problems in phrase mode as we had with pixels... !!! FIX !!!
//This is not causing the problem in Hover Strike... :-/
//The problem was with the SREADX not shifting. Still problems with Z comparisons & other text in pregame screen...
if (!winhibit)
{
	if (phrase_mode)
	{
		JaguarWriteLong(address + 0, srcz >> 32, BLITTER);
		JaguarWriteLong(address + 4, srcz & 0xFFFFFFFF, BLITTER);
	}
	else
	{
		if (pixsize == 4)
			JaguarWriteWord(address, srcz & 0x0000FFFF, BLITTER);
	}
}//*/
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
//	printf(" [%08X%08X]\n", (uint32_t)(srcz >> 32), (uint32_t)(srcz & 0xFFFFFFFF));
//	fflush(stdout);
//printf(" [dstart=%X dend=%X pwidth=%X srcshift=%X]", dstart, dend, pwidth, srcshift);
	WriteLog("    [dstart=? dend=? pwidth=? srcshift=%X]", srcshift);
	WriteLog("[daas=%X dabs=%X dam=%X ds=%X daq=%s]\n", daddasel, daddbsel, daddmode, data_sel, (daddq_sel ? "T" : "F"));
//	fflush(stdout);
}
#endif
				}

/*
This is because the address generator was using only 15 bits of the X when it should have
used 16!

There's a slight problem here: The X pointer isn't wrapping like it should when it hits
the edge of the window... Notice how the X isn't reset at the edge of the window:

Blit! (CMD = 00010000)
Flags: PATDSEL
  count = 160 x 261
  a1_base = 000E8008, a2_base = 0001FA68
  a1_x = 0000, a1_y = 0000, a1_frac_x = 0000, a1_frac_y = 0000, a2_x = 0000, a2_y = 0000
  a1_step_x = 0000, a1_step_y = 0000, a1_stepf_x = 0000, a1_stepf_y = 0000, a2_step_x = 0000, a2_step_y = 0000
  a1_inc_x = 0000, a1_inc_y = 0000, a1_incf_x = 0000, a1_incf_y = 0000
  a1_win_x = 0000, a1_win_y = 0000, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+phr/+0 a2add=+phr/+0
  a1_pixsize = 5, a2_pixsize = 5
   srcd=7717771777177717  dstd=0000000000000000 patd=7730773077307730 iinc=00000000
  srcz1=0000000000000000 srcz2=0000000000000000 dstz=0000000000000000 zinc=00000000, coll=0
  Phrase mode is ON
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
  Entering DWRITE state...     Dest write address/pix address: 000E8008/0 [7730773077307730] (icount=009E, inc=2)
 srcz=0000000000000000][dcomp=AA zcomp=00 dbinh=00]
[srcz=0000000000000000 dstz=0000000000000000 zwdata=0000000000000000 mask=7FFF]
    [dstart=0 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F]
  Entering A1_ADD state [a1_x=0000, a1_y=0000, addasel=0, addbsel=0, modx=1, addareg=F, adda_xconst=1, adda_yconst=0]...
  Entering DWRITE state...     Dest write address/pix address: 000E8018/0 [7730773077307730] (icount=009C, inc=2)
 srcz=0000000000000000][dcomp=AA zcomp=00 dbinh=00]
[srcz=0000000000000000 dstz=0000000000000000 zwdata=0000000000000000 mask=7FFF]
    [dstart=0 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F]
  Entering A1_ADD state [a1_x=0002, a1_y=0000, addasel=0, addbsel=0, modx=1, addareg=F, adda_xconst=1, adda_yconst=0]...

...

  Entering A1_ADD state [a1_x=009C, a1_y=0000, addasel=0, addbsel=0, modx=1, addareg=F, adda_xconst=1, adda_yconst=0]...
  Entering DWRITE state...     Dest write address/pix address: 000E84F8/0 [7730773077307730] (icount=0000, inc=2)
 srcz=0000000000000000][dcomp=AA zcomp=00 dbinh=00]
[srcz=0000000000000000 dstz=0000000000000000 zwdata=0000000000000000 mask=7FFF]
    [dstart=0 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F]
  Entering A1_ADD state [a1_x=009E, a1_y=0000, addasel=0, addbsel=0, modx=1, addareg=F, adda_xconst=1, adda_yconst=0]...
  Entering IDLE_INNER state...

  Leaving INNER state... (ocount=0104)
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]

  Entering INNER state...
  Entering DWRITE state...     Dest write address/pix address: 000E8508/0 [7730773077307730] (icount=009E, inc=2)
 srcz=0000000000000000][dcomp=AA zcomp=00 dbinh=00]
[srcz=0000000000000000 dstz=0000000000000000 zwdata=0000000000000000 mask=7FFF]
    [dstart=0 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F]
  Entering A1_ADD state [a1_x=00A0, a1_y=0000, addasel=0, addbsel=0, modx=1, addareg=F, adda_xconst=1, adda_yconst=0]...
  Entering DWRITE state...     Dest write address/pix address: 000E8518/0 [7730773077307730] (icount=009C, inc=2)
 srcz=0000000000000000][dcomp=AA zcomp=00 dbinh=00]
[srcz=0000000000000000 dstz=0000000000000000 zwdata=0000000000000000 mask=7FFF]
    [dstart=0 dend=40 pwidth=8 srcshift=0][daas=0 dabs=0 dam=7 ds=0 daq=F]
  Entering A1_ADD state [a1_x=00A2, a1_y=0000, addasel=0, addbsel=0, modx=1, addareg=F, adda_xconst=1, adda_yconst=0]...

*/

				if (a1_add)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
//printf("  Entering A1_ADD state [addasel=%X, addbsel=%X, modx=%X, addareg=%s, adda_xconst=%u, adda_yconst=%s]...\n", addasel, addbsel, modx, (addareg ? "T" : "F"), adda_xconst, (adda_yconst ? "1" : "0"));
WriteLog("  Entering A1_ADD state [a1_x=%04X, a1_y=%04X, addasel=%X, addbsel=%X, modx=%X, addareg=%s, adda_xconst=%u, adda_yconst=%s]...\n", a1_x, a1_y, addasel, addbsel, modx, (addareg ? "T" : "F"), adda_xconst, (adda_yconst ? "1" : "0"));
//fflush(stdout);
}
#endif
int16_t adda_x, adda_y, addb_x, addb_y, data_x, data_y, addq_x, addq_y;
ADDAMUX(adda_x, adda_y, addasel, a1_step_x, a1_step_y, a1_stepf_x, a1_stepf_y, a2_step_x, a2_step_y,
	a1_inc_x, a1_inc_y, a1_incf_x, a1_incf_y, adda_xconst, adda_yconst, addareg, suba_x, suba_y);
ADDBMUX(addb_x, addb_y, addbsel, a1_x, a1_y, a2_x, a2_y, a1_frac_x, a1_frac_y);
ADDRADD(addq_x, addq_y, a1fracldi, adda_x, adda_y, addb_x, addb_y, modx, suba_x, suba_y);

#if 0//def VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  [adda_x=%d, adda_y=%d, addb_x=%d, addb_y=%d, addq_x=%d, addq_y=%d]\n", adda_x, adda_y, addb_x, addb_y, addq_x, addq_y);
//fflush(stdout);
}
#endif
//Now, write to what???
//a2ptrld comes from a2ptrldi...
//I believe it's addbsel that determines the writeback...
// This is where atick[0] & [1] come in, in determining which part (fractional, integer)
// gets written to...
//a1_x = addq_x;
//a1_y = addq_y;
//Kludge, to get A1 channel increment working...
if (a1addx == 3)
{
	a1_frac_x = addq_x, a1_frac_y = addq_y;

addasel = 2, addbsel = 0, a1fracldi = false;
ADDAMUX(adda_x, adda_y, addasel, a1_step_x, a1_step_y, a1_stepf_x, a1_stepf_y, a2_step_x, a2_step_y,
	a1_inc_x, a1_inc_y, a1_incf_x, a1_incf_y, adda_xconst, adda_yconst, addareg, suba_x, suba_y);
ADDBMUX(addb_x, addb_y, addbsel, a1_x, a1_y, a2_x, a2_y, a1_frac_x, a1_frac_y);
ADDRADD(addq_x, addq_y, a1fracldi, adda_x, adda_y, addb_x, addb_y, modx, suba_x, suba_y);

	a1_x = addq_x, a1_y = addq_y;
}
else
	a1_x = addq_x, a1_y = addq_y;
				}

				if (a2_add)
				{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
//printf("  Entering A2_ADD state [addasel=%X, addbsel=%X, modx=%X, addareg=%s, adda_xconst=%u, adda_yconst=%s]...\n", addasel, addbsel, modx, (addareg ? "T" : "F"), adda_xconst, (adda_yconst ? "1" : "0"));
WriteLog("  Entering A2_ADD state [a2_x=%04X, a2_y=%04X, addasel=%X, addbsel=%X, modx=%X, addareg=%s, adda_xconst=%u, adda_yconst=%s]...\n", a2_x, a2_y, addasel, addbsel, modx, (addareg ? "T" : "F"), adda_xconst, (adda_yconst ? "1" : "0"));
//fflush(stdout);
}
#endif
//void ADDAMUX(int16_t &adda_x, int16_t &adda_y, uint8_t addasel, int16_t a1_step_x, int16_t a1_step_y,
//	int16_t a1_stepf_x, int16_t a1_stepf_y, int16_t a2_step_x, int16_t a2_step_y,
//	int16_t a1_inc_x, int16_t a1_inc_y, int16_t a1_incf_x, int16_t a1_incf_y, uint8_t adda_xconst,
//	bool adda_yconst, bool addareg, bool suba_x, bool suba_y)
//void ADDBMUX(int16_t &addb_x, int16_t &addb_y, uint8_t addbsel, int16_t a1_x, int16_t a1_y,
//	int16_t a2_x, int16_t a2_y, int16_t a1_frac_x, int16_t a1_frac_y)
//void ADDRADD(int16_t &addq_x, int16_t &addq_y, bool a1fracldi,
//	int16_t adda_x, int16_t adda_y, int16_t addb_x, int16_t addb_y, uint8_t modx, bool suba_x, bool suba_y)
//void DATAMUX(int16_t &data_x, int16_t &data_y, uint32_t gpu_din, int16_t addq_x, int16_t addq_y, bool addqsel)
int16_t adda_x, adda_y, addb_x, addb_y, data_x, data_y, addq_x, addq_y;
ADDAMUX(adda_x, adda_y, addasel, a1_step_x, a1_step_y, a1_stepf_x, a1_stepf_y, a2_step_x, a2_step_y,
	a1_inc_x, a1_inc_y, a1_incf_x, a1_incf_y, adda_xconst, adda_yconst, addareg, suba_x, suba_y);
ADDBMUX(addb_x, addb_y, addbsel, a1_x, a1_y, a2_x, a2_y, a1_frac_x, a1_frac_y);
ADDRADD(addq_x, addq_y, a1fracldi, adda_x, adda_y, addb_x, addb_y, modx, suba_x, suba_y);

#if 0//def VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  [adda_x=%d, adda_y=%d, addb_x=%d, addb_y=%d, addq_x=%d, addq_y=%d]\n", adda_x, adda_y, addb_x, addb_y, addq_x, addq_y);
//fflush(stdout);
}
#endif
//Now, write to what???
//a2ptrld comes from a2ptrldi...
//I believe it's addbsel that determines the writeback...
a2_x = addq_x;
a2_y = addq_y;
				}
			}
/*
Flags: SRCEN CLIP_A1 UPDA1 UPDA1F UPDA2 DSTA2 GOURZ ZMODE=0 LFUFUNC=C SRCSHADE
  count = 64 x 55
  a1_base = 0015B000, a2_base = 0014B000
  a1_x = 0000, a1_y = 0000, a1_frac_x = 8000, a1_frac_y = 8000, a2_x = 001F, a2_y = 0038
  a1_step_x = FFFFFFC0, a1_step_y = 0001, a1_stepf_x = 0000, a1_stepf_y = 2AAA, a2_step_x = FFFFFFC0, a2_step_y = 0001
  a1_inc_x = 0001, a1_inc_y = 0000, a1_incf_x = 0000, a1_incf_y = 0000
  a1_win_x = 0040, a1_win_y = 0040, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+inc/+0 a2add=+1/+0
  a1_pixsize = 4, a2_pixsize = 4
   srcd=FF00FF00FF00FF00  dstd=0000000000000000 patd=0000000000000000 iinc=00000000
  srcz1=0000000000000000 srcz2=0000000000000000 dstz=0000000000000000 zinc=00000000, col=0
  Phrase mode is off
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
  Entering SREAD state...    Source read address/pix address: 0015B000/0 [6505650565056505]
  Entering A1_ADD state [a1_x=0000, a1_y=0000, addasel=3, addbsel=2, modx=0, addareg=T, adda_xconst=7, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0014E83E/0 [dstart=0 dend=10 pwidth=8 srcshift=0][daas=4 dabs=5 dam=7 ds=1 daq=F] [0000000000006505] (icount=003F, inc=1)
  Entering A2_ADD state [a2_x=001F, a2_y=0038, addasel=0, addbsel=1, modx=0, addareg=F, adda_xconst=0, adda_yconst=0]...
  Entering SREAD state...    Source read address/pix address: 0015B000/0 [6505650565056505]
  Entering A1_ADD state [a1_x=FFFF8000, a1_y=FFFF8000, addasel=3, addbsel=2, modx=0, addareg=T, adda_xconst=7, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0014E942/0 [dstart=0 dend=10 pwidth=8 srcshift=0][daas=4 dabs=5 dam=7 ds=1 daq=F] [0000000000006505] (icount=003E, inc=1)
  Entering A2_ADD state [a2_x=0021, a2_y=0039, addasel=0, addbsel=1, modx=0, addareg=F, adda_xconst=0, adda_yconst=0]...
  Entering SREAD state...    Source read address/pix address: 0015B000/0 [6505650565056505]
  Entering A1_ADD state [a1_x=FFFF8000, a1_y=FFFF8000, addasel=3, addbsel=2, modx=0, addareg=T, adda_xconst=7, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0014EA46/0 [dstart=0 dend=10 pwidth=8 srcshift=0][daas=4 dabs=5 dam=7 ds=1 daq=F] [0000000000006505] (icount=003D, inc=1)
  Entering A2_ADD state [a2_x=0023, a2_y=003A, addasel=0, addbsel=1, modx=0, addareg=F, adda_xconst=0, adda_yconst=0]...
  Entering SREAD state...    Source read address/pix address: 0015B000/0 [6505650565056505]
  Entering A1_ADD state [a1_x=FFFF8000, a1_y=FFFF8000, addasel=3, addbsel=2, modx=0, addareg=T, adda_xconst=7, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0014EB4A/0 [dstart=0 dend=10 pwidth=8 srcshift=0][daas=4 dabs=5 dam=7 ds=1 daq=F] [0000000000006505] (icount=003C, inc=1)
  Entering A2_ADD state [a2_x=0025, a2_y=003B, addasel=0, addbsel=1, modx=0, addareg=F, adda_xconst=0, adda_yconst=0]...
  ...
  Entering SREAD state...    Source read address/pix address: 0015B000/0 [6505650565056505]
  Entering A1_ADD state [a1_x=FFFF8000, a1_y=FFFF8000, addasel=3, addbsel=2, modx=0, addareg=T, adda_xconst=7, adda_yconst=0]...
  Entering DWRITE state...
     Dest write address/pix address: 0015283A/0 [dstart=0 dend=10 pwidth=8 srcshift=0][daas=4 dabs=5 dam=7 ds=1 daq=F] [0000000000006505] (icount=0000, inc=1)
  Entering A2_ADD state [a2_x=009D, a2_y=0077, addasel=0, addbsel=1, modx=0, addareg=F, adda_xconst=0, adda_yconst=0]...
  Entering IDLE_INNER state...
  Leaving INNER state... (ocount=0036)
  [in=F a1f=T a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering A1FUPDATE state...
  [in=F a1f=F a1=T zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering A1UPDATE state... (-32768/-32768 -> 32704/-32767)
  [in=F a1f=F a1=F zf=F z=F a2=T iif=F iii=F izf=F izi=F]
  Entering A2UPDATE state... (159/120 -> 95/121)
  [in=T a1f=F a1=F zf=F z=F a2=F iif=F iii=F izf=F izi=F]
  Entering INNER state...
*/

#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  Leaving INNER state...");
//fflush(stdout);
}
#endif
			indone = true;
// The outer counter is updated here as well on the clock cycle...

/* the inner loop is started whenever another state is about to
cause the inner state to go active */
//Instart		:= ND7 (instart, innert[0], innert[2..7]);

//Actually, it's done only when inner gets asserted without the 2nd line of conditions
//(inner AND !indone)
//fixed now...
//Since we don't get here until the inner loop is finished (indone = true) we can get
//away with doing it here...!
			ocount--;

			if (ocount == 0)
				outer0 = true;
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog(" (ocount=%04X)\n", ocount);
//fflush(stdout);
}
#endif
		}

		if (a1fupdate)
		{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  Entering A1FUPDATE state...\n");
//fflush(stdout);
}
#endif
			uint32_t a1_frac_xt = (uint32_t)a1_frac_x + (uint32_t)a1_stepf_x;
			uint32_t a1_frac_yt = (uint32_t)a1_frac_y + (uint32_t)a1_stepf_y;
			a1FracCInX = a1_frac_xt >> 16;
			a1FracCInY = a1_frac_yt >> 16;
			a1_frac_x = (uint16_t)(a1_frac_xt & 0xFFFF);
			a1_frac_y = (uint16_t)(a1_frac_yt & 0xFFFF);
		}

		if (a1update)
		{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  Entering A1UPDATE state... (%d/%d -> ", a1_x, a1_y);
//fflush(stdout);
}
#endif
			a1_x += a1_step_x + a1FracCInX;
			a1_y += a1_step_y + a1FracCInY;
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("%d/%d)\n", a1_x, a1_y);
//fflush(stdout);
}
#endif
		}

		if (a2update)
		{
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("  Entering A2UPDATE state... (%d/%d -> ", a2_x, a2_y);
//fflush(stdout);
}
#endif
			a2_x += a2_step_x;
			a2_y += a2_step_y;
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("%d/%d)\n", a2_x, a2_y);
//fflush(stdout);
}
#endif
		}
	}

// We never get here! !!! FIX !!!

#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	WriteLog("Done!\na1_x=%04X a1_y=%04X a1_frac_x=%04X a1_frac_y=%04X a2_x=%04X a2_y%04X\n",
		GET16(blitter_ram, A1_PIXEL + 2),
		GET16(blitter_ram, A1_PIXEL + 0),
		GET16(blitter_ram, A1_FPIXEL + 2),
		GET16(blitter_ram, A1_FPIXEL + 0),
		GET16(blitter_ram, A2_PIXEL + 2),
		GET16(blitter_ram, A2_PIXEL + 0));
//	fflush(stdout);
}
#endif

	// Write values back to registers (in real blitter, these are continuously updated)
	SET16(blitter_ram, A1_PIXEL + 2, a1_x);
	SET16(blitter_ram, A1_PIXEL + 0, a1_y);
	SET16(blitter_ram, A1_FPIXEL + 2, a1_frac_x);
	SET16(blitter_ram, A1_FPIXEL + 0, a1_frac_y);
	SET16(blitter_ram, A2_PIXEL + 2, a2_x);
	SET16(blitter_ram, A2_PIXEL + 0, a2_y);

#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
{
	WriteLog("Writeback!\na1_x=%04X a1_y=%04X a1_frac_x=%04X a1_frac_y=%04X a2_x=%04X a2_y%04X\n",
		GET16(blitter_ram, A1_PIXEL + 2),
		GET16(blitter_ram, A1_PIXEL + 0),
		GET16(blitter_ram, A1_FPIXEL + 2),
		GET16(blitter_ram, A1_FPIXEL + 0),
		GET16(blitter_ram, A2_PIXEL + 2),
		GET16(blitter_ram, A2_PIXEL + 0));
//	fflush(stdout);
}
#endif
}


/*
	int16_t a1_x = (int16_t)GET16(blitter_ram, A1_PIXEL + 2);
	int16_t a1_y = (int16_t)GET16(blitter_ram, A1_PIXEL + 0);
	uint16_t a1_frac_x = GET16(blitter_ram, A1_FPIXEL + 2);
	uint16_t a1_frac_y = GET16(blitter_ram, A1_FPIXEL + 0);
	int16_t a2_x = (int16_t)GET16(blitter_ram, A2_PIXEL + 2);
	int16_t a2_y = (int16_t)GET16(blitter_ram, A2_PIXEL + 0);

Seems that the ending a1_x should be written between blits, but it doesn't seem to be...

Blit! (CMD = 01800000)
Flags: LFUFUNC=C
  count = 28672 x 1
  a1_base = 00050000, a2_base = 00070000
  a1_x = 0000, a1_y = 0000, a1_frac_x = 49CD, a1_frac_y = 0000, a2_x = 0033, a2_y = 0001
  a1_step_x = 0000, a1_step_y = 0000, a1_stepf_x = 939A, a1_stepf_y = 0000, a2_step_x = 0000, a2_step_y = 0000
  a1_inc_x = 0000, a1_inc_y = 0000, a1_incf_x = 0000, a1_incf_y = 0000
  a1_win_x = 0100, a1_win_y = 0020, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+phr/+0 a2add=+phr/+0
  a1_pixsize = 4, a2_pixsize = 3
   srcd=DEDEDEDEDEDEDEDE  dstd=0000000000000000 patd=0000000000000000 iinc=00000000
  srcz1=0000000000000000 srcz2=0000000000000000 dstz=0000000000000000 zinc=00000000, coll=0
  Phrase mode is ON

Blit! (CMD = 01800000)
Flags: LFUFUNC=C
  count = 28672 x 1
  a1_base = 00050000, a2_base = 00070000
  a1_x = 0000, a1_y = 0000, a1_frac_x = 49CD, a1_frac_y = 0000, a2_x = 0033, a2_y = 0001
  a1_step_x = 0000, a1_step_y = 0000, a1_stepf_x = 939A, a1_stepf_y = 0000, a2_step_x = 0000, a2_step_y = 0000
  a1_inc_x = 0000, a1_inc_y = 0000, a1_incf_x = 0000, a1_incf_y = 0000
  a1_win_x = 0100, a1_win_y = 0020, a2_mask_x = 0000, a2_mask_y = 0000
  a2_mask=F a1add=+phr/+0 a2add=+phr/+0
  a1_pixsize = 4, a2_pixsize = 3
   srcd=D6D6D6D6D6D6D6D6  dstd=0000000000000000 patd=0000000000000000 iinc=00000000
  srcz1=0000000000000000 srcz2=0000000000000000 dstz=0000000000000000 zinc=00000000, coll=0
  Phrase mode is ON
*/



// Various pieces of the blitter puzzle are teased out here...



/*
DEF ADDRGEN (
INT24/	address		// byte address
		pixa[0..2]	// bit part of address, un-pipe-lined
		:OUT;
INT16/	a1_x
INT16/	a1_y
INT21/	a1_base
		a1_pitch[0..1]
		a1_pixsize[0..2]
		a1_width[0..5]
		a1_zoffset[0..1]
INT16/	a2_x
INT16/	a2_y
INT21/	a2_base
		a2_pitch[0..1]
		a2_pixsize[0..2]
		a2_width[0..5]
		a2_zoffset[0..1]
		apipe		// load address pipe-line latch
		clk			// co-processor clock
		gena2		// generate A2 as opposed to A1
		zaddr		// generate Z address
		:IN);
*/

void ADDRGEN(uint32_t &address, uint32_t &pixa, bool gena2, bool zaddr,
	uint16_t a1_x, uint16_t a1_y, uint32_t a1_base, uint8_t a1_pitch, uint8_t a1_pixsize, uint8_t a1_width, uint8_t a1_zoffset,
	uint16_t a2_x, uint16_t a2_y, uint32_t a2_base, uint8_t a2_pitch, uint8_t a2_pixsize, uint8_t a2_width, uint8_t a2_zoffset)
{
//	uint16_t x = (gena2 ? a2_x : a1_x) & 0x7FFF;
	uint16_t x = (gena2 ? a2_x : a1_x) & 0xFFFF;	// Actually uses all 16 bits to generate address...!
	uint16_t y = (gena2 ? a2_y : a1_y) & 0x0FFF;
	uint8_t width = (gena2 ? a2_width : a1_width);
	uint8_t pixsize = (gena2 ? a2_pixsize : a1_pixsize);
	uint8_t pitch = (gena2 ? a2_pitch : a1_pitch);
	uint32_t base = (gena2 ? a2_base : a1_base) >> 3;//Only upper 21 bits are passed around the bus? Seems like it...
	uint8_t zoffset = (gena2 ? a2_zoffset : a1_zoffset);

	uint32_t ytm = ((uint32_t)y << 2) + (width & 0x02 ? (uint32_t)y << 1 : 0) + (width & 0x01 ? (uint32_t)y : 0);

	uint32_t ya = (ytm << (width >> 2)) >> 2;

	uint32_t pa = ya + x;

	/*uint32*/ pixa = pa << pixsize;

	uint8_t pt = ((pitch & 0x01) && !(pitch & 0x02) ? 0x01 : 0x00)
		| (!(pitch & 0x01) && (pitch & 0x02) ? 0x02 : 0x00);
//	uint32_t phradr = pixa << pt;
	uint32_t phradr = (pixa >> 6) << pt;
	uint32_t shup = (pitch == 0x03 ? (pixa >> 6) : 0);

	uint8_t za = (zaddr ? zoffset : 0) & 0x03;
//	uint32_t addr = za + (phradr & 0x07) + (shup << 1) + base;
	uint32_t addr = za + phradr + (shup << 1) + base;
	/*uint32*/ address = ((pixa & 0x38) >> 3) | ((addr & 0x1FFFFF) << 3);
#if 0//def VERBOSE_BLITTER_LOGGING
if (logBlit)
{
WriteLog("    [gena2=%s, x=%04X, y=%04X, w=%1X, pxsz=%1X, ptch=%1X, b=%08X, zoff=%1X]\n", (gena2 ? "T" : "F"), x, y, width, pixsize, pitch, base, zoffset);
WriteLog("    [ytm=%X, ya=%X, pa=%X, pixa=%X, pt=%X, phradr=%X, shup=%X, za=%X, addr=%X, address=%X]\n", ytm, ya, pa, pixa, pt, phradr, shup, za, addr, address);
//fflush(stdout);
}
#endif
	pixa &= 0x07;
/*
  Entering INNER state...
    [gena2=T, x=0002, y=0000, w=20, pxsz=4, ptch=0, b=000012BA, zoff=0]
    [ytm=0, ya=0, pa=2, pixa=20, pt=0, phradr=0, shup=0, za=0, addr=12BA, address=95D4]
  Entering SREADX state... [dstart=0 dend=20 pwidth=8 srcshift=20]
    Source extra read address/pix address: 000095D4/0 [0000001C00540038]
  Entering A2_ADD state [a2_x=0002, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
    [gena2=T, x=0004, y=0000, w=20, pxsz=4, ptch=0, b=000012BA, zoff=0]
    [ytm=0, ya=0, pa=4, pixa=40, pt=0, phradr=1, shup=0, za=0, addr=12BB, address=95D8]
  Entering SREAD state... [dstart=0 dend=20 pwidth=8 srcshift=0]
    Source read address/pix address: 000095D8/0 [0054003800009814]
  Entering A2_ADD state [a2_x=0004, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
    [gena2=F, x=0000, y=0000, w=20, pxsz=4, ptch=0, b=00006E52, zoff=0]
    [ytm=0, ya=0, pa=0, pixa=0, pt=0, phradr=0, shup=0, za=0, addr=6E52, address=37290]
  Entering DWRITE state...
     Dest write address/pix address: 00037290/0 [dstart=0 dend=20 pwidth=8 srcshift=0] (icount=026E, inc=4)
  Entering A1_ADD state [a1_x=0000, a1_y=0000, addasel=0, addbsel=0, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
    [gena2=T, x=0008, y=0000, w=20, pxsz=4, ptch=0, b=000012BA, zoff=0]
    [ytm=0, ya=0, pa=8, pixa=80, pt=0, phradr=2, shup=0, za=0, addr=12BC, address=95E0]
*/
/*
Obviously wrong:
  Entering SREAD state...
    [gena2=T, x=0004, y=0000, w=20, pxsz=4, ptch=0, b=000010AC, zoff=0]
    [ytm=0, ya=0, pa=4, pixa=0, pt=0, phradr=40, shup=0, za=0, addr=10AC, address=8560]
    Source read address/pix address: 00008560/0 [8C27981B327E00F0]

2nd pass (still wrong):
  Entering SREAD state...
    [gena2=T, x=0004, y=0000, w=20, pxsz=4, ptch=0, b=000010AC, zoff=0]
    [ytm=0, ya=0, pa=4, pixa=0, pt=0, phradr=40, shup=0, za=0, addr=10EC, address=8760]
    Source read address/pix address: 00008760/0 [00E06DC04581880C]

Correct!:
  Entering SREAD state...
    [gena2=T, x=0004, y=0000, w=20, pxsz=4, ptch=0, b=000010AC, zoff=0]
    [ytm=0, ya=0, pa=4, pixa=0, pt=0, phradr=1, shup=0, za=0, addr=10AD, address=8568]
    Source read address/pix address: 00008568/0 [6267981A327C00F0]

OK, now we're back into incorrect (or is it?):
  Entering SREADX state... [dstart=0 dend=20 pwidth=8 srcshift=20]
    Source extra read address/pix address: 000095D4/0 [0000 001C 0054 0038]
  Entering A2_ADD state [a2_x=0002, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
  Entering SREAD state... [dstart=0 dend=20 pwidth=8 srcshift=0]
    Source read address/pix address: 000095D8/0 [0054 0038 0000 9814]
  Entering A2_ADD state [a2_x=0004, a2_y=0000, addasel=0, addbsel=1, modx=2, addareg=F, adda_xconst=2, adda_yconst=0]...
I think this may be correct...!
*/
}

/*
// source and destination address update conditions

Sraat0		:= AN2 (sraat[0], sreadxi, srcenz\);
Sraat1		:= AN2 (sraat[1], sreadi, srcenz\);
Srca_addi	:= OR4 (srca_addi, szreadxi, szreadi, sraat[0..1]);
Srca_add	:= FD1Q (srca_add, srca_addi, clk);

Dstaat		:= AN2 (dstaat, dwritei, dstwrz\);
Dsta_addi	:= OR2 (dsta_addi, dzwritei, dstaat);
// Dsta_add	:= FD1Q (dsta_add, dsta_addi, clk);

// source and destination address generate conditions

Gensrc		:= OR4 (gensrc, sreadxi, szreadxi, sreadi, szreadi);
Gendst		:= OR4 (gendst, dreadi, dzreadi, dwritei, dzwritei);
Dsta2\		:= INV1 (dsta2\, dsta2);
Gena2t0		:= NAN2 (gena2t[0], gensrc, dsta2\);
Gena2t1		:= NAN2 (gena2t[1], gendst, dsta2);
Gena2i		:= NAN2 (gena2i, gena2t[0..1]);
Gena2		:= FD1QU (gena2, gena2i, clk);

Zaddr		:= OR4 (zaddr, szreadx, szread, dzread, dzwrite);
*/

/*void foo(void)
{
	// Basically, the above translates to:
	bool srca_addi = (sreadxi && !srcenz) || (sreadi && !srcenz) || szreadxi || szreadi;

	bool dsta_addi = (dwritei && !dstwrz) || dzwritei;

	bool gensrc = sreadxi || szreadxi || sreadi || szreadi;
	bool gendst = dreadi || szreadi || dwritei || dzwritei;
	bool gena2i = (gensrc && !dsta2) || (gendst && dsta2);

	bool zaddr = szreadx || szread || dzread || dzwrite;
}*/

/*
// source data reads

Srcdpset\	:= NAN2 (srcdpset\, readreq, sread);
Srcdpt1 	:= NAN2 (srcdpt[1], srcdpend, srcdack\);
Srcdpt2		:= NAN2 (srcdpt[2], srcdpset\, srcdpt[1]);
Srcdpend	:= FD2Q (srcdpend, srcdpt[2], clk, reset\);

Srcdxpset\	:= NAN2 (srcdxpset\, readreq, sreadx);
Srcdxpt1 	:= NAN2 (srcdxpt[1], srcdxpend, srcdxack\);
Srcdxpt2	:= NAN2 (srcdxpt[2], srcdxpset\, srcdxpt[1]);
Srcdxpend	:= FD2Q (srcdxpend, srcdxpt[2], clk, reset\);

Sdpend		:= OR2 (sdpend, srcdxpend, srcdpend);
Srcdreadt	:= AN2 (srcdreadt, sdpend, read_ack);

//2/9/92 - enhancement?
//Load srcdread on the next tick as well to modify it in srcshade

Srcdreadd	:= FD1Q (srcdreadd, srcdreadt, clk);
Srcdread	:= AOR1 (srcdread, srcshade, srcdreadd, srcdreadt);

// source zed reads

Srczpset\	:= NAN2 (srczpset\, readreq, szread);
Srczpt1 	:= NAN2 (srczpt[1], srczpend, srczack\);
Srczpt2		:= NAN2 (srczpt[2], srczpset\, srczpt[1]);
Srczpend	:= FD2Q (srczpend, srczpt[2], clk, reset\);

Srczxpset\	:= NAN2 (srczxpset\, readreq, szreadx);
Srczxpt1 	:= NAN2 (srczxpt[1], srczxpend, srczxack\);
Srczxpt2	:= NAN2 (srczxpt[2], srczxpset\, srczxpt[1]);
Srczxpend	:= FD2Q (srczxpend, srczxpt[2], clk, reset\);

Szpend		:= OR2 (szpend, srczpend, srczxpend);
Srczread	:= AN2 (srczread, szpend, read_ack);

// destination data reads

Dstdpset\	:= NAN2 (dstdpset\, readreq, dread);
Dstdpt0 	:= NAN2 (dstdpt[0], dstdpend, dstdack\);
Dstdpt1		:= NAN2 (dstdpt[1], dstdpset\, dstdpt[0]);
Dstdpend	:= FD2Q (dstdpend, dstdpt[1], clk, reset\);
Dstdread	:= AN2 (dstdread, dstdpend, read_ack);

// destination zed reads

Dstzpset\	:= NAN2 (dstzpset\, readreq, dzread);
Dstzpt0 	:= NAN2 (dstzpt[0], dstzpend, dstzack\);
Dstzpt1		:= NAN2 (dstzpt[1], dstzpset\, dstzpt[0]);
Dstzpend	:= FD2Q (dstzpend, dstzpt[1], clk, reset\);
Dstzread	:= AN2 (dstzread, dstzpend, read_ack);
*/

/*void foo2(void)
{
	// Basically, the above translates to:
	bool srcdpend = (readreq && sread) || (srcdpend && !srcdack);
	bool srcdxpend = (readreq && sreadx) || (srcdxpend && !srcdxack);
	bool sdpend = srcxpend || srcdpend;
	bool srcdread = ((sdpend && read_ack) && srcshade) || (sdpend && read_ack);//the latter term is lookahead

}*/


////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////
// Here's an important bit: The source data adder logic. Need to track down the inputs!!! //
////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////

/*
DEF ADDARRAY (
INT16/  addq[0..3]
        :OUT;
        clk
        daddasel[0..2]  // data adder input A selection
        daddbsel[0..3]
        daddmode[0..2]
INT32/  dstd[0..1]
INT32/  iinc
        initcin[0..3]   // carry into the adders from the initializers
        initinc[0..63]  // the initialisation increment
        initpix[0..15]  // Data initialiser pixel value
INT32/  istep
INT32/  patd[0..1]
INT32/  srcdlo
INT32/  srcdhi
INT32/  srcz1[0..1]
INT32/  srcz2[0..1]
        reset\
INT32/  zinc
INT32/  zstep
        :IN);
*/
void ADDARRAY(uint16_t * addq, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode,
	uint64_t dstd, uint32_t iinc, uint8_t initcin[], uint64_t initinc, uint16_t initpix,
	uint32_t istep, uint64_t patd, uint64_t srcd, uint64_t srcz1, uint64_t srcz2,
	uint32_t zinc, uint32_t zstep)
{
	uint32_t initpix2 = ((uint32_t)initpix << 16) | initpix;
	uint32_t addalo[8], addahi[8];
	addalo[0] = dstd & 0xFFFFFFFF;
	addalo[1] = initpix2;
	addalo[2] = 0;
	addalo[3] = 0;
	addalo[4] = srcd & 0xFFFFFFFF;
	addalo[5] = patd & 0xFFFFFFFF;
	addalo[6] = srcz1 & 0xFFFFFFFF;
	addalo[7] = srcz2 & 0xFFFFFFFF;
	addahi[0] = dstd >> 32;
	addahi[1] = initpix2;
	addahi[2] = 0;
	addahi[3] = 0;
	addahi[4] = srcd >> 32;
	addahi[5] = patd >> 32;
	addahi[6] = srcz1 >> 32;
	addahi[7] = srcz2 >> 32;
	uint16_t adda[4];
	adda[0] = addalo[daddasel] & 0xFFFF;
	adda[1] = addalo[daddasel] >> 16;
	adda[2] = addahi[daddasel] & 0xFFFF;
	adda[3] = addahi[daddasel] >> 16;

	uint16_t wordmux[8];
	wordmux[0] = iinc & 0xFFFF;
	wordmux[1] = iinc >> 16;
	wordmux[2] = zinc & 0xFFFF;
	wordmux[3] = zinc >> 16;;
	wordmux[4] = istep & 0xFFFF;
	wordmux[5] = istep >> 16;;
	wordmux[6] = zstep & 0xFFFF;
	wordmux[7] = zstep >> 16;;
	uint16_t word = wordmux[((daddbsel & 0x08) >> 1) | (daddbsel & 0x03)];
	uint16_t addb[4];
	bool dbsel2 = daddbsel & 0x04;
	bool iincsel = (daddbsel & 0x01) && !(daddbsel & 0x04);

	if (!dbsel2 && !iincsel)
		addb[0] = srcd & 0xFFFF,
		addb[1] = (srcd >> 16) & 0xFFFF,
		addb[2] = (srcd >> 32) & 0xFFFF,
		addb[3] = (srcd >> 48) & 0xFFFF;
	else if (dbsel2 && !iincsel)
		addb[0] = addb[1] = addb[2] = addb[3] = word;
	else if (!dbsel2 && iincsel)
		addb[0] = initinc & 0xFFFF,
		addb[1] = (initinc >> 16) & 0xFFFF,
		addb[2] = (initinc >> 32) & 0xFFFF,
		addb[3] = (initinc >> 48) & 0xFFFF;
	else
		addb[0] = addb[1] = addb[2] = addb[3] = 0;

	uint8_t cinsel = (daddmode >= 1 && daddmode <= 4 ? 1 : 0);

static uint8_t co[4];//These are preserved between calls...
	uint8_t cin[4];

	for(int i=0; i<4; i++)
		cin[i] = initcin[i] | (co[i] & cinsel);

	bool eightbit = daddmode & 0x02;
	bool sat = daddmode & 0x03;
	bool hicinh = ((daddmode & 0x03) == 0x03);

//Note that the carry out is saved between calls to this function...
	for(int i=0; i<4; i++)
		ADD16SAT(addq[i], co[i], adda[i], addb[i], cin[i], sat, eightbit, hicinh);
}


/*
DEF ADD16SAT (
INT16/  r               // result
        co              // carry out
        :IO;
INT16/  a
INT16/  b
        cin
        sat
        eightbit
        hicinh
        :IN);
*/
void ADD16SAT(uint16_t &r, uint8_t &co, uint16_t a, uint16_t b, uint8_t cin, bool sat, bool eightbit, bool hicinh)
{
/*if (logBlit)
{
	printf("--> [sat=%s 8b=%s hicinh=%s] %04X + %04X (+ %u) = ", (sat ? "T" : "F"), (eightbit ? "T" : "F"), (hicinh ? "T" : "F"), a, b, cin);
	fflush(stdout);
}*/
	uint8_t carry[4];
	uint32_t qt = (a & 0xFF) + (b & 0xFF) + cin;
	carry[0] = (qt & 0x0100 ? 1 : 0);
	uint16_t q = qt & 0x00FF;
	carry[1] = (carry[0] && !eightbit ? carry[0] : 0);
	qt = (a & 0x0F00) + (b & 0x0F00) + (carry[1] << 8);
	carry[2] = (qt & 0x1000 ? 1 : 0);
	q |= qt & 0x0F00;
	carry[3] = (carry[2] && !hicinh ? carry[2] : 0);
	qt = (a & 0xF000) + (b & 0xF000) + (carry[3] << 12);
	co = (qt & 0x10000 ? 1 : 0);
	q |= qt & 0xF000;

	uint8_t btop = (eightbit ? (b & 0x0080) >> 7 : (b & 0x8000) >> 15);
	uint8_t ctop = (eightbit ? carry[0] : co);

	bool saturate = sat && (btop ^ ctop);
	bool hisaturate = saturate && !eightbit;
/*if (logBlit)
{
	printf("bt=%u ct=%u s=%u hs=%u] ", btop, ctop, saturate, hisaturate);
	fflush(stdout);
}*/

	r = (saturate ? (ctop ? 0x00FF : 0x0000) : q & 0x00FF);
	r |= (hisaturate ? (ctop ? 0xFF00 : 0x0000) : q & 0xFF00);
/*if (logBlit)
{
	printf("%04X (co=%u)\n", r, co);
	fflush(stdout);
}*/
}


/**  ADDAMUX - Address adder input A selection  *******************

This module generates the data loaded into the address adder input A.  This is
the update value, and can be one of four registers :  A1 step, A2 step, A1
increment and A1 fraction.  It can complement these values to perform
subtraction, and it can generate constants to increment / decrement the window
pointers.

addasel[0..2] select the register to add

000	A1 step integer part
001	A1 step fraction part
010	A1 increment integer part
011	A1 increment fraction part
100	A2 step

adda_xconst[0..2] generate a power of 2 in the range 1-64 or all zeroes when
they are all 1.

addareg selects register value to be added as opposed to constant
value.

suba_x, suba_y complement the X and Y values

*/

/*
DEF ADDAMUX (
INT16/	adda_x
INT16/	adda_y
	:OUT;
	addasel[0..2]
INT16/	a1_step_x
INT16/	a1_step_y
INT16/	a1_stepf_x
INT16/	a1_stepf_y
INT16/	a2_step_x
INT16/	a2_step_y
INT16/	a1_inc_x
INT16/	a1_inc_y
INT16/	a1_incf_x
INT16/	a1_incf_y
	adda_xconst[0..2]
	adda_yconst
	addareg
	suba_x
	suba_y :IN);
*/
void ADDAMUX(int16_t &adda_x, int16_t &adda_y, uint8_t addasel, int16_t a1_step_x, int16_t a1_step_y,
	int16_t a1_stepf_x, int16_t a1_stepf_y, int16_t a2_step_x, int16_t a2_step_y,
	int16_t a1_inc_x, int16_t a1_inc_y, int16_t a1_incf_x, int16_t a1_incf_y, uint8_t adda_xconst,
	bool adda_yconst, bool addareg, bool suba_x, bool suba_y)
{

/*INT16/	addac_x, addac_y, addar_x, addar_y, addart_x, addart_y,
INT16/	addas_x, addas_y, suba_x16, suba_y16
:LOCAL;
BEGIN

Zero		:= TIE0 (zero);*/

/* Multiplex the register terms */

/*Addaselb[0-2]	:= BUF8 (addaselb[0-2], addasel[0-2]);
Addart_x	:= MX4 (addart_x, a1_step_x, a1_stepf_x, a1_inc_x, a1_incf_x, addaselb[0..1]);
Addar_x		:= MX2 (addar_x, addart_x, a2_step_x, addaselb[2]);
Addart_y	:= MX4 (addart_y, a1_step_y, a1_stepf_y, a1_inc_y, a1_incf_y, addaselb[0..1]);
Addar_y		:= MX2 (addar_y, addart_y, a2_step_y, addaselb[2]);*/

////////////////////////////////////// C++ CODE //////////////////////////////////////
	int16_t xterm[4], yterm[4];
	xterm[0] = a1_step_x, xterm[1] = a1_stepf_x, xterm[2] = a1_inc_x, xterm[3] = a1_incf_x;
	yterm[0] = a1_step_y, yterm[1] = a1_stepf_y, yterm[2] = a1_inc_y, yterm[3] = a1_incf_y;
	int16_t addar_x = (addasel & 0x04 ? a2_step_x : xterm[addasel & 0x03]);
	int16_t addar_y = (addasel & 0x04 ? a2_step_y : yterm[addasel & 0x03]);
//////////////////////////////////////////////////////////////////////////////////////

/* Generate a constant value - this is a power of 2 in the range
0-64, or zero.  The control bits are adda_xconst[0..2], when they
are all 1  the result is 0.
Constants for Y can only be 0 or 1 */

/*Addac_xlo	:= D38H (addac_x[0..6], unused[0], adda_xconst[0..2]);
Unused[0]	:= DUMMY (unused[0]);

Addac_x		:= JOIN (addac_x, addac_x[0..6], zero, zero, zero, zero, zero, zero, zero, zero, zero);
Addac_y		:= JOIN (addac_y, adda_yconst, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero,
			zero, zero, zero, zero, zero);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	int16_t addac_x = (adda_xconst == 0x07 ? 0 : 1 << adda_xconst);
	int16_t addac_y = (adda_yconst ? 0x01 : 0);
//////////////////////////////////////////////////////////////////////////////////////

/* Select between constant value and register value */

/*Addas_x		:= MX2 (addas_x, addac_x, addar_x, addareg);
Addas_y		:= MX2 (addas_y, addac_y, addar_y, addareg);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	int16_t addas_x = (addareg ? addar_x : addac_x);
	int16_t addas_y = (addareg ? addar_y : addac_y);
//////////////////////////////////////////////////////////////////////////////////////

/* Complement these values (complement flag gives adder carry in)*/

/*Suba_x16	:= JOIN (suba_x16, suba_x, suba_x, suba_x, suba_x, suba_x, suba_x, suba_x, suba_x, suba_x,
			suba_x, suba_x, suba_x, suba_x, suba_x, suba_x, suba_x);
Suba_y16	:= JOIN (suba_y16, suba_y, suba_y, suba_y, suba_y, suba_y, suba_y, suba_y, suba_y, suba_y,
			suba_y, suba_y, suba_y, suba_y, suba_y, suba_y, suba_y);
Adda_x		:= EO (adda_x, suba_x16, addas_x);
Adda_y		:= EO (adda_y, suba_y16, addas_y);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	adda_x = addas_x ^ (suba_x ? 0xFFFF : 0x0000);
	adda_y = addas_y ^ (suba_y ? 0xFFFF : 0x0000);
//////////////////////////////////////////////////////////////////////////////////////

//END;
}


/**  ADDBMUX - Address adder input B selection  *******************

This module selects the register to be updated by the address
adder.  This can be one of three registers, the A1 and A2
pointers, or the A1 fractional part. It can also be zero, so that the step
registers load directly into the pointers.
*/

/*DEF ADDBMUX (
INT16/	addb_x
INT16/	addb_y
	:OUT;
	addbsel[0..1]
INT16/	a1_x
INT16/	a1_y
INT16/	a2_x
INT16/	a2_y
INT16/	a1_frac_x
INT16/	a1_frac_y
	:IN);
INT16/	zero16 :LOCAL;
BEGIN*/
void ADDBMUX(int16_t &addb_x, int16_t &addb_y, uint8_t addbsel, int16_t a1_x, int16_t a1_y,
	int16_t a2_x, int16_t a2_y, int16_t a1_frac_x, int16_t a1_frac_y)
{

/*Zero		:= TIE0 (zero);
Zero16		:= JOIN (zero16, zero, zero, zero, zero, zero, zero, zero,
			zero, zero, zero, zero, zero, zero, zero, zero, zero);
Addbselb[0-1]	:= BUF8 (addbselb[0-1], addbsel[0-1]);
Addb_x		:= MX4 (addb_x, a1_x, a2_x, a1_frac_x, zero16, addbselb[0..1]);
Addb_y		:= MX4 (addb_y, a1_y, a2_y, a1_frac_y, zero16, addbselb[0..1]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	int16_t xterm[4], yterm[4];
	xterm[0] = a1_x, xterm[1] = a2_x, xterm[2] = a1_frac_x, xterm[3] = 0;
	yterm[0] = a1_y, yterm[1] = a2_y, yterm[2] = a1_frac_y, yterm[3] = 0;
	addb_x = xterm[addbsel & 0x03];
	addb_y = yterm[addbsel & 0x03];
//////////////////////////////////////////////////////////////////////////////////////

//END;
}


/**  DATAMUX - Address local data bus selection  ******************

Select between the adder output and the input data bus
*/

/*DEF DATAMUX (
INT16/	data_x
INT16/	data_y
	:OUT;
INT32/	gpu_din
INT16/	addq_x
INT16/	addq_y
	addqsel
	:IN);

INT16/	gpu_lo, gpu_hi
:LOCAL;
BEGIN*/
void DATAMUX(int16_t &data_x, int16_t &data_y, uint32_t gpu_din, int16_t addq_x, int16_t addq_y, bool addqsel)
{
/*Gpu_lo		:= JOIN (gpu_lo, gpu_din{0..15});
Gpu_hi		:= JOIN (gpu_hi, gpu_din{16..31});

Addqselb	:= BUF8 (addqselb, addqsel);
Data_x		:= MX2 (data_x, gpu_lo, addq_x, addqselb);
Data_y		:= MX2 (data_y, gpu_hi, addq_y, addqselb);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	data_x = (addqsel ? addq_x : (int16_t)(gpu_din & 0xFFFF));
	data_y = (addqsel ? addq_y : (int16_t)(gpu_din >> 16));
//////////////////////////////////////////////////////////////////////////////////////

//END;
}


/******************************************************************
addradd
29/11/90

Blitter Address Adder
---------------------
The blitter address adder is a pair of sixteen bit adders, one
each for X and Y.  The multiplexing of the input terms is
performed elsewhere, but this adder can also perform modulo
arithmetic to align X-addresses onto phrase boundaries.

modx[0..2] take values
000	no mask
001	mask bit 0
010	mask bits 1-0
..
110  	mask bits 5-0

******************************************************************/

/*IMPORT duplo, tosh;

DEF ADDRADD (
INT16/	addq_x
INT16/	addq_y
		:OUT;
		a1fracldi		// propagate address adder carry
INT16/	adda_x
INT16/	adda_y
INT16/	addb_x
INT16/	addb_y
		clk[0]			// co-processor clock
		modx[0..2]
		suba_x
		suba_y
		:IN);

BEGIN

Zero		:= TIE0 (zero);*/
void ADDRADD(int16_t &addq_x, int16_t &addq_y, bool a1fracldi,
	uint16_t adda_x, uint16_t adda_y, uint16_t addb_x, uint16_t addb_y, uint8_t modx, bool suba_x, bool suba_y)
{

/* Perform the addition */

/*Adder_x		:= ADD16 (addqt_x[0..15], co_x, adda_x{0..15}, addb_x{0..15}, ci_x);
Adder_y		:= ADD16 (addq_y[0..15], co_y, adda_y{0..15}, addb_y{0..15}, ci_y);*/

/* latch carry and propagate if required */

/*Cxt0		:= AN2 (cxt[0], co_x, a1fracldi);
Cxt1		:= FD1Q (cxt[1], cxt[0], clk[0]);
Ci_x		:= EO (ci_x, cxt[1], suba_x);

yt0			:= AN2 (cyt[0], co_y, a1fracldi);
Cyt1		:= FD1Q (cyt[1], cyt[0], clk[0]);
Ci_y		:= EO (ci_y, cyt[1], suba_y);*/

////////////////////////////////////// C++ CODE //////////////////////////////////////
//I'm sure the following will generate a bunch of warnings, but will have to do for now.
	static uint16_t co_x = 0, co_y = 0;	// Carry out has to propogate between function calls...
	uint16_t ci_x = co_x ^ (suba_x ? 1 : 0);
	uint16_t ci_y = co_y ^ (suba_y ? 1 : 0);
	uint32_t addqt_x = adda_x + addb_x + ci_x;
	uint32_t addqt_y = adda_y + addb_y + ci_y;
	co_x = ((addqt_x & 0x10000) && a1fracldi ? 1 : 0);
	co_y = ((addqt_y & 0x10000) && a1fracldi ? 1 : 0);
//////////////////////////////////////////////////////////////////////////////////////

/* Mask low bits of X to 0 if required */

/*Masksel		:= D38H (unused[0], masksel[0..4], maskbit[5], unused[1], modx[0..2]);

Maskbit[0-4]	:= OR2 (maskbit[0-4], masksel[0-4], maskbit[1-5]);

Mask[0-5]	:= MX2 (addq_x[0-5], addqt_x[0-5], zero, maskbit[0-5]);

Addq_x		:= JOIN (addq_x, addq_x[0..5], addqt_x[6..15]);
Addq_y		:= JOIN (addq_y, addq_y[0..15]);*/

////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint16_t mask[8] = { 0xFFFF, 0xFFFE, 0xFFFC, 0xFFF8, 0xFFF0, 0xFFE0, 0xFFC0, 0x0000 };
	addq_x = addqt_x & mask[modx];
	addq_y = addqt_y & 0xFFFF;
//////////////////////////////////////////////////////////////////////////////////////

//Unused[0-1]	:= DUMMY (unused[0-1]);

//END;
}


/*
DEF DATA (
		wdata[0..63]	// co-processor write data bus
		:BUS;
		dcomp[0..7]		// data byte equal flags
		srcd[0..7]		// bits to use for bit to byte expansion
		zcomp[0..3]		// output from Z comparators
		:OUT;
		a1_x[0..1]		// low two bits of A1 X pointer
		big_pix			// pixel organisation is big-endian
		blitter_active	// blitter is active
		clk				// co-processor clock
		cmpdst			// compare dest rather than source
		colorld			// load the pattern color fields
		daddasel[0..2]	// data adder input A selection
		daddbsel[0..3]	// data adder input B selection
		daddmode[0..2]	// data adder mode
		daddq_sel		// select adder output vs. GPU data
		data[0..63]		// co-processor read data bus
		data_ena		// enable write data
		data_sel[0..1]	// select data to write
		dbinh\[0..7]	// byte oriented changed data inhibits
		dend[0..5]		// end of changed write data zone
		dpipe[0..1]		// load computed data pipe-line latch
		dstart[0..5]	// start of changed write data zone
		dstdld[0..1]	// dest data load (two halves)
		dstzld[0..1]	// dest zed load (two halves)
		ext_int			// enable extended precision intensity calculations
INT32/	gpu_din			// GPU data bus
		iincld			// I increment load
		iincldx			// alternate I increment load
		init_if			// initialise I fraction phase
		init_ii			// initialise I integer phase
		init_zf			// initialise Z fraction phase
		intld[0..3]		// computed intensities load
		istepadd		// intensity step integer add
		istepfadd		// intensity step fraction add
		istepld			// I step load
		istepdld		// I step delta load
		lfu_func[0..3]	// LFU function code
		patdadd			// pattern data gouraud add
		patdld[0..1]	// pattern data load (two halves)
		pdsel[0..1]		// select pattern data type
		phrase_mode		// phrase write mode
		reload			// transfer contents of double buffers
		reset\			// system reset
		srcd1ld[0..1]	// source register 1 load (two halves)
		srcdread		// source data read load enable
		srczread		// source zed read load enable
		srcshift[0..5]	// source alignment shift
		srcz1ld[0..1]	// source zed 1 load (two halves)
		srcz2add		// zed fraction gouraud add
		srcz2ld[0..1]	// source zed 2 load (two halves)
		textrgb			// texture mapping in RGB mode
		txtd[0..63]		// data from the texture unit
		zedld[0..3]		// computed zeds load
		zincld			// Z increment load
		zmode[0..2]		// Z comparator mode
		zpipe[0..1]		// load computed zed pipe-line latch
		zstepadd		// zed step integer add
		zstepfadd		// zed step fraction add
		zstepld			// Z step load
		zstepdld		// Z step delta load
		:IN);
*/

void DATA(uint64_t &wdata, uint8_t &dcomp, uint8_t &zcomp, bool &nowrite,
	bool big_pix, bool cmpdst, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode, bool daddq_sel, uint8_t data_sel,
	uint8_t dbinh, uint8_t dend, uint8_t dstart, uint64_t dstd, uint32_t iinc, uint8_t lfu_func, uint64_t &patd, bool patdadd,
	bool phrase_mode, uint64_t srcd, bool srcdread, bool srczread, bool srcz2add, uint8_t zmode,
	bool bcompen, bool bkgwren, bool dcompen, uint8_t icount, uint8_t pixsize,
	uint64_t &srcz, uint64_t dstz, uint32_t zinc)
{
/*
  Stuff we absolutely *need* to have passed in/out:
IN:
  patdadd, dstd, srcd, patd, daddasel, daddbsel, daddmode, iinc, srcz1, srcz2, big_pix, phrase_mode, cmpdst
OUT:
  changed patd (wdata I guess...) (Nope. We pass it back directly now...)
*/

// Source data registers

/*Data_src	:= DATA_SRC (srcdlo, srcdhi, srcz[0..1], srczo[0..1], srczp[0..1], srcz1[0..1], srcz2[0..1], big_pix,
			clk, gpu_din, intld[0..3], local_data0, local_data1, srcd1ld[0..1], srcdread, srczread, srcshift[0..5],
			srcz1ld[0..1], srcz2add, srcz2ld[0..1], zedld[0..3], zpipe[0..1]);
Srcd[0-7]	:= JOIN (srcd[0-7], srcdlo{0-7});
Srcd[8-31]	:= JOIN (srcd[8-31], srcdlo{8-31});
Srcd[32-63]	:= JOIN (srcd[32-63], srcdhi{0-31});*/

// Destination data registers

/*Data_dst	:= DATA_DST (dstd[0..63], dstz[0..1], clk, dstdld[0..1], dstzld[0..1], load_data[0..1]);
Dstdlo		:= JOIN (dstdlo, dstd[0..31]);
Dstdhi		:= JOIN (dstdhi, dstd[32..63]);*/

// Pattern and Color data registers

// Looks like this is simply another register file for the pattern data registers. No adding or anything funky
// going on. Note that patd & patdv will output the same info.
// Patdldl/h (patdld[0..1]) can select the local_data bus to overwrite the current pattern data...
// Actually, it can be either patdld OR patdadd...!
/*Data_pat	:= DATA_PAT (colord[0..15], int0dp[8..10], int1dp[8..10], int2dp[8..10], int3dp[8..10], mixsel[0..2],
			patd[0..63], patdv[0..1], clk, colorld, dpipe[0], ext_int, gpu_din, intld[0..3], local_data0, local_data1,
			patdadd, patdld[0..1], reload, reset\);
Patdlo		:= JOIN (patdlo, patd[0..31]);
Patdhi		:= JOIN (patdhi, patd[32..63]);*/

// Multiplying data Mixer (NOT IN JAGUAR I)

/*Datamix		:= DATAMIX (patdo[0..1], clk, colord[0..15], dpipe[1], dstd[0..63], int0dp[8..10], int1dp[8..10],
			int2dp[8..10], int3dp[8..10], mixsel[0..2], patd[0..63], pdsel[0..1], srcd[0..63], textrgb, txtd[0..63]);*/

// Logic function unit

/*Lfu		:= LFU (lfu[0..1], srcdlo, srcdhi, dstdlo, dstdhi, lfu_func[0..3]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint64_t funcmask[2] = { 0, 0xFFFFFFFFFFFFFFFFLL };
	uint64_t func0 = funcmask[lfu_func & 0x01];
	uint64_t func1 = funcmask[(lfu_func >> 1) & 0x01];
	uint64_t func2 = funcmask[(lfu_func >> 2) & 0x01];
	uint64_t func3 = funcmask[(lfu_func >> 3) & 0x01];
	uint64_t lfu = (~srcd & ~dstd & func0) | (~srcd & dstd & func1) | (srcd & ~dstd & func2) | (srcd & dstd & func3);
//////////////////////////////////////////////////////////////////////////////////////

// Increment and Step Registers

// Does it do anything without the step add lines? Check it!
// No. This is pretty much just a register file without the Jaguar II lines...
/*Inc_step	:= INC_STEP (iinc, istep[0..31], zinc, zstep[0..31], clk, ext_int, gpu_din, iincld, iincldx, istepadd,
			istepfadd, istepld, istepdld, reload, reset\, zincld, zstepadd, zstepfadd, zstepld, zstepdld);
Istep		:= JOIN (istep, istep[0..31]);
Zstep		:= JOIN (zstep, zstep[0..31]);*/

// Pixel data comparator

/*Datacomp	:= DATACOMP (dcomp[0..7], cmpdst, dstdlo, dstdhi, patdlo, patdhi, srcdlo, srcdhi);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	dcomp = 0;
	uint64_t cmpd = patd ^ (cmpdst ? dstd : srcd);

	if ((cmpd & 0x00000000000000FFLL) == 0)
		dcomp |= 0x01;
	if ((cmpd & 0x000000000000FF00LL) == 0)
		dcomp |= 0x02;
	if ((cmpd & 0x0000000000FF0000LL) == 0)
		dcomp |= 0x04;
	if ((cmpd & 0x00000000FF000000LL) == 0)
		dcomp |= 0x08;
	if ((cmpd & 0x000000FF00000000LL) == 0)
		dcomp |= 0x10;
	if ((cmpd & 0x0000FF0000000000LL) == 0)
		dcomp |= 0x20;
	if ((cmpd & 0x00FF000000000000LL) == 0)
		dcomp |= 0x40;
	if ((cmpd & 0xFF00000000000000LL) == 0)
		dcomp |= 0x80;
//////////////////////////////////////////////////////////////////////////////////////

// Zed comparator for Z-buffer operations

/*Zedcomp		:= ZEDCOMP (zcomp[0..3], srczp[0..1], dstz[0..1], zmode[0..2]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
//srczp is srcz pipelined, also it goes through a source shift as well...
/*The shift is basically like so (each piece is 16 bits long):

	0         1         2         3         4          5         6
	srcz1lolo srcz1lohi srcz1hilo srcz1hihi srcrz2lolo srcz2lohi srcz2hilo

with srcshift bits 4 & 5 selecting the start position
*/
//So... basically what we have here is:
	zcomp = 0;

	if ((((srcz & 0x000000000000FFFFLL) < (dstz & 0x000000000000FFFFLL)) && (zmode & 0x01))
		|| (((srcz & 0x000000000000FFFFLL) == (dstz & 0x000000000000FFFFLL)) && (zmode & 0x02))
		|| (((srcz & 0x000000000000FFFFLL) > (dstz & 0x000000000000FFFFLL)) && (zmode & 0x04)))
		zcomp |= 0x01;

	if ((((srcz & 0x00000000FFFF0000LL) < (dstz & 0x00000000FFFF0000LL)) && (zmode & 0x01))
		|| (((srcz & 0x00000000FFFF0000LL) == (dstz & 0x00000000FFFF0000LL)) && (zmode & 0x02))
		|| (((srcz & 0x00000000FFFF0000LL) > (dstz & 0x00000000FFFF0000LL)) && (zmode & 0x04)))
		zcomp |= 0x02;

	if ((((srcz & 0x0000FFFF00000000LL) < (dstz & 0x0000FFFF00000000LL)) && (zmode & 0x01))
		|| (((srcz & 0x0000FFFF00000000LL) == (dstz & 0x0000FFFF00000000LL)) && (zmode & 0x02))
		|| (((srcz & 0x0000FFFF00000000LL) > (dstz & 0x0000FFFF00000000LL)) && (zmode & 0x04)))
		zcomp |= 0x04;

	if ((((srcz & 0xFFFF000000000000LL) < (dstz & 0xFFFF000000000000LL)) && (zmode & 0x01))
		|| (((srcz & 0xFFFF000000000000LL) == (dstz & 0xFFFF000000000000LL)) && (zmode & 0x02))
		|| (((srcz & 0xFFFF000000000000LL) > (dstz & 0xFFFF000000000000LL)) && (zmode & 0x04)))
		zcomp |= 0x08;

//TEMP, TO TEST IF ZCOMP IS THE CULPRIT...
//Nope, this is NOT the problem...
//zcomp=0;
// We'll do the comparison/bit/byte inhibits here, since that's they way it happens
// in the real thing (dcomp goes out to COMP_CTRL and back into DATA through dbinh)...
#if 1
	uint8_t dbinht;
//	bool nowrite;
	COMP_CTRL(dbinht, nowrite,
		bcompen, true/*big_pix*/, bkgwren, dcomp, dcompen, icount, pixsize, phrase_mode, srcd & 0xFF, zcomp);
	dbinh = dbinht;
//	dbinh = 0x00;
#endif

#if 1
#ifdef VERBOSE_BLITTER_LOGGING
if (logBlit)
	WriteLog("\n[dcomp=%02X zcomp=%02X dbinh=%02X]\n", dcomp, zcomp, dbinh);
#endif
#endif
//////////////////////////////////////////////////////////////////////////////////////

// 22 Mar 94
// The data initializer - allows all four initial values to be computed from one (NOT IN JAGUAR I)

/*Datinit		:= DATINIT (initcin[0..3], initinc[0..63], initpix[0..15], a1_x[0..1], big_pix, clk, iinc, init_if, init_ii,
			init_zf, istep[0..31], zinc, zstep[0..31]);*/

// Adder array for Z and intensity increments

/*Addarray	:= ADDARRAY (addq[0..3], clk, daddasel[0..2], daddbsel[0..3], daddmode[0..2], dstdlo, dstdhi, iinc,
			initcin[0..3], initinc[0..63], initpix[0..15], istep, patdv[0..1], srcdlo, srcdhi, srcz1[0..1],
			srcz2[0..1], reset\, zinc, zstep);*/
/*void ADDARRAY(uint16_t * addq, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode,
	uint64_t dstd, uint32_t iinc, uint8_t initcin[], uint64_t initinc, uint16_t initpix,
	uint32_t istep, uint64_t patd, uint64_t srcd, uint64_t srcz1, uint64_t srcz2,
	uint32_t zinc, uint32_t zstep)*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint16_t addq[4];
	uint8_t initcin[4] = { 0, 0, 0, 0 };
	ADDARRAY(addq, daddasel, daddbsel, daddmode, dstd, iinc, initcin, 0, 0, 0, patd, srcd, 0, 0, 0, 0);

	//This is normally done asynchronously above (thru local_data) when in patdadd mode...
//And now it's passed back to the caller to be persistent between calls...!
//But it's causing some serious fuck-ups in T2K now... !!! FIX !!! [DONE--???]
//Weird! It doesn't anymore...!
	if (patdadd)
		patd = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
//////////////////////////////////////////////////////////////////////////////////////

// Local data bus multiplexer

/*Local_mux	:= LOCAL_MUX (local_data[0..1], load_data[0..1],
	addq[0..3], gpu_din, data[0..63], blitter_active, daddq_sel);
Local_data0	:= JOIN (local_data0, local_data[0]);
Local_data1	:= JOIN (local_data1, local_data[1]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////

// Data output multiplexer and tri-state drive

/*Data_mux	:= DATA_MUX (wdata[0..63], addq[0..3], big_pix, dstdlo, dstdhi, dstz[0..1], data_sel[0..1], data_ena,
			dstart[0..5], dend[0..5], dbinh\[0..7], lfu[0..1], patdo[0..1], phrase_mode, srczo[0..1]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
// NOTE: patdo comes from DATAMIX and can be considered the same as patd for Jaguar I

//////////////////////////////////////////////////////////////////////////////////////
//}

/*DEF DATA_MUX (
		wdata[0..63]	// co-processor rwrite data bus
		:BUS;
INT16/	addq[0..3]
		big_pix			// Pixel organisation is big-endian
INT32/	dstdlo
INT32/	dstdhi
INT32/	dstzlo
INT32/	dstzhi
		data_sel[0..1]	// source of write data
		data_ena		// enable write data onto read/write bus
		dstart[0..5]	// start of changed write data
		dend[0..5]		// end of changed write data
		dbinh\[0..7]	// byte oriented changed data inhibits
INT32/	lfu[0..1]
INT32/	patd[0..1]
		phrase_mode		// phrase write mode
INT32/	srczlo
INT32/	srczhi
		:IN);*/

/*INT32/	addql[0..1], ddatlo, ddathi zero32
:LOCAL;
BEGIN

Phrase_mode\	:= INV1 (phrase_mode\, phrase_mode);
Zero		:= TIE0 (zero);
Zero32		:= JOIN (zero32, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero, zero);*/

/* Generate a changed data mask */

/*Edis		:= OR6 (edis\, dend[0..5]);
Ecoarse		:= DECL38E (e_coarse\[0..7], dend[3..5], edis\);
E_coarse[0]	:= INV1 (e_coarse[0], e_coarse\[0]);
Efine		:= DECL38E (unused[0], e_fine\[1..7], dend[0..2], e_coarse[0]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint8_t decl38e[2][8] = { { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },
		{ 0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF, 0x7F } };
	uint8_t dech38[8] = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
	uint8_t dech38el[2][8] = { { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 },
		{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };

			int en = (dend & 0x3F ? 1 : 0);
	uint8_t e_coarse = decl38e[en][(dend & 0x38) >> 3];		// Actually, this is e_coarse inverted...
	uint8_t e_fine = decl38e[(e_coarse & 0x01) ^ 0x01][dend & 0x07];
	e_fine &= 0xFE;
//////////////////////////////////////////////////////////////////////////////////////

/*Scoarse		:= DECH38 (s_coarse[0..7], dstart[3..5]);
Sfen\		:= INV1 (sfen\, s_coarse[0]);
Sfine		:= DECH38EL (s_fine[0..7], dstart[0..2], sfen\);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint8_t s_coarse = dech38[(dstart & 0x38) >> 3];
	uint8_t s_fine = dech38el[(s_coarse & 0x01) ^ 0x01][dstart & 0x07];
//////////////////////////////////////////////////////////////////////////////////////

/*Maskt[0]	:= BUF1 (maskt[0], s_fine[0]);
Maskt[1-7]	:= OAN1P (maskt[1-7], maskt[0-6], s_fine[1-7], e_fine\[1-7]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint16_t maskt = s_fine & 0x0001;
	maskt |= (((maskt & 0x0001) || (s_fine & 0x02)) && (e_fine & 0x02) ? 0x0002 : 0x0000);
	maskt |= (((maskt & 0x0002) || (s_fine & 0x04)) && (e_fine & 0x04) ? 0x0004 : 0x0000);
	maskt |= (((maskt & 0x0004) || (s_fine & 0x08)) && (e_fine & 0x08) ? 0x0008 : 0x0000);
	maskt |= (((maskt & 0x0008) || (s_fine & 0x10)) && (e_fine & 0x10) ? 0x0010 : 0x0000);
	maskt |= (((maskt & 0x0010) || (s_fine & 0x20)) && (e_fine & 0x20) ? 0x0020 : 0x0000);
	maskt |= (((maskt & 0x0020) || (s_fine & 0x40)) && (e_fine & 0x40) ? 0x0040 : 0x0000);
	maskt |= (((maskt & 0x0040) || (s_fine & 0x80)) && (e_fine & 0x80) ? 0x0080 : 0x0000);
//////////////////////////////////////////////////////////////////////////////////////

/* Produce a look-ahead on the ripple carry:
masktla = s_coarse[0] . /e_coarse[0] */
/*Masktla		:= AN2 (masktla, s_coarse[0], e_coarse\[0]);
Maskt[8]	:= OAN1P (maskt[8], masktla, s_coarse[1], e_coarse\[1]);
Maskt[9-14]	:= OAN1P (maskt[9-14], maskt[8-13], s_coarse[2-7], e_coarse\[2-7]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	maskt |= (((s_coarse & e_coarse & 0x01) || (s_coarse & 0x02)) && (e_coarse & 0x02) ? 0x0100 : 0x0000);
	maskt |= (((maskt & 0x0100) || (s_coarse & 0x04)) && (e_coarse & 0x04) ? 0x0200 : 0x0000);
	maskt |= (((maskt & 0x0200) || (s_coarse & 0x08)) && (e_coarse & 0x08) ? 0x0400 : 0x0000);
	maskt |= (((maskt & 0x0400) || (s_coarse & 0x10)) && (e_coarse & 0x10) ? 0x0800 : 0x0000);
	maskt |= (((maskt & 0x0800) || (s_coarse & 0x20)) && (e_coarse & 0x20) ? 0x1000 : 0x0000);
	maskt |= (((maskt & 0x1000) || (s_coarse & 0x40)) && (e_coarse & 0x40) ? 0x2000 : 0x0000);
	maskt |= (((maskt & 0x2000) || (s_coarse & 0x80)) && (e_coarse & 0x80) ? 0x4000 : 0x0000);
//////////////////////////////////////////////////////////////////////////////////////

/* The bit terms are mirrored for big-endian pixels outside phrase
mode.  The byte terms are mirrored for big-endian pixels in phrase
mode.  */

/*Mirror_bit	:= AN2M (mir_bit, phrase_mode\, big_pix);
Mirror_byte	:= AN2H (mir_byte, phrase_mode, big_pix);

Masktb[14]	:= BUF1 (masktb[14], maskt[14]);
Masku[0]	:= MX4 (masku[0],  maskt[0],  maskt[7],  maskt[14],  zero, mir_bit, mir_byte);
Masku[1]	:= MX4 (masku[1],  maskt[1],  maskt[6],  maskt[14],  zero, mir_bit, mir_byte);
Masku[2]	:= MX4 (masku[2],  maskt[2],  maskt[5],  maskt[14],  zero, mir_bit, mir_byte);
Masku[3]	:= MX4 (masku[3],  maskt[3],  maskt[4],  masktb[14], zero, mir_bit, mir_byte);
Masku[4]	:= MX4 (masku[4],  maskt[4],  maskt[3],  masktb[14], zero, mir_bit, mir_byte);
Masku[5]	:= MX4 (masku[5],  maskt[5],  maskt[2],  masktb[14], zero, mir_bit, mir_byte);
Masku[6]	:= MX4 (masku[6],  maskt[6],  maskt[1],  masktb[14], zero, mir_bit, mir_byte);
Masku[7]	:= MX4 (masku[7],  maskt[7],  maskt[0],  masktb[14], zero, mir_bit, mir_byte);
Masku[8]	:= MX2 (masku[8],  maskt[8],  maskt[13], mir_byte);
Masku[9]	:= MX2 (masku[9],  maskt[9],  maskt[12], mir_byte);
Masku[10]	:= MX2 (masku[10], maskt[10], maskt[11], mir_byte);
Masku[11]	:= MX2 (masku[11], maskt[11], maskt[10], mir_byte);
Masku[12]	:= MX2 (masku[12], maskt[12], maskt[9],  mir_byte);
Masku[13]	:= MX2 (masku[13], maskt[13], maskt[8],  mir_byte);
Masku[14]	:= MX2 (masku[14], maskt[14], maskt[0],  mir_byte);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool mir_bit = true/*big_pix*/ && !phrase_mode;
	bool mir_byte = true/*big_pix*/ && phrase_mode;
	uint16_t masku = maskt;

	if (mir_bit)
	{
		masku &= 0xFF00;
		masku |= (maskt >> 7) & 0x0001;
		masku |= (maskt >> 5) & 0x0002;
		masku |= (maskt >> 3) & 0x0004;
		masku |= (maskt >> 1) & 0x0008;
		masku |= (maskt << 1) & 0x0010;
		masku |= (maskt << 3) & 0x0020;
		masku |= (maskt << 5) & 0x0040;
		masku |= (maskt << 7) & 0x0080;
	}

	if (mir_byte)
	{
		masku = 0;
		masku |= (maskt >> 14) & 0x0001;
		masku |= (maskt >> 13) & 0x0002;
		masku |= (maskt >> 12) & 0x0004;
		masku |= (maskt >> 11) & 0x0008;
		masku |= (maskt >> 10) & 0x0010;
		masku |= (maskt >> 9)  & 0x0020;
		masku |= (maskt >> 8)  & 0x0040;
		masku |= (maskt >> 7)  & 0x0080;

		masku |= (maskt >> 5) & 0x0100;
		masku |= (maskt >> 3) & 0x0200;
		masku |= (maskt >> 1) & 0x0400;
		masku |= (maskt << 1) & 0x0800;
		masku |= (maskt << 3) & 0x1000;
		masku |= (maskt << 5) & 0x2000;
		masku |= (maskt << 7) & 0x4000;
	}
//////////////////////////////////////////////////////////////////////////////////////

/* The maskt terms define the area for changed data, but the byte
inhibit terms can override these */

/*Mask[0-7]	:= AN2 (mask[0-7], masku[0-7], dbinh\[0]);
Mask[8-14]	:= AN2H (mask[8-14], masku[8-14], dbinh\[1-7]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint16_t mask = masku & (!(dbinh & 0x01) ? 0xFFFF : 0xFF00);
	mask &= ~(((uint16_t)dbinh & 0x00FE) << 7);
//////////////////////////////////////////////////////////////////////////////////////

/*Addql[0]	:= JOIN (addql[0], addq[0..1]);
Addql[1]	:= JOIN (addql[1], addq[2..3]);

Dsel0b[0-1]	:= BUF8 (dsel0b[0-1], data_sel[0]);
Dsel1b[0-1]	:= BUF8 (dsel1b[0-1], data_sel[1]);
Ddatlo		:= MX4 (ddatlo, patd[0], lfu[0], addql[0], zero32, dsel0b[0], dsel1b[0]);
Ddathi		:= MX4 (ddathi, patd[1], lfu[1], addql[1], zero32, dsel0b[1], dsel1b[1]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	uint64_t dmux[4];
	dmux[0] = patd;
	dmux[1] = lfu;
	dmux[2] = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
	dmux[3] = 0;
	uint64_t ddat = dmux[data_sel];
//////////////////////////////////////////////////////////////////////////////////////

/*Zed_sel		:= AN2 (zed_sel, data_sel[0..1]);
Zed_selb[0-1]	:= BUF8 (zed_selb[0-1], zed_sel);

Dat[0-7]	:= MX4 (dat[0-7],   dstdlo{0-7},   ddatlo{0-7},   dstzlo{0-7},   srczlo{0-7},   mask[0-7], zed_selb[0]);
Dat[8-15]	:= MX4 (dat[8-15],  dstdlo{8-15},  ddatlo{8-15},  dstzlo{8-15},  srczlo{8-15},  mask[8],   zed_selb[0]);
Dat[16-23]	:= MX4 (dat[16-23], dstdlo{16-23}, ddatlo{16-23}, dstzlo{16-23}, srczlo{16-23}, mask[9],   zed_selb[0]);
Dat[24-31]	:= MX4 (dat[24-31], dstdlo{24-31}, ddatlo{24-31}, dstzlo{24-31}, srczlo{24-31}, mask[10],  zed_selb[0]);
Dat[32-39]	:= MX4 (dat[32-39], dstdhi{0-7},   ddathi{0-7},   dstzhi{0-7},   srczhi{0-7},   mask[11],  zed_selb[1]);
Dat[40-47]	:= MX4 (dat[40-47], dstdhi{8-15},  ddathi{8-15},  dstzhi{8-15},  srczhi{8-15},  mask[12],  zed_selb[1]);
Dat[48-55]	:= MX4 (dat[48-55], dstdhi{16-23}, ddathi{16-23}, dstzhi{16-23}, srczhi{16-23}, mask[13],  zed_selb[1]);
Dat[56-63]	:= MX4 (dat[56-63], dstdhi{24-31}, ddathi{24-31}, dstzhi{24-31}, srczhi{24-31}, mask[14],  zed_selb[1]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	wdata = ((ddat & mask) | (dstd & ~mask)) & 0x00000000000000FFLL;
	wdata |= (mask & 0x0100 ? ddat : dstd) & 0x000000000000FF00LL;
	wdata |= (mask & 0x0200 ? ddat : dstd) & 0x0000000000FF0000LL;
	wdata |= (mask & 0x0400 ? ddat : dstd) & 0x00000000FF000000LL;
	wdata |= (mask & 0x0800 ? ddat : dstd) & 0x000000FF00000000LL;
	wdata |= (mask & 0x1000 ? ddat : dstd) & 0x0000FF0000000000LL;
	wdata |= (mask & 0x2000 ? ddat : dstd) & 0x00FF000000000000LL;
	wdata |= (mask & 0x4000 ? ddat : dstd) & 0xFF00000000000000LL;
/*if (logBlit)
{
	printf("\n[ddat=%08X%08X dstd=%08X%08X wdata=%08X%08X mask=%04X]\n",
		(uint32_t)(ddat >> 32), (uint32_t)(ddat & 0xFFFFFFFF),
		(uint32_t)(dstd >> 32), (uint32_t)(dstd & 0xFFFFFFFF),
		(uint32_t)(wdata >> 32), (uint32_t)(wdata & 0xFFFFFFFF), mask);
	fflush(stdout);
}//*/
//This is a crappy way of handling this, but it should work for now...
	uint64_t zwdata;
	zwdata = ((srcz & mask) | (dstz & ~mask)) & 0x00000000000000FFLL;
	zwdata |= (mask & 0x0100 ? srcz : dstz) & 0x000000000000FF00LL;
	zwdata |= (mask & 0x0200 ? srcz : dstz) & 0x0000000000FF0000LL;
	zwdata |= (mask & 0x0400 ? srcz : dstz) & 0x00000000FF000000LL;
	zwdata |= (mask & 0x0800 ? srcz : dstz) & 0x000000FF00000000LL;
	zwdata |= (mask & 0x1000 ? srcz : dstz) & 0x0000FF0000000000LL;
	zwdata |= (mask & 0x2000 ? srcz : dstz) & 0x00FF000000000000LL;
	zwdata |= (mask & 0x4000 ? srcz : dstz) & 0xFF00000000000000LL;
if (logBlit)
{
	WriteLog("\n[srcz=%08X%08X dstz=%08X%08X zwdata=%08X%08X mask=%04X]\n",
		(uint32_t)(srcz >> 32), (uint32_t)(srcz & 0xFFFFFFFF),
		(uint32_t)(dstz >> 32), (uint32_t)(dstz & 0xFFFFFFFF),
		(uint32_t)(zwdata >> 32), (uint32_t)(zwdata & 0xFFFFFFFF), mask);
//	fflush(stdout);
}//*/
	srcz = zwdata;
//////////////////////////////////////////////////////////////////////////////////////

/*Data_enab[0-1]	:= BUF8 (data_enab[0-1], data_ena);
Datadrv[0-31]	:= TS (wdata[0-31],  dat[0-31],  data_enab[0]);
Datadrv[32-63]	:= TS (wdata[32-63], dat[32-63], data_enab[1]);

Unused[0]	:= DUMMY (unused[0]);

END;*/
}


/**  COMP_CTRL - Comparator output control logic  *****************

This block is responsible for taking the comparator outputs and
using them as appropriate to inhibit writes.  Two methods are
supported for inhibiting write data:

-	suppression of the inner loop controlled write operation
-	a set of eight byte inhibit lines to write back dest data

The first technique is used in pixel oriented modes, the second in
phrase mode, but the phrase mode form is only applicable to eight
and sixteen bit pixel modes.

Writes can be suppressed by data being equal, by the Z comparator
conditions being met, or by the bit to pixel expansion scheme.

Pipe-lining issues: the data derived comparator outputs are stable
until the next data read, well after the affected write from this
operation.  However, the inner counter bits can count immediately
before the ack for the last write.  Therefore, it is necessary to
delay bcompbit select terms by one inner loop pipe-line stage,
when generating the select for the data control - the output is
delayed one further tick to give it write data timing (2/34).

There is also a problem with computed data - the new values are
calculated before the write associated with the old value has been
performed.  The is taken care of within the zed comparator by
pipe-lining the comparator inputs where appropriate.
*/

//#define LOG_COMP_CTRL
/*DEF COMP_CTRL (
	dbinh\[0..7]	// destination byte inhibit lines
	nowrite		// suppress inner loop write operation
	:OUT;
	bcompen		// bit selector inhibit enable
	big_pix		// pixels are big-endian
	bkgwren		// enable dest data write in pix inhibit
	clk		// co-processor clock
	dcomp[0..7]	// output of data byte comparators
	dcompen		// data comparator inhibit enable
	icount[0..2]	// low bits of inner count
	pixsize[0..2]	// destination pixel size
	phrase_mode	// phrase write mode
	srcd[0..7]	// bits to use for bit to byte expansion
	step_inner	// inner loop advance
	zcomp[0..3]	// output of word zed comparators
	:IN);*/
void COMP_CTRL(uint8_t &dbinh, bool &nowrite,
	bool bcompen, bool big_pix, bool bkgwren, uint8_t dcomp, bool dcompen, uint8_t icount,
	uint8_t pixsize, bool phrase_mode, uint8_t srcd, uint8_t zcomp)
{
//BEGIN

/*Bkgwren\	:= INV1 (bkgwren\, bkgwren);
Phrase_mode\	:= INV1 (phrase_mode\, phrase_mode);
Pixsize\[0-2]	:= INV2 (pixsize\[0-2], pixsize[0-2]);*/

/* The bit comparator bits are derived from the source data, which
will have been suitably aligned for phrase mode.  The contents of
the inner counter are used to select which bit to use.

When not in phrase mode the inner count value is used to select
one bit.  It is assumed that the count has already occurred, so,
7 selects bit 0, etc.  In big-endian pixel mode, this turns round,
so that a count of 7 selects bit 7.

In phrase mode, the eight bits are used directly, and this mode is
only applicable to 8-bit pixel mode (2/34) */

/*Bcompselt[0-2]	:= EO (bcompselt[0-2], icount[0-2], big_pix);
Bcompbit	:= MX8 (bcompbit, srcd[7], srcd[6], srcd[5],
			srcd[4], srcd[3], srcd[2], srcd[1], srcd[0], bcompselt[0..2]);
Bcompbit\	:= INV1 (bcompbit\, bcompbit);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("\n     [bcompen=%s dcompen=%s phrase_mode=%s bkgwren=%s dcomp=%02X zcomp=%02X]", (bcompen ? "T" : "F"), (dcompen ? "T" : "F"), (phrase_mode ? "T" : "F"), (bkgwren ? "T" : "F"), dcomp, zcomp);
	WriteLog("\n     ");
//	fflush(stdout);
}
#endif
	uint8_t bcompselt = (big_pix ? ~icount : icount) & 0x07;
	uint8_t bitmask[8] = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };
	bool bcompbit = srcd & bitmask[bcompselt];
//////////////////////////////////////////////////////////////////////////////////////

/* pipe-line the count */
/*Bcompsel[0-2]	:= FDSYNC (bcompsel[0-2], bcompselt[0-2], step_inner, clk);
Bcompbt		:= MX8 (bcompbitpt, srcd[7], srcd[6], srcd[5],
			srcd[4], srcd[3], srcd[2], srcd[1], srcd[0], bcompsel[0..2]);
Bcompbitp	:= FD1Q (bcompbitp, bcompbitpt, clk);
Bcompbitp\	:= INV1 (bcompbitp\, bcompbitp);*/

/* For pixel mode, generate the write inhibit signal for all modes
on bit inhibit, for 8 and 16 bit modes on comparator inhibit, and
for 16 bit mode on Z inhibit

Nowrite = bcompen . /bcompbit . /phrase_mode
	+ dcompen . dcomp[0] . /phrase_mode . pixsize = 011
	+ dcompen . dcomp[0..1] . /phrase_mode . pixsize = 100
	+ zcomp[0] . /phrase_mode . pixsize = 100
*/

/*Nowt0		:= NAN3 (nowt[0], bcompen, bcompbit\, phrase_mode\);
Nowt1		:= ND6  (nowt[1], dcompen, dcomp[0], phrase_mode\, pixsize\[2], pixsize[0..1]);
Nowt2		:= ND7  (nowt[2], dcompen, dcomp[0..1], phrase_mode\, pixsize[2], pixsize\[0..1]);
Nowt3		:= NAN5 (nowt[3], zcomp[0], phrase_mode\, pixsize[2], pixsize\[0..1]);
Nowt4		:= NAN4 (nowt[4], nowt[0..3]);
Nowrite		:= AN2  (nowrite, nowt[4], bkgwren\);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	nowrite = ((bcompen && !bcompbit && !phrase_mode)
		|| (dcompen && (dcomp & 0x01) && !phrase_mode && (pixsize == 3))
		|| (dcompen && ((dcomp & 0x03) == 0x03) && !phrase_mode && (pixsize == 4))
		|| ((zcomp & 0x01) && !phrase_mode && (pixsize == 4)))
		&& !bkgwren;
//////////////////////////////////////////////////////////////////////////////////////

/*Winht		:= NAN3 (winht, bcompen, bcompbitp\, phrase_mode\);
Winhibit	:= NAN4 (winhibit, winht, nowt[1..3]);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
//This is the same as above, but with bcompbit delayed one tick and called 'winhibit'
//Small difference: Besides the pipeline effect, it's also not using !bkgwren...
//	bool winhibit = (bcompen && !
	bool winhibit = (bcompen && !bcompbit && !phrase_mode)
		|| (dcompen && (dcomp & 0x01) && !phrase_mode && (pixsize == 3))
		|| (dcompen && ((dcomp & 0x03) == 0x03) && !phrase_mode && (pixsize == 4))
		|| ((zcomp & 0x01) && !phrase_mode && (pixsize == 4));
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[nw=%s wi=%s]", (nowrite ? "T" : "F"), (winhibit ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/* For phrase mode, generate the byte inhibit signals for eight bit
mode 011, or sixteen bit mode 100
dbinh\[0] =  pixsize[2] . zcomp[0]
	 +  pixsize[2] . dcomp[0] . dcomp[1] . dcompen
	 + /pixsize[2] . dcomp[0] . dcompen
	 + /srcd[0] . bcompen

Inhibits 0-3 are also used when not in phrase mode to write back
destination data.
*/

/*Srcd\[0-7]	:= INV1 (srcd\[0-7], srcd[0-7]);

Di0t0		:= NAN2H (di0t[0], pixsize[2], zcomp[0]);
Di0t1		:= NAN4H (di0t[1], pixsize[2], dcomp[0..1], dcompen);
Di0t2		:= NAN2 (di0t[2], srcd\[0], bcompen);
Di0t3		:= NAN3 (di0t[3], pixsize\[2], dcomp[0], dcompen);
Di0t4		:= NAN4 (di0t[4], di0t[0..3]);
Dbinh[0]	:= ANR1P (dbinh\[0], di0t[4], phrase_mode, winhibit);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	dbinh = 0;
	bool di0t0_1 = ((pixsize & 0x04) && (zcomp & 0x01))
		|| ((pixsize & 0x04) && (dcomp & 0x01) && (dcomp & 0x02) && dcompen);
	bool di0t4 = di0t0_1
		|| (!(srcd & 0x01) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x01) && dcompen);
	dbinh |= (!((di0t4 && phrase_mode) || winhibit) ? 0x01 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di0t0_1=%s di0t4=%s]", (di0t0_1 ? "T" : "F"), (di0t4 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di1t0		:= NAN3 (di1t[0], pixsize\[2], dcomp[1], dcompen);
Di1t1		:= NAN2 (di1t[1], srcd\[1], bcompen);
Di1t2		:= NAN4 (di1t[2], di0t[0..1], di1t[0..1]);
Dbinh[1]	:= ANR1 (dbinh\[1], di1t[2], phrase_mode, winhibit);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool di1t2 = di0t0_1
		|| (!(srcd & 0x02) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x02) && dcompen);
	dbinh |= (!((di1t2 && phrase_mode) || winhibit) ? 0x02 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di1t2=%s]", (di1t2 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di2t0		:= NAN2H (di2t[0], pixsize[2], zcomp[1]);
Di2t1		:= NAN4H (di2t[1], pixsize[2], dcomp[2..3], dcompen);
Di2t2		:= NAN2 (di2t[2], srcd\[2], bcompen);
Di2t3		:= NAN3 (di2t[3], pixsize\[2], dcomp[2], dcompen);
Di2t4		:= NAN4 (di2t[4], di2t[0..3]);
Dbinh[2]	:= ANR1 (dbinh\[2], di2t[4], phrase_mode, winhibit);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
//[bcompen=F dcompen=T phrase_mode=T bkgwren=F][nw=F wi=F]
//[di0t0_1=F di0t4=F][di1t2=F][di2t0_1=T di2t4=T][di3t2=T][di4t0_1=F di2t4=F][di5t2=F][di6t0_1=F di6t4=F][di7t2=F]
//[dcomp=$00 dbinh=$0C][7804780400007804] (icount=0005, inc=4)
	bool di2t0_1 = ((pixsize & 0x04) && (zcomp & 0x02))
		|| ((pixsize & 0x04) && (dcomp & 0x04) && (dcomp & 0x08) && dcompen);
	bool di2t4 = di2t0_1
		|| (!(srcd & 0x04) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x04) && dcompen);
	dbinh |= (!((di2t4 && phrase_mode) || winhibit) ? 0x04 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di2t0_1=%s di2t4=%s]", (di2t0_1 ? "T" : "F"), (di2t4 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di3t0		:= NAN3 (di3t[0], pixsize\[2], dcomp[3], dcompen);
Di3t1		:= NAN2 (di3t[1], srcd\[3], bcompen);
Di3t2		:= NAN4 (di3t[2], di2t[0..1], di3t[0..1]);
Dbinh[3]	:= ANR1 (dbinh\[3], di3t[2], phrase_mode, winhibit);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool di3t2 = di2t0_1
		|| (!(srcd & 0x08) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x08) && dcompen);
	dbinh |= (!((di3t2 && phrase_mode) || winhibit) ? 0x08 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di3t2=%s]", (di3t2 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di4t0		:= NAN2H (di4t[0], pixsize[2], zcomp[2]);
Di4t1		:= NAN4H (di4t[1], pixsize[2], dcomp[4..5], dcompen);
Di4t2		:= NAN2 (di4t[2], srcd\[4], bcompen);
Di4t3		:= NAN3 (di4t[3], pixsize\[2], dcomp[4], dcompen);
Di4t4		:= NAN4 (di4t[4], di4t[0..3]);
Dbinh[4]	:= NAN2 (dbinh\[4], di4t[4], phrase_mode);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool di4t0_1 = ((pixsize & 0x04) && (zcomp & 0x04))
		|| ((pixsize & 0x04) && (dcomp & 0x10) && (dcomp & 0x20) && dcompen);
	bool di4t4 = di4t0_1
		|| (!(srcd & 0x10) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x10) && dcompen);
	dbinh |= (!(di4t4 && phrase_mode) ? 0x10 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di4t0_1=%s di2t4=%s]", (di4t0_1 ? "T" : "F"), (di4t4 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di5t0		:= NAN3 (di5t[0], pixsize\[2], dcomp[5], dcompen);
Di5t1		:= NAN2 (di5t[1], srcd\[5], bcompen);
Di5t2		:= NAN4 (di5t[2], di4t[0..1], di5t[0..1]);
Dbinh[5]	:= NAN2 (dbinh\[5], di5t[2], phrase_mode);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool di5t2 = di4t0_1
		|| (!(srcd & 0x20) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x20) && dcompen);
	dbinh |= (!(di5t2 && phrase_mode) ? 0x20 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di5t2=%s]", (di5t2 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di6t0		:= NAN2H (di6t[0], pixsize[2], zcomp[3]);
Di6t1		:= NAN4H (di6t[1], pixsize[2], dcomp[6..7], dcompen);
Di6t2		:= NAN2 (di6t[2], srcd\[6], bcompen);
Di6t3		:= NAN3 (di6t[3], pixsize\[2], dcomp[6], dcompen);
Di6t4		:= NAN4 (di6t[4], di6t[0..3]);
Dbinh[6]	:= NAN2 (dbinh\[6], di6t[4], phrase_mode);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool di6t0_1 = ((pixsize & 0x04) && (zcomp & 0x08))
		|| ((pixsize & 0x04) && (dcomp & 0x40) && (dcomp & 0x80) && dcompen);
	bool di6t4 = di6t0_1
		|| (!(srcd & 0x40) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x40) && dcompen);
	dbinh |= (!(di6t4 && phrase_mode) ? 0x40 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di6t0_1=%s di6t4=%s]", (di6t0_1 ? "T" : "F"), (di6t4 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

/*Di7t0		:= NAN3 (di7t[0], pixsize\[2], dcomp[7], dcompen);
Di7t1		:= NAN2 (di7t[1], srcd\[7], bcompen);
Di7t2		:= NAN4 (di7t[2], di6t[0..1], di7t[0..1]);
Dbinh[7]	:= NAN2 (dbinh\[7], di7t[2], phrase_mode);*/
////////////////////////////////////// C++ CODE //////////////////////////////////////
	bool di7t2 = di6t0_1
		|| (!(srcd & 0x80) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x80) && dcompen);
	dbinh |= (!(di7t2 && phrase_mode) ? 0x80 : 0x00);
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[di7t2=%s]", (di7t2 ? "T" : "F"));
//	fflush(stdout);
}
#endif
//////////////////////////////////////////////////////////////////////////////////////

//END;
//kludge
dbinh = ~dbinh;
#ifdef LOG_COMP_CTRL
if (logBlit)
{
	WriteLog("[dcomp=$%02X dbinh=$%02X]\n    ", dcomp, dbinh);
//	fflush(stdout);
}
#endif
}


////////////////////////////////////// C++ CODE //////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////

// !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!!
// !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!!
// !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!! TESTING !!!

#endif

