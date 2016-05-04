#include <time.h>
#ifndef REGTEST

int timespec_get( struct timespec *ts, int base )
{
    return 0;
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
