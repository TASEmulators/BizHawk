#include <stdio.h>
#include <stdbool.h>
#include <stdlib.h>
#include <stddef.h>
#include <string.h>
#include <errno.h>
#include <stdarg.h>
#ifndef _WIN32
#include <unistd.h>
#include <sys/select.h>
#endif
#include "gb.h"

void GB_attributed_logv(GB_gameboy_t *gb, GB_log_attributes attributes, const char *fmt, va_list args)
{
    char *string = NULL;
    vasprintf(&string, fmt, args);
    if (string) {
        if (gb->log_callback) {
            gb->log_callback(gb, string, attributes);
        }
        else {
            /* Todo: Add ANSI escape sequences for attributed text */
            printf("%s", string);
        }
    }
    free(string);
}

void GB_attributed_log(GB_gameboy_t *gb, GB_log_attributes attributes, const char *fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    GB_attributed_logv(gb, attributes, fmt, args);
    va_end(args);
}

void GB_log(GB_gameboy_t *gb, const char *fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    GB_attributed_logv(gb, 0, fmt, args);
    va_end(args);
}

static char *default_input_callback(GB_gameboy_t *gb)
{
    char *expression = NULL;
    size_t size = 0;

    if (getline(&expression, &size, stdin) == -1) {
        /* The user doesn't have STDIN or used ^D. We make sure the program keeps running. */
        GB_set_async_input_callback(gb, NULL); /* Disable async input */
        return strdup("c");
    }

    if (!expression) {
        return strdup("");
    }

    size_t length = strlen(expression);
    if (expression[length - 1] == '\n') {
        expression[length - 1] = 0;
    }
    return expression;
}

static char *default_async_input_callback(GB_gameboy_t *gb)
{
#ifndef _WIN32
    fd_set set;
    FD_ZERO(&set);
    FD_SET(STDIN_FILENO, &set);
    struct timeval time = {0,};
    if (select(1, &set, NULL, NULL, &time) == 1) {
        if (feof(stdin)) {
            GB_set_async_input_callback(gb, NULL); /* Disable async input */
            return NULL;
        }
        return default_input_callback(gb);
    }
#endif
    return NULL;
}

void GB_init(GB_gameboy_t *gb)
{
    memset(gb, 0, sizeof(*gb));
    gb->ram = malloc(gb->ram_size = 0x2000);
    gb->vram = malloc(gb->vram_size = 0x2000);

    gb->input_callback = default_input_callback;
    gb->async_input_callback = default_async_input_callback;
    gb->cartridge_type = &GB_cart_defs[0]; // Default cartridge type
    gb->audio_quality = 4;
    
    GB_reset(gb);
}

void GB_init_cgb(GB_gameboy_t *gb)
{
    memset(gb, 0, sizeof(*gb));
    gb->ram = malloc(gb->ram_size = 0x2000 * 8);
    gb->vram = malloc(gb->vram_size = 0x2000 * 2);
    gb->is_cgb = true;

    gb->input_callback = default_input_callback;
    gb->async_input_callback = default_async_input_callback;
    gb->cartridge_type = &GB_cart_defs[0]; // Default cartridge type
    gb->audio_quality = 4;

    GB_reset(gb);
}

void GB_free(GB_gameboy_t *gb)
{
    gb->magic = 0;
    if (gb->ram) {
        free(gb->ram);
    }
    if (gb->vram) {
        free(gb->vram);
    }
    if (gb->mbc_ram) {
        free(gb->mbc_ram);
    }
    if (gb->rom) {
        free(gb->rom);
    }
    if (gb->audio_buffer) {
        free(gb->audio_buffer);
    }
    if (gb->breakpoints) {
        free(gb->breakpoints);
    }
    for (int i = 0x200; i--;) {
        if (gb->bank_symbols[i]) {
            GB_map_free(gb->bank_symbols[i]);
        }
    }
    for (int i = 0x400; i--;) {
        if (gb->reversed_symbol_map.buckets[i]) {
            GB_symbol_t *next = gb->reversed_symbol_map.buckets[i]->next;
            free(gb->reversed_symbol_map.buckets[i]);
            gb->reversed_symbol_map.buckets[i] = next;
        }
    }
    memset(gb, 0, sizeof(*gb));
}

