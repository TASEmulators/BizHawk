/* fopen( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdlib.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"
#include "_PDCLIB_glue.h"
#include <string.h>
#include <errno.h>

extern FILE * _PDCLIB_filelist;

FILE * fopen( const char * _PDCLIB_restrict filename, 
              const char * _PDCLIB_restrict mode )
{
    int imode = _PDCLIB_filemode( mode );
    
    if( imode == 0 || filename == NULL )
        return NULL;

    _PDCLIB_fd_t              fd;
    const _PDCLIB_fileops_t * ops;
    if(!_PDCLIB_open( &fd, &ops, filename, imode )) {
        return NULL;
    }

    FILE * f = _PDCLIB_fvopen( fd, ops, imode, filename );
    if(!f) {
        int saveErrno = errno;
        ops->close(fd);
        errno = saveErrno;
    }
    return f;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* Some of the tests are not executed for regression tests, as the libc on
       my system is at once less forgiving (segfaults on mode NULL) and more
       forgiving (accepts undefined modes).
    */
    FILE * fh;
    remove( testfile );
    TESTCASE_NOREG( fopen( NULL, NULL ) == NULL );
    TESTCASE( fopen( NULL, "w" ) == NULL );
    TESTCASE_NOREG( fopen( "", NULL ) == NULL );
    TESTCASE( fopen( "", "w" ) == NULL );
    TESTCASE( fopen( "foo", "" ) == NULL );
    TESTCASE_NOREG( fopen( testfile, "wq" ) == NULL ); /* Undefined mode */
    TESTCASE_NOREG( fopen( testfile, "wr" ) == NULL ); /* Undefined mode */
    TESTCASE( ( fh = fopen( testfile, "w" ) ) != NULL );
    TESTCASE( fclose( fh ) == 0 );
    TESTCASE( remove( testfile ) == 0 );
    return TEST_RESULTS;
}

#endif
