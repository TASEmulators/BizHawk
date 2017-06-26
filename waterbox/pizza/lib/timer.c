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
#include "interrupt.h"
#include "mmu.h"
#include "timer.h"

/* pointer to interrupt flags (handy) */
interrupts_flags_t *timer_if;


void timer_init()
{
    /* reset values */
    timer.next = 256;
    timer.sub = 0;
	
    /* pointer to interrupt flags */
    timer_if   = mmu_addr(0xFF0F);
}

void timer_write_reg(uint16_t a, uint8_t v)
{
    switch (a)
    {
        case 0xFF04: timer.div = 0; return;
        case 0xFF05: timer.cnt = v; return;
        case 0xFF06: timer.mod = v; return;
        case 0xFF07: timer.ctrl = v; 
    }

    if (timer.ctrl & 0x04)
        timer.active = 1;
    else
        timer.active = 0;
        
    switch (timer.ctrl & 0x03)
    {
        case 0x00: timer.threshold = 1024; break;
        case 0x01: timer.threshold = 16; break;
        case 0x02: timer.threshold = 64; break;
        case 0x03: timer.threshold = 256; break;
    }

    if (timer.active)
        timer.sub_next = cycles.cnt + timer.threshold;
}

uint8_t timer_read_reg(uint16_t a)
{
    switch (a)
    {
        case 0xFF04: return timer.div;
        case 0xFF05: return timer.cnt;
        case 0xFF06: return timer.mod;
        case 0xFF07: return timer.ctrl;
    }

    return 0xFF;
}


