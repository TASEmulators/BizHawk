/*  src/psp/profile.c: Profiling support for Yabause core
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

#ifdef SYS_PROFILE_H  // i.e. profiling enabled -- continues to end of file

#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include "profile.h"

/*************************************************************************/

/* Names for each tracking slot */
const char * const prof_names[PROF__END] = {
    [PROF__UNKNOWN       ] = "(unknown)",
    [PROF_Total_Emulation] = "Total Emulation",
    [PROF_MSH2           ] = "MSH2",
    [PROF_68K            ] = "68K",
    [PROF_SCSP           ] = "SCSP",
    [PROF_SCU            ] = "SCU",
    [PROF_SSH2           ] = "SSH2",
    [PROF_CDB            ] = "CDB",
    [PROF_CDIO           ] = "CDIO",
    [PROF_SMPC           ] = "SMPC",
    [PROF_hblankin       ] = "hblankin",
    [PROF_hblankout      ] = "hblankout",
    [PROF_VDP1_VDP2      ] = "VDP1/VDP2",
    [PROF_vblankin       ] = "vblankin",
    [PROF__OVERHEAD      ] = "(profiling overhead)",
};

/* Latest start time for each tracking slot */
static uint32_t prof_start[PROF__END];

/* Total run time (microseconds) for each tracking slot */
static uint64_t prof_total[PROF__END];

/* Number of calls for each tracking slot */
static uint32_t prof_calls[PROF__END];

/* Approximate profiling overhead per call (microseconds) */
static double prof_overhead = 0;
/* Profiling overhead per call that appears in slot totals (microseconds) */
static double visible_overhead = 0;

/*************************************************************************/
/*************************************************************************/

/**
 * psp_profile_reset:  Reset all profiling counters.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void psp_profile_reset(void)
{
    memset(prof_start, 0, sizeof(prof_start));
    memset(prof_total, 0, sizeof(prof_total));
    memset(prof_calls, 0, sizeof(prof_calls));
}

/*-----------------------------------------------------------------------*/

/**
 * psp_profile_start:  Start the profile timer for the given slot with the
 * given timestamp.
 *
 * [Parameters]
 *          slot: Tracking slot (0..PROF__END-1)
 *     timestamp: 32-bit system timestamp from sceKernelGetSystemTimeLow()
 * [Return value]
 *     None
 */
__attribute__((noinline))  // To avoid throwing off overhead measurements
void psp_profile_start(int slot, uint32_t timestamp)
{
    /* Make sure this runs quickly, since it counts against the slot's run
     * time */
    prof_start[slot] = timestamp;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_profile_stop:  Stop the profile timer for the given slot, and update
 * the call count and total run time.
 *
 * [Parameters]
 *          slot: Tracking slot (0..PROF__END-1)
 *     timestamp: 32-bit system timestamp from sceKernelGetSystemTimeLow()
 * [Return value]
 *     None
 * [Notes]
 *     The run time for a single call must not exceed 2^32-1 microseconds
 *     (about 71.5 minutes), or the profiling totals will be inaccurate.
 */
__attribute__((noinline))
void psp_profile_stop(int slot, uint32_t timestamp)
{
    const uint32_t run_time = timestamp - prof_start[slot];
    prof_total[slot] += run_time;
    prof_calls[slot]++;
}

/*************************************************************************/

/**
 * psp_profile_print:  Print profiling statistics.  Information for each slot
 * with a nonzero call count is printed on one line, sorted in descending
 * order by total run time.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void psp_profile_print(void)
{
    int order[PROF__END];
    double usec[PROF__END];
    int num_slots = 0;
    int i, j;

    /* First calculate the approximate profiling overhead per call, if we
     * haven't done so already */
    if (!prof_overhead) {
        uint32_t start = sceKernelGetSystemTimeLow();
        for (i = 0; i < 1000; i++) {
            psp_profile_start(PROF__OVERHEAD, sceKernelGetSystemTimeLow());
            psp_profile_stop(PROF__OVERHEAD, sceKernelGetSystemTimeLow());
        }
        uint32_t end = sceKernelGetSystemTimeLow();
        prof_overhead = (end - start) / 1000.0;

        uint32_t visible_sum = 0;
        for (i = 0; i < 1000; i++) {
            uint32_t this_time = sceKernelGetSystemTimeLow();
            psp_profile_start(PROF__OVERHEAD, this_time);
            visible_sum += sceKernelGetSystemTimeLow() - this_time;
        }
        visible_overhead = visible_sum / 1000.0;
    }

    /* Find which slots need to be printed, and calculate their data along
     * with total overhead */
    uint32_t total_calls = 0;
    double total_overhead = 0;
    for (i = 0; i < PROF__OVERHEAD; i++) {
        if (prof_calls[i] > 0) {
            order[num_slots++] = i;
            if (i == PROF_Total_Emulation) {
                /* We'll subtract the total overhead from this value once
                 * we know it */
                usec[i] = (double)prof_total[i];
            } else {
                const double overhead = visible_overhead * prof_calls[i];
                usec[i] = (double)prof_total[i] - overhead;
            }
        }
        total_calls += prof_calls[i];
        total_overhead += prof_overhead * prof_calls[i];
    }
    /* Invisible overhead from the PROF_Total_Emulation slot isn't included
     * in the first place, so don't try to subtract it out */
    total_overhead -= (prof_overhead - visible_overhead)
                      * prof_calls[PROF_Total_Emulation];
    usec[PROF_Total_Emulation] -= total_overhead;

    /* Sort the slots by total run time.  There aren't many slots, so a
     * selection sort will do fine */
    for (i = 0; i < num_slots-1; i++) {
        int best = i;
        for (j = i+1; j < num_slots; j++) {
            if (usec[order[j]] > usec[order[best]]) {
                best = j;
            }
        }
        if (best != i) {
            const int tmp = order[i];
            order[i] = order[best];
            order[best] = tmp;
        }
    }

    /* Print out the sorted info, adjusting for overhead */
    printf("   Calls   Total (s)   usec/call   %% of max   Name\n");
    const double max_usec = usec[order[0]];
    for (i = 0; i < num_slots; i++) {
        const double sec = usec[order[i]] / 1000000.0;
        const double avg = (sec / prof_calls[order[i]]) * 1000000.0;
        const double pct= (usec[order[i]] / max_usec) * 100.0;
        printf("%8u   %9.3f   %9.2f    %5.1f%%    %s\n",
               prof_calls[order[i]], sec, avg, pct, prof_names[order[i]]);
    }
    printf("%8u   %9.3f   %9.2f    %5.1f%%    %s\n",
           total_calls, total_overhead / 1000000.0,
           (total_overhead / total_calls),
           (total_overhead / max_usec) * 100.0, "(profiling overhead)");
}

/*************************************************************************/
/*************************************************************************/

#endif  // #ifdef SYS_PROFILE_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
