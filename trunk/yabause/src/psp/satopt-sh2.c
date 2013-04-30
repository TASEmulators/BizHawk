/*  src/psp/satopt-sh2.c: Saturn-specific SH-2 optimization routines
    Copyright 2009-2010 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

/*************************************************************************/
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "../core.h"
#include "../cs2.h"
#include "../memory.h"
#include "../scsp.h"
#include "../scu.h"
#include "../sh2core.h"
#include "../vdp1.h"
#include "../vdp2.h"
#include "../yabause.h"

#include "sh2.h"
#include "sh2-internal.h"

#include "misc.h"
#include "satopt-sh2.h"

#ifdef JIT_DEBUG_TRACE
# include "../sh2d.h"
#endif

/*************************************************************************/

/* Shorthand macros for memory access, both saving us from having to write
 * out 0xFFFFF all over the place and allowing optimized memory access
 * using constant offsets where possible (we assume small constant offsets
 * don't fall outside the relevant memory region).  Note that offsetted
 * byte accesses to 16-bit-organized memory generally will not be optimized
 * on little-endian systems, because the XOR has to be applied after adding
 * the offset to the base; if the base is known to be 16-bit aligned, it
 * may be faster to load a 16-bit word and mask or shift as appropriate. */

#define HRAM_PTR(base)  ((uint8_t *)&HighWram[(base) & 0xFFFFF])
#define HRAM_LOADB(base,offset) \
    T2ReadByte(HighWram, ((base) + (offset)) & 0xFFFFF)
#define HRAM_LOADW(base,offset) \
    T2ReadWord(HighWram, ((base) & 0xFFFFF) + (offset))
#define HRAM_LOADL(base,offset) \
    T2ReadLong(HighWram, ((base) & 0xFFFFF) + (offset))
#define HRAM_STOREB(base,offset,data) \
    T2WriteByte(HighWram, ((base) + (offset)) & 0xFFFFF, (data))
#define HRAM_STOREW(base,offset,data) \
    T2WriteWord(HighWram, ((base) & 0xFFFFF) + (offset), (data))
#define HRAM_STOREL(base,offset,data) \
    T2WriteLong(HighWram, ((base) & 0xFFFFF) + (offset), (data))

#define LRAM_PTR(base)  ((uint8_t *)&LowWram[(base) & 0xFFFFF])
#define LRAM_LOADB(base,offset) \
    T2ReadByte(LowWram, ((base) + (offset)) & 0xFFFFF)
#define LRAM_LOADW(base,offset) \
    T2ReadWord(LowWram, ((base) & 0xFFFFF) + (offset))
#define LRAM_LOADL(base,offset) \
    T2ReadLong(LowWram, ((base) & 0xFFFFF) + (offset))
#define LRAM_STOREB(base,offset,data) \
    T2WriteByte(LowWram, ((base) + (offset)) & 0xFFFFF, (data))
#define LRAM_STOREW(base,offset,data) \
    T2WriteWord(LowWram, ((base) & 0xFFFFF) + (offset), (data))
#define LRAM_STOREL(base,offset,data) \
    T2WriteLong(LowWram, ((base) & 0xFFFFF) + (offset), (data))

#define VDP1_PTR(base)  ((uint8_t *)&Vdp1Ram[(base) & 0x7FFFF])
#define VDP1_LOADB(base,offset) \
    T1ReadByte(Vdp1Ram, ((base) & 0x7FFFF) + (offset))
#define VDP1_LOADW(base,offset) \
    T1ReadWord(Vdp1Ram, ((base) & 0x7FFFF) + (offset))
#define VDP1_LOADL(base,offset) \
    T1ReadLong(Vdp1Ram, ((base) & 0x7FFFF) + (offset))
#define VDP1_STOREB(base,offset,data) \
    T1WriteByte(Vdp1Ram, ((base) & 0x7FFFF) + (offset), (data))
#define VDP1_STOREW(base,offset,data) \
    T1WriteWord(Vdp1Ram, ((base) & 0x7FFFF) + (offset), (data))
#define VDP1_STOREL(base,offset,data) \
    T1WriteLong(Vdp1Ram, ((base) & 0x7FFFF) + (offset), (data))

#define VDP2_PTR(base)  ((uint8_t *)&Vdp2Ram[(base) & 0x7FFFF])
#define VDP2_LOADB(base,offset) \
    T1ReadByte(Vdp2Ram, ((base) & 0x7FFFF) + (offset))
#define VDP2_LOADW(base,offset) \
    T1ReadWord(Vdp2Ram, ((base) & 0x7FFFF) + (offset))
#define VDP2_LOADL(base,offset) \
    T1ReadLong(Vdp2Ram, ((base) & 0x7FFFF) + (offset))
#define VDP2_STOREB(base,offset,data) \
    T1WriteByte(Vdp2Ram, ((base) & 0x7FFFF) + (offset), (data))
#define VDP2_STOREW(base,offset,data) \
    T1WriteWord(Vdp2Ram, ((base) & 0x7FFFF) + (offset), (data))
#define VDP2_STOREL(base,offset,data) \
    T1WriteLong(Vdp2Ram, ((base) & 0x7FFFF) + (offset), (data))

/* Use these macros instead of [BW]SWAP{16,32} when loading from or
 * storing to work RAM or VDP memory, to avoid breaking things on
 * big-endian systems.  RAM_VDP_SWAPL is for copying longwords directly
 * between work RAM and VDP memory, and is one operation faster on
 * little-endian machines than VDP_SWAPL(RAM_SWAPL(x)). */

#ifdef WORDS_BIGENDIAN
# define RAM_SWAPL(x)      (x)
# define VDP_SWAPW(x)      (x)
# define VDP_SWAPL(x)      (x)
# define RAM_VDP_SWAPL(x)  (x)
#else
# define RAM_SWAPL(x)      WSWAP32(x)
# define VDP_SWAPW(x)      BSWAP16(x)
# define VDP_SWAPL(x)      BSWAP32(x)
# define RAM_VDP_SWAPL(x)  BSWAP16(x)  // BSWAP32(WSWAP32(x)) == BSWAP16(x)
#endif

/*************************************************************************/
/********************** Optimization routine table ***********************/
/*************************************************************************/

#ifdef ENABLE_JIT  // Through end of file

/*************************************************************************/

/* List of detection and translation routines for special-case blocks */

#ifdef PSP
# define ALIGN_OPTIMIZER  __attribute__((aligned(64)))
#else
# define ALIGN_OPTIMIZER  /*nothing*/
#endif

#define DECLARE_OPTIMIZER(name)                                 \
    ALIGN_OPTIMIZER static FASTCALL void name(SH2State *state)

#define DECLARE_OPTIMIZER_WITH_DETECT(name)                     \
    static int name##_detect(SH2State *state, uint32_t address, \
                                    const uint16_t *fetch);     \
    DECLARE_OPTIMIZER(name)

/*----------------------------------*/

DECLARE_OPTIMIZER_WITH_DETECT(BIOS_000025AC);
DECLARE_OPTIMIZER_WITH_DETECT(BIOS_00002EFA);
DECLARE_OPTIMIZER            (BIOS_00003BC6);
DECLARE_OPTIMIZER            (BIOS_06001670);
DECLARE_OPTIMIZER_WITH_DETECT(BIOS_06010D22);
DECLARE_OPTIMIZER            (BIOS_060115A4);
DECLARE_OPTIMIZER_WITH_DETECT(BIOS_060115B6);
DECLARE_OPTIMIZER_WITH_DETECT(BIOS_060115D4);
DECLARE_OPTIMIZER            (BIOS_0602E364);
DECLARE_OPTIMIZER_WITH_DETECT(BIOS_0602E630);

/*----------------------------------*/

DECLARE_OPTIMIZER_WITH_DETECT(Azel_0600614C);
DECLARE_OPTIMIZER            (Azel_060061F0);
DECLARE_OPTIMIZER            (Azel_0600C4DC);
DECLARE_OPTIMIZER            (Azel_0600C59C);
DECLARE_OPTIMIZER_WITH_DETECT(Azel_0600C5B4);
DECLARE_OPTIMIZER            (Azel_0600C5F8);
DECLARE_OPTIMIZER            (Azel_0600C690);
DECLARE_OPTIMIZER_WITH_DETECT(Azel_06010F24);
DECLARE_OPTIMIZER            (Azel_06014274);
DECLARE_OPTIMIZER            (Azel_0601E330);
DECLARE_OPTIMIZER            (Azel_0601E910);
DECLARE_OPTIMIZER            (Azel_0601E95A);
DECLARE_OPTIMIZER            (Azel_0601EC20);
DECLARE_OPTIMIZER            (Azel_0601EC3C);
DECLARE_OPTIMIZER            (Azel_0601EE60);
DECLARE_OPTIMIZER            (Azel_0601EEE8);
DECLARE_OPTIMIZER            (Azel_0601F240);
DECLARE_OPTIMIZER            (Azel_0601F24C);
DECLARE_OPTIMIZER            (Azel_0601F2D6);
DECLARE_OPTIMIZER            (Azel_0601F2F2);
DECLARE_OPTIMIZER            (Azel_0601F30E);
DECLARE_OPTIMIZER            (Azel_0601F3F4);
DECLARE_OPTIMIZER            (Azel_0601FB70);
DECLARE_OPTIMIZER            (Azel_06022E18);
DECLARE_OPTIMIZER            (Azel_06035530);
DECLARE_OPTIMIZER            (Azel_06035552);
DECLARE_OPTIMIZER            (Azel_0603556C);
DECLARE_OPTIMIZER            (Azel_06035A8C);
DECLARE_OPTIMIZER            (Azel_06035A9C);
DECLARE_OPTIMIZER            (Azel_06035AA0);
DECLARE_OPTIMIZER            (Azel_06035B14);
DECLARE_OPTIMIZER            (Azel_06035C18);
DECLARE_OPTIMIZER            (Azel_06035C3C);
DECLARE_OPTIMIZER            (Azel_06035C60);
DECLARE_OPTIMIZER            (Azel_06035C84);
DECLARE_OPTIMIZER            (Azel_06035C90);
DECLARE_OPTIMIZER            (Azel_06035C96);
DECLARE_OPTIMIZER            (Azel_06035D24);
DECLARE_OPTIMIZER            (Azel_06035D30);
DECLARE_OPTIMIZER            (Azel_06035D36);
DECLARE_OPTIMIZER            (Azel_06035DD4);
DECLARE_OPTIMIZER            (Azel_06035DE0);
DECLARE_OPTIMIZER            (Azel_06035DE6);
DECLARE_OPTIMIZER            (Azel_06035E70);
DECLARE_OPTIMIZER            (Azel_06035EA0);
DECLARE_OPTIMIZER            (Azel_06035ED0);
DECLARE_OPTIMIZER            (Azel_06035F00);
DECLARE_OPTIMIZER            (Azel_06035F04);
DECLARE_OPTIMIZER            (Azel_060360F0);
DECLARE_OPTIMIZER_WITH_DETECT(Azel_0603A22C);
DECLARE_OPTIMIZER            (Azel_0603A242);
DECLARE_OPTIMIZER            (Azel_0603ABE0);
DECLARE_OPTIMIZER            (Azel_0603DD6E);

/*----------------------------------*/

static const struct {

    /* Start address applicable to this translation. */
    uint32_t address;

    /* Routine that implements the SH-2 code.  If NULL, no optimized
     * translation is generated; instead, the hints specified in the
     * .hint_* fields are applied to the current block. */
    FASTCALL void (*execute)(SH2State *state);

    /* Routine to detect whether to use this translation; returns the
     * number of 16-bit words processed (nonzero) to use it, else zero.
     * If NULL, the checksum is checked instead. */
    int (*detect)(SH2State *state, uint32_t address, const uint16_t *fetch);

    /* Checksum and block length (in instructions) if detect == NULL. */
    uint32_t sum;
    unsigned int length;

    /* Nonzero if this function is foldable (i.e. does not modify any
     * registers other than R0-R7, MACH/MACL, and PC).  If not set, the
     * function is never folded even if it is detected to be a candidate
     * for folding. */
    uint8_t foldable;

    /* Bitmask indicating which general purpose registers should be hinted
     * as having a constant value at block start. */
    uint16_t hint_constant_reg;

    /* Bitmask indicating which general purpose registers should be hinted
     * as containing a data pointer at block start. */
    uint16_t hint_data_pointer_reg;

    /* Bitmask indicating which of the first 32 instructions in the block
     * should be hinted as loading a data pointer. */
    uint32_t hint_data_pointer_load;

} hand_tuned_table[] = {

    /******** BIOS startup animation ********/

    {0x00001CFC, BIOS_000025AC, BIOS_000025AC_detect},  // 1.00 JP
    {0x000025AC, BIOS_000025AC, BIOS_000025AC_detect},  // 1.01 JP / UE
    {0x00002658, BIOS_00002EFA, BIOS_00002EFA_detect},  // 1.00 JP
    {0x00002EFA, BIOS_00002EFA, BIOS_00002EFA_detect},  // 1.01 JP / UE
    {0x00002D44, BIOS_00003BC6, .sum = 0x1A6ECE, .length = 70}, // 1.00 JP
    {0x00003BC6, BIOS_00003BC6, .sum = 0x1A40C9, .length = 71}, // 1.01 JP / UE
    {0x06001664, BIOS_06001670, .sum = 0x04A2D6, .length = 13}, // 1.00 JP
    {0x06001670, BIOS_06001670, .sum = 0x04A2D6, .length = 13}, // 1.01 JP
    {0x06001674, BIOS_06001670, .sum = 0x04A2D6, .length = 13}, // UE
    {0x06010D22, BIOS_06010D22, BIOS_06010D22_detect},  // JP
    {0x06010D36, BIOS_06010D22, BIOS_06010D22_detect},  // UE
    {0x060115C0, BIOS_060115A4, .sum = 0x032D22, .length =  9}, // 1.00 JP
    {0x060115A4, BIOS_060115A4, .sum = 0x032D22, .length =  9}, // 1.01 JP
    {0x06011654, BIOS_060115A4, .sum = 0x032D22, .length =  9}, // UE
    {0x060115D2, BIOS_060115B6, BIOS_060115B6_detect},  // 1.00 JP
    {0x060115B6, BIOS_060115B6, BIOS_060115B6_detect},  // 1.01 JP
    {0x06011666, BIOS_060115B6, BIOS_060115B6_detect},  // UE
    {0x060115F0, BIOS_060115D4, BIOS_060115D4_detect},  // 1.00 JP
    {0x060115D4, BIOS_060115D4, BIOS_060115D4_detect},  // 1.01 JP
    {0x06011684, BIOS_060115D4, BIOS_060115D4_detect},  // UE
    {0x0602DA80, NULL,          .sum = 0x0A6FD6, .length = 31,  // JP
         .hint_data_pointer_load = 1<<20},
    {0x06039A80, NULL,          .sum = 0x0A6FD6, .length = 31,  // UE
         .hint_data_pointer_load = 1<<20},
    {0x0602DABE, NULL,          .sum = 0x016AF5, .length =  3,  // JP
         .hint_data_pointer_reg = 1<<12},
    {0x06039ABE, NULL,          .sum = 0x016AF5, .length =  3,  // UE
         .hint_data_pointer_reg = 1<<12},
    {0x0602DAC4, NULL,          .sum = 0x016AF5, .length =  3,  // JP
         .hint_data_pointer_reg = 1<<12},
    {0x06039AC4, NULL,          .sum = 0x016AF5, .length =  3,  // UE
         .hint_data_pointer_reg = 1<<12},
    {0x0602DACA, NULL,          .sum = 0x016AF6, .length =  3,  // JP
         .hint_data_pointer_reg = 1<<12},
    {0x06039ACA, NULL,          .sum = 0x016AF6, .length =  3,  // UE
         .hint_data_pointer_reg = 1<<12},
    {0x0602DF10, NULL,          .sum = 0x04634B, .length = 13,  // 1.00 JP
         .hint_data_pointer_load = 1<<2},
    {0x0602DF30, NULL,          .sum = 0x04634B, .length = 13,  // 1.01 JP
         .hint_data_pointer_load = 1<<2},
    {0x06039F30, NULL,          .sum = 0x04634B, .length = 13,  // UE
         .hint_data_pointer_load = 1<<2},
    {0x0602E364, BIOS_0602E364, .sum = 0x074A3C, .length = 20,  // JP
         .foldable = 1},
    {0x0603A364, BIOS_0602E364, .sum = 0x074A3C, .length = 20,  // UE
         .foldable = 1},
    {0x0602E3B0, NULL,          .sum = 0x02AAF2, .length =  7,  // JP
         .hint_data_pointer_load = 1<<6},
    {0x0603A3B0, NULL,          .sum = 0x02AAF2, .length =  7,  // UE
         .hint_data_pointer_load = 1<<6},
    {0x0602E410, NULL,          .sum = 0x0298A3, .length =  5,  // JP
         .hint_data_pointer_load = 1<<4},
    {0x0603A410, NULL,          .sum = 0x0298A3, .length =  5,  // UE
         .hint_data_pointer_load = 1<<4},
    {0x0602E4B8, NULL,          .sum = 0x02984F, .length =  5,  // JP
         .hint_data_pointer_load = 1<<4},
    {0x0603A4B8, NULL,          .sum = 0x02984F, .length =  5,  // UE
         .hint_data_pointer_load = 1<<4},
    {0x0602E560, NULL,          .sum = 0x0297FB, .length =  5,  // JP
         .hint_data_pointer_load = 1<<4},
    {0x0603A560, NULL,          .sum = 0x0297FB, .length =  5,  // UE
         .hint_data_pointer_load = 1<<4},
    {0x0602E38C, NULL,          .sum = 0x04E94B, .length = 18,  // JP
         .hint_data_pointer_load = 1<<0 | 1<<7},
    {0x0603A38C, NULL,          .sum = 0x07294B, .length = 18,  // UE
         .hint_data_pointer_load = 1<<0 | 1<<7},
    {0x0602E630, BIOS_0602E630, BIOS_0602E630_detect},  // JP
    {0x0603A630, BIOS_0602E630, BIOS_0602E630_detect},  // UE

    /******** Azel: Panzer Dragoon RPG (JP) ********/

    {0x0600614C, Azel_0600614C, Azel_0600614C_detect},
    {0x060061F0, Azel_060061F0, .sum = 0x2FB438, .length = 167, .foldable = 1},
    {0x0600C4DC, Azel_0600C4DC, .sum = 0x155D7C, .length = 64, .foldable = 1},
    {0x0600C59C, Azel_0600C59C, .sum = 0x0C1C5C, .length = 30},
    {0x0600C5B4, Azel_0600C5B4, Azel_0600C5B4_detect},
    {0x0600C5F8, Azel_0600C5F8, .sum = 0x085791, .length = 22, .foldable = 1},
    {0x0600C690, Azel_0600C690, .sum = 0x05E193, .length = 15, .foldable = 1},
    {0x06010F24, Azel_06010F24, Azel_06010F24_detect},
    {0x06010F52, Azel_06010F24, Azel_06010F24_detect},
    {0x06014274, Azel_06014274, .sum = 0x23F1EA, .length = 140, .foldable = 1},
    {0x0601E330, Azel_0601E330, .sum = 0x1F9402, .length = 68},
    {0x0601E910, Azel_0601E910, .sum = 0x0EDD5D, .length = 37},
    {0x0601E95A, Azel_0601E95A, .sum = 0x0EDD2D, .length = 37},
    {0x0601EC20, Azel_0601EC20, .sum = 0x05F967, .length = 14},
    {0x0601EC3C, Azel_0601EC3C, .sum = 0x05F97D, .length = 14},
    {0x0601EE60, Azel_0601EE60, .sum = 0x0958CD, .length = 39},  // MOV R0,R1
    {0x0601EE60, Azel_0601EE60, .sum = 0x0998AD, .length = 39},  // BRA 601F02C
    {0x0601EEAE, NULL,          .sum = 0x05CD03, .length = 17,
         .hint_data_pointer_load = 1<<3},
    {0x0601EED6, NULL,          .sum = 0x01C56B, .length = 7,
         .hint_data_pointer_reg = 1<<0 | 1<<1},
    {0x0601EEE8, Azel_0601EEE8, .sum = 0x075BB7, .length = 20, .foldable = 1},
    {0x0601F120, NULL,          .sum = 0x0988A3, .length = 24,
         .hint_data_pointer_load = 1<<17},
    {0x0601F140, NULL,          .sum = 0x02D776, .length = 8,
         .hint_data_pointer_load = 1<<1},
    {0x0601F150, NULL,          .sum = 0x034F6E, .length = 7,
         .hint_data_pointer_load = 1<<0 | 1<<1 | 1<<2},
    {0x0601F15E, NULL,          .sum = 0x034F70, .length = 7,
         .hint_data_pointer_load = 1<<0 | 1<<1 | 1<<2},
    {0x0601F16C, NULL,          .sum = 0x029E99, .length = 7,
         .hint_data_pointer_load = 1<<0 | 1<<1 | 1<<2},
    {0x0601F190, NULL,          .sum = 0x01C56B, .length = 7,
         .hint_data_pointer_reg = 1<<0 | 1<<1},
    {0x0601F240, Azel_0601F240, .sum = 0x13322C, .length = 75},
    {0x0601F24C, Azel_0601F24C, .sum = 0x11E92D, .length = 69},
    {0x0601F2D6, Azel_0601F2D6, .sum = 0x05F6D3, .length = 14},
    {0x0601F2F2, Azel_0601F2F2, .sum = 0x05F6E9, .length = 14},
    {0x0601F30E, Azel_0601F30E, .sum = 0x250327, .length = 115},
    {0x0601F3F4, Azel_0601F3F4, .sum = 0x1A4DA5, .length = 84},
    {0x0601FB70, Azel_0601FB70, .sum = 0x095941, .length = 39},  // MOV R0,R1
    {0x0601FB70, Azel_0601FB70, .sum = 0x09995B, .length = 39},  // BRA 601FDB0
    {0x0601FBBE, NULL,          .sum = 0x049C2B, .length = 14,
         .hint_data_pointer_load = 1<<0},
    {0x0601FC3A, NULL,          .sum = 0x060C32, .length = 18,
         .hint_data_pointer_reg = 1<<4},
    {0x0601FC6C, Azel_0601EEE8, .sum = 0x075BB7, .length = 20, .foldable = 1},
    {0x0601FDB8, NULL,          .sum = 0x091C0C, .length = 55,
         .hint_data_pointer_reg = 1<<1},
    {0x0601FEA4, NULL,          .sum = 0x084DF0, .length = 20,
         .hint_data_pointer_load = 1<<17},
    {0x0601FEC4, NULL,          .sum = 0x01822F, .length = 4,
         .hint_data_pointer_load = 1<<1},
    {0x0601FECC, NULL,          .sum = 0x034F73, .length = 7,
         .hint_data_pointer_load = 1<<0 | 1<<1 | 1<<2},
    {0x0601FEDA, NULL,          .sum = 0x034F75, .length = 7,
         .hint_data_pointer_load = 1<<0 | 1<<1 | 1<<2},
    {0x0601FEE8, NULL,          .sum = 0x029E9A, .length = 7,
         .hint_data_pointer_load = 1<<0 | 1<<1 | 1<<2},
    {0x06021FA2, NULL,          .sum = 0x2A12AF, .length = 136,
         .hint_constant_reg = 1<<13},
    {0x06022E18, Azel_06022E18, .sum = 0x09ABCB, .length = 33, .foldable = 1},
    {0x06028EE8, NULL,          .sum = 0x1F09D1, .length = 103,
         .hint_constant_reg = 1<<10},
    {0x0602F834, NULL,          .sum = 0x0676B0, .length = 16,
         .hint_data_pointer_load = 1<<11},
    {0x06035530, Azel_06035530, .sum = 0x05ABFA, .length = 17, .foldable = 1},
    {0x06035552, Azel_06035552, .sum = 0x0358EB, .length = 13, .foldable = 1},
    {0x0603556C, Azel_0603556C, .sum = 0x0332E9, .length = 11, .foldable = 1},
    {0x06035A8C, Azel_06035A8C, .sum = 0x0A1ECF, .length = 36, .foldable = 1},
    {0x06035A9C, Azel_06035A9C, .sum = 0x065B51, .length = 28, .foldable = 1},
    {0x06035AA0, Azel_06035AA0, .sum = 0x052642, .length = 26, .foldable = 1},
    {0x06035B14, Azel_06035B14, .sum = 0x05CBE1, .length = 37, .foldable = 1},
    {0x06035C18, Azel_06035C18, .sum = 0x085E83, .length = 18, .foldable = 1},
    {0x06035C3C, Azel_06035C3C, .sum = 0x08595C, .length = 18, .foldable = 1},
    {0x06035C60, Azel_06035C60, .sum = 0x085DB6, .length = 18, .foldable = 1},
    {0x06035C84, Azel_06035C84, .sum = 0x0F24AE, .length = 80, .foldable = 1},
    {0x06035C90, Azel_06035C90, .sum = 0x0C95AD, .length = 74, .foldable = 1},
    {0x06035C96, Azel_06035C96, .sum = 0x0B1711, .length = 71, .foldable = 1},
    {0x06035D24, Azel_06035D24, .sum = 0x12CFBE, .length = 88, .foldable = 1},
    {0x06035D30, Azel_06035D30, .sum = 0x1040BD, .length = 82, .foldable = 1},
    {0x06035D36, Azel_06035D36, .sum = 0x0EC21D, .length = 79, .foldable = 1},
    {0x06035DD4, Azel_06035DD4, .sum = 0x0EAFA6, .length = 78, .foldable = 1},
    {0x06035DE0, Azel_06035DE0, .sum = 0x0C20A5, .length = 72, .foldable = 1},
    {0x06035DE6, Azel_06035DE6, .sum = 0x0AA20A, .length = 69, .foldable = 1},
    {0x06035E70, Azel_06035E70, .sum = 0x042E91, .length = 24, .foldable = 1},
    {0x06035EA0, Azel_06035EA0, .sum = 0x042E97, .length = 24, .foldable = 1},
    {0x06035ED0, Azel_06035ED0, .sum = 0x042E9D, .length = 24, .foldable = 1},
    {0x06035F00, Azel_06035F00, .sum = 0x04AC41, .length = 35, .foldable = 1},
    {0x06035F04, Azel_06035F04, .sum = 0x036FCE, .length = 33, .foldable = 1},
    {0x060360F0, Azel_060360F0, .sum = 0x00D021, .length =  4, .foldable = 1},
    {0x0603A22C, Azel_0603A22C, Azel_0603A22C_detect},
    {0x0603A242, Azel_0603A242, .sum = 0x11703E, .length = 49},
    {0x0603ABE0, Azel_0603ABE0, .sum = 0x0BABA4, .length = 35},
    {0x0603DD6E, Azel_0603DD6E, .sum = 0x0BB96E, .length = 31},
    {0x0605444C, NULL,          .sum = 0x102906, .length = 56,
         .hint_constant_reg = 1<<12},
    {0x06057450, NULL,          .sum = 0x0D4F8B, .length = 37,
         .hint_constant_reg = 1<<12},
    {0x06058910, NULL,          .sum = 0x0F2F81, .length = 52,
         .hint_constant_reg = 1<<12},
    {0x06059068, NULL,          .sum = 0x42DE2F, .length = 196,
         .hint_constant_reg = 1<<11},
    {0x0605906E, NULL,          .sum = 0x40F2FD, .length = 193,
         .hint_constant_reg = 1<<11},
    {0x060693AE, NULL,          .sum = 0x452D32, .length = 237,
         .hint_constant_reg = 1<<14},
    {0x0606B7E4, NULL,          .sum = 0x5AFDCE, .length = 292,
         .hint_constant_reg = 1<<10},
    {0x0606B898, NULL,          .sum = 0x2FFF9D, .length = 155,
         .hint_constant_reg = 1<<10 | 1<<12},
    {0x06080280, NULL,          .sum = 0x22D6B8, .length = 134,
         .hint_constant_reg = 1<<13},
    {0x0608DE80, NULL,          .sum = 0x114129, .length = 53,
         .hint_constant_reg = 1<<13},
    {0x060A03FE, NULL,          .sum = 0x4D786E, .length = 246,
         .hint_constant_reg = 1<<10 | 1<<14},

};

