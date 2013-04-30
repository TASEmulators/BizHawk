/*  Copyright 2006 Theo Berkau

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

#ifndef NETLINK_H
#define NETLINK_H

#define NETLINK_BUFFER_SIZE     1024

typedef struct
{
   u8 RBR;
   u8 THR;
   u8 IER;
   u8 DLL;
   u8 DLM;
   u8 IIR;
   u8 FCR;
   u8 LCR;
   u8 MCR;
   u8 LSR;
   u8 MSR;
   u8 SCR;
} netlinkregs_struct;

typedef struct {
   u8 inbuffer[NETLINK_BUFFER_SIZE];
   u8 outbuffer[NETLINK_BUFFER_SIZE];
   u32 inbufferstart, inbufferend, inbuffersize;
   u32 outbufferstart, outbufferend, outbuffersize;
   netlinkregs_struct reg;
   int isechoenab;
   int connectsocket;
   int connectstatus;
   u32 cycles;
   int modemstate;
   char ipstring[16];
   char portstring[6];
} Netlink;

extern Netlink *NetlinkArea;

u8 FASTCALL NetlinkReadByte(u32 addr);
void FASTCALL NetlinkWriteByte(u32 addr, u8 val);
int NetlinkInit(const char *settingstring);
void NetlinkDeInit(void);
void NetlinkExec(u32 timing);

#endif
