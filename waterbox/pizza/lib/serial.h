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

#ifndef __SERIAL_HDR__
#define __SERIAL_HDR__

#include <stdint.h>
#include <stdio.h>

typedef struct serial_ctrl_s
{ 
    uint8_t clock;
    uint8_t speed;
    uint8_t spare;
    uint8_t transfer_start;
} serial_ctrl_t;

typedef struct serial_s {

    /* pointer to serial controller register */
    // serial_ctrl_t  ctrl;

    uint8_t clock;
    uint8_t speed;
    uint8_t spare;
    uint8_t transfer_start;

    /* pointer to FF01 data */
    uint8_t  data;

    /* sent bits */
    uint8_t  bits_sent;

    /* data to send */
    uint8_t  data_to_send;

    /* peer clock */
    uint8_t  data_to_recv;

    /* counter */
    uint64_t next;

    /* peer connected? */
    uint8_t  peer_connected:1;
    uint8_t  data_sent:1;
    uint8_t  data_sent_clock:1;
    uint8_t  data_sent_transfer_start:1;
    uint8_t  data_recv:1;
    uint8_t  data_recv_clock:1;
    uint8_t  data_recv_transfer_start:1;
    uint8_t  spare10:1;

    uint_fast32_t  last_send_cnt;

} serial_t;

extern serial_t serial;

/* callback when receive something on serial */
typedef void (*serial_data_send_cb_t) (uint8_t v, uint8_t clock, 
                                       uint8_t transfer_start);

/* prototypes */
void    serial_init();
void    serial_lock();
void    serial_write_reg(uint16_t a, uint8_t v);
void    serial_verify_intr();
uint8_t serial_read_reg(uint16_t a);
void    serial_recv_byte(uint8_t v, uint8_t clock, uint8_t transfer_start);
void    serial_recv_clock();
void    serial_send_byte();
void    serial_set_send_cb(serial_data_send_cb_t cb);
void    serial_unlock();
void    serial_wait_data();

#endif
