/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef _WIN32
#include <unistd.h>
#include <fcntl.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#endif
#include <string.h>
#include <stdlib.h>

#include "serialize.h"
#include "io.h"
#include "blastem.h"
#include "render.h"
#include "util.h"
#include "bindings.h"

#define CYCLE_NEVER 0xFFFFFFFF
#define MIN_POLL_INTERVAL 6840

const char * device_type_names[] = {
	"None",
	"SMS gamepad",
	"3-button gamepad",
	"6-button gamepad",
	"Mega Mouse",
	"Saturn Keyboard",
	"XBAND Keyboard",
	"Menacer",
	"Justifier",
	"Sega multi-tap",
	"EA 4-way Play cable A",
	"EA 4-way Play cable B",
	"Sega Parallel Transfer Board",
	"Generic Device",
	"Generic Serial",
	"Heartbeat Personal Trainer"
};

#define GAMEPAD_TH0 0
#define GAMEPAD_TH1 1
#define GAMEPAD_EXTRA 2
#define GAMEPAD_NONE 0xF

#define IO_TH0 0
#define IO_TH1 1
#define IO_STATE 2

enum {
	IO_WRITE_PENDING,
	IO_WRITTEN,
	IO_READ_PENDING,
	IO_READ
};

enum {
	HBPT_NEED_INIT,
	HBPT_IDLE,
	HBPT_CMD_PAYLOAD,
	HBPT_REPLY
};

typedef struct {
	uint8_t states[2], value;
} gp_button_def;


static gp_button_def button_defs[NUM_GAMEPAD_BUTTONS] = {
	[DPAD_UP] = {.states = {GAMEPAD_TH0, GAMEPAD_TH1}, .value = 0x1},
	[DPAD_DOWN] = {.states = {GAMEPAD_TH0, GAMEPAD_TH1}, .value = 0x2},
	[DPAD_LEFT] = {.states = {GAMEPAD_TH1, GAMEPAD_NONE}, .value = 0x4},
	[DPAD_RIGHT] = {.states = {GAMEPAD_TH1, GAMEPAD_NONE}, .value = 0x8},
	[BUTTON_A] = {.states = {GAMEPAD_TH0, GAMEPAD_NONE}, .value = 0x10},
	[BUTTON_B] = {.states = {GAMEPAD_TH1, GAMEPAD_NONE}, .value = 0x10},
	[BUTTON_C] = {.states = {GAMEPAD_TH1, GAMEPAD_NONE}, .value = 0x20},
	[BUTTON_START] = {.states = {GAMEPAD_TH0, GAMEPAD_NONE}, .value = 0x20},
	[BUTTON_X] = {.states = {GAMEPAD_EXTRA, GAMEPAD_NONE}, .value = 0x4},
	[BUTTON_Y] = {.states = {GAMEPAD_EXTRA, GAMEPAD_NONE}, .value = 0x2},
	[BUTTON_Z] = {.states = {GAMEPAD_EXTRA, GAMEPAD_NONE}, .value = 0x1},
	[BUTTON_MODE] = {.states = {GAMEPAD_EXTRA, GAMEPAD_NONE}, .value = 0x8},
};

static io_port *find_gamepad(sega_io *io, uint8_t gamepad_num)
{
	for (int i = 0; i < 3; i++)
	{
		io_port *port = io->ports + i;
		if (port->device_type < IO_MOUSE && port->device.pad.gamepad_num == gamepad_num) {
			return port;
		}
		if (port->device_type == IO_HEARTBEAT_TRAINER && port->device.heartbeat_trainer.device_num == gamepad_num) {
			return port;
		} 
	}
	return NULL;
}

static io_port *find_mouse(sega_io *io, uint8_t mouse_num)
{
	for (int i = 0; i < 3; i++)
	{
		io_port *port = io->ports + i;
		if (port->device_type == IO_MOUSE && port->device.mouse.mouse_num == mouse_num) {
			return port;
		}
	}
	return NULL;
}

static io_port *find_keyboard(sega_io *io)
{
	for (int i = 0; i < 3; i++)
	{
		io_port *port = io->ports + i;
		if (port->device_type == IO_SATURN_KEYBOARD || port->device_type == IO_XBAND_KEYBOARD) {
			return port;
		}
	}
	return NULL;
}

void io_port_gamepad_down(io_port *port, uint8_t button)
{
	gp_button_def *def = button_defs + button;
	port->input[def->states[0]] |= def->value;
	if (def->states[1] != GAMEPAD_NONE) {
		port->input[def->states[1]] |= def->value;
	}
}

void io_port_gamepad_up(io_port *port, uint8_t button)
{
	gp_button_def *def = button_defs + button;
	port->input[def->states[0]] &= ~def->value;
	if (def->states[1] != GAMEPAD_NONE) {
		port->input[def->states[1]] &= ~def->value;
	}
}

void io_gamepad_down(sega_io *io, uint8_t gamepad_num, uint8_t button)
{
	io_port *port = find_gamepad(io, gamepad_num);
	if (port) {
		io_port_gamepad_down(port, button);
	}
}

void io_gamepad_up(sega_io *io, uint8_t gamepad_num, uint8_t button)
{
	io_port *port = find_gamepad(io, gamepad_num);
	if (port) {
		io_port_gamepad_up(port, button);
	}
}

void io_mouse_down(sega_io *io, uint8_t mouse_num, uint8_t button)
{
	io_port *port = find_mouse(io, mouse_num);
	if (port) {
		port->input[0] |= button;
	}
}

void io_mouse_up(sega_io *io, uint8_t mouse_num, uint8_t button)
{
	io_port *port = find_mouse(io, mouse_num);
	if (port) {
		port->input[0] &= ~button;
	}
}

void io_mouse_motion_absolute(sega_io *io, uint8_t mouse_num, uint16_t x, uint16_t y)
{
	io_port *port = find_mouse(io, mouse_num);
	if (port) {
		port->device.mouse.cur_x = x;
		port->device.mouse.cur_y = y;
	}
}

void io_mouse_motion_relative(sega_io *io, uint8_t mouse_num, int32_t x, int32_t y)
{
	io_port *port = find_mouse(io, mouse_num);
	if (port) {
		port->device.mouse.cur_x += x;
		port->device.mouse.cur_y += y;
	}
}

void store_key_event(io_port *keyboard_port, uint16_t code)
{
	if (keyboard_port && keyboard_port->device.keyboard.write_pos != keyboard_port->device.keyboard.read_pos) {
		//there's room in the buffer, record this event
		keyboard_port->device.keyboard.events[keyboard_port->device.keyboard.write_pos] = code;
		if (keyboard_port->device.keyboard.read_pos == 0xFF) {
			//ring buffer was empty, update read_pos to indicate there is now data
			keyboard_port->device.keyboard.read_pos = keyboard_port->device.keyboard.write_pos;
		}
		keyboard_port->device.keyboard.write_pos = (keyboard_port->device.keyboard.write_pos + 1) & 7;
	}
}

void io_keyboard_down(sega_io *io, uint8_t scancode)
{
	store_key_event(find_keyboard(io), scancode);
}

void io_keyboard_up(sega_io *io, uint8_t scancode)
{
	store_key_event(find_keyboard(io), 0xF000 | scancode);
}

uint8_t io_has_keyboard(sega_io *io)
{
	return find_keyboard(io) != NULL;
}

