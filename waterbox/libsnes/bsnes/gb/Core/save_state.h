/* Macros to make the GB_gameboy_t struct more future compatible when state saving */
#ifndef save_state_h
#define save_state_h
#include <stddef.h>

#define GB_PADDING(type, old_usage) type old_usage##__do_not_use

#ifdef __cplusplus
/* For bsnes integration. C++ code does not need section information, and throws a fit over certain types such
   as anonymous enums inside unions */
#define GB_SECTION(name, ...) __attribute__ ((aligned (8))) __VA_ARGS__
#else
#define GB_SECTION(name, ...) __attribute__ ((aligned (8))) union {uint8_t name##_section_start; struct {__VA_ARGS__};}; uint8_t name##_section_end[0]
#define GB_SECTION_OFFSET(name) (offsetof(GB_gameboy_t, name##_section_start))
#define GB_SECTION_SIZE(name) (offsetof(GB_gameboy_t, name##_section_end) - offsetof(GB_gameboy_t, name##_section_start))
#define GB_GET_SECTION(gb, name) ((void*)&((gb)->name##_section_start))
#endif

#define GB_aligned_double __attribute__ ((aligned (8))) double


/* Public calls related to save states */
int GB_save_state(GB_gameboy_t *gb, const char *path);
size_t GB_get_save_state_size(GB_gameboy_t *gb);
/* Assumes buffer is big enough to contain the save state. Use with GB_get_save_state_size(). */
void GB_save_state_to_buffer(GB_gameboy_t *gb, uint8_t *buffer);

int GB_load_state(GB_gameboy_t *gb, const char *path);
int GB_load_state_from_buffer(GB_gameboy_t *gb, const uint8_t *buffer, size_t length);
#endif /* save_state_h */
