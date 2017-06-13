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

#ifndef __UTILS_HDR__
#define __UTILS_HDR__

#include <pthread.h>

/* binary semaphore */
typedef struct utils_binary_sem_s 
{
    pthread_mutex_t mutex;
    pthread_cond_t cvar;
    int v;

} utils_binary_sem_t;

/* prototypes */
void    utils_binary_sem_init(utils_binary_sem_t *p);
void    utils_binary_sem_post(utils_binary_sem_t *p);
void    utils_binary_sem_wait(utils_binary_sem_t *p, unsigned int nanosecs);
void    utils_log(const char *format, ...);
void    utils_log_urgent(const char *format, ...);
void    utils_ts_log(const char *format, ...);

#endif
