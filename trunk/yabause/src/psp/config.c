/*  src/psp/config.c: Configuration data management for PSP
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

#include "config.h"
#include "psp-sh2.h"
#include "psp-video.h"
#include "sh2.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Configuration file name (always stored in program directory) */
#define PATH_INI  "yabause.ini"

/* Data file paths */
static char path_bios[256] = "bios.bin";
static char path_cd[256] = "cd.iso";
static char path_bup[256] = "backup.bin";

/* General settings */
static int start_in_emu = 0;
static int use_me = 0;
static uint32_t me_writeback_period = 1;
static uint32_t me_uncached_boundary = 0x800;
static int bup_autosave = 1;

/* Button configuration */
static uint32_t button[6] = {
    [CONFIG_BUTTON_A] = PSP_CTRL_CROSS,
    [CONFIG_BUTTON_B] = PSP_CTRL_CIRCLE,
    [CONFIG_BUTTON_C] = 0,
    [CONFIG_BUTTON_X] = PSP_CTRL_SQUARE,
    [CONFIG_BUTTON_Y] = PSP_CTRL_TRIANGLE,
    [CONFIG_BUTTON_Z] = 0,
};

/* Module selections */
static int module_sh2 = SH2CORE_PSP;
static int module_video = VIDCORE_PSP;

/* Display settings */
static int cache_textures = 1;
static int smooth_textures = 0;
static int smooth_hires = 0;
static int enable_rotate = 1;
static int optimize_rotate = 1;
static int frameskip_auto = 0;
static int frameskip_num = 0;
static int frameskip_interlace = 1;
static int frameskip_rotate = 1;
static int show_fps = 0;

static uint32_t sh2_optimizations = SH2_OPTIMIZE_ASSUME_SAFE_DIVISION
                                  | SH2_OPTIMIZE_BRANCH_TO_RTS
                                  | SH2_OPTIMIZE_FOLD_SUBROUTINES
                                  | SH2_OPTIMIZE_LOCAL_ACCESSES
                                  | SH2_OPTIMIZE_LOCAL_POINTERS
                                  | SH2_OPTIMIZE_MAC_NOSAT
                                  | SH2_OPTIMIZE_POINTERS
                                  | SH2_OPTIMIZE_POINTERS_MAC
                                  | SH2_OPTIMIZE_STACK;
/* All known optimization flags (so we can leave newly-implemented flags at
 * their default values when loading the config file) */
#define SH2_KNOWN_OPTIMIZATIONS  (SH2_OPTIMIZE_ASSUME_SAFE_DIVISION \
                                | SH2_OPTIMIZE_BRANCH_TO_RTS \
                                | SH2_OPTIMIZE_FOLD_SUBROUTINES \
                                | SH2_OPTIMIZE_LOCAL_ACCESSES \
                                | SH2_OPTIMIZE_LOCAL_POINTERS \
                                | SH2_OPTIMIZE_MAC_NOSAT \
                                | SH2_OPTIMIZE_POINTERS \
                                | SH2_OPTIMIZE_POINTERS_MAC \
                                | SH2_OPTIMIZE_STACK)

/* Deciline (precise timing) mode flag */
static int deciline_mode = 0;
/* Audio sync flag */
static int audio_sync = 1;
/* Clock sync flag */
static int clock_sync = 1;
/* Start-from-fixed-time flag */
static int clock_fixed_time = 0;

/*-----------------------------------------------------------------------*/

/* Local function declarations */

static int parse_string(const char *file, int line, const char *name,
                        const char *text, char *buffer, unsigned int bufsize);
static int parse_int(const char *file, int line, const char *name,
                     const char *text, int *value_ret);
