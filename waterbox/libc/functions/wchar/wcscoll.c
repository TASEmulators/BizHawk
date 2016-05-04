/* wcscoll( const wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

/* I did much searching as to how various people implement this.
 *
 * OpenBSD, NetBSD and Musl libc for Linux implement this as a call to wcscmp
 * and have various "todo" notices on this function, and on the other hand
 * glibc implements it as a 500 line function. FreeBSD has an implementation 
 * which kind of uses their single byte character strcoll data for the first
 * 256 characters, but looks incredibly fragile and likely to break.
 *
 * TL;DR: Nobody uses this, and this will probably work perfectly fine for you.
 */

int wcscoll( const wchar_t * s1, const wchar_t * s2 )
{
    return wcscmp(s1, s2);
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}
#endif
