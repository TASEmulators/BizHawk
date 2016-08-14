#ifndef CALLBACKS_H
#define CALLBACKS_H

#include "types.h"

typedef void (*CDCallback)(int32 addr, int32 addrtype, int32 flags);

extern void (*biz_execcb)(unsigned addr);
extern void (*biz_readcb)(unsigned addr);
extern void (*biz_writecb)(unsigned addr);
extern void (*biz_tracecb)(void);
extern CDCallback biz_cdcallback;
extern unsigned biz_lastpc;

enum eCDLog_AddrType
{
	eCDLog_AddrType_MDCART, eCDLog_AddrType_RAM68k, eCDLog_AddrType_RAMZ80, eCDLog_AddrType_SRAM,
};

enum eCDLog_Flags
{
	eCDLog_Flags_Exec68k = 0x01,
	eCDLog_Flags_Data68k = 0x04,
	eCDLog_Flags_ExecZ80First = 0x08,
	eCDLog_Flags_ExecZ80Operand = 0x10,
	eCDLog_Flags_DataZ80 = 0x20,
	eCDLog_Flags_DMASource = 0x40
};

#endif
