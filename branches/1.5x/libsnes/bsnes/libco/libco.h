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

#if defined(LIBCO_IMPORT)
#define LIBCO_IMPORTDECL __declspec(dllimport)
#elif defined(LIBCO_EXPORT)
#define LIBCO_IMPORTDECL __declspec(dllexport)
#else
#define LIBCO_IMPORTDECL
#endif

LIBCO_IMPORTDECL cothread_t co_active();
LIBCO_IMPORTDECL cothread_t co_create(unsigned int, coentry_t);
LIBCO_IMPORTDECL void co_delete(cothread_t);
LIBCO_IMPORTDECL void co_switch(cothread_t);

#ifdef __cplusplus
}
#endif

/* ifndef LIBCO_H */
#endif
