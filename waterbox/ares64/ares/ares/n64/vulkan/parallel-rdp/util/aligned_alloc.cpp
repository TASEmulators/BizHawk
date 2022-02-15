/* Copyright (c) 2017-2020 Hans-Kristian Arntzen
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "aligned_alloc.hpp"
#include <stdlib.h>
#include <string.h>
#ifdef _WIN32
#include <malloc.h>
#endif

namespace Util
{
void *memalign_alloc(size_t boundary, size_t size)
{
#if defined(_WIN32)
    return _aligned_malloc(size, boundary);
#elif defined(_ISOC11_SOURCE)
    return aligned_alloc(boundary, size);
#elif (_POSIX_C_SOURCE >= 200112L) || (_XOPEN_SOURCE >= 600)
	void *ptr = nullptr;
	if (posix_memalign(&ptr, boundary, size) < 0)
		return nullptr;
	return ptr;
#else
    // Align stuff ourselves. Kinda ugly, but will work anywhere.
    void **place;
    uintptr_t addr = 0;
    void *ptr = malloc(boundary + size + sizeof(uintptr_t));

    if (ptr == nullptr)
        return nullptr;

    addr = ((uintptr_t)ptr + sizeof(uintptr_t) + boundary) & ~(boundary - 1);
    place = (void **) addr;
    place[-1] = ptr;

    return (void *) addr;
#endif
}

void *memalign_calloc(size_t boundary, size_t size)
{
    void *ret = memalign_alloc(boundary, size);
    if (ret)
        memset(ret, 0, size);
    return ret;
}

void memalign_free(void *ptr)
{
#if defined(_WIN32)
    _aligned_free(ptr);
#elif !defined(_ISOC11_SOURCE) && !((_POSIX_C_SOURCE >= 200112L) || (_XOPEN_SOURCE >= 600))
    if (ptr != nullptr)
    {
        void **p = (void **) ptr;
        free(p[-1]);
    }
#else
    free(ptr);
#endif
}
}