int GB_load_boot_rom(GB_gameboy_t *gb, const char *path)
{
    FILE *f = fopen(path, "rb");
    if (!f) {
        GB_log(gb, "Could not open boot ROM: %s.\n", strerror(errno));
        return errno;
    }
    fread(gb->boot_rom, sizeof(gb->boot_rom), 1, f);
    fclose(f);
    return 0;
}

int GB_load_rom(GB_gameboy_t *gb, const char *path)
{
    FILE *f = fopen(path, "rb");
    if (!f) {
        GB_log(gb, "Could not open ROM: %s.\n", strerror(errno));
        return errno;
    }
    fseek(f, 0, SEEK_END);
    gb->rom_size = (ftell(f) + 0x3FFF) & ~0x3FFF; /* Round to bank */
    /* And then round to a power of two */
    while (gb->rom_size & (gb->rom_size - 1)) {
        /* I promise this works. */
        gb->rom_size |= gb->rom_size >> 1;
        gb->rom_size++;
    }
    fseek(f, 0, SEEK_SET);
    if (gb->rom) {
        free(gb->rom);
    }
    gb->rom = malloc(gb->rom_size);
    memset(gb->rom, 0xFF, gb->rom_size); /* Pad with 0xFFs */
    fread(gb->rom, gb->rom_size, 1, f);
    fclose(f);
    GB_configure_cart(gb);

    return 0;
}

int GB_save_battery(GB_gameboy_t *gb, const char *path)
{
    if (!gb->cartridge_type->has_battery) return 0; // Nothing to save.
    if (gb->mbc_ram_size == 0 && !gb->cartridge_type->has_rtc) return 0; /* Claims to have battery, but has no RAM or RTC */
    FILE *f = fopen(path, "wb");
    if (!f) {
        GB_log(gb, "Could not open battery save: %s.\n", strerror(errno));
        return errno;
    }

    if (fwrite(gb->mbc_ram, 1, gb->mbc_ram_size, f) != gb->mbc_ram_size) {
        fclose(f);
        return EIO;
    }
    if (gb->cartridge_type->has_rtc) {
        if (fwrite(&gb->rtc_real, 1, sizeof(gb->rtc_real), f) != sizeof(gb->rtc_real)) {
            fclose(f);
            return EIO;
        }

        if (fwrite(&gb->last_rtc_second, 1, sizeof(gb->last_rtc_second), f) != sizeof(gb->last_rtc_second)) {
            fclose(f);
            return EIO;
        }
    }

    errno = 0;
    fclose(f);
    return errno;
}

/* Loading will silently stop if the format is incomplete */
void GB_load_battery(GB_gameboy_t *gb, const char *path)
{
    FILE *f = fopen(path, "rb");
    if (!f) {
        return;
    }

    if (fread(gb->mbc_ram, 1, gb->mbc_ram_size, f) != gb->mbc_ram_size) {
        goto reset_rtc;
    }

    if (fread(&gb->rtc_real, 1, sizeof(gb->rtc_real), f) != sizeof(gb->rtc_real)) {
        goto reset_rtc;
    }

    if (fread(&gb->last_rtc_second, 1, sizeof(gb->last_rtc_second), f) != sizeof(gb->last_rtc_second)) {
        goto reset_rtc;
    }

    if (gb->last_rtc_second > time(NULL)) {
        /* We must reset RTC here, or it will not advance. */
        goto reset_rtc;
    }

    if (gb->last_rtc_second < 852076800) { /* 1/1/97. There weren't any RTC games that time,
                                            so if the value we read is lower it means it wasn't
                                            really RTC data. */
        goto reset_rtc;
    }
    goto exit;
reset_rtc:
    gb->last_rtc_second = time(NULL);
    gb->rtc_real.high |= 0x80; /* This gives the game a hint that the clock should be reset. */
exit:
    fclose(f);
    return;
}

