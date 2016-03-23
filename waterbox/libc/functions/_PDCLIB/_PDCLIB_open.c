/* _PDCLIB_open( char const * const, int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

/* This is a stub implementation of open.
*/

#include <stdio.h>
#include <errno.h>

#ifndef REGTEST
#include "_PDCLIB_glue.h"

bool _PDCLIB_open( _PDCLIB_fd_t * pFd, const _PDCLIB_fileops_t ** pOps,
                   char const * const filename, unsigned int mode )
{
    errno = ENOTSUP;
    return false;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <stdlib.h>
#include <string.h>

int main( void )
{
    return TEST_RESULTS;
}

#endif

