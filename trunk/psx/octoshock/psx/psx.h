#pragma once

#include "emuware/emuware.h"
#include "cdrom/cdromif.h"
#include "video/surface.h"
#include "masmem.h"
#include "endian.h"


//
// Comment out these 2 defines for extra speeeeed.
//
#define PSX_DBGPRINT_ENABLE    1
#define PSX_EVENT_SYSTEM_CHECKS 1

//
// It's highly unlikely the user will want these if they're intentionally compiling without the debugger.
#ifndef WANT_DEBUGGER
 #undef PSX_DBGPRINT_ENABLE
 #undef PSX_EVENT_SYSTEM_CHECKS
#endif
//
//
//

namespace MDFN_IEN_PSX
{
 #define PSX_DBG_ERROR		0	// Emulator-level error.
 #define PSX_DBG_WARNING	1	// Warning about game doing questionable things/hitting stuff that might not be emulated correctly.
 #define PSX_DBG_BIOS_PRINT	2	// BIOS printf/putchar output.
 #define PSX_DBG_SPARSE		3	// Sparse(relatively) information debug messages(CDC commands).
 #define PSX_DBG_FLOOD		4	// Heavy informational debug messages(GPU commands; TODO).

#if PSX_DBGPRINT_ENABLE
 void PSX_DBG(unsigned level, const char *format, ...) throw() MDFN_COLD MDFN_FORMATSTR(gnu_printf, 2, 3);

 #define PSX_WARNING(format, ...) { PSX_DBG(PSX_DBG_WARNING, format "\n", ## __VA_ARGS__); }
 #define PSX_DBGINFO(format, ...) { }
#else
 static INLINE void PSX_DBG(unsigned level, const char* format, ...) { }
 static INLINE void PSX_WARNING(const char* format, ...) { }
 static INLINE void PSX_DBGINFO(const char* format, ...) { }
#endif

 typedef int32 pscpu_timestamp_t;

 bool PSX_EventHandler(const pscpu_timestamp_t timestamp);

 void PSX_MemWrite8(pscpu_timestamp_t timestamp, uint32 A, uint32 V);
 void PSX_MemWrite16(pscpu_timestamp_t timestamp, uint32 A, uint32 V);
 void PSX_MemWrite24(pscpu_timestamp_t timestamp, uint32 A, uint32 V);
 void PSX_MemWrite32(pscpu_timestamp_t timestamp, uint32 A, uint32 V);

 uint8 PSX_MemRead8(pscpu_timestamp_t &timestamp, uint32 A);
 uint16 PSX_MemRead16(pscpu_timestamp_t &timestamp, uint32 A);
 uint32 PSX_MemRead24(pscpu_timestamp_t &timestamp, uint32 A);
 uint32 PSX_MemRead32(pscpu_timestamp_t &timestamp, uint32 A);

 uint8 PSX_MemPeek8(uint32 A);
 uint16 PSX_MemPeek16(uint32 A);
 uint32 PSX_MemPeek32(uint32 A);

 // Should write to WO-locations if possible
 #if 0
 void PSX_MemPoke8(uint32 A, uint8 V);
 void PSX_MemPoke16(uint32 A, uint16 V);
 void PSX_MemPoke32(uint32 A, uint32 V);
 #endif

 void PSX_RequestMLExit(void);
 void ForceEventUpdates(const pscpu_timestamp_t timestamp);

 enum
 {
  PSX_EVENT__SYNFIRST = 0,
  PSX_EVENT_GPU,
  PSX_EVENT_CDC,
  //PSX_EVENT_SPU,
  PSX_EVENT_TIMER,
  PSX_EVENT_DMA,
  PSX_EVENT_FIO,
  PSX_EVENT__SYNLAST,
  PSX_EVENT__COUNT,
 };

 #define PSX_EVENT_MAXTS       		0x20000000
 void PSX_SetEventNT(const int type, const pscpu_timestamp_t next_timestamp);

