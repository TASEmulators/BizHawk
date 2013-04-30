/*  src/psp/common.h: Common header for PSP source files
    Copyright 2009 Andrew Church

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

#ifndef PSP_COMMON_H
#define PSP_COMMON_H

/**************************************************************************/

/* Various system headers */

#include <stdarg.h>
#include <stdint.h>
#include <stdio.h>
#define abs builtin_abs  // Avoid shadowing warnings for common identifiers
#define div builtin_div
#include <stdlib.h>
#undef abs
#undef div
#define index builtin_index
#include <string.h>
#undef index
#define remainder builtin_remainder
#define y0 builtin_y0
#define y1 builtin_y1
#include <math.h>
#undef remainder
#undef y0
#undef y1


#define u8 pspsdk_u8  // Avoid type collisions with ../core.h
#define s8 pspsdk_s8
#define u16 pspsdk_u16
#define s16 pspsdk_s16
#define u32 pspsdk_u32
#define s32 pspsdk_s32
#define u64 pspsdk_u64
#define s64 pspsdk_s64

#ifdef PSP
# include <pspuser.h>
# include <pspaudio.h>
# include <pspctrl.h>
# include <pspdisplay.h>
# include <pspge.h>
# include <pspgu.h>
# include <psppower.h>
# include <psputility.h>
# define PSP_SYSTEMPARAM_ID_INT_X_IS_CONFIRM  9  // Presumably, anyway
#endif

#undef u8
#undef s8
#undef u16
#undef s16
#undef u32
#undef s32
#undef u64
#undef s64

/* Helpful hints for GCC */
#if defined(__GNUC__) && defined(PSP)
extern void sceKernelExitGame(void) __attribute__((noreturn));
extern int sceKernelExitThread(int status) __attribute__((noreturn));
extern int sceKernelExitDeleteThread(int status) __attribute__((noreturn));
#endif

/**************************************************************************/

/* Thread priority constants */
enum {
    THREADPRI_MAIN      = 32,
    THREADPRI_CD_READ   = 25,
    THREADPRI_UTILITY   = 21,
    THREADPRI_SOUND     = 20,
    THREADPRI_SYSTEM_CB = 15,
};

/*----------------------------------*/

/* Program directory (determined from argv[0]) */
extern char progpath[256];

/* Saturn control pad handle (set at initialization time, and used by menu
 * code to change button assignments) */
extern void *padbits;

/* Flag indicating whether the ME is available for use */
extern int me_available;

/* Have we successfully initialized the Yabause core? */
extern int yabause_initted;

/**************************************************************************/

/* Convenience macros (not PSP-related, except DSTART/DEND) */

/*----------------------------------*/

/* Get the length of an array */
#define lenof(a)  (sizeof((a)) / sizeof((a)[0]))
/* Bound a value between two limits (inclusive) */
#define bound(x,low,high)  __extension__({  \
    typeof(x) __x = (x);                    \
    typeof(low) __low = (low);              \
    typeof(high) __high = (high);           \
    __x < __low ? __low : __x > __high ? __high : __x; \
})

/* Get offset of a structure member */
#undef offsetof
#ifdef __GNUC__
# define offsetof(type,member)  __builtin_offsetof(type,member)
#else
# define offsetof(type,member)  ((uintptr_t)&(((type *)0)->member))
#endif

/* Declare a function to be constant (i.e. not touching memory) */
#undef CONST_FUNCTION
#ifdef __GNUC__
# define CONST_FUNCTION  __attribute__((const))
#else
# define CONST_FUNCTION  /*nothing*/
#endif

/* Force a function to be inlined if possible (use in place of "inline") */
#ifdef __GNUC__
# define ALWAYS_INLINE  inline __attribute__((always_inline))
#else
# define ALWAYS_INLINE  inline
#endif

/* Prevent a function from being inlined */
#ifdef __GNUC__
# define NOINLINE  __attribute__((noinline))
#else
# define NOINLINE  /*nothing*/
#endif

