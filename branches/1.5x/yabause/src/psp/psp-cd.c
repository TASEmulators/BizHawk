/*  src/psp/psp-cd.c: PSP virtual CD interface module
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

#include "../cdbase.h"

#include "psp-cd.h"
#include "sys.h"

#ifdef SYS_PROFILE_H
# include "profile.h"  // Can only be our own
#else
# define DONT_PROFILE
# include "../profile.h"
#endif

/*************************************************************************/
/************************* Interface definition **************************/
/*************************************************************************/

/* Interface function declarations (must come before interface definition) */

static int psp_cd_init(const char *path);
static void psp_cd_deinit(void);
static int psp_cd_get_status(void);
static s32 psp_cd_read_toc(u32 *toc_buffer);
static int psp_cd_read_sector(u32 sector, void *buffer);
static void psp_cd_read_ahead(u32 sector);

/*-----------------------------------------------------------------------*/

/* Module interface definition */

CDInterface CDPSP = {
    .id            = CDCORE_PSP,
    .Name          = "PSP Virtual CD Interface",
    .Init          = psp_cd_init,
    .DeInit        = psp_cd_deinit,
    .GetStatus     = psp_cd_get_status,
    .ReadTOC       = psp_cd_read_toc,
    .ReadSectorFAD = psp_cd_read_sector,
    .ReadAheadFAD  = psp_cd_read_ahead,
};

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* File descriptor for ISO image (0 = none open) */
static int iso_fd;

/* Table-of-contents data for ISO image */
static uint32_t iso_toc[102];

/* Sector indices and corresponding file offsets for each track */
static struct {
    uint32_t first_sector;  // First sector in track (0 = nonexistent track)
    uint32_t last_sector;   // Last sector in track (inclusive)
    uint32_t file_offset;   // File offset of first sector, in bytes
    uint32_t sector_size;   // Size of each sector in the track, in bytes
} tracks[99];  // tracks[0] = track 1, tracks[1] = track 2, etc.

/*----------------------------------*/

/* Read-ahead buffers and sectors loaded into them.  We read multiple
 * sectors at once to minimize the overhead from system calls. */

#define READ_UNIT  8  // Number of sectors to read at once

static __attribute__((aligned(16)))
    uint8_t readahead_buffer[READ_UNIT*2][2352];
static uint32_t readahead_sector[READ_UNIT*2];

/*----------------------------------*/

/* CD read thread ID */
static int cd_read_thid;

/* Variables for communication with CD read thread (see cd_read_thread()
 * documentation for details) */
static volatile uint8_t cd_read_idle;
static volatile uint8_t cd_read_requested;
static volatile uint8_t cd_read_terminate;
static volatile uint8_t cd_read_index;
static volatile uint32_t cd_read_sector;

/*-----------------------------------------------------------------------*/

/* Local function declarations */

static int examine_iso(int fd);
static int examine_cue(int fd);
static int32_t msf_to_sector(const char *msf);

