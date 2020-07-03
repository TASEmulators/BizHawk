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

// allocations are 16k larger than asked for, which is all used as guard space
#define GUARD_SIZE 0x4000

typedef struct {
	// used by coswap.s, has to be at the beginning of the struct
	struct {
		uint64_t rsp;
		uint64_t rbp;
		uint64_t rbx;
		uint64_t r12;
		uint64_t r13;
		uint64_t r14;
		uint64_t r15;
	} jmp_buf;
	// points to the lowest address in the stack
	// NB: because of guard space, this is not valid stack
	void* stack;
	// length of the stack that we allocated in bytes
	uint64_t stack_size;
} cothread_impl;

// the cothread that represents the real host thread we started from
static cothread_impl co_host_buffer;
// what cothread are we in right now
static cothread_impl* co_active_handle;

static void free_thread(cothread_impl* co)
{
	if (munmap(co->stack, co->stack_size) != 0)
		abort();
	free(co);
}

static cothread_impl* alloc_thread(uint64_t size)
{
	cothread_impl* co = calloc(1, sizeof(*co));
	if (!co)
		return NULL;

	// align up to 4k
	size = (size + 4095) & ~4095ul;
	size += GUARD_SIZE;

	co->stack = mmap(NULL, size,
		PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS | MAP_STACK, -1, 0);
	
	if (co->stack == (void*)(-1))
	{
		free(co);
		return NULL;
	}

	if (mprotect(co->stack, GUARD_SIZE, PROT_NONE) != 0)
	{
		free_thread(co);
		return NULL;
	}

	co->stack_size = size;
	return co;
}

extern void co_swap(cothread_impl*, cothread_impl*);

static void crash(void)
{
	__asm__("int3"); // called only if cothread_t entrypoint returns
}

ECL_EXPORT void co_clean(void)
{
	memset(&co_host_buffer, 0, sizeof(co_host_buffer));
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
		uint64_t* p = (uint64_t*)((char*)co->stack + co->stack_size); // seek to top of stack
		*--p = (uint64_t)crash; // crash if entrypoint returns
		*--p = (uint64_t)entrypoint; // start of function
		co->jmp_buf.rsp = (uint64_t)p; // stack pointer
	}

	return co;
}

void co_delete(cothread_t handle)
{
	free_thread(handle);
}

// static uint64_t hoststart;
// static uint64_t hostend;

void co_switch(cothread_t handle)
{
	cothread_impl* co = handle;

	// uint64_t start;
	// uint64_t end;
	// if (co_active_handle == &co_host_buffer)
	// {
	// 	// migrating off of real thread; save stack params
	// 	__asm__("movq %%gs:0x08, %0": "=r"(end));
	// 	__asm__("movq %%gs:0x10, %0": "=r"(start));
	// 	hoststart = start;
	// 	hostend = end;
	// }
	// if (handle == &co_host_buffer)
	// {
	// 	// migrating onto real thread; load stack params
	// 	start = hoststart;
	// 	end = hostend;
	// 	hoststart = 0;
	// 	hostend = 0;
	// }
	// else
	// {
	// 	// migrating onto cothread; compute its extents we allocated them
	// 	start = (uintptr_t)co->stack;
	// 	end = start + co->stack_size;
	// }
	// __asm__("movq %0, %%gs:0x08":: "r"(end));
	// __asm__("movq %0, %%gs:0x10":: "r"(start));

	register cothread_impl* co_previous_handle = co_active_handle;
	co_swap(co_active_handle = co, co_previous_handle);
}