static int parse_uint32(const char *file, int line, const char *name,
                        const char *text, uint32_t *value_ret);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * config_load:  Load configuration data from the configuration file.
 * Invalid data is ignored, and options not specified in the configuration
 * file are left unchanged.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void config_load(void)
{
    /* Open the configuration file, aborting if it's not available */
    FILE *f = fopen(PATH_INI, "r");
    if (!f) {
        perror("fopen(" PATH_INI ")");
        return;
    }

    /* Read the entire file in at once and close the file pointer */
    char *filebuf;
    if (fseek(f, 0, SEEK_END) < 0) {
        perror("fseek(SEEK_END)");
      close_and_return:
        fclose(f);
        return;
    }
    long filesize = ftell(f);
    if (filesize < 0) {
        perror("ftell()");
        goto close_and_return;
    }
    if (fseek(f, 0, SEEK_SET) < 0) {
        perror("fseek(SEEK_SET)");
        goto close_and_return;
    }
    filebuf = malloc(filesize+1);  // Leave space for a trailing \0
    if (!filebuf) {
        fprintf(stderr, "No memory for config file buffer (%ld bytes)\n",
                filesize);
        goto close_and_return;
    }
    if (fread(filebuf, filesize, 1, f) != 1) {
        fprintf(stderr, "Failed to read config file\n");
        free(filebuf);
        goto close_and_return;
    }
    fclose(f);
    filebuf[filesize] = 0;

    /* Parse each line of the configuration file; lines are of the form
     *     name=value
     * with no spaces permitted on either side of the "=".  We take care
     * to treat names as case-insensitive and to support any of "\r",
     * "\r\n" or "\n" as a line terminator, in case people edit the file
     * on their own. */

    char *s, *eol;
    int line;
    for (s = filebuf, line = 1; *s; s = eol, line++) {

        eol = s + strcspn(s, "\r\n");
        if (*eol == '\r') {
            *eol++ = 0;
        }
        if (*eol == '\n') {
            *eol++ = 0;
        }
        char *name = s;
        char *value = strchr(s, '=');
        if (!value) {
            fprintf(stderr, "%s:%d: Missing `='\n", PATH_INI, line);
            continue;
        }
        *value++ = 0;

        if (stricmp(name, "path_bios") == 0) {
            parse_string(PATH_INI, line, name, value,
                         path_bios, sizeof(path_bios));

        } else if (stricmp(name, "path_cd") == 0) {
            parse_string(PATH_INI, line, name, value,
                         path_cd, sizeof(path_cd));

        } else if (stricmp(name, "path_bup") == 0) {
            parse_string(PATH_INI, line, name, value,
                         path_bup, sizeof(path_bup));

        } else if (stricmp(name, "start_in_emu") == 0) {
            parse_int(PATH_INI, line, name, value, &start_in_emu);

        } else if (stricmp(name, "use_me") == 0) {
            parse_int(PATH_INI, line, name, value, &use_me);

        } else if (stricmp(name, "me_writeback_period") == 0) {
            parse_uint32(PATH_INI, line, name, value, &me_writeback_period);
            if (!me_writeback_period
             || (me_writeback_period & (me_writeback_period - 1))
            ) {
                fprintf(stderr, "config_load(): Invalid value %u for"
                        " me_writeback_period (must be a power of 2)\n",
                        me_writeback_period);
                me_writeback_period = 1;
            }

        } else if (stricmp(name, "me_uncached_boundary") == 0) {
            parse_uint32(PATH_INI, line, name, value, &me_uncached_boundary);
            if (me_uncached_boundary > 0x80000) {
                me_uncached_boundary = 0x80000;
            }

        } else if (stricmp(name, "bup_autosave") == 0) {
            parse_int(PATH_INI, line, name, value, &bup_autosave);

        } else if (stricmp(name, "button.A") == 0) {
            parse_uint32(PATH_INI, line, name, value,&button[CONFIG_BUTTON_A]);

        } else if (stricmp(name, "button.B") == 0) {
            parse_uint32(PATH_INI, line, name, value,&button[CONFIG_BUTTON_B]);

        } else if (stricmp(name, "button.C") == 0) {
            parse_uint32(PATH_INI, line, name, value,&button[CONFIG_BUTTON_C]);

        } else if (stricmp(name, "button.X") == 0) {
            parse_uint32(PATH_INI, line, name, value,&button[CONFIG_BUTTON_X]);

        } else if (stricmp(name, "button.Y") == 0) {
            parse_uint32(PATH_INI, line, name, value,&button[CONFIG_BUTTON_Y]);

        } else if (stricmp(name, "button.Z") == 0) {
            parse_uint32(PATH_INI, line, name, value,&button[CONFIG_BUTTON_Z]);

        } else if (stricmp(name, "module_sh2") == 0) {
            parse_int(PATH_INI, line, name, value, &module_sh2);

        } else if (stricmp(name, "module_video") == 0) {
            parse_int(PATH_INI, line, name, value, &module_video);

        } else if (stricmp(name, "cache_textures") == 0) {
            parse_int(PATH_INI, line, name, value, &cache_textures);

        } else if (stricmp(name, "smooth_textures") == 0) {
            parse_int(PATH_INI, line, name, value, &smooth_textures);

        } else if (stricmp(name, "smooth_hires") == 0) {
            parse_int(PATH_INI, line, name, value, &smooth_hires);

        } else if (stricmp(name, "enable_rotate") == 0) {
            parse_int(PATH_INI, line, name, value, &enable_rotate);

        } else if (stricmp(name, "optimize_rotate") == 0) {
            parse_int(PATH_INI, line, name, value, &optimize_rotate);

        } else if (stricmp(name, "frameskip_auto") == 0) {
            parse_int(PATH_INI, line, name, value, &frameskip_auto);

        } else if (stricmp(name, "frameskip_num") == 0) {
            parse_int(PATH_INI, line, name, value, &frameskip_num);
            if (frameskip_num < 0) {
                frameskip_num = 0;
            } else if (frameskip_num > 9) {
                frameskip_num = 9;
            }

        } else if (stricmp(name, "frameskip_interlace") == 0) {
            parse_int(PATH_INI, line, name, value, &frameskip_interlace);

        } else if (stricmp(name, "frameskip_rotate") == 0) {
            parse_int(PATH_INI, line, name, value, &frameskip_rotate);

        } else if (stricmp(name, "show_fps") == 0) {
            parse_int(PATH_INI, line, name, value, &show_fps);

        } else if (stricmp(name, "sh2_optimizations") == 0) {
            uint32_t newval = strtoul(value, &s, 10);
            if (*s != '/') {
                fprintf(stderr, "%s:%d: Bad format for `%s' value\n",
                        PATH_INI, line, name);
                continue;
            }
            uint32_t mask = strtoul(s+1, &s, 10);
            if (*s) {
                fprintf(stderr, "%s:%d: Bad format for `%s' value\n",
                        PATH_INI, line, name);
                continue;
            }
            sh2_optimizations &= ~mask;
            sh2_optimizations |= newval & mask;

        } else if (stricmp(name, "deciline_mode") == 0) {
            parse_int(PATH_INI, line, name, value, &deciline_mode);

        } else if (stricmp(name, "audio_sync") == 0) {
            parse_int(PATH_INI, line, name, value, &audio_sync);

        } else if (stricmp(name, "clock_sync") == 0) {
            parse_int(PATH_INI, line, name, value, &clock_sync);

        } else if (stricmp(name, "clock_fixed_time") == 0) {
            parse_int(PATH_INI, line, name, value, &clock_fixed_time);

        } else {
            fprintf(stderr, "%s:%d: Unknown configuration variable `%s'\n",
                    PATH_INI, line, name);

        }

    }  // for (s = filebuf, line = 1; *s; s = eol, line++)
}

