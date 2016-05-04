#ifndef _PDCLIB_THREADCONFIG_H
#define _PDCLIB_THREADCONFIG_H
#include "_PDCLIB_aux.h"
#include "_PDCLIB_config.h"

#ifdef __cplusplus
extern "C" {
#endif
#define _PDCLIB_ONCE_FLAG_INIT 0
#define _PDCLIB_ONCE_FLAG_IS_DONE(_f) (*(_f) == 1)
typedef char _PDCLIB_once_flag;

void _PDCLIB_call_once(_PDCLIB_once_flag *flag, void (*func)(void));

#define _PDCLIB_THRD_HAVE_MISC
#define _PDCLIB_CND_T char
#define _PDCLIB_MTX_T char
#define _PDCLIB_TSS_T struct _PDCLIB_tss

struct _PDCLIB_tss {
	struct _PDCLIB_tss *self;
	void *value;
};

#ifdef __cplusplus
}
#endif
#endif
