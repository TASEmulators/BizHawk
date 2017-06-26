/*
  libco.amd64 (2016-09-14)
  author: byuu
  license: public domain
*/

#define LIBCO_C
#include "libco.h"

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>

static long long co_active_buffer[64];
static cothread_t co_active_handle = 0;

// allocations are 16k larger than asked for,
// and include guard space between the stack and the storage

static void* alloc_thread(size_t* size)
{
	// align up to 4k
	*size = (*size + 16384 + 4095) & ~4095;

	uint64_t* ptr = mmap(NULL, *size,
		PROT_READ | PROT_WRITE, MAP_ANONYMOUS, -1, 0);
	if (ptr == (uint64_t*)(-1))
		return NULL;

	ptr[512] = *size;
	for (int i = 513; i < 2048; i++)
		ptr[i] = 0xdeadbeefdeadbeef;

	if (mprotect(ptr + 512, 512 * 3 * sizeof(uint64_t), PROT_NONE) != 0)
		abort();

	return ptr;
}

static void free_thread(void* p)
{
	uint64_t* ptr = (uint64_t*)p;
	if (mprotect(ptr + 512, 512 * 3 * sizeof(uint64_t), PROT_READ | PROT_WRITE) != 0)
		abort();
	uint64_t size = ptr[512];
	memset(p, 0, size);
	if (munmap(ptr, size) != 0)
		abort();
}

extern void co_swap(cothread_t, cothread_t);

static void crash()
{
	assert(0); /* called only if cothread_t entrypoint returns */
}

void co_clean()
{
	memset(co_active_buffer, 0, sizeof(co_active_buffer));
}

cothread_t co_active()
{
	if (!co_active_handle)
		co_active_handle = &co_active_buffer;
	return co_active_handle;
}

cothread_t co_create(unsigned int sz, void (*entrypoint)(void))
{
	cothread_t handle;
	if (!co_active_handle)
		co_active_handle = &co_active_buffer;

	uint64_t size = sz;

	if (handle = (cothread_t)alloc_thread(&size))
	{
		uint64_t* p = (uint64_t*)((char*)handle + size); // seek to top of stack
		*--p = (uint64_t)crash;							 /* crash if entrypoint returns */
		*--p = (uint64_t)entrypoint;						 /* start of function */
		*(uint64_t*)handle = (uint64_t)p;				 /* stack pointer */
	}

	return handle;
}

void co_delete(cothread_t handle)
{
	free_thread(handle);
}

void co_switch(cothread_t handle)
{
	register cothread_t co_previous_handle = co_active_handle;
	co_swap(co_active_handle = handle, co_previous_handle);
}
