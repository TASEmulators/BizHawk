#define _PDCLIB_EXTENSIONS
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>

static size_t mcb(void *p, const char *buf, size_t size)
{
	*(size_t *)p += size;
	return size;
} 

static size_t vprintflen(const char *restrict fmt, va_list arg)
{
	size_t ret = 0;
	_vcbprintf(&ret, mcb, fmt, arg);
	return ret + 1;
}

int asprintf(char **strp, const char *restrict fmt, ...)
{
    va_list arg, arg2;
    va_start(arg, fmt);
	va_copy(arg2, arg);
	
	size_t sz = vprintflen(fmt, arg);

	*strp = malloc(sz);
	if (!strp)
	{
		va_end(arg);
		va_end(arg2);
		return -1;
	}

	int ret = vsnprintf(*strp, sz, fmt, arg2);
	va_end(arg);
	va_end(arg2);
	return ret;
}

int vasprintf(char **strp, const char *restrict fmt, va_list arg)
{
    va_list arg2;
	va_copy(arg2, arg);
	
	size_t sz = vprintflen(fmt, arg);

	*strp = malloc(sz);
	if (!strp)
	{
		va_end(arg2);
		return -1;
	}

	int ret = vsnprintf(*strp, sz, fmt, arg2);
	va_end(arg2);
	return ret;
}