void GB_run(GB_gameboy_t *gb)
{
    GB_debugger_run(gb);
    GB_cpu_run(gb);
    if (gb->vblank_just_occured) {
        GB_update_joyp(gb);
        GB_rtc_run(gb);
        GB_debugger_handle_async_commands(gb);
    }
}

uint64_t GB_run_frame(GB_gameboy_t *gb)
{
    /* Configure turbo temporarily, the user wants to handle FPS capping manually. */
    bool old_turbo = gb->turbo;
    bool old_dont_skip = gb->turbo_dont_skip;
    gb->turbo = true;
    gb->turbo_dont_skip = true;
    
    gb->cycles_since_last_sync = 0;
    while (true) {
        GB_run(gb);
        if (gb->vblank_just_occured) {
            break;
        }
    }
    gb->turbo = old_turbo;
    gb->turbo_dont_skip = old_dont_skip;
    return gb->cycles_since_last_sync * FRAME_LENGTH * LCDC_PERIOD;
}

void GB_set_pixels_output(GB_gameboy_t *gb, uint32_t *output)
{
    gb->screen = output;
}

void GB_set_vblank_callback(GB_gameboy_t *gb, GB_vblank_callback_t callback)
{
    gb->vblank_callback = callback;
}

void GB_set_log_callback(GB_gameboy_t *gb, GB_log_callback_t callback)
{
    gb->log_callback = callback;
}

void GB_set_input_callback(GB_gameboy_t *gb, GB_input_callback_t callback)
{
    if (gb->input_callback == default_input_callback) {
        gb->async_input_callback = NULL;
    }
    gb->input_callback = callback;
}

void GB_set_async_input_callback(GB_gameboy_t *gb, GB_input_callback_t callback)
{
    gb->async_input_callback = callback;
}

void GB_set_rgb_encode_callback(GB_gameboy_t *gb, GB_rgb_encode_callback_t callback)
{
    if (!gb->rgb_encode_callback && !gb->is_cgb) {
        gb->sprite_palettes_rgb[4] = gb->sprite_palettes_rgb[0] = gb->background_palettes_rgb[0] =
        callback(gb, 0xFF, 0xFF, 0xFF);
        gb->sprite_palettes_rgb[5] = gb->sprite_palettes_rgb[1] = gb->background_palettes_rgb[1] =
        callback(gb, 0xAA, 0xAA, 0xAA);
        gb->sprite_palettes_rgb[6] = gb->sprite_palettes_rgb[2] = gb->background_palettes_rgb[2] =
        callback(gb, 0x55, 0x55, 0x55);
        gb->sprite_palettes_rgb[7] = gb->sprite_palettes_rgb[3] = gb->background_palettes_rgb[3] =
        callback(gb, 0, 0, 0);
    }
    gb->rgb_encode_callback = callback;
}

void GB_set_infrared_callback(GB_gameboy_t *gb, GB_infrared_callback_t callback)
{
    gb->infrared_callback = callback;
}

void GB_set_infrared_input(GB_gameboy_t *gb, bool state)
{
    gb->infrared_input = state;
    gb->cycles_since_input_ir_change = 0;
    gb->ir_queue_length = 0;
}

void GB_queue_infrared_input(GB_gameboy_t *gb, bool state, long cycles_after_previous_change)
{
    if (gb->ir_queue_length == GB_MAX_IR_QUEUE) {
        GB_log(gb, "IR Queue is full\n");
        return;
    }
    gb->ir_queue[gb->ir_queue_length++] = (GB_ir_queue_item_t){state, cycles_after_previous_change};
}

void GB_set_rumble_callback(GB_gameboy_t *gb, GB_rumble_callback_t callback)
{
    gb->rumble_callback = callback;
}

