#define SNES_EXPORT extern "C" dllexport
//TODO - find out somehow when the parent process is gone (check a pid or some slot on that for existence and match vs logged value?)
//         we cant just check the state of a socket or named pipe or whatever in the case of IPCRingBuffer

//TODO - clean up signaling namespaces.. hooks vs callbacks vs etc. (unify them, at least)
//maybe turn into signal vs break concept (signals would be synchronous [expecting the frontend to always field them immediately and allow execution to resume immediately]) vs breaks (which the frontend receives control after)
//also a COMMAND concept (comes from frontend.. run, init, etc.)
//
//TODO - consolidate scanline breakpoint into this, with something like a set_break(type,address,size)  which could be used in the future for read/write/execute but for now, for scanline (address would be the scanline)
//
//TODO - factor out modular components (ringbuffer and the like)

//types of messages:
//cmd: frontend->core: "command to core" a command from the frontend which causes emulation to proceed. when sending a command, the frontend should wait for an eMessage_BRK_Complete before proceeding, although a debugger might proceed after any BRK
//query: frontend->core: "query to core" a query from the frontend which can (and should) be satisfied immediately by the core but which does not result in emulation processes (notably, nothing resembling a CMD and nothing which can trigger a BRK)
//sig: core->frontend: "core signal" a synchronous operation called from the emulation process which the frontend should handle immediately without issuing any calls into the core
//brk: core->frontend: "core break" the emulation process has suspended. the frontend is free to do whatever it wishes.
//(and there are other assorted special messages...)


#include <Windows.h>

#define LIBSNES_IMPORT
#include "snes/snes.hpp"
#include "libsnes.hpp"

#include <libco/libco.h>

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <map>
#include <string>
#include <vector>

typedef int(__cdecl *snesVideoRefresh_t)(int w, int h, bool get);
snesVideoRefresh_t snesVideoRefreshManaged;

typedef int(__cdecl *snesAudioFlush_t)(int nsamples);
snesAudioFlush_t snesAudioFlushManaged;

typedef void(__cdecl *snesInputNotify_t)(int index);
snesInputNotify_t snesInputNotifyManaged;

typedef void(__cdecl *snesFreeSharedMemory_t)(const char *name);
snesFreeSharedMemory_t snesFreeSharedMemoryManaged;

typedef short(__cdecl *snesInputState_t)(int, int, int, int);
snesInputState_t snesInputStateManaged;

typedef void(__cdecl *snesTraceCallback_t)(const char *);
snesTraceCallback_t snesTraceCallbackManaged;

typedef const char *(__cdecl *snesPathRequest_t)(int slot, const char *hint);
snesPathRequest_t snesPathRequestManaged;

typedef void(__cdecl *snesScanlineStart_t)(int line);
snesScanlineStart_t snesScanlineStartManaged;

typedef void(__cdecl *snesHook_t)(unsigned int addr);
snesHook_t snesHookExecManaged;
snesHook_t snesHookReadManaged;

typedef void(__cdecl *snesHookWrite_t)(unsigned int addr, byte value);
snesHookWrite_t snesHookWriteManaged;



SNES_EXPORT void SetSnesHookExec(snesHook_t f)
{
	snesHookExecManaged = f;
}

SNES_EXPORT void SetSnesHookRead(snesHook_t f)
{
	snesHookReadManaged = f;
}

SNES_EXPORT void SetSnesHookWrite(snesHookWrite_t f)
{
	snesHookWriteManaged = f;
}

SNES_EXPORT void SetSnesScanlineStart(snesScanlineStart_t f)
{
	snesScanlineStartManaged = f;
}

SNES_EXPORT void SetSnesPathRequest(snesPathRequest_t f)
{
	snesPathRequestManaged = f;
}

SNES_EXPORT void SetSnesTraceCallback(snesTraceCallback_t f)
{
	snesTraceCallbackManaged = f;
}
SNES_EXPORT void SetSnesVideoRefresh(snesVideoRefresh_t f)
{
	snesVideoRefreshManaged = f;
}
SNES_EXPORT void SetSnesAudioFlush(snesAudioFlush_t f)
{
	snesAudioFlushManaged = f;
}
SNES_EXPORT void SetSnesInputNotify(snesInputNotify_t f)
{
	snesInputNotifyManaged = f;
}
SNES_EXPORT void SetSnesFreeSharedMemory(snesFreeSharedMemory_t f)
{
	snesFreeSharedMemoryManaged = f;
}
SNES_EXPORT void SetSnesInputState(snesInputState_t f)
{
	snesInputStateManaged = f;
}