 void PSX_GPULineHook(const pscpu_timestamp_t timestamp, const pscpu_timestamp_t line_timestamp, bool vsync, uint32 *pixels, const MDFN_PixelFormat* const format, const unsigned width, const unsigned pix_clock_offset, const unsigned pix_clock, const unsigned pix_clock_divider);

 uint32 PSX_GetRandU32(uint32 mina, uint32 maxa);
};


#include "dis.h"
#include "cpu.h"
#include "irq.h"
#include "gpu.h"
#include "dma.h"
//#include "sio.h"
#include "debug.h"

namespace MDFN_IEN_PSX
{
 class PS_CDC;
 class PS_SPU;

 extern PS_CPU *CPU;
 extern PS_GPU *GPU;
 extern PS_CDC *CDC;
 extern PS_SPU *SPU;
 extern MultiAccessSizeMem<2048 * 1024, uint32, false> MainRAM;
};

enum eRegion
{
 REGION_JP = 0,
 REGION_NA = 1,
 REGION_EU = 2,
 REGION_NONE = 3
};

enum eShockStep
{
	eShockStep_Frame
};

enum eShockFramebufferFlags
{
	eShockFramebufferFlags_None = 0,
	eShockFramebufferFlags_Normalize = 1
};

enum ePeripheralType
{
	ePeripheralType_None = 0, //can be used to signify disconnection
	
	ePeripheralType_Pad = 1, //SCPH-1080
	ePeripheralType_DualShock = 2, //SCPH-1200
	ePeripheralType_DualAnalog = 3, //SCPH-1180
	
	ePeripheralType_Multitap = 10,
};

enum eShockSetting
{
	REGION_AUTODETECT = 0,
	REGION_DEFAULT = 1,
	SLSTART = 2,
	SLEND = 3,
	SLSTARTP = 4,
	SLENDP = 5
};

int shock_GetSetting(eShockSetting setting);

#define MDFN_MSC_RESET 0
#define MDFN_MSC_POWER 1
#define MDFN_MSC_INSERT_DISK 2
#define MDFN_MSC_SELECT_DISK 3
#define MDFN_MSC_EJECT_DISK 4

#define SHOCK_OK 0
#define SHOCK_ERROR -1
#define SHOCK_NOCANDO -2
#define SHOCK_INVALID_ADDRESS -3

struct ShockTOCTrack
{
	u8 adr;
	u8 control;
	u32 lba;
};

struct ShockTOC
{
  u8 first_track;
  u8 last_track;
  u8 disc_type;
};

// [0] is unused, [100] is for the leadout track.
// Also, for convenience, tracks[last_track + 1] will always refer
// to the leadout track(even if last_track < 99, IE the leadout track details are duplicated).
typedef s32 (*ShockDisc_ReadTOC)(void* opaque, ShockTOC *read_target, ShockTOCTrack tracks[100 + 1]);
typedef s32 (*ShockDisc_ReadLBA)(void* opaque, s32 lba, void* dst);

class ShockDiscRef
{
public:
	ShockDiscRef(void *opaque, s32 lbaCount, ShockDisc_ReadTOC cbReadTOC, ShockDisc_ReadLBA cbReadLBA2448, bool suppliesDeinterleavedSubcode)
		: mOpaque(opaque)
		, mLbaCount(lbaCount)
		, mcbReadTOC(cbReadTOC)
		, mcbReadLBA2448(cbReadLBA2448)
		, mSuppliesDeinterleavedSubcode(suppliesDeinterleavedSubcode)
	{
	}

	s32 ReadTOC( ShockTOC *read_target, ShockTOCTrack tracks[100 + 1])
	{
		return mcbReadTOC(mOpaque, read_target, tracks);
	}

	s32 ReadLBA2448(s32 lba, void* dst2448);
	s32 ReadLBA2048(s32 lba, void* dst2048);

private:
	s32 InternalReadLBA2448(s32 lba, void* dst2448, bool needSubcode);
	void *mOpaque;
	s32 mLbaCount;
	ShockDisc_ReadTOC mcbReadTOC;
	ShockDisc_ReadLBA mcbReadLBA2448;
	bool mSuppliesDeinterleavedSubcode;
};

