/*  src/psp/config.h: Header for configuration data management for PSP
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

#ifndef PSP_CONFIG_H
#define PSP_CONFIG_H

/*************************************************************************/

/**
 * CONFIG_BUTTON_*:  Constants identifying Saturn controller buttons which
 * are passed to the config_get_button() and config_set_button() functions.
 */
typedef enum ConfigButtonID_ {
    CONFIG_BUTTON_A = 0,
    CONFIG_BUTTON_B,
    CONFIG_BUTTON_C,
    CONFIG_BUTTON_X,
    CONFIG_BUTTON_Y,
    CONFIG_BUTTON_Z,
} ConfigButtonID;

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
extern void config_load(void);

/**
 * config_save:  Save the current configuration to the configuration file.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int config_save(void);

/**
 * config_get_*:  Retrieve the current value of a configuration variable.
 *
 * [Parameters]
 *     id: Button ID (only for config_get_button())
 * [Return value]
 *     Current value of configuration variable
 */
extern const char *config_get_path_bios(void);
extern const char *config_get_path_cd(void);
extern const char *config_get_path_bup(void);
extern int config_get_start_in_emu(void);
extern int config_get_use_me(void);
extern uint32_t config_get_me_writeback_period(void);
extern uint32_t config_get_me_uncached_boundary(void);
extern int config_get_bup_autosave(void);
extern uint32_t config_get_button(ConfigButtonID id);
extern int config_get_module_sh2(void);
extern int config_get_module_video(void);
extern int config_get_cache_textures(void);
extern int config_get_smooth_textures(void);
extern int config_get_smooth_hires(void);
extern int config_get_enable_rotate(void);
extern int config_get_optimize_rotate(void);
extern int config_get_frameskip_auto(void);
extern int config_get_frameskip_num(void);
extern int config_get_frameskip_interlace(void);
extern int config_get_frameskip_rotate(void);
extern int config_get_show_fps(void);
extern uint32_t config_get_sh2_optimizations(void);
extern int config_get_deciline_mode(void);
extern int config_get_audio_sync(void);
extern int config_get_clock_sync(void);
extern int config_get_clock_fixed_time(void);

/**
 * config_set_*:  Set the value of a configuration variable.
 *
 * [Parameters]
 *        id: Button ID (only for config_get_button())
 *     value: New value for configuration variable
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int config_set_path_bios(const char *value);
extern int config_set_path_cd(const char *value);
extern int config_set_path_bup(const char *value);
extern int config_set_start_in_emu(int value);
extern int config_set_use_me(int value);
extern int config_set_me_writeback_period(uint32_t value);
extern int config_set_me_uncached_boundary(uint32_t value);
extern int config_set_bup_autosave(int value);
extern int config_set_button(ConfigButtonID id, uint32_t value);
extern int config_set_module_sh2(int value);
extern int config_set_module_video(int value);
extern int config_set_cache_textures(int value);
extern int config_set_smooth_textures(int value);
extern int config_set_smooth_hires(int value);
extern int config_set_enable_rotate(int value);
extern int config_set_optimize_rotate(int value);
extern int config_set_frameskip_auto(int value);
extern int config_set_frameskip_num(int value);
extern int config_set_frameskip_interlace(int value);
extern int config_set_frameskip_rotate(int value);
extern int config_set_show_fps(int value);
extern int config_set_sh2_optimizations(uint32_t value);
extern int config_set_deciline_mode(int value);
extern int config_set_audio_sync(int value);
extern int config_set_clock_sync(int value);
extern int config_set_clock_fixed_time(int value);

/*************************************************************************/

#endif  // PSP_CONFIG_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
