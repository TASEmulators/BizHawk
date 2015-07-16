#pragma once

#include <inttypes.h>
#include <stdint.h>
#include <cstdlib>

#ifdef _MSC_VER
#define SIZEOF_CHAR sizeof(char)
#define SIZEOF_SHORT sizeof(short)
#define SIZEOF_INT sizeof(int)
#define SIZEOF_LONG sizeof(long)
#define SIZEOF_LONG_LONG sizeof(long long)
#define SIZEOF_OFF_T sizeof(void*)
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

#if defined(_MSC_VER)
	#define strncasecmp _strnicmp
	#define NO_CLONE
#endif

#if defined(_MSC_VER) && _MSC_VER < 1900
  #define snprintf _snprintf
  #define vsnprintf _vsnprintf
  #define strcasecmp _stricmp
	#define final
	#define noexcept
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

//http://stackoverflow.com/questions/1537964/visual-c-equivalent-of-gccs-attribute-packed
#ifdef _MSC_VER
#define EW_PACKED( ... ) __pragma( pack(push, 1) ) __VA_ARGS__  __pragma( pack(pop) )
#else
#define EW_PACKED( ... )  __VA_ARGS__ __attribute__((__packed__))
#endif
