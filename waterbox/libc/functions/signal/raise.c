/* raise( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <signal.h>

#ifndef REGTEST

#include <stdio.h>
#include <stdlib.h>

extern void (*_PDCLIB_sigabrt)( int );
extern void (*_PDCLIB_sigfpe)( int );
extern void (*_PDCLIB_sigill)( int );
extern void (*_PDCLIB_sigint)( int );
extern void (*_PDCLIB_sigsegv)( int );
extern void (*_PDCLIB_sigterm)( int );

int raise( int sig )
{
    void (*sighandler)( int );
    char const * message;
    switch ( sig )
    {
        case SIGABRT:
            sighandler = _PDCLIB_sigabrt;
            message = "Abnormal termination (SIGABRT)";
            break;
        case SIGFPE:
            sighandler = _PDCLIB_sigfpe;
            message = "Arithmetic exception (SIGFPE)";
            break;
        case SIGILL:
            sighandler = _PDCLIB_sigill;
            message = "Illegal instruction (SIGILL)";
            break;
        case SIGINT:
            sighandler = _PDCLIB_sigint;
            message = "Interactive attention signal (SIGINT)";
            break;
        case SIGSEGV:
            sighandler = _PDCLIB_sigsegv;
            message = "Invalid memory access (SIGSEGV)";
            break;
        case SIGTERM:
            sighandler = _PDCLIB_sigterm;
            message = "Termination request (SIGTERM)";
            break;
        default:
            fprintf( stderr, "Unknown signal #%d\n", sig );
            _Exit( EXIT_FAILURE );
    }
    if ( sighandler == SIG_DFL )
    {
        fputs( message, stderr );
        _Exit( EXIT_FAILURE );
    }
    else if ( sighandler != SIG_IGN )
    {
        sighandler = signal( sig, SIG_DFL );
        sighandler( sig );
    }
    return 0;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

#include <stdlib.h>

static volatile sig_atomic_t flag = 0;

static int expected_signal = 0;

static void test_handler( int sig )
{
    TESTCASE( sig == expected_signal );
    flag = 1;
}

int main( void )
{
    /* Could be other than SIG_DFL if you changed the implementation. */
    TESTCASE( signal( SIGABRT, SIG_IGN ) == SIG_DFL );
    /* Should be ignored. */
    TESTCASE( raise( SIGABRT ) == 0 );
    /* Installing test handler, old handler should be returned */
    TESTCASE( signal( SIGABRT, test_handler ) == SIG_IGN );
    /* Raising and checking SIGABRT */
    expected_signal = SIGABRT;
    TESTCASE( raise( SIGABRT ) == 0 );
    TESTCASE( flag == 1 );
    /* Re-installing test handler, should have been reset to default */
    /* Could be other than SIG_DFL if you changed the implementation. */
    TESTCASE( signal( SIGABRT, test_handler ) == SIG_DFL );
    /* Raising and checking SIGABRT */
    flag = 0;
    TESTCASE( raise( SIGABRT ) == 0 );
    TESTCASE( flag == 1 );
    /* Installing test handler for different signal... */
    TESTCASE( signal( SIGTERM, test_handler ) == SIG_DFL );
    /* Raising and checking SIGTERM */
    expected_signal = SIGTERM;
    TESTCASE( raise( SIGTERM ) == 0 );
    TESTCASE( flag == 1 );
    return TEST_RESULTS;
}
#endif