static void set_serial_clock(io_port *port)
{
	switch(port->serial_ctrl >> 6)
	{
	case 0: port->serial_divider = 11186; break; //4800 bps
	case 1: port->serial_divider = 22372; break; //2400 bps
	case 2: port->serial_divider = 44744; break; //1200 bps
	case 3: port->serial_divider = 178976; break; //300 bps
	}
}

void process_device(char * device_type, io_port * port)
{
	set_serial_clock(port);
	//assuming that the io_port struct has been zeroed if this is the first time this has been called
	if (!device_type)
	{
		return;
	}

	const int gamepad_len = strlen("gamepad");
	if (startswith(device_type, "gamepad"))
	{
		if (
			(device_type[gamepad_len] != '3' && device_type[gamepad_len] != '6' && device_type[gamepad_len] != '2')
			|| device_type[gamepad_len+1] != '.' || device_type[gamepad_len+2] < '1'
			|| device_type[gamepad_len+2] > '8' || device_type[gamepad_len+3] != 0
		) {
			warning("%s is not a valid gamepad type\n", device_type);
		} else if (device_type[gamepad_len] == '3') {
			port->device_type = IO_GAMEPAD3;
		} else if (device_type[gamepad_len] == '2') {
			port->device_type = IO_GAMEPAD2;
		} else {
			port->device_type = IO_GAMEPAD6;
		}
		port->device.pad.gamepad_num = device_type[gamepad_len+2] - '0';
	} else if(startswith(device_type, "heartbeat_trainer.")) {
		port->device_type = IO_HEARTBEAT_TRAINER;
		port->device.heartbeat_trainer.nv_memory = NULL;
		port->device.heartbeat_trainer.device_num = device_type[strlen("heartbeat_trainer.")] - '0';
	} else if(startswith(device_type, "mouse")) {
		if (port->device_type != IO_MOUSE) {
			port->device_type = IO_MOUSE;
			port->device.mouse.mouse_num = device_type[strlen("mouse")+1] - '0';
			port->device.mouse.last_read_x = 0;
			port->device.mouse.last_read_y = 0;
			port->device.mouse.cur_x = 0;
			port->device.mouse.cur_y = 0;
			port->device.mouse.latched_x = 0;
			port->device.mouse.latched_y = 0;
			port->device.mouse.ready_cycle = CYCLE_NEVER;
			port->device.mouse.tr_counter = 0;
		}
	} else if(!strcmp(device_type, "saturn keyboard")) {
		if (port->device_type != IO_SATURN_KEYBOARD) {
			port->device_type = IO_SATURN_KEYBOARD;
			port->device.keyboard.read_pos = 0xFF;
			port->device.keyboard.write_pos = 0;
		}
	} else if(!strcmp(device_type, "xband keyboard")) {
		if (port->device_type != IO_XBAND_KEYBOARD) {
			port->device_type = IO_XBAND_KEYBOARD;
			port->device.keyboard.read_pos = 0xFF;
			port->device.keyboard.write_pos = 0;
		}
	} else if(!strcmp(device_type, "sega_parallel")) {
		if (port->device_type != IO_SEGA_PARALLEL) {
			port->device_type = IO_SEGA_PARALLEL;
			port->device.stream.data_fd = -1;
			port->device.stream.listen_fd = -1;
		}
	} else if(!strcmp(device_type, "generic")) {
		if (port->device_type != IO_GENERIC) {
			port->device_type = IO_GENERIC;
			port->device.stream.data_fd = -1;
			port->device.stream.listen_fd = -1;
		}
	} else if(!strcmp(device_type, "serial")) {
		if (port->device_type != IO_GENERIC_SERIAL) {
			port->device_type = IO_GENERIC_SERIAL;
			port->device.stream.data_fd = -1;
			port->device.stream.listen_fd = -1;
		}
	}
}

char * io_name(int i)
{
	switch (i)
	{
	case 0:
		return "1";
	case 1:
		return "2";
	case 2:
		return "EXT";
	default:
		return "invalid";
	}
}

static char * sockfile_name;
static void cleanup_sockfile()
{
	unlink(sockfile_name);
}

void setup_io_devices(tern_node * config, rom_info *rom, sega_io *io)
{
	io_port * ports = io->ports;
	tern_node *io_nodes = tern_find_path(config, "io\0devices\0", TVAL_NODE).ptrval;
	char * io_1 = rom->port1_override ? rom->port1_override : tern_find_ptr_default(io_nodes, "1", "gamepad6.1");
	char * io_2 = rom->port2_override ? rom->port2_override : tern_find_ptr_default(io_nodes, "2", "gamepad6.2");
	char * io_ext = rom->ext_override ? rom->ext_override : tern_find_ptr(io_nodes, "ext");

	process_device(io_1, ports);
	process_device(io_2, ports+1);
	process_device(io_ext, ports+2);

	uint8_t mouse_mode;
	if (ports[0].device_type == IO_MOUSE || ports[1].device_type == IO_MOUSE || ports[2].device_type == IO_MOUSE) {
		if (render_fullscreen()) {
				mouse_mode = MOUSE_RELATIVE;
		} else {
			if (rom->mouse_mode && !strcmp(rom->mouse_mode, "absolute")) {
				mouse_mode = MOUSE_ABSOLUTE;
			} else {
				mouse_mode = MOUSE_CAPTURE;
			}
		}
	} else {
		mouse_mode = MOUSE_NONE;
	}
	bindings_set_mouse_mode(mouse_mode);

	for (int i = 0; i < 3; i++)
	{
#ifndef _WIN32
		if (ports[i].device_type == IO_SEGA_PARALLEL && ports[i].device.stream.data_fd == -1)
		{
			char *pipe_name = tern_find_path(config, "io\0parallel_pipe\0", TVAL_PTR).ptrval;
			if (!pipe_name)
			{
				warning("IO port %s is configured to use the sega parallel board, but no paralell_pipe is set!\n", io_name(i));
				ports[i].device_type = IO_NONE;
			} else {
				debug_message("IO port: %s connected to device '%s' with pipe name: %s\n", io_name(i), device_type_names[ports[i].device_type], pipe_name);
				if (!strcmp("stdin", pipe_name))
				{
					ports[i].device.stream.data_fd = STDIN_FILENO;
				} else {
					if (mkfifo(pipe_name, 0666) && errno != EEXIST)
					{
						warning("Failed to create fifo %s for Sega parallel board emulation: %d %s\n", pipe_name, errno, strerror(errno));
						ports[i].device_type = IO_NONE;
					} else {
						ports[i].device.stream.data_fd = open(pipe_name, O_NONBLOCK | O_RDONLY);
						if (ports[i].device.stream.data_fd == -1)
						{
							warning("Failed to open fifo %s for Sega parallel board emulation: %d %s\n", pipe_name, errno, strerror(errno));
							ports[i].device_type = IO_NONE;
						}
					}
				}
			}
		} else if (ports[i].device_type == IO_GENERIC || ports[i].device_type == IO_GENERIC_SERIAL && ports[i].device.stream.data_fd == -1) {
			char *sock_name = tern_find_path(config, "io\0socket\0", TVAL_PTR).ptrval;
			if (!sock_name)
			{
				warning("IO port %s is configured to use generic IO, but no socket is set!\n", io_name(i));
				ports[i].device_type = IO_NONE;
			} else {
				debug_message("IO port: %s connected to device '%s' with socket name: %s\n", io_name(i), device_type_names[ports[i].device_type], sock_name);
				ports[i].device.stream.data_fd = -1;
				ports[i].device.stream.listen_fd = socket(AF_UNIX, SOCK_STREAM, 0);
				size_t pathlen = strlen(sock_name);
				size_t addrlen = offsetof(struct sockaddr_un, sun_path) + pathlen + 1;
				struct sockaddr_un *saddr = malloc(addrlen);
				saddr->sun_family = AF_UNIX;
				memcpy(saddr->sun_path, sock_name, pathlen+1);
				if (bind(ports[i].device.stream.listen_fd, (struct sockaddr *)saddr, addrlen))
				{
					warning("Failed to bind socket for IO Port %s to path %s: %d %s\n", io_name(i), sock_name, errno, strerror(errno));
					goto cleanup_sock;
				}
				if (listen(ports[i].device.stream.listen_fd, 1))
				{
					warning("Failed to listen on socket for IO Port %s: %d %s\n", io_name(i), errno, strerror(errno));
					goto cleanup_sockfile;
				}
				sockfile_name = sock_name;
				atexit(cleanup_sockfile);
				continue;
cleanup_sockfile:
				unlink(sock_name);
cleanup_sock:
				close(ports[i].device.stream.listen_fd);
				ports[i].device_type = IO_NONE;
			}
		} else
#endif
		if (ports[i].device_type == IO_GAMEPAD3 || ports[i].device_type == IO_GAMEPAD6 || ports[i].device_type == IO_GAMEPAD2) {
			debug_message("IO port %s connected to gamepad #%d with type '%s'\n", io_name(i), ports[i].device.pad.gamepad_num, device_type_names[ports[i].device_type]);
		} else if (ports[i].device_type == IO_HEARTBEAT_TRAINER) {
			debug_message("IO port %s connected to Heartbeat Personal Trainer #%d\n", io_name(i), ports[i].device.heartbeat_trainer.device_num);
			if (rom->save_type == SAVE_HBPT) {
				ports[i].device.heartbeat_trainer.nv_memory = rom->save_buffer;
				uint32_t page_size = 16;
				for (; page_size < 128; page_size *= 2)
				{
					if (rom->save_size / page_size < 256) {
						break;
					}
				}
				ports[i].device.heartbeat_trainer.nv_page_size = page_size;
				uint32_t num_pages = rom->save_size / page_size;
				ports[i].device.heartbeat_trainer.nv_pages = num_pages < 256 ? num_pages : 255;
			} else {
				ports[i].device.heartbeat_trainer.nv_page_size = 16;
				ports[i].device.heartbeat_trainer.nv_pages = 32;
				size_t bufsize = 
					ports[i].device.heartbeat_trainer.nv_page_size * ports[i].device.heartbeat_trainer.nv_pages
					+ 5 + 8;
				ports[i].device.heartbeat_trainer.nv_memory = malloc(bufsize);
				memset(ports[i].device.heartbeat_trainer.nv_memory, 0xFF, bufsize);
			}
			ports[i].device.heartbeat_trainer.state = HBPT_NEED_INIT;
		} else {
			debug_message("IO port %s connected to device '%s'\n", io_name(i), device_type_names[ports[i].device_type]);
		}
	}
}


