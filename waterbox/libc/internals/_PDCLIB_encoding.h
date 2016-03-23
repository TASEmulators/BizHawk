/* Encoding support <_PDCLIB_encoding.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef __PDCLIB_ENCODING_H
#define __PDCLIB_ENCODING_H __PDCLIB_ENCODING_H

#include <uchar.h>

/* Must be cauued with bufsize >= 1, in != NULL, out != NULL, ps != NULL
 *
 * Converts a UTF-16 (char16_t) to a UCS4 (char32_t) value. Returns
 *   1, 2   : Valid character (converted to UCS-4)
 *   -1     : Encoding error
 *   -2     : Partial character (only lead surrogate in buffer)
 */
static inline int _PDCLIB_c16rtoc32(
            _PDCLIB_char32_t    *_PDCLIB_restrict   out, 
    const   _PDCLIB_char16_t    *_PDCLIB_restrict   in,
            _PDCLIB_size_t                          bufsize,
            _PDCLIB_mbstate_t   *_PDCLIB_restrict   ps  
)
{
    if(ps->_Surrogate) {
        // We already have a lead surrogate
        if((*in & ~0x3FF) != 0xDC00) {
            // Encoding error
            return -1;
        } else {
            // Decode and reset state
            *out = (ps->_Surrogate & 0x3FF) << 10 | (*in & 0x3FF);
            ps->_Surrogate = 0;
            return 1;
        }
    } if((*in & ~0x3FF) == 0xD800) {
        // Lead surrogate
        if(bufsize >= 2) {
            // Buffer big enough
            if((in[1] & ~0x3FF) != 0xDC00) {
                // Encoding error
                return -1;
            } else {
                *out = (in[0] & 0x3FF) << 10 | (in[1] & 0x3FF);
                return 2;
            }
        } else {
            // Buffer too small - update state
            ps->_Surrogate = *in;
            return -2;
        }
    } else {
        // BMP character
        *out = *in;
        return 1;
    }
}

static inline _PDCLIB_size_t _PDCLIB_c32rtoc16(
            _PDCLIB_wchar_t     *_PDCLIB_restrict   out,
    const   _PDCLIB_char32_t    *_PDCLIB_restrict   in,
            _PDCLIB_size_t                          bufsize,
            _PDCLIB_mbstate_t   *_PDCLIB_restrict   ps
)
{
    if(ps->_Surrogate) {
        *out = ps->_Surrogate;
        ps->_Surrogate = 0;
        return 0;
    }

    if(*in <= 0xFFFF) {
        // BMP character
        *out = *in;
        return 1;
    } else {
        // Supplementary plane character
        *out = 0xD800 | (*in >> 10);
        if(bufsize >= 2) {
            out[1] = 0xDC00 | (*in & 0x3FF);
            return 2;
        } else {
            ps->_Surrogate = 0xDC00 | (*in & 0x3FF);
            return 1;
        }
    }
}

struct _PDCLIB_charcodec_t {
    /* Reads at most *_P_insz code units from *_P_inbuf and writes the result 
     * into *_P_outbuf, writing at most *_P_outsz code units. Updates 
     * *_P_outbuf, *_P_outsz, *_P_inbuf, *_P_outsz with the resulting state
     *
     * If _P_outbuf is NULL, then the input must be processed but no output 
     * generated. _P_outsz may be processed as normal.
     *
     * Returns true if the conversion completed successfully (i.e. one of 
     * _P_outsize or _P_insize reached zero and no coding errors were 
     * encountered), else return false.
     */

    /* mbsinit. Mandatory. */
    _PDCLIB_bool (*__mbsinit)(const _PDCLIB_mbstate_t *_P_ps);

    /* UCS-4 variants. Mandatory. */

    _PDCLIB_bool (*__mbstoc32s)(
        _PDCLIB_char32_t       *_PDCLIB_restrict *_PDCLIB_restrict   _P_outbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict   _P_outsz,
        const char             *_PDCLIB_restrict *_PDCLIB_restrict   _P_inbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict   _P_insz,
        _PDCLIB_mbstate_t                        *_PDCLIB_restrict   _P_ps
    );

