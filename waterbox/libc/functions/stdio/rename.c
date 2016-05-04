/* rename( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_glue.h"

#include <string.h>

extern _PDCLIB_file_t * _PDCLIB_filelist;

int rename( const char * old, const char * new )
{
    FILE * current = _PDCLIB_filelist;
    while ( current != NULL )
    {
        if ( ( current->filename != NULL ) && ( strcmp( current->filename, old ) == 0 ) )
        {
            /* File of that name currently open. Do not rename. */
            return EOF;
        }
        current = current->next;
    }
    return _PDCLIB_rename( old, new );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <stdlib.h>

int main( void )
{
    FILE * file;
    remove( testfile1 );
    remove( testfile2 );
    /* make sure that neither file exists */
    TESTCASE( fopen( testfile1, "r" ) == NULL );
    TESTCASE( fopen( testfile2, "r" ) == NULL );
    /* rename file 1 to file 2 - expected to fail */
    TESTCASE( rename( testfile1, testfile2 ) != 0 );
    /* create file 1 */
    TESTCASE( ( file = fopen( testfile1, "w" ) ) != NULL );
    TESTCASE( fputs( "x", file ) != EOF );
    TESTCASE( fclose( file ) == 0 );
    /* check that file 1 exists */
    TESTCASE( ( file = fopen( testfile1, "r" ) ) != NULL );
    TESTCASE( fclose( file ) == 0 );
    /* rename file 1 to file 2 */
    TESTCASE( rename( testfile1, testfile2 ) == 0 );
    /* check that file 2 exists, file 1 does not */
    TESTCASE( fopen( testfile1, "r" ) == NULL );
    TESTCASE( ( file = fopen( testfile2, "r" ) ) != NULL );
    TESTCASE( fclose( file ) == 0 );
    /* create another file 1 */
    TESTCASE( ( file = fopen( testfile1, "w" ) ) != NULL );
    TESTCASE( fputs( "x", file ) != EOF );
    TESTCASE( fclose( file ) == 0 );
    /* check that file 1 exists */
    TESTCASE( ( file = fopen( testfile1, "r" ) ) != NULL );
    TESTCASE( fclose( file ) == 0 );
    /* rename file 1 to file 2 - expected to fail, see comment in
       _PDCLIB_rename() itself.
    */
    /* NOREG as glibc overwrites existing destination file. */
    TESTCASE_NOREG( rename( testfile1, testfile2 ) != 0 );
    /* remove both files */
    TESTCASE( remove( testfile1 ) == 0 );
    TESTCASE( remove( testfile2 ) == 0 );
    /* check that they're gone */
    TESTCASE( fopen( testfile1, "r" ) == NULL );
    TESTCASE( fopen( testfile2, "r" ) == NULL );
    return TEST_RESULTS;
}

#endif

