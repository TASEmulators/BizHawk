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

#ifndef __TIMER_HDR__
#define __TIMER_HDR__

#include <stdint.h>

/* timer status */
typedef struct timer_gb_s
{
    /* is it active? */
    uint8_t active;

    /* divider - 0xFF04 */
    uint8_t div;

    /* modulo  - 0xFF06 */
    uint8_t mod;

    /* control - 0xFF07 */
    uint8_t ctrl;
    
    /* counter - 0xFF05 */
    uint_fast32_t cnt;
    
    /* threshold */
    uint32_t threshold;

    /* current value    */
    uint_fast32_t sub;
    uint64_t next;

    /* spare */
    uint_fast32_t sub_next;
} timer_gb_t;

/* global status of timer */
timer_gb_t timer;

/* prototypes */
void    timer_init();
void    timer_step();
void    timer_write_reg(uint16_t a, uint8_t v);
uint8_t timer_read_reg(uint16_t a);

#endif
