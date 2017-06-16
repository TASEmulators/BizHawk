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

#include <string.h>
#include <time.h>

#include "cycles.h"
#include "global.h"
#include "gpu.h"
#include "mmu.h"
#include "serial.h"
#include "sound.h"
#include "timer.h"
#include "interrupt.h"
#include "utils.h"

interrupts_flags_t *cycles_if;

/* instance of the main struct */
cycles_t cycles = { 0, 0, 0, 0 };

#define CYCLES_PAUSES 256

/* hard sync stuff (for remote connection) */
uint8_t  cycles_hs_mode = 0;

/* type of next */
typedef enum 
{
    CYCLES_NEXT_TYPE_CYCLES,
    CYCLES_NEXT_TYPE_CYCLES_HS,
    CYCLES_NEXT_TYPE_DMA,
} cycles_next_type_enum_e;

/* closest next and its type */
uint_fast32_t            cycles_very_next;
cycles_next_type_enum_e  cycles_next_type;

/* set hard sync mode. sync is given by the remote peer + local timer */
void cycles_start_hs()
{
    utils_log("Hard sync mode ON\n");

    /* boolean set to on */
    cycles_hs_mode = 1;
}

void cycles_stop_hs()
{
    utils_log("Hard sync mode OFF\n");

    /* boolean set to on */
    cycles_hs_mode = 0;
}

/* set double or normal speed */
void cycles_set_speed(char dbl)
{
    /* set global */
    global_cpu_double_speed = dbl;

    /* update clock */
    if (global_cpu_double_speed)
        cycles.clock = 4194304 * 2;
    else
        cycles.clock = 4194304;

    /* calculate the mask */
    cycles_change_emulation_speed();
}

/* set emulation speed */
void cycles_change_emulation_speed()
{
            cycles.step = ((4194304 / CYCLES_PAUSES) 
                          << global_cpu_double_speed);
}

void cycles_closest_next()
{
    int_fast32_t diff = cycles.cnt - cycles.next;

    /* init */
    cycles_very_next  = cycles.next; 
    cycles_next_type  = CYCLES_NEXT_TYPE_CYCLES;

    int_fast32_t diff_new = cycles.cnt - mmu.dma_next;

    /* DMA? */
    if (diff_new < diff)
    {
        /* this is the new lowest */
        cycles_very_next = mmu.dma_next;
        cycles_next_type = CYCLES_NEXT_TYPE_DMA; 
    }
}