extern SNES::Interface *iface;

typedef uint8 u8;
typedef int32 s32;
typedef uint32 u32;
typedef uint16 u16;

enum eMessage : int32
{
	eMessage_NotSet,

	eMessage_Shutdown,

	eMessage_QUERY_serialize_size,
	eMessage_QUERY_dequeue_message,
	eMessage_QUERY_enable_scanline,
	eMessage_QUERY_set_backdropColor,
	eMessage_QUERY_peek_logical_register,
	eMessage_QUERY_peek_cpu_regs,
	eMessage_QUERY_set_cdl,
};

HANDLE hMapFile, hEvent;
void* hMapFilePtr;
static bool running = false;

void snes_video_refresh(const uint32_t *data, unsigned width, unsigned height)
{	
	int destOfs = 
		snesVideoRefreshManaged(width, height, true);
	char* buf = (char*)hMapFilePtr + destOfs;
	int bufsize = 512 * 480 * 4;
	memcpy(buf, data, bufsize);
	snesVideoRefreshManaged(width, height, false);
}

bool audio_en = false;
static const int AUDIOBUFFER_SIZE = 44100*2;
uint16_t audiobuffer[AUDIOBUFFER_SIZE];
int audiobuffer_idx = 0;

void SIG_FlushAudio()
{
	int nsamples = audiobuffer_idx;
	
	char* buf = (char *)hMapFilePtr + (snesAudioFlushManaged(nsamples));
	memcpy(buf, audiobuffer, nsamples * 2);
	//extra just in case we had to unexpectedly flush audio and then carry on with some other process... yeah, its rickety.

	audiobuffer_idx = 0;
}

//this is the raw callback from the emulator internals when a new audio sample is available
void snes_audio_sample(uint16_t left, uint16_t right)
{
	if(!audio_en) return;

	//if theres no room in the audio buffer, we need to send a flush signal
	if(audiobuffer_idx == AUDIOBUFFER_SIZE)
		SIG_FlushAudio();

	audiobuffer[audiobuffer_idx++] = left;
	audiobuffer[audiobuffer_idx++] = right;
}

void snes_input_poll(void)
{
	// empty?
}
int16_t snes_input_state(unsigned port, unsigned device, unsigned index, unsigned id)
{
	return snesInputStateManaged(port, device, index, id);
}
void snes_input_notify(int index)
{
	snesInputNotifyManaged(index);
}

void snes_trace(const char *msg)
{
	snesTraceCallbackManaged(msg);
}

char SNES_PATH_REQUEST_TEMP[2048];
const char* snes_path_request(int slot, const char* hint)
{
	strncpy(SNES_PATH_REQUEST_TEMP, snesPathRequestManaged(slot, hint), sizeof(SNES_PATH_REQUEST_TEMP));

	return SNES_PATH_REQUEST_TEMP;
}

void snes_scanlineStart(int line)
{
	snesScanlineStartManaged(line);
}

class SharedMemoryBlock
{
public:
	std::string memtype;
	HANDLE handle;
};

typedef const char *(__cdecl *allocSharedMemory_t)(const char *name, int size);
static std::map<void*,SharedMemoryBlock*> memHandleTable;

allocSharedMemory_t AllocSharedMemoryManaged;
SNES_EXPORT void SetAllocSharedMemory(allocSharedMemory_t f)
{
	AllocSharedMemoryManaged = f;
}

void* implementation_snes_allocSharedMemory(const char *memtype, size_t amt)
{

	if(!running)
	{
		return NULL;
	}

	std::string blockname = AllocSharedMemoryManaged(memtype, amt);

	auto mapfile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, blockname.c_str());
	
	if(mapfile == INVALID_HANDLE_VALUE)
		return NULL;

	auto ptr = MapViewOfFile(mapfile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

	auto smb = new SharedMemoryBlock();
	smb->memtype = memtype;
	smb->handle = mapfile;

	memHandleTable[ptr] = smb;

	return ptr;
}

void* snes_allocSharedMemory(const char* memtype, size_t amt)
{
	//its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
	if(!running) return NULL;

	return implementation_snes_allocSharedMemory(memtype, amt);
}

