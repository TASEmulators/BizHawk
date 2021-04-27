#include "gb.h"
#include <stdio.h>
#include <errno.h>

static bool dump_section(FILE *f, const void *src, uint32_t size)
{
    if (fwrite(&size, 1, sizeof(size), f) != sizeof(size)) {
        return false;
    }
    
    if (fwrite(src, 1, size, f) != size) {
        return false;
    }
    
    return true;
}

#define DUMP_SECTION(gb, f, section) dump_section(f, GB_GET_SECTION(gb, section), GB_SECTION_SIZE(section))

/* Todo: we need a sane and protable save state format. */
int GB_save_state(GB_gameboy_t *gb, const char *path)
{
    FILE *f = fopen(path, "wb");
    if (!f) {
        GB_log(gb, "Could not open save state: %s.\n", strerror(errno));
        return errno;
    }
    
    if (fwrite(GB_GET_SECTION(gb, header), 1, GB_SECTION_SIZE(header), f) != GB_SECTION_SIZE(header)) goto error;
    if (!DUMP_SECTION(gb, f, core_state)) goto error;
    if (!DUMP_SECTION(gb, f, dma       )) goto error;
    if (!DUMP_SECTION(gb, f, mbc       )) goto error;
    if (!DUMP_SECTION(gb, f, hram      )) goto error;
    if (!DUMP_SECTION(gb, f, timing    )) goto error;
    if (!DUMP_SECTION(gb, f, apu       )) goto error;
    if (!DUMP_SECTION(gb, f, rtc       )) goto error;
    if (!DUMP_SECTION(gb, f, video     )) goto error;
    
    if (GB_is_hle_sgb(gb)) {
        if (!dump_section(f, gb->sgb, sizeof(*gb->sgb))) goto error;
    }
    
    if (fwrite(gb->mbc_ram, 1, gb->mbc_ram_size, f) != gb->mbc_ram_size) {
        goto error;
    }
    
    if (fwrite(gb->ram, 1, gb->ram_size, f) != gb->ram_size) {
        goto error;
    }
    
    if (fwrite(gb->vram, 1, gb->vram_size, f) != gb->vram_size) {
        goto error;
    }
    
    errno = 0;
    
error:
    fclose(f);
    return errno;
}

#undef DUMP_SECTION

size_t GB_get_save_state_size(GB_gameboy_t *gb)
{
    return GB_SECTION_SIZE(header)
    + GB_SECTION_SIZE(core_state) + sizeof(uint32_t)
    + GB_SECTION_SIZE(dma       ) + sizeof(uint32_t)
    + GB_SECTION_SIZE(mbc       ) + sizeof(uint32_t)
    + GB_SECTION_SIZE(hram      ) + sizeof(uint32_t)
    + GB_SECTION_SIZE(timing    ) + sizeof(uint32_t)
    + GB_SECTION_SIZE(apu       ) + sizeof(uint32_t)
    + GB_SECTION_SIZE(rtc       ) + sizeof(uint32_t)
    + GB_SECTION_SIZE(video     ) + sizeof(uint32_t)
    + (GB_is_hle_sgb(gb)? sizeof(*gb->sgb) + sizeof(uint32_t) : 0)
    + gb->mbc_ram_size
    + gb->ram_size
    + gb->vram_size;
}

/* A write-line function for memory copying */
static void buffer_write(const void *src, size_t size, uint8_t **dest)
{
    memcpy(*dest, src, size);
    *dest += size;
}

static void buffer_dump_section(uint8_t **buffer, const void *src, uint32_t size)
{
    buffer_write(&size, sizeof(size), buffer);
    buffer_write(src, size, buffer);
}

