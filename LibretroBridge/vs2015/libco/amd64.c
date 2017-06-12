/*
libco.amd64 (2016-09-14)
author: byuu
license: public domain
*/

#define LIBCO_C
#include "libco.h"

#include <assert.h>
#include <stdlib.h>
#include <string.h>

#ifdef __cplusplus
extern "C" {
#endif

	static long long co_active_buffer[64];
	static cothread_t co_active_handle = 0;

	static void* smalloc(size_t size)
	{
		char* ret = malloc(size + 16);
		if (ret)
		{
			*(size_t*)ret = size;
			return ret + 16;
		}
		return NULL;
	}

	static void sfree(void* ptr)
	{
		char* original = (char*)ptr - 16;
		size_t size = *(size_t*)original + 16;
		memset(original, 0, size);
		free(original);
	}

	extern void co_swap(cothread_t, cothread_t);

	static void crash() {
		assert(0);  /* called only if cothread_t entrypoint returns */
	}

	void co_clean() {
		memset(co_active_buffer, 0, sizeof(co_active_buffer));
	}

	cothread_t co_active() {
		if (!co_active_handle) co_active_handle = &co_active_buffer;
		return co_active_handle;
	}

	cothread_t co_create(unsigned int size, void(*entrypoint)(void)) {
		cothread_t handle;
		if (!co_active_handle) co_active_handle = &co_active_buffer;
		size += 512;  /* allocate additional space for storage */
		size &= ~15;  /* align stack to 16-byte boundary */

		if (handle = (cothread_t)smalloc(size)) {
			long long *p = (long long*)((char*)handle + size);  /* seek to top of stack */
			*--p = (long long)crash;                            /* crash if entrypoint returns */
			*--p = (long long)entrypoint;                       /* start of function */
			*(long long*)handle = (long long)p;                 /* stack pointer */
		}

		return handle;
	}

	void co_delete(cothread_t handle) {
		sfree(handle);
	}

	void co_switch(cothread_t handle) {
		register cothread_t co_previous_handle = co_active_handle;
		co_swap(co_active_handle = handle, co_previous_handle);
	}

#ifdef __cplusplus
}
#endif
