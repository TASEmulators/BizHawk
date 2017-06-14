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

#include "cycles.h"
#include "global.h"
#include "gpu.h"
#include "interrupt.h"
#include "input.h"
#include "mmu.h"
#include "sound.h"
#include "serial.h"
#include "timer.h"
#include "utils.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <strings.h>
#include <time.h>

/* GAMEBOY MEMORY AREAS 

0x0000 - 0x00FF - BIOS
0x0000 - 0x3FFF - First 16k of game ROM (permanent)
0x4000 - 0x7FFF - ROM banks (switchable)
0x8000 - 0x9FFF - Video RAM (8kb - keeps pixels data) 
0xA000 - 0xBFFF - External RAM (switchable, it was on cartridge,
                                8kb banks, max 32k, NON volatile)
0xC000 - 0xDFFF - Gameboy RAM
0xE000 - 0xEFFF - ????????????????
0xFE00 - 0xFF7F - I/O
0xFF80 - 0xFFFE - Temp RAM
0xFFFF          - Turn on/off interrupts

*/

/* cartridge memory (max 8MB) */
uint8_t cart_memory[1 << 22];

/* RAM memory area */
uint8_t *ram;
uint32_t ram_sz;

/* main struct */
mmu_t mmu;

/* function to call when rumble */
mmu_rumble_cb_t mmu_rumble_cb = NULL;


/* return absolute memory address */
void *mmu_addr(uint16_t a)
{
    return (void *) &mmu.memory[a];
}

/* return absolute memory address */
void *mmu_addr_vram0()
{
    return (void *) &mmu.vram0;
}

/* return absolute memory address */
void *mmu_addr_vram1()
{
    return (void *) &mmu.vram1;
}

/* debug purposes */
void mmu_dump_all()
{
    int i;

    printf("#### MAIN MEMORY ####\n\n");

    for (i=0; i<0x10000; i++)
    {
        if ((i & 0x0f) == 0x00)
            printf("\n%04x: ", i);
        printf(" %02x", mmu.memory[i]);
    }

    if (global_cgb)
    {
        printf("#### VRAM 0 ####\n\n");

        for (i=0; i<0x2000; i++)
        {
            if ((i & 0x0f) == 0x00)
                printf("\n%04x: ", i);
            printf(" %02x", mmu.vram0[i]);
        }

        printf("#### VRAM 1 ####\n\n");

        for (i=0; i<0x2000; i++)
        {
            if ((i & 0x0f) == 0x00)
                printf("\n%04x: ", i);
            printf(" %02x", mmu.vram1[i]);
        }
    }
}

/* init (alloc) system state.memory */
void mmu_init(uint8_t c, uint8_t rn)
{
    mmu.rom_current_bank = 0x01;
    mmu.ram_current_bank = 0x00;

    /* set ram to NULL */
    ram = NULL;

    /* save carttype and qty of ROM blocks */
    mmu.carttype = c;
    mmu.roms = rn;

    mmu.vram_idx = 0;
    mmu.wram_current_bank = 1;
    mmu.ram_current_bank = 0;
    mmu.ram_external_enabled = 0;
    mmu.dma_cycles = 0;
    mmu.dma_address = 0;
    mmu.rtc_mode = 0;
    time(&mmu.rtc_time);

    /* reset memory */
    bzero(mmu.memory, 65536);
}

/* init (alloc) system state.memory */
void mmu_init_ram(uint32_t c)
{
    ram_sz = c;

    ram = malloc(c);

    bzero(ram, c);
}

/* load data in a certain address */
void mmu_load(uint8_t *data, size_t sz, uint16_t a)
{
    memcpy(&mmu.memory[a], data, sz);
}

/* load full cartridge */
void mmu_load_cartridge(uint8_t *data, size_t sz)
{
    /* copy max 32k into working memory */
    memcpy(mmu.memory, data, 2 << 14);

    /* copy full cartridge */
    memcpy(cart_memory, data, sz);
}

/* move 8 bit from s to d */
void mmu_move(uint16_t d, uint16_t s)
{
    mmu_write(d, mmu_read(s));
}

