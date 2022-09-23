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
#include "settings.h"

// Blitter register RAM (most of it is hidden from the user)

static uint8_t blitter_ram[0x100];

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

// 1 bpp pixel read
#define PIXEL_SHIFT_1(a)      (((~a##_x) >> 16) & 7)
#define PIXEL_OFFSET_1(a)     (((((uint32_t)a##_y >> 16) * a##_width / 8) + (((uint32_t)a##_x >> 19) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 19) & 7))
#define READ_PIXEL_1(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_1(a), BLITTER) >> PIXEL_SHIFT_1(a)) & 0x01)

// 2 bpp pixel read
#define PIXEL_SHIFT_2(a)      (((~a##_x) >> 15) & 6)
#define PIXEL_OFFSET_2(a)     (((((uint32_t)a##_y >> 16) * a##_width / 4) + (((uint32_t)a##_x >> 18) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 18) & 7))
#define READ_PIXEL_2(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_2(a), BLITTER) >> PIXEL_SHIFT_2(a)) & 0x03)

// 4 bpp pixel read
#define PIXEL_SHIFT_4(a)      (((~a##_x) >> 14) & 4)
#define PIXEL_OFFSET_4(a)     (((((uint32_t)a##_y >> 16) * (a##_width/2)) + (((uint32_t)a##_x >> 17) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 17) & 7))
#define READ_PIXEL_4(a)       ((JaguarReadByte(a##_addr+PIXEL_OFFSET_4(a), BLITTER) >> PIXEL_SHIFT_4(a)) & 0x0f)

// 8 bpp pixel read
#define PIXEL_OFFSET_8(a)     (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~7)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 7))
#define READ_PIXEL_8(a)       (JaguarReadByte(a##_addr+PIXEL_OFFSET_8(a), BLITTER))

// 16 bpp pixel read
#define PIXEL_OFFSET_16(a)    (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~3)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 3))
#define READ_PIXEL_16(a)       (JaguarReadWord(a##_addr+(PIXEL_OFFSET_16(a)<<1), BLITTER))

// 32 bpp pixel read
#define PIXEL_OFFSET_32(a)    (((((uint32_t)a##_y >> 16) * a##_width) + (((uint32_t)a##_x >> 16) & ~1)) * (1 + a##_pitch) + (((uint32_t)a##_x >> 16) & 1))
#define READ_PIXEL_32(a)      (JaguarReadLong(a##_addr+(PIXEL_OFFSET_32(a)<<2), BLITTER))

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

// z data read
#define READ_ZDATA(a,f) (READ_ZDATA_16(a))

// 16 bpp z data write
#define WRITE_ZDATA_16(a,d)     {  JaguarWriteWord(a##_addr+(ZDATA_OFFSET_16(a)<<1), d, BLITTER); }

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

// 2 bpp pixel write
#define WRITE_PIXEL_2(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_2(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_2(a), BLITTER)&(~(0x03 << PIXEL_SHIFT_2(a))))|(d<<PIXEL_SHIFT_2(a)), BLITTER); }

// 4 bpp pixel write
#define WRITE_PIXEL_4(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_4(a), (JaguarReadByte(a##_addr+PIXEL_OFFSET_4(a), BLITTER)&(~(0x0f << PIXEL_SHIFT_4(a))))|(d<<PIXEL_SHIFT_4(a)), BLITTER); }

// 8 bpp pixel write
#define WRITE_PIXEL_8(a,d)       { JaguarWriteByte(a##_addr+PIXEL_OFFSET_8(a), d, BLITTER); }

// 16 bpp pixel write
#define WRITE_PIXEL_16(a,d)     {  JaguarWriteWord(a##_addr+(PIXEL_OFFSET_16(a)<<1), d, BLITTER); }

// 32 bpp pixel write
#define WRITE_PIXEL_32(a,d)		{ JaguarWriteLong(a##_addr+(PIXEL_OFFSET_32(a)<<2), d, BLITTER); }

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
static int gd_i[4];
static int gd_c[4];
static int gd_ia, gd_ca;
static int colour_index = 0;
static int32_t zadd;
static uint32_t z_i[4];

static int32_t a1_clip_x, a1_clip_y;