/*************************************************************************/

#endif  // ENABLE_JIT

/*************************************************************************/
/************************** Interface function ***************************/
/*************************************************************************/

/**
 * saturn_optimize_sh2:  Search for and return, if available, a native
 * implementation of the SH-2 routine starting at the given address.
 * If for_fold is nonzero, this function returns nonzero and NULL in
 * *func_ret to indicate that the routine at the given address should not
 * be folded.
 *
 * [Parameters]
 *        state: Processor state block pointer
 *      address: Address from which to translate
 *        fetch: Pointer corresponding to "address" from which opcodes can
 *                  be fetched
 *     func_ret: Pointer to variable to receive address of native function
 *                  implementing this routine if return value is nonzero
 *     for_fold: Nonzero if the callback is being called to look up a
 *                  subroutine for folding, zero if being called for a
 *                  full block translation
 * [Return value]
 *     Length of translated block in instructions (nonzero) if a native
 *     function is returned, else zero
 */
unsigned int saturn_optimize_sh2(SH2State *state, uint32_t address,
                                 const uint16_t *fetch,
                                 SH2NativeFunctionPointer *func_ret,
                                 int for_fold)
{
#ifdef ENABLE_JIT

    unsigned int i;
    for (i = 0; i < lenof(hand_tuned_table); i++) {

        if (hand_tuned_table[i].address != address) {
            continue;
        }

        unsigned int num_insns;
        if (hand_tuned_table[i].detect) {
            num_insns = hand_tuned_table[i].detect(state, address, fetch);
            if (!num_insns) {
                continue;
            }
        } else {
            num_insns = hand_tuned_table[i].length;
            const uint32_t sum = checksum_fast16(fetch, num_insns);
            if (sum != hand_tuned_table[i].sum) {
                continue;
            }
        }

        if (hand_tuned_table[i].execute == NULL) {

            if (for_fold) {
                /* Tell the caller not to fold this function, because we
                 * want to apply our optimization hints (which we can't do
                 * while folding). */
                *func_ret = NULL;
                return 1;
            }

            uint32_t bitmask;
            unsigned int bit;

            for (bitmask = hand_tuned_table[i].hint_constant_reg, bit = 0;
                 bitmask != 0;
                 bitmask &= ~(1<<bit), bit++
            ) {
                if (bitmask & (1<<bit)) {
                    sh2_optimize_hint_constant_register(state, bit);
                }
            }

            for (bitmask = hand_tuned_table[i].hint_data_pointer_reg, bit = 0;
                 bitmask != 0;
                 bitmask &= ~(1<<bit), bit++
            ) {
                if (bitmask & (1<<bit)) {
                    sh2_optimize_hint_data_pointer_register(state, bit);
                }
            }

            for (bitmask = hand_tuned_table[i].hint_data_pointer_load, bit = 0;
                 bitmask != 0;
                 bitmask &= ~(1<<bit), bit++
            ) {
                if (bitmask & (1<<bit)) {
                    sh2_optimize_hint_data_pointer_load(state, address+bit*2);
                }
            }

            return 0;

        } else {  // hand_tuned_table[i].execute != NULL

            if (for_fold && !hand_tuned_table[i].foldable) {
                /* This isn't a foldable optimizer, but it might be
                 * foldable if translated normally, so let the scanner
                 * have a pass at it. */
                return 0;
            }

#ifdef JIT_DEBUG_TRACE
            unsigned int n;
            for (n = 0; n < num_insns; n++) {
                char tracebuf[100];
                SH2Disasm(address + n*2, fetch[n], 0, tracebuf);
                fprintf(stderr, "%08X: %04X  %s\n",
                        address + n*2, fetch[n], tracebuf+12);
            }
#endif
            *func_ret = hand_tuned_table[i].execute;
            return num_insns;

        }

    }  // for (i = 0; i < lenof(hand_tuned_table); i++)

#endif  // ENABLE_JIT

    return 0;  // No match found
}

/*************************************************************************/
/***************** Case-specific optimization functions ******************/
/*************************************************************************/

#ifdef ENABLE_JIT  // Through end of file

/*************************************************************************/

/**** Saturn BIOS optimizations ****/

/*-----------------------------------------------------------------------*/

/* 0x25AC: Peripheral detection(?) loop */

static int BIOS_000025AC_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return fetch[-2] == 0x4C0B  // jsr @r12
        && fetch[-1] == 0x0009  // nop
        && fetch[ 0] == 0x2008  // tst r0, r0
        && fetch[ 1] == 0x8901  // bt fetch[4] --> bf fetch[12]
        && fetch[ 2] == 0xA008  // bra fetch[12] --> nop
        && fetch[ 3] == 0x0009  // nop
        && fetch[ 4] == 0x62D3  // mov r13, r2
        && fetch[ 5] == 0x32E7  // cmp/gt r14, r2
        && fetch[ 6] == 0x8F02  // bf/s fetch[10] --> bf/s fetch[-2]
        && fetch[ 7] == 0x7D01  // add #1, r13
        && fetch[ 8] == 0xA008  // bra fetch[18]
        && fetch[ 9] == 0xE000  // mov #0, r0
        && fetch[10] == 0xAFF2  // bra fetch[-2]
        && fetch[11] == 0x0009  // nop
        ? 12 : 0;
}

static FASTCALL void BIOS_000025AC(SH2State *state)
{
    if (UNLIKELY(state->R[0])) {
        state->SR &= ~SR_T;
        state->PC += 2*12;
        state->cycles += 5;
        return;
    }

    state->R[2] = state->R[13];
    state->SR &= ~SR_T;
    state->SR |= (state->R[13] > state->R[14]) << SR_T_SHIFT;
    if (UNLIKELY(state->R[13]++ > state->R[14])) {
        state->R[0] = 0;
        state->PC += 2*18;
        state->cycles += 11;
        return;
    }

    if (LIKELY(state->R[13]+15 <= state->R[14])) {
        state->R[13] += 15;
        state->cycles += 15*76 + 15;
    } else {
        state->cycles += 15;
    }
    state->PR = state->PC;
    state->PC = state->R[12];
}

/*-----------------------------------------------------------------------*/

/* 0x2EFA: CD read loop */

static int BIOS_00002EFA_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return fetch[0] == 0x6503  // mov r0, r5
        && fetch[1] == 0x3CB3  // cmp/ge r11, r12
        && fetch[2] == 0x8D06  // bt/s fetch[10]
        && fetch[3] == 0x64C3  // mov r12, r4
        // fetch[4] == 0x7401  // add #1, r4      -- these two are swapped
        // fetch[5] == 0x6352  // mov.l @r5, r3   -- in some BIOS versions
        && fetch[6] == 0x2E32  // mov.l r3, @r14
        && fetch[7] == 0x34B3  // cmp/ge r11, r4
        && fetch[8] == 0x8FFA  // bf/s fetch[4]
        && fetch[9] == 0x7E04  // add #4, r14
        ? 10 : 0;
}

static FASTCALL void BIOS_00002EFA(SH2State *state)
{
    uint8_t *page_base;

    if (UNLIKELY(state->R[0] != 0x25818000)
     || UNLIKELY(!(page_base = direct_pages[state->R[14]>>19]))
    ) {
        state->R[5] = state->R[0];
        state->PC += 2;
        state->cycles += 1;
        return;
    }

    state->R[5] = state->R[0];
    state->SR |= SR_T;  // Always ends up set from here down

    int32_t count = state->R[11];
    int32_t i = state->R[12];
    int32_t left = count - i;
    if (UNLIKELY(left <= 0)) {
        state->R[4] = i;
        state->PC += 2*10;
        state->cycles += 5;
        return;
    }

    uint8_t *ptr = page_base + state->R[14];
    state->R[4] = count;
    state->R[14] += left*4;
    state->PC += 2*10;
    state->cycles += 7*left + (4-1);

    /* Call the copy routine last to avoid unnecessary register saves and
     * restores. */
    Cs2RapidCopyT2(ptr, left);
}

/*-----------------------------------------------------------------------*/

/* 0x3BC6: CD status read routine */

static FASTCALL void BIOS_00003BC6(SH2State *state)
{
    /* With the current CS2 implementation, this all amounts to a simple
     * read of registers CR1-CR4, but let's not depend on that behavior. */

    state->R[0] = -3;  // Default return value (error)

    unsigned int try;
    for (try = 0; try < 100; try++, state->cycles += 67) {
        const unsigned int CR1 = Cs2ReadWord(0x90018);
        const unsigned int CR2 = Cs2ReadWord(0x9001C);
        const unsigned int CR3 = Cs2ReadWord(0x90020);
        const unsigned int CR4 = Cs2ReadWord(0x90024);
        HRAM_STOREW(state->R[4], 0, CR1);
        HRAM_STOREW(state->R[4], 2, CR2);
        HRAM_STOREW(state->R[4], 4, CR3);
        HRAM_STOREW(state->R[4], 6, CR4);

        const unsigned int CR1_test = Cs2ReadWord(0x90018);
        const unsigned int CR2_test = Cs2ReadWord(0x9001C);
        const unsigned int CR3_test = Cs2ReadWord(0x90020);
        const unsigned int CR4_test = Cs2ReadWord(0x90024);
        if (CR1_test==CR1 && CR2_test==CR2 && CR3_test==CR3 && CR4_test==CR4) {
            state->R[0] = 0;
            state->cycles += 65;
            break;
        }
    }

    state->PC = state->PR;
    state->cycles += 15 + 12;
}

/*-----------------------------------------------------------------------*/

/* 0x6001670: Sound RAM load loop (optimized to avoid slowdowns from ME
 * cache issues, since it includes a read-back test) */

