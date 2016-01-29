#pragma once

#include "octoshock.h"
#include "emuware/emuware.h"
#include "video/surface.h"
#include "masmem.h"
#include "endian.h"
#include "emuware/EW_state.h"


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
 void PSX_DBG(unsigned level, const char *format, ...) noexcept MDFN_COLD MDFN_FORMATSTR(gnu_printf, 2, 3);
 void PSX_DBG_BIOS_PUTC(uint8 c) noexcept;

 #define PSX_WARNING(format, ...) { PSX_DBG(PSX_DBG_WARNING, format "\n", ## __VA_ARGS__); }
 #define PSX_DBGINFO(format, ...) { }
#else
 static INLINE void PSX_DBG(unsigned level, const char* format, ...) { }
 static INLINE void PSX_DBG_BIOS_PUTC(uint8 c) { }
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
 void PSX_MemPoke8(uint32 A, uint8 V);
 void PSX_MemPoke16(uint32 A, uint16 V);
 void PSX_MemPoke32(uint32 A, uint32 V);

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

 void PSX_SetDMACycleSteal(unsigned stealage);

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
 extern MultiAccessSizeMem<2048 * 1024, false> MainRAM;
};

enum eRegion
{
 REGION_JP = 0,
 REGION_NA = 1,
 REGION_EU = 2,
 REGION_NONE = 3
};

enum eShockDeinterlaceMode
{
	eShockDeinterlaceMode_Weave,
	eShockDeinterlaceMode_Bob,
	eShockDeinterlaceMode_BobOffset
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

enum eShockRenderType
{
	eShockRenderType_Normal,
	eShockRenderType_ClipOverscan,

	//this should discard peculiar X adjustments during scan-out (done)
	//as well as peculiar Y adjustments (not done)
	//it's unclear whether the latter will actually ever be needed..
	//are any earthquake effects shaking the whole screen? 
	eShockRenderType_Framebuffer
};

enum eMemType
{
	eMemType_MainRAM = 0, //2048K
	eMemType_BiosROM = 1, //512K
	eMemType_PIOMem = 2, //64K
	eMemType_GPURAM = 3, //512K
	eMemType_SPURAM = 4, //512K
	eMemType_DCache = 5 //1K
};

enum ePeripheralType
{
	ePeripheralType_None = 0, //can be used to signify disconnection
	
	ePeripheralType_Pad = 1, //SCPH-1080
	ePeripheralType_DualShock = 2, //SCPH-1200
	ePeripheralType_DualAnalog = 3, //SCPH-1180

	ePeripheralType_Multitap = 10,
};

enum eShockStateTransaction : s32
{
	eShockStateTransaction_BinarySize = 0,
	eShockStateTransaction_BinaryLoad = 1,
	eShockStateTransaction_BinarySave = 2,
	eShockStateTransaction_TextLoad = 3,
	eShockStateTransaction_TextSave = 4
};

enum eShockMemcardTransaction
{
	eShockMemcardTransaction_Connect = 0, //connects it to the addressed port (not supported yet)
	eShockMemcardTransaction_Disconnect = 1, //disconnects it from the addressed port (not supported yet)
	eShockMemcardTransaction_Write = 2, //writes from the frontend to the memcard
	eShockMemcardTransaction_Read = 3, //reads from the memcard to the frontend. Also clears the dirty flag
	eShockMemcardTransaction_CheckDirty = 4, //checks whether the memcard is dirty
};

enum eShockMemCb
{
	eShockMemCb_None = 0,
	eShockMemCb_Read = 1,
	eShockMemCb_Write = 2,
	eShockMemCb_Execute = 4
};

#define MDFN_MSC_RESET 0
#define MDFN_MSC_POWER 1
#define MDFN_MSC_INSERT_DISK 2
#define MDFN_MSC_SELECT_DISK 3
#define MDFN_MSC_EJECT_DISK 4

#define SHOCK_OK 0
#define SHOCK_FALSE 0
#define SHOCK_TRUE 1
#define SHOCK_ERROR -1
#define SHOCK_NOCANDO -2
#define SHOCK_INVALID_ADDRESS -3
#define SHOCK_OVERFLOW -4

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

struct ShockRegisters_CPU
{
  u32 GPR[32];
  u32 PC, PC_NEXT;
  u32 IN_BD_SLOT;
  u32 LO, HI;
	u32 SR, CAUSE, EPC;
};

// [0] is unused, [100] is for the leadout track.
// Also, for convenience, tracks[last_track + 1] will always refer
// to the leadout track(even if last_track < 99, IE the leadout track details are duplicated).
typedef s32 (*ShockDisc_ReadTOC)(void* opaque, ShockTOC *read_target, ShockTOCTrack tracks[100 + 1]);
typedef s32 (*ShockDisc_ReadLBA)(void* opaque, s32 lba, void* dst);

//The callback to be issued for traces
typedef void (*ShockCallback_Trace)(void* opaque, u32 PC, u32 inst, const char* msg);

//the callback to be issued for memory hook events
//note: only one callback can be set. the type is sent to mask that one callback, not indicate which event type the callback is fore.
//there isnt one callback per type.
typedef void (*ShockCallback_Mem)(u32 address, eShockMemCb type, u32 size, u32 value);

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

	//formerly ReadRawSector
	//Reads 2352 + 96
	s32 ReadLBA2448(s32 lba, void* dst2448);

