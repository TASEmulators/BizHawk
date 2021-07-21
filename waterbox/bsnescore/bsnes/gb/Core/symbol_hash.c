#include "gb.h"
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>

static size_t GB_map_find_symbol_index(GB_symbol_map_t *map, uint16_t addr)
{
    if (!map->symbols) {
        return 0;
    }
    ssize_t min = 0;
    ssize_t max = map->n_symbols;
    while (min < max) {
        size_t pivot = (min + max) / 2;
        if (map->symbols[pivot].addr == addr) return pivot;
        if (map->symbols[pivot].addr > addr) {
            max = pivot;
        }
        else {
            min = pivot + 1;
        }
    }
    return (size_t) min;
}

GB_bank_symbol_t *GB_map_add_symbol(GB_symbol_map_t *map, uint16_t addr, const char *name)
{
    size_t index = GB_map_find_symbol_index(map, addr);

    map->symbols = realloc(map->symbols, (map->n_symbols + 1) * sizeof(map->symbols[0]));
    memmove(&map->symbols[index + 1], &map->symbols[index], (map->n_symbols - index) * sizeof(map->symbols[0]));
    map->symbols[index].addr = addr;
    map->symbols[index].name = strdup(name);
    map->n_symbols++;
    return &map->symbols[index];
}

const GB_bank_symbol_t *GB_map_find_symbol(GB_symbol_map_t *map, uint16_t addr)
{
    if (!map) return NULL;
    size_t index = GB_map_find_symbol_index(map, addr);
    if (index < map->n_symbols && map->symbols[index].addr != addr) {
        index--;
    }
    if (index < map->n_symbols) {
        return &map->symbols[index];
    }
    return NULL;
}

GB_symbol_map_t *GB_map_alloc(void)
{
    GB_symbol_map_t *map = malloc(sizeof(*map));
    memset(map, 0, sizeof(*map));
    return map;
}

void GB_map_free(GB_symbol_map_t *map)
{
    for (unsigned i = 0; i < map->n_symbols; i++) {
        free(map->symbols[i].name);
    }

    if (map->symbols) {
        free(map->symbols);
    }

    free(map);
}

static unsigned hash_name(const char *name)
{
    unsigned r = 0;
    while (*name) {
        r <<= 1;
        if (r & 0x400) {
            r ^= 0x401;
        }
        r += (unsigned char)*(name++);
    }

    return r & 0x3FF;
}

void GB_reversed_map_add_symbol(GB_reversed_symbol_map_t *map, uint16_t bank, GB_bank_symbol_t *bank_symbol)
{
    unsigned hash = hash_name(bank_symbol->name);
    GB_symbol_t *symbol = malloc(sizeof(*symbol));
    symbol->name = bank_symbol->name;
    symbol->addr = bank_symbol->addr;
    symbol->bank = bank;
    symbol->next = map->buckets[hash];
    map->buckets[hash] = symbol;
}

const GB_symbol_t *GB_reversed_map_find_symbol(GB_reversed_symbol_map_t *map, const char *name)
{
    unsigned hash = hash_name(name);
    GB_symbol_t *symbol = map->buckets[hash];

    while (symbol) {
        if (strcmp(symbol->name, name) == 0) return symbol;
        symbol = symbol->next;
    }

    return NULL;
}
