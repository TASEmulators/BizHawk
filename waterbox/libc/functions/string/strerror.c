/* strerror( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

#include "_PDCLIB_locale.h"

/* TODO: Doing this via a static array is not the way to do it. */
char * strerror( int errnum )
{
    if ( errnum >= _PDCLIB_ERRNO_MAX )
    {
        return (char *)"Unknown error";
    }
    else
    {
        return (char *)_PDCLIB_threadlocale()->_ErrnoStr[errnum];
    }
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <stdio.h>
#include <errno.h>

int main( void )
{
    TESTCASE( strerror( ERANGE ) != strerror( EDOM ) );
    return TEST_RESULTS;
}
#endif
