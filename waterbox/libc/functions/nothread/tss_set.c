#ifndef REGTEST
#include <threads.h>

int tss_set(tss_t key, void *val)
{
	key.self->value = val;
	return thrd_success;
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
