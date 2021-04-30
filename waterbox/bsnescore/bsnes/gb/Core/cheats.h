#ifndef cheats_h
#define cheats_h
#include "gb_struct_def.h"

#define GB_CHEAT_ANY_BANK 0xFFFF

typedef struct GB_cheat_s GB_cheat_t;

void GB_add_cheat(GB_gameboy_t *gb, const char *description, uint16_t address, uint16_t bank, uint8_t value, uint8_t old_value, bool use_old_value, bool enabled);
void GB_update_cheat(GB_gameboy_t *gb, const GB_cheat_t *cheat, const char *description, uint16_t address, uint16_t bank, uint8_t value, uint8_t old_value, bool use_old_value, bool enabled);
bool GB_import_cheat(GB_gameboy_t *gb, const char *cheat, const char *description, bool enabled);
const GB_cheat_t *const *GB_get_cheats(GB_gameboy_t *gb, size_t *size);
void GB_remove_cheat(GB_gameboy_t *gb, const GB_cheat_t *cheat);
bool GB_cheats_enabled(GB_gameboy_t *gb);
void GB_set_cheats_enabled(GB_gameboy_t *gb, bool enabled);
void GB_load_cheats(GB_gameboy_t *gb, const char *path);
int GB_save_cheats(GB_gameboy_t *gb, const char *path);

#ifdef GB_INTERNAL
#ifdef GB_DISABLE_CHEATS
#define GB_apply_cheat(...)
#else
void GB_apply_cheat(GB_gameboy_t *gb, uint16_t address, uint8_t *value);
#endif
#endif

typedef struct {
    size_t size;
    GB_cheat_t *cheats[];
} GB_cheat_hash_t;

struct GB_cheat_s {
    uint16_t address;
    uint16_t bank;
    uint8_t value;
    uint8_t old_value;
    bool use_old_value;
    bool enabled;
    char description[128];
};

#endif
