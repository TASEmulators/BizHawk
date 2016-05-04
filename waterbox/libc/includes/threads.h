/* Threads <threads.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_THREADS_H
#define _PDCLIB_THREADS_H _PDCLIB_THREADS_H
#include "_PDCLIB_int.h"
#include "_PDCLIB_threadconfig.h"

#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

#define thread_local _Thread_local

typedef _PDCLIB_once_flag once_flag;

enum
{
    mtx_plain       = 0,
    mtx_recursive   = (1 << 0),
    mtx_timed       = (1 << 1),

    _PDCLIB_mtx_valid_mask = mtx_recursive | mtx_timed
};

enum
{
    thrd_success    = 0,
    thrd_timeout    = 1,
    thrd_busy       = 2,
    thrd_error      = 3,
    thrd_nomem      = 4,
};

#define ONCE_FLAG_INIT _PDCLIB_ONCE_FLAG_INIT
#ifdef _PDCLIB_ONCE_FLAG_IS_DONE
static inline void call_once( once_flag * flag, void (*func)( void ) )
{
    if ( ! _PDCLIB_ONCE_FLAG_IS_DONE( flag ) )
    {
        _PDCLIB_call_once( flag, func );
    }
}
#else
void call_once( once_flag * flag, void (*func)( void ) );
#endif

#ifdef _PDCLIB_MTX_T
typedef _PDCLIB_MTX_T mtx_t;
void mtx_destroy( mtx_t * mtx ) _PDCLIB_nothrow;
int mtx_init( mtx_t * mtx, int type ) _PDCLIB_nothrow;
int mtx_lock( mtx_t * mtx ) _PDCLIB_nothrow;
int mtx_timedlock( mtx_t * _PDCLIB_restrict mtx, const struct timespec * _PDCLIB_restrict ts ) _PDCLIB_nothrow;
int mtx_trylock( mtx_t * mtx ) _PDCLIB_nothrow;
int mtx_unlock( mtx_t * mtx ) _PDCLIB_nothrow;
#endif

#ifdef _PDCLIB_CND_T
typedef _PDCLIB_CND_T cnd_t;
int cnd_broadcast( cnd_t * cond ) _PDCLIB_nothrow;
void cnd_destroy( cnd_t * cond ) _PDCLIB_nothrow;
int cnd_init( cnd_t * cond ) _PDCLIB_nothrow;
int cnd_signal( cnd_t * cond ) _PDCLIB_nothrow;
int cnd_timedwait( cnd_t *_PDCLIB_restrict cond, mtx_t * _PDCLIB_restrict mtx, const struct timespec * _PDCLIB_restrict ts ) _PDCLIB_nothrow;
int cnd_wait( cnd_t * cond, mtx_t * mtx ) _PDCLIB_nothrow;
#endif

#ifdef _PDCLIB_THRD_T
#define _PDCLIB_THRD_HAVE_MISC
typedef _PDCLIB_THRD_T thrd_t;
typedef int (*thrd_start_t)( void * );

int thrd_create( thrd_t * thr, thrd_start_t func, void * arg ) _PDCLIB_nothrow;
thrd_t thrd_current( void ) _PDCLIB_nothrow;
int thrd_detach( thrd_t thr ) _PDCLIB_nothrow;
int thrd_equal( thrd_t thr0, thrd_t thr1 ) _PDCLIB_nothrow;

/* Not nothrow: systems may use exceptions at thread exit */
_PDCLIB_noreturn void thrd_exit( int res );
/* Not nothrow: systems may potentially propogate exceptions out of thrd_join? */
int thrd_join( thrd_t thr, int * res );
#endif

#ifdef _PDCLIB_THRD_HAVE_MISC
int thrd_sleep( const struct timespec * duration, struct timespec * remaining ) _PDCLIB_nothrow;
void thrd_yield( void ) _PDCLIB_nothrow;
#endif

/* The behaviour of tss_t is woefully underspecified in the C11 standard. In
   particular, it never specifies where/when/<b>if</b> destructors are called.

   In lieu of any clarification, we assume the behaviour of POSIX pthread_key_t
*/

#ifdef _PDCLIB_TSS_T
#define TSS_DTOR_ITERATIONS _PDCLIB_TSS_DTOR_ITERATIONS

typedef _PDCLIB_TSS_T tss_t;
typedef void (*tss_dtor_t)( void * );

int tss_create( tss_t * key, tss_dtor_t dtor ) _PDCLIB_nothrow;
void tss_delete( tss_t key ) _PDCLIB_nothrow;
void * tss_get( tss_t key ) _PDCLIB_nothrow;
int tss_set( tss_t key, void * val ) _PDCLIB_nothrow;
#endif

#ifdef __cplusplus
}
#endif

#endif
