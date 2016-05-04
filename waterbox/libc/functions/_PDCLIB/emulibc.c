#include <stdint.h>
#include <stddef.h>
#include "_PDCLIB_glue.h"
#include <errno.h>
#include <emulibc.h>

ECL_EXPORT ECL_ENTRY __attribute__((noreturn)) void (*_ecl_trap)(void); // something very unexpected happened.  should not return
ECL_EXPORT ECL_ENTRY void *(*_ecl_sbrk)(size_t n); // sbrk.  won't return if the request can't be satisfied
ECL_EXPORT ECL_ENTRY void (*_ecl_debug_puts)(const char *); // low level debug write, doesn't involve STDIO

ECL_EXPORT ECL_ENTRY void *(*_ecl_sbrk_sealed)(size_t n); // allocate memory; see emulibc.h
ECL_EXPORT ECL_ENTRY void *(*_ecl_sbrk_invisible)(size_t n); // allocate memory; see emulibc.h

void *alloc_sealed(size_t size)
{
	return _ecl_sbrk_sealed(size);
}

void *alloc_invisible(size_t size)
{
	return _ecl_sbrk_invisible(size);
}

void _debug_puts(const char *s)
{
	_ecl_debug_puts(s);
}

void *_PDCLIB_sbrk(size_t n)
{
	void *ret = _ecl_sbrk(n);
	return ret;
}

void _PDCLIB_Exit( int status )
{
	_ecl_trap();
}

