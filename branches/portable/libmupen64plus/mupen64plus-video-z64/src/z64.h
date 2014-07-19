/*
 * z64
 *
 * Copyright (C) 2007  ziggy
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *
**/

#ifndef _Z64_H_
#define _Z64_H_
#include <stdint.h>

#define DACRATE_NTSC    (48681812)
#define DACRATE_PAL     (49656530)
#define DACRATE_MPAL    (48628316)

#define SP_INTERRUPT    0x1
#define SI_INTERRUPT    0x2
#define AI_INTERRUPT    0x4
#define VI_INTERRUPT    0x8
#define PI_INTERRUPT    0x10
#define DP_INTERRUPT    0x20

#define SP_STATUS_HALT                  0x0001
#define SP_STATUS_BROKE                 0x0002
#define SP_STATUS_DMABUSY               0x0004
#define SP_STATUS_DMAFULL               0x0008
#define SP_STATUS_IOFULL                0x0010
#define SP_STATUS_SSTEP                 0x0020
#define SP_STATUS_INTR_BREAK    0x0040
#define SP_STATUS_SIGNAL0               0x0080
#define SP_STATUS_SIGNAL1               0x0100
#define SP_STATUS_SIGNAL2               0x0200
#define SP_STATUS_SIGNAL3               0x0400
#define SP_STATUS_SIGNAL4               0x0800
#define SP_STATUS_SIGNAL5               0x1000
#define SP_STATUS_SIGNAL6               0x2000
#define SP_STATUS_SIGNAL7               0x4000

#define DP_STATUS_XBUS_DMA              0x01
#define DP_STATUS_FREEZE                0x02
#define DP_STATUS_FLUSH                 0x04
#define DP_STATUS_START_GCLK            0x008
#define DP_STATUS_TMEM_BUSY             0x010
#define DP_STATUS_PIPE_BUSY             0x020
#define DP_STATUS_CMD_BUSY                      0x040
#define DP_STATUS_CBUF_READY            0x080
#define DP_STATUS_DMA_BUSY                      0x100
#define DP_STATUS_END_VALID             0x200
#define DP_STATUS_START_VALID           0x400

#define R4300i_SP_Intr 1


#define LSB_FIRST 1 // TODO : check for platform
#ifdef LSB_FIRST
        #define BYTE_ADDR_XOR           3
        #define WORD_ADDR_XOR           1
        #define BYTE4_XOR_BE(a)         ((a) ^ 3)                               /* read/write a byte to a 32-bit space */
#else
        #define BYTE_ADDR_XOR           0
        #define WORD_ADDR_XOR           0
        #define BYTE4_XOR_BE(a)         (a)
#endif


typedef uint64_t UINT64;
typedef int64_t INT64;
typedef uint32_t UINT32;
typedef int32_t INT32;
typedef uint16_t UINT16;
typedef int16_t INT16;
typedef uint8_t UINT8;
typedef int8_t INT8;
typedef unsigned int offs_t;
#endif

