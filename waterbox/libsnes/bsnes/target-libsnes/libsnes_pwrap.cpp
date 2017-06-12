//types of messages:
//cmd: frontend->core: "command to core" a command from the frontend which causes emulation to proceed. when sending a command, the frontend should wait for an eMessage_BRK_Complete before proceeding, although a debugger might proceed after any BRK
//query: frontend->core: "query to core" a query from the frontend which can (and should) be satisfied immediately by the core but which does not result in emulation processes (notably, nothing resembling a CMD and nothing which can trigger a BRK)
//sig: core->frontend: "core signal" a synchronous operation called from the emulation process which the frontend should handle immediately without issuing any calls into the core
//brk: core->frontend: "core break" the emulation process has suspended. the frontend is free to do whatever it wishes.

#define LIBSNES_IMPORT
#include "snes/snes.hpp"
#include "libsnes.hpp"
#include <emulibc.h>

#include <libco.h>

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <string>
#include <vector>

extern SNES::Interface *iface;

typedef uint8 u8;
typedef uint16 u16;
typedef uint64 u64;
typedef uint32 u32;

typedef int32 s32;

typedef void(*Action)();

enum eMessage : int32
{
	eMessage_NotSet,

	eMessage_Resume,

	eMessage_QUERY_FIRST,
	eMessage_QUERY_get_memory_size,
	eMessage_QUERY_peek,
	eMessage_QUERY_poke,
	eMessage_QUERY_serialize_size,
	eMessage_QUERY_set_color_lut,
	eMessage_QUERY_GetMemoryIdName,
	eMessage_QUERY_state_hook_exec,
	eMessage_QUERY_state_hook_read,
	eMessage_QUERY_state_hook_write,
	eMessage_QUERY_state_hook_nmi,
	eMessage_QUERY_state_hook_irq,
	eMessage_QUERY_enable_trace,
	eMessage_QUERY_enable_scanline,
	eMessage_QUERY_enable_audio,
	eMessage_QUERY_set_layer_enable,
	eMessage_QUERY_set_backdropColor,
	eMessage_QUERY_peek_logical_register,
	eMessage_QUERY_peek_cpu_regs,
	eMessage_QUERY_set_cdl,
	eMessage_QUERY_LAST,

	eMessage_CMD_FIRST,
	eMessage_CMD_init,
	eMessage_CMD_power,
	eMessage_CMD_reset,
	eMessage_CMD_run,
	eMessage_CMD_serialize,
	eMessage_CMD_unserialize,
	eMessage_CMD_load_cartridge_normal,
	eMessage_CMD_load_cartridge_sgb,
	eMessage_CMD_term,
	eMessage_CMD_unload_cartridge,
	eMessage_CMD_LAST,

	eMessage_SIG_video_refresh,
	eMessage_SIG_input_poll,
	eMessage_SIG_input_state,
	eMessage_SIG_input_notify,
	eMessage_SIG_audio_flush,
	eMessage_SIG_path_request,
	eMessage_SIG_trace_callback,
	eMessage_SIG_allocSharedMemory,
	eMessage_SIG_freeSharedMemory,

	eMessage_BRK_Complete,
	eMessage_BRK_hook_exec,
	eMessage_BRK_hook_read,
	eMessage_BRK_hook_write,
	eMessage_BRK_hook_nmi,
	eMessage_BRK_hook_irq,
	eMessage_BRK_scanlineStart,
};

enum eStatus : int32
{
	eStatus_Idle,
	eStatus_CMD,
	eStatus_BRK
};


//watch it! the size of this struct is important!
#ifdef _MSC_VER
#pragma pack(push,1)
#endif
struct CPURegsComm {
	u32 pc;
	u16 a, x, y, z, s, d, vector; //7x
	u8 p, nothing;
	u32 aa, rd;
	u8 sp, dp, db, mdr;
}
#ifndef _MSC_VER
__attribute__((__packed__))
#endif
;
#ifdef _MSC_VER
#pragma pack(pop)
#endif

struct LayerEnablesComm
{
	u8 BG1_Prio0, BG1_Prio1;
	u8 BG2_Prio0, BG2_Prio1;
	u8 BG3_Prio0, BG3_Prio1;
	u8 BG4_Prio0, BG4_Prio1;
	u8 Obj_Prio0, Obj_Prio1, Obj_Prio2, Obj_Prio3;
};

//TODO: do any of these need to be volatile?
struct CommStruct
{
	//the cmd being executed
	eMessage cmd;

