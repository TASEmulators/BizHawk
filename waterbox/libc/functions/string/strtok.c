/* strtok( char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

char * strtok( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2 )
{
    static char * tmp = NULL;
    const char * p = s2;

    if ( s1 != NULL )
    {
        /* new string */
        tmp = s1;
    }
    else
    {
        /* old string continued */
        if ( tmp == NULL )
        {
            /* No old string, no new string, nothing to do */
            return NULL;
        }
        s1 = tmp;
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
        return ( tmp = NULL );
    }

    /* skipping non-s2 characters */
    tmp = s1;
    while ( *tmp )
    {
        p = s2;
        while ( *p )
        {
            if ( *tmp == *p++ )
            {
                /* found seperator; overwrite with '\0', position tmp, return */
                *tmp++ = '\0';
                return s1;
            }
        }
        ++tmp;
    }

    /* parsed to end of string */
    tmp = NULL;
    return s1;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char s[] = "_a_bc__d_";
    TESTCASE( strtok( s, "_" ) == &s[1] );
    TESTCASE( s[1] == 'a' );
    TESTCASE( s[2] == '\0' );
    TESTCASE( strtok( NULL, "_" ) == &s[3] );
    TESTCASE( s[3] == 'b' );
    TESTCASE( s[4] == 'c' );
    TESTCASE( s[5] == '\0' );
    TESTCASE( strtok( NULL, "_" ) == &s[7] );
    TESTCASE( s[6] == '_' );
    TESTCASE( s[7] == 'd' );
    TESTCASE( s[8] == '\0' );
    TESTCASE( strtok( NULL, "_" ) == NULL );
    strcpy( s, "ab_cd" );
    TESTCASE( strtok( s, "_" ) == &s[0] );
    TESTCASE( s[0] == 'a' );
    TESTCASE( s[1] == 'b' );
    TESTCASE( s[2] == '\0' );
    TESTCASE( strtok( NULL, "_" ) == &s[3] );
    TESTCASE( s[3] == 'c' );
    TESTCASE( s[4] == 'd' );
    TESTCASE( s[5] == '\0' );
    TESTCASE( strtok( NULL, "_" ) == NULL );
    return TEST_RESULTS;
}
#endif