	//formerly ReadRawSectorPWOnly
	//Reads 96 bytes (of raw subchannel PW data) into pwbuf.
	//Probably the same format as what's at the end of ReadLBA2448
	//TODO - reorder args
	bool ReadLBA_PW(uint8* pwbuf96, int32 lba, bool hint_fullread);

	//only used by disc analysis stuff which should be refactored anyway. should eventually be removed
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

struct ShockRenderOptions
{
	s32 scanline_start, scanline_end;
	eShockRenderType renderType;
	eShockDeinterlaceMode deinterlaceMode;
	bool skip;
};

struct ShockMemcardTransaction
{
	eShockMemcardTransaction transaction;
	void* buffer128k;
};

struct ShockStateTransaction
{
	eShockStateTransaction transaction;
	void *buffer;
	s32 bufferLength;

	//originally this was a pointer, however, we had problems getting it to marshal correctly
	EW::FPtrs ff;
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

//Performs one of several transactions on an attached memory card.
EW_EXPORT s32 shock_Peripheral_MemcardTransact(void* psx, s32 address, ShockMemcardTransaction* transaction);

//Polls the given peripheral address for it's active flag status (inverse of lag flag)
//If you want, the lag flag can be cleared
//Returns SHOCK_TRUE if the input has been read this frame
//Returns SHOCK_FALSE if the input hasnt been read this frame (is lagging)
//Returns SHOCK_NOCANDO if there is no peripheral there 
//Returns SHOCK_INVALID_ADDRESS if address is invalid
//Returns SHOCK_ERROR for other errors.
EW_EXPORT s32 shock_Peripheral_PollActive(void* psx, s32 address, s32 clear);

//Mounts a PS-EXE executable 
EW_EXPORT s32 shock_MountEXE(void* psx, void* exebuf, s32 size, s32 ignore_pcsp);

//Sets the power to ON. Returns SHOCK_NOCANDO if already on.
EW_EXPORT s32 shock_PowerOn(void* psx);

//Triggers a soft reset immediately. Returns SHOCK_NOCANDO if console is powered off.
EW_EXPORT s32 shock_SoftReset(void* psx);

//Sets the power to OFF. Returns SHOCK_NOCANDO if already off.
EW_EXPORT s32 shock_PowerOff(void* psx);

//Opens the disc tray. Returns SHOCK_NOCANDO if already open.
EW_EXPORT s32 shock_OpenTray(void* psx);

//Sets the disc in the tray. Returns SHOCK_NOCANDO if it's closed. You can pass NULL to remove a disc from the tray
EW_EXPORT s32 shock_SetDisc(void* psx, ShockDiscRef* disc);

//POKES the disc in the tray, for use after loading a state. Does not affect any of the internal emulation parameters
EW_EXPORT s32 shock_PokeDisc(void* psx, ShockDiscRef* disc);

//Closes the disc tray. Returns SHOCK_NOCANDO if already closed.
EW_EXPORT s32 shock_CloseTray(void* psx);

//Sets rendering options for next frame
EW_EXPORT s32 shock_SetRenderOptions(void* psx, ShockRenderOptions* opts);

//Steps emulation by the specified interval
//TODO - think about something. After loadstating, the device input state is probably nonsense. 
//Normally we'd set the input before frame advancing. But every frontend might not do that, and we might not be stepping by one frame.
//What to do about this?
EW_EXPORT s32 shock_Step(void* psx, eShockStep step);

//Fetches the framebuffer. Can retrieve parameters (set the job ptr to NULL) or fill the provided job ptr with the framebuffer (make sure its big enough).
//This helps us copy fewer times.
EW_EXPORT s32 shock_GetFramebuffer(void* psx, ShockFramebufferInfo* fb);

//Returns the queued SPU output (usually ~737 samples per frame) as the normal 16bit interleaved stereo format
//The size of the queue will be returned. Make sure your buffer can handle it. Pass NULL just to get the required size.
EW_EXPORT s32 shock_GetSamples(void* psx, void* buffer);

//Returns information about a memory buffer for peeking (main memory, spu memory, etc.)
EW_EXPORT s32 shock_GetMemData(void* psx, void** ptr, s32* size, s32 memType);

//Savestate work. Returns the size if that's what was requested, otherwise error codes
EW_EXPORT s32 shock_StateTransaction(void *psx, ShockStateTransaction* transaction);

//Retrieves the CPU registers in a compact struct
EW_EXPORT s32 shock_GetRegisters_CPU(void* psx, ShockRegisters_CPU* buffer);

//Sets a CPU register. Rather than have an enum for the registers, lets just use the index (not offset) within the struct
EW_EXPORT s32 shock_SetRegister_CPU(void* psx, s32 index, u32 value);

//Sets the callback to be used for CPU tracing
EW_EXPORT s32 shock_SetTraceCallback(void* psx, void* opaque, ShockCallback_Trace callback);

//Sets whether LEC is enabled (sector level error correction). Defaults to FALSE (disabled)
EW_EXPORT s32 shock_SetLEC(void* psx, bool enabled);

//whether "determine lag from GPU frames" signal is set (GPU did something considered non-lag)
//returns SHOCK_TRUE or SHOCK_FALSE
EW_EXPORT s32 shock_GetGPUUnlagged(void* psx);