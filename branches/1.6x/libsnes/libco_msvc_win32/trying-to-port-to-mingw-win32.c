//links of interest
//http://connect.microsoft.com/VisualStudio/feedback/details/100319/really-wierd-behaviour-in-crt-io-coupled-with-some-inline-assembly
//http://en.wikipedia.org/wiki/Thread_Information_Block
//http://social.msdn.microsoft.com/Forums/en-US/vclanguage/thread/72093e46-4524-4f54-9f36-c7e8a309d1db/   //FS warning

#define LIBCO_C
#define LIBCO_EXPORT
#include "../bsnes/libco/libco.h"
#include <stdlib.h>
#include <setjmp.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*coentry_t)(void);

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

#define WINVER 0x0400
#define _WIN32_WINNT 0x0400
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#ifndef _MSC_VER
typedef struct __JUMP_BUFFER {
    unsigned long Ebp;
    unsigned long Ebx;
    unsigned long Edi;
    unsigned long Esi;
    unsigned long Esp;
    unsigned long Eip;
    unsigned long Registration;
    unsigned long TryLevel;
    unsigned long Cookie;
    unsigned long UnwindFunc;
    unsigned long UnwindData[6];
} _JUMP_BUFFER;
#endif

#pragma warning(disable:4733)
#pragma warning(disable:4311)

static void capture_fs(cothread_struct* rec)
{
	asm(
		"mov %0, dword ptr fs:[0];"
		"mov %1, dword ptr fs:[4];"
		"mov %2, dword ptr fs:[8];"
		:"=r"(rec->seh_frame), "=r"(rec->stack_top), "=r"(rec->stack_bottom)
		:
		:
		);
}

static void restore_fs(cothread_struct* rec)
{
	asm(
		"mov dword ptr fs:[0], %0;"
		"mov dword ptr fs:[4], %1;"
		"mov dword ptr fs:[8], %2;"
		:
		:"r"(rec->seh_frame), "r"(rec->stack_top), "r"(rec->stack_bottom)
		:
		);
}

static void os_co_wrapper()
{
	cothread_struct* rec = (cothread_struct*)co_active();
	//__try
	//{
		rec->coentry();
	//}
	//__except(EXCEPTION_EXECUTE_HANDLER)
	//{
	//	//unhandled win32 exception in coroutine. 
	//	//this coroutine will now be suspended permanently and control will be yielded to caller, for lack of anything better to do. 
	//	//perhaps the process should just terminate.
	//	for(;;)
	//	{
	//		//dead coroutine
	//		co_switch(rec->caller);
	//	}
	//}
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
