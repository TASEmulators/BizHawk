/* atexit( void (*)( void ) )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

extern void (*_PDCLIB_exitstack[])( void );
extern size_t _PDCLIB_exitptr;

int atexit( void (*func)( void ) )
{
    if ( _PDCLIB_exitptr == 0 )
    {
        return -1;
    }
    else
    {
        _PDCLIB_exitstack[ --_PDCLIB_exitptr ] = func;
        return 0;
    }
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"
#include <assert.h>

static int flags[ 32 ];

static void counthandler( void )
{
    static int count = 0;
    flags[ count ] = count;
    ++count;
}

static void checkhandler( void )
{
    for ( int i = 0; i < 31; ++i )
    {
        assert( flags[ i ] == i );
    }
}

int main( void )
{
    TESTCASE( atexit( &checkhandler ) == 0 );
    for ( int i = 0; i < 31; ++i )
    {
        TESTCASE( atexit( &counthandler ) == 0 );
    }
    return TEST_RESULTS;
}

#endif
