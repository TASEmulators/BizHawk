#ifndef symbol_hash_h
#define symbol_hash_h

#include <stdlib.h>
#include <string.h>

typedef struct {
    char *name;
    uint16_t addr;
} GB_bank_symbol_t;

typedef struct GB_symbol_s {
    struct GB_symbol_s *next;
    const char *name;
    uint16_t bank;
    uint16_t addr;
} GB_symbol_t;

typedef struct {
    GB_bank_symbol_t *symbols;
    size_t n_symbols;
} GB_symbol_map_t;

typedef struct {
    GB_symbol_t *buckets[0x400];
} GB_reversed_symbol_map_t;

#ifdef GB_INTERNAL
void GB_reversed_map_add_symbol(GB_reversed_symbol_map_t *map, uint16_t bank, GB_bank_symbol_t *symbol);
const GB_symbol_t *GB_reversed_map_find_symbol(GB_reversed_symbol_map_t *map, const char *name);
GB_bank_symbol_t *GB_map_add_symbol(GB_symbol_map_t *map, uint16_t addr, const char *name);
const GB_bank_symbol_t *GB_map_find_symbol(GB_symbol_map_t *map, uint16_t addr);
GB_symbol_map_t *GB_map_alloc(void);
void GB_map_free(GB_symbol_map_t *map);
#endif

#endif /* symbol_hash_h */
