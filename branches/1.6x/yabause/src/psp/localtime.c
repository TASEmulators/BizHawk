/*  src/psp/localtime.c: PSP implementation of localtime()
    Copyright 2009 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include "common.h"
#include <time.h>  // For struct tm definition

#include "localtime.h"
#include "sys.h"

/*************************************************************************/

/* Time zone offset from UTC (amount to add to time() result) */
static int32_t utc_offset;

/*************************************************************************/
/*************************************************************************/

/**
 * localtime_init:  Perform initialization required for localtime().
 * Called by PSP initialization code at program startup.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void localtime_init(void)
{
    /* Find the PSP's time zone */
    int utc_offset_min;
    int32_t result = sceUtilityGetSystemParamInt(
        PSP_SYSTEMPARAM_ID_INT_TIMEZONE, &utc_offset_min
    );
    if (result == 0) {
        utc_offset = utc_offset_min * 60;
    } else {
        DMSG("Failed to get time zone: %s", psp_strerror(result));
        utc_offset = 0;
    }

    /* Check whether daylight saving time is in use */
    int dst;
    result = sceUtilityGetSystemParamInt(
        PSP_SYSTEMPARAM_ID_INT_DAYLIGHTSAVINGS, &dst
    );
    if (result == 0) {
        if (dst) {
            utc_offset += 60*60;
        }
    } else {
        DMSG("Failed to get DST status: %s", psp_strerror(result));
    }

}

/*************************************************************************/

/**
 * localtime_utc_offset:  Return the UTC offset of the current time zone in
 * seconds (for example, GMT-1 has a UTC offset of -3600 seconds).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     UTC offset in seconds
 */
int32_t localtime_utc_offset(void)
{
    return utc_offset;
}

/*************************************************************************/

/**
 * internal_localtime_r:  Local, reentrant version of localtime() used by
 * ../smpc.c.  Breaks down a timestamp integer into Y/M/D h:m:s
 * representation.
 *
 * [Parameters]
 *      timep: Timestamp to break down
 *     result: Broken-down time
 * [Return value]
 *     result, or NULL on error
 */
struct tm *internal_localtime_r(const time_t *timep, struct tm *result)
{
    static const uint8_t mdays[12] = {31,28,31,30,31,30,31,31,30,31,30,31};

    PRECOND(timep != NULL, return NULL);
    PRECOND(result != NULL, return NULL);
    time_t t = *timep + utc_offset;

    /* Weekday is simple: ((days since 1970/1/1) + Thursday) % 7 */
    result->tm_wday = (t/86400 + 4) % 7;

    /* Calculate year */
    result->tm_year = 70;
    int32_t yearsecs = 365*86400;
    while (t >= yearsecs) {
        t -= yearsecs;
        result->tm_year++;
        /* Careful here -- tm_year starts at 1900, not 0 */
        int isleap = (result->tm_year%4 == 0
                      && (result->tm_year%100 != 0
                          || result->tm_year%400 == 100));
        yearsecs = isleap ? 366*86400 : 365*86400;
    }

    /* Calculate month */
    int month = 0;
    while (t >= mdays[month] * 86400) {
        t -= mdays[month] * 86400;
        month++;
    }
    result->tm_mon = month;

    /* The rest is straightforward */
    result->tm_sec = t % 60;
    t /= 60;
    result->tm_min = t % 60;
    t /= 60;
    result->tm_hour = t % 24;
    t /= 24;
    result->tm_mday = t + 1;

    return result;
}

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
