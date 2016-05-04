/* strlcat( char *, const char *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

#pragma weak strlcat = _PDCLIB_strlcat
size_t _PDCLIB_strlcat(
   char *restrict dst,
   const char *restrict src,
   size_t dstsize);

size_t _PDCLIB_strlcat(
   char *restrict dst,
   const char *restrict src,
   size_t dstsize)
{
    size_t needed = 0;
    size_t j = 0;

    while(dst[needed]) needed++;

    while(needed < dstsize && (dst[needed] = src[j]))
        needed++, j++;

    while(src[j++]) needed++;
    needed++;

    if (needed > dstsize && dstsize)
      dst[dstsize - 1] = 0;

    return needed;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char dstbuf[16];

    strcpy(dstbuf, "hi");
    TESTCASE_NOREG( strlcat(dstbuf, "", 16) == 3 );
    TESTCASE_NOREG( strlcat(dstbuf, "hi", 16) == 5 );
    TESTCASE_NOREG( strlcat(dstbuf, "hello, world", 16) == 17 );
    TESTCASE_NOREG( strlcat(dstbuf, "hi", 16) == 18 );
    TESTCASE_NOREG( strcmp(dstbuf, "hihihello, worl") == 0);
    return TEST_RESULTS;
}

#endif