/* read 8 bit data from a memory addres */
uint8_t mmu_read(uint16_t a)
{
    /* always takes 4 cycles */
    cycles_step();

    /* 90% of the read is in the ROM area */
    if (a < 0x8000)
        return mmu.memory[a];

    /* test VRAM */
    if (a < 0xA000)
    {
        if (global_cgb)
        {
            if (mmu.vram_idx == 0)
                return mmu.vram0[a - 0x8000];
            else
                return mmu.vram1[a - 0x8000];
        }
         
        return mmu.memory[a];
    }

    if (a < 0xC000)
    {
        if (mmu.rtc_mode != 0x00)
        {
            time_t diff = mmu.rtc_latch_time - mmu.rtc_time;

            switch (mmu.rtc_mode)
            {
                case 0x08:
                    return (diff % 60);
                case 0x09:
                    return ((diff / 60) % 60);
                case 0x0A:
                    return (diff / 3600) % 24;
                case 0x0B:
                    return (diff / (3600 * 24)) & 0x00FF;
                case 0x0C:
                    return ((diff / (3600 * 24)) & 0xFF00) >> 8;
            }
        }
        else
            return mmu.memory[a];
    }

    /* RAM */
    if (a < 0xE000)
        return mmu.memory[a];

    /* RAM mirror */
    if (a < 0xFE00)
        return mmu.memory[a - 0x2000];

    switch (a)
    {
        /* serial registers */
        case 0xFF01: 
        case 0xFF02: 
            return serial_read_reg(a);

        /* don't ask me why.... */
        case 0xFF44:
            return (mmu.memory[0xFF44] == 153 ? 0 : mmu.memory[0xFF44]);

        /* sound registers */
        case 0xFF10 ... 0xFF3F:
            return sound_read_reg(a, mmu.memory[a]);

        /* joypad reading */
        case 0xFF00:
            return input_get_keys(mmu.memory[a]);

        /* CGB HDMA transfer */
        case 0xFF55:

            if (!global_cgb) break;

            /* HDMA result */
            if (mmu.hdma_to_transfer)
                return (mmu.hdma_to_transfer / 0x10 - 0x01);
            else
                return 0xFF;

        /* CGB color palette registers */
        case 0xFF68:
        case 0xFF69:
        case 0xFF6A:
        case 0xFF6B:

            if (!global_cgb) break;

            /* color palettes registers */
            return gpu_read_reg(a);

        /* timer registers */
        case 0xFF04 ... 0xFF07:
            return timer_read_reg(a);      
      
    }

    return mmu.memory[a];
}

/* read 16 bit data from a memory addres */
unsigned int mmu_read_16(uint16_t a)
{
    return (mmu_read(a) | (mmu_read(a + 1) << 8));
}

/* read 8 bit data from a memory addres (not affecting cycles) */
uint8_t mmu_read_no_cyc(uint16_t a)
{
    if (a >= 0xE000 && a <= 0xFDFF)
        return mmu.memory[a - 0x2000];

    return mmu.memory[a];
}

void mmu_restore_ram(char *fn)
{   
    /* save only if cartridge got a battery */
    if (mmu.carttype == 0x03 ||
        mmu.carttype == 0x06 ||
        mmu.carttype == 0x09 ||
        mmu.carttype == 0x0D ||
        mmu.carttype == 0x0F ||
        mmu.carttype == 0x10 ||
        mmu.carttype == 0x13 ||
        mmu.carttype == 0x17 ||
        mmu.carttype == 0x1B ||
        mmu.carttype == 0x1E ||
        mmu.carttype == 0x22 ||
        mmu.carttype == 0xFF)
    {
        FILE *fp = fopen(fn, "r+");

        /* it could be not present */
        if (fp == NULL)
            return;

        if (ram_sz <= 0x2000)
        {
            /* no need to put togheter pieces of ram banks */
            fread(&mmu.memory[0xA000], ram_sz, 1, fp);
        }
        else
        {
            /* read entire file into ram buffer */
            fread(mmu.ram_internal, 0x2000, 1, fp);
            fread(ram, ram_sz, 1, fp);

            /* copy internal RAM to 0xA000 address */
            memcpy(&mmu.memory[0xA000], mmu.ram_internal, 0x2000);
        }

        fclose(fp);
    } 
}

