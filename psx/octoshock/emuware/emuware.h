#pragma once

#include <inttypes.h>
#include <stdint.h>

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

#ifdef _MSC_VER
  #define snprintf _snprintf
  #define vsnprintf _vsnprintf
  #define strcasecmp _stricmp
  #define strncasecmp _strnicmp
#endif

#define TRUE_1 1
#define FALSE_0 0

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