static FASTCALL void BIOS_06001670(SH2State *state)
{
    const uint32_t data = MappedMemoryReadLong(state->R[1]);
    state->R[1] += 4;
    MappedMemoryWriteLong(state->R[3], data);
    state->R[3] += 4;
    state->R[6]--;
    if (state->R[6] != 0) {
        state->cycles += 50;
    } else {
        state->PC = state->PR;
        state->cycles += 52;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x6010D22: 3-D intro animation idle loop */

static int BIOS_06010D22_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return (fetch[-2] & 0xF000) == 0xB000  // bsr 0x60101AC  [only wastes time]
        && fetch[-1] == 0xE41E  // mov #30, r4
        && fetch[ 0] == 0xD011  // mov.l @(0x6010D68,pc), r0
        && fetch[ 1] == 0x6001  // mov.w @r0, r0
        && fetch[ 2] == 0x2008  // tst r0, r0
        && fetch[ 3] == 0x8BF9  // bf 0x6010D1E
        ? 4 : 0;
}

static FASTCALL void BIOS_06010D22(SH2State *state)
{
    const uint32_t address = HRAM_LOADL(state->PC & -4, 4 + 0x11*4);
    state->R[0] = HRAM_LOADW(address, 0);
    if (state->R[0]) {
        state->SR &= ~SR_T;
        state->cycles = state->cycle_limit;
    } else {
        state->SR |= SR_T;
        state->PC += 8;
        state->cycles += 4;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x60115A4, etc.: Routine with multiple JSRs and a looping recursive BSR
 * (this helps cover the block switch overhead) */

static FASTCALL void BIOS_060115A4(SH2State *state)
{
    state->R[15] -= 12;
    HRAM_STOREL(state->R[15], 8, state->R[14]);
    HRAM_STOREL(state->R[15], 4, state->R[13]);
    HRAM_STOREL(state->R[15], 0, state->PR);
    state->R[14] = state->R[4];
    state->PR = state->PC + 9*2;

    state->PC = HRAM_LOADL((state->PC + 3*2) & -4, 4 + 0x24*4);
    BIOS_0602E364(state);

    state->PC = HRAM_LOADL(state->R[14], 16);
    state->cycles += 11;
}

/*----------------------------------*/

static int BIOS_060115B6_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    const uint32_t jsr_address = HRAM_LOADL(state->PC & -4, 4 + 0x22*4);
    return checksum_fast16(fetch, 5) == 0x3081A
        && BIOS_0602E630_detect(state, jsr_address,
                                fetch + ((jsr_address - address) >> 1))
        ? 5 : 0;
}

static FASTCALL void BIOS_060115B6(SH2State *state)
{
    state->R[4] = state->R[14];
    state->R[13] = 0;
    state->PR = state->PC + 15*2;
    state->PC = HRAM_LOADL(state->PC & -4, 4 + 0x22*4);
    state->cycles += 7;
    return BIOS_0602E630(state);
}

/*----------------------------------*/

static int BIOS_060115D4_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return checksum_fast16(fetch-10, 21) == 0x8E03C ? 11 : 0;
}

static FASTCALL void BIOS_060115D4(SH2State *state)
{
    const uint32_t R14_68_address = HRAM_LOADL(state->R[14], 68);
    if (state->R[13] < HRAM_LOADL(R14_68_address, 0)) {
        state->R[4] = HRAM_LOADL(R14_68_address, 4 + 4*state->R[13]);
        state->R[13]++;
        state->PC -= 24*2;  // Recursive call to 0x60115A4 (PR is already set)
        state->cycles += 19;
    } else {
        state->PR = HRAM_LOADL(state->R[15], 0);
        state->R[13] = HRAM_LOADL(state->R[15], 4);
        state->R[14] = HRAM_LOADL(state->R[15], 8);
        state->R[15] += 12;
        state->PC = HRAM_LOADL((state->PC + 8*2) & -4, 4 + 0x17*4);
        state->cycles += 12;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x602E364: Short but difficult-to-optimize data initialization routine */

static FASTCALL void BIOS_0602E364(SH2State *state)
{
    const uint32_t r2 = HRAM_LOADL(state->PC & -4, 0*2 + 4 + 0xF*4);
    const uint32_t r5 = HRAM_LOADL(state->PC & -4, 2*2 + 4 + 0xF*4);
    const uint32_t r0 = HRAM_LOADL(state->PC & -4, 4*2 + 4 + 0xF*4);
    const uint32_t r3 = HRAM_LOADW(r2, 0) + 1;
    HRAM_STOREW(r2, 0, r3);
    const uint32_t r6 = HRAM_LOADL(r0, 0);
    const uint32_t r5_new = r5 + r3*48;
    HRAM_STOREL(r0, 0, r5_new);

    /* Help out the optimizer by telling it we can load multiple values
     * at once. */
    const uint32_t *src = (const uint32_t *)HRAM_PTR(r6);
    uint32_t *dest = (uint32_t *)HRAM_PTR(r5_new);
    unsigned int i;
    for (i = 0; i < 12; i += 4) {
        const uint32_t a = src[i+0];
        const uint32_t b = src[i+1];
        const uint32_t c = src[i+2];
        const uint32_t d = src[i+3];
        dest[i+0] = a;
        dest[i+1] = b;
        dest[i+2] = c;
        dest[i+3] = d;
    }

    state->PC = state->PR;
    state->cycles += 87;
}

/*-----------------------------------------------------------------------*/

/* 0x602E630: Coordinate transformation */

static int BIOS_0602E630_is_UE;

static int BIOS_0602E630_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    if (address == 0x602E630 && checksum_fast16(fetch,612) == 0xA87EE4) {
        BIOS_0602E630_is_UE = 0;
        return 612;
    }
    if (address == 0x603A630 && checksum_fast16(fetch,600) == 0xA9F4CC) {
        BIOS_0602E630_is_UE = 1;
        return 600;
    }
    return 0;
}

static FASTCALL void BIOS_0602E630(SH2State *state)
{
    int32_t counter;

    int16_t * const R12_ptr =
        (int16_t *)HRAM_PTR(HRAM_LOADL((state->PC + 11*2) & -4, 4 + 0x7B*4));
    int32_t * const R13_ptr =
        (int32_t *)HRAM_PTR(HRAM_LOADL((state->PC + 12*2) & -4, 4 + 0x7B*4));

    const uint32_t M_R7 = HRAM_LOADL((state->PC + 24*2) & -4, 4 + 0x76*4);
    const int32_t * const M = (const int32_t *)HRAM_PTR(HRAM_LOADL(M_R7, 0));

    const uint32_t R5 = HRAM_LOADL(state->R[4], 56) + 4;
    counter = (int16_t)HRAM_LOADW(R5, -4);
    state->cycles += 30;

    if (counter > 0) {

        /* 0x602E66E */

        state->cycles += 19 + (counter * /*minimum of*/ 110);

#ifdef PSP
# define DO_MULT(dest_all,dest_z,index) \
    int32_t dest_all, dest_z;                                           \
    do {                                                                \
        int32_t temp_x, temp_y, temp_z;                                 \
        asm(".set push; .set noreorder\n"                               \
            "lw %[temp_z], %[idx]*16+8(%[M])\n"                         \
            "lw %[temp_x], %[idx]*16+0(%[M])\n"                         \
            "lw %[temp_y], %[idx]*16+4(%[M])\n"                         \
            "ror %[temp_z], %[temp_z], 16\n"                            \
            "mult %[temp_z], %[in_z]\n"                                 \
            "ror %[temp_x], %[temp_x], 16\n"                            \
            "ror %[temp_y], %[temp_y], 16\n"                            \
            "mfhi %[temp_z]\n"                                          \
            "mflo %[dst_z]\n"                                           \
            "madd %[temp_x], %[in_x]\n"                                 \
            "sll %[temp_z], %[temp_z], 16\n"                            \
            "srl %[dst_z], %[dst_z], 16\n"                              \
            "lw %[temp_x], %[idx]*16+12(%[M])\n"                        \
            "madd %[temp_y], %[in_y]\n"                                 \
            "or %[dst_z], %[dst_z], %[temp_z]\n"                        \
            "ror %[temp_z], %[temp_x], 16\n"                            \
            "mfhi %[temp_x]\n"                                          \
            "mflo %[temp_y]\n"                                          \
            "sll %[temp_x], %[temp_x], 16\n"                            \
            "srl %[temp_y], %[temp_y], 16\n"                            \
            "or %[dst_all], %[temp_x], %[temp_y]\n"                     \
            "addu %[dst_all], %[dst_all], %[temp_z]\n"                  \
            ".set pop"                                                  \
            : [dst_all] "=r" (dest_all), [dst_z] "=&r" (dest_z),        \
              [temp_x] "=&r" (temp_x), [temp_y] "=&r" (temp_y),         \
              [temp_z] "=&r" (temp_z)                                   \
            : [M] "r" (M), [idx] "i" (index), [in_x] "r" (in_x),        \
              [in_y] "r" (in_y), [in_z] "r" (in_z)                      \
            : "hi", "lo"                                                \
        );                                                              \
    } while (0)
#else  // !PSP
# define GET_M(i)  ((int64_t)(int32_t)RAM_SWAPL(M[(i)]))
# define DO_MULT(dest_all,dest_z,index) \
    const int32_t dest_z = ((int64_t)in_z * GET_M((index)*4+2)) >> 16;  \
    int32_t dest_all = (((int64_t)in_x * GET_M((index)*4+0)             \
                       + (int64_t)in_y * GET_M((index)*4+1)             \
                       + (int64_t)in_z * GET_M((index)*4+2)) >> 16)     \
                     + GET_M((index)*4+3);
#endif

        const uint32_t testflag = HRAM_LOADL(state->R[4], 20);
        const int32_t *in = (const int32_t *)HRAM_PTR(R5);
        int32_t *out = R13_ptr;
        int16_t *coord_out = R12_ptr;

        do {
            const int32_t in_x = RAM_SWAPL(in[0]);
            const int32_t in_y = RAM_SWAPL(in[1]);
            const int32_t in_z = RAM_SWAPL(in[2]);

            DO_MULT(out_z, zz, 2);
            if (out_z < 0 && testflag) {
                out_z += out_z >> 3;
            }
            out[ 2] = RAM_SWAPL(out_z);
            out[14] = RAM_SWAPL(out_z - (zz<<1));

            DO_MULT(out_x, zx, 0);
            out[ 0] = RAM_SWAPL(out_x);
            out[12] = RAM_SWAPL(out_x - (zx<<1));

            DO_MULT(out_y, zy, 1);
            out[ 1] = RAM_SWAPL(out_y);
            out[13] = RAM_SWAPL(out_y - (zy<<1));

            /* The result gets truncated to 16 bits here, so we don't need
             * to worry about the 32->24 bit precision loss with floats.
             * (There are only a few pixels out of place during the entire
             * animation as a result of rounding error.) */
            const float coord_mult = 192.0f / out_z;
            *coord_out++ = (int16_t)ifloorf(out_x * coord_mult);
            *coord_out++ = (int16_t)ifloorf(out_y * coord_mult);

            in += 3;
            out += 3;
            counter -= 2;
        } while (counter > 0);

#undef GET_M
#undef DO_MULT

    }  // if (counter > 0)

    /* 0x602E840 */

    /* Offset for second-half local data accesses */
    const int UE_PC_offset = (BIOS_0602E630_is_UE ? -12*2 : 0);

    const uint32_t R11 = HRAM_LOADL(state->R[4], 64);
    counter = (int16_t)HRAM_LOADW(R11, 0);
    state->cycles += 19;

    if (counter > 0) {

#ifdef PSP
# define DOT3_16(v,x,y,z)  __extension__({                              \
    int32_t __temp1, __temp2, __result;                                 \
    asm(".set push; .set noreorder\n"                                   \
        "lw %[temp1], 0(%[V])\n"                                        \
        "lw %[temp2], 4(%[V])\n"                                        \
        "ror %[temp1], %[temp1], 16\n"                                  \
        "mult %[temp1], %[X]\n"                                         \
        "lw %[temp1], 8(%[V])\n"                                        \
        "ror %[temp2], %[temp2], 16\n"                                  \
        "madd %[temp2], %[Y]\n"                                         \
        "ror %[temp1], %[temp1], 16\n"                                  \
        "madd %[temp1], %[Z]\n"                                         \
        "mflo %[result]\n"                                              \
        "mfhi %[temp1]\n"                                               \
        "sra %[result], %[result], 16\n"                                \
        "ins %[result], %[temp1], 16, 16\n"                             \
        ".set pop"                                                      \
        : [temp1] "=&r" (__temp1), [temp2] "=&r" (__temp2),             \
          [result] "=r" (__result)                                      \
        : [V] "r" (v), [X] "r" (x), [Y] "r" (y), [Z] "r" (z)            \
        : "hi", "lo"                                                    \
    );                                                                  \
    __result;                                                           \
})
# define DOT3_32(v,x,y,z)  __extension__({                              \
    int32_t __temp1, __temp2, __result;                                 \
    asm(".set push; .set noreorder\n"                                   \
        "lw %[temp1], 0(%[V])\n"                                        \
        "lw %[temp2], 4(%[V])\n"                                        \
        "ror %[temp1], %[temp1], 16\n"                                  \
        "mult %[temp1], %[X]\n"                                         \
        "lw %[temp1], 8(%[V])\n"                                        \
        "ror %[temp2], %[temp2], 16\n"                                  \
        "madd %[temp2], %[Y]\n"                                         \
        "ror %[temp1], %[temp1], 16\n"                                  \
        "madd %[temp1], %[Z]\n"                                         \
        "mfhi %[result]\n"                                              \
        ".set pop"                                                      \
        : [temp1] "=&r" (__temp1), [temp2] "=&r" (__temp2),             \
          [result] "=r" (__result)                                      \
        : [V] "r" (v), [X] "r" (x), [Y] "r" (y), [Z] "r" (z)            \
        : "hi", "lo"                                                    \
    );                                                                  \
    __result;                                                           \
})
#else  // !PSP
# define DOT3_16(v,x,y,z)                                               \
    (((int64_t)(int32_t)RAM_SWAPL((v)[0]) * (int64_t)(int32_t)(x)       \
    + (int64_t)(int32_t)RAM_SWAPL((v)[1]) * (int64_t)(int32_t)(y)       \
    + (int64_t)(int32_t)RAM_SWAPL((v)[2]) * (int64_t)(int32_t)(z)) >> 16)
# define DOT3_32(v,x,y,z)                                               \
    (((int64_t)(int32_t)RAM_SWAPL((v)[0]) * (int64_t)(int32_t)(x)       \
    + (int64_t)(int32_t)RAM_SWAPL((v)[1]) * (int64_t)(int32_t)(y)       \
    + (int64_t)(int32_t)RAM_SWAPL((v)[2]) * (int64_t)(int32_t)(z)) >> 32)
#endif

        state->cycles += 68 + 95*(counter-2);

        /* 0x602E850 */

        const int32_t *in = (const int32_t *)(HRAM_PTR(R11) + 28);
        const int32_t *coord_in = &R13_ptr[12];
        const uint32_t out_address =
            HRAM_LOADL((state->PC + 264*2 + UE_PC_offset) & -4, 4 + 0xA2*4);
        int32_t *out = (int32_t *)HRAM_PTR(out_address);
        int16_t *coord_out = &R12_ptr[8];

        const uint16_t *R6_ptr =
            (const uint16_t *)(HRAM_PTR(HRAM_LOADL(state->R[4], 60)) + 4);
        const uint32_t flag_address =
            HRAM_LOADL((state->PC + 348*2 + UE_PC_offset) & -4, 4 + 0x79*4);
        int16_t *flag = (int16_t *)HRAM_PTR(flag_address);

        {
            const int32_t M_2  = RAM_SWAPL(M[ 2]);
            const int32_t M_6  = RAM_SWAPL(M[ 6]);
            const int32_t M_10 = RAM_SWAPL(M[10]);

            out[0] = RAM_SWAPL(-M_2);
            out[1] = RAM_SWAPL(-M_6);
            out[2] = RAM_SWAPL(-M_10);
            const int32_t *in0_0 =
                (const int32_t *)((uintptr_t)R13_ptr + R6_ptr[3]);
            R6_ptr += 10;
            const int32_t test_0 = DOT3_32(in0_0, -M_2, -M_6, -M_10);
            *flag++ = (test_0 < 0);
            out += 3;
            counter--;

            out[0] = RAM_SWAPL(M_2);
            out[1] = RAM_SWAPL(M_6);
            out[2] = RAM_SWAPL(M_10);
            const int32_t *in0_1 =
                (const int32_t *)((uintptr_t)R13_ptr + R6_ptr[3]);
            R6_ptr += 10;
            const int32_t test_1 = DOT3_32(in0_1, M_2, M_6, M_10);
            *flag++ = (test_1 < 0);
            out += 3;
            counter--;
        }

        do {
            const int32_t in_x = RAM_SWAPL(in[0]);
            const int32_t in_y = RAM_SWAPL(in[1]);
            const int32_t in_z = RAM_SWAPL(in[2]);

            const int32_t out_x = DOT3_16(&M[0], in_x, in_y, in_z);
            const int32_t out_y = DOT3_16(&M[4], in_x, in_y, in_z);
            const int32_t out_z = DOT3_16(&M[8], in_x, in_y, in_z);

            out[0] = RAM_SWAPL(out_x);
            out[1] = RAM_SWAPL(out_y);
            out[2] = RAM_SWAPL(out_z);

            const float coord_mult = 192.0f / (int32_t)RAM_SWAPL(coord_in[2]);
            *coord_out++ =
                (int16_t)ifloorf((int32_t)RAM_SWAPL(coord_in[0]) * coord_mult);
            *coord_out++ =
                (int16_t)ifloorf((int32_t)RAM_SWAPL(coord_in[1]) * coord_mult);
            coord_in += 3;

            const int32_t *in0 =
                (const int32_t *)((uintptr_t)R13_ptr + R6_ptr[3]);
            R6_ptr += 10;
            const int32_t test = DOT3_32(in0, out_x, out_y, out_z);
            *flag++ = (test < 0);

            in += 3;
            out += 3;
            counter--;
        } while (counter > 0);

#undef DOT3_16
#undef DOT3_32

    }  // if (counter > 0)

    /* 0x602E914 */
    /* Note: At this point, all GPRs except R9, R12, R13, and R15 are dead */

    const int16_t *flag = (const int16_t *)HRAM_PTR(
        HRAM_LOADL((state->PC + 572*2 + UE_PC_offset) & -4, 4 + 0x0F*4));
    const int32_t *R1_ptr = (const int32_t *)HRAM_PTR(
        HRAM_LOADL((state->PC + 378*2 + UE_PC_offset) & -4, 4 + 0x6C*4));
    const uint16_t *R6_ptr = (const uint16_t *)HRAM_PTR(
        HRAM_LOADL(state->R[4], 60));
    const int32_t *R7_ptr = (const int32_t *)HRAM_PTR(
        HRAM_LOADL((state->PC + 371*2 + UE_PC_offset) & -4, 4 + 0x6D*4));
    const uint32_t R9 = HRAM_LOADL((state->PC + 9*2) & -4, 4 + 0x7A*4);
    uint16_t *R10_ptr = (uint16_t *)HRAM_PTR(
        HRAM_LOADL((state->PC + 370*2 + UE_PC_offset) & -4, 4 + 0x73*4));

    const int32_t limit = *R6_ptr;
    R6_ptr += 2;
    state->cycles += 13;

    for (counter = 0; counter < limit; counter++, R7_ptr += 3, R6_ptr += 10) {

        /* 0x602EAA8 */

        if (!flag[counter]) {
            state->cycles += 15;
            continue;
        }

        /* 0x602E924 */

#ifdef PSP
        int32_t R2;
        {
            int32_t temp1, temp2, temp3;
            asm(".set push; .set noreorder\n"
                "lw %[temp1], 0(%[R7_ptr])\n"
                "lw %[temp2], 0(%[R1_ptr])\n"
                "lw %[temp3], 4(%[R7_ptr])\n"
                "ror %[temp1], %[temp1], 16\n"
                "ror %[temp2], %[temp2], 16\n"
                "mult %[temp1], %[temp2]\n"
                "lw %[temp1], 4(%[R1_ptr])\n"
                "lw %[temp2], 8(%[R7_ptr])\n"
                "ror %[temp3], %[temp3], 16\n"
                "ror %[temp1], %[temp1], 16\n"
                "madd %[temp3], %[temp1]\n"
                "lw %[temp3], 8(%[R1_ptr])\n"
                "ror %[temp2], %[temp2], 16\n"
                "ror %[temp3], %[temp3], 16\n"
                "madd %[temp2], %[temp3]\n"
                "mflo %[temp1]\n"
                "mfhi %[temp2]\n"
                "sra %[temp1], %[temp1], 16\n"
                "addiu %[temp2], %[temp2], 1\n"
                "ins %[temp1], %[temp2], 16, 16\n"
                "sra %[temp1], %[temp1], 10\n"
                "max %[temp1], %[temp1], $zero\n"
                "min %[R2], %[temp1], %[cst_127]\n"
                ".set pop"
                : [R2] "=r" (R2), [temp1] "=&r" (temp1),
                  [temp2] "=&r" (temp2), [temp3] "=&r" (temp3)
                : [R7_ptr] "r" (R7_ptr), [R1_ptr] "r" (R1_ptr),
                  [cst_127] "r" (127)
                : "hi", "lo"
            );
        }
#else  // !PSP
        const int32_t mac =
           ((int64_t) (int32_t)RAM_SWAPL(R7_ptr[0]) * (int32_t)RAM_SWAPL(R1_ptr[0])
          + (int64_t) (int32_t)RAM_SWAPL(R7_ptr[1]) * (int32_t)RAM_SWAPL(R1_ptr[1])
          + (int64_t) (int32_t)RAM_SWAPL(R7_ptr[2]) * (int32_t)RAM_SWAPL(R1_ptr[2])
          ) >> 16;
        const int32_t R2_temp = (mac + 0x10000) >> 10;
        const int32_t R2 = R2_temp < 0 ? 0 : R2_temp > 127 ? 127 : R2_temp;
#endif
        const uint32_t R2_tableaddr = RAM_SWAPL(*(const uint32_t *)&R6_ptr[8]);
        const uint16_t *R2_table = (const uint16_t *)HRAM_PTR(R2_tableaddr);

        const uint32_t R9_address = HRAM_LOADL(R9, 0);
        HRAM_STOREL(R9, 0, R9_address + 48);
        HRAM_STOREW(R9_address, 0, *R6_ptr);
        HRAM_STOREW(R9_address, 42, R2_table[R2]);

        uint32_t * const R9_data32 = (uint32_t *)(HRAM_PTR(R9_address) + 4);
        int32_t R3;
        switch (R6_ptr[1] & 0xFF) {
          case 0x00:
            R9_data32[0] = *(uint32_t *)&R12_ptr[ 0];
            R9_data32[1] = *(uint32_t *)&R12_ptr[ 2];
            R9_data32[2] = *(uint32_t *)&R12_ptr[ 4];
            R9_data32[3] = *(uint32_t *)&R12_ptr[ 6];
            R3 = RAM_SWAPL(R13_ptr[5]) + RAM_SWAPL(R13_ptr[11]);
            break;

          case 0x30:
            R9_data32[0] = *(uint32_t *)&R12_ptr[ 8];
            R9_data32[1] = *(uint32_t *)&R12_ptr[14];
            R9_data32[2] = *(uint32_t *)&R12_ptr[12];
            R9_data32[3] = *(uint32_t *)&R12_ptr[10];
            R3 = RAM_SWAPL(R13_ptr[17]) + RAM_SWAPL(R13_ptr[23]);
            break;

          case 0x60:
            R9_data32[0] = *(uint32_t *)&R12_ptr[ 0];
            R9_data32[1] = *(uint32_t *)&R12_ptr[ 8];
            R9_data32[2] = *(uint32_t *)&R12_ptr[10];
            R9_data32[3] = *(uint32_t *)&R12_ptr[ 2];
            R3 = RAM_SWAPL(R13_ptr[5]) + RAM_SWAPL(R13_ptr[14]);
            break;

          case 0x90:
            R9_data32[0] = *(uint32_t *)&R12_ptr[ 4];
            R9_data32[1] = *(uint32_t *)&R12_ptr[12];
            R9_data32[2] = *(uint32_t *)&R12_ptr[14];
            R9_data32[3] = *(uint32_t *)&R12_ptr[ 6];
            R3 = RAM_SWAPL(R13_ptr[8]) + RAM_SWAPL(R13_ptr[23]);
            break;

          case 0xC0:
            R9_data32[0] = *(uint32_t *)&R12_ptr[ 2];
            R9_data32[1] = *(uint32_t *)&R12_ptr[10];
            R9_data32[2] = *(uint32_t *)&R12_ptr[12];
            R9_data32[3] = *(uint32_t *)&R12_ptr[ 4];
            R3 = RAM_SWAPL(R13_ptr[5]) + RAM_SWAPL(R13_ptr[20]);
            break;

          default:  // case 0xF0
            R9_data32[0] = *(uint32_t *)&R12_ptr[ 0];
            R9_data32[1] = *(uint32_t *)&R12_ptr[ 6];
            R9_data32[2] = *(uint32_t *)&R12_ptr[14];
            R9_data32[3] = *(uint32_t *)&R12_ptr[ 8];
            R3 = RAM_SWAPL(R13_ptr[11]) + RAM_SWAPL(R13_ptr[14]);
            break;
        }

        if (!BIOS_0602E630_is_UE && R3 < -0x30000 && (R6_ptr[1] & 0xFF00)) {
            R3 = -R3;
        }
        R3 >>= 1;
        uint32_t *R3_buffer = (uint32_t *)HRAM_PTR(
            HRAM_LOADL((state->PC + 558*2 + UE_PC_offset) & -4, 4 + 0x17*4));
        R3_buffer[(*R10_ptr)++] = RAM_SWAPL(R3);

        state->cycles += 39 + /*approximately*/ 54;
    }

    /* 0x602EAB8 */

    state->PC = state->PR;
    state->cycles += 10;
}

/*************************************************************************/

/**** Azel: Panzer Dragoon RPG (JP) optimizations ****/

/*-----------------------------------------------------------------------*/

/* Common color calculation logic used by several routines */

static uint32_t Azel_color_calc(const int16_t *local_ptr,
                                const int16_t *r4_ptr, const int16_t *r5_ptr,
                                int32_t r, int32_t g, int32_t b)
{
    int32_t dot = r4_ptr[0] * local_ptr[0]
                + r4_ptr[1] * local_ptr[1]
                + r4_ptr[2] * local_ptr[2];
    if (dot > 0) {
        dot >>= 16;
        b += dot * r5_ptr[0];
        g += dot * r5_ptr[1];
        r += dot * r5_ptr[2];
    }
    return (bound(b, 0, 0x1F00) & 0x1F00) << 2
         | (bound(g, 0, 0x1F00) & 0x1F00) >> 3
         | (bound(r, 0, 0x1F00)         ) >> 8;
}

/*-----------------------------------------------------------------------*/

/* 0x600614C: Idle loop with a JSR */

static int Azel_0600614C_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return fetch[-2] == 0x4B0B  // jsr @r11
        && fetch[-1] == 0x0009  // nop
        && fetch[ 0] == 0x60E2  // mov.l @r14, r0
        && fetch[ 1] == 0x2008  // tst r0, r0
        && fetch[ 2] == 0x8BFA  // bf fetch[-2]
        && state->R[14] == 0x604BC80
        ? 3 : 0;
}

