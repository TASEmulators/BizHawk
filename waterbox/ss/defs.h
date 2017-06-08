#pragma once

#include <cstdint>
#include <cstddef>
#include <algorithm>
#include <cassert>
#include <cstring>
#include <cstdlib>
#include <memory>

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;
typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

#define MDFN_FASTCALL
#define INLINE inline
#define MDFN_COLD
#define MDFN_HOT
#define NO_INLINE
#define NO_CLONE
#define MDFN_UNLIKELY(p) (p)
#define MDFN_LIKELY(p) (p)
//#define MDFN_ASSUME_ALIGNED(p, align) ((decltype(p))__builtin_assume_aligned((p), (align)))
#define MDFN_ASSUME_ALIGNED(p, align) (p)
#define trio_snprintf snprintf
#define trio_vprintf vprintf
#define trio_printf printf
#define trio_sprintf sprintf
#define TRUE true
#define FALSE false
#ifndef __alignas_is_defined
#define alignas(p)
#endif
#define override // remove for gcc 4.7
#define final
#define gettext_noop(s) (s)
#define MDFN_MASTERCLOCK_FIXED(n) ((int64)((double)(n) * (1LL << 32)))

typedef struct
{
	// Pitch(32-bit) must be equal to width and >= the "fb_width" specified in the MDFNGI struct for the emulated system.
	// Height must be >= to the "fb_height" specified in the MDFNGI struct for the emulated system.
	// The framebuffer pointed to by surface->pixels is written to by the system emulation code.
	uint32 *pixels;
	int pitch32;

	// Pointer to an array of int32, number of elements = fb_height, set by the driver code.  Individual elements written
	// to by system emulation code.  If the emulated system doesn't support multiple screen widths per frame, or if you handle
	// such a situation by outputting at a constant width-per-frame that is the least-common-multiple of the screen widths, then
	// you can ignore this.  If you do wish to use this, you must set all elements every frame.
	int32 *LineWidths;

	// Pointer to sound buffer, set by the driver code, that the emulation code should render sound to.
	int16 *SoundBuf;

	// Number of cycles that this frame consumed, using MDFNGI::MasterClock as a time base.
	// Set by emulation code.
	int64 MasterCycles;

	// Maximum size of the sound buffer, in frames.  Set by the driver code.
	int32 SoundBufMaxSize;

	// Number of frames currently in internal sound buffer.  Set by the system emulation code, to be read by the driver code.
	int32 SoundBufSize;

	// Set by the system emulation code every frame, to denote the horizontal and vertical offsets of the image, and the size
	// of the image.  If the emulated system sets the elements of LineWidths, then the width(w) of this structure
	// is ignored while drawing the image.
	int32 x, y, h;

	// Set(optionally) by emulation code.  If InterlaceOn is true, then assume field height is 1/2 DisplayRect.h, and
	// only every other line in surface (with the start line defined by InterlacedField) has valid data
	// (it's up to internal Mednafen code to deinterlace it).
	bool InterlaceOn;
	bool InterlaceField;
} EmulateSpecStruct;

#define MDFN_printf(...)
#define MDFN_PrintError(...)
#define MDFN_FORMATSTR(...)
#define require assert

enum InputDeviceInputType : uint8
{
	IDIT_BUTTON,		   // 1-bit
	IDIT_BUTTON_CAN_RAPID, // 1-bit

	IDIT_SWITCH, // ceil(log2(n))-bit
				 // Current switch position(default 0).
				 // Persistent, and bidirectional communication(can be modified driver side, and Mednafen core and emulation module side)

	IDIT_STATUS, // ceil(log2(n))-bit
				 // emulation module->driver communication

	IDIT_X_AXIS, // (mouse) 16-bits, signed - in-screen/window range: [0.0, nominal_width)
	IDIT_Y_AXIS, // (mouse) 16-bits, signed - in-screen/window range: [0.0, nominal_height)

	IDIT_X_AXIS_REL, // (mouse) 32-bits, signed
	IDIT_Y_AXIS_REL, // (mouse) 32-bits, signed

	IDIT_BYTE_SPECIAL,

	IDIT_RESET_BUTTON, // 1-bit

	IDIT_BUTTON_ANALOG, // 16-bits, 0 - 32767

	IDIT_RUMBLE, // 16-bits, lower 8 bits are weak rumble(0-255), next 8 bits are strong rumble(0-255), 0=no rumble, 255=max rumble.  Somewhat subjective, too...
				 // It's a rather special case of game module->driver code communication.
};

#define IDIT_BUTTON_ANALOG_FLAG_SQLR 0x00000001 // Denotes analog data that may need to be scaled to ensure a more squareish logical range(for emulated
												// analog sticks).
