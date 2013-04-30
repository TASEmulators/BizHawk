/*  src/psp/misc.c: PSP support routines
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

#include "../memory.h"

#include "config.h"
#include "misc.h"
#include "sys.h"

/*************************************************************************/
/************************** Interface routines ***************************/
/*************************************************************************/

/**
 * save_backup_ram:  Save the contents of backup RAM to the configured
 * file.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on failure
 */
extern int save_backup_ram(void)
{
    const char *path = config_get_path_bup();
    if (!path || !*path) {
        DMSG("No backup RAM file configured!");
        goto error_return;
    }

    /* Lock the power switch while writing so the user (hopefully) can't
     * shut the PSP off on us. */
    scePowerLock(0);

    int fd = sceIoOpen(path, PSP_O_WRONLY | PSP_O_CREAT | PSP_O_TRUNC, 0600);
    if (fd < 0) {
        DMSG("open(%s): %s", path, psp_strerror(fd));
        goto error_unlock_power;
    }

    int res = sceIoWrite(fd, BupRam, 0x10000);
    if (res != 0x10000) {
        DMSG("write(%s): %s", path, psp_strerror(fd));
        sceIoClose(fd);
        goto error_unlock_power;
    }

    res = sceIoClose(fd);
    if (res != 0) {
        DMSG("close(%s): %s", path, psp_strerror(fd));
        goto error_unlock_power;
    }

    /* All done--don't forget to unlock the power switch before returning! */
    scePowerUnlock(0);
    return 1;

  error_unlock_power:
    scePowerUnlock(0);
  error_return:
    return 0;
}

/*************************************************************************/

/**
 * psp_writeback_cache_for_scsp:  Write back all dirty data from the SC's
 * cache for an ScspExec() call, depending on the writeback frequency
 * selected by the user.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if writeback was executed, zero if writeback was skipped
 */
int psp_writeback_cache_for_scsp(void)
{
    static uint32_t counter;

    counter++;
    if (!(counter & (config_get_me_writeback_period() - 1))) {
        sceKernelDcacheWritebackAll();
        return 1;
    } else {
        return 0;
    }
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
