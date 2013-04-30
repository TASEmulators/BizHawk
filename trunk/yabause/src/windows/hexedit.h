/*  Copyright 2007 Theo Berkau

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

#ifndef HEXEDIT_H
#define HEXEDIT_H

#include "custctl.h"

#define HEXEDIT "YabauseHexEdit"
#define HEX_SETADDRESSLIST      WM_USER+11
#define HEX_GOTOADDRESS         WM_USER+12
#define HEX_GETSELECTED         WM_USER+13
#define HEX_GETCURADDRESS       WM_USER+14

typedef struct
{
   u32 start;
   u32 end;
   char name[32];
} addrlist_struct;

void InitHexEdit();

#endif