#define TH 0x40
#define TR 0x20
#define TH_TIMEOUT 56000

void mouse_check_ready(io_port *port, uint32_t current_cycle)
{
	if (current_cycle >= port->device.mouse.ready_cycle) {
		port->device.mouse.tr_counter++;
		port->device.mouse.ready_cycle = CYCLE_NEVER;
		if (port->device.mouse.tr_counter == 3) {
			port->device.mouse.latched_x = port->device.mouse.cur_x;
			port->device.mouse.latched_y = port->device.mouse.cur_y;
			/* FIXME mouse mode owned by bindings now
			if (current_io->mouse_mode == MOUSE_ABSOLUTE) {
				//avoid overflow in absolute mode
				int deltax = port->device.mouse.latched_x - port->device.mouse.last_read_x;
				if (abs(deltax) > 255) {
					port->device.mouse.latched_x = port->device.mouse.last_read_x + (deltax > 0 ? 255 : -255);
				}
				int deltay = port->device.mouse.latched_y - port->device.mouse.last_read_y;
				if (abs(deltay) > 255) {
					port->device.mouse.latched_y = port->device.mouse.last_read_y + (deltay > 0 ? 255 : -255);
				}
			}*/
		}
	}
}

void io_adjust_cycles(io_port * port, uint32_t current_cycle, uint32_t deduction)
{
	/*uint8_t control = pad->control | 0x80;
	uint8_t th = control & pad->output;
	if (pad->input[GAMEPAD_TH0] || pad->input[GAMEPAD_TH1]) {
		printf("adjust_cycles | control: %X, TH: %X, GAMEPAD_TH0: %X, GAMEPAD_TH1: %X, TH Counter: %d, Timeout: %d, Cycle: %d\n", control, th, pad->input[GAMEPAD_TH0], pad->input[GAMEPAD_TH1], pad->th_counter,pad->timeout_cycle, current_cycle);
	}*/
	if (port->device_type == IO_GAMEPAD6)
	{
		if (current_cycle >= port->device.pad.timeout_cycle)
		{
			port->device.pad.th_counter = 0;
		} else {
			port->device.pad.timeout_cycle -= deduction;
		}
	} else if (port->device_type == IO_MOUSE) {
		mouse_check_ready(port, current_cycle);
		if (port->device.mouse.ready_cycle != CYCLE_NEVER) {
			port->device.mouse.ready_cycle -= deduction;
		}
	}
	for (int i = 0; i < 8; i++)
	{
		if (port->slow_rise_start[i] != CYCLE_NEVER) {
			if (port->slow_rise_start[i] >= deduction) {
				port->slow_rise_start[i] -= deduction;
			} else {
				port->slow_rise_start[i] = CYCLE_NEVER;
			}
		}
	}
	if (port->transmit_end >= deduction) {
		port->transmit_end -= deduction;
	} else {
		port->transmit_end = 0;
	}
	if (port->receive_end >= deduction) {
		port->receive_end -= deduction;
	} else {
		port->receive_end = 0;
	}
	if (port->last_poll_cycle >= deduction) {
		port->last_poll_cycle -= deduction;
	} else {
		port->last_poll_cycle = 0;
	}
}

#ifndef _WIN32
static void wait_for_connection(io_port *port)
{
	if (port->device.stream.data_fd == -1)
	{
		debug_message("Waiting for socket connection...\n");
		port->device.stream.data_fd = accept(port->device.stream.listen_fd, NULL, NULL);
		fcntl(port->device.stream.data_fd, F_SETFL, O_NONBLOCK | O_RDWR);
	}
}

static void poll_for_connection(io_port *port)
{
	if (port->device.stream.data_fd == -1)
	{
		fcntl(port->device.stream.listen_fd, F_SETFL, O_NONBLOCK | O_RDWR);
		port->device.stream.data_fd = accept(port->device.stream.listen_fd, NULL, NULL);
		fcntl(port->device.stream.listen_fd, F_SETFL, O_RDWR);
		if (port->device.stream.data_fd != -1) {
			fcntl(port->device.stream.data_fd, F_SETFL, O_NONBLOCK | O_RDWR);
		}
	}
}

