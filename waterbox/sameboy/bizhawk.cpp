#include <stdint.h>
#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

#define _Static_assert static_assert

extern "C" {
#include "gb.h"
#include "joypad.h"
#include "apu.h"
}

static GB_gameboy_t GB;

static void VBlankCallback(GB_gameboy_t *gb)
{

}

static void LogCallback(GB_gameboy_t *gb, const char *string, GB_log_attributes attributes)
{
	fputs(string, stdout);
}

static uint32_t RgbEncodeCallback(GB_gameboy_t *gb, uint8_t r, uint8_t g, uint8_t b)
{
    return b | g << 8 | r << 16 | 0xff000000;
}

static void InfraredCallback(GB_gameboy_t *gb, bool on, long cycles_since_last_update)
{

}

static void RumbleCallback(GB_gameboy_t *gb, bool rumble_on)
{

}

static void SerialStartCallback(GB_gameboy_t *gb, uint8_t byte_to_send)
{

}

static uint8_t SerialEndCallback(GB_gameboy_t *gb)
{
	return 0;
}

ECL_EXPORT bool Init(bool cgb)
{
    if (cgb)
        GB_init_cgb(&GB);
    else
        GB_init(&GB);
    if (GB_load_boot_rom(&GB, "boot.rom") != 0)
        return false;
    
    if (GB_load_rom(&GB, "game.rom") != 0)
        return false;

    GB_set_vblank_callback(&GB, VBlankCallback);
    GB_set_log_callback(&GB, LogCallback);
    GB_set_rgb_encode_callback(&GB, RgbEncodeCallback);
    GB_set_infrared_callback(&GB, InfraredCallback);
    GB_set_rumble_callback(&GB, RumbleCallback);

    return true;
}

struct MyFrameInfo : public FrameInfo
{

};

ECL_EXPORT void FrameAdvance(MyFrameInfo &f)
{
    GB_set_pixels_output(&GB, f.VideoBuffer);
	// void GB_set_key_state(GB_gameboy_t *gb, GB_key_t index, bool pressed);
    GB_run_frame(&GB);
	f.Samples = 735;
	f.Width = 160;
	f.Height = 144;
}

static void SetMemoryArea(MemoryArea *m, GB_direct_access_t access, const char* name, int32_t flags)
{
    size_t size;
    m->Name = name;
    m->Data = GB_get_direct_access(&GB, access, &size, nullptr);
    m->Size = size;
    m->Flags = flags;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	// TODO: "System Bus"
	SetMemoryArea(m + 0, GB_DIRECT_ACCESS_RAM, "WRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY);
	SetMemoryArea(m + 1, GB_DIRECT_ACCESS_ROM, "ROM", MEMORYAREA_FLAGS_WORDSIZE1);
	SetMemoryArea(m + 2, GB_DIRECT_ACCESS_VRAM, "VRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 3, GB_DIRECT_ACCESS_CART_RAM, "CartRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 4, GB_DIRECT_ACCESS_OAM, "OAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 5, GB_DIRECT_ACCESS_HRAM, "HRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 6, GB_DIRECT_ACCESS_IO, "IO", MEMORYAREA_FLAGS_WORDSIZE1);
	SetMemoryArea(m + 7, GB_DIRECT_ACCESS_BOOTROM, "BOOTROM", MEMORYAREA_FLAGS_WORDSIZE1);
	SetMemoryArea(m + 8, GB_DIRECT_ACCESS_BGP, "BGP", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 8, GB_DIRECT_ACCESS_OBP, "OBP", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
}

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	// TODO
}

int main()
{
	return 0;
}