	//the status of the core
	eStatus status;

	//the SIG or BRK that the core is halted in
	eMessage reason;

	int32 padding1;

	//flexible in/out parameters
	//these are all "overloaded" a little so it isn't clear what's used for what in for any particular message..
	//but I think it will beat having to have some kind of extremely verbose custom layouts for every message
	char* str;
	void* ptr;
	uint32 id, addr, value, size;
	int32 port, device, index, slot;
	int32 width, height;
	int32 scanline;
	SNES::Input::Device inports[2];

	int32 padding2;

	//always used in pairs
	void* buf[3];
	int32 buf_size[3];

	int32 padding3;

	int64 cdl_ptr[4];
	int32 cdl_size[4];

	CPURegsComm cpuregs;
	LayerEnablesComm layerEnables;

	//static configuration-type information which can be grabbed off the core at any time without even needing a QUERY command
	uint32 region;
	uint32 mapper;

	int32 padding4;

	//===========================================================

	//private stuff
	void* privbuf[3]; //TODO remember to tidy this..

	void CopyBuffer(int id, void* ptr, int32 size)
	{
		if (privbuf[id]) free(privbuf[id]);
		buf[id] = privbuf[id] = malloc(size);
		memcpy(buf[id], ptr, size);
		buf_size[id] = size;
	}

	void SetBuffer(int id, void* ptr, int32 size)
	{
		if (privbuf[id]) free(privbuf[id]);
		privbuf[id] = nullptr;
		buf[id] = ptr;
		buf_size[id] = size;
	}


} comm;

//coroutines
cothread_t co_control, co_emu, co_emu_suspended;

//internal state
bool audio_en = false;
static const int AUDIOBUFFER_SIZE = 44100 * 2;
uint16_t audiobuffer[AUDIOBUFFER_SIZE];
int audiobuffer_idx = 0;
Action CMD_cb;

void BREAK(eMessage msg)
{
	comm.status = eStatus_BRK;
	comm.reason = msg;
	co_emu_suspended = co_active();
	co_switch(co_control);
	comm.status = eStatus_CMD;
}

void snes_video_refresh(const uint32_t *data, unsigned width, unsigned height)
{
	comm.width = width;
	comm.height = height;
	comm.ptr = (void*)data;

	BREAK(eMessage_SIG_video_refresh);
}

void do_SIG_audio_flush()
{
	comm.ptr = audiobuffer;
	comm.size = audiobuffer_idx;
	BREAK(eMessage_SIG_audio_flush);
	audiobuffer_idx = 0;
}

//this is the raw callback from the emulator internals when a new audio sample is available
void snes_audio_sample(uint16_t left, uint16_t right)
{
	if(!audio_en) return;

	//if theres no room in the audio buffer, we need to send a flush signal
	if (audiobuffer_idx == AUDIOBUFFER_SIZE)
	{
		do_SIG_audio_flush();
	}

	audiobuffer[audiobuffer_idx++] = left;
	audiobuffer[audiobuffer_idx++] = right;
}

void snes_input_poll(void)
{
	BREAK(eMessage_SIG_input_poll);
}

int16_t snes_input_state(unsigned port, unsigned device, unsigned index, unsigned id)
{
	comm.port = port;
	comm.device = device;
	comm.index = index;
	comm.id = id;
	BREAK(eMessage_SIG_input_state);
	return comm.value;
}
void snes_input_notify(int index)
{
	comm.index = index;
	BREAK(eMessage_SIG_input_notify);
}

void snes_trace(uint32_t which, const char *msg)
{
	comm.value = which;
	comm.str = (char*) msg;
	BREAK(eMessage_SIG_trace_callback);
}

const char* snes_path_request(int slot, const char* hint)
{
	comm.slot = slot;
	comm.str= (char *)hint;
	BREAK(eMessage_SIG_path_request);
	return (const char*)comm.buf[0];
}

void snes_scanlineStart(int line)
{
	comm.scanline = line;
	BREAK(eMessage_BRK_scanlineStart);
}

void* snes_allocSharedMemory(const char* memtype, size_t amt)
{
	//its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
	//if(!running) return NULL;

	void* ret;

	if (strcmp(memtype, "CARTRIDGE_ROM") == 0)
		ret = alloc_sealed(amt);
	else
		ret = alloc_plain(amt);

	comm.str = (char*)memtype;
	comm.size = amt;
	comm.ptr = ret;
	
	BREAK(eMessage_SIG_allocSharedMemory);
	
	return comm.ptr;
}