static void write_serial_byte(io_port *port)
{
	fcntl(port->device.stream.data_fd, F_SETFL, O_RDWR);
	for (int sent = 0; sent != sizeof(port->serial_transmitting);)
	{
		sent = send(port->device.stream.data_fd, &port->serial_transmitting, sizeof(port->serial_transmitting), 0);
		if (sent < 0) {
			close(port->device.stream.data_fd);
			port->device.stream.data_fd = -1;
			wait_for_connection(port);
			fcntl(port->device.stream.data_fd, F_SETFL, O_RDWR);
		}
	}
	fcntl(port->device.stream.data_fd, F_SETFL, O_NONBLOCK | O_RDWR);
}

static void read_serial_byte(io_port *port)
{
	poll_for_connection(port);
	if (port->device.stream.data_fd == -1) {
		return;
	}
	int read = recv(port->device.stream.data_fd, &port->serial_receiving, sizeof(port->serial_receiving), 0);
	if (read < 0 && errno != EAGAIN && errno != EWOULDBLOCK) {
		close(port->device.stream.data_fd);
		port->device.stream.data_fd = -1;
	}
	if (read > 0) {
		port->receive_end = port->serial_cycle + 10 * port->serial_divider;
	}
}

static void service_pipe(io_port *port)
{
	uint8_t value;
	int numRead = read(port->device.stream.data_fd, &value, sizeof(value));
	if (numRead > 0)
	{
		port->input[IO_TH0] = (value & 0xF) | 0x10;
		port->input[IO_TH1] = (value >> 4) | 0x10;
	} else if(numRead == -1 && errno != EAGAIN && errno != EWOULDBLOCK) {
		warning("Error reading pipe for IO port: %d %s\n", errno, strerror(errno));
	}
}

static void service_socket(io_port *port)
{
	uint8_t buf[32];
	uint8_t blocking = 0;
	int numRead = 0;
	while (numRead <= 0)
	{
		numRead = recv(port->device.stream.data_fd, buf, sizeof(buf), 0);
		if (numRead > 0)
		{
			port->input[IO_TH0] = buf[numRead-1];
			if (port->input[IO_STATE] == IO_READ_PENDING)
			{
				port->input[IO_STATE] = IO_READ;
				if (blocking)
				{
					//pending read satisfied, back to non-blocking mode
					fcntl(port->device.stream.data_fd, F_SETFL, O_RDWR | O_NONBLOCK);
				}
			} else if (port->input[IO_STATE] == IO_WRITTEN) {
				port->input[IO_STATE] = IO_READ;
			}
		} else if (numRead == 0) {
			port->device.stream.data_fd = -1;
			wait_for_connection(port);
		} else if (errno != EAGAIN && errno != EWOULDBLOCK) {
			warning("Error reading from socket for IO port: %d %s\n", errno, strerror(errno));
			close(port->device.stream.data_fd);
			wait_for_connection(port);
		} else if (port->input[IO_STATE] == IO_READ_PENDING) {
			//clear the nonblocking flag so the next read will block
			if (!blocking)
			{
				fcntl(port->device.stream.data_fd, F_SETFL, O_RDWR);
				blocking = 1;
			}
		} else {
			//no new data, but that's ok
			break;
		}
	}

	if (port->input[IO_STATE] == IO_WRITE_PENDING)
	{
		uint8_t value = port->output & port->control;
		int written = 0;
		blocking = 0;
		while (written <= 0)
		{
			send(port->device.stream.data_fd, &value, sizeof(value), 0);
			if (written > 0)
			{
				port->input[IO_STATE] = IO_WRITTEN;
				if (blocking)
				{
					//pending write satisfied, back to non-blocking mode
					fcntl(port->device.stream.data_fd, F_SETFL, O_RDWR | O_NONBLOCK);
				}
			} else if (written == 0) {
				port->device.stream.data_fd = -1;
				wait_for_connection(port);
			} else if (errno != EAGAIN && errno != EWOULDBLOCK) {
				warning("Error writing to socket for IO port: %d %s\n", errno, strerror(errno));
				close(port->device.stream.data_fd);
				wait_for_connection(port);
			} else {
				//clear the nonblocking flag so the next write will block
				if (!blocking)
				{
					fcntl(port->device.stream.data_fd, F_SETFL, O_RDWR);
					blocking = 1;
				}
			}
		}
	}
}
#endif

enum {
	HBPT_UNKNOWN1 = 1,
	HBPT_POLL,
	HBPT_READ_PAGE = 5,
	HBPT_WRITE_PAGE,
	HBPT_READ_RTC,
	HBPT_SET_RTC,
	HBPT_GET_STATUS,
	HBPT_ERASE_NVMEM,
	HBPT_NVMEM_PARAMS,
	HBPT_INIT
};

static void start_reply(io_port *port, uint8_t bytes, const uint8_t *src)
{
	port->device.heartbeat_trainer.remaining_bytes = bytes;
	port->device.heartbeat_trainer.state = HBPT_REPLY;
	port->device.heartbeat_trainer.cur_buffer = (uint8_t *)src;
}

static void simple_reply(io_port *port, uint8_t value)
{
	port->device.heartbeat_trainer.param = value;
	start_reply(port, 1, &port->device.heartbeat_trainer.param);
}

static void expect_payload(io_port *port, uint8_t bytes, uint8_t *dst)
{
	port->device.heartbeat_trainer.remaining_bytes = bytes;
	port->device.heartbeat_trainer.state = HBPT_CMD_PAYLOAD;
	port->device.heartbeat_trainer.cur_buffer = dst;
}

void hbpt_check_init(io_port *port)
{
	if (port->device.heartbeat_trainer.state == HBPT_NEED_INIT) {
		port->device.heartbeat_trainer.rtc_base_timestamp = 0;
		for (int i = 0; i < 8; i ++)
		{
			port->device.heartbeat_trainer.rtc_base_timestamp <<= 8;
			port->device.heartbeat_trainer.rtc_base_timestamp |= port->device.heartbeat_trainer.nv_memory[i];
		}
		memcpy(port->device.heartbeat_trainer.rtc_base, port->device.heartbeat_trainer.nv_memory + 8, 5);
		if (port->device.heartbeat_trainer.rtc_base_timestamp == UINT64_MAX) {
			//uninitialized save, set the appropriate status bit
			port->device.heartbeat_trainer.status |= 1;
		}
		port->device.heartbeat_trainer.bpm = 60;
		port->device.heartbeat_trainer.state = HBPT_IDLE;
	}
}

void hbpt_check_send_reply(io_port *port)
{
	if (port->device.heartbeat_trainer.state == HBPT_REPLY && !port->receive_end) {
		port->serial_receiving = *(port->device.heartbeat_trainer.cur_buffer++);
		port->receive_end = port->serial_cycle + 10 * port->serial_divider;
		if (!--port->device.heartbeat_trainer.remaining_bytes) {
			port->device.heartbeat_trainer.state = HBPT_IDLE;
		}
	}
}

uint8_t is_leap_year(uint16_t year)
{
	if (year & 3) {
		return 0;
	}
	if (year % 100) {
		return 1;
	}
	if (year % 400) {
		return 0;
	}
	return 1;
}

