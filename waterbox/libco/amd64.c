/*
  libco.amd64 (2016-09-14)
  author: byuu
  license: public domain
*/

#include "libco.h"

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <emulibc.h>

// allocations are 16k larger than asked for,
// and include guard space between the stack and the storage

typedef struct {
	// used by coswap.s
	uint64_t jmp_buf[32];
	// lowest pointer in the stack; when we go below here, we'll hit the fake guard pages and overflow
	uint64_t* stack_bottom;
	// pointer just off the top (starting) end of the stack
	uint64_t* stack_top;
	// total size that went to mmap
	uint64_t mmap_size;
	uint64_t padding[477];
	// will be protected to unreadable, unwritable
	uint64_t guard[0x3000 / sizeof(uint64_t)];
	uint64_t stack[0];
} cothread_impl;

// the cothread that represents the real host thread we started from
static cothread_impl co_host_buffer;
// what cothread are we in right now
static cothread_impl* co_active_handle;

// a list of all real cothreads (does not include co_host_buffer)
#define MAX_THREADS 64
static cothread_impl* allthreads[MAX_THREADS];

static cothread_impl* alloc_thread(uint64_t size)
{
	cothread_impl** dest = NULL;
	for (int i = 0; i < MAX_THREADS; i++)
	{
		if (!allthreads[i])
		{
			dest = &allthreads[i];
			break;
		}
	}
	if (!dest)
		return NULL;

	// align up to 4k
	size = (size + 4095) & ~4095;

	uint64_t alloc_size = sizeof(cothread_impl) + size;

	cothread_impl* co = mmap(NULL, alloc_size,
		PROT_READ | PROT_WRITE, MAP_ANONYMOUS, -1, 0);

	if (co == (cothread_impl*)(-1))
		return NULL;
	
	co->stack_bottom = &co->stack[0];
	co->stack_top = &co->stack[size / sizeof(uint64_t)];
	co->mmap_size = alloc_size;

	for (int i = 0; i < 0x3000 / sizeof(uint64_t); i++)
		co->guard[i] = 0xdeadbeeffeedface;

	if (mprotect(&co->guard[0], sizeof(co->guard), PROT_NONE) != 0)
		abort();
	
	*dest = co;
	return co;
}

static void free_thread(cothread_impl* co)
{
	cothread_impl** src = NULL;
	for (int i = 0; i < MAX_THREADS; i++)
	{
		if (allthreads[i] == co)
		{
			src = &allthreads[i];
			break;
		}
	}
	if (!src)
		abort();

	if (mprotect(&co->guard[0], sizeof(co->guard), PROT_READ | PROT_WRITE) != 0)
		abort();
	uint64_t alloc_size = co->mmap_size;
	memset(co, 0, alloc_size);
	if (munmap(co, alloc_size) != 0)
		abort();

	*src = NULL;
}

extern void co_swap(cothread_t, cothread_t);

static void crash(void)
{
	assert(0); /* called only if cothread_t entrypoint returns */
}

ECL_EXPORT void co_clean(void)
{
	memset(&co_host_buffer, 0, sizeof(co_host_buffer));
}

ECL_EXPORT void co_probe(void)
{
	// VEH is delivered on the same thread that experienced the exception.
	// That means the stack has to be writable already, so the waterbox host's fault detector won't work
	// when we're on a readonly cothread stack.  So we conservatively probe this entire stack before switching to it.
	for (int i = 0; i < MAX_THREADS; i++)
	{
		if (allthreads[i])
		{
			uint64_t volatile* p = (uint64_t volatile*)allthreads[i]->stack_bottom;
			uint64_t* pend = allthreads[i]->stack_top;
			for (; p < pend; p++)
				*p = *p;
		}
	}
}

cothread_t co_active(void)
{
	if (!co_active_handle)
		co_active_handle = &co_host_buffer;
	return co_active_handle;
}

cothread_t co_create(unsigned int sz, void (*entrypoint)(void))
{
	cothread_impl* co;
	if (!co_active_handle)
		co_active_handle = &co_host_buffer;

	if ((co = alloc_thread(sz)))
	{
		uint64_t* p = co->stack_top; // seek to top of stack
		*--p = (uint64_t)crash; // crash if entrypoint returns
		*--p = (uint64_t)entrypoint; // start of function */
		*(uint64_t*)co = (uint64_t)p; // stack pointer
	}

	return co;
}

void co_delete(cothread_t handle)
{
	free_thread(handle);
}

static uint64_t hoststart;
static uint64_t hostend;

void co_switch(cothread_t handle)
{
	uint64_t start;
	uint64_t end;
	if (co_active_handle == &co_host_buffer)
	{
		// migrating off of real thread; save stack params
		__asm__("movq %%gs:0x08, %0": "=r"(end));
		__asm__("movq %%gs:0x10, %0": "=r"(start));
		hoststart = start;
		hostend = end;
	}
	if (handle == &co_host_buffer)
	{
		// migrating onto real thread; load stack params
		start = hoststart;
		end = hostend;
		hoststart = 0;
		hostend = 0;
	}
	else
	{
		// migrating onto cothread; compute its extents we allocated them
		cothread_impl* co = handle;
		start = (uintptr_t)co->stack_bottom;
		end = (uintptr_t)co->stack_top;
	}
	__asm__("movq %0, %%gs:0x08":: "r"(end));
	__asm__("movq %0, %%gs:0x10":: "r"(start));

	register cothread_t co_previous_handle = co_active_handle;
	co_swap(co_active_handle = handle, co_previous_handle);
}
