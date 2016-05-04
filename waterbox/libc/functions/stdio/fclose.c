/* fclose( FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdlib.h>
#include <errno.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"
#include <threads.h>

extern FILE * _PDCLIB_filelist;

int fclose( FILE * stream )
{
    FILE * current = _PDCLIB_filelist;
    FILE * previous = NULL;
    /* Checking that the FILE handle is actually one we had opened before. */
    while ( current != NULL )
    {
        if ( stream == current )
        {
            /* Flush buffer */
            if ( stream->status & _PDCLIB_FWRITE )
            {
                if ( _PDCLIB_flushbuffer( stream ) == EOF )
                {
                    /* Flush failed, errno already set */
                    return EOF;
                }
            }

            /* Release mutex*/
            mtx_destroy( &stream->lock );

            /* Close handle */
            stream->ops->close(stream->handle);

            /* Remove stream from list */
            if ( previous != NULL )
            {
                previous->next = stream->next;
            }
            else
            {
                _PDCLIB_filelist = stream->next;
            }
            /* Delete tmpfile() */
            if ( stream->status & _PDCLIB_DELONCLOSE )
            {
                remove( stream->filename );
            }
            /* Free user buffer (SetVBuf allocated) */
            if ( stream->status & _PDCLIB_FREEBUFFER )
            {
                free( stream->buffer );
            }
            /* Free stream */
            if ( ! ( stream->status & _PDCLIB_STATIC ) )
            {
                free( stream );
            }
            return 0;
        }
        previous = current;
        current = current->next;
    }

    errno = EINVAL;
    return -1;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
#ifndef REGTEST
    FILE * file1;
    FILE * file2;
    remove( testfile1 );
    remove( testfile2 );
    TESTCASE( _PDCLIB_filelist == stdin );
    TESTCASE( ( file1 = fopen( testfile1, "w" ) ) != NULL );
    TESTCASE( _PDCLIB_filelist == file1 );
    TESTCASE( ( file2 = fopen( testfile2, "w" ) ) != NULL );
    TESTCASE( _PDCLIB_filelist == file2 );
    TESTCASE( fclose( file2 ) == 0 );
    TESTCASE( _PDCLIB_filelist == file1 );
    TESTCASE( ( file2 = fopen( testfile2, "w" ) ) != NULL );
    TESTCASE( _PDCLIB_filelist == file2 );
    TESTCASE( fclose( file1 ) == 0 );
    TESTCASE( _PDCLIB_filelist == file2 );
    TESTCASE( fclose( file2 ) == 0 );
    TESTCASE( _PDCLIB_filelist == stdin );
    TESTCASE( remove( testfile1 ) == 0 );
    TESTCASE( remove( testfile2 ) == 0 );
#else
    puts( " NOTEST fclose() test driver is PDCLib-specific." );
#endif
    return TEST_RESULTS;
}

#endif

