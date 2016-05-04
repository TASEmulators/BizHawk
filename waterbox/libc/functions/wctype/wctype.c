/* wctype( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include <string.h>
#include "_PDCLIB_locale.h"

wctype_t wctype( const char * property )
{
    if(property) switch(property[0])
    {
        case 'a':
            if(strcmp(property, "alpha") == 0) {
                return _PDCLIB_CTYPE_ALPHA;
            } else if(strcmp(property, "alnum") == 0) {
                return _PDCLIB_CTYPE_ALPHA | _PDCLIB_CTYPE_DIGIT;
            } else return 0;

        case 'b':
            if(strcmp(property, "blank") == 0) {
                return _PDCLIB_CTYPE_BLANK;
            } else return 0;

        case 'c':
            if(strcmp(property, "cntrl") == 0) {
                return _PDCLIB_CTYPE_CNTRL;
            } else return 0;

        case 'd':
            if(strcmp(property, "digit") == 0) {
                return _PDCLIB_CTYPE_DIGIT;
            } else return 0;

        case 'g':
            if(strcmp(property, "graph") == 0) {
                return _PDCLIB_CTYPE_GRAPH;
            } else return 0;

        case 'l':
            if(strcmp(property, "lower") == 0) {
                return _PDCLIB_CTYPE_LOWER;
            } else return 0;

        case 'p':
            if(strcmp(property, "print") == 0) {
                return _PDCLIB_CTYPE_GRAPH | _PDCLIB_CTYPE_SPACE;
            } else if(strcmp(property, "punct") == 0) {
                return _PDCLIB_CTYPE_PUNCT;
            } else return 0;

        case 's':
            if(strcmp(property, "space") == 0) {
                return _PDCLIB_CTYPE_SPACE;
            } else return 0;

        case 'u':
            if(strcmp(property, "upper") == 0) {
                return _PDCLIB_CTYPE_UPPER;
            } else return 0;

        case 'x':
            if(strcmp(property, "xdigit") == 0) {
                return _PDCLIB_CTYPE_XDIGT;
            } else return 0;
    }
    return 0;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(wctype("")   == 0);
    TESTCASE_NOREG(wctype(NULL) == 0); // mingw libc crashes on this

    TESTCASE(wctype("alpha")  != 0);
    TESTCASE(wctype("alnum")  != 0);
    TESTCASE(wctype("blank")  != 0);
    TESTCASE(wctype("cntrl")  != 0);
    TESTCASE(wctype("digit")  != 0);
    TESTCASE(wctype("graph")  != 0);
    TESTCASE(wctype("lower")  != 0);
    TESTCASE(wctype("print")  != 0);
    TESTCASE(wctype("punct")  != 0);
    TESTCASE(wctype("space")  != 0);
    TESTCASE(wctype("upper")  != 0);
    TESTCASE(wctype("xdigit") != 0);

    TESTCASE_NOREG(wctype("alpha")  == _PDCLIB_CTYPE_ALPHA);
    TESTCASE_NOREG(wctype("alnum")  == (_PDCLIB_CTYPE_ALPHA | _PDCLIB_CTYPE_DIGIT));
    TESTCASE_NOREG(wctype("blank")  == _PDCLIB_CTYPE_BLANK);
    TESTCASE_NOREG(wctype("cntrl")  == _PDCLIB_CTYPE_CNTRL);
    TESTCASE_NOREG(wctype("digit")  == _PDCLIB_CTYPE_DIGIT);
    TESTCASE_NOREG(wctype("graph")  == _PDCLIB_CTYPE_GRAPH);
    TESTCASE_NOREG(wctype("lower")  == _PDCLIB_CTYPE_LOWER);
    TESTCASE_NOREG(wctype("print")  == (_PDCLIB_CTYPE_GRAPH | _PDCLIB_CTYPE_SPACE));
    TESTCASE_NOREG(wctype("punct")  == _PDCLIB_CTYPE_PUNCT);
    TESTCASE_NOREG(wctype("space")  == _PDCLIB_CTYPE_SPACE);
    TESTCASE_NOREG(wctype("upper")  == _PDCLIB_CTYPE_UPPER);
    TESTCASE_NOREG(wctype("xdigit") == _PDCLIB_CTYPE_XDIGT);
    return TEST_RESULTS;
}
#endif