void snes_freeSharedMemory(void* ptr)
{
	//its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
	//if(!running) return;
	
	if (!ptr) return;

	comm.ptr = ptr;

	BREAK(eMessage_SIG_freeSharedMemory);
}

static void debug_op_exec(uint24 addr)
{
	comm.addr = addr;
	BREAK(eMessage_BRK_hook_exec);
}

static void debug_op_read(uint24 addr)
{
	comm.addr = addr;
	BREAK(eMessage_BRK_hook_read);
}

static void debug_op_write(uint24 addr, uint8 value)
{
	comm.addr = addr;
	comm.value = value;
	BREAK(eMessage_BRK_hook_write);
}

static void debug_op_nmi()
{
	BREAK(eMessage_BRK_hook_nmi);
}

static void debug_op_irq()
{
	BREAK(eMessage_BRK_hook_irq);
}

void pwrap_init()
{
	//bsnes's interface initialization calls into this after initializing itself, so we can get a chance to mod it for pwrap functionalities
	snes_set_video_refresh(snes_video_refresh);
	snes_set_audio_sample(snes_audio_sample);
	snes_set_input_poll(snes_input_poll);
	snes_set_input_state(snes_input_state);
	snes_set_input_notify(snes_input_notify);
	snes_set_path_request(snes_path_request);
	snes_set_allocSharedMemory(snes_allocSharedMemory);
	snes_set_freeSharedMemory(snes_freeSharedMemory);
}

static void Analyze()
{
	//gather some "static" type information, so we dont have to poll annoyingly for it later
	comm.mapper = snes_get_mapper();
	comm.region = snes_get_region();
}

void CMD_LoadCartridgeNormal()
{
	const char* xml = (const char*)comm.buf[0];
	if(!xml[0]) xml = nullptr;
	bool ret = snes_load_cartridge_normal(xml, (const uint8_t*)comm.buf[1], comm.buf_size[1]);
	comm.value = ret?1:0;

	if(ret)
		Analyze();
}

void CMD_LoadCartridgeSGB()
{
	bool ret = snes_load_cartridge_super_game_boy((const char*)comm.buf[0], (const u8*)comm.buf[1], comm.buf_size[1], nullptr, (const u8*)comm.buf[2], comm.buf_size[2]);
	comm.value = ret ? 1 : 0;

	if(ret)
		Analyze();
}

void CMD_init()
{
	snes_init();

	SNES::input.connect(SNES::Controller::Port1, comm.inports[0]);
	SNES::input.connect(SNES::Controller::Port2, comm.inports[1]);
}

static void CMD_Run()
{
	do_SIG_audio_flush();

	//we could avoid this if we saved the current thread before jumping back to co_control, instead of always jumping back to co_emu
	//in effect, we're scrambling the scheduler
	//EDIT - well, we changed that, but.. we still want this probably, for debugging and stuff
	for (;;)
	{
		SNES::scheduler.sync = SNES::Scheduler::SynchronizeMode::None;
		SNES::scheduler.clearExitReason();
		SNES::scheduler.enter();
		if (SNES::scheduler.exit_reason() == SNES::Scheduler::ExitReason::FrameEvent)
		{
			SNES::video.update();
			break;
		}
		//not used yet
		if (SNES::scheduler.exit_reason() == SNES::Scheduler::ExitReason::DebuggerEvent)
			break;
	}

	do_SIG_audio_flush();
}

