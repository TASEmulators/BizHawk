/*  src/psp/profile.h: Profiling header for Yabause core
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

#ifndef PSP_PROFILE_H
#define PSP_PROFILE_H

#include <pspuser.h>  // for sceKernelGetSystemTimeLow()

/*************************************************************************/

/* Tracking slots */
enum {
    PROF__UNKNOWN = 0,  // For any non-matching name
    PROF_Total_Emulation,
    PROF_MSH2,
    PROF_68K,
    PROF_SCSP,
    PROF_SCU,
    PROF_SSH2,
    PROF_CDB,
    PROF_CDIO,
    PROF_SMPC,
    PROF_hblankin,
    PROF_hblankout,
    PROF_VDP1_VDP2,
    PROF_vblankin,
    PROF__OVERHEAD,  // Internal use only (for calculating profiling overhead)
    PROF__END
};

/*-----------------------------------------------------------------------*/

/* Helper macro that converts string parameters to PROFILE_{START,STOP}()
 * into PROF_* indices.  Note that as long as the parameter is a string
 * literal, compiler optimization will convert the whole thing into a
 * single compile-time constant. */

#define PROFILE_INDEX(name)  \
    (strcmp((name),"Total Emulation") == 0 ? PROF_Total_Emulation :     \
     strcmp((name),"MSH2"           ) == 0 ? PROF_MSH2            :     \
     strcmp((name),"68K"            ) == 0 ? PROF_68K             :     \
     strcmp((name),"SCSP"           ) == 0 ? PROF_SCSP            :     \
     strcmp((name),"SCU"            ) == 0 ? PROF_SCU             :     \
     strcmp((name),"SSH2"           ) == 0 ? PROF_SSH2            :     \
     strcmp((name),"CDB"            ) == 0 ? PROF_CDB             :     \
     strcmp((name),"CDIO"           ) == 0 ? PROF_CDIO            :     \
     strcmp((name),"SMPC"           ) == 0 ? PROF_SMPC            :     \
     strcmp((name),"hblankin"       ) == 0 ? PROF_hblankin        :     \
     strcmp((name),"hblankout"      ) == 0 ? PROF_hblankout       :     \
     strcmp((name),"VDP1/VDP2"      ) == 0 ? PROF_VDP1_VDP2       :     \
     strcmp((name),"vblankin"       ) == 0 ? PROF_vblankin        :     \
                                             PROF__UNKNOWN)

/*-----------------------------------------------------------------------*/

/* Macros called by the Yabause core. */

#define PROFILE_START(name) \
    psp_profile_start(PROFILE_INDEX((name)), sceKernelGetSystemTimeLow())

#define PROFILE_STOP(name) \
    psp_profile_stop(PROFILE_INDEX((name)), sceKernelGetSystemTimeLow())

#define PROFILE_PRINT()  psp_profile_print()

#define PROFILE_RESET()  psp_profile_reset()

/*-----------------------------------------------------------------------*/

/* Function declarations */

extern void psp_profile_reset(void);
extern void psp_profile_start(int slot, uint32_t timestamp);
extern void psp_profile_stop(int slot, uint32_t timestamp);
extern void psp_profile_print(void);

/*************************************************************************/

#endif  // PSP_PROFILE_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
