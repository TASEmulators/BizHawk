#include "gb.h"
#include "cheats.h"
#include <stdio.h>
#include <assert.h>
#include <errno.h>

static inline uint8_t hash_addr(uint16_t addr)
{
    return addr;
}

static uint16_t bank_for_addr(GB_gameboy_t *gb, uint16_t addr)
{
    if (addr < 0x4000) {
        return gb->mbc_rom0_bank;
    }
    
    if (addr < 0x8000) {
        return gb->mbc_rom_bank;
    }
    
    if (addr < 0xD000) {
        return 0;
    }
    
    if (addr < 0xE000) {
        return gb->cgb_ram_bank;
    }
    
    return 0;
}

void GB_apply_cheat(GB_gameboy_t *gb, uint16_t address, uint8_t *value)
{
    if (!gb->cheat_enabled) return;
    if (!gb->boot_rom_finished) return;
    const GB_cheat_hash_t *hash = gb->cheat_hash[hash_addr(address)];
    if (hash) {
        for (unsigned i = 0; i < hash->size; i++) {
            GB_cheat_t *cheat = hash->cheats[i];
            if (cheat->address == address && cheat->enabled && (!cheat->use_old_value || cheat->old_value == *value)) {
                if (cheat->bank == GB_CHEAT_ANY_BANK || cheat->bank == bank_for_addr(gb, address)) {
                    *value = cheat->value;
                    break;
                }
            }
        }
    }
}

bool GB_cheats_enabled(GB_gameboy_t *gb)
{
    return gb->cheat_enabled;
}

void GB_set_cheats_enabled(GB_gameboy_t *gb, bool enabled)
{
    gb->cheat_enabled = enabled;
}

void GB_add_cheat(GB_gameboy_t *gb, const char *description, uint16_t address, uint16_t bank, uint8_t value, uint8_t old_value, bool use_old_value, bool enabled)
{
    GB_cheat_t *cheat = malloc(sizeof(*cheat));
    cheat->address = address;
    cheat->bank = bank;
    cheat->value = value;
    cheat->old_value = old_value;
    cheat->use_old_value = use_old_value;
    cheat->enabled = enabled;
    strncpy(cheat->description, description, sizeof(cheat->description));
    cheat->description[sizeof(cheat->description) - 1] = 0;
    gb->cheats = realloc(gb->cheats, (++gb->cheat_count) * sizeof(*cheat));
    gb->cheats[gb->cheat_count - 1] = cheat;
    
    GB_cheat_hash_t **hash = &gb->cheat_hash[hash_addr(address)];
    if (!*hash) {
        *hash = malloc(sizeof(GB_cheat_hash_t) + sizeof(cheat));
        (*hash)->size = 1;
        (*hash)->cheats[0] = cheat;
    }
    else {
        (*hash)->size++;
        *hash = realloc(*hash, sizeof(GB_cheat_hash_t) + sizeof(cheat) * (*hash)->size);
        (*hash)->cheats[(*hash)->size - 1] = cheat;
    }
}

const GB_cheat_t *const *GB_get_cheats(GB_gameboy_t *gb, size_t *size)
{
    *size = gb->cheat_count;
    return (void *)gb->cheats;
}
void GB_remove_cheat(GB_gameboy_t *gb, const GB_cheat_t *cheat)
{
    for (unsigned i = 0; i < gb->cheat_count; i++) {
        if (gb->cheats[i] == cheat) {
            gb->cheats[i] = gb->cheats[--gb->cheat_count];
            if (gb->cheat_count == 0) {
                free(gb->cheats);
                gb->cheats = NULL;
            }
            else {
                gb->cheats = realloc(gb->cheats, gb->cheat_count * sizeof(*cheat));
            }
            break;
        }
    }
    
    GB_cheat_hash_t **hash = &gb->cheat_hash[hash_addr(cheat->address)];
    for (unsigned i = 0; i < (*hash)->size; i++) {
        if ((*hash)->cheats[i] == cheat) {
            (*hash)->cheats[i] = (*hash)->cheats[(*hash)->size--];
            if ((*hash)->size == 0) {
                free(*hash);
                *hash = NULL;
            }
            else {
                *hash = malloc(sizeof(GB_cheat_hash_t) + sizeof(cheat) * (*hash)->size);
            }
            break;
        }
    }
    
    free((void *)cheat);
}

bool GB_import_cheat(GB_gameboy_t *gb, const char *cheat, const char *description, bool enabled)
{
    uint8_t dummy;
    /* GameShark */
    {
        uint8_t bank;
        uint8_t value;
        uint16_t address;
        if (sscanf(cheat, "%02hhx%02hhx%04hx%c", &bank, &value, &address, &dummy) == 3) {
            if (bank >= 0x80) {
                bank &= 0xF;
            }
            GB_add_cheat(gb, description, address, bank, value, 0, false, enabled);
            return true;
        }
    }
    
    /* GameGenie */
    {
        char stripped_cheat[10] = {0,};
        for (unsigned i = 0; i < 9 && *cheat; i++) {
            stripped_cheat[i] = *(cheat++);
            while (*cheat == '-') {
                cheat++;
            }
        }
        
        // Delete the 7th character;
        stripped_cheat[7] = stripped_cheat[8];
        stripped_cheat[8] = 0;
        
        uint8_t old_value;
        uint8_t value;
        uint16_t address;
        if (sscanf(stripped_cheat, "%02hhx%04hx%02hhx%c", &value, &address, &old_value, &dummy) == 3) {
            address = (uint16_t)(address >> 4) | (uint16_t)(address << 12);
            address ^= 0xF000;
            if (address > 0x7FFF) {
                return false;
            }
            old_value = (uint8_t)(old_value >> 2) | (uint8_t)(old_value << 6);
            old_value ^= 0xBA;
            GB_add_cheat(gb, description, address, GB_CHEAT_ANY_BANK, value, old_value, true, enabled);
            return true;
        }
        
        if (sscanf(stripped_cheat, "%02hhx%04hx%c", &value, &address, &dummy) == 2) {
            address = (uint16_t)(address >> 4) | (uint16_t)(address << 12);
            address ^= 0xF000;
            if (address > 0x7FFF) {
                return false;
            }
            GB_add_cheat(gb, description, address, GB_CHEAT_ANY_BANK, value, false, true, enabled);
            return true;
        }
    }
    return false;
}

