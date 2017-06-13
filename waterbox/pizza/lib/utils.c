/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#ifdef __ANDROID__
#include <android/log.h>
#else
#include <stdio.h>
#endif

#include <errno.h>
#include <stdarg.h>
#include <sys/time.h>

#include "cycles.h"
#include "gpu.h"
#include "utils.h"

uint32_t prev_cycles = 0;

void utils_log(const char *format, ...)
{
    char buf[256];

    va_list args;
    va_start(args, format);

#ifdef __ANDROID__

    vsnprintf(buf, 256, format, args);
    __android_log_write(ANDROID_LOG_INFO, "Pizza", buf);

#else

    vsnprintf(buf, 256, format, args);
    printf(buf);

#endif

    va_end(args);
}


void utils_log_urgent(const char *format, ...)
{
    char buf[256];

    va_list args;
    va_start(args, format);

#ifdef __ANDROID__

    vsnprintf(buf, 256, format, args);
    __android_log_write(ANDROID_LOG_INFO, "Pizza", buf);

#else

    vsnprintf(buf, 256, format, args);
    printf(buf);

#endif

    va_end(args);
}

void utils_ts_log(const char *format, ...)
{
    va_list args;
    va_start(args, format);

    char buf[256];
    struct timeval tv;

#ifdef __ANDROID__

    vsnprintf(buf, 256, format, args);
    __android_log_write(ANDROID_LOG_INFO, "Pizza", buf);

#else

    vsprintf(buf, format, args);
    gettimeofday(&tv, NULL);
//    printf("%ld - %s\n", tv.tv_sec, buf);
    printf("LINE %u - CYCLES %u - DIFF %u - %ld:%06ld - %s", 
            *(gpu.ly), cycles.cnt, cycles.cnt - prev_cycles, 
            tv.tv_sec, tv.tv_usec, buf);

    prev_cycles = cycles.cnt;

#endif

    va_end(args);
}

void utils_binary_sem_init(utils_binary_sem_t *p)
{
    pthread_mutex_init(&p->mutex, NULL);
    pthread_cond_init(&p->cvar, NULL);
    p->v = 0;
}

void utils_binary_sem_post(utils_binary_sem_t *p)
{
    pthread_mutex_lock(&p->mutex);
    p->v = 1;
    pthread_cond_signal(&p->cvar);
    pthread_mutex_unlock(&p->mutex);
}

void utils_binary_sem_wait(utils_binary_sem_t *p, unsigned int nanosecs)
{
    struct timespec ts;

    ts.tv_sec = time(NULL) + nanosecs / 1000000000;
    ts.tv_nsec = nanosecs % 1000000000;

    pthread_mutex_lock(&p->mutex);
    while (!p->v)
        if (pthread_cond_timedwait(&p->cvar, &p->mutex, &ts) == ETIMEDOUT)
            break;
    p->v = 0;
    pthread_mutex_unlock(&p->mutex);
}

