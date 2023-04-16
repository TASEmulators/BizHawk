#include <stdbool.h>
#include <stdlib.h>
#include <string.h>
#include "gb.h"

const GB_cartridge_t GB_cart_defs[256] = {
    // From http://gbdev.gg8.se/wiki/articles/The_Cartridge_Header#0147_-_Cartridge_Type
    /* MBC        SUBTYPE          RAM    BAT.   RTC    RUMB.   */
    {  GB_NO_MBC, GB_STANDARD_MBC, false, false, false, false}, // 00h  ROM ONLY
    {  GB_MBC1  , GB_STANDARD_MBC, false, false, false, false}, // 01h  MBC1
    {  GB_MBC1  , GB_STANDARD_MBC, true , false, false, false}, // 02h  MBC1+RAM
    {  GB_MBC1  , GB_STANDARD_MBC, true , true , false, false}, // 03h  MBC1+RAM+BATTERY
    [5] =
    {  GB_MBC2  , GB_STANDARD_MBC, true , false, false, false}, // 05h  MBC2
    {  GB_MBC2  , GB_STANDARD_MBC, true , true , false, false}, // 06h  MBC2+BATTERY
    [8] =
    {  GB_NO_MBC, GB_STANDARD_MBC, true , false, false, false}, // 08h  ROM+RAM
    {  GB_NO_MBC, GB_STANDARD_MBC, true , true , false, false}, // 09h  ROM+RAM+BATTERY
    [0xB] =
    /* Todo: Not supported yet */
    {  GB_NO_MBC, GB_STANDARD_MBC, false, false, false, false}, // 0Bh  MMM01
    {  GB_NO_MBC, GB_STANDARD_MBC, false, false, false, false}, // 0Ch  MMM01+RAM
    {  GB_NO_MBC, GB_STANDARD_MBC, false, false, false, false}, // 0Dh  MMM01+RAM+BATTERY
    [0xF] =
    {  GB_MBC3  , GB_STANDARD_MBC, false, true,  true , false}, // 0Fh  MBC3+TIMER+BATTERY
    {  GB_MBC3  , GB_STANDARD_MBC, true , true,  true , false}, // 10h  MBC3+TIMER+RAM+BATTERY
    {  GB_MBC3  , GB_STANDARD_MBC, false, false, false, false}, // 11h  MBC3
    {  GB_MBC3  , GB_STANDARD_MBC, true , false, false, false}, // 12h  MBC3+RAM
    {  GB_MBC3  , GB_STANDARD_MBC, true , true , false, false}, // 13h  MBC3+RAM+BATTERY
    [0x19] =
    {  GB_MBC5  , GB_STANDARD_MBC, false, false, false, false}, // 19h  MBC5
    {  GB_MBC5  , GB_STANDARD_MBC, true , false, false, false}, // 1Ah  MBC5+RAM
    {  GB_MBC5  , GB_STANDARD_MBC, true , true , false, false}, // 1Bh  MBC5+RAM+BATTERY
    {  GB_MBC5  , GB_STANDARD_MBC, false, false, false, true }, // 1Ch  MBC5+RUMBLE
    {  GB_MBC5  , GB_STANDARD_MBC, true , false, false, true }, // 1Dh  MBC5+RUMBLE+RAM
    {  GB_MBC5  , GB_STANDARD_MBC, true , true , false, true }, // 1Eh  MBC5+RUMBLE+RAM+BATTERY
    [0xFC] =
    {  GB_MBC5  , GB_CAMERA      , true , true , false, false}, // FCh  POCKET CAMERA
    {  GB_NO_MBC, GB_STANDARD_MBC, false, false, false, false}, // FDh  BANDAI TAMA5 (Todo: Not supported)
    {  GB_HUC3  , GB_STANDARD_MBC, true , true , true,  false}, // FEh  HuC3
    {  GB_HUC1  , GB_STANDARD_MBC, true , true , false, false}, // FFh  HuC1+RAM+BATTERY
};

