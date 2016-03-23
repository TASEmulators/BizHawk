#ifndef REGTEST
#include <threads.h>

int tss_create(tss_t *key, tss_dtor_t dtor)
{
	key->self  = key;
	key->value = NULL;
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