static FASTCALL void Azel_0600614C(SH2State *state)
{
    /* Help out the optimizer by loading these early. */
    const int32_t cycle_limit = state->cycle_limit;
    const uint32_t test = *(uint32_t *)HRAM_PTR(0x604BC80);
    const uint32_t r3 = 0x604B68C;  // @(0x38,pc=0x600FCB0)
#if defined(__mips__) && !defined(WORDS_BIGENDIAN)
    /* Similar to the "volatile" in the 0x600C59C optimizer, we force this
     * to be loaded during the load delay slots of "test". */
    const uint32_t r3_HI = (r3 + 0x8000) & 0xF0000;
    uint32_t r3_test;
    asm("lbu %0, %1(%2)"
        : "=r" (r3_test)
        : "i" (((r3 & 0xFFFFF) ^ 1) - r3_HI), "r" (HighWram + r3_HI));
#else
    const uint32_t r3_test = HRAM_LOADB(r3, 0);
#endif

    /* For a comparison against zero, we don't need to swap bytes. */
    if (LIKELY(test)) {

        state->cycles = cycle_limit;

        /* 0x600FCB0 almost always returns straight to us, so we implement
         * the first part of it here. */
        if (UNLIKELY(r3_test != 0)) {
            state->PR = state->PC;
            state->PC = 0x600FCB0 + 4*2;
            state->R[3] = r3;
        }

    } else {

        state->PC += 2*3;
        state->cycles += 3;

    }
}

/*-----------------------------------------------------------------------*/

/* 0x60061F0: RBG0 parameter generation for sky/ground backgrounds.  We
 * also store the slope set here in a video tweak parameter, so the code
 * doesn't have to re-derive it from the coefficients (which may be mangled
 * by the "shimmering" effect added to water). */

