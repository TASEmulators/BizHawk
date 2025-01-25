/*
 Copyright 2015 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef MENU_H_
#define MENU_H_
typedef struct {
	char     *curpath;
	uint16_t latch;
	uint16_t state;
	uint8_t  external_game_load;
} menu_context;


uint16_t menu_read_w(uint32_t address, void * context);
void * menu_write_w(uint32_t address, void * context, uint16_t value);

#endif // MENU_H_
