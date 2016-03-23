#ifndef REGTEST
#include <threads.h>

int mtx_unlock(mtx_t *mtx)
{
	if(--(*mtx) >= 0)
        return thrd_success;
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
