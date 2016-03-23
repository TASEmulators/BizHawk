/* "C" Locale Support

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_CLOCALE_H
#define _PDCLIB_CLOCALE_H _PDCLIB_CLOCALE_H
#include <locale.h>
#ifdef __cplusplus
extern "C" {
#endif

void _PDCLIB_initclocale( locale_t l );

#ifdef __cplusplus
}
#endif
#endif // _PDCLIB_CLOCALE_H
