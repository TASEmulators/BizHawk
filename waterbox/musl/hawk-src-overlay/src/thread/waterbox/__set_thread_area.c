#include "pthread_impl.h"

int __set_thread_area(void *p)
{
	long* context;
	__asm__ ("mov %%gs:0x18,%0" : "=r" (context) );
	context[0] = (long)p;
	return 0;
}
