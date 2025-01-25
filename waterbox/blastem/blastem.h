#ifndef BLASTEM_H_
#define BLASTEM_H_

#include "tern.h"
#include "system.h"

extern int headless;
extern int exit_after;
extern int z80_enabled;
extern int frame_limit;

extern tern_node * config;
extern system_header *current_system;

extern char *save_state_path;
extern char *save_filename;
extern uint8_t use_native_states;
void reload_media(void);
void lockon_media(char *lock_on_path);
void init_system_with_media(const char *path, system_type force_stype);
void apply_updated_config(void);
const system_media *current_media(void);

#endif //BLASTEM_H_