/* this function is gonna be called every M-cycle = 4 ticks of CPU */
void cycles_step()
{
    cycles.cnt += 4;
	cycles.sampleclock += 2 >> global_cpu_double_speed;

/*
    while (cycles.cnt >= cycles_very_next)
    {
        switch (cycles_next_type)
        {
            case CYCLES_NEXT_TYPE_CYCLES:

                deadline.tv_nsec += 1000000000 / CYCLES_PAUSES;

                if (deadline.tv_nsec > 1000000000)
                {
                    deadline.tv_sec += 1;
                    deadline.tv_nsec -= 1000000000;
                }

                clock_nanosleep(CLOCK_MONOTONIC, TIMER_ABSTIME, 
                                &deadline, NULL);

                cycles.next += cycles.step;

                if (cycles.cnt % cycles.clock == 0)
                    cycles.seconds++;

                break;

            case CYCLES_NEXT_TYPE_DMA:

                memcpy(&mmu.memory[0xFE00], &mmu.memory[mmu.dma_address], 160);

                mmu.dma_address = 0x0000;

                mmu.dma_next = 1;

                break;
        }

        cycles_closest_next();
    }
*/

    /* 65536 == cpu clock / CYCLES_PAUSES pauses every second */
    if (cycles.cnt == cycles.next) 
    {
        cycles.next += cycles.step;

        /* update current running seconds */
        if (cycles.cnt % cycles.clock == 0)
            cycles.seconds++;
    }

    /* hard sync next step */
    if (cycles.cnt == cycles.hs_next)
    {
        /* set cycles for hard sync */
        cycles.hs_next += ((4096 * 4) << global_cpu_double_speed);

        /* hard sync is on? */
        if (cycles_hs_mode)
        {
            /* send my status and wait for peer status back */
            serial_send_byte();

            /* wait for reply */
            serial_wait_data();

            /* verify if we need to trigger an interrupt */
            serial_verify_intr();
        }
    }

    /* DMA */
    if (mmu.dma_next == cycles.cnt)
    {
        memcpy(&mmu.memory[0xFE00], &mmu.memory[mmu.dma_address], 160);

        /* reset address */
        mmu.dma_address = 0x0000;

        /* reset */
        mmu.dma_next = 1;
    }

    /* update GPU state */
    if (gpu.next == cycles.cnt)
        gpu_step();

    /* fs clock */
    if (sound.fs_cycles_next == cycles.cnt)
        sound_step_fs();
        
    /* channel one */
    if (sound.channel_one.duty_cycles_next == cycles.cnt)
        sound_step_ch1();

    /* channel two */
    if (sound.channel_two.duty_cycles_next == cycles.cnt)
        sound_step_ch2();
        
    /* channel three */
    if (sound.channel_three.cycles_next <= cycles.cnt)
        sound_step_ch3();        
        
    /* channel four */
    if (sound.channel_four.cycles_next == cycles.cnt)
        sound_step_ch4();

    /* update timer state */
    if (cycles.cnt == timer.next)
    {
        timer.next += 256;
        timer.div++;
    }

    /* timer is on? */
    if (timer.sub_next == cycles.cnt)
    {
        timer.sub_next += timer.threshold;
        timer.cnt++;
            
        /* cnt value > 255? trigger an interrupt */
        if (timer.cnt > 255)
        {
            timer.cnt = timer.mod;

            /* trigger timer interrupt */
            cycles_if->timer = 1;
        }
    }

    /* update serial state */
    if (serial.next == cycles.cnt)
    {
        /* nullize serial next */
        serial.next -= 1;

        /* reset counter */
        serial.bits_sent = 0;

        /* gotta reply with 0xff when asking for ff01 */
        serial.data = 0xFF;

        /* reset transfer_start flag to yell I'M DONE */
        serial.transfer_start = 0;
 
        /* if not connected, trig the fucking interrupt */
        cycles_if->serial_io = 1;
    }    
}

/* things to do when vsync kicks in */
void cycles_vblank()
{
    return;

}

/* stuff tied to entering into hblank state */
void cycles_hdma()
{
    /* HDMA (only CGB) */
    if (mmu.hdma_to_transfer)
    {
        /* hblank transfer */
        if (mmu.hdma_transfer_mode)
        {
            /* transfer when line is changed and we're into HBLANK phase */
            if (mmu.memory[0xFF44] < 143 &&
                mmu.hdma_current_line != mmu.memory[0xFF44] &&
               (mmu.memory[0xFF41] & 0x03) == 0x00)
            {
                /* update current line */
                mmu.hdma_current_line = mmu.memory[0xFF44];

                /* copy 0x10 bytes */
                if (mmu.vram_idx)
                    memcpy(mmu_addr_vram1() + mmu.hdma_dst_address - 0x8000,
                           &mmu.memory[mmu.hdma_src_address], 0x10);
                else
                    memcpy(mmu_addr_vram0() + mmu.hdma_dst_address - 0x8000,
                           &mmu.memory[mmu.hdma_src_address], 0x10);

                /* decrease bytes to transfer */
                mmu.hdma_to_transfer -= 0x10;

                /* increase pointers */
                mmu.hdma_dst_address += 0x10;
                mmu.hdma_src_address += 0x10;
            }
        }
    }
}

char cycles_init()
{
    cycles.inited = 1;

    /* interrupt registers */
    cycles_if = mmu_addr(0xFF0F);

    /* init clock and counter */
    cycles.clock = 4194304;
    cycles.cnt = 0;
    cycles.hs_next = 70224;

    /* mask for pauses cycles fast calc */
    cycles.step = 4194304 / CYCLES_PAUSES;
    cycles.next = 4194304 / CYCLES_PAUSES;

    return 0;
}