/*-----------------------------------------------------------------------*/

/**
 * config_save:  Save the current configuration to the configuration file.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
int config_save(void)
{
    FILE *f = fopen(PATH_INI, "w");
    if (!f) {
        perror("fopen(" PATH_INI ")");
        return 0;
    }

    if (fprintf(f, "path_bios=%s\n",            path_bios              ) < 0
     || fprintf(f, "path_cd=%s\n",              path_cd                ) < 0
     || fprintf(f, "path_bup=%s\n",             path_bup               ) < 0
     || fprintf(f, "start_in_emu=%d\n",         start_in_emu           ) < 0
     || fprintf(f, "use_me=%d\n",               use_me                 ) < 0
     || fprintf(f, "me_writeback_period=%u\n",  me_writeback_period    ) < 0
     || fprintf(f, "me_uncached_boundary=%u\n", me_uncached_boundary   ) < 0
     || fprintf(f, "bup_autosave=%d\n",         bup_autosave           ) < 0
     || fprintf(f, "button.A=%u\n",             button[CONFIG_BUTTON_A]) < 0
     || fprintf(f, "button.B=%u\n",             button[CONFIG_BUTTON_B]) < 0
     || fprintf(f, "button.C=%u\n",             button[CONFIG_BUTTON_C]) < 0
     || fprintf(f, "button.X=%u\n",             button[CONFIG_BUTTON_X]) < 0
     || fprintf(f, "button.Y=%u\n",             button[CONFIG_BUTTON_Y]) < 0
     || fprintf(f, "button.Z=%u\n",             button[CONFIG_BUTTON_Z]) < 0
     || fprintf(f, "module_sh2=%d\n",           module_sh2             ) < 0
     || fprintf(f, "module_video=%d\n",         module_video           ) < 0
     || fprintf(f, "cache_textures=%d\n",       cache_textures         ) < 0
     || fprintf(f, "smooth_textures=%d\n",      smooth_textures        ) < 0
     || fprintf(f, "smooth_hires=%d\n",         smooth_hires           ) < 0
     || fprintf(f, "enable_rotate=%d\n",        enable_rotate          ) < 0
     || fprintf(f, "optimize_rotate=%d\n",      optimize_rotate        ) < 0
     || fprintf(f, "frameskip_auto=%d\n",       frameskip_auto         ) < 0
     || fprintf(f, "frameskip_num=%d\n",        frameskip_num          ) < 0
     || fprintf(f, "frameskip_interlace=%d\n",  frameskip_interlace    ) < 0
     || fprintf(f, "frameskip_rotate=%d\n",     frameskip_rotate       ) < 0
     || fprintf(f, "show_fps=%d\n",             show_fps               ) < 0
     || fprintf(f, "sh2_optimizations=%u/%u\n", sh2_optimizations,
                                                SH2_KNOWN_OPTIMIZATIONS) < 0
     || fprintf(f, "deciline_mode=%d\n",        deciline_mode          ) < 0
     || fprintf(f, "audio_sync=%d\n",           audio_sync             ) < 0
     || fprintf(f, "clock_sync=%d\n",           clock_sync             ) < 0
     || fprintf(f, "clock_fixed_time=%d\n",     clock_fixed_time       ) < 0
    ) {
        perror("fprintf(" PATH_INI ",...)");
        fclose(f);
        return 0;
    }

    if (fclose(f) < 0) {
        perror("fclose(" PATH_INI ")");
        return 0;
    }

    return 1;
}

/*************************************************************************/

