/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef IO_H_
#define IO_H_
#include <stdint.h>
#include "tern.h"
#include "romdb.h"
#include "serialize.h"

enum {
	IO_NONE,
	IO_GAMEPAD2,
	IO_GAMEPAD3,
	IO_GAMEPAD6,
	IO_MOUSE,
	IO_SATURN_KEYBOARD,
	IO_XBAND_KEYBOARD,
	IO_MENACER,
	IO_JUSTIFIER,
	IO_SEGA_MULTI,
	IO_EA_MULTI_A,
	IO_EA_MULTI_B,
	IO_SEGA_PARALLEL,
	IO_GENERIC,
	IO_GENERIC_SERIAL,
	IO_HEARTBEAT_TRAINER
};

typedef struct {
	union {
		struct {
			uint32_t timeout_cycle;
			uint16_t th_counter;
			uint16_t gamepad_num;
		} pad;
		struct {
			int data_fd;
			int listen_fd;
		} stream;
		struct {
			uint32_t ready_cycle;
			uint16_t last_read_x;
			uint16_t last_read_y;
			uint16_t cur_x;
			uint16_t cur_y;
			uint16_t latched_x;
			uint16_t latched_y;
			uint8_t  tr_counter;
			uint8_t  mouse_num;
		} mouse;
		struct {
			uint16_t events[8];
			uint8_t  read_pos;
			uint8_t  write_pos;
			uint8_t  tr_counter;
			uint8_t  mode;
			uint8_t  cmd;
		} keyboard;
		struct {
			uint8_t  *nv_memory;
			uint8_t  *cur_buffer;
			uint64_t rtc_base_timestamp;
			uint8_t  rtc_base[5];
			uint8_t  bpm;
			uint8_t  cadence;
			uint8_t  buttons;
			uint8_t  nv_page_size;
			uint8_t  nv_pages;
			uint8_t  param;
			uint8_t  state;
			uint8_t  status;
			uint8_t  device_num;
			uint8_t  cmd;
			uint8_t  remaining_bytes;
		} heartbeat_trainer;
	} device;
	uint8_t  output;
	uint8_t  control;
	uint8_t  input[3];
	uint32_t slow_rise_start[8];
	uint32_t serial_cycle;
	uint32_t serial_divider;
	uint32_t last_poll_cycle;
	uint32_t transmit_end;
	uint32_t receive_end;
	uint8_t  serial_out;
	uint8_t  serial_transmitting;
	uint8_t  serial_in;
	uint8_t  serial_receiving;
	uint8_t  serial_ctrl;
	uint8_t  device_type;
} io_port;

typedef struct {
	io_port	ports[3];
} sega_io;

//pseudo gamepad for buttons on main console unit
#define GAMEPAD_MAIN_UNIT 255

enum {
	BUTTON_INVALID,
	DPAD_UP,
	DPAD_DOWN,
	DPAD_LEFT,
	DPAD_RIGHT,
	BUTTON_A,
	BUTTON_B,
	BUTTON_C,
	BUTTON_START,
	BUTTON_X,
	BUTTON_Y,
	BUTTON_Z,
	BUTTON_MODE,
	NUM_GAMEPAD_BUTTONS
};

enum {
	MAIN_UNIT_PAUSE
};

enum {
	MOUSE_LEFT = 1,
	MOUSE_RIGHT = 2,
	MOUSE_MIDDLE = 4,
	MOUSE_START = 8,
	PSEUDO_BUTTON_MOTION=0xFF
};

void setup_io_devices(tern_node * config, rom_info *rom, sega_io *io);
void io_adjust_cycles(io_port * pad, uint32_t current_cycle, uint32_t deduction);
void io_run(io_port *port, uint32_t current_cycle);
void io_control_write(io_port *port, uint8_t value, uint32_t current_cycle);
void io_data_write(io_port * pad, uint8_t value, uint32_t current_cycle);
void io_tx_write(io_port *port, uint8_t value, uint32_t current_cycle);
void io_sctrl_write(io_port *port, uint8_t value, uint32_t current_cycle);
uint8_t io_data_read(io_port * pad, uint32_t current_cycle);
uint8_t io_rx_read(io_port * port, uint32_t current_cycle);
uint8_t io_sctrl_read(io_port *port, uint32_t current_cycle);
uint32_t io_next_interrupt(io_port *port, uint32_t current_cycle);
void io_serialize(io_port *port, serialize_buffer *buf);
void io_deserialize(deserialize_buffer *buf, void *vport);

void io_port_gamepad_down(io_port *port, uint8_t button);
void io_port_gamepad_up(io_port *port, uint8_t button);
void io_gamepad_down(sega_io *io, uint8_t gamepad_num, uint8_t button);
void io_gamepad_up(sega_io *io, uint8_t gamepad_num, uint8_t button);
void io_mouse_down(sega_io *io, uint8_t mouse_num, uint8_t button);
void io_mouse_up(sega_io *io, uint8_t mouse_num, uint8_t button);
void io_mouse_motion_absolute(sega_io *io, uint8_t mouse_num, uint16_t x, uint16_t y);
void io_mouse_motion_relative(sega_io *io, uint8_t mouse_num, int32_t x, int32_t y);
void io_keyboard_down(sega_io *io, uint8_t scancode);
void io_keyboard_up(sega_io *io, uint8_t scancode);
uint8_t io_has_keyboard(sega_io *io);

extern const char * device_type_names[];

#endif //IO_H_

