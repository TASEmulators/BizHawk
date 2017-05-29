#include "emulibc.h"

// this is just used to build a dummy .so file that isn't used
void *alloc_sealed(size_t size) { return NULL; }
void *alloc_invisible(size_t size) { return NULL; }
void *alloc_plain(size_t size) { return NULL; }
void _debug_puts(const char *s) { }
