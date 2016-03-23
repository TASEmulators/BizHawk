#ifndef REGTEST
#include <threads.h>

int cnd_signal(cnd_t *cond)
{
	return thrd_success;
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
