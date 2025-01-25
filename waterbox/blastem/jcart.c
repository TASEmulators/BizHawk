#include <stdlib.h>
#include "genesis.h"

static io_port *get_ports(m68k_context *m68k)
{
	genesis_context *gen = m68k->system;
	if (!gen->extra) {
		io_port *ports = calloc(2, sizeof(io_port));
		ports[0].device_type = IO_GAMEPAD3;
		ports[0].device.pad.gamepad_num = 3;
		ports[1].device_type = IO_GAMEPAD3;
		ports[1].device.pad.gamepad_num = 4;
		io_control_write(ports, 0x40, 0);
		io_control_write(ports + 1, 0x40, 0);
		gen->extra = ports;
	}
		
	return gen->extra;
}

void *jcart_write_w(uint32_t address, void *context, uint16_t value)
{
	m68k_context *m68k= context;
	io_port *ports = get_ports(m68k);
	value = value << 6 & 0x40;
	io_data_write(ports, value, m68k->current_cycle);
	io_data_write(ports + 1, value, m68k->current_cycle);
	return context;
}

void *jcart_write_b(uint32_t address, void *context, uint8_t value)
{
	if (address & 1) {
		return jcart_write_w(address, context, value);
	}
	return context;
}

uint16_t jcart_read_w(uint32_t address, void *context)
{
	m68k_context *m68k= context;
	io_port *ports = get_ports(m68k);
	//according to Eke, bit 14 is forced low, at least on the Micro Machines 2 cart
	//TODO: Test behavior of actual cart
	uint16_t value = io_data_read(ports, m68k->current_cycle) << 8;
	value |= io_data_read(ports + 1, m68k->current_cycle);
	return value;
}

uint8_t jcart_read_b(uint32_t address, void *context)
{
	m68k_context *m68k= context;
	io_port *ports = get_ports(m68k);
	return io_data_read(ports + (address & 1), m68k->current_cycle);
}

void jcart_adjust_cycles(genesis_context *context, uint32_t deduction)
{
	io_port *ports = get_ports(context->m68k);
	io_adjust_cycles(ports, context->m68k->current_cycle, deduction);
	io_adjust_cycles(ports + 1, context->m68k->current_cycle, deduction);
}

void jcart_gamepad_down(genesis_context *context, uint8_t gamepad_num, uint8_t button)
{
	io_port *ports = get_ports(context->m68k);
	if (gamepad_num == ports[1].device.pad.gamepad_num) {
		ports++;
	} else if (gamepad_num != ports[0].device.pad.gamepad_num) {
		ports = NULL;
	}
	if (ports) {
		io_port_gamepad_down(ports, button);
	}
}

void jcart_gamepad_up(genesis_context *context, uint8_t gamepad_num, uint8_t button)
{
	io_port *ports = get_ports(context->m68k);
	if (gamepad_num == ports[1].device.pad.gamepad_num) {
		ports++;
	} else if (gamepad_num != ports[0].device.pad.gamepad_num) {
		ports = NULL;
	}
	if (ports) {
		io_port_gamepad_up(ports, button);
	}
}