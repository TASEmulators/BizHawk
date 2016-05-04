/* Format conversion of integer types <inttypes.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_INTTYPES_H
#define _PDCLIB_INTTYPES_H _PDCLIB_INTTYPES_H
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* This structure has a member quot and a member rem, of type intmax_t.
   The order of the members is platform-defined to allow the imaxdiv()
   function below to be implemented efficiently.
*/
typedef struct _PDCLIB_imaxdiv_t imaxdiv_t;

#define PRId8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, d ) )
#define PRId16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, d ) )
#define PRId32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, d ) )
#define PRId64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, d ) )

#define PRIdLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, d ) )
#define PRIdLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, d ) )
#define PRIdLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, d ) )
#define PRIdLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, d ) )

#define PRIdFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, d ) )
#define PRIdFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, d ) )
#define PRIdFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, d ) )
#define PRIdFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, d ) )

#define PRIdMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, d ) )
#define PRIdPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, d ) )

#define PRIi8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, i ) )
#define PRIi16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, i ) )
#define PRIi32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, i ) )
#define PRIi64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, i ) )

#define PRIiLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, i ) )
#define PRIiLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, i ) )
#define PRIiLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, i ) )
#define PRIiLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, i ) )

#define PRIiFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, i ) )
#define PRIiFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, i ) )
#define PRIiFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, i ) )
#define PRIiFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, i ) )

#define PRIiMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, i ) )
#define PRIiPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, i ) )

#define PRIo8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, o ) )
#define PRIo16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, o ) )
#define PRIo32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, o ) )
#define PRIo64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, o ) )

#define PRIoLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, o ) )
#define PRIoLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, o ) )
#define PRIoLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, o ) )
#define PRIoLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, o ) )

#define PRIoFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, o ) )
#define PRIoFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, o ) )
#define PRIoFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, o ) )
#define PRIoFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, o ) )

#define PRIoMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, o ) )
#define PRIoPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, o ) )

#define PRIu8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, u ) )
#define PRIu16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, u ) )
#define PRIu32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, u ) )
#define PRIu64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, u ) )

#define PRIuLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, u ) )
#define PRIuLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, u ) )
#define PRIuLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, u ) )
#define PRIuLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, u ) )

#define PRIuFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, u ) )
#define PRIuFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, u ) )
#define PRIuFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, u ) )
#define PRIuFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, u ) )

#define PRIuMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, u ) )
#define PRIuPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, u ) )

#define PRIx8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, x ) )
#define PRIx16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, x ) )
#define PRIx32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, x ) )
#define PRIx64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, x ) )

#define PRIxLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, x ) )
#define PRIxLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, x ) )
#define PRIxLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, x ) )
#define PRIxLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, x ) )

#define PRIxFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, x ) )
#define PRIxFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, x ) )
#define PRIxFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, x ) )
#define PRIxFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, x ) )

#define PRIxMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, x ) )
#define PRIxPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, x ) )

#define PRIX8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, X ) )
#define PRIX16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, X ) )
#define PRIX32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, X ) )
#define PRIX64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, X ) )

#define PRIXLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, X ) )
#define PRIXLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, X ) )
#define PRIXLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, X ) )
#define PRIXLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, X ) )

#define PRIXFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, X ) )
#define PRIXFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, X ) )
#define PRIXFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, X ) )
#define PRIXFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, X ) )

#define PRIXMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, X ) )
#define PRIXPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, X ) )

#define SCNd8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, d ) )
#define SCNd16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, d ) )
#define SCNd32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, d ) )
#define SCNd64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, d ) )

#define SCNdLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, d ) )
#define SCNdLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, d ) )
#define SCNdLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, d ) )
#define SCNdLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, d ) )

#define SCNdFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, d ) )
#define SCNdFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, d ) )
#define SCNdFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, d ) )
#define SCNdFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, d ) )

#define SCNdMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, d ) )
#define SCNdPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, d ) )

#define SCNi8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, i ) )
#define SCNi16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, i ) )
#define SCNi32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, i ) )
#define SCNi64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, i ) )

#define SCNiLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, i ) )
#define SCNiLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, i ) )
#define SCNiLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, i ) )
#define SCNiLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, i ) )

#define SCNiFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, i ) )
#define SCNiFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, i ) )
#define SCNiFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, i ) )
#define SCNiFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, i ) )

#define SCNiMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, i ) )
#define SCNiPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, i ) )

