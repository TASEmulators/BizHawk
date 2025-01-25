#include "genesis.h"
#include "util.h"

enum {
	I2C_IDLE,
	I2C_START,
	I2C_DEVICE_ACK,
	I2C_ADDRESS_HI,
	I2C_ADDRESS_HI_ACK,
	I2C_ADDRESS,
	I2C_ADDRESS_ACK,
	I2C_READ,
	I2C_READ_ACK,
	I2C_WRITE,
	I2C_WRITE_ACK
};

char * i2c_states[] = {
	"idle",
	"start",
	"device ack",
	"address hi",
	"address hi ack",
	"address",
	"address ack",
	"read",
	"read_ack",
	"write",
	"write_ack"
};

void eeprom_init(eeprom_state *state, uint8_t *buffer, uint32_t size)
{
	state->slave_sda = 1;
	state->host_sda = state->scl = 0;
	state->buffer = buffer;
	state->size = size;
	state->state = I2C_IDLE;
}

void set_host_sda(eeprom_state *state, uint8_t val)
{
	if (state->scl) {
		if (val & ~state->host_sda) {
			//low to high, stop condition
			state->state = I2C_IDLE;
			state->slave_sda = 1;
		} else if (~val & state->host_sda) {
			//high to low, start condition
			state->state = I2C_START;
			state->slave_sda = 1;
			state->counter = 8;
		}
	}
	state->host_sda = val;
}

void set_scl(eeprom_state *state, uint8_t val)
{
	if (val & ~state->scl) {
		//low to high transition
		switch (state->state)
		{
		case I2C_START:
		case I2C_ADDRESS_HI:
		case I2C_ADDRESS:
		case I2C_WRITE:
			state->latch = state->host_sda | state->latch << 1;
			state->counter--;
			if (!state->counter) {
				switch (state->state & 0x7F)
				{
				case I2C_START:
					state->state = I2C_DEVICE_ACK;
					break;
				case I2C_ADDRESS_HI:
					state->address = state->latch << 8;
					state->state = I2C_ADDRESS_HI_ACK;
					break;
				case I2C_ADDRESS:
					state->address |= state->latch;
					state->state = I2C_ADDRESS_ACK;
					break;
				case I2C_WRITE:
					state->buffer[state->address] = state->latch;
					state->state = I2C_WRITE_ACK;
					break;
				}
			}
			break;
		case I2C_DEVICE_ACK:
			if (state->latch & 1) {
				state->state = I2C_READ;
				state->counter = 8;
				if (state->size < 256) {
					state->address = state->latch >> 1;
				}
				state->latch = state->buffer[state->address];
			} else {
				if (state->size < 256) {
					state->address = state->latch >> 1;
					state->state = I2C_WRITE;
				} else if (state->size < 4096) {
					state->address = (state->latch & 0xE) << 7;
					state->state = I2C_ADDRESS;
				} else {
					state->state = I2C_ADDRESS_HI;
				}
				state->counter = 8;
			}
			break;
		case I2C_ADDRESS_HI_ACK:
			state->state = I2C_ADDRESS;
			state->counter = 8;
			break;
		case I2C_ADDRESS_ACK:
			state->state = I2C_WRITE;
			state->address &= state->size-1;
			state->counter = 8;
			break;
		case I2C_READ:
			state->counter--;
			if (!state->counter) {
				state->state = I2C_READ_ACK;
			}
			break;
		case I2C_READ_ACK:
			state->state = I2C_READ;
			state->counter = 8;
			state->address++;
			//TODO: page mask
			state->address &= state->size-1;
			state->latch = state->buffer[state->address];
			break;
		case I2C_WRITE_ACK:
			state->state = I2C_WRITE;
			state->counter = 8;
			state->address++;
			//TODO: page mask
			state->address &= state->size-1;
			break;
		}
	} else if (~val & state->scl) {
		//high to low transition
		switch (state->state & 0x7F)
		{
		case I2C_DEVICE_ACK:
		case I2C_ADDRESS_HI_ACK:
		case I2C_ADDRESS_ACK:
		case I2C_READ_ACK:
		case I2C_WRITE_ACK:
			state->slave_sda = 0;
			break;
		case I2C_READ:
			state->slave_sda = state->latch >> 7;
			state->latch = state->latch << 1;
			break;
		default:
			state->slave_sda = 1;
			break;
		}
	}
	state->scl = val;
}

uint8_t get_sda(eeprom_state *state)
{
	return state->host_sda & state->slave_sda;
}

eeprom_map *find_eeprom_map(uint32_t address, genesis_context *gen)
{
	for (int i = 0; i < gen->num_eeprom; i++)
	{
		if (address >= gen->eeprom_map[i].start && address <= gen->eeprom_map[i].end) {
			return  gen->eeprom_map + i;
		}
	}
	return NULL;
}

void * write_eeprom_i2c_w(uint32_t address, void * context, uint16_t value)
{
	genesis_context *gen = ((m68k_context *)context)->system;
	eeprom_map *map = find_eeprom_map(address, gen);
	if (!map) {
		fatal_error("Could not find EEPROM map for address %X\n", address);
	}
	if (map->scl_mask) {
		set_scl(&gen->eeprom, (value & map->scl_mask) != 0);
	}
	if (map->sda_write_mask) {
		set_host_sda(&gen->eeprom, (value & map->sda_write_mask) != 0);
	}
	return context;
}

void * write_eeprom_i2c_b(uint32_t address, void * context, uint8_t value)
{
	genesis_context *gen = ((m68k_context *)context)->system;
	eeprom_map *map = find_eeprom_map(address, gen);
	if (!map) {
		fatal_error("Could not find EEPROM map for address %X\n", address);
	}

	uint16_t expanded, mask;
	if (address & 1) {
		expanded = value;
		mask = 0xFF;
	} else {
		expanded = value << 8;
		mask = 0xFF00;
	}
	if (map->scl_mask & mask) {
		set_scl(&gen->eeprom, (expanded & map->scl_mask) != 0);
	}
	if (map->sda_write_mask & mask) {
		set_host_sda(&gen->eeprom, (expanded & map->sda_write_mask) != 0);
	}
	return context;
}

uint16_t read_eeprom_i2c_w(uint32_t address, void * context)
{
	genesis_context *gen = ((m68k_context *)context)->system;
	eeprom_map *map = find_eeprom_map(address, gen);
	if (!map) {
		fatal_error("Could not find EEPROM map for address %X\n", address);
	}
	uint16_t ret = 0;
	if (map->sda_read_bit < 16) {
		ret = get_sda(&gen->eeprom) << map->sda_read_bit;
	}
	return ret;	
}

uint8_t read_eeprom_i2c_b(uint32_t address, void * context)
{
	genesis_context *gen = ((m68k_context *)context)->system;
	eeprom_map *map = find_eeprom_map(address, gen);
	if (!map) {
		fatal_error("Could not find EEPROM map for address %X\n", address);
	}
	uint8_t bit = address & 1 ? map->sda_read_bit : map->sda_read_bit - 8;
	uint8_t ret = 0;
	if (bit < 8) {
		ret = get_sda(&gen->eeprom) << bit;
	}
	return ret;
}
