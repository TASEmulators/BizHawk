#ifndef font_h
#define font_h

#include <stdint.h>
extern uint8_t font[];
extern const uint8_t font_max;
#define GLYPH_HEIGHT 8
#define GLYPH_WIDTH 6
#define LEFT_ARROW_STRING "\x86"
#define RIGHT_ARROW_STRING "\x7f"
#define SELECTION_STRING RIGHT_ARROW_STRING
#define CTRL_STRING "\x80\x81\x82"
#define SHIFT_STRING "\x83"
#define CMD_STRING "\x84\x85"
#endif /* font_h */