static FASTCALL void Azel_060061F0(SH2State *state)
{
    int32_t r4 = state->R[4];
    int32_t r5 = state->R[5];
    int32_t delta = state->R[6];
    uint32_t counter = state->R[7];
    const uint32_t out_address = HRAM_LOADL(state->R[15], 0);
    int32_t *out = (int32_t *)HRAM_PTR(out_address);

    extern float psp_video_tweaks_Azel_RBG0_slope;
    extern float psp_video_tweaks_Azel_RBG0_first_recip;

    if (r4 < 0) {
        r4 = -r4;
        r5 = -r5;
        delta = -delta;
    }

    if (r4 != 0) {
        psp_video_tweaks_Azel_RBG0_slope = (float)-delta / (float)r4;
    }

    const float r4_scaled = r4 * 65536.0f;
    const int32_t r4_test = r4 >> 14;  // See below for why we use this.

    if (delta == 0) {
        /* No horizon scaling is taking place.  Note that r5 should be
         * checked against zero here (and in similar cases below), but
         * we're a bit more conservative in order to avoid FPU overflow
         * errors on conversion to integer; since only the low 24 bits of
         * the 16.16 fixed point result are significant, this isn't a
         * problem in practice--it will only come up in cases where the
         * horizon line almost exactly coincides with a screen line. */
        if (r5 > r4_test) {
            const int32_t quotient = ifloorf(r4_scaled / (float)r5);
            state->PC = state->PR;
            state->cycles += 72 + (counter * 5);
            for (; counter != 0; counter--, out++) {
                *out = RAM_SWAPL(quotient);
            }
        } else {
            state->PC = state->PR;
            state->cycles += 30 + (counter * 5);
            for (; counter != 0; counter--, out++) {
                *out = -1;
            }
        }
        return;
    }

    const int32_t r5_final = r5 - (delta * (counter-1));

    if (delta > 0 ? (r5 <= r4_test) : (r5_final <= r4_test)) {
        /* The entire background layer is outside the horizon area. */
        state->PC = state->PR;
        state->cycles += (delta > 0 ? 40 : 43) + (counter * 5);
        for (; counter != 0; counter--, out++) {
            *out = -1;
        }
        return;
    }

    if (delta > 0 && r5_final <= r4_test) {
        /* The bottom of the background layer is outside the horizon area. */
        const uint32_t partial_count =
            MIN(counter - ((r5 - r4_test) / delta) + 1, counter);
        state->cycles += 88 + (partial_count * 5);
        counter -= partial_count;
        int32_t *out_temp = out + counter;
        uint32_t i;
        for (i = partial_count; i != 0; i--, out_temp++) {
            *out_temp = -1;
        }
    } else if (delta < 0 && r5 <= r4_test) {
        /* The top of the background layer is outside the horizon area. */
        const uint32_t partial_count =
            MIN((r5 - r4_test) / delta + 1, counter);
        state->cycles += 86 + (partial_count * 5);
        r5 -= delta * partial_count;
        counter -= partial_count;
        uint32_t i;
        for (i = partial_count; i != 0; i--, out++) {
            *out = -1;
        }
    } else {
        /* The entire background layer is within the horizon area. */
        state->cycles += (delta > 0 ? 39 : 42);
    }

    state->PC = state->PR;
    state->cycles += 15 + (counter * 32) + (counter%2 ? 10 : 0);

    if (r4 != 0) {
        psp_video_tweaks_Azel_RBG0_first_recip = (float)r5 / (float)r4;
    }
    for (; counter != 0; counter--, out++) {
        *out = RAM_SWAPL(ifloorf(r4_scaled / (float)r5));
        r5 -= delta;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x600C4DC: Routine to register a function in the SSH2 call table.  This
 * could be optimized by the main code with a few hints if it had better
 * flow analysis. */

static FASTCALL void Azel_0600C4DC(SH2State *state)
{
    const uint32_t base = 0x604B604;
    const unsigned int index = HRAM_LOADW(base, 0);
    const unsigned int next_index = (index + 1) & 7;

    if (HRAM_LOADL(base, 4 + next_index*16 + 0) != 0) {
        MappedMemoryWriteByte(0xFFFFFE11, 0);
    }
    HRAM_STOREL(base, 4 + index*16 + 4,  state->R[4]);
    HRAM_STOREL(base, 4 + index*16 + 8,  state->R[5]);
    HRAM_STOREL(base, 4 + index*16 + 12, state->R[6]);
    HRAM_STOREL(base, 4 + index*16 + 0,  state->R[7]);
    if (index == HRAM_LOADW(base, 2)) {
        MappedMemoryWriteWord(0x21000000, 0);
    }
    HRAM_STOREW(base, 0, next_index);

    state->PC = state->PR;
    state->cycles += 47;  // Approximate
}

/*-----------------------------------------------------------------------*/

/* 0x600C59C, etc.: SSH2 main loop (idle + function call) */

static NOINLINE void Azel_0600C5A4(SH2State *state);
static ALWAYS_INLINE void Azel_0600C5C0(
    SH2State *state, const uint32_t *r13_ptr, int extra_cycles);

/*----------------------------------*/

static FASTCALL void Azel_0600C59C(SH2State *state)
{
    int32_t test = (int8_t)(((SH2_struct *)(state->userdata))->onchip.FTCSR);

    /* Again, help out the optimizer.  This "volatile" actually speeds
     * things up, because it forces the compiler to load this during the
     * load delay slots of "test", rather than delaying the load so far
     * that the subsequent store to state->cycles ends up stalling.
     * (Granted, this is properly an optimizer bug that ought to be fixed
     * in GCC.  But since GCC's scheduler is so complex it might as well
     * be random chaos, what else can we do?) */
    int32_t cycle_limit =
#ifdef __mips__
        *(volatile int32_t *)&
#endif
        state->cycle_limit;

    if (LIKELY(test >= 0)) {
        state->cycles = cycle_limit;
    } else {
        state->PR = 0x600C5B4;  // Make sure PR is set before 0x600C5C0.
        /* We deliberately chain to a separate NOINLINE routine here to
         * minimize register saves and restores in the common case above,
         * since even setting up and tearing down a stack frame adds a
         * significant percentage to the execution time of this function. */
        return Azel_0600C5A4(state);
    }
}

/*----------------------------------*/

static NOINLINE void Azel_0600C5A4(SH2State *state)
{
    MappedMemoryWriteByte(0xFFFFFE11, 0);
    const unsigned int index = HRAM_LOADW(state->R[13], 2);
    const uint32_t *r13_ptr = (const uint32_t *)HRAM_PTR(state->R[13]);
    return Azel_0600C5C0(state, &r13_ptr[1 + index*4], 7);
}

/*----------------------------------*/

static int Azel_0600C5B4_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return checksum_fast16(fetch-6, 24) == 0x93C29 ? 24 : 0;
}

static FASTCALL void Azel_0600C5B4(SH2State *state)
{
    const uint32_t r13 = state->R[13];
    uint32_t *r13_ptr = (uint32_t *)HRAM_PTR(r13);
    unsigned int index = HRAM_LOADW(r13, 2);
    r13_ptr[1 + index*4] = 0;
    index = (index + 1) & 7;
    HRAM_STOREW(r13, 2, index);
    MappedMemoryWriteWord(0x21800000, 0);
    return Azel_0600C5C0(state, &r13_ptr[1 + index*4], 6);
}

/*----------------------------------*/

static ALWAYS_INLINE void Azel_0600C5C0(
    SH2State *state, const uint32_t *r13_ptr, int extra_cycles)
{
    const uint32_t func_address = RAM_SWAPL(r13_ptr[0]);
    if (func_address) {
        state->R[4] = RAM_SWAPL(r13_ptr[1]);
        state->R[5] = RAM_SWAPL(r13_ptr[2]);
        state->R[6] = RAM_SWAPL(r13_ptr[3]);
        /* PR is known to be correct (0x600C5B4) here. */
        state->PC = func_address;
        state->cycles += extra_cycles + 19;
    } else {
        state->PC = 0x600C59C;
        state->cycles = state->cycle_limit;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x600C5F8, 0x600C690: Cache flush routines (no-ops in emulation) */

static FASTCALL void Azel_0600C5F8(SH2State *state)
{
    state->R[0] = 0x604AEA8 + HRAM_LOADW(0x604B606, 0) * 48;
    state->PC = state->PR;
    state->cycles += 48;
}

static FASTCALL void Azel_0600C690(SH2State *state)
{
    state->PC = state->PR;
    state->cycles += 11 + (state->R[5] / 16) * 5;  // Approximate
}

/*-----------------------------------------------------------------------*/

/* 0x6010F24/0x6010F52: Bitmap copy loop (for movies) */

static int Azel_06010F24_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return fetch[ 0] == 0x6693  // mov r9, r6
        && fetch[ 1] == 0x65D3  // mov r13, r5
        && fetch[ 2] == 0x4A0B  // jsr @r10  [DMA copy routine]
        && fetch[ 3] == 0x64E3  // mov r14, r4
        // fetch[ 4] == 0x53F1  // {mov.l @(4,r15), r3 [counter] | add r8, r14}
        // fetch[ 5] == 0x3E8C  // {add r8, r14 | mov.l @r15, r3 [counter]}
        && fetch[ 6] == 0x73FF  // add #-1, r3
        // fetch[ 7] == 0x2338  // {tst r3, r3 | mov.l r3, @r15}
        // fetch[ 8] == 0x1F31  // {mov.l r3, @(4,r15) | tst r3, r3}
        && fetch[ 9] == 0x8FF5  // bf/s fetch[0]
        && fetch[10] == 0x3DCC  // add r12, r13
        && state->R[13]>>20 == 0x25E
        ? 11 : 0;
}

static FASTCALL void Azel_06010F24(SH2State *state)
{
    const uint32_t src_addr = state->R[14];
    const uint32_t dest_addr = state->R[13];
    const uint32_t size = state->R[9];
    const uint32_t *src = (const uint32_t *)HRAM_PTR(src_addr);
    uint32_t *dest = (uint32_t *)VDP2_PTR(dest_addr);

    uint32_t i;
    for (i = 0; i < size; i += 16, src += 4, dest += 4) {
        const uint32_t word0 = src[0];
        const uint32_t word1 = src[1];
        const uint32_t word2 = src[2];
        const uint32_t word3 = src[3];
        dest[0] = RAM_VDP_SWAPL(word0);
        dest[1] = RAM_VDP_SWAPL(word1);
        dest[2] = RAM_VDP_SWAPL(word2);
        dest[3] = RAM_VDP_SWAPL(word3);
    }

    state->R[14] = src_addr + state->R[8];
    state->R[13] = dest_addr + state->R[12];

    /* Conveniently, the counter is always stored in R3 when we get here
     * (it's first loaded when the loop is executed on the fall-in case)
     * and the stack variables are never referenced once their respective
     * loops complete, so we don't have to worry about which loop we're in. */
    unsigned int counter = state->R[3];
    counter--;
    state->R[3] = counter;

    if (counter != 0) {
        state->SR &= ~SR_T;
        state->cycles += 290;
    } else {
        state->SR |= SR_T;
        state->PC += 2*11;
        state->cycles += 289;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x6014274: Calculation routine (possibly for the line scroll table?) */

static FASTCALL void Azel_06014274(SH2State *state)
{
    const int32_t * const sin_table = (const int32_t *)LRAM_PTR(0x20216660);

    int32_t *r4_ptr = (int32_t *)HRAM_PTR(state->R[4]);

    int32_t r9 = RAM_SWAPL(r4_ptr[6]);
    r4_ptr[6] = RAM_SWAPL(r9 + RAM_SWAPL(r4_ptr[1]));
    const int32_t r10 = RAM_SWAPL(r4_ptr[7]);
    r4_ptr[7] = RAM_SWAPL(r10 + RAM_SWAPL(r4_ptr[4]));
    const int32_t r11 = 176;
    const int32_t r5 = RAM_SWAPL(r4_ptr[2]);
    const int32_t r6 = RAM_SWAPL(r4_ptr[5]);
    const int32_t r7 = RAM_SWAPL(r4_ptr[3]);
    r4_ptr = (int32_t *)HRAM_PTR(RAM_SWAPL(r4_ptr[0]));

    unsigned int counter;
    for (counter = 224; counter != 0; counter--) {
        const int32_t r0 = ((int64_t)r7 * sin_table[r9>>16 & 0xFFF]) >> 16;
        /* We drop 2 bits of precision here so we can use a 32/32bit
         * integer divide instruction; it shouldn't make a difference in
         * the visible result. */
        const int32_t r12 = ((r7 + r0) >> 2) + 0x4000;
        const int32_t quotient = 0x40000000 / r12;
        r4_ptr[1] = RAM_SWAPL((RAM_SWAPL(r4_ptr[1]) + r6) & 0xFFFFFF);
        r4_ptr[2] = RAM_SWAPL(quotient);
        r4_ptr[0] = RAM_SWAPL((0x10000 - quotient) * r11 + r10);
        r4_ptr += 3;
        r9 += r5;
    }

    state->PC = state->PR;
    state->cycles += 86 + 222*48 + 58;
}

/*-----------------------------------------------------------------------*/

/* 0x601E330: Input capture check */

static FASTCALL void Azel_0601E330(SH2State *state)
{
    int32_t test = (int8_t)(((SH2_struct *)(state->userdata))->onchip.FTCSR);
    const uint32_t param = HRAM_LOADL(0x604AEA4, 0);
    if (LIKELY(test >= 0)) {
        state->R[5] = param;
        state->R[6] = 0x604FCFC;
        state->R[7] = 0x604BCB4;
        state->PC = 0x601F1E4;
        state->cycles += 21;
    } else {
        const uint32_t old_r4 = state->R[4];
        state->R[4] = param;

        /* 0x600C5D8 */
        const unsigned int index = HRAM_LOADW(0x604B604, 0);
        state->R[5] = 0x604AEA8 + index*48;
        state->PC = 0x6035AA0;
        Azel_06035AA0(state);

        /* 0x601E3A6 */
        state->R[4] = old_r4;
        state->R[5] = 0;
        state->R[6] = 0;
        state->R[7] = 0x601E314;
        state->PC = 0x600C4DC;
        state->cycles += 16 + 17 + 10;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x601E910, etc.: Various varieties of vertex manipulation routines and
 * their subfunctions */

static void Azel_0601E910_common(SH2State *state,
     int (*clipfunc)(SH2State *state, uint32_t r11, uint32_t r13));
static void Azel_0601E9A4(SH2State *state, const uint32_t color_data,
                          int swapflag);
static int Azel_0601EB10(const int16_t *vertex_data, uint32_t color_data,
                         int base_r, int base_g, int base_b);

static void Azel_0601EC20_common(SH2State *state,
     int (*clipfunc)(SH2State *state, uint32_t r11, uint32_t r13));
static void Azel_0601EC58(SH2State *state, int swapflag);
static int Azel_0601ED58(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b);
static int Azel_0601EDDC(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b);

static void Azel_0601F2D6_common(SH2State *state,
     int (*clipfunc)(SH2State *state, uint32_t r11, uint32_t r13));
static void Azel_0601F49C(SH2State *state, int swapflag);
static int Azel_0601FA68(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b);
static int Azel_0601FAEC(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b);

static int Azel_0601F58A(SH2State *state, uint32_t r11, uint32_t r13);
static int Azel_0601F5A6(SH2State *state,
                         const int16_t *r10_ptr, const int16_t *r11_ptr,
                         const int16_t *r12_ptr, const int16_t *r13_ptr);
static int Azel_0601F5D2(SH2State *state);

static int Azel_0601F5EE(SH2State *state, uint32_t r11, uint32_t r13);

static ALWAYS_INLINE int Azel_0601F762(
    SH2State *state, const uint32_t * const r10_ptr,
    const uint32_t * const r11_ptr, const uint32_t * const r12_ptr,
    const uint32_t * const r13_ptr, uint32_t * const r14_ptr,
    const uint32_t r4_0, const int swapflag);

static int32_t Azel_0601F824(const int16_t *r6_ptr, uint32_t *r10_ptr,
                             uint32_t *r11_ptr, uint32_t *r12_ptr,
                             uint32_t *r13_ptr, uint32_t mask,
                             int (*clipfunc)(uint32_t *r8_ptr, uint32_t *r9_ptr,
                                             const int16_t *r6_ptr));
static int Azel_0601F93A(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr);
static int Azel_0601F948(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr);
static ALWAYS_INLINE int Azel_0601F950(uint32_t *r8_ptr, uint32_t *r9_ptr,
                                       const int16_t *r6_ptr,
                                       const int32_t r8_x, const int32_t x_lim,
                                       int base_cycles);
static int Azel_0601F988(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr);
static int Azel_0601F996(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr);
static ALWAYS_INLINE int Azel_0601F99E(uint32_t *r8_ptr, uint32_t *r9_ptr,
                                       const int16_t *r6_ptr,
                                       const int32_t r8_y, const int32_t y_lim,
                                       int base_cycles);
static int Azel_0601F9D6(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr);

/*----------------------------------*/

static FASTCALL void Azel_0601E910(SH2State *state)
{
    return Azel_0601E910_common(state, Azel_0601F58A);
}

static FASTCALL void Azel_0601E95A(SH2State *state)
{
    return Azel_0601E910_common(state, Azel_0601F5EE);
}

static FASTCALL void Azel_0601EC20(SH2State *state)
{
    return Azel_0601EC20_common(state, Azel_0601F58A);
}

static FASTCALL void Azel_0601EC3C(SH2State *state)
{
    return Azel_0601EC20_common(state, Azel_0601F5EE);
}

static FASTCALL void Azel_0601F2D6(SH2State *state)
{
    return Azel_0601F2D6_common(state, Azel_0601F58A);
}

static FASTCALL void Azel_0601F2F2(SH2State *state)
{
    return Azel_0601F2D6_common(state, Azel_0601F5EE);
}

/*----------------------------------*/

static void Azel_0601E910_common(SH2State *state,
     int (*clipfunc)(SH2State *state, uint32_t r11, uint32_t r13))
{
    const uint32_t saved_PR = state->PR;

    uint32_t color_data = HRAM_LOADL(state->R[15], 4);
    uint32_t index = 0;
    uint32_t r11 = LRAM_LOADL(state->R[4], 0);
    uint32_t r13 = LRAM_LOADL(state->R[4], 4);
    state->R[4] += 8;
    while (r11 != r13) {
        const int swapflag = (*clipfunc)(state, r11, r13);
        const int clipped = (swapflag < 0);
        if (LRAM_LOADL(color_data, 0) == index) {
            if (clipped) {
                state->cycles += 25;
            } else {
                Azel_0601E9A4(state, color_data + 4, swapflag);
                state->cycles += 21;
            }
            color_data += 16;
        } else {
            if (clipped) {
                state->cycles += 19;
            } else {
                Azel_0601F49C(state, swapflag);
                state->cycles += 20;
            }
        }
        index++;
        r11 = LRAM_LOADL(state->R[4], 0);
        r13 = LRAM_LOADL(state->R[4], 4);
        state->R[4] += 8;
    }

    state->PC = saved_PR;
    state->cycles += 11;
}

static void Azel_0601E9A4(SH2State *state, const uint32_t color_data,
                          int swapflag)
{
    static const uint16_t cycles_used[4][4] =
        {{13,13,13,13},{312,319,321,319},{315,322,324,322},{309,317,319,317}};

    const int16_t *quad_data = (const int16_t *)LRAM_PTR(state->R[4]);
    const int quad_type = (quad_data[-6] >> 8) & 3;
    uint32_t *gbr_ptr = (uint32_t *)HRAM_PTR(state->GBR);

    state->cycles += cycles_used[quad_type][swapflag];

    gbr_ptr[0] = RAM_SWAPL(state->R[14] + 32);

    if (quad_type == 0) {
        return;
    }

    const uint32_t color_addr = RAM_SWAPL(gbr_ptr[4]);
    int16_t *out = (int16_t *)VDP1_PTR(color_addr);
    gbr_ptr[4] = RAM_SWAPL(color_addr + 8);
    VDP1_STOREW(state->R[14], 28, color_addr >> 3);

    const int16_t *base_ptr =
        (const int16_t *)(HRAM_PTR(0x601FCB0) + (state->R[1]>>7 & -8));
    const int base_r = base_ptr[0];
    const int base_g = base_ptr[1];
    const int base_b = base_ptr[2];
    int a, b, c, d;

    switch (quad_type) {
      case 1:
        a = Azel_0601EB10(quad_data, color_data+0, base_r, base_g, base_b);
        b = Azel_0601EB10(quad_data, color_data+3, base_r, base_g, base_b);
        c = Azel_0601EB10(quad_data, color_data+6, base_r, base_g, base_b);
        d = Azel_0601EB10(quad_data, color_data+9, base_r, base_g, base_b);
        state->R[4] += 8;
        break;

      case 2:
        a = Azel_0601EB10(quad_data+0, color_data+0, base_r, base_g, base_b);
        b = Azel_0601EB10(quad_data+6, color_data+3, base_r, base_g, base_b);
        c = Azel_0601EB10(quad_data+12,color_data+6, base_r, base_g, base_b);
        d = Azel_0601EB10(quad_data+18,color_data+9, base_r, base_g, base_b);
        state->R[4] += 48;
        break;

      default:  // case 3
        a = Azel_0601EB10(quad_data+0, color_data+0, base_r, base_g, base_b);
        b = Azel_0601EB10(quad_data+3, color_data+3, base_r, base_g, base_b);
        c = Azel_0601EB10(quad_data+6, color_data+6, base_r, base_g, base_b);
        d = Azel_0601EB10(quad_data+9, color_data+9, base_r, base_g, base_b);
        state->R[4] += 24;
        break;
    }  // switch (quad_type)

    out[0 ^ swapflag] = VDP_SWAPW(a);
    out[1 ^ swapflag] = VDP_SWAPW(b);
    out[2 ^ swapflag] = VDP_SWAPW(c);
    out[3 ^ swapflag] = VDP_SWAPW(d);
}

static int Azel_0601EB10(const int16_t *vertex_data, uint32_t color_data,
                         int base_r, int base_g, int base_b)
{
    return Azel_color_calc((const int16_t *)HRAM_PTR(0x601FC94),
                           vertex_data,
                           (const int16_t *)HRAM_PTR(0x601FCA8),
                           base_r + ((int8_t)LRAM_LOADB(color_data,2) << 8),
                           base_g + ((int8_t)LRAM_LOADB(color_data,1) << 8),
                           base_b + ((int8_t)LRAM_LOADB(color_data,0) << 8));
}

/*----------------------------------*/

static void Azel_0601EC20_common(SH2State *state,
     int (*clipfunc)(SH2State *state, uint32_t r11, uint32_t r13))
{
    const uint32_t saved_PR = state->PR;

    uint32_t r11 = LRAM_LOADL(state->R[4], 0);
    uint32_t r13 = LRAM_LOADL(state->R[4], 4);
    state->R[4] += 8;
    while (r11 != r13) {
        const int swapflag = (*clipfunc)(state, r11, r13);
        const int clipped = (swapflag < 0);
        if (clipped) {
            state->cycles += 12;
        } else {
            Azel_0601EC58(state, swapflag);
            state->cycles += 13;
        }
        r11 = LRAM_LOADL(state->R[4], 0);
        r13 = LRAM_LOADL(state->R[4], 4);
        state->R[4] += 8;
    }

    state->PC = saved_PR;
    state->cycles += 11;
}

static void Azel_0601EC58(SH2State *state, int swapflag)
{
    static const uint16_t cycles_used[4][4] =
        {{10,10,10,10}, {98,98,98,98}, {285,291,293,291}, {266,272,274,272}};

    const int16_t *quad_data = (const int16_t *)LRAM_PTR(state->R[4]);
    const int quad_type = (quad_data[-6] >> 8) & 3;
    uint32_t *gbr_ptr = (uint32_t *)HRAM_PTR(state->GBR);

    state->cycles += cycles_used[quad_type][swapflag];

    gbr_ptr[0] = RAM_SWAPL(state->R[14] - 32);

    if (quad_type == 0) {
        return;
    }

    const uint32_t color_addr = RAM_SWAPL(gbr_ptr[4]);
    int16_t *out = (int16_t *)VDP1_PTR(color_addr);
    gbr_ptr[4] = RAM_SWAPL(color_addr - 8);
    VDP1_STOREW(state->R[14], 28, color_addr >> 3);

    const int16_t *base_ptr =
        (const int16_t *)(HRAM_PTR(0x601EF2C) + (state->R[1]>>7 & -8));
    const int base_r = base_ptr[0];
    const int base_g = base_ptr[1];
    const int base_b = base_ptr[2];
    int a, b, c, d;

    switch (quad_type) {
      case 1:
        a = b = c = d = Azel_0601ED58(quad_data, base_r, base_g, base_b);
        state->R[4] += 8;
        break;

      case 2:
        a = Azel_0601EDDC(quad_data+ 0, base_r, base_g, base_b);
        b = Azel_0601EDDC(quad_data+ 6, base_r, base_g, base_b);
        c = Azel_0601EDDC(quad_data+12, base_r, base_g, base_b);
        d = Azel_0601EDDC(quad_data+18, base_r, base_g, base_b);
        state->R[4] += 48;
        break;

      default:  // case 3
        a = Azel_0601ED58(quad_data+0, base_r, base_g, base_b);
        b = Azel_0601ED58(quad_data+3, base_r, base_g, base_b);
        c = Azel_0601ED58(quad_data+6, base_r, base_g, base_b);
        d = Azel_0601ED58(quad_data+9, base_r, base_g, base_b);
        state->R[4] += 24;
        break;
    }  // switch (quad_type)

    out[0 ^ swapflag] = VDP_SWAPW(a);
    out[1 ^ swapflag] = VDP_SWAPW(b);
    out[2 ^ swapflag] = VDP_SWAPW(c);
    out[3 ^ swapflag] = VDP_SWAPW(d);
}

static int Azel_0601ED58(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b)
{
    return Azel_color_calc((const int16_t *)HRAM_PTR(0x601EF10),
                           vertex_data,
                           (const int16_t *)HRAM_PTR(0x601EF24),
                           base_r, base_g, base_b);
}

static int Azel_0601EDDC(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b)
{
    return Azel_color_calc((const int16_t *)HRAM_PTR(0x601EF10),
                           vertex_data,
                           (const int16_t *)HRAM_PTR(0x601EF24),
                           base_r + vertex_data[5],
                           base_g + vertex_data[4],
                           base_b + vertex_data[3]);
}

/*----------------------------------*/

static void Azel_0601F2D6_common(SH2State *state,
     int (*clipfunc)(SH2State *state, uint32_t r11, uint32_t r13))
{
    const uint32_t saved_PR = state->PR;

    uint32_t r11 = LRAM_LOADL(state->R[4], 0);
    uint32_t r13 = LRAM_LOADL(state->R[4], 4);
    state->R[4] += 8;
    while (r11 != r13) {
        const int swapflag = (*clipfunc)(state, r11, r13);
        const int clipped = (swapflag < 0);
        if (clipped) {
            state->cycles += 12;
        } else {
            Azel_0601F49C(state, swapflag);
            state->cycles += 13;
        }
        r11 = LRAM_LOADL(state->R[4], 0);
        r13 = LRAM_LOADL(state->R[4], 4);
        state->R[4] += 8;
    }

    state->PC = saved_PR;
    state->cycles += 11;
}

static void Azel_0601F49C(SH2State *state, int swapflag)
{
    static const uint16_t cycles_used[4][4] =
        {{10,10,10,10}, {98,98,98,98}, {285,291,293,291}, {266,272,274,272}};

    const int16_t *quad_data = (const int16_t *)LRAM_PTR(state->R[4]);
    const int quad_type = (quad_data[-6] >> 8) & 3;
    uint32_t *gbr_ptr = (uint32_t *)HRAM_PTR(state->GBR);

    state->cycles += cycles_used[quad_type][swapflag];

    gbr_ptr[0] = RAM_SWAPL(state->R[14] + 32);

    if (quad_type == 0) {
        return;
    }

    const uint32_t color_addr = RAM_SWAPL(gbr_ptr[4]);
    int16_t *out = (int16_t *)VDP1_PTR(color_addr);
    gbr_ptr[4] = RAM_SWAPL(color_addr + 8);
    VDP1_STOREW(state->R[14], 28, color_addr >> 3);

    const int16_t *base_ptr =
        (const int16_t *)(HRAM_PTR(0x601FCB0) + (state->R[1]>>7 & -8));
    const int base_r = base_ptr[0];
    const int base_g = base_ptr[1];
    const int base_b = base_ptr[2];
    int a, b, c, d;

    switch (quad_type) {
      case 1:
        a = b = c = d = Azel_0601FA68(quad_data, base_r, base_g, base_b);
        state->R[4] += 8;
        break;

      case 2:
        a = Azel_0601FAEC(quad_data+ 0, base_r, base_g, base_b);
        b = Azel_0601FAEC(quad_data+ 6, base_r, base_g, base_b);
        c = Azel_0601FAEC(quad_data+12, base_r, base_g, base_b);
        d = Azel_0601FAEC(quad_data+18, base_r, base_g, base_b);
        state->R[4] += 48;
        break;

      default:  // case 3
        a = Azel_0601FA68(quad_data+0, base_r, base_g, base_b);
        b = Azel_0601FA68(quad_data+3, base_r, base_g, base_b);
        c = Azel_0601FA68(quad_data+6, base_r, base_g, base_b);
        d = Azel_0601FA68(quad_data+9, base_r, base_g, base_b);
        state->R[4] += 24;
        break;
    }  // switch (quad_type)

    out[0 ^ swapflag] = VDP_SWAPW(a);
    out[1 ^ swapflag] = VDP_SWAPW(b);
    out[2 ^ swapflag] = VDP_SWAPW(c);
    out[3 ^ swapflag] = VDP_SWAPW(d);
}

static int Azel_0601FA68(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b)
{
    return Azel_color_calc((const int16_t *)HRAM_PTR(0x601FC94),
                           vertex_data,
                           (const int16_t *)HRAM_PTR(0x601FCA8),
                           base_r, base_g, base_b);
}

static int Azel_0601FAEC(const int16_t *vertex_data,
                         int base_r, int base_g, int base_b)
{
    return Azel_color_calc((const int16_t *)HRAM_PTR(0x601FC94),
                           vertex_data,
                           (const int16_t *)HRAM_PTR(0x601FCA8),
                           base_r + vertex_data[5],
                           base_g + vertex_data[4],
                           base_b + vertex_data[3]);
}

/*----------------------------------*/

static int Azel_0601F58A(SH2State *state, uint32_t r11, uint32_t r13)
{
    r11 <<= 3;
    r13 <<= 3;
    const uint32_t r7 = state->R[7];
    const int16_t *r10_ptr = (const int16_t *)HRAM_PTR(r7 + (r11 >> 16));
    const int16_t *r11_ptr = (const int16_t *)HRAM_PTR(r7 + (r11 & 0xFFFF));
    const int16_t *r12_ptr = (const int16_t *)HRAM_PTR(r7 + (r13 >> 16));
    const int16_t *r13_ptr = (const int16_t *)HRAM_PTR(r7 + (r13 & 0xFFFF));
    return Azel_0601F5A6(state, r10_ptr, r11_ptr, r12_ptr, r13_ptr);
}

static int Azel_0601F5A6(SH2State *state,
                         const int16_t *r10_ptr, const int16_t *r11_ptr,
                         const int16_t *r12_ptr, const int16_t *r13_ptr)
{
    const int32_t a = (r13_ptr[1] - r11_ptr[1]) * (r12_ptr[0] - r10_ptr[0]);
    const int32_t b = (r12_ptr[1] - r10_ptr[1]) * (r13_ptr[0] - r11_ptr[0]);
    if (b > a) {
        state->cycles += 35;
        return Azel_0601F5D2(state);
    }

    const int swapflag = 0;

    state->R[14] = HRAM_LOADL(state->GBR, 0);
    uint32_t *r14_ptr = (uint32_t *)VDP1_PTR(state->R[14]);

    r14_ptr[3] = RAM_VDP_SWAPL(*(const uint32_t *)r10_ptr);
    r14_ptr[4] = RAM_VDP_SWAPL(*(const uint32_t *)r11_ptr);
    r14_ptr[5] = RAM_VDP_SWAPL(*(const uint32_t *)r12_ptr);
    r14_ptr[6] = RAM_VDP_SWAPL(*(const uint32_t *)r13_ptr);

    const uint32_t r4_0 = LRAM_LOADL(state->R[4], 0);

    state->cycles += 49;
    return Azel_0601F762(state,
                         (const uint32_t *)r10_ptr,
                         (const uint32_t *)r11_ptr,
                         (const uint32_t *)r12_ptr,
                         (const uint32_t *)r13_ptr,
                         r14_ptr, r4_0, swapflag);
}

static int Azel_0601F5D2(SH2State *state)
{
    static const uint8_t r4ofs_cycles[8] = {
        12, 20, 60, 36,  // R4 offset (polygon data size)
        10, 12, 15, 15,  // Cycle count
    };
    const unsigned int quad_type = (LRAM_LOADW(state->R[4], 0) >> 8) & 0x3;
    state->R[4] += r4ofs_cycles[quad_type];
    state->cycles += r4ofs_cycles[quad_type+4];
    return -1;
}

/*----------------------------------*/

static int Azel_0601F5EE(SH2State *state, uint32_t r11, uint32_t r13)
{
    uint32_t cycles = state->cycles;

    r11 <<= 5;
    r13 <<= 5;
    const uint32_t r7 = state->R[7];
    uint32_t *r10_ptr = (uint32_t *)HRAM_PTR(r7 + (r11 >> 16));
    uint32_t *r11_ptr = (uint32_t *)HRAM_PTR(r7 + (r11 & 0xFFFF));
    uint32_t *r12_ptr = (uint32_t *)HRAM_PTR(r7 + (r13 >> 16));
    uint32_t *r13_ptr = (uint32_t *)HRAM_PTR(r7 + (r13 & 0xFFFF));

    if (r10_ptr[6] & r11_ptr[6] & r12_ptr[6] & r13_ptr[6]) {
        state->cycles = cycles + 27;
        return Azel_0601F5D2(state);
    }
    if ((r10_ptr[6] | r11_ptr[6] | r12_ptr[6] | r13_ptr[6]) & RAM_SWAPL(0x20)) {
        state->cycles = cycles + 33;
        return Azel_0601F5D2(state);
    }
    if (!(r10_ptr[6] | r11_ptr[6] | r12_ptr[6] | r13_ptr[6])) {
        state->cycles = cycles + 35 - 14;
        return Azel_0601F5A6(state,
                             (const int16_t *)r10_ptr,
                             (const int16_t *)r11_ptr,
                             (const int16_t *)r12_ptr,
                             (const int16_t *)r13_ptr);
    }

    r10_ptr[7] = r10_ptr[6];
    r11_ptr[7] = r11_ptr[6];
    r12_ptr[7] = r12_ptr[6];
    r13_ptr[7] = r13_ptr[6];
    r10_ptr[4] = RAM_SWAPL((int32_t)((int16_t *)r10_ptr)[0]);
    r10_ptr[5] = RAM_SWAPL((int32_t)((int16_t *)r10_ptr)[1]);
    r11_ptr[4] = RAM_SWAPL((int32_t)((int16_t *)r11_ptr)[0]);
    r11_ptr[5] = RAM_SWAPL((int32_t)((int16_t *)r11_ptr)[1]);
    r12_ptr[4] = RAM_SWAPL((int32_t)((int16_t *)r12_ptr)[0]);
    r12_ptr[5] = RAM_SWAPL((int32_t)((int16_t *)r12_ptr)[1]);
    r13_ptr[4] = RAM_SWAPL((int32_t)((int16_t *)r13_ptr)[0]);
    r13_ptr[5] = RAM_SWAPL((int32_t)((int16_t *)r13_ptr)[1]);

    const int16_t * const r6_ptr = (const int16_t *)HRAM_PTR(state->R[6]);
    int32_t result = Azel_0601F824(r6_ptr, r10_ptr, r11_ptr, r12_ptr, r13_ptr,
                                   0x10, Azel_0601F9D6);
    cycles += result & 0xFFFF;
    if (result < 0) {
        state->cycles = cycles + 61;
        return Azel_0601F5D2(state);
    }

    const int32_t r10_x = RAM_SWAPL(r10_ptr[4]);
    const int32_t r11_x = RAM_SWAPL(r11_ptr[4]);
    const int32_t r12_x = RAM_SWAPL(r12_ptr[4]);
    const int32_t r13_x = RAM_SWAPL(r13_ptr[4]);
    const int32_t r10_y = RAM_SWAPL(r10_ptr[5]);
    const int32_t r11_y = RAM_SWAPL(r11_ptr[5]);
    const int32_t r12_y = RAM_SWAPL(r12_ptr[5]);
    const int32_t r13_y = RAM_SWAPL(r13_ptr[5]);
    int32_t r0 = (r13_y - r11_y) * (r12_x - r10_x);
    int32_t r2 = (r12_y - r10_y) * (r13_x - r11_x);
    if (r2 > r0) {
        state->cycles = cycles + 79;
        return Azel_0601F5D2(state);
    }

    result = Azel_0601F824(r6_ptr, r10_ptr, r11_ptr, r12_ptr, r13_ptr,
                           0x1, Azel_0601F948);
    cycles += result & 0xFFFF;
    if (result < 0) {
        state->cycles = cycles + 84;
        return Azel_0601F5D2(state);
    }
    result = Azel_0601F824(r6_ptr, r10_ptr, r11_ptr, r12_ptr, r13_ptr,
                           0x2, Azel_0601F93A);
    cycles += result & 0xFFFF;
    if (result < 0) {
        state->cycles = cycles + 89;
        return Azel_0601F5D2(state);
    }
    result = Azel_0601F824(r6_ptr, r10_ptr, r11_ptr, r12_ptr, r13_ptr,
                           0x4, Azel_0601F996);
    cycles += result & 0xFFFF;
    if (result < 0) {
        state->cycles = cycles + 94;
        return Azel_0601F5D2(state);
    }
    result = Azel_0601F824(r6_ptr, r10_ptr, r11_ptr, r12_ptr, r13_ptr,
                           0x8, Azel_0601F988);
    cycles += result & 0xFFFF;
    if (result < 0) {
        state->cycles = cycles + 99;
        return Azel_0601F5D2(state);
    }

    int swapflag;
    uint32_t r4_0 = LRAM_LOADL(state->R[4], 0);
    if (r10_ptr[7]) {
        if (r11_ptr[7]) {
            if (r12_ptr[7]) {
                if (r13_ptr[7]) {
                    swapflag = 0;
                    cycles += 116;
                } else {
                    uint32_t *temp1, *temp2;
                    temp1 = r10_ptr; temp2 = r13_ptr;
                            r10_ptr = temp2; r13_ptr = temp1;
                    temp1 = r11_ptr; temp2 = r12_ptr;
                            r11_ptr = temp2; r12_ptr = temp1;
                    r4_0 ^= 0x20;
                    swapflag = 3;
                    cycles += 124;
                }
            } else {
                uint32_t *temp1, *temp2;
                temp1 = r10_ptr; temp2 = r12_ptr;
                        r10_ptr = temp2; r12_ptr = temp1;
                temp1 = r11_ptr; temp2 = r13_ptr;
                        r11_ptr = temp2; r13_ptr = temp1;
                r4_0 ^= 0x30;
                swapflag = 2;
                cycles += 123;
            }
        } else {
            uint32_t *temp1, *temp2;
            temp1 = r10_ptr; temp2 = r11_ptr;
                    r10_ptr = temp2; r11_ptr = temp1;
            temp1 = r12_ptr; temp2 = r13_ptr;
                    r12_ptr = temp2; r13_ptr = temp1;
            r4_0 ^= 0x10;
            swapflag = 1;
            cycles += 120;
        }
    } else {
        swapflag = 0;
        cycles += 105;
    }

    state->R[14] = HRAM_LOADL(state->GBR, 0);
    uint32_t *r14_ptr = (uint32_t *)VDP1_PTR(state->R[14]);

    const uint16_t *r10_ptr16 = (const uint16_t *)r10_ptr;
    const uint16_t *r11_ptr16 = (const uint16_t *)r11_ptr;
    const uint16_t *r12_ptr16 = (const uint16_t *)r12_ptr;
    const uint16_t *r13_ptr16 = (const uint16_t *)r13_ptr;
    r14_ptr[3] = VDP_SWAPL(r10_ptr16[9]<<16 | r10_ptr16[11]);
    r14_ptr[4] = VDP_SWAPL(r11_ptr16[9]<<16 | r11_ptr16[11]);
    r14_ptr[5] = VDP_SWAPL(r12_ptr16[9]<<16 | r12_ptr16[11]);
    r14_ptr[6] = VDP_SWAPL(r13_ptr16[9]<<16 | r13_ptr16[11]);

    state->cycles = cycles + 24;
    return Azel_0601F762(state, r10_ptr, r11_ptr, r12_ptr, r13_ptr,
                         r14_ptr, r4_0, swapflag);
}

/*----------------------------------*/

static ALWAYS_INLINE int Azel_0601F762(
    SH2State *state, const uint32_t * const r10_ptr,
    const uint32_t * const r11_ptr, const uint32_t * const r12_ptr,
    const uint32_t * const r13_ptr, uint32_t * const r14_ptr,
    const uint32_t r4_0, const int swapflag)
{
    const uint32_t r4_1 = LRAM_LOADL(state->R[4], 4);
    const uint32_t r4_2 = LRAM_LOADL(state->R[4], 8);
    state->R[4] += 12;

    *(uint16_t *)&r14_ptr[0] = VDP_SWAPW(r4_0 | 0x1000);
    r14_ptr[1] = VDP_SWAPL(r4_1);
    r14_ptr[2] = VDP_SWAPL(r4_2);

    int32_t r1 = RAM_SWAPL(r10_ptr[1]);
    int32_t r2 = RAM_SWAPL(r11_ptr[1]);
    int32_t r3 = RAM_SWAPL(r12_ptr[1]);
    int32_t r5 = RAM_SWAPL(r13_ptr[1]);
    const uint32_t out_addr = HRAM_LOADL(state->GBR, 32);
    uint16_t *out_ptr = (uint16_t *)HRAM_PTR(out_addr);
    const int32_t mult = HRAM_LOADL(state->R[6], 52);

    switch (r4_0 >> 28) {
      case 0:
        r1 = MAX(MAX(r1, r2), MAX(r3, r5));
        state->cycles += 21;
      l_601F7F4:
        r1 = ((int64_t)r1 * (int64_t)mult) >> 32;
        if (r1 <= 0) {
            r1 = 1;
        }
        out_ptr[2] = r1;
        out_ptr[3] = (uint16_t)(state->R[14] >> 3);
        state->cycles += 15;
        break;

      default:  // case 1
        r1 = MIN(MIN(r1, r2), MIN(r3, r5));
        state->cycles += 21;
        goto l_601F7F4;

      case 2:
        r2 -= r1;
        r3 -= r1;
        r5 -= r1;
        r2 += r3 + r5;
        r1 += r2 >> 2;
        state->cycles += 18;
        goto l_601F7F4;

      case 3:
        r2 -= r1;
        r3 -= r1;
        r5 -= r1;
        r2 += r3 + r5;
        r1 += r2 >> 2;
        r1 = ((int64_t)r1 * (int64_t)mult) >> 32;
        if (r1 <= 0) {
            r1 = 1;
        }
        out_ptr[2] = 0x7FFF;
        out_ptr[3] = (uint16_t)(state->R[14] >> 3);
        state->cycles += 25;
        break;
    }  // switch (r4_0 >> 28)

    /* 0x601F810 */

    HRAM_STOREL(state->GBR, 32, out_addr + 8);
    HRAM_STOREL(state->GBR, 12, HRAM_LOADL(state->GBR, 12) + 1);
    HRAM_STOREL(state->GBR, 28, HRAM_LOADL(state->GBR, 28) + 1);
    state->R[1] = r1;
    state->cycles += 8 + 11;
    return swapflag;
}

/*----------------------------------*/

static int32_t Azel_0601F824(const int16_t *r6_ptr, uint32_t *r10_ptr,
                             uint32_t *r11_ptr, uint32_t *r12_ptr,
                             uint32_t *r13_ptr, uint32_t mask,
                             int (*clipfunc)(uint32_t *r8_ptr, uint32_t *r9_ptr,
                                             const int16_t *r6_ptr))
{
    int32_t cycles = 0;

    /* We save a bit of time by byte-swapping just this value and loading
     * the values to check directly from native memory. */
    mask = RAM_SWAPL(mask);
    const uint32_t r10_check = r10_ptr[7] & mask;
    const uint32_t r11_check = r11_ptr[7] & mask;
    const uint32_t r12_check = r12_ptr[7] & mask;
    const uint32_t r13_check = r13_ptr[7] & mask;

    if (r13_check) {
        if (r12_check) {
            cycles += 10;
            if (r11_check) {
                if (r10_check) {
                    cycles += 20;
                    return 0x80000000 | cycles;
                } else {
                    cycles += (*clipfunc)(r11_ptr, r10_ptr, r6_ptr);
                    cycles += (*clipfunc)(r12_ptr, r10_ptr, r6_ptr);
                    cycles += (*clipfunc)(r13_ptr, r10_ptr, r6_ptr);
                    cycles += 27;
                    return cycles;
                }
            } else if (r10_check) {
                cycles += (*clipfunc)(r10_ptr, r11_ptr, r6_ptr);
                cycles += (*clipfunc)(r13_ptr, r11_ptr, r6_ptr);
                cycles += (*clipfunc)(r12_ptr, r11_ptr, r6_ptr);
                cycles += 28;
                return cycles;
            } else {
                cycles += (*clipfunc)(r13_ptr, r10_ptr, r6_ptr);
                cycles += (*clipfunc)(r12_ptr, r11_ptr, r6_ptr);
                cycles += 24;
                return cycles;
            }
        } else if (r11_check) {
            if (r10_check) {
                cycles += (*clipfunc)(r10_ptr, r12_ptr, r6_ptr);
                cycles += (*clipfunc)(r13_ptr, r12_ptr, r6_ptr);
                cycles += (*clipfunc)(r11_ptr, r12_ptr, r6_ptr);
                cycles += 28;
                return cycles;
            } else {
                cycles += 17;
                return 0x80000000 | cycles;
            }
        } else if (r10_check) {
            cycles += (*clipfunc)(r10_ptr, r11_ptr, r6_ptr);
            cycles += (*clipfunc)(r13_ptr, r12_ptr, r6_ptr);
            cycles += 25;
            return cycles;
        } else {
            const int32_t r1 = RAM_SWAPL(r12_ptr[1]);
            const int32_t r2 = RAM_SWAPL(r10_ptr[1]);
            cycles += (r1 > r2) ? 23 : 21;
            cycles += (*clipfunc)(r13_ptr, (r1 > r2) ? r12_ptr : r10_ptr,
                                  r6_ptr);
            return cycles;
        }

    } else if (r12_check) {
        if (r11_check) {
            if (r10_check) {
                cycles += (*clipfunc)(r11_ptr, r13_ptr, r6_ptr);
                cycles += (*clipfunc)(r12_ptr, r13_ptr, r6_ptr);
                cycles += (*clipfunc)(r10_ptr, r13_ptr, r6_ptr);
                cycles += 28;
                return cycles;
            } else {
                cycles += (*clipfunc)(r11_ptr, r10_ptr, r6_ptr);
                cycles += (*clipfunc)(r12_ptr, r13_ptr, r6_ptr);
                cycles += 24;
                return cycles;
            }
        } else if (r10_check) {
            cycles += 18;
            return 0x80000000 | cycles;
        } else {
            const int32_t r1 = RAM_SWAPL(r13_ptr[1]);
            const int32_t r2 = RAM_SWAPL(r11_ptr[1]);
            cycles += (r1 > r2) ? 23 : 21;
            cycles += (*clipfunc)(r12_ptr, (r1 > r2) ? r13_ptr : r11_ptr,
                                  r6_ptr);
            return cycles;
        }

    } else if (r11_check) {
        if (r10_check) {
            cycles += (*clipfunc)(r10_ptr, r13_ptr, r6_ptr);
            cycles += (*clipfunc)(r11_ptr, r12_ptr, r6_ptr);
            cycles += 25;
            return cycles;
        } else {
            const int32_t r1 = RAM_SWAPL(r10_ptr[1]);
            const int32_t r2 = RAM_SWAPL(r12_ptr[1]);
            cycles += (r1 > r2) ? 23 : 21;
            cycles += (*clipfunc)(r11_ptr, (r1 > r2) ? r10_ptr : r12_ptr,
                                  r6_ptr);
            return cycles;
        }

    } else if (r10_check) {
        const int32_t r1 = RAM_SWAPL(r11_ptr[1]);
        const int32_t r2 = RAM_SWAPL(r13_ptr[1]);
        cycles += (r1 > r2) ? 24 : 22;
        cycles += (*clipfunc)(r10_ptr, (r1 > r2) ? r11_ptr : r13_ptr, r6_ptr);
        return cycles;

    } else {
        cycles += 15;
        return cycles;
    }
}

/*----------------------------------*/

static int Azel_0601F93A(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr)
{
    const int32_t r8_x = RAM_SWAPL(r8_ptr[4]);
    const int32_t x_max = r6_ptr[3];
    if (r8_x <= x_max) {
        return 8;
    }
    return Azel_0601F950(r8_ptr, r9_ptr, r6_ptr, r8_x, x_max, 6);
}

static int Azel_0601F948(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr)
{
    const int32_t r8_x = RAM_SWAPL(r8_ptr[4]);
    const int32_t x_min = r6_ptr[2];
    if (r8_x >= x_min) {
        return 10;
    }
    return Azel_0601F950(r8_ptr, r9_ptr, r6_ptr, r8_x, x_min, 4);
}

static ALWAYS_INLINE int Azel_0601F950(uint32_t *r8_ptr, uint32_t *r9_ptr,
                                       const int16_t *r6_ptr,
                                       const int32_t r8_x, const int32_t x_lim,
                                       int base_cycles)
{
    r8_ptr[4] = RAM_SWAPL(x_lim);
    const int32_t dx_lim = x_lim - r8_x;
    const int32_t dx_r9 = RAM_SWAPL(r9_ptr[4]) - r8_x;
    int32_t frac8;
    if (UNLIKELY(dx_r9 == 0)) {
        frac8 = (dx_lim >= 0) ? 0xFF : 0;
    } else {
        frac8 = bound((dx_lim << 8) / dx_r9, 0, 255);
    }
    const int32_t r8_y = RAM_SWAPL(r8_ptr[5]);
    const int32_t r9_y = RAM_SWAPL(r9_ptr[5]);
    const int32_t new_r8_y = (int16_t)(r8_y + (((r9_y - r8_y) * frac8) >> 8));
    r8_ptr[5] = RAM_SWAPL(new_r8_y);

    return 29 + base_cycles;
}

/*----------------------------------*/

static int Azel_0601F988(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr)
{
    const int32_t r8_y = RAM_SWAPL(r8_ptr[5]);
    const int32_t y_max = r6_ptr[0];
    if (r8_y <= y_max) {
        return 8;
    }
    return Azel_0601F99E(r8_ptr, r9_ptr, r6_ptr, r8_y, y_max, 6);
}

static int Azel_0601F996(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr)
{
    const int32_t r8_y = RAM_SWAPL(r8_ptr[5]);
    const int32_t y_min = r6_ptr[1];
    if (r8_y >= y_min) {
        return 10;
    }
    return Azel_0601F99E(r8_ptr, r9_ptr, r6_ptr, r8_y, y_min, 4);
}

static ALWAYS_INLINE int Azel_0601F99E(uint32_t *r8_ptr, uint32_t *r9_ptr,
                                       const int16_t *r6_ptr,
                                       const int32_t r8_y, const int32_t y_lim,
                                       int base_cycles)
{
    r8_ptr[5] = RAM_SWAPL(y_lim);
    const int32_t dy_lim = y_lim - r8_y;
    const int32_t dy_r9 = RAM_SWAPL(r9_ptr[5]) - r8_y;
    int32_t frac8;
    if (UNLIKELY(dy_r9 == 0)) {
        frac8 = (dy_lim >= 0) ? 0xFF : 0;
    } else {
        frac8 = bound((dy_lim << 8) / dy_r9, 0, 255);
    }
    const int32_t r8_x = RAM_SWAPL(r8_ptr[4]);
    const int32_t r9_x = RAM_SWAPL(r9_ptr[4]);
    const int32_t new_r8_x = (int16_t)(r8_x + (((r9_x - r8_x) * frac8) >> 8));
    r8_ptr[4] = RAM_SWAPL(new_r8_x);

    return 29 + base_cycles;
}

/*----------------------------------*/

static int Azel_0601F9D6(uint32_t *r8_ptr, uint32_t *r9_ptr,
                         const int16_t *r6_ptr)
{
    const int32_t *r6_ptr32 = (const int32_t *)r6_ptr;
    int32_t r3 = RAM_SWAPL(r8_ptr[1]);
    int32_t r0 = (RAM_SWAPL(r6_ptr32[4]) << 8) - r3;
    int32_t r2 = RAM_SWAPL(r9_ptr[1]) - r3;
    const float frac = (float)r0 / (float)r2;
    r2 = RAM_SWAPL(r9_ptr[2]);
    r0 = RAM_SWAPL(r8_ptr[2]);
    r2 = r0 + (int32_t)((r2 - r0) * frac);
    r3 = RAM_SWAPL(r9_ptr[3]);
    r0 = RAM_SWAPL(r8_ptr[3]);
    r3 = r0 + (int32_t)((r3 - r0) * frac);
    const int32_t mult = RAM_SWAPL(r6_ptr32[12]);
    r2 = ((int64_t)r2 * (int64_t)mult) >> 32;
    r3 = ((int64_t)r3 * (int64_t)mult) >> 32;
    r8_ptr[4] = RAM_SWAPL(r2);
    r8_ptr[5] = RAM_SWAPL(r3);

    const uint32_t r1 = (r3 > r6_ptr[4]) << 3
                      | (r3 < r6_ptr[5]) << 2
                      | (r2 > r6_ptr[7]) << 1
                      | (r2 < r6_ptr[6]) << 0;
    r8_ptr[7] = RAM_SWAPL(r1);

    return 73;
}

/*-----------------------------------------------------------------------*/

/* 0x601EE60: Coordinate transformation routine whose second instruction is
 * modified from 0x601F1{20,40}; we detect the change here and hint
 * 0x601EE62 as a data pointer to avoid having to repeatedly translate the
 * block.  Incidentally, 0x601F1{20,40} are themselves called from (among
 * other places) 0x601F1{50,5E,6C}, which _push and pop_ the _instruction
 * word_.  Good grief... */

static FASTCALL void Azel_0601EE60(SH2State *state)
{
    const uint16_t insn = HRAM_LOADW(0x601EE62, 0);
    if (insn>>12 == 0xA) {  // BRA instruction
        state->R[0] = 0x601EF10;
        state->MACL = state->MACH = 0;
        state->PC += 6 + ((int32_t)(insn<<20) >> 19);
        state->cycles += 4;
    } else {
        if (insn != 0x6103) {
            DMSG("WARNING: Wrong instruction at 0x601EE62 (%04X)", insn);
        }
        const int32_t *M_ptr =
            state->R[5] & 0x6000000 ? (int32_t *)HRAM_PTR(state->R[5])
                                    : (int32_t *)LRAM_PTR(state->R[5]);
        const int32_t in_x = HRAM_LOADL(0x601EF18, 0);
        const int32_t in_y = HRAM_LOADL(0x601EF18, 4);
        const int32_t in_z = HRAM_LOADL(0x601EF18, 8);
        int16_t out_x = (RAM_SWAPL(M_ptr[ 0]) * in_x
                       + RAM_SWAPL(M_ptr[ 4]) * in_y
                       + RAM_SWAPL(M_ptr[ 8]) * in_z) >> 16;
        int16_t out_y = (RAM_SWAPL(M_ptr[ 1]) * in_x
                       + RAM_SWAPL(M_ptr[ 5]) * in_y
                       + RAM_SWAPL(M_ptr[ 9]) * in_z) >> 16;
        int16_t out_z = (RAM_SWAPL(M_ptr[ 2]) * in_x
                       + RAM_SWAPL(M_ptr[ 6]) * in_y
                       + RAM_SWAPL(M_ptr[10]) * in_z) >> 16;
        HRAM_STOREW(0x601EF10, 0, out_x);
        HRAM_STOREW(0x601EF10, 2, out_y);
        HRAM_STOREW(0x601EF10, 4, out_z);
        state->PC = state->PR;
        state->cycles += 58;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x601EEE8: Routine that writes a local data array in a loop; optimized
 * to prevent JIT overwrite checks from flooding the blacklist array.  An
 * identical routine is located at 0x601FC6C; this code is used for both. */

static FASTCALL void Azel_0601EEE8(SH2State *state)
{
    const int r = (state->R[4]>> 0 & 0xFF) << 8;
    const int g = (state->R[4]>> 8 & 0xFF) << 8;
    const int b = (state->R[4]>>16 & 0xFF) << 8;
    int16_t *out = (int16_t *)HRAM_PTR(((state->PC + 8*2) & -4) + 4 + 0xC*4);
    int16_t *top = out + 32*4;
    for (; out != top; out += 4) {
        out[0] = r;
        out[1] = g;
        out[2] = b;
    }
    state->PC = state->PR;
    state->cycles += 14 + 32*7;
}

/*-----------------------------------------------------------------------*/

/* 0x601F240, 0x601F24C: Calculation function */

static ALWAYS_INLINE void Azel_0601F24E(SH2State *state, int32_t r2);

/*----------------------------------*/

static FASTCALL void Azel_0601F240(SH2State *state)
{
    const int32_t r2 = LRAM_LOADL(state->R[4], 0);
    state->cycles += 8;
    return Azel_0601F24E(state,
                         ((int64_t)r2 * (int64_t)(int32_t)state->R[8]) >> 16);
}

static FASTCALL void Azel_0601F24C(SH2State *state)
{
    const int32_t r2 = LRAM_LOADL(state->R[4], 0);
    return Azel_0601F24E(state, r2);
}

static ALWAYS_INLINE void Azel_0601F24E(SH2State *state, int32_t r2)
{
    uint32_t r0;
    int32_t r8, r9;
    int32_t r1 = HRAM_LOADL(state->R[5], 44);
    int32_t r3 = HRAM_LOADL(state->R[6], 20);
    if (r1 - r2 >= r3) {
        state->SR |= SR_T;
        state->PC = state->PR;
        state->cycles += 12;
        return;
    }
    int32_t r10 = HRAM_LOADL(state->R[6], 16);
    if (r1 + r2 < r10) {
        state->SR |= SR_T;
        state->PC = state->PR;
        state->cycles += 17;
        return;
    }
    r0 = (r1 + r2 >= r3) << 5
       | (r1 - r2 < r10) << 4;

#if defined(__mips__)
    int32_t temp;
    asm(".set push; .set noreorder\n"
        "lw %[r8], 36(%[r6])\n"
        "lw %[r9], 32(%[r6])\n"
        "lw %[r3], 28(%[r5])\n"
        "ror %[r8], %[r8], 16\n"
        "mult %[r8], %[r1]\n"
        "ror %[r9], %[r9], 16\n"
        "ror %[r3], %[r3], 16\n"
        "mflo %[r8]\n"
        "mfhi %[temp]\n"
        "mult %[r9], %[r2]\n"
        "srl %[r8], %[r8], 16\n"
        "ins %[r8], %[temp], 16, 16\n"
        "mflo %[r9]\n"
        "mfhi %[temp]\n"
        "srl %[r9], %[r9], 16\n"
        "ins %[r9], %[temp], 16, 16\n"
        ".set pop"
        : [r3] "=&r" (r3), [r8] "=&r" (r8), [r9] "=&r" (r9),
          [temp] "=&r" (temp)
        : [r5] "r" (HRAM_PTR(state->R[5])), [r6] "r" (HRAM_PTR(state->R[6])),
          [r1] "r" (r1), [r2] "r" (r2)
    );
#else
    r3 = HRAM_LOADL(state->R[5], 28);
    r8 = HRAM_LOADL(state->R[6], 36);
    r9 = HRAM_LOADL(state->R[6], 32);
    r8 = ((int64_t)r8 * (int64_t)r1) >> 16;
    r9 = ((int64_t)r9 * (int64_t)r2) >> 16;
#endif
    if (r8 + r9 < r3) {
        state->SR |= SR_T;
        state->PC = state->PR;
        state->cycles += 41;
        return;
    }
    if (r8 + r9 < -r3) {
        state->SR |= SR_T;
        state->PC = state->PR;
        state->cycles += 44;
        return;
    }
    r0 |= (r8 - r9 <  r3) << 3
       |  (r8 - r9 < -r3) << 2;

#if defined(__mips__)
    asm(".set push; .set noreorder\n"
        "lw %[r8], 44(%[r6])\n"
        "lw %[r9], 40(%[r6])\n"
        "lw %[r3], 12(%[r5])\n"
        "ror %[r8], %[r8], 16\n"
        "mult %[r8], %[r1]\n"
        "ror %[r9], %[r9], 16\n"
        "ror %[r3], %[r3], 16\n"
        "mflo %[r8]\n"
        "mfhi %[temp]\n"
        "mult %[r9], %[r2]\n"
        "srl %[r8], %[r8], 16\n"
        "ins %[r8], %[temp], 16, 16\n"
        "mflo %[r9]\n"
        "mfhi %[temp]\n"
        "srl %[r9], %[r9], 16\n"
        "ins %[r9], %[temp], 16, 16\n"
        ".set pop"
        : [r3] "=&r" (r3), [r8] "=&r" (r8), [r9] "=&r" (r9),
          [temp] "=&r" (temp)
        : [r5] "r" (HRAM_PTR(state->R[5])), [r6] "r" (HRAM_PTR(state->R[6])),
          [r1] "r" (r1), [r2] "r" (r2)
    );
#else
    r3 = HRAM_LOADL(state->R[5], 12);
    r8 = HRAM_LOADL(state->R[6], 44);
    r9 = HRAM_LOADL(state->R[6], 40);
    r8 = ((int64_t)r8 * (int64_t)r1) >> 16;
    r9 = ((int64_t)r9 * (int64_t)r2) >> 16;
#endif
    if (r8 + r9 < r3) {
        state->SR |= SR_T;
        state->PC = state->PR;
        state->cycles += 69;
        return;
    }
    if (r8 + r9 < -r3) {
        state->SR |= SR_T;
        state->PC = state->PR;
        state->cycles += 72;
        return;
    }
    r0 |= (r8 - r9 <  r3) << 1
       |  (r8 - r9 < -r3) << 0;

    state->R[0] = r0;
    state->SR &= ~SR_T;
    state->PC = state->PR;
    state->cycles += 76;
}

/*-----------------------------------------------------------------------*/

/* 0x601F30E: Coordinate transform routine */

static FASTCALL void Azel_0601F30E(SH2State *state)
{
    const int32_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int32_t *)HRAM_PTR(state->R[4])
        : (const int32_t *)LRAM_PTR(state->R[4]);
    const int32_t *r5_ptr = (const int32_t *)HRAM_PTR(state->R[5]);
    const int32_t *r6_ptr = (const int32_t *)HRAM_PTR(state->R[6]);

    /* 0x601F38E */

    const int32_t r6_x = RAM_SWAPL(r6_ptr[6]);
    const int32_t M11 = (r6_x * (int32_t)RAM_SWAPL(r5_ptr[0])) >> 12;
    const int32_t M12 = (r6_x * (int32_t)RAM_SWAPL(r5_ptr[1])) >> 12;
    const int32_t M13 = (r6_x * (int32_t)RAM_SWAPL(r5_ptr[2])) >> 12;
    const int32_t M14 =  r6_x * (int32_t)RAM_SWAPL(r5_ptr[3]);

    const int32_t r6_y = -RAM_SWAPL(r6_ptr[7]);
    const int32_t M21 = (r6_y * (int32_t)RAM_SWAPL(r5_ptr[4])) >> 12;
    const int32_t M22 = (r6_y * (int32_t)RAM_SWAPL(r5_ptr[5])) >> 12;
    const int32_t M23 = (r6_y * (int32_t)RAM_SWAPL(r5_ptr[6])) >> 12;
    const int32_t M24 =  r6_y * (int32_t)RAM_SWAPL(r5_ptr[7]);

    const int32_t M31 = (int32_t)RAM_SWAPL(r5_ptr[ 8]) >> 4;
    const int32_t M32 = (int32_t)RAM_SWAPL(r5_ptr[ 9]) >> 4;
    const int32_t M33 = (int32_t)RAM_SWAPL(r5_ptr[10]) >> 4;
    const int32_t M34 = (int32_t)RAM_SWAPL(r5_ptr[11]) << 8;

    uint32_t counter = RAM_SWAPL(r4_ptr[1]);
    const uint32_t in_address = RAM_SWAPL(r4_ptr[2]);

    state->cycles += 111;

    /* 0x601F314 */

    state->cycles += 8 + 56*counter;

    const int16_t *in = in_address & 0x06000000
        ? (const int16_t *)HRAM_PTR(in_address)
        : (const int16_t *)LRAM_PTR(in_address);
    int16_t *out = (int16_t *)HRAM_PTR(state->R[7]);

    do {
        const int32_t out_x = M11*in[0] + M12*in[1] + M13*in[2] + M14;
        const int32_t out_y = M21*in[0] + M22*in[1] + M23*in[2] + M24;
        const int32_t out_z = M31*in[0] + M32*in[1] + M33*in[2] + M34;
        const float coord_mult = 256.0f / out_z;
        out[0] = ifloorf(coord_mult * out_x);
        out[1] = ifloorf(coord_mult * out_y);
        *(int32_t *)&out[2] = RAM_SWAPL(out_z);
        in += 3;
        out += 4;
    } while (--counter != 0);

    state->R[4] += 12;
    state->PC = state->PR;
}

/*-----------------------------------------------------------------------*/

/* 0x601F3F4: Coordinate transform routine with boundary checks */

static FASTCALL void Azel_0601F3F4(SH2State *state)
{
    const int32_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int32_t *)HRAM_PTR(state->R[4])
        : (const int32_t *)LRAM_PTR(state->R[4]);
    const int32_t *r5_ptr = (const int32_t *)HRAM_PTR(state->R[5]);
    const int32_t *r6_ptr = (const int32_t *)HRAM_PTR(state->R[6]);

    /* 0x601F38E */

    const int32_t r6_x = RAM_SWAPL(r6_ptr[6]);
    const int32_t M11 = (r6_x * (int32_t)RAM_SWAPL(r5_ptr[0])) >> 12;
    const int32_t M12 = (r6_x * (int32_t)RAM_SWAPL(r5_ptr[1])) >> 12;
    const int32_t M13 = (r6_x * (int32_t)RAM_SWAPL(r5_ptr[2])) >> 12;
    const int32_t M14 =  r6_x * (int32_t)RAM_SWAPL(r5_ptr[3]);

    const int32_t r6_y = -RAM_SWAPL(r6_ptr[7]);
    const int32_t M21 = (r6_y * (int32_t)RAM_SWAPL(r5_ptr[4])) >> 12;
    const int32_t M22 = (r6_y * (int32_t)RAM_SWAPL(r5_ptr[5])) >> 12;
    const int32_t M23 = (r6_y * (int32_t)RAM_SWAPL(r5_ptr[6])) >> 12;
    const int32_t M24 =  r6_y * (int32_t)RAM_SWAPL(r5_ptr[7]);

    const int32_t M31 = (int32_t)RAM_SWAPL(r5_ptr[ 8]) >> 4;
    const int32_t M32 = (int32_t)RAM_SWAPL(r5_ptr[ 9]) >> 4;
    const int32_t M33 = (int32_t)RAM_SWAPL(r5_ptr[10]) >> 4;
    const int32_t M34 = (int32_t)RAM_SWAPL(r5_ptr[11]) << 8;

    uint32_t counter = RAM_SWAPL(r4_ptr[1]);
    const uint32_t in_address = RAM_SWAPL(r4_ptr[2]);

    state->cycles += 111;

    /* 0x601F3FA */

    state->cycles += 7 + 63*counter;

    const int16_t *in = in_address & 0x06000000
        ? (const int16_t *)HRAM_PTR(in_address)
        : (const int16_t *)LRAM_PTR(in_address);
    int16_t *out = (int16_t *)HRAM_PTR(state->R[7]);

    do {
        const int32_t out_x = M11*in[0] + M12*in[1] + M13*in[2] + M14;
        const int32_t out_y = M21*in[0] + M22*in[1] + M23*in[2] + M24;
        const int32_t out_z = M31*in[0] + M32*in[1] + M33*in[2] + M34;
        *(int32_t *)&out[2] = RAM_SWAPL(out_z);
        *(int32_t *)&out[4] = RAM_SWAPL(out_x);
        *(int32_t *)&out[6] = RAM_SWAPL(out_y);
        uint32_t clip_flags = (out_z >= RAM_SWAPL(r6_ptr[5]) << 8) << 5
                            | (out_z <  RAM_SWAPL(r6_ptr[4]) << 8) << 4;
        if (!(clip_flags & 0x10)) {
            const float coord_mult = 256.0f / out_z;
            out[0] = ifloorf(coord_mult * out_x);
            out[1] = ifloorf(coord_mult * out_y);
            clip_flags |= (out[1] > ((const int16_t *)r6_ptr)[4]) << 3
                       |  (out[1] < ((const int16_t *)r6_ptr)[5]) << 2
                       |  (out[0] > ((const int16_t *)r6_ptr)[7]) << 1
                       |  (out[0] < ((const int16_t *)r6_ptr)[6]) << 0;
            state->cycles += 19;
        }
        *(uint32_t *)&out[12] = RAM_SWAPL(clip_flags);
        in += 3;
        out += 16;
    } while (--counter != 0);

    state->R[4] += 12;
    state->PC = state->PR;
}

/*-----------------------------------------------------------------------*/

/* 0x601FB70: Entry to another self-modified routine like 0x601EE60 */

static FASTCALL void Azel_0601FB70(SH2State *state)
{
    const uint16_t insn = HRAM_LOADW(0x601FB72, 0);
    if (insn>>12 == 0xA) {  // BRA instruction
        state->R[0] = 0x601FC94;
        state->MACL = state->MACH = 0;
        state->PC += 6 + ((int32_t)(insn<<20) >> 19);
        state->cycles += 4;
    } else {
        if (insn != 0x6103) {
            DMSG("WARNING: Wrong instruction at 0x601FB72 (%04X)", insn);
        }
        const int32_t *M_ptr =
            state->R[5] & 0x6000000 ? (int32_t *)HRAM_PTR(state->R[5])
                                    : (int32_t *)LRAM_PTR(state->R[5]);
        const int32_t in_x = HRAM_LOADL(0x601FC9C, 0);
        const int32_t in_y = HRAM_LOADL(0x601FC9C, 4);
        const int32_t in_z = HRAM_LOADL(0x601FC9C, 8);
        int16_t out_x = (RAM_SWAPL(M_ptr[ 0]) * in_x
                       + RAM_SWAPL(M_ptr[ 4]) * in_y
                       + RAM_SWAPL(M_ptr[ 8]) * in_z) >> 16;
        int16_t out_y = (RAM_SWAPL(M_ptr[ 1]) * in_x
                       + RAM_SWAPL(M_ptr[ 5]) * in_y
                       + RAM_SWAPL(M_ptr[ 9]) * in_z) >> 16;
        int16_t out_z = (RAM_SWAPL(M_ptr[ 2]) * in_x
                       + RAM_SWAPL(M_ptr[ 6]) * in_y
                       + RAM_SWAPL(M_ptr[10]) * in_z) >> 16;
        HRAM_STOREW(0x601FC94, 0, out_x);
        HRAM_STOREW(0x601FC94, 2, out_y);
        HRAM_STOREW(0x601FC94, 4, out_z);
        state->PC = state->PR;
        state->cycles += 58;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x6022E18: Short but difficult-to-optimize routine */

static FASTCALL void Azel_06022E18(SH2State *state)
{
    uint32_t cycles = state->cycles;

    int32_t counter = HRAM_LOADL(state->R[4], 4);
    if (counter > 0) {
        HRAM_STOREL(state->R[4], 4, counter-1);
        state->R[0] = HRAM_LOADL(state->R[4], 8);
        state->PC = state->PR;
        state->cycles = cycles + 9;
        return;
    }

    uint32_t index = HRAM_LOADL(state->R[4], 0);
    int32_t r0 = (int16_t)LRAM_LOADW(state->R[5], index*2);
    const uint32_t limit = state->R[6];
    if (index != 0) {
        HRAM_STOREL(state->R[4], 4, (r0 & 15) - 1);
        r0 &= -16;
        cycles += 5;
    } else {
        HRAM_STOREL(state->R[4], 4, 0);
        r0 <<= 4;
    }
    HRAM_STOREL(state->R[4], 8, r0);
    index++;
    if (index >= limit) {
        index = 0;
        cycles++;
    }
    HRAM_STOREL(state->R[4], 0, index);
    state->R[0] = r0;
    state->PC = state->PR;
    state->cycles = cycles + 21;
}

/*-----------------------------------------------------------------------*/

/* 0x6035xxx: Mathematical library routines */

static FASTCALL void Azel_06035530(SH2State *state)
{
    if (LIKELY(state->R[5] != 0)) {
        state->R[0] =
            ((int64_t)(int32_t)state->R[4] << 16) / (int32_t)state->R[5];
        state->cycles += 52;
    } else {
        state->R[0] = 0;
        state->cycles += 7;
    }
    state->PC = state->PR;
}

static FASTCALL void Azel_06035552(SH2State *state)
{
    if (LIKELY(state->R[6] != 0)) {
        state->R[0] =
            ((int64_t)(int32_t)state->R[4] * (int64_t)(int32_t)state->R[5])
            / (int32_t)state->R[6];
    }
    state->cycles += 15;
    state->PC = state->PR;
}

static FASTCALL void Azel_0603556C(SH2State *state)
{
#ifdef __mips__  // GCC's optimizer fails yet again...

    asm(".set push; .set noreorder\n"
# ifdef WORDS_BIGENDIAN  // Just for completeness
        "lh $v0, 18(%[state])\n"
        "lh $v1, 22(%[state])\n"
# else
        "lh $v0, 16(%[state])\n"
        "lh $v1, 20(%[state])\n"
# endif
        "lw $a1, 92(%[state])\n"
        "lw $a2, 84(%[state])\n"
        "mult $v0, $v1\n"
        "lw $v1, 24(%[state])\n"
        "addiu $a1, $a1, 15\n"
        "sw $a1, 92(%[state])\n"
        "sw $a2, 88(%[state])\n"
        /* 1 cycle wasted */
        "mflo $v0\n"
        "div $zero, $v0, $v1\n"  // $zero needed to avoid the div-by-zero check
        /* Up to 33 cycles wasted (sigh) */
        "lw $v0, 0(%[state])\n"
        "bnezl $v1, 1f\n"
        "mflo $v0\n"
        "1:\n"
        /* This is totally evil, but it works as long as GCC doesn't try to
         * set up any stack frames on us, and it lets the routine fit in
         * one cache line (barely). */
        "jr $ra\n"
        "sw $v0, 0(%[state])\n"
        ".set pop"
        : "=m" (*state)
        : [state] "r" (state)
        : "v0", "v1", "a1", "a2", "hi", "lo"
    );

#else  // !__mips__

    if (LIKELY(state->R[6] != 0)) {
        state->R[0] = ((int16_t)state->R[4] * (int16_t)state->R[5])
                      / (int32_t)state->R[6];
    }
    state->cycles += 15;
    state->PC = state->PR;

#endif
}

/*----------------------------------*/

static FASTCALL void Azel_06035A8C(SH2State *state)
{
    const uint32_t ptr = HRAM_LOADL(0x604AEA4, 0);
    HRAM_STOREL(0x604AEA4, 0, ptr + 48);
    state->R[4] = ptr;
    state->R[5] = ptr + 48;
    state->cycles += 9;
    return Azel_06035AA0(state);
}

static FASTCALL void Azel_06035A9C(SH2State *state)
{
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    state->cycles += 2;
    return Azel_06035AA0(state);
}

static FASTCALL void Azel_06035AA0(SH2State *state)
{
    const uint32_t *src = (const uint32_t *)HRAM_PTR(state->R[4]);
    uint32_t *dest = (uint32_t *)HRAM_PTR(state->R[5]);

    const uint32_t *src_limit = &src[12];
    for (; src < src_limit; src += 4, dest += 4) {
        const uint32_t word0 = src[0];
        const uint32_t word1 = src[1];
        const uint32_t word2 = src[2];
        const uint32_t word3 = src[3];
        dest[0] = word0;
        dest[1] = word1;
        dest[2] = word2;
        dest[3] = word3;
    }

    state->R[0] = state->R[5];
    state->PC = state->PR;
    state->cycles += 27;
}

/*----------------------------------*/

static ALWAYS_INLINE void Azel_06035B14_F00_common(
    SH2State *state, const int32_t *vector, const int32_t *matrix,
    int32_t *out_x_ptr, int32_t *out_y_ptr, int32_t *out_z_ptr);

static FASTCALL void Azel_06035B14(SH2State *state)
{
    const int32_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int32_t *)HRAM_PTR(state->R[4])
        : (const int32_t *)LRAM_PTR(state->R[4]);
    int32_t *r5_ptr = (int32_t *)HRAM_PTR(HRAM_LOADL(0x604AEA4, 0));

    state->PC = state->PR;
    state->cycles += 56;
    return Azel_06035B14_F00_common(state, r4_ptr, r5_ptr,
                                    &r5_ptr[3], &r5_ptr[7], &r5_ptr[11]);
}

static FASTCALL void Azel_06035F00(SH2State *state)
{
    const int32_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int32_t *)HRAM_PTR(state->R[4])
        : (const int32_t *)LRAM_PTR(state->R[4]);
    int32_t *r5_ptr = (int32_t *)HRAM_PTR(state->R[5]);
    const int32_t *r6_ptr = (const int32_t *)HRAM_PTR(HRAM_LOADL(0x604AEA4, 0));

    state->PC = state->PR;
    state->cycles += 54;
    return Azel_06035B14_F00_common(state, r4_ptr, r6_ptr,
                                    &r5_ptr[0], &r5_ptr[1], &r5_ptr[2]);
}

static FASTCALL void Azel_06035F04(SH2State *state)
{
    const int32_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int32_t *)HRAM_PTR(state->R[4])
        : (const int32_t *)LRAM_PTR(state->R[4]);
    int32_t *r5_ptr = (int32_t *)HRAM_PTR(state->R[5]);
    const int32_t *r6_ptr = (const int32_t *)HRAM_PTR(state->R[6]);

    state->PC = state->PR;
    state->cycles += 52;
    return Azel_06035B14_F00_common(state, r4_ptr, r6_ptr,
                                    &r5_ptr[0], &r5_ptr[1], &r5_ptr[2]);
}

static ALWAYS_INLINE void Azel_06035B14_F00_common(
    SH2State *state, const int32_t *vector, const int32_t *matrix,
    int32_t *out_x_ptr, int32_t *out_y_ptr, int32_t *out_z_ptr)
{
#ifdef PSP

    int32_t v_0, v_1, v_2, M_0, M_1, M_2, M_3, hi, lo;
    asm(".set push; .set noreorder\n"

        "lw %[v_0], 0(%[vector])\n"
        "lw %[M_0], 0(%[matrix])\n"
        "lw %[v_1], 4(%[vector])\n"
        "ror %[v_0], %[v_0], 16\n"
        "ror %[M_0], %[M_0], 16\n"
        "mult %[v_0], %[M_0]\n"
        "lw %[M_1], 4(%[matrix])\n"
        "ror %[v_1], %[v_1], 16\n"
        "ror %[M_1], %[M_1], 16\n"
        "madd %[v_1], %[M_1]\n"
        "lw %[v_2], 8(%[vector])\n"
        "lw %[M_2], 8(%[matrix])\n"
        "lw %[M_3], 12(%[matrix])\n"
        "ror %[v_2], %[v_2], 16\n"
        "ror %[M_2], %[M_2], 16\n"
        "madd %[v_2], %[M_2]\n"
        "lw %[M_0], 16(%[matrix])\n"
        "lw %[M_1], 20(%[matrix])\n"
        "lw %[M_2], 24(%[matrix])\n"
        "ror %[M_3], %[M_3], 16\n"
        "ror %[M_0], %[M_0], 16\n"
        "mfhi %[hi]\n"
        "mflo %[lo]\n"

        "mult %[v_0], %[M_0]\n"
        "ror %[M_1], %[M_1], 16\n"
        "srl %[lo], %[lo], 16\n"
        "ins %[lo], %[hi], 16, 16\n"
        "madd %[v_1], %[M_1]\n"
        "ror %[M_2], %[M_2], 16\n"
        "addu %[lo], %[M_3], %[lo]\n"
        "lw %[M_3], 28(%[matrix])\n"
        "ror %[lo], %[lo], 16\n"
        "sw %[lo], 0(%[out_x_ptr])\n"
        "madd %[v_2], %[M_2]\n"
        "lw %[M_0], 32(%[matrix])\n"
        "lw %[M_1], 36(%[matrix])\n"
        "lw %[M_2], 40(%[matrix])\n"
        "ror %[M_3], %[M_3], 16\n"
        "ror %[M_0], %[M_0], 16\n"
        "mfhi %[hi]\n"
        "mflo %[lo]\n"

        "mult %[v_0], %[M_0]\n"
        "ror %[M_1], %[M_1], 16\n"
        "srl %[lo], %[lo], 16\n"
        "ins %[lo], %[hi], 16, 16\n"
        "madd %[v_1], %[M_1]\n"
        "ror %[M_2], %[M_2], 16\n"
        "addu %[lo], %[M_3], %[lo]\n"
        "lw %[M_3], 44(%[matrix])\n"
        "ror %[lo], %[lo], 16\n"
        "sw %[lo], 0(%[out_y_ptr])\n"
        "madd %[v_2], %[M_2]\n"
        "ror %[M_3], %[M_3], 16\n"
        "mfhi %[hi]\n"
        "mflo %[lo]\n"
        "srl %[lo], %[lo], 16\n"
        "ins %[lo], %[hi], 16, 16\n"
        "addu %[lo], %[M_3], %[lo]\n"
        "ror %[lo], %[lo], 16\n"
        "sw %[lo], 0(%[out_z_ptr])\n"

        ".set pop"
        : "=m" (*out_x_ptr), "=m" (*out_y_ptr), "=m" (*out_z_ptr),
          [v_0] "=&r" (v_0), [v_1] "=&r" (v_1), [v_2] "=&r" (v_2),
          [M_0] "=&r" (M_0), [M_1] "=&r" (M_1), [M_2] "=&r" (M_2),
          [M_3] "=&r" (M_3), [hi] "=&r" (hi), [lo] "=&r" (lo)
        : [vector] "r" (vector), [matrix] "r" (matrix),
          [out_x_ptr] "r" (out_x_ptr), [out_y_ptr] "r" (out_y_ptr),
          [out_z_ptr] "r" (out_z_ptr)
        : "hi", "lo"
    );

#else // !PSP

    const int32_t temp0 =
        ((int64_t)RAM_SWAPL(vector[0]) * (int64_t)RAM_SWAPL(matrix[0])
       + (int64_t)RAM_SWAPL(vector[1]) * (int64_t)RAM_SWAPL(matrix[1])
       + (int64_t)RAM_SWAPL(vector[2]) * (int64_t)RAM_SWAPL(matrix[2])
       ) >> 16;
    *out_x_ptr = RAM_SWAPL(RAM_SWAPL(matrix[3]) + temp0);

    const int32_t temp1 =
        ((int64_t)RAM_SWAPL(vector[0]) * (int64_t)RAM_SWAPL(matrix[4])
       + (int64_t)RAM_SWAPL(vector[1]) * (int64_t)RAM_SWAPL(matrix[5])
       + (int64_t)RAM_SWAPL(vector[2]) * (int64_t)RAM_SWAPL(matrix[6])
       ) >> 16;
    *out_y_ptr = RAM_SWAPL(RAM_SWAPL(matrix[7]) + temp1);

    const int32_t temp2 =
        ((int64_t)RAM_SWAPL(vector[0]) * (int64_t)RAM_SWAPL(matrix[8])
       + (int64_t)RAM_SWAPL(vector[1]) * (int64_t)RAM_SWAPL(matrix[9])
       + (int64_t)RAM_SWAPL(vector[2]) * (int64_t)RAM_SWAPL(matrix[10])
       ) >> 16;
    *out_z_ptr = RAM_SWAPL(RAM_SWAPL(matrix[11]) + temp2);

#endif // PSP
}

/*----------------------------------*/

static ALWAYS_INLINE void Azel_06035xxx_rotate_common(
    int angle, int32_t *r5_ptr, unsigned int x_idx, unsigned int y_idx,
    int invert);

static FASTCALL void Azel_06035C18(SH2State *state)
{
    const int16_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int16_t *)HRAM_PTR(state->R[4])
        : (const int16_t *)LRAM_PTR(state->R[4]);
    const uint32_t r5 = HRAM_LOADL(0x604AEA4, 0);
    int32_t *r5_ptr = r5 & 0x6000000 ? (int32_t *)HRAM_PTR(r5)
                                     : (int32_t *)LRAM_PTR(r5);

    if (r4_ptr[4] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[4] & 0xFFF, r5_ptr, 0, 1, 0);
    }
    if (r4_ptr[2] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[2] & 0xFFF, r5_ptr, 0, 2, 1);
    }
    if (r4_ptr[0] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[0] & 0xFFF, r5_ptr, 1, 2, 0);
    }

    state->PC = state->PR;
    state->cycles += 290;
}

static FASTCALL void Azel_06035C3C(SH2State *state)
{
    const int16_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int16_t *)HRAM_PTR(state->R[4])
        : (const int16_t *)LRAM_PTR(state->R[4]);
    const uint32_t r5 = HRAM_LOADL(0x604AEA4, 0);
    int32_t *r5_ptr = r5 & 0x6000000 ? (int32_t *)HRAM_PTR(r5)
                                     : (int32_t *)LRAM_PTR(r5);

    if (r4_ptr[2] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[2] & 0xFFF, r5_ptr, 0, 2, 1);
    }
    if (r4_ptr[0] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[0] & 0xFFF, r5_ptr, 1, 2, 0);
    }
    if (r4_ptr[4] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[4] & 0xFFF, r5_ptr, 0, 1, 0);
    }

    state->PC = state->PR;
    state->cycles += 290;
}

