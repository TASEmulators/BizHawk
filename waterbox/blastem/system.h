#ifndef SYSTEM_H_
#define SYSTEM_H_
#include <stddef.h>
#include <stdint.h>
#include "system_header.h"

typedef struct system_media system_media;

typedef enum {
	SYSTEM_UNKNOWN,
	SYSTEM_GENESIS,
	SYSTEM_GENESIS_PLAYER,
	SYSTEM_SMS,
	SYSTEM_SMS_PLAYER,
	SYSTEM_JAGUAR,
} system_type;

typedef enum {
	DEBUGGER_NATIVE,
	DEBUGGER_GDB
} debugger_type;

typedef void (*system_fun)(system_header *);
typedef uint16_t (*system_fun_r16)(system_header *);
typedef void (*system_str_fun)(system_header *, char *);
typedef uint8_t (*system_str_fun_r8)(system_header *, char *);
typedef void (*system_u32_fun)(system_header *, uint32_t);
typedef void (*system_u8_fun)(system_header *, uint8_t);
typedef uint8_t (*system_u8_fun_r8)(system_header *, uint8_t);
typedef void (*system_u8_u8_fun)(system_header *, uint8_t, uint8_t);
typedef void (*system_mabs_fun)(system_header *, uint8_t, uint16_t, uint16_t);
typedef void (*system_mrel_fun)(system_header *, uint8_t, int32_t, int32_t);
typedef uint8_t *(*system_ptrszt_fun_rptr8)(system_header *, size_t *);
typedef void (*system_ptr8_sizet_fun)(system_header *, uint8_t *, size_t);

#include "arena.h"
#include "romdb.h"
#include "event_log.h"

struct system_header {
	system_header           *next_context;
	system_str_fun          start_context;
	system_fun              resume_context;
	system_fun              load_save;
	system_fun              persist_save;
	system_u8_fun_r8        load_state;
	system_fun              request_exit;
	system_fun              soft_reset;
	system_fun              free_context;
	system_fun_r16          get_open_bus_value;
	system_u32_fun          set_speed_percent;
	system_fun              inc_debug_mode;
	system_u8_u8_fun        gamepad_down;
	system_u8_u8_fun        gamepad_up;
	system_u8_u8_fun        mouse_down;
	system_u8_u8_fun        mouse_up;
	system_mabs_fun         mouse_motion_absolute;
	system_mrel_fun         mouse_motion_relative;
	system_u8_fun           keyboard_down;
	system_u8_fun           keyboard_up;
	system_fun              config_updated;
	system_ptrszt_fun_rptr8 serialize;
	system_ptr8_sizet_fun   deserialize;
	system_str_fun          start_vgm_log;
	system_fun              stop_vgm_log;
	rom_info                info;
	arena                   *arena;
	char                    *next_rom;
	char                    *save_dir;
	uint8_t                 enter_debugger;
	uint8_t                 should_exit;
	uint8_t                 save_state;
	uint8_t                 delayed_load_slot;
	uint8_t                 has_keyboard;
	uint8_t                 vgm_logging;
	uint8_t                 force_release;
	debugger_type           debugger_type;
	system_type             type;
};

struct system_media {
	void         *buffer;
	char         *dir;
	char         *name;
	char         *extension;
	system_media *chain;
	uint32_t     size;
};

#define OPT_ADDRESS_LOG (1U << 31U)

system_type detect_system_type(system_media *media);
system_header *alloc_config_system(system_type stype, system_media *media, uint32_t opts, uint8_t force_region);
system_header *alloc_config_player(system_type stype, event_reader *reader);
void system_request_exit(system_header *system, uint8_t force_release);

#endif //SYSTEM_H_
