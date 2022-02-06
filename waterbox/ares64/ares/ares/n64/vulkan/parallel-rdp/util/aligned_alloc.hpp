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

#pragma once

#include <stddef.h>
#include <stdexcept>
#include <new>

namespace Util
{
void *memalign_alloc(size_t boundary, size_t size);
void *memalign_calloc(size_t boundary, size_t size);
void memalign_free(void *ptr);

template <typename T>
struct AlignedAllocation
{
    static void *operator new(size_t size)
    {
        void *ret = ::Util::memalign_alloc(alignof(T), size);
        if (!ret) throw std::bad_alloc();
        return ret;
    }

    static void *operator new[](size_t size)
    {
        void *ret = ::Util::memalign_alloc(alignof(T), size);
        if (!ret) throw std::bad_alloc();
        return ret;
    }

    static void operator delete(void *ptr)
    {
        return ::Util::memalign_free(ptr);
    }

    static void operator delete[](void *ptr)
    {
        return ::Util::memalign_free(ptr);
    }
};
}
