/*  Copyright 2006 Guillaume Duhamel

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include <Carbon/Carbon.h>

#include "../yabause.h"
#include "../peripheral.h"
#include "../sh2core.h"
#include "../sh2int.h"
#include "../vidogl.h"
#include "../vidsoft.h"
#include "../scsp.h"
#include "../sndsdl.h"
#include "../cdbase.h"
#include "../cs0.h"
#include "../m68kcore.h"

extern yabauseinit_struct yinit;

WindowRef CreateSettingsWindow();

typedef struct _YuiAction YuiAction;

/*
struct _YuiAction {
	UInt32 key;
	const char * name;
	void (*press)(void);
	void (*release)(void);
};

extern YuiAction key_config[];
*/