//
// Generic blit handler
//
void blitter_generic(uint32_t cmd)
{
	uint32_t srcdata, srczdata, dstdata, dstzdata, writedata, inhibit;
	uint32_t bppSrc = (DSTA2 ? 1 << ((REG(A1_FLAGS) >> 3) & 0x07) : 1 << ((REG(A2_FLAGS) >> 3) & 0x07));

	while (outer_loop--)
	{
		uint32_t a1_start = a1_x, a2_start = a2_x, bitPos = 0;

		if (BCOMPEN && SRCENX)
		{
			if (n_pixels < bppSrc)
				bitPos = bppSrc - n_pixels;
		}

		inner_loop = n_pixels;
		while (inner_loop--)
		{
			srcdata = srczdata = dstdata = dstzdata = writedata = inhibit = 0;

			if (!DSTA2)
			{
				if (SRCEN || SRCENX)
				{
					srcdata = READ_PIXEL(a2, REG(A2_FLAGS));

					if (SRCENZ)
						srczdata = READ_ZDATA(a2, REG(A2_FLAGS));
					else if (cmd & 0x0001C020)
						srczdata = READ_RDATA(SRCZINT, a2, REG(A2_FLAGS), a2_phrase_mode);
				}
				else
				{
					srcdata = READ_RDATA(SRCDATA, a2, REG(A2_FLAGS), a2_phrase_mode);

					if (cmd & 0x0001C020)
						srczdata = READ_RDATA(SRCZINT, a2, REG(A2_FLAGS), a2_phrase_mode);
				}

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

				if (GOURZ)
					srczdata = z_i[colour_index] >> 16;

				if (Z_OP_INF && srczdata <  dstzdata)	inhibit = 1;
				if (Z_OP_EQU && srczdata == dstzdata)	inhibit = 1;
				if (Z_OP_SUP && srczdata >  dstzdata)	inhibit = 1;

				if (DCOMPEN | BCOMPEN)
				{
					if (BCOMPEN)
					{
						uint32_t pixShift = (~bitPos) & (bppSrc - 1);
						srcdata = (srcdata >> pixShift) & 0x01;

						bitPos++;
					}

					if (!CMPDST)
					{
						if (srcdata == 0)
							inhibit = 1;
					}
					else
					{
						if (dstdata == READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode))
							inhibit = 1;
					}
				}

				if (CLIPA1)
				{
					inhibit |= (((a1_x >> 16) < a1_clip_x && (a1_x >> 16) >= 0
						&& (a1_y >> 16) < a1_clip_y && (a1_y >> 16) >= 0) ? 0 : 1);
				}

				if (!inhibit)
				{
					if (PATDSEL)
					{
						writedata = READ_RDATA(PATTERNDATA, a1, REG(A1_FLAGS), a1_phrase_mode);
					}
					else if (ADDDSEL)
					{
						writedata = (srcdata & 0xFF) + (dstdata & 0xFF);

						if (!TOPBEN)
						{
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

				if (BKGWREN || !inhibit)
				{
					WRITE_PIXEL(a1, REG(A1_FLAGS), writedata);
					if (DSTWRZ)
						WRITE_ZDATA(a1, REG(A1_FLAGS), srczdata);
				}
			}
			else
			{
				if (SRCEN)
				{
					srcdata = READ_PIXEL(a1, REG(A1_FLAGS));
					if (SRCENZ)
						srczdata = READ_ZDATA(a1, REG(A1_FLAGS));
					else if (cmd & 0x0001C020)
						srczdata = READ_RDATA(SRCZINT, a1, REG(A1_FLAGS), a1_phrase_mode);
				}
				else
				{
					srcdata = READ_RDATA(SRCDATA, a1, REG(A1_FLAGS), a1_phrase_mode);
					if (cmd & 0x001C020)
						srczdata = READ_RDATA(SRCZINT, a1, REG(A1_FLAGS), a1_phrase_mode);
				}

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

				if (Z_OP_INF && srczdata < dstzdata)	inhibit = 1;
				if (Z_OP_EQU && srczdata == dstzdata)	inhibit = 1;
				if (Z_OP_SUP && srczdata > dstzdata)	inhibit = 1;

				if (DCOMPEN | BCOMPEN)
				{
					if (!CMPDST)
					{
						if (srcdata == 0)
							inhibit = 1;
					}
					else
					{
						if (dstdata == READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode))
							inhibit = 1;
					}
				}

				if (CLIPA1)
				{
					inhibit |= (((a1_x >> 16) < a1_clip_x && (a1_x >> 16) >= 0
						&& (a1_y >> 16) < a1_clip_y && (a1_y >> 16) >= 0) ? 0 : 1);
				}

				if (!inhibit)
				{
					if (PATDSEL)
					{
						writedata = READ_RDATA(PATTERNDATA, a2, REG(A2_FLAGS), a2_phrase_mode);
					}
					else if (ADDDSEL)
					{
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

				if (BKGWREN || !inhibit)
				{
					WRITE_PIXEL(a2, REG(A2_FLAGS), writedata);

					if (DSTWRZ)
						WRITE_ZDATA(a2, REG(A2_FLAGS), srczdata);
				}
			}

			if (!BCOMPEN)
			{
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
			}

			if (GOURZ)
				z_i[colour_index] += zadd;

			if (GOURD || SRCSHADE)
			{
				gd_i[colour_index] += gd_ia;

				if ((int32_t)gd_i[colour_index] < 0)
					gd_i[colour_index] = 0;
				if (gd_i[colour_index] > 0x00FFFFFF)
					gd_i[colour_index] = 0x00FFFFFF;

				gd_c[colour_index] += gd_ca;
				if ((int32_t)gd_c[colour_index] < 0)
					gd_c[colour_index] = 0;
				if (gd_c[colour_index] > 0x000000FF)
					gd_c[colour_index] = 0x000000FF;
			}

			if (GOURD || SRCSHADE || GOURZ)
			{
				if (a1_phrase_mode)
					colour_index = (colour_index + 1) & 0x03;
			}
		}

		if (a1_phrase_mode)
		{
			uint32_t size = 64 / a1_psize;

			if (a2_phrase_mode && DSTA2)
			{
				uint32_t extra = (a2_start >> 16) % size;
				a1_x += extra << 16;
			}

			uint32_t pixelSize = (size - 1) << 16;
			a1_x = (a1_x + pixelSize) & ~pixelSize;
		}

		if (a2_phrase_mode)
		{
			uint32_t size = 64 / a2_psize;

			if (a1_phrase_mode && !DSTA2)
			{
				uint32_t extra = (a1_start >> 16) % size;
				a2_x += extra << 16;
			}

			uint32_t pixelSize = (size - 1) << 16;
			a2_x = (a2_x + pixelSize) & ~pixelSize;
		}

		a1_x += a1_step_x;
		a1_y += a1_step_y;
		a2_x += a2_step_x;
		a2_y += a2_step_y;
	}

	WREG(A1_PIXEL,  (a1_y & 0xFFFF0000) | ((a1_x >> 16) & 0xFFFF));
	WREG(A1_FPIXEL, (a1_y << 16) | (a1_x & 0xFFFF));
	WREG(A2_PIXEL,  (a2_y & 0xFFFF0000) | ((a2_x >> 16) & 0xFFFF));
}

void blitter_blit(uint32_t cmd)
{
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

	uint32_t m = (REG(A1_FLAGS) >> 9) & 0x03, e = (REG(A1_FLAGS) >> 11) & 0x0F;
	a1_width = ((0x04 | m) << e) >> 2;

	a2_x = (REG(A2_PIXEL) & 0x0000FFFF) << 16;
	a2_y = (REG(A2_PIXEL) & 0xFFFF0000);

	m = (REG(A2_FLAGS) >> 9) & 0x03, e = (REG(A2_FLAGS) >> 11) & 0x0F;
	a2_width = ((0x04 | m) << e) >> 2;
	a2_mask_x = ((REG(A2_MASK) & 0x0000FFFF) << 16) | 0xFFFF;
	a2_mask_y = (REG(A2_MASK) & 0xFFFF0000) | 0xFFFF;

	if (!(REG(A2_FLAGS) & 0x8000))
	{
		a2_mask_x = 0xFFFFFFFF;
		a2_mask_y = 0xFFFFFFFF;
	}

	a1_phrase_mode = 0;

	a2_yadd = a1_yadd = (YADD1_A1 ? 1 << 16 : 0);

	if (YSIGNSUB_A1)
		a1_yadd = -a1_yadd;

	switch (xadd_a1_control)
	{
		case XADDPHR:
			a1_xadd = 1 << 16;
			a1_phrase_mode = 1;
			break;
		case XADDPIX:
			a1_xadd = 1 << 16;
			break;
		case XADD0:
			a1_xadd = 0;
			break;
		case XADDINC:
			a1_xadd = (REG(A1_INC) << 16)		 | (REG(A1_FINC) & 0x0000FFFF);
			a1_yadd = (REG(A1_INC) & 0xFFFF0000) | (REG(A1_FINC) >> 16);
			break;
	}

	if (XSIGNSUB_A1)
		a1_xadd = -a1_xadd;

	if (YSIGNSUB_A2)
		a2_yadd = -a2_yadd;

	a2_phrase_mode = 0;

	switch (xadd_a2_control)
	{
		case XADDPHR:
			a2_xadd = 1 << 16;
			a2_phrase_mode = 1;
			break;
		case XADDPIX:
			a2_xadd = 1 << 16;
			break;
		case XADD0:
			a2_xadd = 0;
			break;
		case XADDINC:
			break;
	}

	if (XSIGNSUB_A2)
		a2_xadd = -a2_xadd;

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

	if (CLIPA1)
		a1_clip_x = REG(A1_CLIP) & 0x7FFF,
		a1_clip_y = (REG(A1_CLIP) >> 16) & 0x7FFF;

	a2_psize = 1 << ((REG(A2_FLAGS) >> 3) & 0x07);
	a1_psize = 1 << ((REG(A1_FLAGS) >> 3) & 0x07);

	if (GOURZ)
	{
		zadd = REG(ZINC);

		for(int v=0; v<4; v++)
			z_i[v] = REG(PHRASEZ0 + v*4);
	}

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

	blitter_generic(cmd);
}

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
}

uint8_t BlitterReadByte(uint32_t offset, uint32_t who)
{
	offset &= 0xFF;

	if (offset == (0x38 + 0))
		return 0x00;
	if (offset == (0x38 + 1))
		return 0x00;
	if (offset == (0x38 + 2))
		return 0x08;
	if (offset == (0x38 + 3))
		return 0x05;

	if (offset >= 0x04 && offset <= 0x07)
		return blitter_ram[offset + 0x08];

	if (offset >= 0x2C && offset <= 0x2F)
		return blitter_ram[offset + 0x04];

	return blitter_ram[offset];
}

uint16_t BlitterReadWord(uint32_t offset, uint32_t who)
{
	return ((uint16_t)BlitterReadByte(offset, who) << 8) | (uint16_t)BlitterReadByte(offset+1, who);
}

uint32_t BlitterReadLong(uint32_t offset, uint32_t who)
{
	return (BlitterReadWord(offset, who) << 16) | BlitterReadWord(offset+2, who);
}

void BlitterWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	offset &= 0xFF;

	if ((offset >= 0x7C) && (offset <= 0x9B))
	{
		switch (offset)
		{
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

void BlitterWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	BlitterWriteByte(offset + 0, data >> 8, who);
	BlitterWriteByte(offset + 1, data & 0xFF, who);

	if ((offset & 0xFF) == 0x3A)
	{
		if (vjs.useFastBlitter)
			blitter_blit(GET32(blitter_ram, 0x38));
		else
			BlitterMidsummer2();
	}
}

void BlitterWriteLong(uint32_t offset, uint32_t data, uint32_t who)
{
	BlitterWriteWord(offset + 0, data >> 16, who);
	BlitterWriteWord(offset + 2, data & 0xFFFF, who);
}

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

void BlitterMidsummer2(void)
{
	uint32_t cmd = GET32(blitter_ram, COMMAND);

	bool srcen = (SRCEN), srcenx = (SRCENX), srcenz = (SRCENZ),
		dsten = (DSTEN), dstenz = (DSTENZ), dstwrz = (DSTWRZ), clip_a1 = (CLIPA1),
		upda1 = (UPDA1), upda1f = (UPDA1F), upda2 = (UPDA2), dsta2 = (DSTA2),
		gourd = (GOURD), gourz = (GOURZ), topben = (TOPBEN), topnen = (TOPNEN),
		patdsel = (PATDSEL), adddsel = (ADDDSEL), cmpdst = (CMPDST), bcompen = (BCOMPEN),
		dcompen = (DCOMPEN), bkgwren = (BKGWREN), srcshade = (SRCSHADE);

	uint8_t zmode = (cmd & 0x01C0000) >> 18, lfufunc = (cmd & 0x1E00000) >> 21;

	bool polygon = false, datinit = false, a1_stepld = false, a2_stepld = false, ext_int = false;
	bool istepadd = false, istepfadd = false, finneradd = false, inneradd = false;
	bool zstepfadd = false, zstepadd = false;

	bool go = true, idle = true, inner = false, a1fupdate = false, a1update = false,
		zfupdate = false, zupdate = false, a2update = false, init_if = false, init_ii = false,
		init_zf = false, init_zi = false;

	bool outer0 = false, indone = false;

	bool idlei, inneri, a1fupdatei, a1updatei, zfupdatei, zupdatei, a2updatei, init_ifi, init_iii,
		init_zfi, init_zii;

	bool notgzandp = !(gourz && polygon);

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
	uint32_t a1_base = GET32(blitter_ram, A1_BASE) & 0xFFFFFFF8;
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
	uint32_t collision = GET32(blitter_ram, COLLISIONCTRL);

	uint8_t pixsize = (dsta2 ? a2_pixsize : a1_pixsize);

	a2addy = a1addy;

	bool phrase_mode = ((!dsta2 && a1addx == 0) || (dsta2 && a2addx == 0) ? true : false);

	uint16_t a1FracCInX = 0, a1FracCInY = 0;

	while (true)
	{
		if ((idle && !go) || (inner && outer0 && indone))
		{
			idlei = true;
			break;
		}
		else
			idlei = false;

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

		if (inner && indone && !outer0 && upda1f)
		{
			a1fupdatei = true;
		}
		else
			a1fupdatei = false;

		if ((a1fupdate)
			|| (inner && indone && !outer0 && !upda1f && upda1))
		{
			a1updatei = true;
		}
		else
			a1updatei = false;

		if ((a1update && gourz && polygon)
			|| (inner && indone && !outer0 && !upda1f && !upda1 && gourz && polygon))
		{
			zfupdatei = true;
		}
		else
			zfupdatei = false;

		if (zfupdate)
		{
			zupdatei = true;
		}
		else
			zupdatei = false;

		if ((a1update && upda2 && notgzandp)
			|| (zupdate && upda2)
			|| (inner && indone && !outer0 && !upda1f && notgzandp && !upda1 && upda2))
		{
			a2updatei = true;
		}
		else
			a2updatei = false;

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

		if (init_if)
		{
			init_iii = true;
		}
		else
			init_iii = false;

		if (init_ii && gourz)
		{
			init_zfi = true;
		}
		else
			init_zfi = false;

		if (init_zf)
		{
			init_zii = true;
		}
		else
			init_zii = false;

		idle = idlei;
		inner = inneri;
		a1fupdate = a1fupdatei;
		a1update = a1updatei;
		zfupdate = zfupdatei;
		zupdate = zupdatei;
		a2update = a2updatei;
		init_if = init_ifi;
		init_ii = init_iii;
		init_zf = init_zfi;
		init_zi = init_zii;

		if (inner)
		{
			indone = false;

			uint16_t icount = GET16(blitter_ram, PIXLINECOUNTER + 2);
			bool idle_inner = true, step = true, sreadx = false, szreadx = false, sread = false,
				szread = false, dread = false, dzread = false, dwrite = false, dzwrite = false;
			bool inner0 = false;
			bool idle_inneri, sreadxi, szreadxi, sreadi, szreadi, dreadi, dzreadi, dwritei, dzwritei;

			bool textext = false, txtread = false;

			uint8_t srcshift = 0;
			bool sshftld = true;

			while (true)
			{
				if ((idle_inner && !step)
					|| (dzwrite && step && inner0)
					|| (dwrite && step && !dstwrz && inner0))
				{
					idle_inneri = true;
					break;
				}
				else
					idle_inneri = false;

				if ((idle_inner && step && srcenx)
					|| (sreadx && !step))
				{
					sreadxi = true;
				}
				else
					sreadxi = false;

				if ((sreadx && step && srcenz)
					|| (szreadx && !step))
				{
					szreadxi = true;
				}
				else
					szreadxi = false;

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

				if ((sread && step && srcenz)
					|| (szread && !step))
				{
					szreadi = true;
				}
				else
					szreadi = false;

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


				if ((dzwrite && !step)
					|| (dwrite && step && dstwrz))
				{
					dzwritei = true;
				}
				else
					dzwritei = false;

				sshftld = idle_inner;

				idle_inner = idle_inneri;
				sreadx = sreadxi;
				szreadx = szreadxi;
				sread = sreadi;
				szread = szreadi;
				dread = dreadi;
				dzread = dzreadi;
				dwrite = dwritei;
				dzwrite = dzwritei;

				bool srca_addi = (sreadxi && !srcenz) || (sreadi && !srcenz) || szreadxi || szreadi;

				bool dsta_addi = (dwritei && !dstwrz) || dzwritei;

				bool gensrc = sreadxi || szreadxi || sreadi || szreadi;
				bool gendst = dreadi || dzreadi || dwritei || dzwritei;
				bool gena2i = (gensrc && !dsta2) || (gendst && dsta2);

				bool zaddr = szreadx || szread || dzread || dzwrite;

				bool fontread = (sread || sreadx) && bcompen;
				bool justify = !(!fontread && phrase_mode);

				bool a1_add = (dsta2 ? srca_addi : dsta_addi);
				bool a2_add = (dsta2 ? dsta_addi : srca_addi);

				uint8_t addasel = (a1fupdate || (a1_add && a1addx == 3) ? 0x01 : 0x00);
				addasel |= (a1_add && a1addx == 3 ? 0x02 : 0x00);
				addasel |= (a2update ? 0x04 : 0x00);

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

				bool adda_yconst = a1addy;

				bool addareg = ((a1_add && a1addx == 3) || a1update || a1fupdate
					|| (a2_add && a2addx == 3) || a2update ? true : false);

				bool suba_x = ((a1_add && a1xsign && a1addx == 1) || (a2_add && a2xsign && a2addx == 1) ? true : false);
				bool suba_y = ((a1_add && a1addy && a1ysign) || (a2_add && a2addy && a2ysign) ? true : false);

				uint8_t addbsel = (a2update || a2_add || (a1fupdate && a1_stepld)
				    || (a1update && a1_stepld) || (a2update && a2_stepld) ? 0x01 : 0x00);
				addbsel |= (a1fupdate || (a1_add && a1addx == 3) || (a1fupdate && a1_stepld)
				    || (a1update && a1_stepld) || (a2update && a2_stepld) ? 0x02 : 0x00);

				uint8_t maska1 = (a1_add && a1addx == 0 ? 6 - a1_pixsize : 0);
				uint8_t maska2 = (a2_add && a2addx == 0 ? 6 - a2_pixsize : 0);
				uint8_t modx = (a2_add ? maska2 : maska1);

				bool a1fracldi = a1fupdate || (a1_add && a1addx == 3);


				bool srcdreadd = false;

				bool shadeadd = dwrite && srcshade;

				uint8_t daddasel = ((dwrite && gourd) || (dzwrite && gourz) || istepadd || zstepfadd
					|| init_if || init_ii || init_zf || init_zi ? 0x01 : 0x00);
				daddasel |= ((dzwrite && gourz) || zstepadd || zstepfadd ? 0x02 : 0x00);
				daddasel |= (((gourd || gourz) && !(init_if || init_ii || init_zf || init_zi))
					|| (dwrite && srcshade) ? 0x04 : 0x00);

				uint8_t daddbsel = ((dwrite && gourd) || (dzwrite && gourz) || (dwrite && srcshade)
					|| istepadd || zstepadd || init_if || init_ii || init_zf || init_zi ? 0x01 : 0x00);
				daddbsel |= ((dzwrite && gourz) || zstepadd || zstepfadd ? 0x02 : 0x00);
				daddbsel |= ((dwrite && gourd) || (dzwrite && gourz) || (dwrite && srcshade)
					|| istepadd || istepfadd || zstepadd || zstepfadd ? 0x04 : 0x00);
				daddbsel |= (istepadd && istepfadd && zstepadd && zstepfadd ? 0x08 : 0x00);

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

				bool patfadd = (dwrite && gourd) || (istepfadd && !datinit) || init_if;
				bool patdadd = (dwrite && gourd) || (istepadd && !datinit) || init_ii;
				bool srcz1add = (dzwrite && gourz) || (zstepadd && !datinit) || init_zi;
				bool srcz2add = (dzwrite && gourz) || zstepfadd || init_zf;
				bool srcshadd = srcdreadd && srcshade;
				bool daddq_sel = patfadd || patdadd || srcz1add || srcz2add || srcshadd;

				uint8_t data_sel = ((!patdsel && !adddsel) || dzwrite ? 0x01 : 0x00)
					| (adddsel || dzwrite ? 0x02 : 0x00);

				uint32_t address, pixAddr;
				ADDRGEN(address, pixAddr, gena2i, zaddr,
					a1_x, a1_y, a1_base, a1_pitch, a1_pixsize, a1_width, a1_zoffset,
					a2_x, a2_y, a2_base, a2_pitch, a2_pixsize, a2_width, a2_zoffset);

				if (!justify)
					address &= 0xFFFFF8;

				uint8_t dstxp = (dsta2 ? a2_x : a1_x) & 0x3F;
				uint8_t srcxp = (dsta2 ? a1_x : a2_x) & 0x3F;
				uint8_t shftv = ((dstxp - srcxp) << pixsize) & 0x3F;

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

				shfti |= (srcen && phrase_mode ? (sshftld ? shftv & 0x38 : srcshift & 0x38) : 0);
				srcshift = shfti;

				if (sreadx)
				{
					srcd2 = srcd1;
					srcd1 = ((uint64_t)JaguarReadLong(address + 0, BLITTER) << 32)
						| (uint64_t)JaguarReadLong(address + 4, BLITTER);

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
				}

				if (szreadx)
				{
					srcz2 = srcz1;
					srcz1 = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);
				}

				if (sread)
				{
					srcd2 = srcd1;
					srcd1 = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);

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
				}

				if (szread)
				{
					srcz2 = srcz1;
					srcz1 = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);

					if (!phrase_mode && pixsize == 4)
						srcz1 >>= 48;
				}

				if (dread)
				{
					dstd = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);

					if (!phrase_mode)
					{
						if (pixsize == 5)
							dstd >>= 32;
						else if (pixsize == 4)
							dstd >>= 48;
						else
							dstd >>= 56;
					}
				}

				if (dzread)
				{
					dstz = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);

					if (!phrase_mode && pixsize == 4)
						dstz >>= 48;
				}

				uint64_t srcz;
				bool winhibit;

				if (dwrite)
				{
					int8_t inct = -((dsta2 ? a2_x : a1_x) & 0x07);
					uint8_t inc = 0;
					inc = (!phrase_mode || (phrase_mode && (inct & 0x01)) ? 0x01 : 0x00);
					inc |= (phrase_mode && (((pixsize == 3 || pixsize == 4) && (inct & 0x02)) || pixsize == 5 && !(inct & 0x01)) ? 0x02 : 0x00);
					inc |= (phrase_mode && ((pixsize == 3 && (inct & 0x04)) || (pixsize == 4 && !(inct & 0x03))) ? 0x04 : 0x00);
					inc |= (phrase_mode && pixsize == 3 && !(inct & 0x07) ? 0x08 : 0x00);

					uint16_t oldicount = icount;
					icount -= inc;

					if (icount == 0 || ((icount & 0x8000) && !(oldicount & 0x8000)))
						inner0 = true;

					uint8_t dstart = 0;

					if (pixsize == 3)
						dstart = (dstxp & 0x07) << 3;
					if (pixsize == 4)
						dstart = (dstxp & 0x03) << 4;
					if (pixsize == 5)
						dstart = (dstxp & 0x01) << 5;

					dstart = (phrase_mode ? dstart : pixAddr & 0x07);

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

					uint8_t inner_mask = 0;

					if (pixsize == 3)
						inner_mask = (icount & 0x07) << 3;
					if (pixsize == 4)
						inner_mask = (icount & 0x03) << 4;
					if (pixsize == 5)
						inner_mask = (icount & 0x01) << 5;
					if (!inner0)
						inner_mask = 0;

					window_mask = (window_mask == 0 ? 0x40 : window_mask);
					inner_mask = (inner_mask == 0 ? 0x40 : inner_mask);
					uint8_t emask = (window_mask > inner_mask ? inner_mask : window_mask);
					uint8_t pma = pixAddr + (1 << pixsize);
					uint8_t dend = (phrase_mode ? emask : pma);

					uint8_t pwidth = (((dend | dstart) & 0x07) == 0 ? 0x08 : (dend - dstart) & 0x07);

					if (phrase_mode && !dsten)
						dstd = ((uint64_t)JaguarReadLong(address, BLITTER) << 32) | (uint64_t)JaguarReadLong(address + 4, BLITTER);

					uint64_t srcd = (srcd2 << (64 - srcshift)) | (srcd1 >> srcshift);

					if (srcshift == 0)
						srcd = srcd1;

					if (!phrase_mode && srcshift != 0)
						srcd = ((srcd2 & 0xFF) << (8 - srcshift)) | ((srcd1 & 0xFF) >> srcshift);

					if (gourz)
					{
						uint16_t addq[4];
						uint8_t initcin[4] = { 0, 0, 0, 0 };
						ADDARRAY(addq, 7, 6, 0, 0, 0, initcin, 0, 0, 0, 0, 0, srcz1, srcz2, zinc, 0);
						srcz2 = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
						ADDARRAY(addq, 6, 7, 1, 0, 0, initcin, 0, 0, 0, 0, 0, srcz1, srcz2, zinc, 0);
						srcz1 = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
					}

					uint8_t zSrcShift = srcshift & 0x30;
					srcz = (srcz2 << (64 - zSrcShift)) | (srcz1 >> zSrcShift);

					if (zSrcShift == 0)
						srcz = srcz1;

					if (srcshade)
					{
						uint16_t addq[4];
						uint8_t initcin[4] = { 0, 0, 0, 0 };
						ADDARRAY(addq, 4, 5, 7, dstd, iinc, initcin, 0, 0, 0, patd, srcd, 0, 0, 0, 0);
						srcd = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
					}

					if (patfadd)
					{
						uint16_t addq[4];
						uint8_t initcin[4] = { 0, 0, 0, 0 };
						ADDARRAY(addq, 4, 4, 0, dstd, iinc, initcin, 0, 0, 0, patd, srcd, 0, 0, 0, 0);
						srcd1 = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
					}

					uint64_t wdata;
					uint8_t dcomp, zcomp;
					DATA(wdata, dcomp, zcomp, winhibit,
						true, cmpdst, daddasel, daddbsel, daddmode, daddq_sel, data_sel, 0,
						dend, dstart, dstd, iinc, lfufunc, patd, patdadd,
						phrase_mode, srcd, false, false, srcz2add, zmode,
						bcompen, bkgwren, dcompen, icount & 0x07, pixsize,
						srcz, dstz, zinc);

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
				}

				if (dzwrite)
				{

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
					}
				}

				if (a1_add)
				{
					int16_t adda_x, adda_y, addb_x, addb_y, data_x, data_y, addq_x, addq_y;
					ADDAMUX(adda_x, adda_y, addasel, a1_step_x, a1_step_y, a1_stepf_x, a1_stepf_y, a2_step_x, a2_step_y,
						a1_inc_x, a1_inc_y, a1_incf_x, a1_incf_y, adda_xconst, adda_yconst, addareg, suba_x, suba_y);
					ADDBMUX(addb_x, addb_y, addbsel, a1_x, a1_y, a2_x, a2_y, a1_frac_x, a1_frac_y);
					ADDRADD(addq_x, addq_y, a1fracldi, adda_x, adda_y, addb_x, addb_y, modx, suba_x, suba_y);

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
					int16_t adda_x, adda_y, addb_x, addb_y, data_x, data_y, addq_x, addq_y;
					ADDAMUX(adda_x, adda_y, addasel, a1_step_x, a1_step_y, a1_stepf_x, a1_stepf_y, a2_step_x, a2_step_y,
						a1_inc_x, a1_inc_y, a1_incf_x, a1_incf_y, adda_xconst, adda_yconst, addareg, suba_x, suba_y);
					ADDBMUX(addb_x, addb_y, addbsel, a1_x, a1_y, a2_x, a2_y, a1_frac_x, a1_frac_y);
					ADDRADD(addq_x, addq_y, a1fracldi, adda_x, adda_y, addb_x, addb_y, modx, suba_x, suba_y);

					a2_x = addq_x;
					a2_y = addq_y;
				}
			}

			indone = true;
			ocount--;

			if (ocount == 0)
				outer0 = true;
		}

		if (a1fupdate)
		{
			uint32_t a1_frac_xt = (uint32_t)a1_frac_x + (uint32_t)a1_stepf_x;
			uint32_t a1_frac_yt = (uint32_t)a1_frac_y + (uint32_t)a1_stepf_y;
			a1FracCInX = a1_frac_xt >> 16;
			a1FracCInY = a1_frac_yt >> 16;
			a1_frac_x = (uint16_t)(a1_frac_xt & 0xFFFF);
			a1_frac_y = (uint16_t)(a1_frac_yt & 0xFFFF);
		}

		if (a1update)
		{
			a1_x += a1_step_x + a1FracCInX;
			a1_y += a1_step_y + a1FracCInY;
		}

		if (a2update)
		{
			a2_x += a2_step_x;
			a2_y += a2_step_y;
		}
	}

	SET16(blitter_ram, A1_PIXEL + 2, a1_x);
	SET16(blitter_ram, A1_PIXEL + 0, a1_y);
	SET16(blitter_ram, A1_FPIXEL + 2, a1_frac_x);
	SET16(blitter_ram, A1_FPIXEL + 0, a1_frac_y);
	SET16(blitter_ram, A2_PIXEL + 2, a2_x);
	SET16(blitter_ram, A2_PIXEL + 0, a2_y);
}

