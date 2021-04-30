#pragma once
#include_next <stdio.h>
#include <stdlib.h>

int access(const char *filename, int mode);
#define R_OK 2
#define W_OK 4

#ifndef __MINGW32__
#ifndef __LIBRETRO__
static inline int vasprintf(char **str, const char *fmt, va_list args)
{
    size_t size = _vscprintf(fmt, args) + 1;
    *str = malloc(size);
    int ret = vsprintf(*str, fmt, args);
    if (ret != size - 1) {
        free(*str);
        *str = NULL;
        return -1;
    }
    return ret;
}
#endif
#endif

/* This code is public domain -- Will Hartung 4/9/09 */
static inline size_t getline(char **lineptr, size_t *n, FILE *stream) 
{
    char *bufptr = NULL;
    char *p = bufptr;
    size_t size;
    int c;

    if (lineptr == NULL) {
        return -1;
    }
    if (stream == NULL) {
        return -1;
    }
    if (n == NULL) {
        return -1;
    }
    bufptr = *lineptr;
    size = *n;

    c = fgetc(stream);
    if (c == EOF) {
        return -1;
    }
    if (bufptr == NULL) {
        bufptr = malloc(128);
        if (bufptr == NULL) {
            return -1;
        }
        size = 128;
    }
    p = bufptr;
    while (c != EOF) {
        if ((p - bufptr) > (size - 1)) {
            size = size + 128;
            bufptr = realloc(bufptr, size);
            if (bufptr == NULL) {
                return -1;
            }
        }
        *p++ = c;
        if (c == '\n') {
            break;
        }
        c = fgetc(stream);
    }

    *p++ = '\0';
    *lineptr = bufptr;
    *n = size;

    return p - bufptr - 1;
}

#define snprintf _snprintf
