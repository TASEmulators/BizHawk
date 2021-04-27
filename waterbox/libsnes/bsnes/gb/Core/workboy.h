#ifndef workboy_h
#define workboy_h
#include <stdint.h>
#include <stdbool.h>
#include <time.h>
#include "gb_struct_def.h"


typedef struct {
    uint8_t byte_to_send;
    bool bit_to_send;
    uint8_t byte_being_received;
    uint8_t bits_received;
    uint8_t mode;
    uint8_t key;
    bool shift_down;
    bool user_shift_down;
    uint8_t buffer[0x15];
    uint8_t buffer_index; // In nibbles during read, in bytes during write
} GB_workboy_t;

typedef void (*GB_workboy_set_time_callback)(GB_gameboy_t *gb, time_t time);
typedef time_t (*GB_workboy_get_time_callback)(GB_gameboy_t *gb);

enum {
    GB_WORKBOY_NONE = 0xFF,
    GB_WORKBOY_REQUIRE_SHIFT = 0x40,
    GB_WORKBOY_FORBID_SHIFT = 0x80,
    
    GB_WORKBOY_CLOCK = 1,
    GB_WORKBOY_TEMPERATURE = 2,
    GB_WORKBOY_MONEY = 3,
    GB_WORKBOY_CALCULATOR = 4,
    GB_WORKBOY_DATE = 5,
    GB_WORKBOY_CONVERSION = 6,
    GB_WORKBOY_RECORD = 7,
    GB_WORKBOY_WORLD = 8,
    GB_WORKBOY_PHONE = 9,
    GB_WORKBOY_ESCAPE = 10,
    GB_WORKBOY_BACKSPACE = 11,
    GB_WORKBOY_UNKNOWN = 12,
    GB_WORKBOY_LEFT = 13,
    GB_WORKBOY_Q = 17,
    GB_WORKBOY_1 = 17 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_W = 18,
    GB_WORKBOY_2 = 18 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_E = 19,
    GB_WORKBOY_3 = 19 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_R = 20,
    GB_WORKBOY_T = 21,
    GB_WORKBOY_Y = 22 ,
    GB_WORKBOY_U = 23 ,
    GB_WORKBOY_I = 24,
    GB_WORKBOY_EXCLAMATION_MARK = 24 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_O = 25,
    GB_WORKBOY_TILDE = 25 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_P = 26,
    GB_WORKBOY_ASTERISK = 26 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_DOLLAR = 27 | GB_WORKBOY_FORBID_SHIFT,
    GB_WORKBOY_HASH = 27 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_A = 28,
    GB_WORKBOY_4 = 28 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_S = 29,
    GB_WORKBOY_5 = 29 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_D = 30,
    GB_WORKBOY_6 = 30 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_F = 31,
    GB_WORKBOY_PLUS = 31 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_G = 32,
    GB_WORKBOY_MINUS = 32 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_H = 33,
    GB_WORKBOY_J = 34,
    GB_WORKBOY_K = 35,
    GB_WORKBOY_LEFT_PARENTHESIS = 35 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_L = 36,
    GB_WORKBOY_RIGHT_PARENTHESIS = 36 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_SEMICOLON = 37 | GB_WORKBOY_FORBID_SHIFT,
    GB_WORKBOY_COLON = 37,
    GB_WORKBOY_ENTER = 38,
    GB_WORKBOY_SHIFT_DOWN = 39,
    GB_WORKBOY_Z = 40,
    GB_WORKBOY_7 = 40 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_X = 41,
    GB_WORKBOY_8 = 41 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_C = 42,
    GB_WORKBOY_9 = 42 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_V = 43,
    GB_WORKBOY_DECIMAL_POINT = 43 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_B = 44,
    GB_WORKBOY_PERCENT = 44 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_N = 45,
    GB_WORKBOY_EQUAL = 45 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_M = 46,
    GB_WORKBOY_COMMA = 47 | GB_WORKBOY_FORBID_SHIFT,
    GB_WORKBOY_LT = 47 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_DOT = 48 | GB_WORKBOY_FORBID_SHIFT,
    GB_WORKBOY_GT  = 48 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_SLASH = 49 | GB_WORKBOY_FORBID_SHIFT,
    GB_WORKBOY_QUESTION_MARK = 49 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_SHIFT_UP = 50,
    GB_WORKBOY_0 = 51 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_UMLAUT = 51,
    GB_WORKBOY_SPACE = 52,
    GB_WORKBOY_QUOTE = 53 | GB_WORKBOY_FORBID_SHIFT,
    GB_WORKBOY_AT = 53 | GB_WORKBOY_REQUIRE_SHIFT,
    GB_WORKBOY_UP = 54,
    GB_WORKBOY_DOWN = 55,
    GB_WORKBOY_RIGHT = 56,
};


void GB_connect_workboy(GB_gameboy_t *gb,
                        GB_workboy_set_time_callback set_time_callback,
                        GB_workboy_get_time_callback get_time_callback);
bool GB_workboy_is_enabled(GB_gameboy_t *gb);
void GB_workboy_set_key(GB_gameboy_t *gb, uint8_t key);

#endif
