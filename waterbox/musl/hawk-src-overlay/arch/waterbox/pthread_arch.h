static inline struct pthread *__pthread_self()
{
	long* context;
	__asm__ ("mov %%gs:0x18,%0" : "=r" (context) );
	return (struct pthread*)context[0];
}

#define TP_ADJ(p) (p)

#define MC_PC gregs[REG_RIP]
