/* Diagnostics <assert.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include "_PDCLIB_aux.h"
#include "_PDCLIB_config.h"

/*
   Defines a macro assert() that, depending on the value of the preprocessor
   symbol NDEBUG, does
   * evaluate to a void expression if NDEBUG is set OR the parameter expression
     evaluates to true;
   * print an error message and terminates the program if NDEBUG is not set AND
     the parameter expression evaluates to false.
   The error message contains the parameter expression, name of the source file
  (__FILE__), line number (__LINE__), and (from C99 onward) name of the function
  (__func__).
    The header can be included MULTIPLE times, and redefines the macro depending
   on the current setting of NDEBUG.
*/

#ifndef _PDCLIB_ASSERT_H
#define _PDCLIB_ASSERT_H _PDCLIB_ASSERT_H

#ifdef __cplusplus
extern "C" {
#endif

/* Functions _NOT_ tagged noreturn as this hampers debugging */
void _PDCLIB_assert99( char const * const, char const * const, char const * const );
void _PDCLIB_assert89( char const * const );

#ifdef __cplusplus
}
#endif

#if _PDCLIB_C_VERSION >= 2011
#define static_assert _Static_assert
#else
#define static_assert( e, m )
#endif

#endif

/* If NDEBUG is set, assert() is a null operation. */
#undef assert

#ifdef NDEBUG
#define assert( ignore ) ( (void) 0 )
#elif _PDCLIB_C_MIN(99)
#define assert(expression) \
    do { if(!(expression)) { \
        _PDCLIB_assert99("Assertion failed: " _PDCLIB_symbol2string(expression)\
                         ", function ", __func__, \
                         ", file " __FILE__ \
                         ", line " _PDCLIB_symbol2string( __LINE__ ) \
                         "." _PDCLIB_endl ); \
        _PDCLIB_UNREACHABLE; \
      } \
    } while(0)

#else
#define assert(expression) \
    do { if(!(expression)) { \
        _PDCLIB_assert89("Assertion failed: " _PDCLIB_symbol2string(expression)\
                         ", file " __FILE__ \
                         ", line " _PDCLIB_symbol2string( __LINE__ ) \
                         "." _PDCLIB_endl ); \
        _PDCLIB_UNREACHABLE; \
      } \
    } while(0)
#endif