struct ShockDiscInfo
{
	s32 region;
	char id[5]; //SCEI, SCEA, SCEE, etc. with null terminator
};

struct ShockFramebufferInfo
{
	s32 width, height;
	s32 flags;
	void* ptr;
};

//Creates a ShockDiscRef (representing a disc) with the given properties. Returns it in the specified output pointer.
//The ReadLBA2048 function should return 0x01 or 0x02 depending on which mode was there.
//Others should return SHOCK_OK
EW_EXPORT s32 shock_CreateDisc(ShockDiscRef** outDisc, void *Opaque, s32 lbaCount, ShockDisc_ReadTOC ReadTOC, ShockDisc_ReadLBA ReadLBA2448, bool suppliesDeinterleavedSubcode);

//Destroys a ShockDiscRef created with shock_CreateDisc. Make sure you havent left it in the playstation before destroying it!
EW_EXPORT s32 shock_DestroyDisc(ShockDiscRef* disc);

//Inspects a disc by looking for the system.cnf and retrieves some necessary information about it.
//Useful for determining the region of a disc
EW_EXPORT s32 shock_AnalyzeDisc(ShockDiscRef* disc, ShockDiscInfo* info);

//Creates the psx instance as a console of the specified region.
//Additionally mounts the firmware from the provided buffer (the contents are copied)
//TODO - receive a model number parameter instead
EW_EXPORT s32 shock_Create(void** psx, s32 region, void* firmware512k);

//Frees the psx instance created with shock_Create
EW_EXPORT s32 shock_Destroy(void* psx);

//Attaches (or detaches) a peripheral at the given address. 
//Send ePeripheralType_None to detach. 
//Do not attach when something is already attached. 
//You can detach when nothing is attached.
//Returns SHOCK_NOCANDO if something inappropriate is done.
//Presently this has only been validated as functioning correctly before the initial PowerOn, but we would like to use it other times.
EW_EXPORT s32 shock_Peripheral_Connect(void* psx, s32 address, s32 type);

//Sets pad-type input (pad,dualshock,dualanalog) on the specified address;
//Read more about the input format (buttons, analog range) here: TBD
EW_EXPORT s32 shock_Peripheral_SetPadInput(void* psx, s32 address, u32 buttons, u8 left_x, u8 left_y, u8 right_x, u8 right_y);

//Sets the power to ON. Returns SHOCK_NOCANDO if already on.
EW_EXPORT s32 shock_PowerOn(void* psx);

//Sets the power to OFF. Returns SHOCK_NOCANDO if already off.
EW_EXPORT s32 shock_PowerOff(void* psx);

//Opens the disc tray. Returns SHOCK_NOCANDO if already open.
EW_EXPORT s32 shock_OpenTray(void* psx);

//Sets the disc in the tray. Returns SHOCK_NOCANDO if it's closed. You can pass NULL to remove a disc from the tray
EW_EXPORT s32 shock_SetDisc(void* psx, ShockDiscRef* disc);

//Closes the disc tray. Returns SHOCK_NOCANDO if already closed.
EW_EXPORT s32 shock_CloseTray(void* psx);

//Steps emulation by the specified interval
EW_EXPORT s32 shock_Step(void* psx, eShockStep step);

//Fetches the framebuffer. Can retrieve parameters (set the job ptr to NULL) or fill the provided job ptr with the framebuffer (make sure its big enough).
//This helps us copy fewer times.
EW_EXPORT s32 shock_GetFramebuffer(void* psx, ShockFramebufferInfo* fb);

//Returns the queued SPU output (usually ~737 samples per frame) as the normal 16bit interleaved stereo format
//The size of the queue will be returned. Make sure your buffer can handle it. Pass NULL just to get the required size.
EW_EXPORT s32 shock_GetSamples(void* buffer);