static FASTCALL void Azel_06035C60(SH2State *state)
{
    const int16_t *r4_ptr = state->R[4] & 0x06000000
        ? (const int16_t *)HRAM_PTR(state->R[4])
        : (const int16_t *)LRAM_PTR(state->R[4]);
    const uint32_t r5 = HRAM_LOADL(0x604AEA4, 0);
    int32_t *r5_ptr = r5 & 0x6000000 ? (int32_t *)HRAM_PTR(r5)
                                     : (int32_t *)LRAM_PTR(r5);

    if (r4_ptr[2] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[4] & 0xFFF, r5_ptr, 0, 1, 0);
    }
    if (r4_ptr[1] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[2] & 0xFFF, r5_ptr, 0, 2, 1);
    }
    if (r4_ptr[0] & 0xFFF) {
        Azel_06035xxx_rotate_common(r4_ptr[0] & 0xFFF, r5_ptr, 1, 2, 0);
    }

    state->PC = state->PR;
    state->cycles += 290;
}

static FASTCALL void Azel_06035C84(SH2State *state)
{
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    return Azel_06035C96(state);
}

static FASTCALL void Azel_06035C90(SH2State *state)
{
    state->R[4] >>= 16;
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    return Azel_06035C96(state);
}

static FASTCALL void Azel_06035C96(SH2State *state)
{
    const int32_t angle = state->R[4] & 0xFFF;
    int32_t *r5_ptr =
        state->R[5] & 0x6000000 ? (int32_t *)HRAM_PTR(state->R[5])
                                : (int32_t *)LRAM_PTR(state->R[5]);
    Azel_06035xxx_rotate_common(angle, r5_ptr, 1, 2, 0);
    state->PC = state->PR;
    state->cycles += 88;
}

