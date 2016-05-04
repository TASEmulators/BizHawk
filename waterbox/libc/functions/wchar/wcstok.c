/* wcstok( wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wcstok( wchar_t * _PDCLIB_restrict s1, 
                  const wchar_t * _PDCLIB_restrict s2,
                  wchar_t ** _PDCLIB_restrict ptr )
{
    const wchar_t * p = s2;

    if ( s1 != NULL )
    {
        /* new string */
        *ptr = s1;
    }
    else
    {
        /* old string continued */
        if ( *ptr == NULL )
        {
            /* No old string, no new string, nothing to do */
            return NULL;
        }
        s1 = *ptr;
    }

    /* skipping leading s2 characters */
    while ( *p && *s1 )
    {
        if ( *s1 == *p )
        {
            /* found seperator; skip and start over */
            ++s1;
            p = s2;
            continue;
        }
        ++p;
    }

    if ( ! *s1 )
    {
        /* no more to parse */
        return ( *ptr = NULL );
    }

    /* skipping non-s2 characters */
    *ptr = s1;
    while ( **ptr )
    {
        p = s2;
        while ( *p )
        {
            if ( **ptr == *p++ )
            {
                /* found seperator; overwrite with '\0', position *ptr, return */
                *(*ptr)++ = L'\0';
                return s1;
            }
        }
        ++(*ptr);
    }

    /* parsed to end of string */
    *ptr = NULL;
    return s1;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    // MinGW at least has a very nonconforming (different signature!) variety
    // of wcstok
#ifndef REGTEST
    wchar_t s[] = L"_a_bc__d_";
    wchar_t* state  = NULL;
    wchar_t* tokres;

    TESTCASE( ( tokres = wcstok( s, L"_", &state ) ) == &s[1] );
    TESTCASE( s[1] == L'a' );
    TESTCASE( s[2] == L'\0' );
    TESTCASE( ( tokres = wcstok( NULL, L"_", &state ) ) == &s[3] );
    TESTCASE( s[3] == L'b' );
    TESTCASE( s[4] == L'c' );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( ( tokres = wcstok( NULL, L"_", &state ) ) == &s[7] );
    TESTCASE( s[6] == L'_' );
    TESTCASE( s[7] == L'd' );
    TESTCASE( s[8] == L'\0' );
    TESTCASE( ( tokres = wcstok( NULL, L"_", &state ) ) == NULL );
    wcscpy( s, L"ab_cd" );
    TESTCASE( ( tokres = wcstok( s, L"_", &state ) ) == &s[0] );
    TESTCASE( s[0] == L'a' );
    TESTCASE( s[1] == L'b' );
    TESTCASE( s[2] == L'\0' );
    TESTCASE( ( tokres = wcstok( NULL, L"_", &state ) ) == &s[3] );
    TESTCASE( s[3] == L'c' );
    TESTCASE( s[4] == L'd' );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( ( tokres = wcstok( NULL, L"_", &state ) ) == NULL );
#endif
    return TEST_RESULTS;
}
#endif