void GB_set_serial_transfer_start_callback(GB_gameboy_t *gb, GB_serial_transfer_start_callback_t callback)
{
    gb->serial_transfer_start_callback = callback;
}

void GB_set_serial_transfer_end_callback(GB_gameboy_t *gb, GB_serial_transfer_end_callback_t callback)
{
    gb->serial_transfer_end_callback = callback;
}

uint8_t GB_serial_get_data(GB_gameboy_t *gb)
{
    if (gb->io_registers[GB_IO_SC] & 1) {
        /* Internal Clock */
        GB_log(gb, "Serial read request while using internal clock. \n");
        return 0xFF;
    }
    return gb->io_registers[GB_IO_SB];
}
void GB_serial_set_data(GB_gameboy_t *gb, uint8_t data)
{
    if (gb->io_registers[GB_IO_SC] & 1) {
        /* Internal Clock */
        GB_log(gb, "Serial write request while using internal clock. \n");
        return;
    }
    gb->io_registers[GB_IO_SB] = data;
    gb->io_registers[GB_IO_IF] |= 8;
}

void GB_set_sample_rate(GB_gameboy_t *gb, unsigned int sample_rate)
{
    if (gb->audio_buffer) {
        free(gb->audio_buffer);
    }
    gb->buffer_size = sample_rate / 25; // 40ms delay
    gb->audio_buffer = malloc(gb->buffer_size * sizeof(*gb->audio_buffer));
    gb->sample_rate = sample_rate;
    gb->audio_position = 0;
}

void GB_disconnect_serial(GB_gameboy_t *gb)
{
    gb->serial_transfer_start_callback = NULL;
    gb->serial_transfer_end_callback = NULL;
    
    /* Reset any internally-emulated device. Currently, only the printer. */
    memset(&gb->printer, 0, sizeof(gb->printer));
}

bool GB_is_inited(GB_gameboy_t *gb)
{
    return gb->magic == 'SAME';
}

bool GB_is_cgb(GB_gameboy_t *gb)
{
    return gb->is_cgb;
}

void GB_set_turbo_mode(GB_gameboy_t *gb, bool on, bool no_frame_skip)
{
    gb->turbo = on;
    gb->turbo_dont_skip = no_frame_skip;
}

void GB_set_rendering_disabled(GB_gameboy_t *gb, bool disabled)
{
    gb->disable_rendering = disabled;
}

void *GB_get_user_data(GB_gameboy_t *gb)
{
    return gb->user_data;
}

void GB_set_user_data(GB_gameboy_t *gb, void *data)
{
    gb->user_data = data;
}

void GB_reset(GB_gameboy_t *gb)
{
    uint32_t mbc_ram_size = gb->mbc_ram_size;
    bool cgb = gb->is_cgb;
    memset(gb, 0, (size_t)GB_GET_SECTION((GB_gameboy_t *) 0, unsaved));
    gb->version = GB_STRUCT_VERSION;
    
    gb->mbc_rom_bank = 1;
    gb->last_rtc_second = time(NULL);
    gb->cgb_ram_bank = 1;
    gb->io_registers[GB_IO_JOYP] = 0xF;
    gb->mbc_ram_size = mbc_ram_size;
    if (cgb) {
        gb->ram_size = 0x2000 * 8;
        memset(gb->ram, 0, gb->ram_size);
        gb->vram_size = 0x2000 * 2;
        memset(gb->vram, 0, gb->vram_size);
        
        gb->is_cgb = true;
        gb->cgb_mode = true;
        gb->io_registers[GB_IO_OBP0] = gb->io_registers[GB_IO_OBP1] = 0x00;
    }
    else {
        gb->ram_size = 0x2000;
        memset(gb->ram, 0, gb->ram_size);
        gb->vram_size = 0x2000;
        memset(gb->vram, 0, gb->vram_size);
        
        if (gb->rgb_encode_callback) {
            gb->sprite_palettes_rgb[4] = gb->sprite_palettes_rgb[0] = gb->background_palettes_rgb[0] =
                gb->rgb_encode_callback(gb, 0xFF, 0xFF, 0xFF);
            gb->sprite_palettes_rgb[5] = gb->sprite_palettes_rgb[1] = gb->background_palettes_rgb[1] =
                gb->rgb_encode_callback(gb, 0xAA, 0xAA, 0xAA);
            gb->sprite_palettes_rgb[6] = gb->sprite_palettes_rgb[2] = gb->background_palettes_rgb[2] =
                gb->rgb_encode_callback(gb, 0x55, 0x55, 0x55);
            gb->sprite_palettes_rgb[7] = gb->sprite_palettes_rgb[3] = gb->background_palettes_rgb[3] =
                gb->rgb_encode_callback(gb, 0, 0, 0);
        }
        gb->io_registers[GB_IO_OBP0] = gb->io_registers[GB_IO_OBP1] = 0xFF;
    }
    /* The serial interrupt always occur on the 0xF8th cycle of every 0x100 cycle since boot. */
    gb->serial_cycles = 0x100 - 0xF8;
    gb->io_registers[GB_IO_SC] = 0x7E;
    gb->magic = (uintptr_t)'SAME';
}

