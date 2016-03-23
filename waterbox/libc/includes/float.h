/* Characteristics of floating types <float.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_FLOAT_H
#define _PDCLIB_FLOAT_H _PDCLIB_FLOAT_H
#include "_PDCLIB_float.h"

#define FLT_ROUNDS      _PDCLIB_FLT_ROUNDS
#define FLT_EVAL_METHOD _PDCLIB_FLT_EVAL_METHOD
#define DECIMAL_DIG     _PDCLIB_DECIMAL_DIG

/* Radix of exponent representation */
#define FLT_RADIX       _PDCLIB_FLT_RADIX

/* Number of base-FLT_RADIX digits in the significand of a float */
#define FLT_MANT_DIG    _PDCLIB_FLT_MANT_DIG

/* Number of decimal digits of precision in a float */
#define FLT_DIG         _PDCLIB_FLT_DIG

/* Difference between 1.0 and the minimum float greater than 1.0 */
#define FLT_EPSILON     _PDCLIB_FLT_EPSILON

/* Minimum int x such that FLT_RADIX**(x-1) is a normalised float */
#define FLT_MIN_EXP     _PDCLIB_FLT_MIN_EXP

/* Minimum normalised float */
#define FLT_MIN         _PDCLIB_FLT_MIN

/* Minimum int x such that 10**x is a normalised float */
#define FLT_MIN_10_EXP  _PDCLIB_FLT_MIN_10_EXP

/* Maximum int x such that FLT_RADIX**(x-1) is a representable float */
#define FLT_MAX_EXP     _PDCLIB_FLT_MAX_EXP

/* Maximum float */
#define FLT_MAX         _PDCLIB_FLT_MAX

/* Maximum int x such that 10**x is a representable float */
#define FLT_MAX_10_EXP  _PDCLIB_FLT_MAX_10_EXP


/* Number of base-FLT_RADIX digits in the significand of a double */
#define DBL_MANT_DIG    _PDCLIB_DBL_MANT_DIG

/* Number of decimal digits of precision in a double */
#define DBL_DIG         _PDCLIB_DBL_DIG

/* Difference between 1.0 and the minimum double greater than 1.0 */
#define DBL_EPSILON     _PDCLIB_DBL_EPSILON

/* Minimum int x such that FLT_RADIX**(x-1) is a normalised double */
#define DBL_MIN_EXP     _PDCLIB_DBL_MIN_EXP

/* Minimum normalised double */
#define DBL_MIN         _PDCLIB_DBL_MIN

/* Minimum int x such that 10**x is a normalised double */
#define DBL_MIN_10_EXP  _PDCLIB_DBL_MIN_10_EXP

/* Maximum int x such that FLT_RADIX**(x-1) is a representable double */
#define DBL_MAX_EXP     _PDCLIB_DBL_MAX_EXP

/* Maximum double */
#define DBL_MAX         _PDCLIB_DBL_MAX

/* Maximum int x such that 10**x is a representable double */
#define DBL_MAX_10_EXP  _PDCLIB_DBL_MAX_10_EXP


/* Number of base-FLT_RADIX digits in the significand of a long double */
#define LDBL_MANT_DIG   _PDCLIB_LDBL_MANT_DIG

/* Number of decimal digits of precision in a long double */
#define LDBL_DIG        _PDCLIB_LDBL_DIG

/* Difference between 1.0 and the minimum long double greater than 1.0 */
#define LDBL_EPSILON    _PDCLIB_LDBL_EPSILON

/* Minimum int x such that FLT_RADIX**(x-1) is a normalised long double */
#define LDBL_MIN_EXP    _PDCLIB_LDBL_MIN_EXP

/* Minimum normalised long double */
#define LDBL_MIN        _PDCLIB_LDBL_MIN

/* Minimum int x such that 10**x is a normalised long double */
#define LDBL_MIN_10_EXP _PDCLIB_LDBL_MIN_10_EXP

/* Maximum int x such that FLT_RADIX**(x-1) is a representable long double */
#define LDBL_MAX_EXP    _PDCLIB_LDBL_MAX_EXP

/* Maximum long double */
#define LDBL_MAX        _PDCLIB_LDBL_MAX

/* Maximum int x such that 10**x is a representable long double */
#define LDBL_MAX_10_EXP _PDCLIB_LDBL_MAX_10_EXP

#endif
