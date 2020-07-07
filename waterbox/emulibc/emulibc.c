#include "emulibc.h"
#include <stdio.h>
#include <sys/mman.h>

// Keep this in sync with the rust code!!
struct __AddressRange {
	unsigned long start;
	unsigned long size;
};
struct __WbxSysLayout {
	struct __AddressRange elf;
	struct __AddressRange main_thread;
	struct __AddressRange sbrk;
	struct __AddressRange sealed;
	struct __AddressRange invis;
	struct __AddressRange plain;
	struct __AddressRange mmap;
};
__attribute__((section(".invis"))) __attribute__((visibility("default"))) struct __WbxSysLayout __wbxsysinfo;

void* alloc_helper(size_t size, const struct __AddressRange* range, unsigned long* current, const char* name)
{
	if (!*current)
	{
		printf("Initializing heap %s at %p:%p\n", name, (void*)range->start, (void*)(range->start + range->size));
		*current = range->start;
	}

	unsigned long start = *current;
	unsigned long end = start + size;
	end = (end + 15) & ~15ul;

	if (end < start || end > range->start + range->size)
	{
		fprintf(stderr, "Failed to satisfy allocation of %lu bytes on %s heap\n", size, name);
		return NULL;
	}
	else
	{
		unsigned long pstart = (start + 0xfff) & ~0xffful;
		unsigned long pend = (end + 0xfff) & ~0xffful;
		if (pstart < pend)
		{
			if (mmap((void*)pstart, pend - pstart, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS | MAP_FIXED | MAP_FIXED_NOREPLACE, -1, 0) == MAP_FAILED)
			{
				fprintf(stderr, "VERY STRANGE: mmap() failed to satisfy allocation of %lu bytes on %s heap\n", size, name);
				return NULL;
			}
		}
		printf("Allocated %lu bytes on %s heap, usage %lu/%lu\n", size, name, end - range->start, range->size);
		*current = end;
		return (void*)start;
	}
}

static unsigned long __sealed_current;
void* alloc_sealed(size_t size)
{
	return alloc_helper(size, &__wbxsysinfo.sealed, &__sealed_current, "sealed");
}

static unsigned long __invisible_current;
void* alloc_invisible(size_t size)
{
	return alloc_helper(size, &__wbxsysinfo.invis, &__invisible_current, "invisible");
}

static unsigned long __plain_current;
void* alloc_plain(size_t size)
{
	return alloc_helper(size, &__wbxsysinfo.plain, &__plain_current, "plain");
}

// TODO: This existed before we even had stdio support.  Retire?
void _debug_puts(const char *s)
{
	fprintf(stderr, "%s\n", s);
}

ECL_EXPORT void ecl_seal()
{
	if (__sealed_current)
	{
		if (mprotect((void*)__wbxsysinfo.sealed.start, (__sealed_current - __wbxsysinfo.sealed.start + 0xfff) & ~0xffful, PROT_READ) != 0)
			__asm__("int3");
	}
}
