//
// Jaguar EEPROM handler
//
// by Cal2
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Cleanups/enhancements by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  ------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

#include "eeprom.h"

#include <stdlib.h>
#include <string.h>
#include "jaguar.h"

uint16_t eeprom_ram[64];
bool eeprom_dirty;

static void EEPROMSave(void);
static void eeprom_set_di(uint32_t state);
static void eeprom_set_cs(uint32_t state);
static uint32_t eeprom_get_do(void);

enum { EE_STATE_START = 1, EE_STATE_OP_A, EE_STATE_OP_B, EE_STATE_0, EE_STATE_1,
	EE_STATE_2, EE_STATE_3, EE_STATE_0_0, EE_READ_ADDRESS, EE_STATE_0_0_0,
	EE_STATE_0_0_1, EE_STATE_0_0_2, EE_STATE_0_0_3, EE_STATE_0_0_1_0, EE_READ_DATA,
	EE_STATE_BUSY, EE_STATE_1_0, EE_STATE_1_1, EE_STATE_2_0, EE_STATE_3_0 };

static uint16_t jerry_ee_state = EE_STATE_START;
static uint16_t jerry_ee_op = 0;
static uint16_t jerry_ee_rstate = 0;
static uint16_t jerry_ee_address_data = 0;
static uint16_t jerry_ee_address_cnt = 6;
static uint16_t jerry_ee_data = 0;
static uint16_t jerry_ee_data_cnt = 16;
static uint16_t jerry_writes_enabled = 0;
static uint16_t jerry_ee_direct_jump = 0;

void EepromInit(void)
{
	memset(eeprom_ram, 0xFF, 64 * sizeof(uint16_t));
	eeprom_dirty = false;
}

void EepromReset(void)
{
}

void EepromDone(void)
{
}

static void EEPROMSave(void)
{
	eeprom_dirty = true;
}

uint8_t EepromReadByte(uint32_t offset)
{
	switch (offset)
	{
		case 0xF14001:
			return eeprom_get_do();
		case 0xF14801:
			break;
		case 0xF15001:
			eeprom_set_cs(1);
			break;
	}

	return 0x00;
}


uint16_t EepromReadWord(uint32_t offset)
{
	return ((uint16_t)EepromReadByte(offset + 0) << 8) | EepromReadByte(offset + 1);
}

void EepromWriteByte(uint32_t offset, uint8_t data)
{
	switch (offset)
	{
		case 0xF14001:
			break;
		case 0xF14801:
			eeprom_set_di(data & 0x01);
			break;
		case 0xF15001:
			eeprom_set_cs(1);
			break;
	}
}

void EepromWriteWord(uint32_t offset, uint16_t data)
{
	EepromWriteByte(offset + 0, (data >> 8) & 0xFF);
	EepromWriteByte(offset + 1, data & 0xFF);
}