static FASTCALL void Azel_06035D24(SH2State *state)
{
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    return Azel_06035D36(state);
}

static FASTCALL void Azel_06035D30(SH2State *state)
{
    state->R[4] >>= 16;
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    return Azel_06035D36(state);
}

static FASTCALL void Azel_06035D36(SH2State *state)
{
    const int32_t angle = state->R[4] & 0xFFF;
    int32_t *r5_ptr =
        state->R[5] & 0x6000000 ? (int32_t *)HRAM_PTR(state->R[5])
                                : (int32_t *)LRAM_PTR(state->R[5]);
    Azel_06035xxx_rotate_common(angle, r5_ptr, 0, 2, 1);
    state->PC = state->PR;
    state->cycles += 96;
}

static FASTCALL void Azel_06035DD4(SH2State *state)
{
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    return Azel_06035DE6(state);
}

static FASTCALL void Azel_06035DE0(SH2State *state)
{
    state->R[4] >>= 16;
    state->R[5] = HRAM_LOADL(0x604AEA4, 0);
    return Azel_06035DE6(state);
}

static FASTCALL void Azel_06035DE6(SH2State *state)
{
    const int32_t angle = state->R[4] & 0xFFF;
    int32_t *r5_ptr =
        state->R[5] & 0x6000000 ? (int32_t *)HRAM_PTR(state->R[5])
                                : (int32_t *)LRAM_PTR(state->R[5]);
    Azel_06035xxx_rotate_common(angle, r5_ptr, 0, 1, 0);
    state->PC = state->PR;
    state->cycles += 87;
}

