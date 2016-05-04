#ifndef _EMULIBC_H
#define _EMULIBC_H

#include <stddef.h>

// mark an entry point or callback pointer
#define ECL_ENTRY __attribute__((ms_abi))
// mark a visible symbol
#define ECL_EXPORT __attribute__((visibility("default")))

// allocate memory from the "sealed" pool.  this memory can never be freed,
// and can only be allocated or written to during the init phase.  after that, the host
// seals the pool, making it read only and all of its contents frozen.  good for LUTs and
// ROMs
void *alloc_sealed(size_t size);

// allocate memory from the "invisible" pool.  this memory can never be freed.
// this memory is not savestated!  this should only be used for a large buffer whose contents
// you are absolutely sure will not harm savestates
void *alloc_invisible(size_t size);

// send a debug string somewhere, bypassing stdio
void _debug_puts(const char *);

#endif