void implementation_snes_freeSharedMemory(void *ptr)
{
	if(!ptr) return;
	auto smb = memHandleTable.find(ptr)->second;
	UnmapViewOfFile(ptr);
	CloseHandle(smb->handle);
	//printf("WritePipe(eMessage_SIG_freeSharedMemory);\n");
	snesFreeSharedMemoryManaged(smb->memtype.c_str());
}

void snes_freeSharedMemory(void* ptr)
{
	//its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
	if(!running) return;

	implementation_snes_freeSharedMemory(ptr);
}

void InitBsnes()
{
	//setup all hooks to forward messages to the frontend
	snes_set_video_refresh(snes_video_refresh);
	snes_set_audio_sample(snes_audio_sample);
	snes_set_input_poll(snes_input_poll);
	snes_set_input_state(snes_input_state);
	snes_set_input_notify(snes_input_notify);
	snes_set_path_request(snes_path_request);
	
	snes_set_allocSharedMemory(snes_allocSharedMemory);
	snes_set_freeSharedMemory(snes_freeSharedMemory);
}


static void debug_op_exec(uint24 addr)
{
	snesHookExecManaged(addr);
}

static void debug_op_read(uint24 addr)
{
	snesHookReadManaged(addr);
}

static void debug_op_write(uint24 addr, uint8 value)
{
	snesHookWriteManaged(addr, value);
}

static void debug_op_nmi()
{
	// not supported yet
}

static void debug_op_irq()
{
	// not supported yet
}

void HandleMessage_QUERY(eMessage msg)
{
}

SNES_EXPORT void QUERY_enable_trace(bool state)
{
	if (!!state)
		snes_set_trace_callback(snes_trace);
	else snes_set_trace_callback(NULL);
}
SNES_EXPORT const char *QUERY_library_id(void)
{
	return snes_library_id();
}
SNES_EXPORT unsigned int QUERY_library_revision_major(void)
{
	return snes_library_revision_major();
}
SNES_EXPORT unsigned int QUERY_library_revision_minor(void)
{
	return snes_library_revision_minor();
}
SNES_EXPORT char QUERY_snes_get_region()
{
	return snes_get_region();
}
SNES_EXPORT u32 QUERY_snes_get_memory_size(u32 which)
{
	return snes_get_memory_size(which);
}
SNES_EXPORT void QUERY_get_memory_data(unsigned int id, char *out)
{
	memcpy(out, snes_get_memory_data(id), snes_get_memory_size(id));
}
SNES_EXPORT uint8_t QUERY_peek(u32 id, u32 addr)
{
	if (id == SNES_MEMORY_SYSBUS)
		return bus_read(addr);
	else
		return snes_get_memory_data(id)[addr];
}
SNES_EXPORT void QUERY_poke(u32 id, u32 addr, uint8_t val)
{
	if (id == SNES_MEMORY_SYSBUS)
		bus_write(addr, val);
	else
		snes_get_memory_data(id)[addr] = val;
}
SNES_EXPORT void QUERY_snes_set_layer_enable(s32 layer, s32 priority, bool enable)
{
	snes_set_layer_enable(layer, priority, enable);
}
SNES_EXPORT void QUERY_set_state_hook_exec(bool enable)
{
	SNES::cpu.debugger.op_exec = enable ? debug_op_exec : hook<void(uint24)>();
}
SNES_EXPORT void QUERY_set_state_hook_read(bool state)
{
	SNES::cpu.debugger.op_read = state ? debug_op_read : hook<void(uint24)>();
}
SNES_EXPORT void QUERY_set_state_hook_write(bool state)
{
	SNES::cpu.debugger.op_write = state ? debug_op_write : hook<void(uint24, uint8)>();
}
SNES_EXPORT void QUERY_enable_audio(bool enable)
{
	audio_en = enable;
}
SNES_EXPORT void QUERY_set_color_lut(uint32_t blob[])
{
	snes_set_color_lut(blob);
}
SNES_EXPORT char QUERY_get_mapper()
{
	return snes_get_mapper();
}
SNES_EXPORT void CMD_run()
{
	SIG_FlushAudio();

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

	SIG_FlushAudio();
}
SNES_EXPORT const char *QUERY_get_memory_id(uint32 which)
{
	return snes_get_memory_id_name(which);
}
SNES_EXPORT void QUERY_enable_scanline(bool enable)
{
	if (enable)
		snes_set_scanlineStart(snes_scanlineStart);
	else snes_set_scanlineStart(NULL);
}
SNES_EXPORT int QUERY_peek_logical_register(int which)
{
	return snes_peek_logical_register(which);
}
SNES_EXPORT void QUERY_set_backdropColor(s32 col)
{
	snes_set_backdropColor(col);
}
typedef struct {
	u32 pc;
	u16 a, x, y, z, s, d, vector; //7x
	u8 p, nothing;
	u32 aa, rd;
	u8 sp, dp, db, mdr;
} cpuregs_t;
SNES_EXPORT void QUERY_peek_cpu_regs(cpuregs_t &cpuregs)
{
	cpuregs.pc = (u32)SNES::cpu.regs.pc;
	cpuregs.a = SNES::cpu.regs.a;
	cpuregs.x = SNES::cpu.regs.x;
	cpuregs.y = SNES::cpu.regs.y;
	cpuregs.z = SNES::cpu.regs.z;
	cpuregs.s = SNES::cpu.regs.s;
	cpuregs.d = SNES::cpu.regs.d;
	cpuregs.aa = (u32)SNES::cpu.aa;
	cpuregs.rd = (u32)SNES::cpu.rd;
	cpuregs.sp = SNES::cpu.sp;
	cpuregs.dp = SNES::cpu.dp;
	cpuregs.db = SNES::cpu.regs.db;
	cpuregs.mdr = SNES::cpu.regs.mdr;
	cpuregs.vector = SNES::cpu.regs.vector;
	cpuregs.p = SNES::cpu.regs.p;
	cpuregs.nothing = 0;
}

