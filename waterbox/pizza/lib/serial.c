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
#include "serial.h"
#include "utils.h"

/* main variable */
serial_t serial;

/* function to call when frame is ready */
serial_data_send_cb_t serial_data_send_cb;

interrupts_flags_t *serial_if;

/* second message before the first was handled? */
uint8_t serial_second_set = 0;
uint8_t serial_second_data = 0;
uint8_t serial_second_clock = 0;
uint8_t serial_second_transfer_start = 0;
uint8_t serial_waiting_data = 0;

void serial_verify_intr()
{
    if (serial.data_recv && serial.data_sent)
    {
        serial.data_recv = 0;
        serial.data_sent = 0;

        /* valid couple of messages for a serial interrupt? */
        if ((serial.data_recv_clock != serial.data_sent_clock) &&
            serial.data_recv_transfer_start &&  
            serial.data_sent_transfer_start)
        {
            /* put received data into 0xFF01 (serial.data) */
            /* and notify with an interrupt                */
            serial.transfer_start = 0;
            serial.data = serial.data_to_recv;

            serial_if->serial_io = 1;
        }

        /* a message is already on queue? */
        if (serial_second_set)
        {
            serial_second_set = 0;
            serial.data_recv = 1;
            serial.data_to_recv = serial_second_data;
            serial.data_recv_clock = serial_second_clock;
            serial.data_recv_transfer_start = serial_second_transfer_start;
        }
    }
}

void serial_init()
{
    /* pointer to interrupt flags */
    serial_if = mmu_addr(0xFF0F);

    /* init counters */
    serial.bits_sent = 0;

    /* start as not connected */
    serial.peer_connected = 0;
}

void serial_write_reg(uint16_t a, uint8_t v)
{
    switch (a)
    {
    case 0xFF01: 
        serial.data = v;
        return;
    case 0xFF02: 
        serial.clock = v & 0x01; 
        serial.speed = (v & 0x02) ? 0x01 : 0x00;
        serial.spare = ((v >> 2) & 0x1F);
        serial.transfer_start = (v & 0x80) ? 0x01 : 0x00;

        /* reset? */
        serial.data_sent = 0;
        break;
    }

    if (serial.transfer_start && 
        !serial.peer_connected &&
        serial.clock)
    {
        if (serial.speed)
            serial.next = cycles.cnt + 8 * 8;
	    else
            serial.next = cycles.cnt + 256 * 8;
    }
}

uint8_t serial_read_reg(uint16_t a)
{
    uint8_t v = 0xFF;

    switch (a)
    {
        case 0xFF01: v = serial.data; break;
        case 0xFF02: v = ((serial.clock) ? 0x01 : 0x00) |
                          ((serial.speed) ? 0x02 : 0x00) |
                          (serial.spare << 2)            |
                          ((serial.transfer_start) ? 0x80 : 0x00); 
    }

    return v;
}

void serial_recv_byte(uint8_t v, uint8_t clock, uint8_t transfer_start)
{
    /* second message during same span time? */
    if (serial.data_recv)
    {
        /* store it. handle it later */
        serial_second_set = 1;
        serial_second_data = v;
        serial_second_clock = clock;
        serial_second_transfer_start = transfer_start;
        return;
    }

    /* received side OK */
    serial.data_recv = 1;
    serial.data_recv_clock = clock;
    serial.data_to_recv = v;
    serial.data_recv_transfer_start = transfer_start;

    /* notify main thread in case it's waiting */
    //if (serial_waiting_data)
        //pthread_cond_signal(&serial_cond);
}

void serial_send_byte()
{
    serial.data_sent = 1;
    serial.data_to_send = serial.data; 
    serial.data_sent_clock = serial.clock; 
    serial.data_sent_transfer_start = serial.transfer_start; 

    if (serial_data_send_cb)
        (*serial_data_send_cb) (serial.data, serial.clock, 
                                serial.transfer_start);
}

void serial_set_send_cb(serial_data_send_cb_t cb)
{
    serial_data_send_cb = cb;
}

void serial_wait_data()
{
    if (serial.data_sent && serial.data_recv == 0)
    {
        /* wait max 3 seconds */
        //struct timespec wait;

        //wait.tv_sec = time(NULL) + 3;

        /* this is very important to avoid EINVAL return! */
        //wait.tv_nsec = 0;

        /* declare i'm waiting for data */
        //serial_waiting_data = 1;

        /* notify something has arrived */
        // pthread_cond_timedwait(&serial_cond, &serial_mutex, &wait);

        /* not waiting anymore */
        //serial_waiting_data = 0;
    }
}
