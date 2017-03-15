/*
original author: Nach
license: public domain

additional work: zeromus
note: more ARM compilers are supported here (check the ifdefs in _JUMP_BUFFER)
and: work has been done to make this coexist more peaceably with .net
*/



#define LIBCO_C
#include "libco.h"
#include <stdlib.h>
#include <setjmp.h>
#include <string.h>
#include <stdint.h>

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
	int ownstack;
} cothread_struct;

static thread_local cothread_struct _co_primary;
static thread_local cothread_struct *co_running = 0;

cothread_t co_primary() { return (cothread_t)&_co_primary; }


//-------------------
#ifdef _MSC_VER

//links of interest
//http://connect.microsoft.com/VisualStudio/feedback/details/100319/really-wierd-behaviour-in-crt-io-coupled-with-some-inline-assembly
//http://en.wikipedia.org/wiki/Thread_Information_Block
//http://social.msdn.microsoft.com/Forums/en-US/vclanguage/thread/72093e46-4524-4f54-9f36-c7e8a309d1db/   //FS warning


#define WINVER 0x0400
#define _WIN32_WINNT 0x0400
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#pragma warning(disable:4733)
#pragma warning(disable:4311)

static void capture_fs(cothread_struct* rec)
{
	int temp;
	__asm mov eax, dword ptr fs:[0];
	__asm mov temp, eax;
	rec->seh_frame = temp;
	__asm mov eax, dword ptr fs:[4];
	__asm mov temp, eax;
	rec->stack_top = temp;
	__asm mov eax, dword ptr fs:[8];
	__asm mov temp, eax;
	rec->stack_bottom = temp;
}

static void restore_fs(cothread_struct* rec)
{
	int temp;
	temp = rec->seh_frame;
	__asm mov eax, temp;
	__asm mov dword ptr fs:[0], eax
	temp = rec->stack_top;
	__asm mov eax, temp;
	__asm mov dword ptr fs:[4], eax
	temp = rec->stack_bottom;
	__asm mov eax, temp;
	__asm mov dword ptr fs:[8], eax
}

static void os_co_wrapper()
{
	cothread_struct* rec = (cothread_struct*)co_active();
	__try
	{
		rec->coentry();
	}
	__except(EXCEPTION_EXECUTE_HANDLER)
	{
		//unhandled win32 exception in coroutine. 
		//this coroutine will now be suspended permanently and control will be yielded to caller, for lack of anything better to do. 
		//perhaps the process should just terminate.
		for(;;)
		{
			//dead coroutine
			co_switch(rec->caller);
		}
	}
}

static void os_co_create(cothread_struct* rec, unsigned int size, coentry_t coentry)
{
	_JUMP_BUFFER* jb = (_JUMP_BUFFER*)&rec->context;
	cothread_struct temp;

	jb->Esp = (unsigned long)rec->stack + size - 4;
	jb->Eip = (unsigned long)os_co_wrapper;

	rec->stack_top = jb->Esp + 4;
	rec->stack_bottom = (unsigned long)rec->stack;

	//wild assumption about SEH frame.. seems to work
	capture_fs(&temp);
	rec->seh_frame = temp.seh_frame;
}

static void os_pre_setjmp(cothread_t target)
{
	cothread_struct* rec = (cothread_struct*)target;
	capture_fs(co_running);
	rec->caller = co_running;
}

static void os_pre_longjmp(cothread_struct* rec)
{
	restore_fs(rec);
}

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
	if(!co_running) co_running = &_co_primary;
	return (cothread_t)co_running;
}

void* co_getstack(cothread_t cothread)
{
  return ((cothread_struct*)cothread)->stack;
}

cothread_t co_create(unsigned int stacksize, coentry_t coentry)
{
	cothread_struct* ret = (cothread_struct*)co_create_withstack(malloc(stacksize), stacksize, coentry);
	if(ret)
		ret->ownstack = 1;
	return (cothread_t)ret;
}

cothread_t co_create_withstack(void* stack, int stacksize, coentry_t coentry)
{
	cothread_struct *thread;

	if(!co_running) co_running = &_co_primary;

	thread = (cothread_struct*)malloc(sizeof(cothread_struct));
	if(thread)
	{
		thread->coentry = coentry;
		thread->stack = stack;
    
		{
			setjmp(thread->context);
			os_co_create(thread,stacksize,coentry);
		}
		thread->ownstack = 0;
	}

	return (cothread_t)thread;
}

void co_delete(cothread_t cothread)
{
	if(cothread)
	{
		cothread_struct* thread = (cothread_struct*)cothread;
		if (thread->ownstack)
			free(thread->stack);
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
