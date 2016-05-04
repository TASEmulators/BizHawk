#ifndef REGTEST
#include <threads.h>

void tss_delete(tss_t key)
{
	key.self->self = NULL;
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

/* Tested in tss_get.c */
int main( void )
{
    return TEST_RESULTS;
}

#endif
