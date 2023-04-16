#include <cstdint>
#include <climits>

typedef std::int8_t s8;
typedef std::int16_t s16;
typedef std::int32_t s32;
typedef std::int64_t s64;

typedef std::uint8_t u8;
typedef std::uint16_t u16;
typedef std::uint32_t u32;
typedef std::uint64_t u64;

typedef u8 boolean;

#define EXPORT extern "C" __attribute__((visibility("default")))

enum class RETRO_ENVIRONMENT {
	EXPERIMENTAL = 0x10000,
	SET_ROTATION = 1,
	GET_OVERSCAN = 2,
	GET_CAN_DUPE = 3,
	SET_MESSAGE = 6,
	SHUTDOWN = 7,
	SET_PERFORMANCE_LEVEL = 8,
	GET_SYSTEM_DIRECTORY = 9,
	SET_PIXEL_FORMAT = 10,
	SET_INPUT_DESCRIPTORS = 11,
	SET_KEYBOARD_CALLBACK = 12,
	SET_DISK_CONTROL_INTERFACE = 13,
	SET_HW_RENDER = 14,
	GET_VARIABLE = 15,
	SET_VARIABLES = 16,
	GET_VARIABLE_UPDATE = 17,
	SET_SUPPORT_NO_GAME = 18,
	GET_LIBRETRO_PATH = 19,
	SET_AUDIO_CALLBACK = 22,
	SET_FRAME_TIME_CALLBACK = 21,
	GET_RUMBLE_INTERFACE = 23,
	GET_INPUT_DEVICE_CAPABILITIES = 24,
	GET_SENSOR_INTERFACE = 25 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	GET_CAMERA_INTERFACE = 26 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	GET_LOG_INTERFACE = 27,
	GET_PERF_INTERFACE = 28,
	GET_LOCATION_INTERFACE = 29,
	GET_CONTENT_DIRECTORY = 30,
	GET_CORE_ASSETS_DIRECTORY = 30,
	GET_SAVE_DIRECTORY = 31,
	SET_SYSTEM_AV_INFO = 32,
	SET_PROC_ADDRESS_CALLBACK = 33,
	SET_SUBSYSTEM_INFO = 34,
	SET_CONTROLLER_INFO = 35,
	SET_MEMORY_MAPS = 36 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	SET_GEOMETRY = 37,
	GET_USERNAME = 38,
	GET_LANGUAGE = 39,
	GET_CURRENT_SOFTWARE_FRAMEBUFFER = 40 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	GET_HW_RENDER_INTERFACE = 41 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	SET_SUPPORT_ACHIEVEMENTS = 42 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	SET_HW_RENDER_CONTEXT_NEGOTIATION_INTERFACE = 43 | RETRO_ENVIRONMENT::EXPERIMENTAL,
	SET_SERIALIZATION_QUIRKS = 44,
};

enum class RETRO_DEVICE {
	NONE = 0,
	JOYPAD = 1,
	MOUSE = 2,
	KEYBOARD = 3,
	LIGHTGUN = 4,
	ANALOG = 5,
	POINTER = 6,

	LAST,
};

enum class RETRO_DEVICE_ID_ANALOG {
	X = 0,
	Y = 1,
	BUTTON = 2,

	LAST,
};

enum class RETRO_DEVICE_ID_MOUSE {
	X = 0,
	Y = 1,
	LEFT = 2,
	RIGHT = 3,
	WHEELUP = 4,
	WHEELDOWN = 5,
	MIDDLE = 6,
	HORIZ_WHEELUP = 7,
	HORIZ_WHEELDOWN = 8,
	BUTTON_4 = 9,
	BUTTON_5 = 10,

	LAST,
};

enum class RETRO_DEVICE_ID_LIGHTGUN {
	X = 0,
	Y = 1,
	TRIGGER = 2,
	CURSOR = 3,
	TURBO = 4,
	PAUSE = 5,
	START = 6,
	SELECT = 7,
	AUX_C = 8,
	DPAD_UP = 9,
	DPAD_DOWN = 10,
	DPAD_LEFT = 11,
	DPAD_RIGHT = 12,
	SCREEN_X = 13,
	SCREEN_Y = 14,
	IS_OFFSCREEN = 15,
	RELOAD = 16,

	LAST,
};

enum class RETRO_DEVICE_ID_POINTER {
	X = 0,
	Y = 1,
	PRESSED = 2,
	COUNT = 3,

	LAST,
};

