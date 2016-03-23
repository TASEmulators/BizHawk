#ifndef REGTEST
#include <threads.h>

int mtx_init(mtx_t *mtx, int type)
{
	*mtx = 0;
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