/**
 * config_get_*:  Retrieve the current value of a configuration variable.
 *
 * [Parameters]
 *     id: Button ID (only for config_get_button())
 * [Return value]
 *     Current value of configuration variable
 */

const char *config_get_path_bios(void)
{
    return path_bios;
}

const char *config_get_path_cd(void)
{
    return path_cd;
}

const char *config_get_path_bup(void)
{
    return path_bup;
}

int config_get_start_in_emu(void)
{
    return start_in_emu;
}

int config_get_use_me(void)
{
    return use_me;
}

uint32_t config_get_me_writeback_period(void)
{
    return me_writeback_period;
}

uint32_t config_get_me_uncached_boundary(void)
{
    return me_uncached_boundary;
}

int config_get_bup_autosave(void)
{
    return bup_autosave;
}

uint32_t config_get_button(ConfigButtonID id)
{
    PRECOND(id >= CONFIG_BUTTON_A && id <= CONFIG_BUTTON_Z, return 0);
    return button[id];
}

int config_get_module_sh2(void)
{
    return module_sh2;
}

int config_get_module_video(void)
{
    return module_video;
}

int config_get_cache_textures(void)
{
    return cache_textures;
}

int config_get_smooth_textures(void)
{
    return smooth_textures;
}

int config_get_smooth_hires(void)
{
    return smooth_hires;
}

