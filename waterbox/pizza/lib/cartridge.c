/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <sys/stat.h>

#include "global.h"
#include "mmu.h"
#include "utils.h"

/* buffer big enough to contain the largest possible ROM */
uint8_t rom[2 << 24];

/* battery backed RAM & RTC*/
char file_sav[1024];
char file_rtc[1024];

/* guess what              */
/* return values           */
/* 0: OK                   */
/* 1: Can't open/read file */
/* 2: Unknown cartridge    */

char cartridge_load(const void* data, size_t sz) 
{
    int i,z = 0;

    if (sz < 1 || sz > 2 << 24)
        return 1;

	memcpy(rom, data, sz);

    /* gameboy color? */
    if (rom[0x143] == 0xC0 || rom[0x143] == 0x80)
    {
        utils_log("Gameboy Color cartridge\n");
        global_cgb = 1;
    }
    else
    {
        utils_log("Gameboy Classic cartridge\n");
        global_cgb = 0;
    }

    /* get cartridge infos */
    uint8_t mbc = rom[0x147];

    utils_log("Cartridge code: %02x\n", mbc);

    switch (mbc)
    {
        case 0x00: utils_log("ROM ONLY\n"); break;
        case 0x01: utils_log("MBC1\n"); break;
        case 0x02: utils_log("MBC1 + RAM\n"); break;
        case 0x03: utils_log("MBC1 + RAM + BATTERY\n"); break;
        case 0x05: utils_log("MBC2\n"); break;
        case 0x06: mmu_init_ram(512); utils_log("MBC2 + BATTERY\n"); break;
        case 0x10: utils_log("MBC3 + TIMER + RAM + BATTERY\n"); break;
        case 0x11: utils_log("MBC3\n"); break;
        case 0x12: utils_log("MBC3 + RAM\n"); break;
        case 0x13: utils_log("MBC3 + RAM + BATTERY\n"); break;
        case 0x19: utils_log("MBC5\n"); break;
        case 0x1A: utils_log("MBC5 + RAM\n"); break;
        case 0x1B: utils_log("MBC5 + RAM + BATTERY\n"); break;
        case 0x1C: global_rumble = 1; 
                   utils_log("MBC5 + RUMBLE\n"); 
                   break;
        case 0x1D: global_rumble = 1; 
                   utils_log("MBC5 + RUMBLE + RAM\n"); 
                   break;
        case 0x1E: global_rumble = 1; 
                   utils_log("MBC5 + RUMBLE + RAM + BATTERY\n"); 
                   break;

        default: utils_log("Unknown cartridge type: %02x\n", mbc);
                 return 2;
    }

    /* title */
    for (i=0x134; i<0x143; i++)
        if (rom[i] > 0x40 && rom[i] < 0x5B)
            global_cart_name[z++] = rom[i];

    global_cart_name[z] = '\0';

    utils_log("%s\n", global_cart_name);

    /* get ROM banks */
    uint8_t byte = rom[0x148];

    utils_log("ROM: ");

    switch (byte)
    {
        case 0x00: utils_log("0 banks\n"); break;
        case 0x01: utils_log("4 banks\n"); break;
        case 0x02: utils_log("8 banks\n"); break;
        case 0x03: utils_log("16 banks\n"); break;
        case 0x04: utils_log("32 banks\n"); break;
        case 0x05: utils_log("64 banks\n"); break;
        case 0x06: utils_log("128 banks\n"); break;
        case 0x07: utils_log("256 banks\n"); break;
        case 0x52: utils_log("72 banks\n"); break;
        case 0x53: utils_log("80 banks\n"); break;
        case 0x54: utils_log("96 banks\n"); break;
    }

    /* init MMU */
    mmu_init(mbc, byte);

    /* get RAM banks */
    byte = rom[0x149];

    utils_log("RAM: ");

    switch (byte)
    {
        case 0x00: utils_log("NO RAM\n"); break;
        case 0x01: mmu_init_ram(1 << 11); utils_log("2 kB\n"); break;
        case 0x02: 
                   /* MBC5 got bigger values */
                   if (mbc >= 0x19 && mbc <= 0x1E)
                   {
                       mmu_init_ram(1 << 16); 
                       utils_log("64 kB\n"); 
                   }
                   else
                   {
                       mmu_init_ram(1 << 13); 
                       utils_log("8 kB\n"); 
                   }
                   break;
        case 0x03: mmu_init_ram(1 << 15); utils_log("32 kB\n"); break;
        case 0x04: mmu_init_ram(1 << 17); utils_log("128 kB\n"); break;
        case 0x05: mmu_init_ram(1 << 16); utils_log("64 kB\n"); break;
    }

    /* restore saved RAM if it's the case */
    mmu_restore_ram(file_sav);

    /* restore saved RTC if it's the case */
    mmu_restore_rtc(file_rtc);

    /* load FULL ROM at 0x0000 address of system memory */
    mmu_load_cartridge(rom, sz);

    return 0; 
}

void cartridge_term()
{
    /* save persistent data (battery backed RAM and RTC clock) */
    mmu_save_ram(file_sav);
    mmu_save_rtc(file_rtc);
}
