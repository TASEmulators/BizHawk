#ifndef REGTEST
#include <threads.h>

void thrd_yield(void)
{
	/* does nothing */
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
