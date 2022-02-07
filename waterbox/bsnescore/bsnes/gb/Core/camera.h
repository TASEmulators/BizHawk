#ifndef camera_h
#define camera_h
#include <stdint.h>
#include "gb_struct_def.h"

typedef uint8_t (*GB_camera_get_pixel_callback_t)(GB_gameboy_t *gb, uint8_t x, uint8_t y);
typedef void (*GB_camera_update_request_callback_t)(GB_gameboy_t *gb);

enum {
    GB_CAMERA_SHOOT_AND_1D_FLAGS = 0,
    GB_CAMERA_GAIN_AND_EDGE_ENHACEMENT_FLAGS = 1,
    GB_CAMERA_EXPOSURE_HIGH = 2,
    GB_CAMERA_EXPOSURE_LOW = 3,
    GB_CAMERA_EDGE_ENHANCEMENT_INVERT_AND_VOLTAGE = 4,
    GB_CAMERA_DITHERING_PATTERN_START = 6,
    GB_CAMERA_DITHERING_PATTERN_END = 0x35,
};

uint8_t GB_camera_read_image(GB_gameboy_t *gb, uint16_t addr);

void GB_set_camera_get_pixel_callback(GB_gameboy_t *gb, GB_camera_get_pixel_callback_t callback);
void GB_set_camera_update_request_callback(GB_gameboy_t *gb, GB_camera_update_request_callback_t callback);

void GB_camera_updated(GB_gameboy_t *gb);

void GB_camera_write_register(GB_gameboy_t *gb, uint16_t addr, uint8_t value);
uint8_t GB_camera_read_register(GB_gameboy_t *gb, uint16_t addr);

#endif
