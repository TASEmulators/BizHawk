/* _PDCLIB_strtox_prelim( const char *, char *, int * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>
#include <stddef.h>
#include <string.h>
#ifndef REGTEST
const char * _PDCLIB_strtox_prelim( const char * p, char * sign, int * base )
{
    /* skipping leading whitespace */
    while ( isspace( *p ) ) ++p;
    /* determining / skipping sign */
    if ( *p != '+' && *p != '-' ) *sign = '+';
    else *sign = *(p++);
    /* determining base */
    if ( *p == '0' )
    {
        ++p;
        if ( ( *base == 0 || *base == 16 ) && ( *p == 'x' || *p == 'X' ) )
        {
            *base = 16;
            ++p;
            /* catching a border case here: "0x" followed by a non-digit should
               be parsed as the unprefixed zero.
               We have to "rewind" the parsing; having the base set to 16 if it
               was zero previously does not hurt, as the result is zero anyway.
            */
            if ( memchr( _PDCLIB_digits, tolower(*p), *base ) == NULL )
            {
                p -= 2;
            }
        }
        else if ( *base == 0 )
        {
            *base = 8;
        }
        else
        {
            --p;
        }
    }
    else if ( ! *base )
    {
        *base = 10;
    }
    return ( ( *base >= 2 ) && ( *base <= 36 ) ) ? p : NULL;
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
#ifndef REGTEST
    int base = 0;
    char sign = '\0';
    char test1[] = "  123";
    char test2[] = "\t+0123";
    char test3[] = "\v-0x123";
    TESTCASE( _PDCLIB_strtox_prelim( test1, &sign, &base ) == &test1[2] );
    TESTCASE( sign == '+' );
    TESTCASE( base == 10 );
    base = 0;
    sign = '\0';
    TESTCASE( _PDCLIB_strtox_prelim( test2, &sign, &base ) == &test2[3] );
    TESTCASE( sign == '+' );
    TESTCASE( base == 8 );
    base = 0;
    sign = '\0';
    TESTCASE( _PDCLIB_strtox_prelim( test3, &sign, &base ) == &test3[4] );
    TESTCASE( sign == '-' );
    TESTCASE( base == 16 );
    base = 10;
    sign = '\0';
    TESTCASE( _PDCLIB_strtox_prelim( test3, &sign, &base ) == &test3[2] );
    TESTCASE( sign == '-' );
    TESTCASE( base == 10 );
    base = 1;
    TESTCASE( _PDCLIB_strtox_prelim( test3, &sign, &base ) == NULL );
    base = 37;
    TESTCASE( _PDCLIB_strtox_prelim( test3, &sign, &base ) == NULL );
#endif
    return TEST_RESULTS;
}

#endif