static ALWAYS_INLINE void Azel_06035xxx_rotate_common(
    int angle, int32_t *r5_ptr, unsigned int x_idx, unsigned int y_idx,
    int invert)
{
    const int32_t *sin_table = (const int32_t *)LRAM_PTR(0x216660);
    const int32_t *cos_table = (const int32_t *)LRAM_PTR(0x217660);
    const int32_t sin_angle = invert ? -RAM_SWAPL(sin_table[angle])
                                     : +RAM_SWAPL(sin_table[angle]);
    const int32_t cos_angle = RAM_SWAPL(cos_table[angle]);
    int32_t new_x, new_y;

#ifdef PSP

    int32_t old_x, old_y;

    asm(".set push; .set noreorder\n"

        "lw %[old_x], 0+%[x_ofs](%[r5_ptr])\n"
        "lw %[old_y], 0+%[y_ofs](%[r5_ptr])\n"
        "ror %[old_x], %[old_x], 16\n"
        "mult %[old_x], %[cos_angle]\n"
        "ror %[old_y], %[old_y], 16\n"
        "madd %[old_y], %[sin_angle]\n"
        "mfhi %[new_y]\n"
        "mflo %[new_x]\n"
        "mult %[old_x], %[nsin_angle]\n"
        "lw %[old_x], 16+%[x_ofs](%[r5_ptr])\n"
        /* We want ROR(HI<<16 | LO>>16, 16), but that's equivalent
         * to (HI & 0xFFFF) | (LO & 0xFFFF0000), which we can do
         * with a single INS instruction. */
        "ins %[new_x], %[new_y], 0, 16\n"
        "sw %[new_x], 0+%[x_ofs](%[r5_ptr])\n"
        "ror %[old_x], %[old_x], 16\n"
        "madd %[old_y], %[cos_angle]\n"
        "lw %[old_y], 16+%[y_ofs](%[r5_ptr])\n"
        "ror %[old_y], %[old_y], 16\n"
        "mfhi %[new_x]\n"
        "mflo %[new_y]\n"

        "mult %[old_x], %[cos_angle]\n"
        "ins %[new_y], %[new_x], 0, 16\n"
        "sw %[new_y], 0+%[y_ofs](%[r5_ptr])\n"
        "madd %[old_y], %[sin_angle]\n"
        "mfhi %[new_y]\n"
        "mflo %[new_x]\n"
        "mult %[old_x], %[nsin_angle]\n"
        "lw %[old_x], 32+%[x_ofs](%[r5_ptr])\n"
        "ins %[new_x], %[new_y], 0, 16\n"
        "sw %[new_x], 16+%[x_ofs](%[r5_ptr])\n"
        "ror %[old_x], %[old_x], 16\n"
        "madd %[old_y], %[cos_angle]\n"
        "lw %[old_y], 32+%[y_ofs](%[r5_ptr])\n"
        "ror %[old_y], %[old_y], 16\n"
        "mfhi %[new_x]\n"
        "mflo %[new_y]\n"

        "mult %[old_x], %[cos_angle]\n"
        "ins %[new_y], %[new_x], 0, 16\n"
        "sw %[new_y], 16+%[y_ofs](%[r5_ptr])\n"
        "madd %[old_y], %[sin_angle]\n"
        "mfhi %[new_y]\n"
        "mflo %[new_x]\n"
        "mult %[old_x], %[nsin_angle]\n"
        "ins %[new_x], %[new_y], 0, 16\n"
        "sw %[new_x], 32+%[x_ofs](%[r5_ptr])\n"
        "madd %[old_y], %[cos_angle]\n"
        "mfhi %[new_x]\n"
        "mflo %[new_y]\n"
        "ins %[new_y], %[new_x], 0, 16\n"
        "sw %[new_y], 32+%[y_ofs](%[r5_ptr])\n"

        ".set pop"
        : "=m" (r5_ptr[0*4 + x_idx]), "=m" (r5_ptr[0*4 + y_idx]),
          "=m" (r5_ptr[1*4 + x_idx]), "=m" (r5_ptr[1*4 + y_idx]),
          "=m" (r5_ptr[2*4 + x_idx]), "=m" (r5_ptr[2*4 + y_idx]),
          [new_x] "=&r" (new_x), [new_y] "=&r" (new_y),
          [old_x] "=&r" (old_x), [old_y] "=&r" (old_y)
        : [sin_angle] "r" (sin_angle), [cos_angle] "r" (cos_angle),
          [nsin_angle] "r" (-sin_angle), [r5_ptr] "r" (r5_ptr),
          [x_ofs] "i" (x_idx*4), [y_ofs] "i" (y_idx*4)
        : "hi", "lo"
    );

#else  // !PSP

    #define DOT2(x1,y1,x2,y2) \
        (((int64_t)(x1) * (int64_t)(x2) + (int64_t)(y1) * (int64_t)(y2)) >> 16)
    #define ROTATE(index) \
        new_x = DOT2(RAM_SWAPL(r5_ptr[index*4 + x_idx]), \
                     RAM_SWAPL(r5_ptr[index*4 + y_idx]), \
                     cos_angle, sin_angle);              \
        new_y = DOT2(RAM_SWAPL(r5_ptr[index*4 + x_idx]), \
                     RAM_SWAPL(r5_ptr[index*4 + y_idx]), \
                     -sin_angle, cos_angle);             \
        r5_ptr[index*4 + x_idx] = RAM_SWAPL(new_x);      \
        r5_ptr[index*4 + y_idx] = RAM_SWAPL(new_y)

    ROTATE(0);
    ROTATE(1);
    ROTATE(2);

    #undef DOT2
    #undef ROTATE

#endif
}

/*----------------------------------*/

static ALWAYS_INLINE void Azel_06035Exx_scale_common(
    int32_t scale, int32_t *r5_ptr);

static FASTCALL void Azel_06035E70(SH2State *state)
{
    uint32_t r5 = HRAM_LOADL(0x604AEA4, 0);
    int32_t *r5_ptr = r5 & 0x6000000 ? (int32_t *)HRAM_PTR(r5)
                                     : (int32_t *)LRAM_PTR(r5);
    Azel_06035Exx_scale_common(state->R[4], r5_ptr+0);
    state->PC = state->PR;
    state->cycles += 28;
}

static FASTCALL void Azel_06035EA0(SH2State *state)
{
    uint32_t r5 = HRAM_LOADL(0x604AEA4, 0);
    int32_t *r5_ptr = r5 & 0x6000000 ? (int32_t *)HRAM_PTR(r5)
                                     : (int32_t *)LRAM_PTR(r5);
    Azel_06035Exx_scale_common(state->R[4], r5_ptr+1);
    state->PC = state->PR;
    state->cycles += 28;
}

static FASTCALL void Azel_06035ED0(SH2State *state)
{
    uint32_t r5 = HRAM_LOADL(0x604AEA4, 0);
    int32_t *r5_ptr = r5 & 0x6000000 ? (int32_t *)HRAM_PTR(r5)
                                     : (int32_t *)LRAM_PTR(r5);
    Azel_06035Exx_scale_common(state->R[4], r5_ptr+2);
    state->PC = state->PR;
    state->cycles += 28;
}

static ALWAYS_INLINE void Azel_06035Exx_scale_common(
    int32_t scale, int32_t *r5_ptr)
{
#ifdef PSP

    int32_t a, b, c, hi, lo;

    asm(".set push; .set noreorder\n"

        "lw %[a], 0(%[r5_ptr])\n"
        "lw %[b], 16(%[r5_ptr])\n"
        "lw %[c], 32(%[r5_ptr])\n"
        "ror %[a], %[a], 16\n"
        "mult %[a], %[scale]\n"
        "ror %[b], %[b], 16\n"
        "ror %[c], %[c], 16\n"
        "mfhi %[hi]\n"
        "mflo %[lo]\n"
        "mult %[b], %[scale]\n"
        // As with rotation, we take a shortcut for ROR(HI<<16 | LO>>16, 16).
        "ins %[lo], %[hi], 0, 16\n"
        "sw %[lo], 0(%[r5_ptr])\n"
        "mfhi %[hi]\n"
        "mflo %[lo]\n"
        "mult %[c], %[scale]\n"
        "ins %[lo], %[hi], 0, 16\n"
        "sw %[lo], 16(%[r5_ptr])\n"
        "mfhi %[hi]\n"
        "mflo %[lo]\n"
        "ins %[lo], %[hi], 0, 16\n"
        "sw %[lo], 32(%[r5_ptr])\n"

        ".set pop"
        : "=m" (r5_ptr[0]), "=m" (r5_ptr[4]), "=m" (r5_ptr[8]),
          [a] "=&r" (a), [b] "=&r" (b), [c] "=&r" (c),
          [hi] "=&r" (hi), [lo] "=&r" (lo)
        : [scale] "r" (scale), [r5_ptr] "r" (r5_ptr)
        : "hi", "lo"
    );

#else  // !PSP

    r5_ptr[0] = RAM_SWAPL(((int64_t)RAM_SWAPL(r5_ptr[0]) * scale) >> 16);
    r5_ptr[4] = RAM_SWAPL(((int64_t)RAM_SWAPL(r5_ptr[4]) * scale) >> 16);
    r5_ptr[8] = RAM_SWAPL(((int64_t)RAM_SWAPL(r5_ptr[8]) * scale) >> 16);

#endif
}

/*-----------------------------------------------------------------------*/

/* 0x60360F0: Delay routine (not automatically foldable due to the BF) */

static FASTCALL void Azel_060360F0(SH2State *state)
{
    state->PC = state->PR;
    state->cycles += state->R[4]*4 + 1;
}

/*-----------------------------------------------------------------------*/

/* 0x603A22C: Wrapper for 0x603A242 that jumps in with a BRA */

static int Azel_0603A22C_detect(SH2State *state, uint32_t address,
                                const uint16_t *fetch)
{
    return fetch[0] == 0xA009  // bra 0x603A242
        && fetch[1] == 0xE700  // mov #0, r7
        ? 2 : 0;
}

static FASTCALL void Azel_0603A22C(SH2State *state)
{
    state->R[7] = 0;
    return Azel_0603A242(state);
}

/*----------------------------------*/

/* 0x603A242: CD command execution routine */

static int Azel_0603A242_state = 0;
static int Azel_0603A242_cmd51_count = 0;  // Count of sequential 0x51 commands

static FASTCALL void Azel_0603A242(SH2State *state)
{
    if (Azel_0603A242_state == 0) {
        const uint32_t r4 = state->R[4];
        uint16_t *ptr_6053278 = (uint16_t *)HRAM_PTR(0x6053278);
        *ptr_6053278 |= Cs2ReadWord(0x90008);
        if ((*ptr_6053278 & r4) != r4) {
            state->R[0] = -1;
            state->PC = state->PR;
            state->cycles += 77;
            return;
        }
        if (!(*ptr_6053278 & 1)) {
            state->R[0] = -2;
            state->PC = state->PR;
            state->cycles += 82;
            return;
        }

        Cs2WriteWord(0x90008, ~(r4 | 1));
        *ptr_6053278 &= ~1;
        const uint32_t r5 = state->R[5];
        uintptr_t r5_base = (uintptr_t)direct_pages[r5>>19];
        const uint16_t *r5_ptr = (const uint16_t *)(r5_base + r5);
        Cs2WriteWord(0x90018, r5_ptr[0]);
        Cs2WriteWord(0x9001C, r5_ptr[1]);
        Cs2WriteWord(0x90020, r5_ptr[2]);
        Cs2WriteWord(0x90024, r5_ptr[3]);
        state->cycles += 88;
        Azel_0603A242_state = 1;
        if (r5_ptr[0]>>8 == 0x51) {
            Azel_0603A242_cmd51_count++;
        } else if (r5_ptr[0]>>8 != 0) { // Command 0x00 doesn't reset the count
            Azel_0603A242_cmd51_count = 0;
        }
        return;
    }

    if (Azel_0603A242_state == 1) {
        uint32_t status = (uint16_t)Cs2ReadWord(0x90008);
        if (status & 1) {
            state->cycles += 23;
            Azel_0603A242_state = 2;
        } else {
            /* Technically a timeout loop, but we assume no timeouts */
            state->cycles = state->cycle_limit;
        }
        return;
    }

    if (Azel_0603A242_state == 2) {
        const uint32_t r6 = state->R[6];
        uintptr_t r6_base = (uintptr_t)direct_pages[r6>>19];
        uint16_t *r6_ptr = (uint16_t *)(r6_base + r6);
        const unsigned int CR1 = Cs2ReadWord(0x90018);
        const unsigned int CR2 = Cs2ReadWord(0x9001C);
        const unsigned int CR3 = Cs2ReadWord(0x90020);
        const unsigned int CR4 = Cs2ReadWord(0x90024);
        if (Azel_0603A242_cmd51_count >= 0 && CR4 == 0) {
            /* We're probably waiting for a sector and it hasn't arrived
             * yet, so consume enough cycles to get us to that next sector.
             * But be careful we don't wait an extra sector if the current
             * sector finished reading between executing the CS2 command
             * and retrieving the delay period. */
            const unsigned int usec_left = Cs2GetTimeToNextSector();
            if (usec_left > 0 && usec_left < (1000000/(75*2))*9/10) {
                uint32_t cycles_left = 0;
                if (yabsys.CurSH2FreqType == CLKTYPE_26MHZ) {
                    cycles_left = (26847 * usec_left) / 1000;
                } else {
                    cycles_left = (28637 * usec_left) / 1000;
                }
                state->cycles += cycles_left;
            }
        }
        r6_ptr[0] = CR1;
        r6_ptr[1] = CR2;
        r6_ptr[2] = CR3;
        r6_ptr[3] = CR4;
        uint16_t *dest = (uint16_t *)HRAM_PTR(0x605329C);
        *((uint8_t *)dest + 1) = CR1>>8;
        if (state->R[7]) {
            dest[2] = CR1<<8 | CR2>>8;
            dest[3] = CR2<<8 | CR3>>8;
            dest[4] = CR3 & 0xFF;
            dest[5] = CR4;
        }
        state->R[0] = 0;
#if defined(TRACE) || defined(TRACE_STEALTH) || defined(TRACE_LITE)
        state->R[1] = ~0xF0;
        state->R[2] = 0;
        state->R[3] = ~0xF0;
        state->R[4] = state->R[15] - 12;
        state->R[5] = 0x605329C;
        state->SR &= ~SR_T;
#endif
        state->PC = state->PR;
        state->cycles += 121;
        Azel_0603A242_state = 0;
        return;
    }
}

/*-----------------------------------------------------------------------*/

/* 0x603ABE0: The sole purpose of this routine seems to be to copy the
 * first sample in a streaming audio ring buffer over the last sample.
 * I don't know whether this is to work around an idiosyncrasy of the real
 * SCSP or what, but it causes glitches when using the ME because we try
 * to read the sample from an uncached address after writing it to a
 * cached address.  In any case, the function (as applied to audio data)
 * is meaningless for emulation, so we null it out entirely. */

static FASTCALL void Azel_0603ABE0(SH2State *state)
{
    if ((MappedMemoryReadLong(state->R[4]+0xA0) & 0x1FF00000) == 0x05A00000) {
        state->PC = state->PR;
        state->cycles += 23;
        return;
    }
    /* It's not touching sound RAM, so it must be doing something else.
     * Let it run normally. */
    state->R[0] = 0xEF;
    state->PC += 2;
    state->cycles++;
}

/*-----------------------------------------------------------------------*/

/* 0x603DD6E: CD read routine (actually a generalized copy routine, but
 * doesn't seem to be used for anything else) */

static FASTCALL void Azel_0603DD6E(SH2State *state)
{
    int32_t len = MappedMemoryReadLong(state->R[15]);
    uint32_t dest = state->R[4];

    if (UNLIKELY(state->R[5] != 1)
     || UNLIKELY(state->R[6] != 0x25818000)
     || UNLIKELY(state->R[7] != 0)
     || UNLIKELY(len <= 0)
     || UNLIKELY((len & 3) != 0)
    ) {
        state->SR &= ~SR_T;
        state->SR |= (state->R[4] == 0) << SR_T_SHIFT;
        state->PC += 2;
        state->cycles += 1;
        return;
    }

    state->PC = state->PR;
    state->cycles += 30 + len*2;

    const uint32_t dest_page = dest>>19;
    uint8_t *dest_base;

    dest_base = direct_pages[dest_page];
    if (dest_base) {
        Cs2RapidCopyT2(dest_base + dest, len/4);
        sh2_write_notify(dest, len);
        return;
    }

    dest_base = byte_direct_pages[dest_page];
    if (dest_base) {
        Cs2RapidCopyT1(dest_base + dest, len/4);
        return;
    }

    if ((dest & 0x1FF00000) == 0x05A00000) {
        Cs2RapidCopyT2(SoundRam + (dest & 0x7FFFF), len/4);
        M68KWriteNotify(dest & 0x7FFFF, len);
        return;
    }

    for (; len > 0; len -= 4, dest += 4) {
        const uint32_t word = MappedMemoryReadLong(0x25818000);
        MappedMemoryWriteLong(dest, word);
    }
}

/*************************************************************************/

#endif  // ENABLE_JIT

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
