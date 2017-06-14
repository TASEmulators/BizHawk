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

#ifndef __CYCLES_HDR__
#define __CYCLES_HDR__

#include <stdint.h>
#include <stdio.h>

typedef struct cycles_s
{
    /* am i init'ed? */
    uint_fast32_t          inited;
    
    /* ticks counter */
    uint_fast32_t          cnt;

    /* CPU clock */
    uint_fast32_t          clock;

    /* handy for calculation */
    uint_fast32_t          next;

    /* step varying on cpu and emulation speed */
    uint_fast32_t          step;

    /* total running seconds */
    uint_fast32_t          seconds;

    /* 2 spares */
    uint_fast32_t          hs_next;


    uint_fast32_t          spare2;

} cycles_t;

extern cycles_t cycles;

// extern uint8_t  cycles_hs_local_cnt;
// extern uint8_t  cycles_hs_peer_cnt;

/* callback function */
typedef void (*cycles_send_cb_t) (uint32_t v);

/* prototypes */
void cycles_change_emulation_speed();
void cycles_hdma();
char cycles_init();
void cycles_set_speed(char dbl);
void cycles_start_hs();
void cycles_step();
void cycles_stop_hs();
void cycles_term();
void cycles_vblank();

#endif