static void cd_read_thread(void);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * psp_cd_init:  Initialize the virtual CD interface.
 *
 * [Parameters]
 *     path: Pathname for physical device to associate with virtual CD drive
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_cd_init(const char *path)
{
    if (!path || !*path) {
        DMSG("No file given, behaving like an empty drive");
        goto error_return;
    }

    /* Open the requested file. */
    iso_fd = sceIoOpen(path, PSP_O_RDONLY, 0);
    if (iso_fd < 0) {
        DMSG("Failed to open %s: %s", path, psp_strerror(iso_fd));
        goto error_return;
    }

    /* Is it a CUE file? */
    char buf[4];
    if (sceIoRead(iso_fd, buf, 4) != 4) {
        DMSG("Failed to read 4 bytes from %s", path);
        goto error_close_iso;
    }
    const int is_cue = (memcmp(buf, "FILE", 4) == 0);

    /* Record information about the ISO image. */
    memset(iso_toc, 0xFF, sizeof(iso_toc));
    memset(tracks, 0, sizeof(tracks));
    if (is_cue) {
        int new_fd = examine_cue(iso_fd);
        if (new_fd) {
            sceIoClose(iso_fd);
            iso_fd = new_fd;
        } else {
            DMSG("Failed to parse CUE file %s", path);
            goto error_close_iso;
        }
    } else {
        if (!examine_iso(iso_fd)) {
            DMSG("Failed to examine ISO file %s", path);
            goto error_close_iso;
        }
    }

    /* Start up the CD-reading thread. */
    memset(readahead_sector, 0, sizeof(readahead_sector));
    cd_read_idle = 0;
    cd_read_requested = 0;
    cd_read_terminate = 0;
    cd_read_sector = 0;
    cd_read_index = 0;
    cd_read_thid = sys_start_thread("YabauseCDReader", cd_read_thread,
                                    THREADPRI_CD_READ, 0x1000, 0, NULL);
    if (cd_read_thid < 0) {
        DMSG("Failed to start CD reader thread: %s",
             psp_strerror(cd_read_thid));
        goto error_close_iso;
    }

    /* Success! */
    return 0;

    /* On error, we return success and behave like an empty drive. */
  error_close_iso:
    sceIoClose(iso_fd);
  error_return:    
    iso_fd = 0;
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_cd_deinit:  Shut down the virtual CD interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_cd_deinit(void)
{
    if (iso_fd) {
        cd_read_terminate = 1;
        sceKernelWakeupThread(cd_read_thid);
        unsigned int tries = 0;
        while (!sys_delete_thread_if_stopped(cd_read_thid, NULL)) {
            if (++tries > 100) {
                DMSG("CD reader thread failed to terminate, killing it");
                sceKernelTerminateDeleteThread(cd_read_thid);
                break;
            }
            sceKernelDelayThread(1000);  // 1ms
        }
        cd_read_thid = 0;
        sceIoClose(iso_fd);
        iso_fd = 0;
    }
}

/*************************************************************************/

/**
 * psp_cd_get_status:  Return the status of the physical device associated
 * with the virtual CD drive.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     0: CD present, disc spinning
 *     1: CD present, disc not spinning
 *     2: CD not present
 *     3: Tray open
 */
static int psp_cd_get_status(void)
{
    return iso_fd ? 0 : 2;
}

/*************************************************************************/

/**
 * psp_cd_read_toc:  Return the TOC (table of contents) data for the disc
 * currently inserted in the virtual CD drive.
 *
 * [Parameters]
 *     toc_buffer: Buffer into which TOC data is to be stored
 * [Return value]
 *     Number of bytes returned
 */
static s32 psp_cd_read_toc(u32 *toc)
{
    if (!iso_fd) {
        return 0;
    }

    memcpy(toc, iso_toc, sizeof(iso_toc));
    return sizeof(iso_toc);
}

/*************************************************************************/

/**
 * psp_cd_read_sector:  Read a 2352-byte raw sector from the virtual CD.
 *
 * [Parameters]
 *     sector: Linear sector number to read
 *     buffer: Buffer into which sector data is to be stored
 * [Return value]
 *     Nonzero on success, zero on failure
 */
static int psp_cd_read_sector(u32 sector, void *buffer)
{
    if (!iso_fd) {
        return 0;
    }

    /* If the requested sector is already loaded, just return it. */
    unsigned int index;
    for (index = 0; index < lenof(readahead_sector); index++) {
        if (readahead_sector[index] == sector) {
            memcpy(buffer, readahead_buffer[index], 2352);
            return 1;
        }
    }

    /* The sector might have been in the middle of being read in, so wait
     * for any pending read operation to complete and check again. */
    unsigned int tries = 0;
    while (!cd_read_idle) {
        if (++tries > 100) {
            DMSG("Timeout waiting for CD reader to become idle");
            break;
        }
        sceKernelDelayThread(100);  // 0.1ms
    }
    for (index = 0; index < lenof(readahead_sector); index++) {
        if (readahead_sector[index] == sector) {
            memcpy(buffer, readahead_buffer[index], 2352);
            return 1;
        }
    }

    /* The sector isn't available after all, so start a new read request
     * and wait for it to complete. */
    while (cd_read_requested) {
        if (++tries > 100) {
            DMSG("Timeout waiting for request");
            return 0;
        }
        sceKernelDelayThread(100);  // 0.1ms
    }
    cd_read_sector = sector;
    cd_read_index = 0;
    cd_read_requested = 1;
    sceKernelWakeupThread(cd_read_thid);
    tries = 0;
    while (cd_read_requested || !cd_read_idle) {
        if (++tries > 1000) {
            DMSG("Timeout waiting for read");
            return 0;
        }
        sceKernelDelayThread(100);  // 0.1ms
    }

    /* Ensure the sector was actually loaded, and return it. */
    if (readahead_sector[0] != sector) {
        DMSG("Failed to read sector %u", (unsigned int)sector);
        return 0;
    }
    memcpy(buffer, readahead_buffer[0], 2352);
    return 1;
}

/*************************************************************************/

/**
 * psp_cd_read_ahead:  Start an asynchronous read of the given sector from
 * the virtual CD.
 *
 * [Parameters]
 *     sector: Linear sector number to read
 * [Return value]
 *     1 on success, 0 on failure
 */
static void psp_cd_read_ahead(u32 sector)
{
    if (!iso_fd) {
        return;
    }

    /* See whether the requested sector has already been read in. */
    unsigned int index;
    for (index = 0; index < lenof(readahead_sector); index++) {
        if (readahead_sector[index] == sector) {
            break;
        }
    }

    uint32_t req_sector;     // Sector to request
    unsigned int req_index;  // Target buffer index to request

    if (index < lenof(readahead_sector)) {
        /* If the sector has already been read in, we normally don't need
         * do anything.  But if we're approaching the end of the current
         * group of READ_UNIT sector buffers and the following sectors
         * haven't yet been read in, start that read now. */
        if (index % READ_UNIT < READ_UNIT/2) {
            return;
        }
        const unsigned int group_index = (index / READ_UNIT) * READ_UNIT;
        const unsigned int next_group =
            (group_index + READ_UNIT) % lenof(readahead_sector);
        const uint32_t next_group_sector =
            readahead_sector[group_index + (READ_UNIT-1)];
        if (!next_group_sector) {
            return;  // We must have reached the end of the track
        }
        if (readahead_sector[next_group] == next_group_sector) {
            return;  // Following sectors are already read in
        }
        req_sector = next_group_sector;
        req_index = next_group;
    } else {
        /* The sector wasn't available, so start a new read operation. */
        req_sector = sector;
        req_index = 0;
    }

    /* If there's a pending read request, we can't do anything (so as not
     * to block). */
    if (cd_read_requested) {
        DMSG("Async read in progress, skipping readahead");
        return;
    }

    /* Pass the requested sector and buffer index to the read thread. */
    cd_read_sector = req_sector;
    cd_read_index = req_index;
    cd_read_requested = 1;
    sceKernelWakeupThread(cd_read_thid);
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * examine_iso:  Examine a raw ISO image and fill in the iso_toc[] and
 * tracks[] arrays with its information.
 *
 * [Parameters]
 *     fd: File descriptor for ISO image
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int examine_iso(int fd)
{
    /* Retrieve the size of the ISO image. */

    const uint32_t file_size = sceIoLseek(fd, 0, PSP_SEEK_END);
    if ((int32_t)file_size < 0) {
        DMSG("Failed to retrieve ISO file size: %s", psp_strerror(file_size));
        return 0;
    } else if (file_size == 0) {
        DMSG("ISO file is empty!");
        return 0;
    }

    /* Guess at the sector size based on the file size. */

    unsigned int sector_size;
    if (file_size % 2048 == 0 && file_size % 2352 == 0) {
        /* It could be either 2048 or 2352 bytes per sector.  Try and find
         * an ISO filesystem header to tell which it is. */
        char buf[8];
        sceIoLseek(fd, 2048*16, PSP_SEEK_SET);
        if (sceIoRead(fd, buf, 8) != 8) {
            DMSG("Failed to read 8 bytes from offset 2048*16");
            return 0;
        }
        if (memcmp(buf, "\1CD001\1"/*\0*/, 8) == 0) {
            sector_size = 2048;
        } else {
            sceIoLseek(fd, 2352*16+16, PSP_SEEK_SET);
            if (sceIoRead(fd, buf, 8) != 8) {
                DMSG("Failed to read 8 bytes from offset 2352*16+16");
                return 0;
            }
            if (memcmp(buf, "\1CD001\1"/*\0*/, 8) == 0) {
                sector_size = 2352;
            } else {
                DMSG("Can't find an ISO9660 header, assuming 2048-byte"
                     " sectors");
                sector_size = 2048;
            }
        }
    } else if (file_size % 2048 == 0) {
        /* Not a multiple of 2352 bytes, so presumably 2048-byte sectors. */
        sector_size = 2048;
    } else if (file_size % 2352 == 0) {
        /* Not a multiple of 2048 bytes, so presumably 2352-byte sectors. */
        sector_size = 2352;
    } else {
        DMSG("Can't figure out sector size, assuming 2048-byte sectors");
        sector_size = 2048;
    }

    /* Fill in the TOC and track table. */

    const uint32_t num_sectors = file_size / sector_size;

    iso_toc[  0] = 0x41000000 | 150;
    iso_toc[ 99] = 0x41010000;
    iso_toc[100] = 0x41010100;
    iso_toc[101] = 0x41000000 | num_sectors;

    tracks[0].first_sector = 150;
    tracks[0].last_sector  = 150 + num_sectors - 1;
    tracks[0].file_offset  = 0;
    tracks[0].sector_size  = sector_size;

    /* All done! */

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * examine_cue:  Examine a CUE file, open the corresponding ISO image file,
 * and fill in the iso_toc[] and tracks[] arrays with the image information.
 *
 * [Parameters]
 *     fd: File descriptor for CUE file
 * [Return value]
 *     File descriptor for ISO image (nonzero) on success, zero on error
 */
static int examine_cue(int fd)
{
    /* Load the entire CUE file into memory. */

    const uint32_t file_size = sceIoLseek(fd, 0, PSP_SEEK_END);
    if ((int32_t)file_size < 0) {
        DMSG("Failed to retrieve CUE file size: %s", psp_strerror(file_size));
        goto error_return;
    } else if (file_size == 0) {
        /* This should be impossible, since we read from the file to
         * determine that it's a CUE file, but let's play it safe anyway... */
        DMSG("CUE file is empty!");
        goto error_return;
    } else if (file_size > 1000000) {
        /* Way too big to be a CUE file, so give up rather than trying to
         * load tons of data into memory just to find out that it's bad. */
        DMSG("CUE file is too big! (%u bytes)", file_size);
        goto error_return;
    }

    char *cue_buffer = malloc(file_size);
    if (!cue_buffer) {
        DMSG("Failed to allocate buffer for CUE file (%u bytes)", file_size);
        goto error_return;
    }
    sceIoLseek(fd, 0, PSP_SEEK_SET);
    if (sceIoRead(fd, cue_buffer, file_size) != file_size) {
        DMSG("Failed to read CUE file into memory");
        goto error_free_cue_buffer;
    }

    /* The first line should be FILE "image-file" [...], giving the
     * filename of the corresponding disc image.  Strip any directory name
     * from the image and attempt to open the file. */

    char *s, *eol;
    int new_fd;
    uint32_t image_size;

    eol = cue_buffer + strcspn(cue_buffer, "\r\n");
    if (*eol) {
        *eol++ = 0;
    }
    eol += strspn(eol, "\r\n");
    if (strncmp(cue_buffer, "FILE \"", 6) != 0) {
        DMSG("Invalid CUE format: File does not begin with `FILE \"'");
        goto error_free_cue_buffer;
    }
    char *path = cue_buffer + 6;
    s = strchr(path, '"');
    if (!s) {
        DMSG("Invalid CUE format: FILE path missing closing quote");
        goto error_free_cue_buffer;
    }
    *s = 0;

    new_fd = sceIoOpen(path, PSP_O_RDONLY, 0);
    if (new_fd < 0) {
        DMSG("Failed to open image file %s: %s", path, psp_strerror(new_fd));
        goto error_free_cue_buffer;
    }

    image_size = sceIoLseek(new_fd, 0, PSP_SEEK_END);
    if ((int32_t)image_size < 0) {
        DMSG("Failed to retrieve size of image file %s: %s", path,
             psp_strerror(image_size));
        goto error_close_new_fd;
    }

    /* Process each remaining line in the CUE file. */

    int linenum = 1;
    unsigned int current_track = 0;
    uint32_t pregap_accum = 150;  // Accumulated pregap sectors

    while (*eol) {
        char *line = eol;
        linenum++;
        eol += strcspn(eol, "\r\n");
        if (*eol) {
            *eol++ = 0;
        }
        eol += strspn(eol, "\r\n");

        /* Get the tag, skipping leading spaces; if it's an empty line,
         * just skip to the next one. */

        line += strspn(line, " \t");
        if (!*line) {
            continue;
        }
        char *tag = line;
        line += strcspn(line, " \t");
        if (*line) {
            *line++ = 0;
        }
        line += strspn(line, " \t");

        /* Parse remaining parameters based on the tag type. */

        if (strcmp(tag, "TRACK") == 0) {

            char *track_str = line;
            line += strcspn(line, " \t");
            if (*line) {
                *line++ = 0;
            }
            line += strspn(line, " \t");
            current_track = strtoul(track_str, &s, 10);
            if (*s || current_track < 1 || current_track > 99) {
                DMSG("Invalid CUE format (line %u): Bad track number %s",
                     linenum, track_str);
                goto error_close_new_fd;
            }

            char *mode = line;
            if (strcmp(mode, "AUDIO") == 0) {
                iso_toc[current_track-1] = 0x01000000;  // Sector # comes later
                tracks[current_track-1].sector_size = 2352;
            } else if (strcmp(mode, "MODE1/2352") == 0
                    || strcmp(mode, "MODE2/2352") == 0
            ) {
                iso_toc[current_track-1] = 0x41000000;
                tracks[current_track-1].sector_size = 2352;
            } else if (strcmp(mode, "MODE1/2048") == 0
                    || strcmp(mode, "MODE2/2048") == 0
            ) {
                iso_toc[current_track-1] = 0x41000000;
                tracks[current_track-1].sector_size = 2048;
            }

        } else if (strcmp(tag, "INDEX") == 0) {

            if (!current_track) {
                DMSG("Invalid CUE format (line %u): INDEX tag with no"
                     " current track", linenum);
                goto error_close_new_fd;
            }

            char *index_str = line;
            line += strcspn(line, " \t");
            if (*line) {
                *line++ = 0;
            }
            line += strspn(line, " \t");
            const unsigned int index = strtoul(index_str, &s, 10);
            if (*s) {
                DMSG("Invalid CUE format (line %u): Bad index number %s",
                     linenum, index_str);
                goto error_close_new_fd;
            }
            if (index != 1) {
                continue;  // We only care about index #1
            }

            char *msf = line;
            const int32_t sector = msf_to_sector(msf);
            if (sector < 0) {
                DMSG("Invalid CUE format (line %u): Bad MSF string %s",
                     linenum, msf);
                goto error_close_new_fd;
            }

            iso_toc[current_track-1] |= sector + pregap_accum;
            tracks[current_track-1].first_sector = sector + pregap_accum;
            /* Temporarily store the accumulated pregap in last_sector */
            tracks[current_track-1].last_sector = pregap_accum;

        } else if (strcmp(tag, "PREGAP") == 0) {

            if (!current_track) {
                DMSG("Invalid CUE format (line %u): PREGAP tag with no"
                     " current track", linenum);
                goto error_close_new_fd;
            }

            char *msf = line;
            const int32_t pregap_sectors = msf_to_sector(msf);
            if (pregap_sectors < 0) {
                DMSG("Invalid CUE format (line %u): Bad MSF string %s",
                     linenum, msf);
                goto error_close_new_fd;
            }

            pregap_accum += pregap_sectors;

        } else {

            /* Either an invalid tag or one we don't care about, so skip it. */

        }

    }  // while (*eol)

    if (!tracks[0].first_sector) {
        DMSG("Invalid CUE file: Track 1 missing");
        goto error_close_new_fd;
    }

    /* Fill in the remaining fields in the track table. */

    if (tracks[0].first_sector != tracks[0].last_sector) {
        DMSG("First track does not start at file offset 0, assuming sector"
             " size %u for skipped sectors", tracks[0].sector_size);
        const uint32_t track1_skipped =
            tracks[0].first_sector - tracks[0].last_sector;
        tracks[0].file_offset = track1_skipped * tracks[0].sector_size;
    } else {
        tracks[0].file_offset = 0;
    }

    unsigned int track;
    for (track = 2; track <= current_track; track++) {
        if (!tracks[track-1].first_sector) {
            DMSG("Invalid CUE file: Intermediate track %u missing", track);
            goto error_close_new_fd;
        }
        const uint32_t pregap = tracks[track-1].last_sector;
        const uint32_t added_pregap = pregap - tracks[track-2].last_sector;
        tracks[track-2].last_sector =
            tracks[track-1].first_sector - added_pregap - 1;
        const uint32_t last_track_sectors =
            tracks[track-2].last_sector - tracks[track-2].first_sector + 1;
        tracks[track-1].file_offset =
            tracks[track-2].file_offset
                + (last_track_sectors * tracks[track-2].sector_size);
    }

    const uint32_t last_track_size = image_size - tracks[track-2].file_offset;
    const uint32_t last_track_sectors =
        last_track_size / tracks[track-2].sector_size;
    tracks[track-2].last_sector =
        tracks[track-2].first_sector + last_track_sectors - 1;

    /* Generate the final TOC entries. */

    iso_toc[ 99] = (iso_toc[0] & 0xFF000000) | 0x00010000;
    iso_toc[100] = (iso_toc[current_track-1] & 0xFF000000)
                   | (current_track << 16);
    iso_toc[101] = (iso_toc[current_track-1] & 0xFF000000)
                   | (tracks[current_track-1].last_sector + 1);

    /* All done! */

    free(cue_buffer);
    return new_fd;


    /* Error handling. */

  error_close_new_fd:
    sceIoClose(new_fd);
  error_free_cue_buffer:
    free(cue_buffer);
  error_return:
    return 0;
}

/*----------------------------------*/

/**
 * msf_to_sector:  Convert a time (Minutes:Seconds:Frames) string to the
 * corresponding sector index.  Helper function for examine_cue().
 *
 * [Parameters]
 *     msf: Time string (MM:SS:FF)
 * [Return value]
 *     Corresponding sector index (nonzero), or negative if string is invalid
 */
static int32_t msf_to_sector(const char *msf)
{
    uint32_t minutes, seconds, frames;
    const char *s;

    if (!msf
     || (minutes = strtoul(msf, (char **)&s, 10)) > 99
     || *s++ != ':'
     || (seconds = strtoul(s, (char **)&s, 10)) > 59
     || *s++ != ':'
     || (frames  = strtoul(s, (char **)&s, 10)) > 74
     || *s != 0
    ) {
        return -1;
    }

    return (minutes*60 + seconds)*75 + frames;
}

/*************************************************************************/
/**************************** CD read thread *****************************/
/*************************************************************************/

/**
 * cd_read_thread:  Thread which performs reads from the CD image file as
 * requested by the main program.
 *
 * The main program should follow these steps to request a read:
 *    1) Wait for cd_read_requested to become zero.
 *    2) Store the desired sector address (150-...) in cd_read_sector.
 *    3) Store the desired target readahead buffer index in cd_read_index.
 *          This value must be a multiple of READ_UNIT.
 *    4) Store 1 in cd_read_requested.
 *    5) Call sceKernelWakeupThread() to wake up the read thread.
 * The main program can determine whether all pending requests have been
 * completed by checking cd_read_idle for a nonzero value.
 *
 * Sectors which have been read in are stored in the read-ahead buffer;
 * for any nonzero entry in readahead_sector[], the corresponding entry in
 * readahead_buffer[] contains the raw data for that sector (converted, if
 * necessary, from the format in the file--i.e., 2048-byte sectors in the
 * file are filled out to 2352-byte sectors in readahead_buffer[]).
 *
 * To terminate the thread, the main program should store 1 in
 * cd_read_terminate and wake up the thread.
 *
 * The file descriptor (iso_fd) and track table (tracks[]) must not be
 * modified while the thread is running.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Does not return
 */
static void cd_read_thread(void)
{
    PROFILE_START("CDIO");

    while (!cd_read_terminate) {

        /* Wait for a trigger write from the main program. */
        while (!cd_read_requested && !cd_read_terminate) {
            cd_read_idle = 1;
            /* Since the read thread runs at a higher priority than the
             * main program thread, there is (barring an OS bug) no danger
             * of a race condition causing the read thread to be
             * interrupted by the main thread between the while() test and
             * this sleep call. */
            PROFILE_STOP("CDIO");
            sceKernelSleepThread();
            PROFILE_START("CDIO");
        }
        if (cd_read_terminate) {
            break;
        }
        cd_read_idle = 0;

        /* Save the requested sector number, then clear the request trigger. */
        const uint32_t sector = cd_read_sector;
        const unsigned int index = cd_read_index;
        cd_read_requested = 0;

        /* Figure out which track the sector is in and retrieve the track
         * information. */
        unsigned int track;
        for (track = 0; track < lenof(tracks); track++) {
            if (sector >= tracks[track].first_sector
             && sector <= tracks[track].last_sector
            ) {
                break;
            }
        }
        if (track >= lenof(tracks)) {
            DMSG("Failed to find track for sector %u", (unsigned int)sector);
            continue;
        }
        const uint32_t first_sector = tracks[track].first_sector;
        const uint32_t file_offset  = tracks[track].file_offset;
        const uint32_t sector_size  = tracks[track].sector_size;

        /* Clear out all sector information previously stored in this group
         * of buffers. */
        unsigned int i;
        for (i = index; i < index + READ_UNIT; i++) {
            readahead_sector[i] = 0;
        }

        /* Check how many sectors we should read from this location (avoid
         * reading beyond the end of the track). */
        unsigned int sectors_to_read = READ_UNIT;
        if (sectors_to_read > (tracks[track].last_sector+1) - sector) {
            sectors_to_read = (tracks[track].last_sector+1) - sector;
        }

        /* Seek to the proper location in the disc image. */
        const uint32_t relative_sector = sector - first_sector;
        const uint32_t relative_offset = relative_sector * sector_size;
        const uint32_t absolute_offset = file_offset + relative_offset;
        uint32_t res = sceIoLseek(iso_fd, absolute_offset, PSP_SEEK_SET);
        if (res != absolute_offset) {
            DMSG("sceIoLseek(%u, %u, SEEK_SET): %s", iso_fd, absolute_offset,
                 psp_strerror(res));
            continue;
        }

        /* Read the sector data from the disc image. */
        if (sector_size == 2352) {
            PROFILE_STOP("CDIO");
            res = sceIoRead(iso_fd, readahead_buffer[index],
                            2352 * sectors_to_read);
            PROFILE_START("CDIO");
        } else if (sector_size == 2048) {
            static const uint8_t sector_header[16] =
                {0, 255, 255, 255, 255, 255, 255, 255,
                 255, 255, 255, 0, 0, 0, 0, 0};
            memcpy(readahead_buffer[index], sector_header, 16);
            PROFILE_STOP("CDIO");
            res = sceIoRead(iso_fd, (uint8_t *)readahead_buffer[index] + 16,
                            2048 * sectors_to_read);
            PROFILE_START("CDIO");
            for (i = sectors_to_read-1; i >= 1; i--) {
                /* We have to move the data first, or the sector header
                 * could overwrite part of the sector we're moving. */
                memmove((uint8_t *)readahead_buffer[index+i] + 16,
                        (uint8_t *)readahead_buffer[index] + 16 + 2048*i,
                        2048);
                memcpy(readahead_buffer[index+i], sector_header, 16);
            }
        } else {
            DMSG("IMPOSSIBLE: bad sector_size %u for track %u",
                 sector_size, track);
            continue;
        }
        if (res != sector_size * sectors_to_read) {
            DMSG("sceIoRead(%u, buffer[%u], %u): %s", iso_fd, index,
                 sector_size, res < 0 ? psp_strerror(res) : "Short read");
            continue;
        }

        /* Fill in readahead_sector[] with the newly-read-in sectors. */
        for (i = 0; i < sectors_to_read; i++) {
            readahead_sector[index+i] = sector+i;
        }

    }  // while (!cd_read_terminate)

    PROFILE_STOP("CDIO");
    sceKernelExitThread(0);
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