struct IDIIS_StatusState
{
	const char *ShortName;
	const char *Name;
	int32 Color; // (msb)0RGB(lsb), -1 for unused.
};
struct InputDeviceInputInfoStruct
{
	const char *SettingName; // No spaces, shouldbe all a-z0-9 and _. Definitely no ~!
	const char *Name;
	int ConfigOrder; // Configuration order during in-game config process, -1 for no config.
	InputDeviceInputType Type;
	const char *ExcludeName; // SettingName of a button that can't be pressed at the same time as this button
	// due to physical limitations.
	uint8 Flags;
	uint8 BitSize;
	uint16 BitOffset;

	union {
		struct
		{
			const char *const *SwitchPosName; //
			uint32 SwitchNumPos;
		};

		struct
		{
			const IDIIS_StatusState *StatusStates;
			uint32 StatusNumStates;
		};
	};
};

struct IDIISG : public std::vector<InputDeviceInputInfoStruct>
{
	IDIISG()
	{
		InputByteSize = 0;
	}

	IDIISG(std::initializer_list<InputDeviceInputInfoStruct> l) : std::vector<InputDeviceInputInfoStruct>(l)
	{
		size_t bit_offset = 0;

		for (auto &idii : *this)
		{
			size_t bit_size = 0;
			size_t bit_align = 1;

			switch (idii.Type)
			{
			default:
				abort();
				break;

			case IDIT_BUTTON:
			case IDIT_BUTTON_CAN_RAPID:
			case IDIT_RESET_BUTTON:
				bit_size = 1;
				break;

			case IDIT_SWITCH:
				bit_size = ceil(log2(idii.SwitchNumPos));
				break;

			case IDIT_STATUS:
				bit_size = ceil(log2(idii.StatusNumStates));
				break;

			case IDIT_X_AXIS:
			case IDIT_Y_AXIS:
				bit_size = 16;
				bit_align = 8;
				break;

			case IDIT_X_AXIS_REL:
			case IDIT_Y_AXIS_REL:
				bit_size = 32;
				bit_align = 8;
				break;

			case IDIT_BYTE_SPECIAL:
				bit_size = 8;
				bit_align = 8;
				break;

			case IDIT_BUTTON_ANALOG:
				bit_size = 16;
				bit_align = 8;
				break;

			case IDIT_RUMBLE:
				bit_size = 16;
				bit_align = 8;
				break;
			}

			bit_offset = (bit_offset + (bit_align - 1)) & ~(bit_align - 1);

			// printf("%s, %zu(%zu)\n", idii.SettingName, bit_offset, bit_offset / 8);

			idii.BitSize = bit_size;
			idii.BitOffset = bit_offset;

			assert(idii.BitSize == bit_size);
			assert(idii.BitOffset == bit_offset);

			bit_offset += bit_size;
		}

		InputByteSize = (bit_offset + 7) / 8;
	}
	uint32 InputByteSize;
};

struct IDIIS_Switch : public InputDeviceInputInfoStruct
{
	IDIIS_Switch(const char *sname, const char *name, int co, const char *const *spn, const uint32 spn_num)
	{
		SettingName = sname;
		Name = name;
		ConfigOrder = co;
		Type = IDIT_SWITCH;

		ExcludeName = NULL;
		Flags = 0;
		SwitchPosName = spn;
		SwitchNumPos = spn_num;
	}
};

struct IDIIS_Status : public InputDeviceInputInfoStruct
{
	IDIIS_Status(const char *sname, const char *name, const IDIIS_StatusState *ss, const uint32 ss_num)
	{
		SettingName = sname;
		Name = name;
		ConfigOrder = -1;
		Type = IDIT_STATUS;

		ExcludeName = NULL;
		Flags = 0;
		StatusStates = ss;
		StatusNumStates = ss_num;
	}
};

struct InputDeviceInfoStruct
{
	const char *ShortName;
	const char *FullName;
	const char *Description;

	const IDIISG &IDII;

	unsigned Flags;

	enum
	{
		FLAG_KEYBOARD = (1U << 0)
	};
};

struct InputPortInfoStruct
{
	const char *ShortName;
	const char *FullName;
	const std::vector<InputDeviceInfoStruct> &DeviceInfo;
	const char *DefaultDevice; // Default device for this port.
};

#include "endian.h"

inline char *strdup(const char *p)
{
	char *ret = (char *)malloc(strlen(p) + 1);
	if (ret)
		strcpy(ret, p);
	return ret;
}

#include "stream/Stream.h"
#include "stream/MemoryStream.h"
#include "math_ops.h"

#include <emulibc.h>

extern int32 (*FirmwareSizeCallback)(const char *filename);
extern void (*FirmwareDataCallback)(const char *filename, uint8 *dest);

extern int setting_ss_slstartp;
extern int setting_ss_slendp;
extern int setting_ss_slstart;
extern int setting_ss_slend;
extern int setting_ss_region_default;
extern int setting_ss_cart;
extern bool setting_ss_correct_aspect;
extern bool setting_ss_h_overscan;
extern bool setting_ss_h_blend;
extern bool setting_ss_region_autodetect;

extern bool InputLagged;
extern void (*InputCallback)();

extern void (*AddMemoryDomain)(const char* name, const void* ptr, int size, bool writable);
