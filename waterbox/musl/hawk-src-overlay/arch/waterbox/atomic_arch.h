#include "syscall.h"

#define a_cas a_cas
static inline int a_cas(volatile int *p, int t, int s)
{
	// keep the branchless asm implementation here
	// if (*p == t) { *p = s; return t; } else { return *p; }
	__asm__ __volatile__ (
		"cmpxchg %3, %1"
		: "=a"(t), "=m"(*p) : "a"(t), "r"(s) : "memory" );
	return t;
}

#define a_cas_p a_cas_p
static inline void *a_cas_p(volatile void *p, void *t, void *s)
{
	__asm__( "cmpxchg %3, %1"
		: "=a"(t), "=m"(*(void *volatile *)p)
		: "a"(t), "r"(s) : "memory" );
	return t;
}

#define a_swap a_swap
static inline int a_swap(volatile int *p, int v)
{
	int ret = *p;
	*p = v;
	return ret;
}

#define a_fetch_add a_fetch_add
static inline int a_fetch_add(volatile int *p, int v)
{
	int ret = *p;
	*p += v;
	return ret;
}

#define a_and a_and
static inline void a_and(volatile int *p, int v)
{
	*p &= v;
}

#define a_or a_or
static inline void a_or(volatile int *p, int v)
{
	*p |= v;
}

#define a_and_64 a_and_64
static inline void a_and_64(volatile uint64_t *p, uint64_t v)
{
	*p &= v;
}

#define a_or_64 a_or_64
static inline void a_or_64(volatile uint64_t *p, uint64_t v)
{
	*p |= v;
}

#define a_inc a_inc
static inline void a_inc(volatile int *p)
{
	*p++;
}

#define a_dec a_dec
static inline void a_dec(volatile int *p)
{
	*p--;
}

#define a_store a_store
static inline void a_store(volatile int *p, int x)
{
	*p = x;
}

#define a_barrier a_barrier
static inline void a_barrier()
{
	__asm__ __volatile__( "" : : : "memory" );
}

#define a_spin a_spin
static inline void a_spin()
{
	syscall(SYS_sched_yield);
}

#define a_crash a_crash
static inline void a_crash()
{
	__asm__ __volatile__( "hlt" : : : "memory" );
}

#define a_ctz_64 a_ctz_64
static inline int a_ctz_64(uint64_t x)
{
	__asm__( "bsf %1,%0" : "=r"(x) : "r"(x) );
	return x;
}

#define a_clz_64 a_clz_64
static inline int a_clz_64(uint64_t x)
{
	__asm__( "bsr %1,%0 ; xor $63,%0" : "=r"(x) : "r"(x) );
	return x;
}