/*----------------------------------*/

/* Convert a float to an int (optimized for PSP, but with alternate
 * versions for testing on other systems) */

#ifdef PSP

#define DEFINE_IFUNC(name,insn)                                         \
static inline CONST_FUNCTION int32_t name(const float x) {              \
    float dummy;                                                        \
    int32_t result;                                                     \
    asm(insn : [result] "=r" (result), [dummy] "=f" (dummy) : [x] "f" (x)); \
    return result;                                                      \
}

DEFINE_IFUNC(ifloorf, "floor.w.s %[dummy],%[x]; mfc1 %[result],%[dummy]")
DEFINE_IFUNC(iceilf,  "ceil.w.s  %[dummy],%[x]; mfc1 %[result],%[dummy]")
DEFINE_IFUNC(itruncf, "trunc.w.s %[dummy],%[x]; mfc1 %[result],%[dummy]")
DEFINE_IFUNC(iroundf, "round.w.s %[dummy],%[x]; mfc1 %[result],%[dummy]")

#elif HAVE_FLOORF

static inline CONST_FUNCTION int ifloorf(float x) {return (int)floorf(x);}
static inline CONST_FUNCTION int iceilf (float x) {return (int)ceilf(x);}
static inline CONST_FUNCTION int itruncf(float x) {return (int)truncf(x);}
static inline CONST_FUNCTION int iroundf(float x) {return (int)roundf(x);}

#else  // !PSP && !HAVE_FLOORF --> use double-precision floor() and ceil()

static inline CONST_FUNCTION int ifloorf(float x) {return (int)floor(x);}
static inline CONST_FUNCTION int iceilf (float x) {return (int)ceil(x);}
static inline CONST_FUNCTION int itruncf(float x)
    {return (x)<0 ? (int)-floor(-x) : (int)floor(x);}
static inline CONST_FUNCTION int iroundf(float x) {return (int)floor(x+0.5f);}

#endif

/*----------------------------------*/

#ifdef PSP_DEBUG

/* Debug/error message macro.  DMSG("message",...) prints to stderr a line
 * in the form:
 *     func_name(file:line): message
 * printf()-style format tokens and arguments are allowed, and no newline
 * is required at the end.  The format string must be a literal string
 * constant.  Note that we sprintf() into a buffer and write the buffer
 * rather than calling fprintf() directly because fprintf() makes multiple
 * low-level write calls, which can slow the program down significantly
 * depending on PSPlink's responsiveness on the PC host. */
/* global shared */ char DMSG_buffer[10000];
#define DMSG(msg,...)  do { \
    snprintf(DMSG_buffer, sizeof(DMSG_buffer), "%s(%s:%d): " msg "\n", \
             __FUNCTION__, __FILE__, __LINE__ , ## __VA_ARGS__); \
    fputs(DMSG_buffer, stderr); \
} while (0)

/* Timing macro.  Start timing with DSTART(); DEND() will then print the
 * elapsed time in microseconds.  Both must occur at the same level of
 * block nesting. */
#define DSTART() { const uint32_t __start = sceKernelGetSystemTimeLow()
#define DEND()     DMSG("time=%u", sceKernelGetSystemTimeLow() - __start); }

#else  // !PSP_DEBUG

/* Disable debug output */
#define DMSG(msg,...)  /*nothing*/
#define DSTART()       /*nothing*/
#define DEND()         /*nothing*/

#endif

/*----------------------------------*/

/* Test a precondition, and perform the given action if it fails */

#define PRECOND(condition,fail_action)  do {         \
    if (UNLIKELY(!(condition))) {                    \
        DMSG("PRECONDITION FAILED: %s", #condition); \
        fail_action;                                 \
    }                                                \
} while (0)

/**************************************************************************/

/* Include the Yabause core header for other common definitions/declarations */

#include "../core.h"

/**************************************************************************/

#endif  // PSP_COMMON_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