void ADDRGEN(uint32_t &address, uint32_t &pixa, bool gena2, bool zaddr,
	uint16_t a1_x, uint16_t a1_y, uint32_t a1_base, uint8_t a1_pitch, uint8_t a1_pixsize, uint8_t a1_width, uint8_t a1_zoffset,
	uint16_t a2_x, uint16_t a2_y, uint32_t a2_base, uint8_t a2_pitch, uint8_t a2_pixsize, uint8_t a2_width, uint8_t a2_zoffset)
{
	uint16_t x = (gena2 ? a2_x : a1_x) & 0xFFFF;
	uint16_t y = (gena2 ? a2_y : a1_y) & 0x0FFF;
	uint8_t width = (gena2 ? a2_width : a1_width);
	uint8_t pixsize = (gena2 ? a2_pixsize : a1_pixsize);
	uint8_t pitch = (gena2 ? a2_pitch : a1_pitch);
	uint32_t base = (gena2 ? a2_base : a1_base) >> 3;
	uint8_t zoffset = (gena2 ? a2_zoffset : a1_zoffset);

	uint32_t ytm = ((uint32_t)y << 2) + (width & 0x02 ? (uint32_t)y << 1 : 0) + (width & 0x01 ? (uint32_t)y : 0);

	uint32_t ya = (ytm << (width >> 2)) >> 2;

	uint32_t pa = ya + x;

	pixa = pa << pixsize;

	uint8_t pt = ((pitch & 0x01) && !(pitch & 0x02) ? 0x01 : 0x00)
		| (!(pitch & 0x01) && (pitch & 0x02) ? 0x02 : 0x00);
	uint32_t phradr = (pixa >> 6) << pt;
	uint32_t shup = (pitch == 0x03 ? (pixa >> 6) : 0);

	uint8_t za = (zaddr ? zoffset : 0) & 0x03;
	uint32_t addr = za + phradr + (shup << 1) + base;
	address = ((pixa & 0x38) >> 3) | ((addr & 0x1FFFFF) << 3);

	pixa &= 0x07;
}

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

	static uint8_t co[4];
	uint8_t cin[4];

	for(int i=0; i<4; i++)
		cin[i] = initcin[i] | (co[i] & cinsel);

	bool eightbit = daddmode & 0x02;
	bool sat = daddmode & 0x03;
	bool hicinh = ((daddmode & 0x03) == 0x03);

	for(int i=0; i<4; i++)
		ADD16SAT(addq[i], co[i], adda[i], addb[i], cin[i], sat, eightbit, hicinh);
}