void mmu_restore_rtc(char *fn)
{
    /* save only if cartridge got a battery */
    if (mmu.carttype == 0x10 ||
        mmu.carttype == 0x13) 
    {
        FILE *fp = fopen(fn, "r+");

        /* it could be not present */
        if (fp == NULL)
        {
            /* just pick current time */
            time(&mmu.rtc_time);
            return;
        }

        /* read last saved time */
        fscanf(fp, "%ld", &mmu.rtc_time);

        fclose(fp);
    }
}

void mmu_save_ram(char *fn)
{
    /* save only if cartridge got a battery */
    if (mmu.carttype == 0x03 || 
        mmu.carttype == 0x06 ||
        mmu.carttype == 0x09 ||
        mmu.carttype == 0x0d ||
        mmu.carttype == 0x0f ||
        mmu.carttype == 0x10 ||
        mmu.carttype == 0x13 ||
        mmu.carttype == 0x17 ||
        mmu.carttype == 0x1b ||
        mmu.carttype == 0x1e ||
        mmu.carttype == 0x22 ||
        mmu.carttype == 0xff)
    {
        FILE *fp = fopen(fn, "w+");

        if (fp == NULL)
        {
            printf("Error dumping RAM\n");
            return;
        } 

        if (ram_sz <= 0x2000)
        {
            /* no need to put togheter pieces of ram banks */
            fwrite(&mmu.memory[0xA000], ram_sz, 1, fp);
        }
        else
        {
            /* yes, i need to put togheter pieces */

            /* save current used bank */
            if (mmu.ram_external_enabled)
                memcpy(&ram[0x2000 * mmu.ram_current_bank],
                       &mmu.memory[0xA000], 0x2000);
            else
                memcpy(mmu.ram_internal,
                       &mmu.memory[0xA000], 0x2000);
           
            /* dump the entire internal + external RAM */
            fwrite(mmu.ram_internal, 0x2000, 1, fp); 
            fwrite(ram, ram_sz, 1, fp); 
        }

        fclose(fp);
    }
}

void mmu_save_rtc(char *fn)
{
    /* save only if cartridge got a battery */
    if (mmu.carttype == 0x10 ||
        mmu.carttype == 0x13)
    {
        FILE *fp = fopen(fn, "w+");

        if (fp == NULL)
        {
            printf("Error saving RTC\n");
            return;
        }

        fprintf(fp, "%ld", mmu.rtc_time);
    }
}

void mmu_set_rumble_cb(mmu_rumble_cb_t cb)
{
    mmu_rumble_cb = cb;
}

void mmu_term()
{
    if (ram)
    {
        free(ram);
        ram = NULL;
    }
}

