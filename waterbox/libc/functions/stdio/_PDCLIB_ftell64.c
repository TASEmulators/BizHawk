/* _PDCLIB_ftell64( FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdint.h>
#include <limits.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

uint_fast64_t _PDCLIB_ftell64_unlocked( FILE * stream )
{
    /* ftell() must take into account:
       - the actual *physical* offset of the file, i.e. the offset as recognized
         by the operating system (and stored in stream->pos.offset); and
       - any buffers held by PDCLib, which
         - in case of unwritten buffers, count in *addition* to the offset; or
         - in case of unprocessed pre-read buffers, count in *substraction* to
           the offset. (Remember to count ungetidx into this number.)
       Conveniently, the calculation ( ( bufend - bufidx ) + ungetidx ) results
       in just the right number in both cases:
         - in case of unwritten buffers, ( ( 0 - unwritten ) + 0 )
           i.e. unwritten bytes as negative number
         - in case of unprocessed pre-read, ( ( preread - processed ) + unget )
           i.e. unprocessed bytes as positive number.
       That is how the somewhat obscure return-value calculation works.
    */

    /* ungetc on a stream at offset==0 will cause an overflow to UINT64_MAX.
     * C99/C11 says that the return value of ftell in this case is 
     * "indeterminate"
     */

    return ( stream->pos.offset - ( ( (int)stream->bufend - (int)stream->bufidx ) + (int)stream->ungetidx ) );
}

uint_fast64_t _PDCLIB_ftell64( FILE * stream )
{
  _PDCLIB_flockfile( stream );
  uint_fast64_t pos = _PDCLIB_ftell64_unlocked( stream );
  _PDCLIB_funlockfile( stream );
  return pos;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <stdlib.h>

int main( void )
{
    /* Tested by ftell */
    return TEST_RESULTS;
}

#endif

