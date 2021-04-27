#ifndef utils_h
#define utils_h
#include <stddef.h>

const char *resource_folder(void);
char *resource_path(const char *filename);
void replace_extension(const char *src, size_t length, char *dest, const char *ext);

#endif /* utils_h */
