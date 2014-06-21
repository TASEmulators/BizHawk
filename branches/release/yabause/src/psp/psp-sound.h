/*  src/psp/psp-sound.h: PSP sound output module header
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

#ifndef PSP_SOUND_H
#define PSP_SOUND_H

#include "../scsp.h"  // for SoundInterface_struct

/*************************************************************************/

/* Module interface definition */
extern SoundInterface_struct SNDPSP;

/* Unique module ID (must be different from any in scsp.h) */
#define SNDCORE_PSP  0x5CE  // "SCE"

/*-----------------------------------------------------------------------*/

/**
 * psp_sound_pause:  Stop audio output.  Called when the system is being
 * suspended.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void psp_sound_pause(void);

/**
 * psp_sound_unpause:  Resume audio output.  Called when the system is
 * resuming from a suspend.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void psp_sound_unpause(void);

/**
 * psp_sound_exit:  Terminate all playback in preparation for exiting.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void psp_sound_exit(void);

/*************************************************************************/

#endif  // PSP_SOUND_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
