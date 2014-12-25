#pragma once

#include <inttypes.h>
#include <stdint.h>
#include <cstdlib>

#ifdef _MSC_VER
typedef __int64 s64;
typedef __int32 s32;
typedef __int16 s16;
typedef __int8 s8;
typedef unsigned __int64 u64;
typedef unsigned __int32 u32;
typedef unsigned __int16 u16;
typedef unsigned __int8 u8;

typedef __int64 int64;
typedef __int32 int32;
typedef __int16 int16;
typedef __int8 int8;
typedef unsigned __int64 uint64;
typedef unsigned __int32 uint32;
typedef unsigned __int16 uint16;
typedef unsigned __int8 uint8;
#else
typedef __int64_t s64;
typedef __int32_t s32;
typedef __int16_t s16;
typedef __int8_t s8;
typedef __uint64_t u64;
typedef __uint32_t u32;
typedef __uint16_t u16;
typedef __uint8_t u8;

typedef __int64_t int64;
typedef __int32_t int32;
typedef __int16_t int16;
typedef __int8_t int8;
typedef __uint64_t uint64;
typedef __uint32_t uint32;
typedef __uint16_t uint16;
typedef __uint8_t uint8;
#endif

#define final
#define noexcept

#ifdef _MSC_VER
#include <intrin.h>
//http://stackoverflow.com/questions/355967/how-to-use-msvc-intrinsics-to-get-the-equivalent-of-this-gcc-code
//if needed
//uint32_t __inline ctz( uint32_t value )
//{
//    DWORD trailing_zero = 0;
//
//    if ( _BitScanForward( &trailing_zero, value ) )
//    {
//        return trailing_zero;
//    }
//    else
//    {
//        // This is undefined, I better choose 32 than 0
//        return 32;
//    }
//}

uint32 __inline __builtin_clz( uint32_t value )
{
    unsigned long leading_zero = 0;

    if ( _BitScanReverse( &leading_zero, value ) )
    {
       return 31 - leading_zero;
    }
    else
    {
         // Same remarks as above
         return 32;
    }
}
#endif

//#if MDFN_GCC_VERSION >= MDFN_MAKE_GCCV(4,7,0)
// #define MDFN_ASSUME_ALIGNED(p, align) __builtin_assume_aligned((p), (align))
//#else
// #define MDFN_ASSUME_ALIGNED(p, align) (p)
//#endif
#define MDFN_ASSUME_ALIGNED(p, align) (p)

//#define MDFN_WARN_UNUSED_RESULT __attribute__ ((warn_unused_result))
#define MDFN_WARN_UNUSED_RESULT

//#define MDFN_COLD __attribute__((cold))
#define MDFN_COLD 

//#define NO_INLINE __attribute__((noinline))
#define NO_INLINE

//#define MDFN_UNLIKELY(n) __builtin_expect((n) != 0, 0)
//#define MDFN_LIKELY(n) __builtin_expect((n) != 0, 1)
#define MDFN_UNLIKELY(n) (n)
#define MDFN_LIKELY(n) (n)

//#define MDFN_NOWARN_UNUSED __attribute__((unused))
#define MDFN_NOWARN_UNUSED

//#define MDFN_FORMATSTR(a,b,c) __attribute__ ((format (a, b, c)))
#define MDFN_FORMATSTR(a,b,c)

#define INLINE inline

#ifndef UNALIGNED
#define UNALIGNED
#endif

#ifdef _MSC_VER
  #define snprintf _snprintf
  #define vsnprintf _vsnprintf
  #define strcasecmp _stricmp
  #define strncasecmp _strnicmp
#endif

#define TRUE_1 1
#define FALSE_0 0

#ifndef ARRAY_SIZE
//taken from winnt.h
extern "C++" // templates cannot be declared to have 'C' linkage
template <typename T, size_t N>
char (*BLAHBLAHBLAH( UNALIGNED T (&)[N] ))[N];

#define ARRAY_SIZE(A) (sizeof(*BLAHBLAHBLAH(A)))
#endif

//------------alignment macros-------------
//dont apply these to types without further testing. it only works portably here on declarations of variables
//cant we find a pattern other people use more successfully?
#if defined(_MSC_VER) || defined(__INTEL_COMPILER)
#define EW_VAR_ALIGN(X) __declspec(align(X))
#elif defined(__GNUC__)
#define EW_VAR_ALIGN(X) __attribute__ ((aligned (X)))
#else
#error 
#endif
//---------------------------------------------

#ifdef EW_EXPORT
#undef EW_EXPORT
#define EW_EXPORT extern "C" __declspec(dllexport)
#else
#define EW_EXPORT extern "C" __declspec(dllimport)
#endif


#define SIZEOF_DOUBLE 8

#define LSB_FIRST

//no MSVC support, no use anyway??
#define override