/* write 16 bit block on a memory address */
void mmu_write(uint16_t a, uint8_t v)
{
    /* update cycles AFTER memory set */
    cycles_step();

    /* color gameboy stuff */
    if (global_cgb)
    {
        /* VRAM write? */
        if (a >= 0x8000 && a < 0xA000)
        {
            if (mmu.vram_idx == 0)
                mmu.vram0[a - 0x8000] = v;
            else
                mmu.vram1[a - 0x8000] = v;

            return;
        }
        else 
        {
            /* wanna access to RTC register? */
            if (a >= 0xA000 && a <= 0xBFFF && mmu.rtc_mode != 0x00)
            {
                time_t t,s1,s2,m1,m2,h1,h2,d1,d2,days;

                /* get current time */
                time(&t);

                /* extract parts in seconds from current and ref times */
                s1 = t % 60;
                s2 = mmu.rtc_time % 60;

                m1 = (t - s1) % (60 * 60);
                m2 = (mmu.rtc_time - s2) % (60 * 60);

                h1 = (t - m1 - s1) % (60 * 60 * 24);
                h2 = (mmu.rtc_time - m2 - s2) % (60 * 60 * 24);

                d1 = t - h1 - m1 - s1; 
                d2 = mmu.rtc_time - h2 - m2 - s2; 

                switch (mmu.rtc_mode)
                {
                    case 0x08:

                        /* remove seconds from current time */
                        mmu.rtc_time -= s2;

                        /* set new seconds */
                        mmu.rtc_time += (s1 - v);

                        return;
                    
                    case 0x09:

                        /* remove seconds from current time */
                        mmu.rtc_time -= m2;

                        /* set new seconds */
                        mmu.rtc_time += (m1 - (v * 60));

                        return;
                    
                    case 0x0A:

                        /* remove seconds from current time */
                        mmu.rtc_time -= h2;

                        /* set new seconds */
                        mmu.rtc_time += (h1 - (v * 60 * 24));

                        return;
                    
                    case 0x0B:

                        days = (((d1 - d2) / 
                                (60 * 60 * 24)) & 0xFF00) | v;

                        /* remove seconds from current time */
                        mmu.rtc_time -= d2;

                        /* set new seconds */
                        mmu.rtc_time += (d1 - (days * 60 * 60 * 24));

                        return;

                    case 0x0C:

                        days = (((d1 - d2) / 
                                (60 * 60 * 24)) & 0xFEFF) | (v << 8);

                        /* remove seconds from current time */
                        mmu.rtc_time -= d2;

                        /* set new seconds */
                        mmu.rtc_time += (d1 - (days * 60 * 60 * 24));

                        return;
                }
            }
        }
            
        /* switch WRAM */
        if (a == 0xFF70)
        {
            /* number goes from 1 to 7 */
            uint8_t new = (v & 0x07);

            if (new == 0) 
                new = 1;

            if (new == mmu.wram_current_bank)
                return;

            /* save current bank */
            memcpy(&mmu.wram[0x1000 * mmu.wram_current_bank],
                   &mmu.memory[0xD000], 0x1000);

            mmu.wram_current_bank = new;

            /* move new ram bank */
            memcpy(&mmu.memory[0xD000],
                   &mmu.wram[0x1000 * mmu.wram_current_bank],
                   0x1000);

            /* save current bank */
            mmu.memory[0xFF70] = new;

            return;
        }

        if (a == 0xFF4F)
        {
            /* extract VRAM index from last bit */            
            mmu.vram_idx = (v & 0x01);

            /* save current VRAM bank */
            mmu.memory[0xFF4F] = mmu.vram_idx;

            return;
        }
    }

    /* wanna write on ROM? */
    if (a < 0x8000)
    {
        /* return in case of ONLY ROM */
        if (mmu.carttype == 0x00)
            return;

        /* TODO - MBC strategies */
        uint8_t b = mmu.rom_current_bank;

        switch (mmu.carttype)
        {
            /* MBC1 */
            case 0x01: 
            case 0x02:  
            case 0x03: 

                if (a >= 0x2000 && a <= 0x3FFF) 
                {
                    /* reset 5 bits */
                    b = mmu.rom_current_bank & 0xE0;

                    /* set them with new value */
                    b |= v & 0x1F;

                    /* doesn't fit on max rom number? */
                    if (b > (2 << mmu.roms))
                    {
                        /* filter result to get a value < max rom number */
                        b %= (2 << mmu.roms);
                    }

                    /* 0x00 is not valid, switch it to 0x01 */
                    if (b == 0x00)
                        b = 0x01;
                }
                else if (a >= 0x4000 && a <= 0x5FFF)
                {
                    /* ROM banking? it's about 2 higher bits */
                    if (mmu.banking == 0)
                    {
                        /* reset 5 bits */
                        b = mmu.rom_current_bank & 0x1F;

                        /* set them with new value */
                        b |= (v << 5);

                        /* doesn't fit on max rom number? */
                        if (b > (2 << mmu.roms))
                        {
                            /* filter result to get a value < max rom number */
                            b %= (2 << mmu.roms);
                        }
                    }
                    else
                    {
                        if ((0x2000 * v) < ram_sz)
                        { 
                            /* save current bank */
                            memcpy(&ram[0x2000 * mmu.ram_current_bank], 
                                   &mmu.memory[0xA000], 0x2000);
  
                            mmu.ram_current_bank = v;

                            /* move new ram bank */
                            memcpy(&mmu.memory[0xA000], 
                                   &ram[0x2000 * mmu.ram_current_bank], 
                                   0x2000);
                        }
                    }
                }
                else if (a >= 0x6000 && a <= 0x7FFF)
                    mmu.banking = v;

                break;

            /* MBC2 */
            case 0x05:
            case 0x06: 

                if (a >= 0x2000 && a <= 0x3FFF) 
                {
                    /* use lower nibble to set current bank */
                    b = v & 0x0f;

                    /*if (b != rom_current_bank)
                        memcpy(&memory[0x4000], 
                               &cart_memory[b * 0x4000], 0x4000);

                    rom_current_bank = b;*/
                }

                break;

            /* MBC3 */
            case 0x10:
            case 0x13:

                if (a >= 0x0000 && a <= 0x1FFF)
                {
                    if (v == 0x0A)
                    {
                        /* already enabled? */
                        if (mmu.ram_external_enabled)
                            return;

                        /* save current bank */
                        memcpy(mmu.ram_internal,
                               &mmu.memory[0xA000], 0x2000);

                        /* restore external ram bank */
                        memcpy(&mmu.memory[0xA000],
                               &ram[0x2000 * mmu.ram_current_bank],
                               0x2000);

                        /* set external RAM eanbled flag */
                        mmu.ram_external_enabled = 1;

                        return;
                    }

                    if (v == 0x00)
                    {
                        /* already disabled? */
                        if (mmu.ram_external_enabled == 0)
                            return;

                        /* save current bank */
                        memcpy(&ram[0x2000 * mmu.ram_current_bank],
                               &mmu.memory[0xA000], 0x2000);

                        /* restore external ram bank */
                        memcpy(&mmu.memory[0xA000],
                               mmu.ram_internal, 0x2000);

                        /* clear external RAM eanbled flag */
                        mmu.ram_external_enabled = 0;
                    }
                }
                else if (a >= 0x2000 && a <= 0x3FFF)
                {
                    /* set them with new value */
                    b = v & 0x7F;

                    /* doesn't fit on max rom number? */
                    if (b > (2 << mmu.roms))
                    {
                        /* filter result to get a value < max rom number */
                        b %= (2 << mmu.roms);
                    }

                    /* 0x00 is not valid, switch it to 0x01 */
                    if (b == 0x00)
                        b = 0x01;
                }
                else if (a >= 0x4000 && a <= 0x5FFF)
                {
                    /* 0x00 to 0x07 is referred to RAM bank */
                    if (v < 0x08)
                    {
                        /* not on RTC mode anymore */
                        mmu.rtc_mode = 0x00;

                        if ((0x2000 * (v & 0x0f)) < ram_sz)
                        {
                            /* save current bank */
                            memcpy(&ram[0x2000 * mmu.ram_current_bank],
                                   &mmu.memory[0xA000], 0x2000);
  
                            mmu.ram_current_bank = v & 0x0f;

                            /* move new ram bank */
                            memcpy(&mmu.memory[0xA000],
                                   &ram[0x2000 * mmu.ram_current_bank],
                                   0x2000);
                        }
                    }
                    else if (v < 0x0d)
                    {
                        /* from 0x08 to 0x0C trigger RTC mode */
                        mmu.rtc_mode = v;
                    }
                    
                }
                else if (a >= 0x6000 && a <= 0x7FFF)
                {
                    /* latch clock data. move clock data to RTC registers */
                    time(&mmu.rtc_latch_time);
                }


                break;

            /* MBC5 */
            case 0x19:
            case 0x1A:
            case 0x1B:
            case 0x1C:
            case 0x1D:
            case 0x1E:

                if (a >= 0x0000 && a <= 0x1FFF)
                {
                    if (v == 0x0A)
                    {
                        /* we got external RAM? some stupid game try */
                        /* to access it despite it doesn't have it   */
                        if (ram_sz == 0)
                            return;

                        /* already enabled? */
                        if (mmu.ram_external_enabled)
                            return;

                        /* save current bank */
                        memcpy(mmu.ram_internal,
                               &mmu.memory[0xA000], 0x2000);

                        /* restore external ram bank */
                        memcpy(&mmu.memory[0xA000],
                               &ram[0x2000 * mmu.ram_current_bank],
                               0x2000);

                        /* set external RAM eanbled flag */
                        mmu.ram_external_enabled = 1;

                        return;
                    }

                    if (v == 0x00)
                    {
                        /* we got external RAM? some stpd game try to do shit */
                        if (ram_sz == 0)
                            return;

                        /* already disabled? */
                        if (mmu.ram_external_enabled == 0)
                            return;

                        /* save current bank */
                        memcpy(&ram[0x2000 * mmu.ram_current_bank], 
                               &mmu.memory[0xA000], 0x2000);

                        /* restore external ram bank */
                        memcpy(&mmu.memory[0xA000],
                               mmu.ram_internal, 0x2000);

                        /* clear external RAM eanbled flag */
                        mmu.ram_external_enabled = 0;
                    }
                }
                if (a >= 0x2000 && a <= 0x2FFF)
                {
                    /* set them with new value */
                    b = (mmu.rom_current_bank & 0xFF00) | v;

                    /* doesn't fit on max rom number? */
                    if (b > (2 << mmu.roms))
                    {
                        /* filter result to get a value < max rom number */
                        b %= (2 << mmu.roms);
                    }
                }
                else if (a >= 0x3000 && a <= 0x3FFF)
                {
                    /* set them with new value */
                    b = (mmu.rom_current_bank & 0x00FF) | ((v & 0x01) << 8);

                    /* doesn't fit on max rom number? */
                    if (b > (2 << mmu.roms))
                    {
                        /* filter result to get a value < max rom number */
                        b %= (2 << mmu.roms);
                    }
                }
                else if (a >= 0x4000 && a <= 0x5FFF)
                {
                    uint8_t mask = 0x0F;

                    if (global_rumble)
                    {
                        mask = 0x07;

                        if (mmu_rumble_cb)
                            (*mmu_rumble_cb) ((v & 0x08) ? 1 : 0);

                        /* check if we want to appizz the motor */
/*                        if (v & 0x08)
                            printf("APPIZZ MOTOR\n");
                        else
                            printf("SPEGN MOTOR\n");*/
                    }

                    if ((0x2000 * (v & mask)) < ram_sz)
                    {
                        /* is externa RAM enabled? */
                        if (!mmu.ram_external_enabled)
                            break;

                        /* wanna switch on the same bank? =\ just discard it */
                        if ((v & 0x0f) == mmu.ram_current_bank)
                            break;

                        /* save current bank */
                        memcpy(&ram[0x2000 * mmu.ram_current_bank],
                               &mmu.memory[0xA000], 0x2000);

                        mmu.ram_current_bank = (v & 0x0f);

                        /* move new ram bank */
                        memcpy(&mmu.memory[0xA000],
                               &ram[0x2000 * mmu.ram_current_bank],
                               0x2000);
                    }
                }

                break;

        }

        /* need to switch? */
        if (b != mmu.rom_current_bank)
        {
            /* copy from cartridge rom to GB switchable bank area */
            memcpy(&mmu.memory[0x4000], &cart_memory[b * 0x4000], 0x4000);

            /* save new current bank */
            mmu.rom_current_bank = b;

            /* re-apply cheats */
//            mmu_apply_gg();
        }

        return; 
    }

    if (a >= 0xE000)
    {
        /* changes on sound registers? */
        if (a >= 0xFF10 && a <= 0xFF3F)
        {
            /* set memory */
            sound_write_reg(a, v);

            return;
        }

        /* mirror area */
        if (a >= 0xE000 && a <= 0xFDFF)
        {
            mmu.memory[a - 0x2000] = v;
            return;
        } 

        /* TODO - put them all */
        switch(a)
        { 
            case 0xFF01: 
            case 0xFF02:
                serial_write_reg(a, v);
                return;
            case 0xFF04 ... 0xFF07:
                timer_write_reg(a, v);
                return;
        }

        /* LCD turned on/off? */
        if (a == 0xFF40)
        {
            if ((v ^ mmu.memory[0xFF40]) & 0x80)
                gpu_toggle(v);
        }

        /* only 5 high bits are writable */
        if (a == 0xFF41)
        {
            mmu.memory[a] = (mmu.memory[a] & 0x07) | (v & 0xf8);
            return;
        }
            
        /* palette update */
        if ((a >= 0xFF47 && a <= 0xFF49) ||
            (a >= 0xFF68 && a <= 0xFF6B))
            gpu_write_reg(a, v);

        /* CGB only registers */
        if (global_cgb)
        {
            switch (a)
            {
                case 0xFF4D: 

                    /* wanna switch speed? */
                    if (v & 0x01)
                    {
                        global_cpu_double_speed ^= 0x01;

                        /* update new clock */ 
                        // cycles_clock = 4194304 << global_double_speed;
                        cycles_set_speed(1);
                        sound_set_speed(1);
                        gpu_set_speed(1);

                        /* save into memory i'm working at double speed */
                        if (global_cpu_double_speed)
                            mmu.memory[a] = 0x80;
                        else 
                            mmu.memory[a] = 0x00;
                    }
 
                    return;
 
                case 0xFF52: 

                    /* high byte of HDMA source address */
                    mmu.hdma_src_address &= 0xff00;

                    /* lower 4 bits are ignored */
                    mmu.hdma_src_address |= (v & 0xf0);
             
                    break;

                case 0xFF51:

                    /* low byte of HDMA source address */
                    mmu.hdma_src_address &= 0x00ff;

                    /* highet 3 bits are ignored (always 100 binary) */
                    mmu.hdma_src_address |= (v << 8); 

                    break;

                case 0xFF54:

                    /* high byte of HDMA source address */
                    mmu.hdma_dst_address &= 0xff00;

                    /* lower 4 bits are ignored */
                    mmu.hdma_dst_address |= (v & 0xf0);

                    break;

                case 0xFF53:

                    /* low byte of HDMA source address */
                    mmu.hdma_dst_address &= 0x00ff;

                    /* highet 3 bits are ignored (always 100 binary) */
                    mmu.hdma_dst_address |= ((v & 0x1f) | 0x80) << 8;

                    break;

                case 0xFF55:

                    /* wanna stop HBLANK transfer? a zero on 7th bit will do */
                    if ((v & 0x80) == 0 && 
                        mmu.hdma_transfer_mode == 0x01 &&
                        mmu.hdma_to_transfer)
                    {
                        mmu.hdma_to_transfer = 0x00;
                        mmu.hdma_transfer_mode = 0x00;

                        return; 
                    } 

                    /* general (0) or hblank (1) ? */
                    mmu.hdma_transfer_mode = ((v & 0x80) ? 1 : 0);

                    /* calc how many bytes gotta be transferred */
                    uint16_t to_transfer = ((v & 0x7f) + 1) * 0x10;

                    /* general must be done immediately */
                    if (mmu.hdma_transfer_mode == 0)
                    {
                        /* copy right now */
                        if (mmu.vram_idx)
                            memcpy(mmu_addr_vram1() + 
                                   (mmu.hdma_dst_address - 0x8000), 
                                   &mmu.memory[mmu.hdma_src_address], 
                                   to_transfer);
                        else
                            memcpy(mmu_addr_vram0() + 
                                   (mmu.hdma_dst_address - 0x8000), 
                                   &mmu.memory[mmu.hdma_src_address], 
                                   to_transfer);

                        /* reset to_transfer var */
                        mmu.hdma_to_transfer = 0;

                        /* move forward src and dst addresses =| */
                        mmu.hdma_src_address += to_transfer;
                        mmu.hdma_dst_address += to_transfer;
                    }
                    else
                    {
                        mmu.hdma_to_transfer = to_transfer;

                        /* check if we're already into hblank phase */
                        cycles_hdma();
                    }
 
                    break;
            }
        }

        /* finally set memory byte with data */
        mmu.memory[a] = v;

        /* DMA access */
        if (a == 0xFF46)
        {
            /* calc source address */ 
            mmu.dma_address = v * 256;

            /* initialize counter, DMA needs 672 ticks */
            mmu.dma_next = cycles.cnt + 4; // 168 / 2;
        }
    }
    else
        mmu.memory[a] = v; 
}

/* write 16 bit block on a memory address */
void mmu_write_16(uint16_t a, uint16_t v)
{
    mmu.memory[a] = (uint8_t) (v & 0x00ff);
    mmu.memory[a + 1] = (uint8_t) (v >> 8);

    /* 16 bit write = +8 cycles */
    cycles_step();
    cycles_step();
}


/* write 16 bit block on a memory address (no cycles affected) */
void mmu_write_no_cyc(uint16_t a, uint8_t v)
{
    mmu.memory[a] = v;
}





