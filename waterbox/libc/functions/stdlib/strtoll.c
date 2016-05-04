/* strtoll( const char *, char * *, int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <limits.h>
#include <stdlib.h>

#ifndef REGTEST

#include <stdint.h>

long long int strtoll( const char * s, char ** endptr, int base )
{
    long long int rc;
    char sign = '+';
    const char * p = _PDCLIB_strtox_prelim( s, &sign, &base );
    if ( base < 2 || base > 36 ) return 0;
    if ( sign == '+' )
    {
        rc = (long long int)_PDCLIB_strtox_main( &p, (unsigned)base, (uintmax_t)LLONG_MAX, (uintmax_t)( LLONG_MAX / base ), (int)( LLONG_MAX % base ), &sign );
    }
    else
    {
        rc = (long long int)_PDCLIB_strtox_main( &p, (unsigned)base, (uintmax_t)LLONG_MIN, (uintmax_t)( LLONG_MIN / -base ), (int)( -( LLONG_MIN % base ) ), &sign );
    }
    if ( endptr != NULL ) *endptr = ( p != NULL ) ? (char *) p : (char *) s;
    return ( sign == '+' ) ? rc : -rc;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <errno.h>

int main( void )
{
    char * endptr;
    /* this, to base 36, overflows even a 256 bit integer */
    char overflow[] = "-ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ_";
    /* tricky border case */
    char tricky[] = "+0xz";
    errno = 0;
    /* basic functionality */
    TESTCASE( strtoll( "123", NULL, 10 ) == 123 );
    /* proper detecting of default base 10 */
    TESTCASE( strtoll( "456", NULL, 0 ) == 456 );
    /* proper functioning to smaller base */
    TESTCASE( strtoll( "14", NULL, 8 ) == 12 );
    /* proper autodetecting of octal */
    TESTCASE( strtoll( "016", NULL, 0 ) == 14 );
    /* proper autodetecting of hexadecimal, lowercase 'x' */
    TESTCASE( strtoll( "0xFF", NULL, 0 ) == 255 );
    /* proper autodetecting of hexadecimal, uppercase 'X' */
    TESTCASE( strtoll( "0Xa1", NULL, 0 ) == 161 );
    /* proper handling of border case: 0x followed by non-hexdigit */
    TESTCASE( strtoll( tricky, &endptr, 0 ) == 0 );
    TESTCASE( endptr == tricky + 2 );
    /* proper handling of border case: 0 followed by non-octdigit */
    TESTCASE( strtoll( tricky, &endptr, 8 ) == 0 );
    TESTCASE( endptr == tricky + 2 );
    /* errno should still be 0 */
    TESTCASE( errno == 0 );
    /* overflowing subject sequence must still return proper endptr */
    TESTCASE( strtoll( overflow, &endptr, 36 ) == LLONG_MIN );
    TESTCASE( errno == ERANGE );
    TESTCASE( ( endptr - overflow ) == 53 );
    /* same for positive */
    errno = 0;
    TESTCASE( strtoll( overflow + 1, &endptr, 36 ) == LLONG_MAX );
    TESTCASE( errno == ERANGE );
    TESTCASE( ( endptr - overflow ) == 53 );
    /* testing skipping of leading whitespace */
    TESTCASE( strtoll( " \n\v\t\f789", NULL, 0 ) == 789 );
    /* testing conversion failure */
    TESTCASE( strtoll( overflow, &endptr, 10 ) == 0 );
    TESTCASE( endptr == overflow );
    endptr = NULL;
    TESTCASE( strtoll( overflow, &endptr, 0 ) == 0 );
    TESTCASE( endptr == overflow );
    /* TODO: These tests assume two-complement, but conversion should work */
    /* for one-complement and signed magnitude just as well. Anyone having */
    /* a platform to test this on?                                         */
    errno = 0;
#if LLONG_MAX >> 62 == 1
    /* testing "even" overflow, i.e. base is power of two */
    TESTCASE( strtoll( "9223372036854775807", NULL, 0 ) == 0x7fffffffffffffff );
    TESTCASE( errno == 0 );
    TESTCASE( strtoll( "9223372036854775808", NULL, 0 ) == LLONG_MAX );
    TESTCASE( errno == ERANGE );
    errno = 0;
    TESTCASE( strtoll( "-9223372036854775807", NULL, 0 ) == (long long)0x8000000000000001 );
    TESTCASE( errno == 0 );
    TESTCASE( strtoll( "-9223372036854775808", NULL, 0 ) == LLONG_MIN );
    TESTCASE( errno == 0 );
    TESTCASE( strtoll( "-9223372036854775809", NULL, 0 ) == LLONG_MIN );
    TESTCASE( errno == ERANGE );
    /* TODO: test "odd" overflow, i.e. base is not power of two */
#elif LLONG_MAX >> 126 == 1
    /* testing "even" overflow, i.e. base is power of two */
    TESTCASE( strtoll( "170141183460469231731687303715884105728", NULL, 0 ) == 0x7fffffffffffffffffffffffffffffff );
    TESTCASE( errno == 0 );
    TESTCASE( strtoll( "170141183460469231731687303715884105729", NULL, 0 ) == LLONG_MAX );
    TESTCASE( errno == ERANGE );
    errno = 0;
    TESTCASE( strtoll( "-170141183460469231731687303715884105728", NULL, 0 ) == -0x80000000000000000000000000000001 );
    TESTCASE( errno == 0 );
    TESTCASE( strtoll( "-170141183460469231731687303715884105729", NULL, 0 ) == LLONG_MIN );
    TESTCASE( errno == 0 );
    TESTCASE( strtoll( "-170141183460469231731687303715884105730", NULL, 0 ) == LLONG_MIN );
    TESTCASE( errno == ERANGE );
    /* TODO: test "odd" overflow, i.e. base is not power of two */
#else
#error Unsupported width of 'long long' (neither 64 nor 128 bit).
#endif
    return TEST_RESULTS;
}

#endif
