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
    
    
    buffer_write(gb->mbc_ram, gb->mbc_ram_size, &buffer);
    buffer_write(gb->ram, gb->ram_size, &buffer);
    buffer_write(gb->vram, gb->vram_size, &buffer);
}

/* Best-effort read function for maximum future compatibility. */
static bool read_section(FILE *f, void *dest, uint32_t size)
{
    uint32_t saved_size = 0;
    if (fread(&saved_size, 1, sizeof(size), f) != sizeof(size)) {
        return false;
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

static bool verify_state_compatibility(GB_gameboy_t *gb, GB_gameboy_t *save)
{
    if (gb->magic != save->magic) {
        GB_log(gb, "File is not a save state, or is from an incompatible operating system.\n");
        return false;
    }
    
    if (gb->version != save->version) {
        GB_log(gb, "Save state is for a different version of SameBoy.\n");
        return false;
    }
    
    if (gb->mbc_ram_size < save->mbc_ram_size) {
        GB_log(gb, "Save state has non-matching MBC RAM size.\n");
        return false;
    }
    
    if (gb->ram_size != save->ram_size) {
        GB_log(gb, "Save state has non-matching RAM size. Try changing emulated model.\n");
        return false;
    }
    
    if (gb->vram_size != save->vram_size) {
        GB_log(gb, "Save state has non-matching VRAM size. Try changing emulated model.\n");
        return false;
    }
    
    return true;
}

#define READ_SECTION(gb, f, section) read_section(f, GB_GET_SECTION(gb, section), GB_SECTION_SIZE(section))

int GB_load_state(GB_gameboy_t *gb, const char *path)
{
    GB_gameboy_t save;
    
    /* Every unread value should be kept the same. */
    memcpy(&save, gb, sizeof(save));
    
    FILE *f = fopen(path, "rb");
    if (!f) {
        GB_log(gb, "Could not open save state: %s.\n", strerror(errno));
        return errno;
    }
    
    if (fread(GB_GET_SECTION(&save, header), 1, GB_SECTION_SIZE(header), f) != GB_SECTION_SIZE(header)) goto error;
    if (!READ_SECTION(&save, f, core_state)) goto error;
    if (!READ_SECTION(&save, f, dma       )) goto error;
    if (!READ_SECTION(&save, f, mbc       )) goto error;
    if (!READ_SECTION(&save, f, hram      )) goto error;
    if (!READ_SECTION(&save, f, timing    )) goto error;
    if (!READ_SECTION(&save, f, apu       )) goto error;
    if (!READ_SECTION(&save, f, rtc       )) goto error;
    if (!READ_SECTION(&save, f, video     )) goto error;
    
    if (!verify_state_compatibility(gb, &save)) {
        errno = -1;
        goto error;
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
    
    if (fread(gb->vram, 1, gb->vram_size, f) != gb->vram_size) {
        fclose(f);
        return EIO;
    }
    
    memcpy(gb, &save, sizeof(save));
    errno = 0;
    
    if (gb->cartridge_type->has_rumble && gb->rumble_callback) {
        gb->rumble_callback(gb, gb->rumble_state);
    }
    
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

static bool buffer_read_section(const uint8_t **buffer, size_t *buffer_length, void *dest, uint32_t size)
{
    uint32_t saved_size = 0;
    if (buffer_read(&saved_size, sizeof(size), buffer, buffer_length) != sizeof(size)) {
        return false;
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

#define READ_SECTION(gb, buffer, length, section) buffer_read_section(&buffer, &length, GB_GET_SECTION(gb, section), GB_SECTION_SIZE(section))
int GB_load_state_from_buffer(GB_gameboy_t *gb, const uint8_t *buffer, size_t length)
{
    GB_gameboy_t save;
    
    /* Every unread value should be kept the same. */
    memcpy(&save, gb, sizeof(save));
    
    if (buffer_read(GB_GET_SECTION(&save, header), GB_SECTION_SIZE(header), &buffer, &length) != GB_SECTION_SIZE(header)) return -1;
    if (!READ_SECTION(&save, buffer, length, core_state)) return -1;
    if (!READ_SECTION(&save, buffer, length, dma       )) return -1;
    if (!READ_SECTION(&save, buffer, length, mbc       )) return -1;
    if (!READ_SECTION(&save, buffer, length, hram      )) return -1;
    if (!READ_SECTION(&save, buffer, length, timing    )) return -1;
    if (!READ_SECTION(&save, buffer, length, apu       )) return -1;
    if (!READ_SECTION(&save, buffer, length, rtc       )) return -1;
    if (!READ_SECTION(&save, buffer, length, video     )) return -1;
    
    if (!verify_state_compatibility(gb, &save)) {
        return -1;
    }
    
    memset(gb->mbc_ram + save.mbc_ram_size, 0xFF, gb->mbc_ram_size - save.mbc_ram_size);
    if (buffer_read(gb->mbc_ram, save.mbc_ram_size, &buffer, &length) != save.mbc_ram_size) {
        return -1;
    }
    
    if (buffer_read(gb->ram, gb->ram_size, &buffer, &length) != gb->ram_size) {
        return -1;
    }
    
    if (buffer_read(gb->vram,gb->vram_size, &buffer, &length) != gb->vram_size) {
        return -1;
    }
    
    memcpy(gb, &save, sizeof(save));
    
    if (gb->cartridge_type->has_rumble && gb->rumble_callback) {
        gb->rumble_callback(gb, gb->rumble_state);
    }
    
    return 0;
}

#undef READ_SECTION
