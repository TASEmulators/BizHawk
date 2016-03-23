/* stdarg

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdarg.h>
#include <limits.h>
#include <float.h>
#ifdef TEST

#include "_PDCLIB_test.h"

typedef int (*intfunc_t)( void );

enum tag_t
{
    TAG_END,
    TAG_INT,
    TAG_LONG,
    TAG_LLONG,
    TAG_DBL,
    TAG_LDBL,
    TAG_INTPTR,
    TAG_LDBLPTR,
    TAG_FUNCPTR
};

static int dummy( void )
{
    return INT_MAX;
}

static int test( enum tag_t s, ... )
{
    enum tag_t tag = s;
    va_list ap;
    va_start( ap, s );
    for (;;)
    {
        switch ( tag )
        {
            case TAG_INT:
            {
                TESTCASE( va_arg( ap, int ) == INT_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_LONG:
            {
                TESTCASE( va_arg( ap, long ) == LONG_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_LLONG:
            {
                TESTCASE( va_arg( ap, long long ) == LLONG_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_DBL:
            {
                TESTCASE( va_arg( ap, double ) == DBL_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_LDBL:
            {
                TESTCASE( va_arg( ap, long double ) == LDBL_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_INTPTR:
            {
                TESTCASE( *( va_arg( ap, int * ) ) == INT_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_LDBLPTR:
            {
                TESTCASE( *( va_arg( ap, long double * ) ) == LDBL_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_FUNCPTR:
            {
                intfunc_t function;
                TESTCASE( ( function = va_arg( ap, intfunc_t ) ) == dummy );
                TESTCASE( function() == INT_MAX );
                tag = va_arg( ap, enum tag_t );
                break;
            }
            case TAG_END:
            {
                va_end( ap );
                return 0;
            }
        }
    }
}

int main( void )
{
    int x = INT_MAX;
    long double d = LDBL_MAX;
    test( TAG_END );
    test( TAG_INT, INT_MAX, TAG_END );
    test( TAG_LONG, LONG_MAX, TAG_LLONG, LLONG_MAX, TAG_END );
    test( TAG_DBL, DBL_MAX, TAG_LDBL, LDBL_MAX, TAG_END );
    test( TAG_INTPTR, &x, TAG_LDBLPTR, &d, TAG_FUNCPTR, dummy, TAG_END );
    return TEST_RESULTS;
}
#endif
