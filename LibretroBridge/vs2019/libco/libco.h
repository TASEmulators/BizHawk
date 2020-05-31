/*
  libco
  version: 0.16 (2010-12-24)
  license: public domain
*/

#ifndef LIBCO_H
#define LIBCO_H

#ifdef LIBCO_C
  #ifdef LIBCO_MP
    #define thread_local __thread
  #else
    #define thread_local
  #endif
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef void* cothread_t;
typedef void (*coentry_t)(void);

void* co_getstack(cothread_t);
cothread_t co_active();
cothread_t co_create_withstack(void* stack, int stacksize, coentry_t);
cothread_t co_create(unsigned int, coentry_t);
void co_delete(cothread_t);
void co_switch(cothread_t);
cothread_t co_primary();

#ifdef __cplusplus
}
#endif

/* ifndef LIBCO_H */
#endif
