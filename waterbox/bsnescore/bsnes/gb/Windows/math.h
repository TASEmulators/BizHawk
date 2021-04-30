#pragma once
#include_next <math.h>
#ifndef __MINGW32__
/* "Old" (Pre-2015) Windows headers/libc don't have round. */
static inline double round(double f)
{
    return f >= 0? (int)(f + 0.5) : (int)(f - 0.5);
}
#endif