#ifndef SOUND_BLARGG_H
#define SOUND_BLARGG_H

/* Uncomment to have Gb_Apu run at 4x normal clock rate (16777216 Hz), useful in
a Game Boy Advance emulator. */
#define GB_APU_OVERCLOCK 4

#ifndef STATIC_CAST
	#if __GNUC__ >= 4
		#define STATIC_CAST(T,expr) static_cast<T> (expr)
		#define CONST_CAST( T,expr) const_cast<T> (expr)
	#else
		#define STATIC_CAST(T,expr) ((T) (expr))
		#define CONST_CAST( T,expr) ((T) (expr))
	#endif
#endif

// BLARGG_COMPILER_HAS_BOOL: If 0, provides bool support for old compiler. If 1,
// compiler is assumed to support bool. If undefined, availability is determined.
#ifndef BLARGG_COMPILER_HAS_BOOL
	#if defined (__MWERKS__)
		#if !__option(bool)
			#define BLARGG_COMPILER_HAS_BOOL 0
		#endif
	#elif defined (_MSC_VER)
		#if _MSC_VER < 1100
			#define BLARGG_COMPILER_HAS_BOOL 0
		#endif
	#elif defined (__GNUC__)
		// supports bool
	#elif __cplusplus < 199711
		#define BLARGG_COMPILER_HAS_BOOL 0
	#endif
#endif
#if defined (BLARGG_COMPILER_HAS_BOOL) && !BLARGG_COMPILER_HAS_BOOL
	typedef int bool;
	const bool true  = 1;
	const bool false = 0;
#endif

/* HAVE_STDINT_H: If defined, use <stdint.h> for int8_t etc.*/
#if defined (HAVE_STDINT_H)
	#include <stdint.h>
/* HAVE_INTTYPES_H: If defined, use <stdint.h> for int8_t etc.*/
#elif defined (HAVE_INTTYPES_H)
	#include <inttypes.h>
#endif

// If expr yields non-NULL error string, returns it from current function,
// otherwise continues normally.
#undef  RETURN_ERR
#define RETURN_ERR( expr ) do {                         \
		const char * blargg_return_err_ = (expr);               \
		if ( blargg_return_err_ ) return blargg_return_err_;    \
	} while ( 0 )

#endif // #ifndef SOUND_BLARGG_H
