#include "gb.h"

static int noise_seed = 0;

/* This is not a complete emulation of the camera chip. Only the features used by the GameBoy Camera ROMs are supported.
    We also do not emulate the timing of the real cart, as it might be actually faster than the webcam. */

static uint8_t generate_noise(uint8_t x, uint8_t y)
{
    int value = (x + y * 128 + noise_seed);
    uint8_t *data = (uint8_t *) &value;
    unsigned hash = 0;

    while ((int *) data != &value + 1) {
        hash ^= (*data << 8);
        if (hash & 0x8000) {
            hash ^= 0x8a00;
            hash ^= *data;
        }
        data++;
        hash <<= 1;
    }
    return (hash >> 8);
}

static long get_processed_color(GB_gameboy_t *gb, uint8_t x, uint8_t y)
{
    if (x >= 128) {
        x = 0;
    }
    if (y >= 112) {
        y = 0;
    }

    long color = gb->camera_get_pixel_callback? gb->camera_get_pixel_callback(gb, x, y) : (generate_noise(x, y));

    static const double gain_values[] =
        {0.8809390, 0.9149149, 0.9457498, 0.9739758,
         1.0000000, 1.0241412, 1.0466537, 1.0677433,
         1.0875793, 1.1240310, 1.1568911, 1.1868043,
         1.2142561, 1.2396208, 1.2743837, 1.3157323,
         1.3525190, 1.3856512, 1.4157897, 1.4434309,
         1.4689574, 1.4926697, 1.5148087, 1.5355703,
         1.5551159, 1.5735801, 1.5910762, 1.6077008,
         1.6235366, 1.6386550, 1.6531183, 1.6669808};
    /* Multiply color by gain value */
    color *= gain_values[gb->camera_registers[GB_CAMERA_GAIN_AND_EDGE_ENHACEMENT_FLAGS] & 0x1F];


    /* Color is multiplied by the exposure register to simulate exposure. */
    color = color * ((gb->camera_registers[GB_CAMERA_EXPOSURE_HIGH] << 8) + gb->camera_registers[GB_CAMERA_EXPOSURE_LOW]) / 0x1000;

    return color;
}

uint8_t GB_camera_read_image(GB_gameboy_t *gb, uint16_t addr)
{
    if (gb->camera_registers[GB_CAMERA_SHOOT_AND_1D_FLAGS] & 1) {
        /* Forbid reading the image while the camera is busy. */
        return 0xFF;
    }
    uint8_t tile_x = addr / 0x10 % 0x10;
    uint8_t tile_y = addr / 0x10 / 0x10;

    uint8_t y = ((addr >> 1) & 0x7) + tile_y * 8;
    uint8_t bit = addr & 1;

    uint8_t ret = 0;

    for (uint8_t x = tile_x * 8; x < tile_x * 8 + 8; x++) {

        long color = get_processed_color(gb, x, y);

        static const double edge_enhancement_ratios[] = {0.5, 0.75, 1, 1.25, 2, 3, 4, 5};
        double edge_enhancement_ratio = edge_enhancement_ratios[(gb->camera_registers[GB_CAMERA_EDGE_ENHANCEMENT_INVERT_AND_VOLTAGE] >> 4) & 0x7];
        if ((gb->camera_registers[GB_CAMERA_GAIN_AND_EDGE_ENHACEMENT_FLAGS] & 0xE0) == 0xE0) {
                color += (color * 4) * edge_enhancement_ratio;
                color -= get_processed_color(gb, x - 1, y) * edge_enhancement_ratio;
                color -= get_processed_color(gb, x + 1, y) * edge_enhancement_ratio;
                color -= get_processed_color(gb, x, y - 1) * edge_enhancement_ratio;
                color -= get_processed_color(gb, x, y + 1) * edge_enhancement_ratio;
        }


        /* The camera's registers are used as a threshold pattern, which defines the dithering */
        uint8_t pattern_base = ((x & 3) + (y & 3) * 4) * 3 + GB_CAMERA_DITHERING_PATTERN_START;

        if (color < gb->camera_registers[pattern_base]) {
            color = 3;
        }
        else if (color < gb->camera_registers[pattern_base + 1]) {
            color = 2;
        }
        else if (color < gb->camera_registers[pattern_base + 2]) {
            color = 1;
        }
        else {
            color = 0;
        }

        ret <<= 1;
        ret |= (color >> bit) & 1;
    }

    return ret;
}

void GB_set_camera_get_pixel_callback(GB_gameboy_t *gb, GB_camera_get_pixel_callback_t callback)
{
    gb->camera_get_pixel_callback = callback;
}

void GB_set_camera_update_request_callback(GB_gameboy_t *gb, GB_camera_update_request_callback_t callback)
{
    gb->camera_update_request_callback = callback;
}

void GB_camera_updated(GB_gameboy_t *gb)
{
    gb->camera_registers[GB_CAMERA_SHOOT_AND_1D_FLAGS] &= ~1;
}

void GB_camera_write_register(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    addr &= 0x7F;
    if (addr == GB_CAMERA_SHOOT_AND_1D_FLAGS) {
        value &= 0x7;
        noise_seed = rand();
        if ((value & 1) && !(gb->camera_registers[GB_CAMERA_SHOOT_AND_1D_FLAGS] & 1) && gb->camera_update_request_callback) {
            /* If no callback is set, ignore the write as if the camera is instantly done */
            gb->camera_registers[GB_CAMERA_SHOOT_AND_1D_FLAGS] |= 1;
            gb->camera_update_request_callback(gb);
        }
    }
    else {
        if (addr >= 0x36) {
            GB_log(gb, "Wrote invalid camera register %02x: %2x\n", addr, value);
            return;
        }
        gb->camera_registers[addr] = value;
    }
}
uint8_t GB_camera_read_register(GB_gameboy_t *gb, uint16_t addr)
{
    if ((addr & 0x7F) == 0) {
        return gb->camera_registers[GB_CAMERA_SHOOT_AND_1D_FLAGS];
    }
    return 0;
}