void QUERY_get_memory_size() {
	comm.value = snes_get_memory_size(comm.value);
}
void QUERY_peek() {
	if (comm.id == SNES_MEMORY_SYSBUS)
		comm.value = bus_read(comm.addr);
	else comm.value = snes_get_memory_data(comm.id)[comm.addr];
}
void QUERY_poke() {
	if (comm.id == SNES_MEMORY_SYSBUS)
		bus_write(comm.addr, comm.value);
	else snes_get_memory_data(comm.id)[comm.addr] = comm.value;
}
void QUERY_set_color_lut() {
	snes_set_color_lut((uint32_t*)comm.ptr);
}
void QUERY_GetMemoryIdName() {
	comm.str = (char* )snes_get_memory_id_name(comm.id);
}
void QUERY_state_hook_exec() {
	SNES::cpu.debugger.op_exec = comm.value ? debug_op_exec : hook<void(uint24)>();
}
void QUERY_state_hook_read() {
	SNES::cpu.debugger.op_read = comm.value ? debug_op_read : hook<void(uint24)>();
}
void QUERY_state_hook_write() {
	SNES::cpu.debugger.op_write = comm.value ? debug_op_write : hook<void(uint24, uint8)>();
}
void QUERY_state_hook_nmi() {
	SNES::cpu.debugger.op_nmi = comm.value ? debug_op_nmi : hook<void()>();
}
void QUERY_state_hook_irq() {
	SNES::cpu.debugger.op_irq = comm.value ? debug_op_irq : hook<void()>();
}
void QUERY_state_enable_trace() {
	snes_set_trace_callback(comm.value, snes_trace);
}
void QUERY_state_enable_scanline() {
	if (comm.value)
		snes_set_scanlineStart(snes_scanlineStart);
	else snes_set_scanlineStart(nullptr);
}
void QUERY_state_enable_audio() {
	audio_en = !!comm.value;
}
void QUERY_state_set_layer_enable() {
	snes_set_layer_enable(0, 0, !!comm.layerEnables.BG1_Prio0);
	snes_set_layer_enable(0, 1, !!comm.layerEnables.BG1_Prio1);
	snes_set_layer_enable(1, 0, !!comm.layerEnables.BG2_Prio0);
	snes_set_layer_enable(1, 1, !!comm.layerEnables.BG2_Prio1);
	snes_set_layer_enable(2, 0, !!comm.layerEnables.BG3_Prio0);
	snes_set_layer_enable(2, 1, !!comm.layerEnables.BG3_Prio1);
	snes_set_layer_enable(3, 0, !!comm.layerEnables.BG4_Prio0);
	snes_set_layer_enable(3, 1, !!comm.layerEnables.BG4_Prio1);
	snes_set_layer_enable(4, 0, !!comm.layerEnables.Obj_Prio0);
	snes_set_layer_enable(4, 1, !!comm.layerEnables.Obj_Prio1);
	snes_set_layer_enable(4, 2, !!comm.layerEnables.Obj_Prio2);
	snes_set_layer_enable(4, 3, !!comm.layerEnables.Obj_Prio3);
}
void QUERY_set_backdropColor() {
	snes_set_backdropColor((s32)comm.value);
}
void QUERY_peek_logical_register() {
	comm.value = snes_peek_logical_register(comm.id);
}
void QUERY_peek_cpu_regs() {
	comm.cpuregs.pc = (u32)SNES::cpu.regs.pc;
	comm.cpuregs.a = SNES::cpu.regs.a;
	comm.cpuregs.x = SNES::cpu.regs.x;
	comm.cpuregs.y = SNES::cpu.regs.y;
	comm.cpuregs.z = SNES::cpu.regs.z;
	comm.cpuregs.s = SNES::cpu.regs.s;
	comm.cpuregs.d = SNES::cpu.regs.d;
	comm.cpuregs.aa = (u32)SNES::cpu.aa;
	comm.cpuregs.rd = (u32)SNES::cpu.rd;
	comm.cpuregs.sp = SNES::cpu.sp;
	comm.cpuregs.dp = SNES::cpu.dp;
	comm.cpuregs.db = SNES::cpu.regs.db;
	comm.cpuregs.mdr = SNES::cpu.regs.mdr;
	comm.cpuregs.vector = SNES::cpu.regs.vector;
	comm.cpuregs.p = SNES::cpu.regs.p;
	comm.cpuregs.nothing = 0;
}
void QUERY_peek_set_cdl() {
	for (int i = 0; i<eCDLog_AddrType_NUM; i++)
	{
		cdlInfo.blocks[i] = (uint8*)comm.cdl_ptr[i];
		cdlInfo.blockSizes[i] = comm.cdl_size[i];
	}
}

const Action kHandlers_CMD[] = {
	CMD_init,
	snes_power,
	snes_reset,
	CMD_Run,
	nullptr,
	nullptr,
	CMD_LoadCartridgeNormal,
	CMD_LoadCartridgeSGB,
	snes_term,
	snes_unload_cartridge,
};

