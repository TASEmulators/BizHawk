/* tmpnam( char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST

#include <string.h>
#include "_PDCLIB_io.h"

char * tmpnam( char * s )
{
    static char filename[ L_tmpnam ];
    FILE * file = tmpfile();
    if ( s == NULL )
    {
        s = filename;
    }
    strcpy( s, file->filename );
    fclose( file );
    return s;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <string.h>

int main( void )
{
    TESTCASE( strlen( tmpnam( NULL ) ) < L_tmpnam );
    return TEST_RESULTS;
}

#endif