    _PDCLIB_bool (*__c32stombs)(
        char                   *_PDCLIB_restrict *_PDCLIB_restrict  _P_outbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict  _P_outsz,
        const _PDCLIB_char32_t *_PDCLIB_restrict *_PDCLIB_restrict  _P_inbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict  _P_insz,
        _PDCLIB_mbstate_t                        *_PDCLIB_restrict  _P_ps
    );

    /* UTF-16 variants; same as above except optional. 
     *
     * If not provided, _PDCLib will internally synthesize on top of the UCS-4
     * variants above, albeit at a performance cost.
     */

    _PDCLIB_bool (*__mbstoc16s)(
        _PDCLIB_char16_t       *_PDCLIB_restrict *_PDCLIB_restrict   _P_outbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict   _P_outsz,
        const char             *_PDCLIB_restrict *_PDCLIB_restrict   _P_inbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict   _P_insz,
        _PDCLIB_mbstate_t                        *_PDCLIB_restrict   _P_ps
    );

    _PDCLIB_bool (*__c16stombs)(
        char                   *_PDCLIB_restrict *_PDCLIB_restrict  _P_outbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict  _P_outsz,
        const _PDCLIB_char16_t *_PDCLIB_restrict *_PDCLIB_restrict  _P_inbuf,
        _PDCLIB_size_t                           *_PDCLIB_restrict  _P_insz,
        _PDCLIB_mbstate_t                        *_PDCLIB_restrict  _P_ps
    );

    size_t __mb_max;
};

/* mbstate _PendState values */
enum {
    /* Nothing pending; _PendChar ignored */
    _PendClear = 0, 

    /* Process the character stored in _PendChar before reading the buffer 
     * passed for the conversion
     */
    _PendPrefix = 1,
};

/* XXX Defining these here is temporary - will move to xlocale in future */
size_t mbrtoc16_l(
        char16_t    *_PDCLIB_restrict   pc16,
        const char  *_PDCLIB_restrict   s, 
        size_t                          n,
        mbstate_t   *_PDCLIB_restrict   ps,
_PDCLIB_locale_t     _PDCLIB_restrict   l);

size_t c16rtomb_l(
        char        *_PDCLIB_restrict   s, 
        char16_t                        c16, 
        mbstate_t   *_PDCLIB_restrict   ps,
_PDCLIB_locale_t     _PDCLIB_restrict   l);

size_t mbrtoc32_l(
        char32_t    *_PDCLIB_restrict   pc32,
        const char  *_PDCLIB_restrict   s, 
        size_t                          n,
        mbstate_t   *_PDCLIB_restrict   ps,
_PDCLIB_locale_t     _PDCLIB_restrict   l);

size_t c32rtomb_l(
        char        *_PDCLIB_restrict   s, 
        char32_t                        c32,
        mbstate_t   *_PDCLIB_restrict   ps,
_PDCLIB_locale_t     _PDCLIB_restrict   l);

#define _PDCLIB_WCHAR_ENCODING_UTF16 16
#define _PDCLIB_WCHAR_ENCODING_UCS4  32

#if !defined(_PDCLIB_WCHAR_ENCODING)
    #define _PDCLIB_WCHAR_ENCODING 0
#endif

#if _PDCLIB_WCHAR_ENCODING == _PDCLIB_WCHAR_ENCODING_UTF16
    #define _PDCLIB_mbrtocwc_l mbrtoc16_l
    #define _PDCLIB_mbrtocwc   mbrtoc16
    #define _PDCLIB_cwcrtomb_l c16rtomb_l
    #define _PDCLIB_cwcrtomb   c16rtomb
#elif _PDCLIB_WCHAR_ENCODING == _PDCLIB_WCHAR_ENCODING_UCS4
    #define _PDCLIB_mbrtocwc_l mbrtoc32_l
    #define _PDCLIB_mbrtocwc   mbrtoc32
    #define _PDCLIB_cwcrtomb_l c32rtomb_l
    #define _PDCLIB_cwcrtomb   c32rtomb
#else
    #error _PDCLIB_WCHAR_ENCODING not defined correctly
    #error Define to one of _PDCLIB_WCHAR_ENCODING_UCS4 or _PDCLIB_WCHAR_ENCODING_UTF16
#endif

#endif
