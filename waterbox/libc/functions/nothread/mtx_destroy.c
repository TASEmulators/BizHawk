#ifndef REGTEST
#include <threads.h>

void mtx_destroy(mtx_t *mtx)
{}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
