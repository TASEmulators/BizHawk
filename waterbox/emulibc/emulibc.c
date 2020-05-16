#include "emulibc.h"

#define __WBXSYSCALL __attribute__((section(".wbxsyscall")))

__WBXSYSCALL void *(*__walloc_sealed)(size_t);
void *alloc_sealed(size_t size)
{
	return __walloc_sealed(size);
}

__WBXSYSCALL void *(*__walloc_invisible)(size_t);
void *alloc_invisible(size_t size)
{
	return __walloc_invisible(size);
}

__WBXSYSCALL void *(*__walloc_plain)(size_t);
void *alloc_plain(size_t size)
{
	return __walloc_plain(size);
}

__WBXSYSCALL void (*__w_debug_puts)(const char *);
void _debug_puts(const char *s)
{
	__w_debug_puts(s);
}
