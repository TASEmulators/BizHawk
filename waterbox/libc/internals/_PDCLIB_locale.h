/* PDCLib locale support <_PDCLIB_locale.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef __PDCLIB_LOCALE_H
#define __PDCLIB_LOCALE_H __PDCLIB_LOCALE_H

#include "_PDCLIB_int.h"

#include <locale.h>
#include <wctype.h>
#include <threads.h>
#include <stdlib.h>

#define _PDCLIB_LOCALE_METHOD_TSS           't'
#define _PDCLIB_LOCALE_METHOD_THREAD_LOCAL  'T'

#if !defined(_PDCLIB_LOCALE_METHOD)
    /* If undefined, no POSIX per thread locales */
    #define _PDCLIB_threadlocale() (&_PDCLIB_global_locale)
#elif _PDCLIB_LOCALE_METHOD == _PDCLIB_LOCALE_METHOD_TSS
    extern tss_t _PDCLIB_locale_tss;
    static inline locale_t _PDCLIB_threadlocale( void )
    {
        locale_t l = tss_get(_PDCLIB_locale_tss);
        if ( l == NULL )
            l = &_PDCLIB_global_locale;
        return l;
    }

    static inline void _PDCLIB_setthreadlocale( locale_t l )
    {
        if ( tss_set( _PDCLIB_locale_tss, l ) != thrd_success )
            abort();
    }
#elif _PDCLIB_LOCALE_METHOD == _PDCLIB_LOCALE_METHOD_THREAD_LOCAL
    extern thread_local locale_t _PDCLIB_locale_tls;
    #define _PDCLIB_threadlocale() ( _PDCLIB_locale_tls || &_PDCLIB_global_locale )
    static inline locale_t _PDCLIB_threadlocale( void )
    {
        locale_t l = _PDCLIB_locale_tls;
        if(l == NULL)
            l = &_PDCLIB_global_locale;
        return l;
    }

    static inline void _PDCLIB_setthreadlocale( locale_t l )
    {
        _PDCLIB_locale_tls = l;
    }
#else
    #error Locale TSS method unspecified
#endif

/* -------------------------------------------------------------------------- */
/* <ctype.h> lookup tables                                                    */
/* -------------------------------------------------------------------------- */

#define _PDCLIB_CTYPE_ALPHA   1
#define _PDCLIB_CTYPE_BLANK   2
#define _PDCLIB_CTYPE_CNTRL   4
#define _PDCLIB_CTYPE_GRAPH   8
#define _PDCLIB_CTYPE_PUNCT  16
#define _PDCLIB_CTYPE_SPACE  32
#define _PDCLIB_CTYPE_LOWER  64
#define _PDCLIB_CTYPE_UPPER 128
#define _PDCLIB_CTYPE_DIGIT 256
#define _PDCLIB_CTYPE_XDIGT 512

#define _PDCLIB_WCTRANS_TOLOWER 1
#define _PDCLIB_WCTRANS_TOUPPER 2

typedef struct _PDCLIB_ctype
{
    _PDCLIB_uint16_t flags;
    unsigned char upper;
    unsigned char lower;
    unsigned char collation;
} _PDCLIB_ctype_t;

typedef struct _PDCLIB_wcinfo
{
    _PDCLIB_wint_t   start;
    _PDCLIB_uint16_t length;
    _PDCLIB_uint16_t flags;
    _PDCLIB_wint_t   lower_delta;
    _PDCLIB_wint_t   upper_delta;
} _PDCLIB_wcinfo_t;

struct _PDCLIB_locale {
    const struct _PDCLIB_charcodec_t * _Codec;
    struct lconv                       _Conv;

    /* ctype / wctype */
    /* XXX: Maybe re-evaluate constness of these later on? */
    const _PDCLIB_wcinfo_t      *_WCType;
    _PDCLIB_size_t               _WCTypeSize;
    const _PDCLIB_ctype_t       *_CType; 

    /* perror/strerror */
    const char * const           _ErrnoStr[_PDCLIB_ERRNO_MAX];
};

extern const _PDCLIB_wcinfo_t _PDCLIB_wcinfo[];
extern const size_t           _PDCLIB_wcinfo_size;

static inline int _PDCLIB_wcinfo_cmp( const void * _key, const void * _obj )
{
    _PDCLIB_int32_t * key = (_PDCLIB_int32_t *) _key;
    _PDCLIB_wcinfo_t * obj = (_PDCLIB_wcinfo_t *) _obj;
    if ( *key < obj->start ) 
    {
        return -1;
    } 
    else if ( *key >= obj->start + obj->length )
    {
        return 1;
    }
    else
    {
        return 0;
    }
}

static inline _PDCLIB_wcinfo_t * _PDCLIB_wcgetinfo( locale_t l, _PDCLIB_int32_t num )
{
    _PDCLIB_wcinfo_t *info = (_PDCLIB_wcinfo_t*) 
        bsearch( &num, l->_WCType, l->_WCTypeSize, 
                 sizeof( l->_WCType[0] ), _PDCLIB_wcinfo_cmp );

    return info;
}

static inline wint_t _PDCLIB_unpackwint( wint_t wc )
{
    if( sizeof(_PDCLIB_wchar_t) == 2 && sizeof(_PDCLIB_wint_t) == 4 ) {
        /* On UTF-16 platforms, as an extension accept a "packed surrogate"
         * encoding. We accept the surrogate pairs either way
         */

        wint_t c = (wc & 0xF800F800);
        if(c == (_PDCLIB_wint_t) 0xD800DC00) {
            // MSW: Lead, LSW: Trail
            wint_t lead  = wc >> 16 & 0x3FF;
            wint_t trail = wc       & 0x3FF;
            wc = lead << 10 | trail;
        } else if(c == (_PDCLIB_wint_t) 0xDC00D800) {
            // MSW: Trail, LSW: Lead
            wint_t trail = wc >> 16 & 0x3FF;
            wint_t lead  = wc       & 0x3FF;
            wc = lead << 10 | trail;
        }

    }
    return wc;
}

/* Internal xlocale-style WCType API */
int _PDCLIB_iswalnum_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswalpha_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswblank_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswcntrl_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswdigit_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswgraph_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswlower_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswprint_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswpunct_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswspace_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswupper_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswxdigit_l( wint_t _Wc, locale_t l );
int _PDCLIB_iswctype_l( wint_t _Wc, wctype_t _Desc, locale_t l );
wint_t _PDCLIB_towlower_l( wint_t _Wc, locale_t l );
wint_t _PDCLIB_towupper_l( wint_t _Wc, locale_t l );
wint_t _PDCLIB_towctrans_l( wint_t _Wc, wctrans_t _Desc, locale_t l );

#endif
