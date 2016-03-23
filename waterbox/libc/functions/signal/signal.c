/* signal( int sig, void (*func)( int ) )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <signal.h>
#include <errno.h>

#ifndef REGTEST

void (*_PDCLIB_sigabrt)( int ) = SIG_DFL;
void (*_PDCLIB_sigfpe)( int )  = SIG_DFL;
void (*_PDCLIB_sigill)( int )  = SIG_DFL;
void (*_PDCLIB_sigint)( int )  = SIG_DFL;
void (*_PDCLIB_sigsegv)( int ) = SIG_DFL;
void (*_PDCLIB_sigterm)( int ) = SIG_DFL;

void (*signal( int sig, void (*func)( int ) ) )( int )
{
    void (*oldhandler)( int );
    if ( sig <= 0 || func == SIG_ERR )
    {
        return SIG_ERR;
    }
    switch ( sig )
    {
        case SIGABRT:
            oldhandler = _PDCLIB_sigabrt;
            _PDCLIB_sigabrt = func;
            break;
        case SIGFPE:
            oldhandler = _PDCLIB_sigfpe;
            _PDCLIB_sigfpe = func;
            break;
        case SIGILL:
            oldhandler = _PDCLIB_sigill;
            _PDCLIB_sigill = func;
            break;
        case SIGINT:
            oldhandler = _PDCLIB_sigint;
            _PDCLIB_sigint = func;
            break;
        case SIGSEGV:
            oldhandler = _PDCLIB_sigsegv;
            _PDCLIB_sigsegv = func;
            break;
        case SIGTERM:
            oldhandler = _PDCLIB_sigterm;
            _PDCLIB_sigterm = func;
            break;
        default:
            errno = EINVAL;
            return SIG_ERR;
    }
    return oldhandler;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* Testing covered by raise.c */
    return TEST_RESULTS;
}
#endif