int config_get_enable_rotate(void)
{
    return enable_rotate;
}

int config_get_optimize_rotate(void)
{
    return optimize_rotate;
}

int config_get_frameskip_auto(void)
{
    return frameskip_auto;
}

int config_get_frameskip_num(void)
{
    return frameskip_num;
}

int config_get_frameskip_interlace(void)
{
    return frameskip_interlace;
}

int config_get_frameskip_rotate(void)
{
    return frameskip_rotate;
}

int config_get_show_fps(void)
{
    return show_fps;
}

uint32_t config_get_sh2_optimizations(void)
{
    return sh2_optimizations;
}

int config_get_deciline_mode(void)
{
    return deciline_mode;
}

int config_get_audio_sync(void)
{
    return audio_sync;
}

int config_get_clock_sync(void)
{
    return clock_sync;
}

int config_get_clock_fixed_time(void)
{
    return clock_fixed_time;
}

/*-----------------------------------------------------------------------*/

/**
 * config_set_*:  Set the value of a configuration variable.
 *
 * [Parameters]
 *        id: Button ID (only for config_get_button())
 *     value: New value for configuration variable
 * [Return value]
 *     Nonzero on success, zero on error
 */

int config_set_path_bios(const char *value)
{
    PRECOND(value != NULL, return 0);
    if (strlen(value) > sizeof(path_bios) - 1) {
        fprintf(stderr, "config_set_path_bios(): Value too long (max %d"
                " characters): %s\n", sizeof(path_bios) - 1, value);
        return 0;
    }
    strcpy(path_bios, value);  // Safe
    return 1;
}

int config_set_path_cd(const char *value)
{
    PRECOND(value != NULL, return 0);
    if (strlen(value) > sizeof(path_cd) - 1) {
        fprintf(stderr, "config_set_path_cd(): Value too long (max %d"
                " characters): %s\n", sizeof(path_cd) - 1, value);
        return 0;
    }
    strcpy(path_cd, value);  // Safe
    return 1;
}

int config_set_path_bup(const char *value)
{
    PRECOND(value != NULL, return 0);
    if (strlen(value) > sizeof(path_bup) - 1) {
        fprintf(stderr, "config_set_path_bup(): Value too long (max %d"
                " characters): %s\n", sizeof(path_bup) - 1, value);
        return 0;
    }
    strcpy(path_bup, value);  // Safe
    return 1;
}

int config_set_start_in_emu(int value)
{
    start_in_emu = value ? 1 : 0;
    return 1;
}

int config_set_use_me(int value)
{
    use_me = value ? 1 : 0;
    return 1;
}

int config_set_me_writeback_period(uint32_t value)
{
    if (value == 0 || (value & (value-1))) {
        fprintf(stderr, "config_set_me_writeback_period(): Invalid period %u"
                " (must be a power of 2)\n", value);
        return 0;
    }
    me_writeback_period = value;
    return 1;
}

int config_set_me_uncached_boundary(uint32_t value)
{
    if (value > 0x80000) {
        fprintf(stderr, "config_set_me_uncached_boundary(): Invalid boundary"
                " %u (maximum %u)\n", value, 0x80000);
        return 0;
    }
    me_uncached_boundary = value;
    return 1;
}

int config_set_bup_autosave(int value)
{
    bup_autosave = value ? 1 : 0;
    return 1;
}

int config_set_button(ConfigButtonID id, uint32_t value)
{
    PRECOND(id >= CONFIG_BUTTON_A && id <= CONFIG_BUTTON_Z, return 0);
    button[id] = value;
    return 1;
}

int config_set_module_sh2(int value)
{
    module_sh2 = value;
    return 1;
}

int config_set_module_video(int value)
{
    module_video = value;
    return 1;
}

int config_set_cache_textures(int value)
{
    cache_textures = value ? 1 : 0;
    return 1;
}

int config_set_smooth_textures(int value)
{
    smooth_textures = value ? 1 : 0;
    return 1;
}

