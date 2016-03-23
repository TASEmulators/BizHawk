/* wchar_t * wcsncpy( wchar_t *, const wchar_t * , size_t );

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t *wcsncpy( wchar_t * _PDCLIB_restrict s1, 
                  const wchar_t * _PDCLIB_restrict s2,
                  size_t n )
{
    wchar_t * rc = s1;
    while ( ( n > 0 ) && ( *s1++ = *s2++ ) )
    {
        /* Cannot do "n--" in the conditional as size_t is unsigned and we have
           to check it again for >0 in the next loop below, so we must not risk
           underflow.
        */
        --n;
    }
    /* Checking against 1 as we missed the last --n in the loop above. */
    while ( n-- > 1 )
    {
        *s1++ = '\0';
    }
    return rc;
}


#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
