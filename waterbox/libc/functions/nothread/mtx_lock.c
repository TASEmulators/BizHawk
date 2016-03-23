#ifndef REGTEST
#include <threads.h>

int mtx_lock(mtx_t *mtx)
{
	(*mtx)++;
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
