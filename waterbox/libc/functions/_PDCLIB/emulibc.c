#include <stdint.h>
#include <stddef.h>
#include "_PDCLIB_glue.h"
#include <errno.h>
#include <emulibc.h>

ECL_EXPORT ECL_ENTRY __attribute__((noreturn)) void (*_ecl_trap)(void); // something very unexpected happened.  should not return
ECL_EXPORT ECL_ENTRY void *(*_ecl_sbrk)(size_t n); // sbrk.  won't return if the request can't be satisfied
ECL_EXPORT ECL_ENTRY void (*_ecl_debug_puts)(const char *); // low level debug write, doesn't involve STDIO

void *_PDCLIB_sbrk(size_t n)
{
	void *ret = _ecl_sbrk(n);
	return ret;
}

void _PDCLIB_Exit( int status )
{
	_ecl_trap();
}