int config_set_smooth_hires(int value)
{
    smooth_hires = value ? 1 : 0;
    return 1;
}

int config_set_enable_rotate(int value)
{
    enable_rotate = value ? 1 : 0;
    return 1;
}

int config_set_optimize_rotate(int value)
{
    optimize_rotate = value ? 1 : 0;
    return 1;
}

int config_set_frameskip_auto(int value)
{
    frameskip_auto = value ? 1 : 0;
    return 1;
}

int config_set_frameskip_num(int value)
{
    if (value < 0) {
        frameskip_num = 0;
    } else if (value > 9) {
        frameskip_num = 9;
    } else {
        frameskip_num = value;
    }
    return 1;
}

int config_set_frameskip_interlace(int value)
{
    frameskip_interlace = value ? 1 : 0;
    return 1;
}

int config_set_frameskip_rotate(int value)
{
    frameskip_rotate = value ? 1 : 0;
    return 1;
}

int config_set_show_fps(int value)
{
    show_fps = value ? 1 : 0;
    return 1;
}

int config_set_sh2_optimizations(uint32_t value)
{
    sh2_optimizations = value & SH2_KNOWN_OPTIMIZATIONS;
    return 1;
}

int config_set_deciline_mode(int value)
{
    deciline_mode = value ? 1 : 0;
    return 1;
}

int config_set_audio_sync(int value)
{
    audio_sync = value ? 1 : 0;
    return 1;
}

int config_set_clock_sync(int value)
{
    clock_sync = value ? 1 : 0;
    return 1;
}

int config_set_clock_fixed_time(int value)
{
    clock_fixed_time = value ? 1 : 0;
    return 1;
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * parse_string:  Parse a string-type configuration entry.
 *
 * [Parameters]
 *        file: Name of configuration file (for error messages)
 *        line: Line number in configuration file (for error messages)
 *        name: Configuration entry name (for error messages)
 *        text: Configuration value text
 *      buffer: Buffer into which to store string value
 *     bufsize: Size of buffer (maximum string length + 1)
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int parse_string(const char *file, int line, const char *name,
                        const char *text, char *buffer, unsigned int bufsize)
{
    PRECOND(text != NULL, return 0);
    PRECOND(buffer != NULL, return 0);

    if (strlen(text) > bufsize - 1) {
        fprintf(stderr, "%s:%d: String for `%s' too long (max %d"
                " characters)\n", file, line, name, bufsize - 1);
        return 0;
    }
    strcpy(buffer, text);  // Safe
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * parse_int:  Parse an integer-type configuration entry.
 *
 * [Parameters]
 *          file: Name of configuration file (for error messages)
 *          line: Line number in configuration file (for error messages)
 *          name: Configuration entry name (for error messages)
 *          text: Configuration value text
 *     value_ret: Pointer to variable into which to store integer value
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int parse_int(const char *file, int line, const char *name,
                     const char *text, int *value_ret)
{
    PRECOND(text != NULL, return 0);
    PRECOND(value_ret != NULL, return 0);

    char *s;
    int newval = strtol(text, &s, 10);
    if (*s) {
        fprintf(stderr, "%s:%d: Value for `%s' must be a number\n",
                file, line, name);
        return 0;
    }
    *value_ret = newval;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * parse_uint32:  Parse a 32-bit unsigned integer configuration entry.
 *
 * [Parameters]
 *          file: Name of configuration file (for error messages)
 *          line: Line number in configuration file (for error messages)
 *          name: Configuration entry name (for error messages)
 *          text: Configuration value text
 *     value_ret: Pointer to variable into which to store integer value
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int parse_uint32(const char *file, int line, const char *name,
                        const char *text, uint32_t *value_ret)
{
    PRECOND(text != NULL, return 0);
    PRECOND(value_ret != NULL, return 0);

    char *s;
    uint32_t newval = strtoul(text, &s, 10);
    if (*s) {
        fprintf(stderr, "%s:%d: Value for `%s' must be a nonnegative number\n",
                file, line, name);
        return 0;
    }
    *value_ret = newval;
    return 1;
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