void GB_switch_model_and_reset(GB_gameboy_t *gb, bool is_cgb)
{
    if (is_cgb) {
        gb->ram = realloc(gb->ram, gb->ram_size = 0x2000 * 8);
        gb->vram = realloc(gb->vram, gb->vram_size = 0x2000 * 2);
    }
    else {
        gb->ram = realloc(gb->ram, gb->ram_size = 0x2000);
        gb->vram = realloc(gb->vram, gb->vram_size = 0x2000);
    }
    gb->is_cgb = is_cgb;
    GB_reset(gb);

}

void *GB_get_direct_access(GB_gameboy_t *gb, GB_direct_access_t access, size_t *size, uint16_t *bank)
{
    /* Set size and bank to dummy pointers if not set */
    size_t dummy_size;
    uint16_t dummy_bank;
    if (!size) {
        size = &dummy_size;
    }
    
    if (!bank) {
        bank = &dummy_bank;
    }
    
    
    switch (access) {
        case GB_DIRECT_ACCESS_ROM:
            *size = gb->rom_size;
            *bank = gb->mbc_rom_bank;
            return gb->rom;
        case GB_DIRECT_ACCESS_RAM:
            *size = gb->ram_size;
            *bank = gb->cgb_ram_bank;
            return gb->ram;
        case GB_DIRECT_ACCESS_CART_RAM:
            *size = gb->mbc_ram_size;
            *bank = gb->mbc_ram_bank;
            return gb->mbc_ram;
        case GB_DIRECT_ACCESS_VRAM:
            *size = gb->vram_size;
            *bank = gb->cgb_vram_bank;
            return gb->vram;
        case GB_DIRECT_ACCESS_HRAM:
            *size = sizeof(gb->hram);
            *bank = 0;
            return &gb->hram;
        case GB_DIRECT_ACCESS_IO:
            *size = sizeof(gb->io_registers);
            *bank = 0;
            return &gb->io_registers;
        case GB_DIRECT_ACCESS_BOOTROM:
            *size = gb->is_cgb? sizeof(gb->boot_rom) : 0x100;
            *bank = 0;
            return &gb->boot_rom;
        case GB_DIRECT_ACCESS_OAM:
            *size = sizeof(gb->oam);
            *bank = 0;
            return &gb->oam;
        case GB_DIRECT_ACCESS_BGP:
            *size = sizeof(gb->background_palettes_data);
            *bank = 0;
            return &gb->background_palettes_data;
        case GB_DIRECT_ACCESS_OBP:
            *size = sizeof(gb->sprite_palettes_data);
            *bank = 0;
            return &gb->sprite_palettes_data;
        default:
            *size = 0;
            *bank = 0;
            return NULL;
    }
}