uint8_t days_in_month(uint8_t month, uint16_t year)
{
	static uint8_t days_per_month[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
	if (month == 2 && is_leap_year(year)) {
		return 29;
	}
	if (month > 12 || !month) {
		return 30;
	}
	return days_per_month[month-1];
}

void hbpt_write_byte(io_port *port)
{
	hbpt_check_init(port);
	uint8_t reply;
	switch (port->device.heartbeat_trainer.state)
	{
	case HBPT_IDLE:
		port->device.heartbeat_trainer.cmd = port->serial_transmitting;
		switch (port->device.heartbeat_trainer.cmd)
		{
		case HBPT_UNKNOWN1:
			start_reply(port, 11, NULL);
			break;
		case HBPT_POLL:
			start_reply(port, 3, &port->device.heartbeat_trainer.bpm);
			if (port->serial_cycle - port->last_poll_cycle > MIN_POLL_INTERVAL) {
				process_events();
				port->last_poll_cycle = port->serial_cycle;
			}
			port->device.heartbeat_trainer.buttons = (port->input[GAMEPAD_TH0] << 2 & 0xC0) | (port->input[GAMEPAD_TH1] & 0x1F);
			if (port->device.heartbeat_trainer.cadence && port->input[GAMEPAD_TH1] & 0x20) {
				port->device.heartbeat_trainer.cadence--;
				printf("Cadence: %d\n", port->device.heartbeat_trainer.cadence);
			} else if (port->device.heartbeat_trainer.cadence < 255 && port->input[GAMEPAD_EXTRA] & 1) {
				port->device.heartbeat_trainer.cadence++;
				printf("Cadence: %d\n", port->device.heartbeat_trainer.cadence);
			}
			if (port->device.heartbeat_trainer.bpm && port->input[GAMEPAD_EXTRA] & 4) {
				port->device.heartbeat_trainer.bpm--;
				printf("Heart Rate: %d\n", port->device.heartbeat_trainer.bpm);
			} else if (port->device.heartbeat_trainer.bpm < 255 && port->input[GAMEPAD_EXTRA] & 2) {
				port->device.heartbeat_trainer.bpm++;
				printf("Heart Rate: %d\n", port->device.heartbeat_trainer.bpm);
			}
			
			break;
		case HBPT_READ_PAGE:
		case HBPT_WRITE_PAGE:
			//strictly speaking for the write case, we want 1 + page size here
			//but the rest of the payload goes to a different destination
			expect_payload(port, 1, &port->device.heartbeat_trainer.param);
			break;
		case HBPT_READ_RTC: {
			uint8_t *rtc = port->device.heartbeat_trainer.rtc_base;
			start_reply(port, 5, rtc);
			uint64_t now = time(NULL);
			uint64_t delta = (now - port->device.heartbeat_trainer.rtc_base_timestamp + 30) / 60;
			rtc[4] += delta % 60;
			if (rtc[4] > 59) {
				rtc[4] -= 60;
				rtc[3]++;
			}
			delta /= 60;
			if (delta) {
				rtc[3] += delta % 24;
				delta /= 24;
				if (rtc[3] > 23) {
					rtc[3] -= 24;
					delta++;
				}
				if (delta) {
					uint16_t year = rtc[0] < 81 ? 2000 + rtc[0] : 1900 + rtc[0];
					uint8_t days_cur_month = days_in_month(rtc[1], year);
					while (delta + rtc[2] > days_cur_month) {
						delta -= days_cur_month + 1 - rtc[2];
						rtc[2] = 1;
						if (++rtc[1] == 13) {
							rtc[1] = 1;
							year++;
						}
						days_cur_month = days_in_month(rtc[1], year);
					}
					rtc[1] += delta;
					rtc[0] = year % 100;
				}
			}
			printf("RTC %02d-%02d-%02d %02d:%02d\n", rtc[0], rtc[1], rtc[2], rtc[3], rtc[4]);
			port->device.heartbeat_trainer.rtc_base_timestamp = now;
			break;
		}
		case HBPT_SET_RTC:
			port->device.heartbeat_trainer.rtc_base_timestamp = time(NULL);
			expect_payload(port, 5, port->device.heartbeat_trainer.rtc_base);
			break;
		case HBPT_GET_STATUS:
			simple_reply(port, port->device.heartbeat_trainer.status);
			break;
		case HBPT_ERASE_NVMEM:
			expect_payload(port, 1, &port->device.heartbeat_trainer.param);
			break;
		case HBPT_NVMEM_PARAMS:
			start_reply(port, 2, &port->device.heartbeat_trainer.nv_page_size);
			break;
		case HBPT_INIT:
			expect_payload(port, 19, NULL);
			break;
		default:
			// it's unclear what these commands do as they are unused by Outback Joey
			// just return 0 to indicate failure
			simple_reply(port, 0);
		}
		break;
	case HBPT_CMD_PAYLOAD:
		if (port->device.heartbeat_trainer.cur_buffer) {
			*(port->device.heartbeat_trainer.cur_buffer++) = port->serial_transmitting;
		}
		if (!--port->device.heartbeat_trainer.remaining_bytes) {
			switch (port->device.heartbeat_trainer.cmd)
			{
			case HBPT_READ_PAGE:
			case HBPT_WRITE_PAGE:
				if (
					port->device.heartbeat_trainer.cmd == HBPT_WRITE_PAGE 
					&& port->device.heartbeat_trainer.cur_buffer != &port->device.heartbeat_trainer.param + 1) {
					simple_reply(port, 1);
					break;
				}
				port->device.heartbeat_trainer.remaining_bytes = port->device.heartbeat_trainer.nv_page_size;
				port->device.heartbeat_trainer.cur_buffer =
					port->device.heartbeat_trainer.param < port->device.heartbeat_trainer.nv_pages
					? port->device.heartbeat_trainer.nv_memory + 5 + 8
						+ port->device.heartbeat_trainer.param * port->device.heartbeat_trainer.nv_page_size
					: NULL;
				if (port->device.heartbeat_trainer.cmd == HBPT_WRITE_PAGE) {
					return;
				}
				port->device.heartbeat_trainer.state = HBPT_REPLY;
				break;
			case HBPT_SET_RTC:
				//save RTC base values back to nv memory area so it's saved to disk on exit
				for (int i = 0; i < 8; i++)
				{
					port->device.heartbeat_trainer.nv_memory[i] = port->device.heartbeat_trainer.rtc_base_timestamp >> (56 - i*8);
				}
				memcpy(port->device.heartbeat_trainer.nv_memory + 8, port->device.heartbeat_trainer.rtc_base, 5);
				simple_reply(port, 1);
				break;
			case HBPT_ERASE_NVMEM:
				memset(
					port->device.heartbeat_trainer.nv_memory + 5 + 8, 
					port->device.heartbeat_trainer.param, 
					port->device.heartbeat_trainer.nv_pages * port->device.heartbeat_trainer.nv_page_size
				);
				simple_reply(port, 1);
				break;
			case HBPT_INIT: {
				static const char reply[] = "(C) HEARTBEAT CORP";
				start_reply(port, strlen(reply), reply);
				break;
			}
			}
		}
	}
	hbpt_check_send_reply(port);
}

void hbpt_read_byte(io_port *port)
{
	hbpt_check_init(port);
	hbpt_check_send_reply(port);
}

const int mouse_delays[] = {112*7, 120*7, 96*7, 132*7, 104*7, 96*7, 112*7, 96*7};

enum {
	KB_SETUP,
	KB_READ,
	KB_WRITE
};

enum {
	SCTRL_BIT_TX_FULL = 1,
	SCTRL_BIT_RX_READY = 2,
	SCTRL_BIT_RX_ERROR = 4,
	SCTRL_BIT_RX_INTEN = 8,
	SCTRL_BIT_TX_ENABLE = 0x10,
	SCTRL_BIT_RX_ENABLE = 0x20
};

void io_run(io_port *port, uint32_t current_cycle)
{
	uint32_t new_serial_cycle = ((current_cycle - port->serial_cycle) / port->serial_divider) * port->serial_divider + port->serial_cycle;
	if (port->transmit_end && port->transmit_end <= new_serial_cycle) {
		port->transmit_end = 0;
		
		if (port->serial_ctrl & SCTRL_BIT_TX_ENABLE) {
			switch (port->device_type)
			{
			case IO_HEARTBEAT_TRAINER:
				hbpt_write_byte(port);
				break;
#ifndef _WIN32
			case IO_GENERIC_SERIAL:
				write_serial_byte(port);
				break;
#endif
			//TODO: think about how serial mode might interact with non-serial peripherals
			}
		}
	}
	if (!port->transmit_end && new_serial_cycle != port->serial_cycle && (port->serial_ctrl & SCTRL_BIT_TX_FULL)) {
		//there's a transmit byte pending and no byte is currently being sent
		port->serial_transmitting = port->serial_out;
		port->serial_ctrl &= ~SCTRL_BIT_TX_FULL;
		//1 start bit, 8 data bits and 1 stop bit
		port->transmit_end = new_serial_cycle + 10 * port->serial_divider;
	}
	port->serial_cycle = new_serial_cycle;
	if (port->serial_ctrl && SCTRL_BIT_RX_ENABLE) {
		if (port->receive_end && new_serial_cycle >= port->receive_end) {
			port->serial_in = port->serial_receiving;
			port->serial_ctrl |= SCTRL_BIT_RX_READY;
			port->receive_end = 0;
		}
		if (!port->receive_end) {
			switch(port->device_type)
			{
			case IO_HEARTBEAT_TRAINER:
				hbpt_read_byte(port);
				break;
#ifndef _WIN32
			case IO_GENERIC_SERIAL:
				read_serial_byte(port);
				break;
#endif
			//TODO: think about how serial mode might interact with non-serial peripherals
			}
		}
	}
}

void io_control_write(io_port *port, uint8_t value, uint32_t current_cycle)
{
	uint8_t changes = value ^ port->control;
	if (changes) {
		for (int i = 0; i < 8; i++)
		{
			if (!(value & 1 << i) && !(port->output & 1 << i)) {
				//port switched from output to input and the output value was 0
				//since there is a weak pull-up on input pins, this will lead
				//to a slow rise from 0 to 1 if the pin isn't being externally driven
				port->slow_rise_start[i] = current_cycle;
			} else {
				port->slow_rise_start[i] = CYCLE_NEVER;
			}
		}
		port->control = value;
	}
}

void io_data_write(io_port * port, uint8_t value, uint32_t current_cycle)
{
	uint8_t old_output = (port->control & port->output) | (~port->control & 0xFF);
	uint8_t output = (port->control & value) | (~port->control & 0xFF);
	switch (port->device_type)
	{
	case IO_GAMEPAD6:
		//check if TH has changed
		if ((old_output & TH) ^ (output & TH)) {
			if (current_cycle >= port->device.pad.timeout_cycle) {
				port->device.pad.th_counter = 0;
			}
			if ((output & TH)) {
				port->device.pad.th_counter++;
			}
			port->device.pad.timeout_cycle = current_cycle + TH_TIMEOUT;
		}
		break;
	case IO_MOUSE:
		mouse_check_ready(port, current_cycle);
		if (output & TH) {
			//request is over or mouse is being reset
			if (port->device.mouse.tr_counter) {
				//request is over
				port->device.mouse.last_read_x = port->device.mouse.latched_x;
				port->device.mouse.last_read_y = port->device.mouse.latched_y;
			}
			port->device.mouse.tr_counter = 0;
			port->device.mouse.ready_cycle = CYCLE_NEVER;
		} else {
			if ((output & TR) != (old_output & TR)) {
				int delay_index = port->device.mouse.tr_counter >= sizeof(mouse_delays) ? sizeof(mouse_delays)-1 : port->device.mouse.tr_counter;
				port->device.mouse.ready_cycle = current_cycle + mouse_delays[delay_index];
			}
		}
		break;
	case IO_SATURN_KEYBOARD:
		if (output & TH) {
			//request is over
			if (port->device.keyboard.tr_counter >= 10 && port->device.keyboard.read_pos != 0xFF) {
				//remove scan code from buffer
				port->device.keyboard.read_pos++;
				port->device.keyboard.read_pos &= 7;
				if (port->device.keyboard.read_pos == port->device.keyboard.write_pos) {
					port->device.keyboard.read_pos = 0xFF;
				}
			}
			port->device.keyboard.tr_counter = 0;
		} else {
			if ((output & TR) != (old_output & TR)) {
				port->device.keyboard.tr_counter++;
			}
		}
		break;
	case IO_XBAND_KEYBOARD:
		if (output & TH) {
			//request is over
			if (
				port->device.keyboard.mode == KB_READ && port->device.keyboard.tr_counter > 6
				&& (port->device.keyboard.tr_counter & 1)
			) {
				if (port->device.keyboard.events[port->device.keyboard.read_pos] & 0xFF00) {
					port->device.keyboard.events[port->device.keyboard.read_pos] &= 0xFF;
				} else {
					port->device.keyboard.read_pos++;
					port->device.keyboard.read_pos &= 7;
					if (port->device.keyboard.read_pos == port->device.keyboard.write_pos) {
						port->device.keyboard.read_pos = 0xFF;
					}
				}
			}
			port->device.keyboard.tr_counter = 0;
			port->device.keyboard.mode = KB_SETUP;
		} else {
			if ((output & TR) != (old_output & TR)) {
				port->device.keyboard.tr_counter++;
				if (port->device.keyboard.tr_counter == 2) {
					port->device.keyboard.mode = (output & 0xF) ? KB_READ : KB_WRITE;
				} else if (port->device.keyboard.mode == KB_WRITE) {
					switch (port->device.keyboard.tr_counter)
					{
					case 3:
						//host writes 0b0001
						break;
					case 4:
						//host writes 0b0000
						break;
					case 5:
						//host writes 0b0000
						break;
					case 6:
						port->device.keyboard.cmd = output << 4;
						break;
					case 7:
						port->device.keyboard.cmd |= output & 0xF;
						//TODO: actually do something with the command
						break;
					}
				} else if (
					port->device.keyboard.mode == KB_READ && port->device.keyboard.tr_counter > 7
					&& !(port->device.keyboard.tr_counter & 1)
				) {
					
					if (port->device.keyboard.events[port->device.keyboard.read_pos] & 0xFF00) {
						port->device.keyboard.events[port->device.keyboard.read_pos] &= 0xFF;
					} else {
						port->device.keyboard.read_pos++;
						port->device.keyboard.read_pos &= 7;
						if (port->device.keyboard.read_pos == port->device.keyboard.write_pos) {
							port->device.keyboard.read_pos = 0xFF;
						}
					}
				}
			}
		}
		break;
#ifndef _WIN32
	case IO_GENERIC:
		wait_for_connection(port);
		port->input[IO_STATE] = IO_WRITE_PENDING;
		service_socket(port);
		break;
#endif
	}
	port->output = value;

}

void io_tx_write(io_port *port, uint8_t value, uint32_t current_cycle)
{
	io_run(port, current_cycle);
	port->serial_out = value;
	port->serial_ctrl |= SCTRL_BIT_TX_FULL;
}

void io_sctrl_write(io_port *port, uint8_t value, uint32_t current_cycle)
{
	io_run(port, current_cycle);
	port->serial_ctrl = (port->serial_ctrl & 0x7) | (value & 0xF8);
	set_serial_clock(port);
}

uint8_t get_scancode_bytes(io_port *port)
{
	if (port->device.keyboard.read_pos == 0xFF) {
		return 0;
	}
	uint8_t bytes = 0, read_pos = port->device.keyboard.read_pos;
	do {
		bytes += port->device.keyboard.events[read_pos] & 0xFF00 ? 2 : 1;
		read_pos++;
		read_pos &= 7;
	} while (read_pos != port->device.keyboard.write_pos);
	
	return bytes;
}

#define SLOW_RISE_DEVICE (30*7)
#define SLOW_RISE_INPUT (12*7)

static uint8_t get_output_value(io_port *port, uint32_t current_cycle, uint32_t slow_rise_delay)
{
	uint8_t output = (port->control | 0x80) & port->output;
	for (int i = 0; i < 8; i++)
	{
		if (!(port->control & 1 << i)) {
			if (port->slow_rise_start[i] != CYCLE_NEVER) {
				if (current_cycle - port->slow_rise_start[i] >= slow_rise_delay) {
					output |= 1 << i;
				}
			} else {
				output |= 1 << i;
			}
		}
	}
	return output;
}

uint8_t io_data_read(io_port * port, uint32_t current_cycle)
{
	uint8_t output = get_output_value(port, current_cycle, SLOW_RISE_DEVICE);
	uint8_t control = port->control | 0x80;
	uint8_t th = output & 0x40;
	uint8_t input;
	uint8_t device_driven;
	if (current_cycle - port->last_poll_cycle > MIN_POLL_INTERVAL) {
		process_events();
		port->last_poll_cycle = current_cycle;
	}
	switch (port->device_type)
	{
	case IO_GAMEPAD2:
		input = ~port->input[GAMEPAD_TH1];
		device_driven = 0x3F;
		break;
	case IO_GAMEPAD3:
	{
		input = port->input[th ? GAMEPAD_TH1 : GAMEPAD_TH0];
		if (!th) {
			input |= 0xC;
		}
		//controller output is logically inverted
		input = ~input;
		device_driven = 0x3F;
		break;
	}
	case IO_GAMEPAD6:
	{
		if (current_cycle >= port->device.pad.timeout_cycle) {
			port->device.pad.th_counter = 0;
		}
		/*if (port->input[GAMEPAD_TH0] || port->input[GAMEPAD_TH1]) {
			printf("io_data_read | control: %X, TH: %X, GAMEPAD_TH0: %X, GAMEPAD_TH1: %X, TH Counter: %d, Timeout: %d, Cycle: %d\n", control, th, port->input[GAMEPAD_TH0], port->input[GAMEPAD_TH1], port->th_counter,port->timeout_cycle, context->current_cycle);
		}*/
		if (th) {
			if (port->device.pad.th_counter == 3) {
				input = port->input[GAMEPAD_EXTRA];
			} else {
				input = port->input[GAMEPAD_TH1];
			}
		} else {
			if (port->device.pad.th_counter == 2) {
				input = port->input[GAMEPAD_TH0] | 0xF;
			} else if(port->device.pad.th_counter == 3) {
				input = port->input[GAMEPAD_TH0]  & 0x30;
			} else {
				input = port->input[GAMEPAD_TH0] | 0xC;
			}
		}
		//controller output is logically inverted
		input = ~input;
		device_driven = 0x3F;
		break;
	}
	case IO_MOUSE:
	{
		mouse_check_ready(port, current_cycle);
		uint8_t tr = output & TR;
		if (th) {
			if (tr) {
				input = 0x10;
			} else {
				input = 0;
			}
		} else {

			int16_t delta_x = port->device.mouse.latched_x - port->device.mouse.last_read_x;
			int16_t delta_y = port->device.mouse.last_read_y - port->device.mouse.latched_y;
			switch (port->device.mouse.tr_counter)
			{
			case 0:
				input = 0xB;
				break;
			case 1:
			case 2:
				input = 0xF;
				break;
			case 3:
				input = 0;
				if (delta_y > 255 || delta_y < -255) {
					input |= 8;
				}
				if (delta_x > 255 || delta_x < -255) {
					input |= 4;
				}
				if (delta_y < 0) {
					input |= 2;
				}
				if (delta_x < 0) {
					input |= 1;
				}
				break;
			case 4:
				input = port->input[0];
				break;
			case 5:
				input = delta_x >> 4 & 0xF;
				break;
			case 6:
				input = delta_x & 0xF;
				break;
			case 7:
				input = delta_y >> 4 & 0xF;
				break;
			case 8:
			default:
				input = delta_y & 0xF;
				break;
			}
			input |= ((port->device.mouse.tr_counter & 1) == 0) << 4;
		}
		device_driven = 0x1F;
		break;
	}
	case IO_SATURN_KEYBOARD:
	{
		if (th) {
			input = 0x11;
		} else {
			uint8_t tr = output & TR;
			uint16_t code = port->device.keyboard.read_pos == 0xFF ? 0 
				: port->device.keyboard.events[port->device.keyboard.read_pos];
			switch (port->device.keyboard.tr_counter)
			{
			case 0:
				input = 1;
				break;
			case 1:
				//Saturn peripheral ID
				input = 3;
				break;
			case 2:
				//data size
				input = 4;
				break;
			case 3:
				//d-pad
				//TODO: set these based on keyboard state
				input = 0xF;
				break;
			case 4:
				//Start ABC
				//TODO: set these based on keyboard state
				input = 0xF;
				break;
			case 5:
				//R XYZ
				//TODO: set these based on keyboard state
				input = 0xF;
				break;
			case 6:
				//L and KBID
				//TODO: set L based on keyboard state
				input = 0x8;
				break;
			case 7:
				//Capslock, Numlock, Scrolllock
				//TODO: set these based on keyboard state
				input = 0;
				break;
			case 8:
				input = 6;
				if (code & 0xFF00) {
					//break
					input |= 1;
				} else if (code) {
					input |= 8;
				}
				break;
			case 9:
				input = code >> 4 & 0xF;
				break;
			case 10:
				input = code & 0xF;
				break;
			case 11:
				input = 0;
				break;
			default:
				input = 1;
				break;
			}
			input |= ((port->device.keyboard.tr_counter & 1) == 0) << 4;
		}
		device_driven = 0x1F;
		break;
	}
	case IO_XBAND_KEYBOARD:
	{
		if (th) {
			input = 0x1C;
		} else {
			uint8_t size;
			if (port->device.keyboard.mode == KB_SETUP || port->device.keyboard.mode == KB_READ) {
				switch (port->device.keyboard.tr_counter)
				{
				case 0:
					input = 0x3;
					break;
				case 1:
					input = 0x6;
					break;
				case 2:
					//This is where thoe host indicates a read or write
					//presumably, the keyboard only outputs this if the host
					//is not already driving the data bus low
					input = 0x9;
					break;
				case 3:
					size = get_scancode_bytes(port);
					if (size) {
						++size;
					}
					if (size > 15) {
						size = 15;
					}
					input = size;
					break;
				case 4:
				case 5:
					//always send packet type 0 for now
					input = 0;
					break;
				default:
					if (port->device.keyboard.read_pos == 0xFF) {
						//we've run out of bytes
						input = 0;
					} else if (port->device.keyboard.events[port->device.keyboard.read_pos] & 0xFF00) {
						if (port->device.keyboard.tr_counter & 1) {
							input = port->device.keyboard.events[port->device.keyboard.read_pos] >> 8 & 0xF;
						} else {
							input = port->device.keyboard.events[port->device.keyboard.read_pos] >> 12;
						}
					} else {
						if (port->device.keyboard.tr_counter & 1) {
							input = port->device.keyboard.events[port->device.keyboard.read_pos] & 0xF;
						} else {
							input = port->device.keyboard.events[port->device.keyboard.read_pos] >> 4;
						}
					}
					break;
				}
			} else {
				input = 0xF;
			}
			input |= ((port->device.keyboard.tr_counter & 1) == 0) << 4;
		}
		//this is not strictly correct at all times, but good enough for now
		device_driven = 0x1F;
		break;
	}
#ifndef _WIN32
	case IO_SEGA_PARALLEL:
		if (!th)
		{
			service_pipe(port);
		}
		input = port->input[th ? IO_TH1 : IO_TH0];
		device_driven = 0x3F;
		break;
	case IO_GENERIC:
		if (port->input[IO_TH0] & 0x80 && port->input[IO_STATE] == IO_WRITTEN)
		{
			//device requested a blocking read after writes
			port->input[IO_STATE] = IO_READ_PENDING;
		}
		service_socket(port);
		input = port->input[IO_TH0];
		device_driven = 0x7F;
		break;
#endif
	default:
		input = 0;
		device_driven = 0;
		break;
	}
	uint8_t value = (input & (~control) & device_driven) | (port->output & control);
	//deal with pins that are configured as inputs, but not being actively driven by the device
	uint8_t floating = (~device_driven) & (~control);
	if (floating) {
		value |= get_output_value(port, current_cycle, SLOW_RISE_INPUT) & floating;
	}
	/*if (port->input[GAMEPAD_TH0] || port->input[GAMEPAD_TH1]) {
		printf ("value: %X\n", value);
	}*/
	return value;
}

uint8_t io_rx_read(io_port * port, uint32_t current_cycle)
{
	io_run(port, current_cycle);
	port->serial_ctrl &= ~SCTRL_BIT_RX_READY;
	return port->serial_in;
}

uint8_t io_sctrl_read(io_port *port, uint32_t current_cycle)
{
	io_run(port, current_cycle);
	return port->serial_ctrl;
}

uint32_t io_next_interrupt(io_port *port, uint32_t current_cycle)
{
	if (!(port->control & 0x80)) {
		return CYCLE_NEVER;
	}
	if (port->serial_ctrl & SCTRL_BIT_RX_INTEN) {
		if (port->serial_ctrl & SCTRL_BIT_RX_READY) {
			return current_cycle;
		}
		if ((port->serial_ctrl & SCTRL_BIT_RX_ENABLE) && port->receive_end) {
			return port->receive_end;
		}
	}
	//TODO: handle external interrupts from TH transitions
	return CYCLE_NEVER;
}

void io_serialize(io_port *port, serialize_buffer *buf)
{
	save_int8(buf, port->output);
	save_int8(buf, port->control);
	save_int8(buf, port->serial_out);
	save_int8(buf, port->serial_in);
	save_int8(buf, port->serial_ctrl);
	save_int8(buf, port->device_type);
	save_buffer32(buf, port->slow_rise_start, 8);
	switch (port->device_type)
	{
	case IO_GAMEPAD6:
		save_int32(buf, port->device.pad.timeout_cycle);
		save_int16(buf, port->device.pad.th_counter);
		break;
	case IO_MOUSE:
		save_int32(buf, port->device.mouse.ready_cycle);
		save_int16(buf, port->device.mouse.last_read_x);
		save_int16(buf, port->device.mouse.last_read_y);
		save_int16(buf, port->device.mouse.latched_x);
		save_int16(buf, port->device.mouse.latched_y);
		save_int8(buf, port->device.mouse.tr_counter);
		break;
	case IO_SATURN_KEYBOARD:
	case IO_XBAND_KEYBOARD:
		save_int8(buf, port->device.keyboard.tr_counter);
		if (port->device_type == IO_XBAND_KEYBOARD) {
			save_int8(buf, port->device.keyboard.mode);
			save_int8(buf, port->device.keyboard.cmd);
		}
		break;
	case IO_HEARTBEAT_TRAINER:
		save_int8(buf, port->device.heartbeat_trainer.bpm);
		save_int8(buf, port->device.heartbeat_trainer.cadence);
		save_int8(buf, port->device.heartbeat_trainer.param);
		save_int8(buf, port->device.heartbeat_trainer.state);
		save_int8(buf, port->device.heartbeat_trainer.status);
		save_int8(buf, port->device.heartbeat_trainer.cmd);
		save_int8(buf, port->device.heartbeat_trainer.remaining_bytes);
		break;
	}
	save_int32(buf, port->serial_cycle);
	save_int32(buf, port->transmit_end);
	save_int32(buf, port->receive_end);
	save_int8(buf, port->serial_transmitting);
	save_int8(buf, port->serial_receiving);
}

void io_deserialize(deserialize_buffer *buf, void *vport)
{
	io_port *port = vport;
	port->output = load_int8(buf);
	port->control = load_int8(buf);
	port->serial_out = load_int8(buf);
	port->serial_in = load_int8(buf);
	port->serial_ctrl = load_int8(buf);
	set_serial_clock(port);
	uint8_t device_type = load_int8(buf);
	load_buffer32(buf, port->slow_rise_start, 8);
	if (device_type != port->device_type) {
		warning("Loaded save state has a different device type from the current configuration");
		return;
	}
	switch (port->device_type)
	{
	case IO_GAMEPAD6:
		port->device.pad.timeout_cycle = load_int32(buf);
		port->device.pad.th_counter = load_int16(buf);
		break;
	case IO_MOUSE:
		port->device.mouse.ready_cycle = load_int32(buf);
		port->device.mouse.last_read_x = load_int16(buf);
		port->device.mouse.last_read_y = load_int16(buf);
		port->device.mouse.latched_x = load_int16(buf);
		port->device.mouse.latched_y = load_int16(buf);
		port->device.mouse.tr_counter = load_int8(buf);
		break;
	case IO_SATURN_KEYBOARD:
	case IO_XBAND_KEYBOARD:
		port->device.keyboard.tr_counter = load_int8(buf);
		if (port->device_type == IO_XBAND_KEYBOARD) {
			port->device.keyboard.mode = load_int8(buf);
			port->device.keyboard.cmd = load_int8(buf);
		}
		break;
	case IO_HEARTBEAT_TRAINER:
		port->device.heartbeat_trainer.bpm = load_int8(buf);
		port->device.heartbeat_trainer.cadence = load_int8(buf);
		port->device.heartbeat_trainer.param = load_int8(buf);
		port->device.heartbeat_trainer.state = load_int8(buf);
		port->device.heartbeat_trainer.status = load_int8(buf);
		port->device.heartbeat_trainer.cmd = load_int8(buf);
		port->device.heartbeat_trainer.remaining_bytes = load_int8(buf);
		break;
	}
	if (buf->cur_pos < buf->size) {
		port->serial_cycle = load_int32(buf);
		port->transmit_end = load_int32(buf);
		port->receive_end = load_int32(buf);
		port->serial_transmitting = load_int8(buf);
		port->serial_receiving = load_int8(buf);
	}
}