SNES_EXPORT u32 QUERY_snes_serialize_size()
{
	return snes_serialize_size();
}

SNES_EXPORT void QUERY_set_cdl(int i, uint8_t *block, uint32_t size)
{
	cdlInfo.blocks[i] = block;
	cdlInfo.blockSizes[i] = size;
}


void OpenConsole() 
{
	AllocConsole();
	freopen("CONOUT$", "w", stdout);
	freopen("CONOUT$", "w", stderr);
	freopen("CONIN$", "r", stdin);
}

SNES_EXPORT bool CMD_load_cartridge(const char *xml, const uint8_t *rom, uint32_t romsize)
{
	return snes_load_cartridge_normal(xml,rom,romsize);
}

SNES_EXPORT bool CMD_load_cart_sgb(
							const char *rom_xml_, uint8 *rom_data, uint32 rom_length, 
							const char *dmg_xml_, uint8 *dmg_data, uint32 dmg_length)
{
	std::string rom_xml = rom_xml_;
	const char* rom_xmlptr = NULL;
	if(rom_xml != "") rom_xmlptr = rom_xml.c_str();

	std::string dmg_xml = dmg_xml_;
	const char* dmg_xmlptr = NULL;
	if(dmg_xml != "") dmg_xmlptr = dmg_xml.c_str();

	return snes_load_cartridge_super_game_boy(rom_xmlptr, rom_data, rom_length, dmg_xmlptr, dmg_data, dmg_length);
}

SNES_EXPORT bool CMD_serialize(s32 size, s32 destOfs)
{
	char* buf = (char*)hMapFilePtr + destOfs;
	return snes_serialize((uint8_t*)buf,size);
}

SNES_EXPORT bool CMD_unserialize(s32 size, s32 destOfs)
{
	char* buf = (char*)hMapFilePtr + destOfs;
	return snes_unserialize((uint8_t*)buf	,size);
}
SNES_EXPORT void CMD_init()
{
	snes_init();
}
SNES_EXPORT void CMD_power()
{
	snes_power();
}
SNES_EXPORT void CMD_reset()
{
	snes_reset();
}
SNES_EXPORT void CMD_term()
{
	snes_term();
}
SNES_EXPORT void CMD_unload_cartridge()
{
	snes_unload_cartridge();
}

extern "C" dllexport BOOL WINAPI DllMain(_In_ HINSTANCE hinstDLL, _In_ DWORD     fdwReason, _In_ LPVOID    lpvReserved)
{
	return TRUE;
}

extern "C" dllexport bool __cdecl DllInit(const char* ipcname)
{
	printf("NEW INSTANCE\n");

	hMapFile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, ipcname);
	if(hMapFile == INVALID_HANDLE_VALUE)
		return false;

	hMapFilePtr = MapViewOfFile(hMapFile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

	running = true;
	printf("running\n");

	return true;
}


void pwrap_init()
{
	//bsnes's interface initialization calls into this after initializing itself, so we can get a chance to mod it for pwrap functionalities
	InitBsnes();
}