#define DUMP_SECTION(gb, buffer, section) buffer_dump_section(&buffer, GB_GET_SECTION(gb, section), GB_SECTION_SIZE(section))
void GB_save_state_to_buffer(GB_gameboy_t *gb, uint8_t *buffer)
{
    buffer_write(GB_GET_SECTION(gb, header), GB_SECTION_SIZE(header), &buffer);
    DUMP_SECTION(gb, buffer, core_state);
    DUMP_SECTION(gb, buffer, dma       );
    DUMP_SECTION(gb, buffer, mbc       );
    DUMP_SECTION(gb, buffer, hram      );
    DUMP_SECTION(gb, buffer, timing    );
    DUMP_SECTION(gb, buffer, apu       );
    DUMP_SECTION(gb, buffer, rtc       );
    DUMP_SECTION(gb, buffer, video     );
    
    if (GB_is_hle_sgb(gb)) {
        buffer_dump_section(&buffer, gb->sgb, sizeof(*gb->sgb));
    }
    
    
    buffer_write(gb->mbc_ram, gb->mbc_ram_size, &buffer);
    buffer_write(gb->ram, gb->ram_size, &buffer);
    buffer_write(gb->vram, gb->vram_size, &buffer);
}

/* Best-effort read function for maximum future compatibility. */
static bool read_section(FILE *f, void *dest, uint32_t size, bool fix_broken_windows_saves)
{
    uint32_t saved_size = 0;
    if (fread(&saved_size, 1, sizeof(size), f) != sizeof(size)) {
        return false;
    }
    
    if (fix_broken_windows_saves) {
        if (saved_size < 4) {
            return false;
        }
        saved_size -= 4;
        fseek(f, 4, SEEK_CUR);
    }
    
    if (saved_size <= size) {
        if (fread(dest, 1, saved_size, f) != saved_size) {
            return false;
        }
    }
    else {
        if (fread(dest, 1, size, f) != size) {
            return false;
        }
        fseek(f, saved_size - size, SEEK_CUR);
    }
    
    return true;
}
#undef DUMP_SECTION

static bool verify_and_update_state_compatibility(GB_gameboy_t *gb, GB_gameboy_t *save)
{
    if (save->ram_size == 0 && (&save->ram_size)[-1] == gb->ram_size) {
        /* This is a save state with a bad printer struct from a 32-bit OS */
        memcpy(save->extra_oam + 4, save->extra_oam, (uintptr_t)&save->ram_size - (uintptr_t)&save->extra_oam);
    }
    if (save->ram_size == 0) {
        /* Save doesn't have ram size specified, it's a pre 0.12 save state with potentially
         incorrect RAM amount if it's a CGB instance */
        if (GB_is_cgb(save)) {
            save->ram_size = 0x2000 * 8; // Incorrect RAM size
        }
        else {
            save->ram_size = gb->ram_size;
        }
    }
    
    if (gb->version != save->version) {
        GB_log(gb, "The save state is for a different version of SameBoy.\n");
        return false;
    }
    
    if (gb->mbc_ram_size < save->mbc_ram_size) {
        GB_log(gb, "The save state has non-matching MBC RAM size.\n");
        return false;
    }
    
    if (gb->vram_size != save->vram_size) {
        GB_log(gb, "The save state has non-matching VRAM size. Try changing the emulated model.\n");
        return false;
    }
    
    if (GB_is_hle_sgb(gb) != GB_is_hle_sgb(save)) {
        GB_log(gb, "The save state is %sfor a Super Game Boy. Try changing the emulated model.\n", GB_is_hle_sgb(save)? "" : "not ");
        return false;
    }
    
    if (gb->ram_size != save->ram_size) {
        if (gb->ram_size == 0x1000 * 8 && save->ram_size == 0x2000 * 8) {
            /* A bug in versions prior to 0.12 made CGB instances allocate twice the ammount of RAM.
               Ignore this issue to retain compatibility with older, 0.11, save states. */
        }
        else {
            GB_log(gb, "The save state has non-matching RAM size. Try changing the emulated model.\n");
            return false;
        }
    }
    
    return true;
}