void ADD16SAT(uint16_t &r, uint8_t &co, uint16_t a, uint16_t b, uint8_t cin, bool sat, bool eightbit, bool hicinh)
{
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

	r = (saturate ? (ctop ? 0x00FF : 0x0000) : q & 0x00FF);
	r |= (hisaturate ? (ctop ? 0xFF00 : 0x0000) : q & 0xFF00);
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

void ADDAMUX(int16_t &adda_x, int16_t &adda_y, uint8_t addasel, int16_t a1_step_x, int16_t a1_step_y,
	int16_t a1_stepf_x, int16_t a1_stepf_y, int16_t a2_step_x, int16_t a2_step_y,
	int16_t a1_inc_x, int16_t a1_inc_y, int16_t a1_incf_x, int16_t a1_incf_y, uint8_t adda_xconst,
	bool adda_yconst, bool addareg, bool suba_x, bool suba_y)
{
	int16_t xterm[4], yterm[4];
	xterm[0] = a1_step_x, xterm[1] = a1_stepf_x, xterm[2] = a1_inc_x, xterm[3] = a1_incf_x;
	yterm[0] = a1_step_y, yterm[1] = a1_stepf_y, yterm[2] = a1_inc_y, yterm[3] = a1_incf_y;
	int16_t addar_x = (addasel & 0x04 ? a2_step_x : xterm[addasel & 0x03]);
	int16_t addar_y = (addasel & 0x04 ? a2_step_y : yterm[addasel & 0x03]);

	int16_t addac_x = (adda_xconst == 0x07 ? 0 : 1 << adda_xconst);
	int16_t addac_y = (adda_yconst ? 0x01 : 0);

	int16_t addas_x = (addareg ? addar_x : addac_x);
	int16_t addas_y = (addareg ? addar_y : addac_y);

	adda_x = addas_x ^ (suba_x ? 0xFFFF : 0x0000);
	adda_y = addas_y ^ (suba_y ? 0xFFFF : 0x0000);
}

/**  ADDBMUX - Address adder input B selection  *******************

This module selects the register to be updated by the address
adder.  This can be one of three registers, the A1 and A2
pointers, or the A1 fractional part. It can also be zero, so that the step
registers load directly into the pointers.
*/

void ADDBMUX(int16_t &addb_x, int16_t &addb_y, uint8_t addbsel, int16_t a1_x, int16_t a1_y,
	int16_t a2_x, int16_t a2_y, int16_t a1_frac_x, int16_t a1_frac_y)
{
	int16_t xterm[4], yterm[4];
	xterm[0] = a1_x, xterm[1] = a2_x, xterm[2] = a1_frac_x, xterm[3] = 0;
	yterm[0] = a1_y, yterm[1] = a2_y, yterm[2] = a1_frac_y, yterm[3] = 0;
	addb_x = xterm[addbsel & 0x03];
	addb_y = yterm[addbsel & 0x03];
}

/**  DATAMUX - Address local data bus selection  ******************

Select between the adder output and the input data bus
*/

void DATAMUX(int16_t &data_x, int16_t &data_y, uint32_t gpu_din, int16_t addq_x, int16_t addq_y, bool addqsel)
{
	data_x = (addqsel ? addq_x : (int16_t)(gpu_din & 0xFFFF));
	data_y = (addqsel ? addq_y : (int16_t)(gpu_din >> 16));
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

void ADDRADD(int16_t &addq_x, int16_t &addq_y, bool a1fracldi,
	uint16_t adda_x, uint16_t adda_y, uint16_t addb_x, uint16_t addb_y, uint8_t modx, bool suba_x, bool suba_y)
{
	static uint16_t co_x = 0, co_y = 0;
	uint16_t ci_x = co_x ^ (suba_x ? 1 : 0);
	uint16_t ci_y = co_y ^ (suba_y ? 1 : 0);
	uint32_t addqt_x = adda_x + addb_x + ci_x;
	uint32_t addqt_y = adda_y + addb_y + ci_y;
	co_x = ((addqt_x & 0x10000) && a1fracldi ? 1 : 0);
	co_y = ((addqt_y & 0x10000) && a1fracldi ? 1 : 0);

	uint16_t mask[8] = { 0xFFFF, 0xFFFE, 0xFFFC, 0xFFF8, 0xFFF0, 0xFFE0, 0xFFC0, 0x0000 };
	addq_x = addqt_x & mask[modx];
	addq_y = addqt_y & 0xFFFF;
}

void DATA(uint64_t &wdata, uint8_t &dcomp, uint8_t &zcomp, bool &nowrite,
	bool big_pix, bool cmpdst, uint8_t daddasel, uint8_t daddbsel, uint8_t daddmode, bool daddq_sel, uint8_t data_sel,
	uint8_t dbinh, uint8_t dend, uint8_t dstart, uint64_t dstd, uint32_t iinc, uint8_t lfu_func, uint64_t &patd, bool patdadd,
	bool phrase_mode, uint64_t srcd, bool srcdread, bool srczread, bool srcz2add, uint8_t zmode,
	bool bcompen, bool bkgwren, bool dcompen, uint8_t icount, uint8_t pixsize,
	uint64_t &srcz, uint64_t dstz, uint32_t zinc)
{
	uint64_t funcmask[2] = { 0, 0xFFFFFFFFFFFFFFFFLL };
	uint64_t func0 = funcmask[lfu_func & 0x01];
	uint64_t func1 = funcmask[(lfu_func >> 1) & 0x01];
	uint64_t func2 = funcmask[(lfu_func >> 2) & 0x01];
	uint64_t func3 = funcmask[(lfu_func >> 3) & 0x01];
	uint64_t lfu = (~srcd & ~dstd & func0) | (~srcd & dstd & func1) | (srcd & ~dstd & func2) | (srcd & dstd & func3);

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

	uint8_t dbinht;
	COMP_CTRL(dbinht, nowrite,
		bcompen, true, bkgwren, dcomp, dcompen, icount, pixsize, phrase_mode, srcd & 0xFF, zcomp);
	dbinh = dbinht;

	uint16_t addq[4];
	uint8_t initcin[4] = { 0, 0, 0, 0 };
	ADDARRAY(addq, daddasel, daddbsel, daddmode, dstd, iinc, initcin, 0, 0, 0, patd, srcd, 0, 0, 0, 0);

	if (patdadd)
		patd = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];

	uint8_t decl38e[2][8] = { { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },
		{ 0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF, 0x7F } };
	uint8_t dech38[8] = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
	uint8_t dech38el[2][8] = { { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 },
		{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };

	int en = (dend & 0x3F ? 1 : 0);
	uint8_t e_coarse = decl38e[en][(dend & 0x38) >> 3];
	uint8_t e_fine = decl38e[(e_coarse & 0x01) ^ 0x01][dend & 0x07];
	e_fine &= 0xFE;

	uint8_t s_coarse = dech38[(dstart & 0x38) >> 3];
	uint8_t s_fine = dech38el[(s_coarse & 0x01) ^ 0x01][dstart & 0x07];

	uint16_t maskt = s_fine & 0x0001;
	maskt |= (((maskt & 0x0001) || (s_fine & 0x02)) && (e_fine & 0x02) ? 0x0002 : 0x0000);
	maskt |= (((maskt & 0x0002) || (s_fine & 0x04)) && (e_fine & 0x04) ? 0x0004 : 0x0000);
	maskt |= (((maskt & 0x0004) || (s_fine & 0x08)) && (e_fine & 0x08) ? 0x0008 : 0x0000);
	maskt |= (((maskt & 0x0008) || (s_fine & 0x10)) && (e_fine & 0x10) ? 0x0010 : 0x0000);
	maskt |= (((maskt & 0x0010) || (s_fine & 0x20)) && (e_fine & 0x20) ? 0x0020 : 0x0000);
	maskt |= (((maskt & 0x0020) || (s_fine & 0x40)) && (e_fine & 0x40) ? 0x0040 : 0x0000);
	maskt |= (((maskt & 0x0040) || (s_fine & 0x80)) && (e_fine & 0x80) ? 0x0080 : 0x0000);

	maskt |= (((s_coarse & e_coarse & 0x01) || (s_coarse & 0x02)) && (e_coarse & 0x02) ? 0x0100 : 0x0000);
	maskt |= (((maskt & 0x0100) || (s_coarse & 0x04)) && (e_coarse & 0x04) ? 0x0200 : 0x0000);
	maskt |= (((maskt & 0x0200) || (s_coarse & 0x08)) && (e_coarse & 0x08) ? 0x0400 : 0x0000);
	maskt |= (((maskt & 0x0400) || (s_coarse & 0x10)) && (e_coarse & 0x10) ? 0x0800 : 0x0000);
	maskt |= (((maskt & 0x0800) || (s_coarse & 0x20)) && (e_coarse & 0x20) ? 0x1000 : 0x0000);
	maskt |= (((maskt & 0x1000) || (s_coarse & 0x40)) && (e_coarse & 0x40) ? 0x2000 : 0x0000);
	maskt |= (((maskt & 0x2000) || (s_coarse & 0x80)) && (e_coarse & 0x80) ? 0x4000 : 0x0000);

	bool mir_bit = !phrase_mode;
	bool mir_byte = phrase_mode;
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

	uint16_t mask = masku & (!(dbinh & 0x01) ? 0xFFFF : 0xFF00);
	mask &= ~(((uint16_t)dbinh & 0x00FE) << 7);

	uint64_t dmux[4];
	dmux[0] = patd;
	dmux[1] = lfu;
	dmux[2] = ((uint64_t)addq[3] << 48) | ((uint64_t)addq[2] << 32) | ((uint64_t)addq[1] << 16) | (uint64_t)addq[0];
	dmux[3] = 0;
	uint64_t ddat = dmux[data_sel];

	wdata = ((ddat & mask) | (dstd & ~mask)) & 0x00000000000000FFLL;
	wdata |= (mask & 0x0100 ? ddat : dstd) & 0x000000000000FF00LL;
	wdata |= (mask & 0x0200 ? ddat : dstd) & 0x0000000000FF0000LL;
	wdata |= (mask & 0x0400 ? ddat : dstd) & 0x00000000FF000000LL;
	wdata |= (mask & 0x0800 ? ddat : dstd) & 0x000000FF00000000LL;
	wdata |= (mask & 0x1000 ? ddat : dstd) & 0x0000FF0000000000LL;
	wdata |= (mask & 0x2000 ? ddat : dstd) & 0x00FF000000000000LL;
	wdata |= (mask & 0x4000 ? ddat : dstd) & 0xFF00000000000000LL;

	uint64_t zwdata;
	zwdata = ((srcz & mask) | (dstz & ~mask)) & 0x00000000000000FFLL;
	zwdata |= (mask & 0x0100 ? srcz : dstz) & 0x000000000000FF00LL;
	zwdata |= (mask & 0x0200 ? srcz : dstz) & 0x0000000000FF0000LL;
	zwdata |= (mask & 0x0400 ? srcz : dstz) & 0x00000000FF000000LL;
	zwdata |= (mask & 0x0800 ? srcz : dstz) & 0x000000FF00000000LL;
	zwdata |= (mask & 0x1000 ? srcz : dstz) & 0x0000FF0000000000LL;
	zwdata |= (mask & 0x2000 ? srcz : dstz) & 0x00FF000000000000LL;
	zwdata |= (mask & 0x4000 ? srcz : dstz) & 0xFF00000000000000LL;

	srcz = zwdata;
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

void COMP_CTRL(uint8_t &dbinh, bool &nowrite,
	bool bcompen, bool big_pix, bool bkgwren, uint8_t dcomp, bool dcompen, uint8_t icount,
	uint8_t pixsize, bool phrase_mode, uint8_t srcd, uint8_t zcomp)
{
	uint8_t bcompselt = (big_pix ? ~icount : icount) & 0x07;
	uint8_t bitmask[8] = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };
	bool bcompbit = srcd & bitmask[bcompselt];

	nowrite = ((bcompen && !bcompbit && !phrase_mode)
		|| (dcompen && (dcomp & 0x01) && !phrase_mode && (pixsize == 3))
		|| (dcompen && ((dcomp & 0x03) == 0x03) && !phrase_mode && (pixsize == 4))
		|| ((zcomp & 0x01) && !phrase_mode && (pixsize == 4)))
		&& !bkgwren;

	bool winhibit = (bcompen && !bcompbit && !phrase_mode)
		|| (dcompen && (dcomp & 0x01) && !phrase_mode && (pixsize == 3))
		|| (dcompen && ((dcomp & 0x03) == 0x03) && !phrase_mode && (pixsize == 4))
		|| ((zcomp & 0x01) && !phrase_mode && (pixsize == 4));

	dbinh = 0;
	bool di0t0_1 = ((pixsize & 0x04) && (zcomp & 0x01))
		|| ((pixsize & 0x04) && (dcomp & 0x01) && (dcomp & 0x02) && dcompen);
	bool di0t4 = di0t0_1
		|| (!(srcd & 0x01) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x01) && dcompen);
	dbinh |= (!((di0t4 && phrase_mode) || winhibit) ? 0x01 : 0x00);

	bool di1t2 = di0t0_1
		|| (!(srcd & 0x02) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x02) && dcompen);
	dbinh |= (!((di1t2 && phrase_mode) || winhibit) ? 0x02 : 0x00);

	bool di2t0_1 = ((pixsize & 0x04) && (zcomp & 0x02))
		|| ((pixsize & 0x04) && (dcomp & 0x04) && (dcomp & 0x08) && dcompen);
	bool di2t4 = di2t0_1
		|| (!(srcd & 0x04) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x04) && dcompen);
	dbinh |= (!((di2t4 && phrase_mode) || winhibit) ? 0x04 : 0x00);

	bool di3t2 = di2t0_1
		|| (!(srcd & 0x08) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x08) && dcompen);
	dbinh |= (!((di3t2 && phrase_mode) || winhibit) ? 0x08 : 0x00);

	bool di4t0_1 = ((pixsize & 0x04) && (zcomp & 0x04))
		|| ((pixsize & 0x04) && (dcomp & 0x10) && (dcomp & 0x20) && dcompen);
	bool di4t4 = di4t0_1
		|| (!(srcd & 0x10) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x10) && dcompen);
	dbinh |= (!(di4t4 && phrase_mode) ? 0x10 : 0x00);

	bool di5t2 = di4t0_1
		|| (!(srcd & 0x20) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x20) && dcompen);
	dbinh |= (!(di5t2 && phrase_mode) ? 0x20 : 0x00);

	bool di6t0_1 = ((pixsize & 0x04) && (zcomp & 0x08))
		|| ((pixsize & 0x04) && (dcomp & 0x40) && (dcomp & 0x80) && dcompen);
	bool di6t4 = di6t0_1
		|| (!(srcd & 0x40) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x40) && dcompen);
	dbinh |= (!(di6t4 && phrase_mode) ? 0x40 : 0x00);

	bool di7t2 = di6t0_1
		|| (!(srcd & 0x80) && bcompen)
		|| (!(pixsize & 0x04) && (dcomp & 0x80) && dcompen);
	dbinh |= (!(di7t2 && phrase_mode) ? 0x80 : 0x00);

	dbinh = ~dbinh;
}
