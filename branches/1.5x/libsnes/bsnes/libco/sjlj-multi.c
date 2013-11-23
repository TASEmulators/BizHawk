#define LIBCO_C
#include "libco.h"
#include <stdlib.h>
#include <setjmp.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
	jmp_buf context;
	coentry_t coentry;
	void *stack;
	unsigned long seh_frame, stack_top, stack_bottom;
	cothread_t caller;
} cothread_struct;

static thread_local cothread_struct co_primary;
static thread_local cothread_struct *co_running = 0;

//-------------------
#if defined(_MSC_VER) || defined(MINGW32)

__declspec(dllimport) cothread_t os_co_create();
__declspec(dllimport) void os_pre_setjmp(cothread_t target);
__declspec(dllimport) void os_pre_longjmp(cothread_struct* rec);

#elif defined(__ARM_EABI__) || defined(__ARMCC_VERSION)

//http://sourceware.org/cgi-bin/cvsweb.cgi/src/newlib/libc/machine/arm/setjmp.S?rev=1.5&content-type=text/x-cvsweb-markup&cvsroot=src

typedef struct 
{
#ifdef LIBCO_ARM_JUMBLED
	int r8,r9,r10,r11,lr,r4,r5,r6,r7,sp;
#else
	int r4,r5,r6,r7,r8,r9,r10,fp;
	#ifndef LIBCO_ARM_NOIP
		int ip;
	#endif
	int sp,lr;
#endif
} _JUMP_BUFFER;

static void os_co_create(cothread_struct* rec, unsigned int size, coentry_t coentry)
{
	_JUMP_BUFFER* jb = (_JUMP_BUFFER*)&rec->context;
	
	jb->sp = (unsigned long)rec->stack + size - 4;
	jb->lr = (unsigned long)coentry;
}

static void os_pre_setjmp(cothread_t target)
{
	cothread_struct* rec = (cothread_struct*)target;
	rec->caller = co_running;
}

static void os_pre_longjmp(cothread_struct* rec)
{
}

#else
#error "sjlj-multi: unsupported processor, compiler or operating system"
#endif
//-------------------

cothread_t co_active()
{
	if(!co_running) co_running = &co_primary;
	return (cothread_t)co_running;
}

cothread_t co_create(unsigned int size, void (*coentry)(void))
{
	cothread_struct *thread;

	if(!co_running) co_running = &co_primary;

	thread = (cothread_struct*)malloc(sizeof(cothread_struct));
	if(thread)
	{
		thread->coentry = coentry;
		thread->stack = malloc(size);
		{
			setjmp(thread->context);
			os_co_create(thread,size,coentry);
		}
	}

	return (cothread_t)thread;
}

void co_delete(cothread_t cothread)
{
	if(cothread)
	{
		if(((cothread_struct*)cothread)->stack)
		{
			free(((cothread_struct*)cothread)->stack);
		}
		free(cothread);
	}
}

void co_switch(cothread_t cothread)
{
	os_pre_setjmp(cothread);
	if(!setjmp(co_running->context))
	{
		co_running = (cothread_struct*)cothread;
		os_pre_longjmp(co_running);
		longjmp(co_running->context,0);
	}
}

#ifdef __cplusplus
}
#endif