enum class RETRO_KEY {
	UNKNOWN = 0,
	FIRST = 0,
	BACKSPACE = 8,
	TAB = 9,
	CLEAR = 12,
	RETURN = 13,
	PAUSE = 19,
	ESCAPE = 27,
	SPACE = 32,
	EXCLAIM = 33,
	QUOTEDBL = 34,
	HASH = 35,
	DOLLAR = 36,
	AMPERSAND = 38,
	QUOTE = 39,
	LEFTPAREN = 40,
	RIGHTPAREN = 41,
	ASTERISK = 42,
	PLUS = 43,
	COMMA = 44,
	MINUS = 45,
	PERIOD = 46,
	SLASH = 47,
	_0 = 48,
	_1 = 49,
	_2 = 50,
	_3 = 51,
	_4 = 52,
	_5 = 53,
	_6 = 54,
	_7 = 55,
	_8 = 56,
	_9 = 57,
	COLON = 58,
	SEMICOLON = 59,
	LESS = 60,
	EQUALS = 61,
	GREATER = 62,
	QUESTION = 63,
	AT = 64,
	LEFTBRACKET = 91,
	BACKSLASH = 92,
	RIGHTBRACKET = 93,
	CARET = 94,
	UNDERSCORE = 95,
	BACKQUOTE = 96,
	a = 97,
	b = 98,
	c = 99,
	d = 100,
	e = 101,
	f = 102,
	g = 103,
	h = 104,
	i = 105,
	j = 106,
	k = 107,
	l = 108,
	m = 109,
	n = 110,
	o = 111,
	p = 112,
	q = 113,
	r = 114,
	s = 115,
	t = 116,
	u = 117,
	v = 118,
	w = 119,
	x = 120,
	y = 121,
	z = 122,
	DELETE = 127,

	KP0 = 256,
	KP1 = 257,
	KP2 = 258,
	KP3 = 259,
	KP4 = 260,
	KP5 = 261,
	KP6 = 262,
	KP7 = 263,
	KP8 = 264,
	KP9 = 265,
	KP_PERIOD = 266,
	KP_DIVIDE = 267,
	KP_MULTIPLY = 268,
	KP_MINUS = 269,
	KP_PLUS = 270,
	KP_ENTER = 271,
	KP_EQUALS = 272,

	UP = 273,
	DOWN = 274,
	RIGHT = 275,
	LEFT = 276,
	INSERT = 277,
	HOME = 278,
	END = 279,
	PAGEUP = 280,
	PAGEDOWN = 281,

	F1 = 282,
	F2 = 283,
	F3 = 284,
	F4 = 285,
	F5 = 286,
	F6 = 287,
	F7 = 288,
	F8 = 289,
	F9 = 290,
	F10 = 291,
	F11 = 292,
	F12 = 293,
	F13 = 294,
	F14 = 295,
	F15 = 296,

	NUMLOCK = 300,
	CAPSLOCK = 301,
	SCROLLOCK = 302,
	RSHIFT = 303,
	LSHIFT = 304,
	RCTRL = 305,
	LCTRL = 306,
	RALT = 307,
	LALT = 308,
	RMETA = 309,
	LMETA = 310,
	LSUPER = 311,
	RSUPER = 312,
	MODE = 313,
	COMPOSE = 314,

	HELP = 315,
	PRINT = 316,
	SYSREQ = 317,
	BREAK = 318,
	MENU = 319,
	POWER = 320,
	EURO = 321,
	UNDO = 322,

	LAST,
};

enum class RETRO_MOD {
	NONE = 0,
	SHIFT = 1,
	CTRL = 2,
	ALT = 4,
	META = 8,
	NUMLOCK = 16,
	CAPSLOCK = 32,
	SCROLLLOCK = 64,
};

enum class RETRO_DEVICE_ID_JOYPAD {
	B = 0,
	Y = 1,
	SELECT = 2,
	START = 3,
	UP = 4,
	DOWN = 5,
	LEFT = 6,
	RIGHT = 7,
	A = 8,
	X = 9,
	L = 10,
	R = 11,
	L2 = 12,
	R2 = 13,
	L3 = 14,
	R3 = 15,

	LAST,
};

enum class RETRO_SENSOR {
	ACCELEROMETER_X = 0,
	ACCELEROMETER_Y = 1,
	ACCELEROMETER_Z = 2,
	GYROSCOPE_X = 3,
	GYROSCOPE_Y = 4,
	GYROSCOPE_Z = 5,
	ILLUMINANCE = 6,

	LAST,
};

enum class RETRO_PIXEL_FORMAT {
	ZRGB1555 = 0,
	XRGB8888 = 1,
	RGB565 = 2,
	UNKNOWN = INT_MAX,
};

enum class RETRO_LANGUAGE {
	ENGLISH = 0,
	JAPANESE = 1,
	FRENCH = 2,
	SPANISH = 3,
	GERMAN = 4,
	ITALIAN = 5,
	DUTCH = 6,
	PORTUGUESE = 7,
	RUSSIAN = 8,
	KOREAN = 9,
	CHINESE_TRADITIONAL = 10,
	CHINESE_SIMPLIFIED = 11,
	ESPERANTO = 12,
	POLISH = 13,
	VIETNAMESE = 14,
	LAST,

	DUMMY = INT_MAX,
};

enum class RETRO_LOG {
	DEBUG = 0,
	INFO,
	WARN,
	ERROR,
	DUMMY = INT_MAX,
};

struct retro_variable {
	const char* key;
	const char* value;
};

struct retro_message {
	const char* msg;
	u32 frames;
};

typedef void (*retro_log_printf_t)(RETRO_LOG level, const char* fmt, ...);

struct retro_log_callback {
	retro_log_printf_t log;
};
