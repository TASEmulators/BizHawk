/* [XSI] char * strdup( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifdef REGTEST
#define _POSIX_C_SOURCE 200809L
#endif

#include <string.h>
#include <stdlib.h>

#ifndef REGTEST

char *strdup(const char *s)
{
    char* ns = NULL;
    if(s) {
        size_t len = strlen(s) + 1;
        ns = malloc(len);
        if(ns)
            strncpy(ns, s, len);
    }
    return ns;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    const char *teststr  = "Hello, world";
    const char *teststr2 = "An alternative test string with non-7-bit characters \xFE\x8C\n";
    char *testres, *testres2;

    TESTCASE((testres  = strdup(teststr)) != NULL);
    TESTCASE((testres2 = strdup(teststr2)) != NULL);
    TESTCASE(strcmp(testres, teststr) == 0);
    TESTCASE(strcmp(testres2, teststr2) == 0);
    free(testres);
    free(testres2);

    return TEST_RESULTS;
}

#endif
