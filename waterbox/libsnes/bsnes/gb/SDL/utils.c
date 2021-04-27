#include <SDL.h>
#include <stdio.h>
#include <string.h>
#include "utils.h"

const char *resource_folder(void)
{
#ifdef DATA_DIR
    return DATA_DIR;
#else
    static const char *ret = NULL;
    if (!ret) {
        ret = SDL_GetBasePath();
        if (!ret) {
            ret = "./";
        }
    }
    return ret;
#endif
}

char *resource_path(const char *filename)
{
    static char path[1024];
    snprintf(path, sizeof(path), "%s%s", resource_folder(), filename);
    return path;
}


void replace_extension(const char *src, size_t length, char *dest, const char *ext)
{
    memcpy(dest, src, length);
    dest[length] = 0;

    /* Remove extension */
    for (size_t i = length; i--;) {
        if (dest[i] == '/') break;
        if (dest[i] == '.') {
            dest[i] = 0;
            break;
        }
    }

    /* Add new extension */
    strcat(dest, ext);
}
