#ifndef __MDFN_TYPES
#define __MDFN_TYPES

#define __STDC_LIMIT_MACROS 1

// Make sure this file is included BEFORE a few common standard C header files(stdio.h, errno.h, math.h, AND OTHERS, but this is not an exhaustive check, nor
// should it be), so that any defines in config.h that change header file behavior will work properly.
#if defined(EOF) || defined(EACCES) || defined(F_LOCK) || defined(NULL) || defined(O_APPEND) || defined(M_LOG2E)
 #error "Wrong include order for types.h"
#endif

// Yes, yes, I know:  There's a better place for including config.h than here, but I'm tired, and this should work fine. :b
#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <assert.h>
#include <inttypes.h>

typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;

typedef struct
{
 union
 {
  struct
  {
   #ifdef MSB_FIRST
   uint8   High;
   uint8   Low;
   #else
   uint8   Low;
   uint8   High;
   #endif
  } Union8;
  uint16 Val16;
 };
} Uuint16;

typedef struct
{
 union
 {
  struct
  {
   #ifdef MSB_FIRST
   Uuint16   High;
   Uuint16   Low;
   #else
   Uuint16   Low;
   Uuint16   High;
   #endif
  } Union16;
  uint32  Val32;
 };
} Uuint32;


#if PSS_STYLE==2

#define PSS "\\"
#define MDFN_PS '\\'

#elif PSS_STYLE==1

#define PSS "/"
#define MDFN_PS '/'

#elif PSS_STYLE==3

#define PSS "\\"
#define MDFN_PS '\\'

#elif PSS_STYLE==4

#define PSS ":" 
#define MDFN_PS ':'

#endif

typedef uint32   UTF32;  /* at least 32 bits */
typedef uint16  UTF16;  /* at least 16 bits */
typedef uint8   UTF8;   /* typically 8 bits */
typedef unsigned char   Boolean; /* 0 or 1 */

#ifndef FALSE
#define FALSE 0
#endif

#ifndef TRUE
#define TRUE 1
#endif

#undef require
#define require( expr ) assert( expr )

#if !defined(MSB_FIRST) && !defined(LSB_FIRST)
 #error "Define MSB_FIRST or LSB_FIRST!"
#endif

//#include "error.h"

#endif
