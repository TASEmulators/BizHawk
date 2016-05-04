#ifndef REGTEST
#include <threads.h>

int cnd_wait(cnd_t *cond, mtx_t *mtx)
{
	return thrd_error;
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
