/* [XSI] char * strndup( const char *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifdef REGTEST
#define _POSIX_C_SOURCE 200809L
#endif

#include <string.h>
#include <stdlib.h>

#ifndef REGTEST

char *strndup( const char * s, size_t len )
{
    char* ns = NULL;
    if(s) {
        ns = malloc(len + 1);
        if(ns) {
            ns[len] = 0;
            // strncpy to be pedantic about modification in multithreaded 
            // applications
            return strncpy(ns, s, len);
        }
    }
    return ns;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
#ifndef REGTEST
    /* Missing on Windows. Maybe use conditionals? */
    const char *teststr  = "Hello, world";
    const char *teststr2 = "\xFE\x8C\n";
    char *testres, *testres2;

    TESTCASE((testres  = strndup(teststr, 5)) != NULL);
    TESTCASE((testres2 = strndup(teststr2, 1)) != NULL);
    TESTCASE(strcmp(testres, teststr) != 0);
    TESTCASE(strncmp(testres, teststr, 5) == 0);
    TESTCASE(strcmp(testres2, teststr2) != 0);
    TESTCASE(strncmp(testres2, teststr2, 1) == 0);
    free(testres);
    free(testres2);
    TESTCASE((testres  = strndup(teststr, 20)) != NULL);
    TESTCASE((testres2 = strndup(teststr2, 5)) != NULL);
    TESTCASE(strcmp(testres, teststr) == 0);
    TESTCASE(strcmp(testres2, teststr2) == 0);
    free(testres);
    free(testres2);
#endif

    return TEST_RESULTS;
}

#endif