static void eeprom_set_di(uint32_t data)
{
	switch (jerry_ee_state)
	{
		case EE_STATE_START:
			jerry_ee_state = EE_STATE_OP_A;
			break;
		case EE_STATE_OP_A:
			jerry_ee_op = (data << 1);
			jerry_ee_state = EE_STATE_OP_B;
			break;
		case EE_STATE_OP_B:
			jerry_ee_op |= data;
			jerry_ee_direct_jump = 0;

			switch (jerry_ee_op)
			{
				case 0: jerry_ee_state = EE_STATE_0; break;
				case 1: jerry_ee_state = EE_STATE_1; break;
				case 2: jerry_ee_state = EE_STATE_2; break;
				case 3: jerry_ee_state = EE_STATE_3; break;
			}

			eeprom_set_di(data);
			break;
		case EE_STATE_0:
			jerry_ee_rstate = EE_STATE_0_0;
			jerry_ee_state = EE_READ_ADDRESS;
			jerry_ee_direct_jump = 1;
			jerry_ee_address_cnt = 6;
			jerry_ee_address_data = 0;
			break;
		case EE_STATE_0_0:
			switch ((jerry_ee_address_data >> 4) & 0x03)
			{
				case 0: jerry_ee_state=EE_STATE_0_0_0; break;
				case 1: jerry_ee_state=EE_STATE_0_0_1; break;
				case 2: jerry_ee_state=EE_STATE_0_0_2; break;
				case 3: jerry_ee_state=EE_STATE_0_0_3; break;
			}

			eeprom_set_di(data);
			break;
		case EE_STATE_0_0_0:
			jerry_writes_enabled = 0;
			jerry_ee_state = EE_STATE_START;
			break;
		case EE_STATE_0_0_1:
			jerry_ee_rstate = EE_STATE_0_0_1_0;
			jerry_ee_state = EE_READ_DATA;
			jerry_ee_data_cnt = 16;
			jerry_ee_data = 0;
			jerry_ee_direct_jump = 1;
			break;
		case EE_STATE_0_0_1_0:
			if (jerry_writes_enabled)
			{
				for(int i=0; i<64; i++)
					eeprom_ram[i] = jerry_ee_data;

				EEPROMSave();
			}

			jerry_ee_state = EE_STATE_BUSY;
			break;
		case EE_STATE_0_0_2:
			if (jerry_writes_enabled)
				for(int i=0; i<64; i++)
					eeprom_ram[i] = 0xFFFF;

			jerry_ee_state = EE_STATE_BUSY;
			break;
		case EE_STATE_0_0_3:
			jerry_writes_enabled = 1;
			jerry_ee_state = EE_STATE_START;
			break;
		case EE_STATE_1:
			jerry_ee_rstate = EE_STATE_1_0;
			jerry_ee_state = EE_READ_ADDRESS;
			jerry_ee_address_cnt = 6;
			jerry_ee_address_data = 0;
			jerry_ee_direct_jump = 1;
			break;
		case EE_STATE_1_0:
			jerry_ee_rstate = EE_STATE_1_1;
			jerry_ee_state = EE_READ_DATA;
			jerry_ee_data_cnt = 16;
			jerry_ee_data = 0;
			jerry_ee_direct_jump = 1;
			break;
		case EE_STATE_1_1:
			if (jerry_writes_enabled)
			{
				eeprom_ram[jerry_ee_address_data] = jerry_ee_data;
				EEPROMSave();
			}

			jerry_ee_state = EE_STATE_BUSY;
			break;
		case EE_STATE_2:
			jerry_ee_rstate = EE_STATE_2_0;
			jerry_ee_state = EE_READ_ADDRESS;
			jerry_ee_address_cnt = 6;
			jerry_ee_address_data = 0;
			jerry_ee_data_cnt = 16;
			jerry_ee_data = 0;
			break;
		case EE_STATE_3:
			jerry_ee_rstate = EE_STATE_3_0;
			jerry_ee_state = EE_READ_ADDRESS;
			jerry_ee_address_cnt = 6;
			jerry_ee_address_data = 0;
			jerry_ee_direct_jump = 1;
			break;
		case EE_STATE_3_0:
			if (jerry_writes_enabled)
				eeprom_ram[jerry_ee_address_data] = 0xFFFF;

			jerry_ee_state = EE_STATE_BUSY;
			break;
		case EE_READ_DATA:
			jerry_ee_data <<= 1;
			jerry_ee_data |= data;
			jerry_ee_data_cnt--;

			if (!jerry_ee_data_cnt)
			{
				jerry_ee_state = jerry_ee_rstate;

				if (jerry_ee_direct_jump)
					eeprom_set_di(data);
			}

			break;
		case EE_READ_ADDRESS:
			jerry_ee_address_data <<= 1;
			jerry_ee_address_data |= data;
			jerry_ee_address_cnt--;

			if (!jerry_ee_address_cnt)
			{
				jerry_ee_state = jerry_ee_rstate;

				if (jerry_ee_direct_jump)
					eeprom_set_di(data);
			}

			break;
		default:
			jerry_ee_state = EE_STATE_OP_A;
	}
}

static void eeprom_set_cs(uint32_t)
{
	jerry_ee_state = EE_STATE_START;
	jerry_ee_op = 0;
	jerry_ee_rstate = 0;
	jerry_ee_address_data = 0;
	jerry_ee_address_cnt = 6;
	jerry_ee_data = 0;
	jerry_ee_data_cnt = 16;
	jerry_writes_enabled = 1;
}


static uint32_t eeprom_get_do(void)
{
	uint16_t data = 1;

	switch (jerry_ee_state)
	{
		case EE_STATE_START:
			data = 1;
			break;
		case EE_STATE_BUSY:
			jerry_ee_state = EE_STATE_START;
			data = 0;
			break;
		case EE_STATE_2_0:
			jerry_ee_data_cnt--;
			data = (eeprom_ram[jerry_ee_address_data] >> jerry_ee_data_cnt) & 0x01;

			if (!jerry_ee_data_cnt)
			{
				jerry_ee_state = EE_STATE_START;
			}
			break;
	}

	return data;
}