static void sanitize_state(GB_gameboy_t *gb)
{
    for (unsigned i = 0; i < 32; i++) {
        GB_palette_changed(gb, false, i * 2);
        GB_palette_changed(gb, true, i * 2);
    }
    
    gb->bg_fifo.read_end &= 0xF;
    gb->bg_fifo.write_end &= 0xF;
    gb->oam_fifo.read_end &= 0xF;
    gb->oam_fifo.write_end &= 0xF;
    gb->object_low_line_address &= gb->vram_size & ~1;
    gb->fetcher_x &= 0x1f;
    if (gb->lcd_x > gb->position_in_line) {
        gb->lcd_x = gb->position_in_line;
    }
    
    if (gb->object_priority == GB_OBJECT_PRIORITY_UNDEFINED) {
        gb->object_priority = gb->cgb_mode? GB_OBJECT_PRIORITY_INDEX : GB_OBJECT_PRIORITY_X;
    }
}

#define READ_SECTION(gb, f, section) read_section(f, GB_GET_SECTION(gb, section), GB_SECTION_SIZE(section), fix_broken_windows_saves)

int GB_load_state(GB_gameboy_t *gb, const char *path)
{
    GB_gameboy_t save;
    
    /* Every unread value should be kept the same. */
    memcpy(&save, gb, sizeof(save));
    /* ...Except ram size, we use it to detect old saves with incorrect ram sizes */
    save.ram_size = 0;
    
    FILE *f = fopen(path, "rb");
    if (!f) {
        GB_log(gb, "Could not open save state: %s.\n", strerror(errno));
        return errno;
    }
    
    bool fix_broken_windows_saves = false;
    if (fread(GB_GET_SECTION(&save, header), 1, GB_SECTION_SIZE(header), f) != GB_SECTION_SIZE(header)) goto error;
    if (save.magic == 0) {
        /* Potentially legacy, broken Windows save state */
        fseek(f, 4, SEEK_SET);
        if (fread(GB_GET_SECTION(&save, header), 1, GB_SECTION_SIZE(header), f) != GB_SECTION_SIZE(header)) goto error;
        fix_broken_windows_saves = true;
    }
    if (gb->magic != save.magic) {
        GB_log(gb, "The file is not a save state, or is from an incompatible operating system.\n");
        return false;
    }
    if (!READ_SECTION(&save, f, core_state)) goto error;
    if (!READ_SECTION(&save, f, dma       )) goto error;
    if (!READ_SECTION(&save, f, mbc       )) goto error;
    if (!READ_SECTION(&save, f, hram      )) goto error;
    if (!READ_SECTION(&save, f, timing    )) goto error;
    if (!READ_SECTION(&save, f, apu       )) goto error;
    if (!READ_SECTION(&save, f, rtc       )) goto error;
    if (!READ_SECTION(&save, f, video     )) goto error;
    
    if (!verify_and_update_state_compatibility(gb, &save)) {
        errno = -1;
        goto error;
    }
    
    if (GB_is_hle_sgb(gb)) {
        if (!read_section(f, gb->sgb, sizeof(*gb->sgb), false)) goto error;
    }
    
    memset(gb->mbc_ram + save.mbc_ram_size, 0xFF, gb->mbc_ram_size - save.mbc_ram_size);
    if (fread(gb->mbc_ram, 1, save.mbc_ram_size, f) != save.mbc_ram_size) {
        fclose(f);
        return EIO;
    }
    
    if (fread(gb->ram, 1, gb->ram_size, f) != gb->ram_size) {
        fclose(f);
        return EIO;
    }

    /* Fix for 0.11 save states that allocate twice the amount of RAM in CGB instances */
    fseek(f, save.ram_size - gb->ram_size, SEEK_CUR);
    
    if (fread(gb->vram, 1, gb->vram_size, f) != gb->vram_size) {
        fclose(f);
        return EIO;
    }
    
    size_t orig_ram_size = gb->ram_size;
    memcpy(gb, &save, sizeof(save));
    gb->ram_size = orig_ram_size;

    errno = 0;
    
    sanitize_state(gb);
    
error:
    fclose(f);
    return errno;
}

#undef READ_SECTION

/* An read-like function for buffer-copying */
static size_t buffer_read(void *dest, size_t length, const uint8_t **buffer, size_t *buffer_length)
{
    if (length > *buffer_length) {
        length = *buffer_length;
    }
    
    memcpy(dest, *buffer, length);
    *buffer += length;
    *buffer_length -= length;
    
    return length;
}