void GB_update_cheat(GB_gameboy_t *gb, const GB_cheat_t *_cheat, const char *description, uint16_t address, uint16_t bank, uint8_t value, uint8_t old_value, bool use_old_value, bool enabled)
{
    GB_cheat_t *cheat = NULL;
    for (unsigned i = 0; i < gb->cheat_count; i++) {
        if (gb->cheats[i] == _cheat) {
            cheat = gb->cheats[i];
            break;
        }
    }
    
    assert(cheat);
    
    if (cheat->address != address) {
        /* Remove from old bucket */
        GB_cheat_hash_t **hash = &gb->cheat_hash[hash_addr(cheat->address)];
        for (unsigned i = 0; i < (*hash)->size; i++) {
            if ((*hash)->cheats[i] == cheat) {
                (*hash)->cheats[i] = (*hash)->cheats[(*hash)->size--];
                if ((*hash)->size == 0) {
                    free(*hash);
                    *hash = NULL;
                }
                else {
                    *hash = malloc(sizeof(GB_cheat_hash_t) + sizeof(cheat) * (*hash)->size);
                }
                break;
            }
        }
        cheat->address = address;
        
        /* Add to new bucket */
        hash = &gb->cheat_hash[hash_addr(address)];
        if (!*hash) {
            *hash = malloc(sizeof(GB_cheat_hash_t) + sizeof(cheat));
            (*hash)->size = 1;
            (*hash)->cheats[0] = cheat;
        }
        else {
            (*hash)->size++;
            *hash = malloc(sizeof(GB_cheat_hash_t) + sizeof(cheat) * (*hash)->size);
            (*hash)->cheats[(*hash)->size - 1] = cheat;
        }
    }
    cheat->bank = bank;
    cheat->value = value;
    cheat->old_value = old_value;
    cheat->use_old_value = use_old_value;
    cheat->enabled = enabled;
    if (description != cheat->description) {
        strncpy(cheat->description, description, sizeof(cheat->description));
        cheat->description[sizeof(cheat->description) - 1] = 0;
    }
}

#define CHEAT_MAGIC 'SBCh'

void GB_load_cheats(GB_gameboy_t *gb, const char *path)
{
    FILE *f = fopen(path, "rb");
    if (!f) {
        return;
    }
    
    uint32_t magic = 0;
    uint32_t struct_size = 0;
    fread(&magic, sizeof(magic), 1, f);
    fread(&struct_size, sizeof(struct_size), 1, f);
    if (magic != CHEAT_MAGIC && magic != __builtin_bswap32(CHEAT_MAGIC)) {
        GB_log(gb, "The file is not a SameBoy cheat database");
        return;
    }
    
    if (struct_size != sizeof(GB_cheat_t)) {
        GB_log(gb, "This cheat database is not compatible with this version of SameBoy");
        return;
    }
    
    // Remove all cheats first
    while (gb->cheats) {
        GB_remove_cheat(gb, gb->cheats[0]);
    }
    
    GB_cheat_t cheat;
    while (fread(&cheat, sizeof(cheat), 1, f)) {
        if (magic == __builtin_bswap32(CHEAT_MAGIC)) {
            cheat.address = __builtin_bswap16(cheat.address);
            cheat.bank = __builtin_bswap16(cheat.bank);
        }
        cheat.description[sizeof(cheat.description) - 1] = 0;
        GB_add_cheat(gb, cheat.description, cheat.address, cheat.bank, cheat.value, cheat.old_value, cheat.use_old_value, cheat.enabled);
    }
    
    return;
}

int GB_save_cheats(GB_gameboy_t *gb, const char *path)
{
    if (!gb->cheat_count) return 0; // Nothing to save.
    FILE *f = fopen(path, "wb");
    if (!f) {
        GB_log(gb, "Could not dump cheat database: %s.\n", strerror(errno));
        return errno;
    }
    
    uint32_t magic = CHEAT_MAGIC;
    uint32_t struct_size = sizeof(GB_cheat_t);
    
    if (fwrite(&magic, sizeof(magic), 1, f) != 1) {
        fclose(f);
        return EIO;
    }
    
    if (fwrite(&struct_size, sizeof(struct_size), 1, f) != 1) {
        fclose(f);
        return EIO;
    }
    
    for (size_t i = 0; i <gb->cheat_count; i++) {
        if (fwrite(gb->cheats[i], sizeof(*gb->cheats[i]), 1, f) != 1) {
            fclose(f);
            return EIO;
        }
    }
    
    errno = 0;
    fclose(f);
    return errno;
}