const Action kHandlers_QUERY[] = {
	QUERY_get_memory_size, //eMessage_QUERY_get_memory_size TODO - grab during bootup (for all possible memdomains)
	QUERY_peek,
	QUERY_poke,
	nullptr, //eMessage_QUERY_serialize_size TODO - grab during bootup/reset (for all possible memdomains)
	QUERY_set_color_lut,
	QUERY_GetMemoryIdName, //snes_get_memory_id_name TODO - grab during bootup (for all possible memdomains)
	QUERY_state_hook_exec, //eMessage_QUERY_state_hook_exec
	QUERY_state_hook_read, //eMessage_QUERY_state_hook_read
	QUERY_state_hook_write, //eMessage_QUERY_state_hook_write
	QUERY_state_hook_nmi, //eMessage_QUERY_state_hook_nmi
	QUERY_state_hook_irq, //eMessage_QUERY_state_hook_irq
	QUERY_state_enable_trace, //eMessage_QUERY_enable_trace TODO - consolidate enable flags
	QUERY_state_enable_scanline, //eMessage_QUERY_enable_scanline TODO - consolidate enable flags
	QUERY_state_enable_audio, //eMessage_QUERY_enable_audio TODO - consolidate enable flags
	QUERY_state_set_layer_enable, //eMessage_QUERY_set_layer_enable
	QUERY_set_backdropColor, //eMessage_QUERY_set_backdropColor
	QUERY_peek_logical_register, //eMessage_QUERY_peek_logical_register
	QUERY_peek_cpu_regs, //eMessage_QUERY_peek_cpu_regs
	QUERY_peek_set_cdl, //eMessage_QUERY_set_cdl
};

//all this does is run commands on the emulation thread infinitely forever
//(I should probably make a mechanism for bailing...)
void new_emuthread()
{
	for (;;)
	{
		//process the current CMD
		CMD_cb();

		//when that returned, we're definitely done with the CMD--so we're now IDLE
		comm.status = eStatus_Idle;

		co_switch(co_control);
	}
}

//------------------------------------------------
//DLL INTERFACE

#include <emulibc.h>
#define EXPORT extern "C" ECL_EXPORT

EXPORT void* DllInit()
{
	#define T(s,n) static_assert(offsetof(CommStruct,s)==n,#n)
	T(cmd, 0);
	T(status, 4);
	T(reason, 8);
	T(str, 16);
	T(ptr, 24);
	T(id, 32);
	T(port, 48);
	T(width, 64);
	T(scanline, 72);
	T(inports, 76);
	T(buf, 88);
	T(buf_size, 112);
	T(cdl_ptr, 128);
	T(cdl_size, 160);
	T(cpuregs, 176);
	T(layerEnables, 208);
	T(region, 220);
	T(mapper, 224);
	// start of private stuff
	T(privbuf, 232);
	#undef T

	memset(&comm, 0, sizeof(comm));

	//make a coroutine thread to run the emulation in. we'll switch back to this cothread when communicating with the frontend
	co_control = co_active();
	if (co_emu)
	{
		// if this was called again, that's OK; delete the old emuthread
		co_delete(co_emu);
		co_emu = nullptr;
	}
	co_emu = co_create(32768 * sizeof(void*), new_emuthread);

	return &comm;
}

EXPORT void Message(eMessage msg)
{
	if (msg == eMessage_Resume)
	{
		cothread_t temp = co_emu_suspended;
		co_emu_suspended = NULL;
		co_switch(temp);
	}

	if (msg >= eMessage_CMD_FIRST && msg <= eMessage_CMD_LAST)
	{
		//CMD is only valid if status is idle
		if (comm.status != eStatus_Idle)
		{
			printf("ERROR: cmd during non-idle\n");
			return;
		}

		comm.status = eStatus_CMD;
		comm.cmd = msg;

		CMD_cb = kHandlers_CMD[msg - eMessage_CMD_FIRST - 1];
		co_switch(co_emu);

		//we could be in ANY STATE when we return from here
	}

	//QUERY can run any time
	//but... some of them might not be safe for re-entrancy.
	//later, we should have metadata for messages that indicates that
	if (msg >= eMessage_QUERY_FIRST && msg <= eMessage_QUERY_LAST)
	{
		Action cb = kHandlers_QUERY[msg - eMessage_QUERY_FIRST - 1];
		if (cb) cb();
	}
}


//receives the given buffer and COPIES it. use this for returning values from SIGs
EXPORT void CopyBuffer(int id, void* ptr, int32 size)
{
	comm.CopyBuffer(id, ptr, size);
}

//receives the given buffer and STASHES IT. use this (carefully) for sending params for CMDs
EXPORT void SetBuffer(int id, void* ptr, int32 size)
{
	comm.SetBuffer(id, ptr, size);
}

EXPORT void PostLoadState()
{
	SNES::ppu.flush_tiledata_cache();
}

int main()
{
	return 0;
}
