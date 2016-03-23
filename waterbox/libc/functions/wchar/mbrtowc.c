/* mbrtowc( wchar_t * pwc, const char * s, size_t n, mbstate_t * ps )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>
#ifndef REGTEST
#include <errno.h>
#include <stdint.h>
#include <assert.h>
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

static size_t mbrtowc_l(
    wchar_t    *restrict    pwc, 
    const char *restrict    s, 
    size_t                  n, 
    mbstate_t  *restrict    ps,
    locale_t    restrict    l
)
{
    size_t res;

    if(s == NULL) {
        s = "";
        n = 1;
    }

    if(ps->_PendState == _PendPrefix) {
        res = _PDCLIB_mbrtocwc_l(pwc, &ps->_PendChar, 1, ps, l);
        switch(res) {
            case 0:
                // Converted the NUL character
                ps->_PendState = _PendClear;
                return 0;

            case 1:
                // Successful conversion
                ps->_PendChar = *s;
                return 1;

            case (size_t) -1:
                // Illegal sequence. mbrtocwc has already set errno.
                return (size_t) -1;

            case (size_t) -3:
                assert(!"Codec had buffered two characters");
                _PDCLIB_UNREACHABLE;
                return 0;

            case (size_t) -2:
                // Incomplete character, continue
                break;
        }
    }

    // _PendClear state
    res = _PDCLIB_mbrtocwc_l(pwc, s, n, ps, l);
    switch(res) {
        case (size_t) -3:
            // Converted entirely from internal state
            ps->_PendChar  = *s;
            ps->_PendState = _PendPrefix;
            return 1;
        default:
            return res;
    }
}

size_t mbrtowc(
    wchar_t *restrict pwc, 
    const char *restrict s, 
    size_t n, 
    mbstate_t *restrict ps
)
{
    static mbstate_t st;
    return mbrtowc_l(pwc, s, n, ps ? ps : &st, _PDCLIB_threadlocale());
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( NO_TESTDRIVER );
    return TEST_RESULTS;
}
#endif
