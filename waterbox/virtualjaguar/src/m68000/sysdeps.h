/*
 * UAE - The Un*x Amiga Emulator - CPU core
 *
 * Try to include the right system headers and get other system-specific
 * stuff right & other collected kludges.
 *
 * If you think about modifying this, think twice. Some systems rely on
 * the exact order of the #include statements. That's also the reason
 * why everything gets included unconditionally regardless of whether
 * it's actually needed by the .c file.
 *
 * Copyright 1996, 1997 Bernd Schmidt
 *
 * Adaptation to Hatari by Thomas Huth
 * Adaptation to Virtual Jagaur by James Hammons
 *
 * This file is distributed under the GNU Public License, version 3 or at
 * your option any later version. Read the file GPLv3 for details.
 *
 */

#ifndef UAE_SYSDEPS_H
#define UAE_SYSDEPS_H

#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <limits.h>

#include <stdarg.h>
#include <stdint.h>

#define ENUMDECL typedef enum
#define ENUMNAME(name) name

/* When using GNU C, make abort more useful.  */
#ifdef __GNUC__
#define abort() \
  do { \
    fprintf(stderr, "Internal error; file %s, line %d\n", __FILE__, __LINE__); \
    (abort) (); \
} while (0)
#endif

#ifndef STATIC_INLINE
#define STATIC_INLINE static __inline__
#endif

/*
 * You can specify numbers from 0 to 5 here. It is possible that higher
 * numbers will make the CPU emulation slightly faster, but if the setting
 * is too high, you will run out of memory while compiling.
 * Best to leave this as it is.
 */
#define CPU_EMU_SIZE 0

#endif /* ifndef UAE_SYSDEPS_H */