void GB_update_mbc_mappings(GB_gameboy_t *gb)
{
    switch (gb->cartridge_type->mbc_type) {
        case GB_NO_MBC: return;
        case GB_MBC1:
            switch (gb->mbc1_wiring) {
                case GB_STANDARD_MBC1_WIRING:
                    gb->mbc_rom_bank = gb->mbc1.bank_low | (gb->mbc1.bank_high << 5);
                    if (gb->mbc1.mode == 0) {
                        gb->mbc_ram_bank = 0;
                        gb->mbc_rom0_bank = 0;
                    }
                    else {
                        gb->mbc_ram_bank = gb->mbc1.bank_high;
                        gb->mbc_rom0_bank = gb->mbc1.bank_high << 5;
                    }
                    if ((gb->mbc_rom_bank & 0x1F) == 0) {
                        gb->mbc_rom_bank++;
                    }
                    break;
                case GB_MBC1M_WIRING:
                    gb->mbc_rom_bank = (gb->mbc1.bank_low & 0xF) | (gb->mbc1.bank_high << 4);
                    if (gb->mbc1.mode == 0) {
                        gb->mbc_ram_bank = 0;
                        gb->mbc_rom0_bank = 0;
                    }
                    else {
                        gb->mbc_rom0_bank = gb->mbc1.bank_high << 4;
                        gb->mbc_ram_bank = 0;
                    }
                    if ((gb->mbc1.bank_low & 0x1F) == 0) {
                        gb->mbc_rom_bank++;
                    }
                    break;
            }
            break;
        case GB_MBC2:
            gb->mbc_rom_bank = gb->mbc2.rom_bank;
            if ((gb->mbc_rom_bank & 0xF) == 0) {
                gb->mbc_rom_bank = 1;
            }
            break;
        case GB_MBC3:
            gb->mbc_rom_bank = gb->mbc3.rom_bank;
            gb->mbc_ram_bank = gb->mbc3.ram_bank;
            if (!gb->is_mbc30) {
                gb->mbc_rom_bank &= 0x7F;
            }
            if (gb->mbc_rom_bank == 0) {
                gb->mbc_rom_bank = 1;
            }
            break;
        case GB_MBC5:
            gb->mbc_rom_bank = gb->mbc5.rom_bank_low | (gb->mbc5.rom_bank_high << 8);
            gb->mbc_ram_bank = gb->mbc5.ram_bank;
            break;
        case GB_HUC1:
            if (gb->huc1.mode == 0) {
                gb->mbc_rom_bank = gb->huc1.bank_low | (gb->mbc1.bank_high << 6);
                gb->mbc_ram_bank = 0;
            }
            else {
                gb->mbc_rom_bank = gb->huc1.bank_low;
                gb->mbc_ram_bank = gb->huc1.bank_high;
            }
            break;
        case GB_HUC3:
            gb->mbc_rom_bank = gb->huc3.rom_bank;
            gb->mbc_ram_bank = gb->huc3.ram_bank;
            break;
    }
}

void GB_configure_cart(GB_gameboy_t *gb)
{
    gb->cartridge_type = &GB_cart_defs[gb->rom[0x147]];
    
    if (gb->rom[0x147] == 0 && gb->rom_size > 0x8000) {
        GB_log(gb, "ROM header reports no MBC, but file size is over 32Kb. Assuming cartridge uses MBC3.\n");
        gb->cartridge_type = &GB_cart_defs[0x11];
    }
    else if (gb->rom[0x147] != 0 && memcmp(gb->cartridge_type, &GB_cart_defs[0], sizeof(GB_cart_defs[0])) == 0) {
        GB_log(gb, "Cartridge type %02x is not yet supported.\n", gb->rom[0x147]);
    }

    if (gb->cartridge_type->has_ram) {
        if (gb->cartridge_type->mbc_type == GB_MBC2) {
            gb->mbc_ram_size = 0x200;
        }
        else {
            static const unsigned ram_sizes[256] = {0, 0x800, 0x2000, 0x8000, 0x20000, 0x10000};
            gb->mbc_ram_size = ram_sizes[gb->rom[0x149]];
        }
        
        if (gb->mbc_ram_size) {
            gb->mbc_ram = malloc(gb->mbc_ram_size);
        }

        /* Todo: Some games assume unintialized MBC RAM is 0xFF. It this true for all cartridges types? */
        memset(gb->mbc_ram, 0xFF, gb->mbc_ram_size);
    }

    /* MBC1 has at least 3 types of wiring (We currently support two (Standard and 4bit-MBC1M) of these).
       See http://forums.nesdev.com/viewtopic.php?f=20&t=14099 */

    /* Attempt to "guess" wiring */
    if (gb->cartridge_type->mbc_type == GB_MBC1) {
        if (gb->rom_size >= 0x44000 && memcmp(gb->rom + 0x104, gb->rom + 0x40104, 0x30) == 0) {
            gb->mbc1_wiring = GB_MBC1M_WIRING;
        }
    }
    
    /* Detect MBC30 */
    if (gb->cartridge_type->mbc_type == GB_MBC3) {
        if (gb->rom_size > 0x200000 || gb->mbc_ram_size > 0x8000) {
            gb->is_mbc30 = true;
        }
    }
    
    /* Set MBC5's bank to 1 correctly */
    if (gb->cartridge_type->mbc_type == GB_MBC5) {
        gb->mbc5.rom_bank_low = 1;
    }
}