static bool buffer_read_section(const uint8_t **buffer, size_t *buffer_length, void *dest, uint32_t size, bool fix_broken_windows_saves)
{
    uint32_t saved_size = 0;
    if (buffer_read(&saved_size, sizeof(size), buffer, buffer_length) != sizeof(size)) {
        return false;
    }
    
    if (saved_size > *buffer_length) return false;
    
    if (fix_broken_windows_saves) {
        if (saved_size < 4) {
            return false;
        }
        saved_size -= 4;
        *buffer += 4;
    }
    
    if (saved_size <= size) {
        if (buffer_read(dest, saved_size, buffer, buffer_length) != saved_size) {
            return false;
        }
    }
    else {
        if (buffer_read(dest, size, buffer, buffer_length) != size) {
            return false;
        }
        *buffer += saved_size - size;
        *buffer_length -= saved_size - size;
    }
    
    return true;
}

#define READ_SECTION(gb, buffer, length, section) buffer_read_section(&buffer, &length, GB_GET_SECTION(gb, section), GB_SECTION_SIZE(section), fix_broken_windows_saves)
int GB_load_state_from_buffer(GB_gameboy_t *gb, const uint8_t *buffer, size_t length)
{
    GB_gameboy_t save;
    
    /* Every unread value should be kept the same. */
    memcpy(&save, gb, sizeof(save));
    bool fix_broken_windows_saves = false;

    if (buffer_read(GB_GET_SECTION(&save, header), GB_SECTION_SIZE(header), &buffer, &length) != GB_SECTION_SIZE(header)) return -1;
    if (save.magic == 0) {
        /* Potentially legacy, broken Windows save state*/
        buffer -= GB_SECTION_SIZE(header) - 4;
        length += GB_SECTION_SIZE(header) - 4;
        if (buffer_read(GB_GET_SECTION(&save, header), GB_SECTION_SIZE(header), &buffer, &length) != GB_SECTION_SIZE(header)) return -1;
        fix_broken_windows_saves = true;
    }
    if (gb->magic != save.magic) {
        GB_log(gb, "The file is not a save state, or is from an incompatible operating system.\n");
        return false;
    }
    if (!READ_SECTION(&save, buffer, length, core_state)) return -1;
    if (!READ_SECTION(&save, buffer, length, dma       )) return -1;
    if (!READ_SECTION(&save, buffer, length, mbc       )) return -1;
    if (!READ_SECTION(&save, buffer, length, hram      )) return -1;
    if (!READ_SECTION(&save, buffer, length, timing    )) return -1;
    if (!READ_SECTION(&save, buffer, length, apu       )) return -1;
    if (!READ_SECTION(&save, buffer, length, rtc       )) return -1;
    if (!READ_SECTION(&save, buffer, length, video     )) return -1;

    
    if (!verify_and_update_state_compatibility(gb, &save)) {
        return -1;
    }
    
    if (GB_is_hle_sgb(gb)) {
        if (!buffer_read_section(&buffer, &length, gb->sgb, sizeof(*gb->sgb), false)) return -1;
    }
    
    memset(gb->mbc_ram + save.mbc_ram_size, 0xFF, gb->mbc_ram_size - save.mbc_ram_size);
    if (buffer_read(gb->mbc_ram, save.mbc_ram_size, &buffer, &length) != save.mbc_ram_size) {
        return -1;
    }
    
    if (buffer_read(gb->ram, gb->ram_size, &buffer, &length) != gb->ram_size) {
        return -1;
    }
    
    if (buffer_read(gb->vram, gb->vram_size, &buffer, &length) != gb->vram_size) {
        return -1;
    }
    
    /* Fix for 0.11 save states that allocate twice the amount of RAM in CGB instances */
    buffer += save.ram_size - gb->ram_size;
    length -= save.ram_size - gb->ram_size;
    
    memcpy(gb, &save, sizeof(save));
    
    sanitize_state(gb);
    
    return 0;
}

#undef READ_SECTION