#define SCNo8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, o ) )
#define SCNo16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, o ) )
#define SCNo32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, o ) )
#define SCNo64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, o ) )

#define SCNoLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, o ) )
#define SCNoLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, o ) )
#define SCNoLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, o ) )
#define SCNoLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, o ) )

#define SCNoFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, o ) )
#define SCNoFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, o ) )
#define SCNoFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, o ) )
#define SCNoFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, o ) )

#define SCNoMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, o ) )
#define SCNoPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, o ) )

#define SCNu8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, u ) )
#define SCNu16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, u ) )
#define SCNu32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, u ) )
#define SCNu64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, u ) )

#define SCNuLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, u ) )
#define SCNuLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, u ) )
#define SCNuLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, u ) )
#define SCNuLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, u ) )

#define SCNuFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, u ) )
#define SCNuFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, u ) )
#define SCNuFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, u ) )
#define SCNuFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, u ) )

#define SCNuMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, u ) )
#define SCNuPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, u ) )

#define SCNx8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, x ) )
#define SCNx16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, x ) )
#define SCNx32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, x ) )
#define SCNx64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, x ) )

#define SCNxLEAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_8_CONV, x ) )
#define SCNxLEAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_16_CONV, x ) )
#define SCNxLEAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_32_CONV, x ) )
#define SCNxLEAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_64_CONV, x ) )

#define SCNxFAST8  _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST8_CONV, x ) )
#define SCNxFAST16 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST16_CONV, x ) )
#define SCNxFAST32 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST32_CONV, x ) )
#define SCNxFAST64 _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_FAST64_CONV, x ) )

#define SCNxMAX _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_MAX_CONV, x ) )
#define SCNxPTR _PDCLIB_symbol2string( _PDCLIB_concat( _PDCLIB_PTR_CONV, x ) )

/* Functions for greatest-width integer types */

/* Calculate the absolute value of j */
intmax_t imaxabs( intmax_t j ) _PDCLIB_nothrow;

/* Return quotient (quot) and remainder (rem) of an integer division in the
   imaxdiv_t struct.
*/
imaxdiv_t imaxdiv( intmax_t numer, intmax_t denom ) _PDCLIB_nothrow;

/* Seperate the character array nptr into three parts: A (possibly empty)
   sequence of whitespace characters, a character representation of an integer
   to the given base, and trailing invalid characters (including the terminating
   null character). If base is 0, assume it to be 10, unless the integer
   representation starts with 0x / 0X (setting base to 16) or 0 (setting base to
   8). If given, base can be anything from 0 to 36, using the 26 letters of the
   base alphabet (both lowercase and uppercase) as digits 10 through 35.
   The integer representation is then converted into the return type of the
   function. It can start with a '+' or '-' sign. If the sign is '-', the result
   of the conversion is negated.
   If the conversion is successful, the converted value is returned. If endptr
   is not a NULL pointer, a pointer to the first trailing invalid character is
   returned in *endptr.
   If no conversion could be performed, zero is returned (and nptr in *endptr,
   if endptr is not a NULL pointer). If the converted value does not fit into
   the return type, the functions return INTMAX_MIN, INTMAX_MAX, or UINTMAX_MAX,
   respectively, depending on the sign of the integer representation and the
   return type, and errno is set to ERANGE.
*/

/* These functions are equivalent to strtol() / strtoul() in <stdlib.h>, but on
   the potentially larger type.
*/
intmax_t strtoimax( const char * _PDCLIB_restrict nptr, char * * _PDCLIB_restrict endptr, int base ) _PDCLIB_nothrow;
uintmax_t strtoumax( const char * _PDCLIB_restrict nptr, char * * _PDCLIB_restrict endptr, int base ) _PDCLIB_nothrow;

/* These functions are equivalent to wcstol() / wcstoul() in <wchar.h>, but on
   the potentially larger type.
*/
/* TODO: Not _PDCLIB_nothrow? */
/*
intmax_t wcstoimax( const _PDCLIB_wchar_t * _PDCLIB_restrict nptr, _PDCLIB_wchar_t * * _PDCLIB_restrict endptr, int base );
uintmax_t wcstoumax( const _PDCLIB_wchar_t * _PDCLIB_restrict nptr, _PDCLIB_wchar_t * * _PDCLIB_restrict endptr, int base );
*/

#ifdef __cplusplus
}
#endif

#endif
