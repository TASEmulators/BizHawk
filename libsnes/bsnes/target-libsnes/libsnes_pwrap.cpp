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

extern SNES::Interface *iface;

typedef uint8 u8;
typedef int32 s32;
typedef uint32 u32;
typedef uint16 u16;

enum eMessage : int32
{
	eMessage_NotSet,

	eMessage_SetBuffer,
	eMessage_BeginBufferIO,
	eMessage_EndBufferIO,
	eMessage_ResumeAfterBRK, //resumes execution of the core, after a BRK. no change to current CMD
	eMessage_Shutdown,

	eMessage_QUERY_library_id,
	eMessage_QUERY_library_revision_major,
	eMessage_QUERY_library_revision_minor,
	eMessage_QUERY_get_region,
	eMessage_QUERY_get_mapper,
	eMessage_QUERY_get_memory_size,
	eMessage_QUERY_get_memory_data, //note: this function isnt used and hasnt been tested in a while
	eMessage_QUERY_peek,
	eMessage_QUERY_poke,
	eMessage_QUERY_serialize_size,
	eMessage_QUERY_poll_message,
	eMessage_QUERY_dequeue_message,
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

	eMessage_CMD_FIRST,
	eMessage_CMD_init,
	eMessage_CMD_power,
	eMessage_CMD_reset,
	eMessage_CMD_run,
	eMessage_CMD_serialize,
	eMessage_CMD_unserialize,
	eMessage_CMD_load_cartridge_normal,
	eMessage_CMD_load_cartridge_super_game_boy,
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
	eMessage_SIG_allocSharedMemory, //?
	eMessage_SIG_freeSharedMemory, //?

	eMessage_BRK_Complete,
	eMessage_BRK_hook_exec,
	eMessage_BRK_hook_read,
	eMessage_BRK_hook_write,
	eMessage_BRK_hook_nmi,
	eMessage_BRK_hook_irq,
	eMessage_BRK_scanlineStart, //implemented as a BRK because that's really what it is, its just a graphical event and not a CPU event
};


enum eEmulationExitReason
{
	eEmulationExitReason_NotSet,
	eEmulationExitReason_BRK,
	eEmulationExitReason_SIG,
	eEmulationExitReason_CMD_Complete,
};

enum eEmulationCallback
{
	eEmulationCallback_snes_video_refresh,
	eEmulationCallback_snes_audio_flush,
	eEmulationCallback_snes_scanline,
	eEmulationCallback_snes_input_poll,
	eEmulationCallback_snes_input_state,
	eEmulationCallback_snes_input_notify,
	eEmulationCallback_snes_path_request,
	
	eEmulationCallback_snes_allocSharedMemory,
	eEmulationCallback_snes_freeSharedMemory,

	eEmulationCallback_snes_trace
};

struct EmulationControl
{
	volatile eMessage command;
	volatile eEmulationExitReason exitReason;
	
	//the result code for a CMD
	s32 cmd_result;

	union
	{
		struct
		{
			volatile eMessage hookExitType;
			uint32 hookAddr;
			uint8 hookValue;
		};

		struct
		{
			volatile eEmulationCallback exitCallbackType;
			union
			{
				struct
				{
					const uint32_t *data;
					unsigned width;
					unsigned height;
				} cb_video_refresh_params;
				struct
				{
					int32_t scanline;
				} cb_scanline_params;
				struct
				{
					unsigned port, device, index, id;
					int16_t result;
				} cb_input_state_params;
				struct
				{
					int index;
				} cb_input_notify_params;
				struct
				{
					int slot;
					const char* hint;
					//yuck
					char result[MAX_PATH];
				} cb_path_request_params;
				struct
				{
					const char* memtype;
					size_t amt;
					void* result;
				} cb_allocSharedMemory_params;
				struct
				{
					void* ptr;
				} cb_freeSharedMemory_params;
				struct
				{
					const char* msg;
				} cb_trace_params;
			};
		};
	};
};

static EmulationControl s_EmulationControl;

class IPCRingBuffer
{
private:
	HANDLE mmf;
	u8* mmvaPtr;
	volatile u8* begin;
	volatile s32* head, *tail;
	int bufsize;

	//this code depends on conventional interpretations of volatile and sequence points which are unlikely to be violated on any system an emulator would be running on

	void Setup(int size)
	{
		bool init = size != -1;
		Owner = init;

		mmvaPtr = (u8*)MapViewOfFile(mmf, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

		//setup management area
		head = (s32*)mmvaPtr;
		tail = (s32*)mmvaPtr + 1;
		s32* bufsizeptr = (s32*)mmvaPtr + 2;
		begin = mmvaPtr + 12;

		if (init)
			*bufsizeptr = bufsize = size - 12;
		else bufsize = *bufsizeptr;
	}
		
	void WaitForWriteCapacity(int amt)
	{
		for (; ; )
		{
			//dont return when available == amt because then we would consume the buffer and be unable to distinguish between full and empty
			if (Available() > amt)
				return;
			//this is a greedy spinlock.
			SwitchToThread();
		}
	}

	int Size()
	{
		int h = *head;
		int t = *tail;
		int size = h - t;
		if (size >= bufsize)
		{
			//shouldnt be possible for size to be anything but bufsize here
			size = 0;
		}
		else if (size < 0) size += bufsize;
		return size;
	}

	int Available()
	{
		return bufsize - Size();
	}

public:
	bool Owner;

	//void Allocate(int size) //not supported

	void Open(const std::string& id)
	{
		HANDLE h = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, id.c_str());
		if(h == INVALID_HANDLE_VALUE)
			return;

		mmf = h;
				
		Setup(-1);
	}

	~IPCRingBuffer()
	{
		if (mmf == NULL) return;
		CloseHandle(mmf);
		mmf = NULL;
	}

	int WaitForSomethingToRead()
	{
		for (; ; )
		{
			int available = Size();
			if (available > 0)
				return available;
			//this is a greedy spinlock.
			SwitchToThread();
			//NOTE: it's annoying right now because libsnes processes die and eat a whole core.
			//we need to gracefully exit somehow
		}
	}

	void Write(const void* ptr, int amt)
	{
		u8* bptr = (u8*)ptr;
		int ofs = 0;
		while (amt > 0)
		{
			int todo = amt;

			//make sure we don't write a big chunk beyond the end of the buffer
			int remain = bufsize - *head;
			if (todo > remain) todo = remain;

			//dont request the entire buffer. we would never get that much available, because we never completely fill up the buffer
			if (todo > bufsize - 1) todo = bufsize - 1;

			//a super efficient approach would chunk this several times maybe instead of waiting for the buffer to be emptied before writing again. but who cares
			WaitForWriteCapacity(todo);

			//messages are likely to be small. we should probably just loop to copy in here. but for now..
			memcpy((u8*)begin + *head, bptr + ofs, todo);

			amt -= todo;
			ofs += todo;
			*head += todo;
			if (*head >= bufsize) *head -= bufsize;
		}
	}

	void Read(void* ptr, int amt)
	{
		u8* bptr = (u8*)ptr;
		int ofs = 0;
		while (amt > 0)
		{
			int available = WaitForSomethingToRead();
			int todo = amt;
			if (todo > available) todo = available;

			//make sure we don't read a big chunk beyond the end of the buffer
			int remain = bufsize - *tail;
			if (todo > remain) todo = remain;

			//messages are likely to be small. we should probably just loop to copy in here. but for now..
			memcpy(bptr + ofs, (u8*)begin + *tail, todo);

			amt -= todo;
			ofs += todo;
			*tail += todo;
			if (*tail >= bufsize) *tail -= bufsize;
		}
	}
}; //class IPCRingBuffer

static bool bufio = false;
static IPCRingBuffer *rbuf = NULL, *wbuf = NULL;

HANDLE hPipe, hMapFile, hEvent;
void* hMapFilePtr;
static bool running = false;

cothread_t co_control, co_emu, co_emu_suspended;

#define SETCONTROL \
{ \
	co_emu_suspended = co_active(); \
	co_switch(co_control); \
} 

#define SETEMU \
{ \
	cothread_t temp = co_emu_suspended; \
	co_emu_suspended = NULL; \
	co_switch(temp); \
}


void ReadPipeBuffer(void* buf, int len)
{
	if(bufio)
	{
		rbuf->Read(buf,len);
		return;
	}
	DWORD bytesRead;
	BOOL result = ReadFile(hPipe, buf, len, &bytesRead, NULL);
	if(!result || bytesRead != len)
		exit(1);
}

template<typename T> T ReadPipe()
{
	T ret;
	ReadPipeBuffer(&ret,sizeof(ret));
	return ret;
}

char* ReadPipeSharedPtr()
{
	return (char*)hMapFilePtr + ReadPipe<int>();
}

template<> bool ReadPipe<bool>()
{
	return !!ReadPipe<char>();
}


void WritePipeBuffer(const void* buf, int len)
{
	if(co_active() != co_control)
	{
		printf("WARNING: WRITING FROM NON-CONTROL THREAD\n");
	}
	//static FILE* outf = NULL;
	//if(!outf) outf = fopen("c:\\trace.bin","wb"); fwrite(buf,1,len,outf); fflush(outf);

	if(bufio)
	{
		wbuf->Write(buf,len);
		return;
	}

	DWORD bytesWritten;
	BOOL result = WriteFile(hPipe, buf, len, &bytesWritten, NULL);
	if(!result || bytesWritten != len)
		exit(1);
}

//remove volatile qualifier...... crazy?
template<typename T> void WritePipe(volatile const T& val)
{
	WritePipeBuffer((T*)&val, sizeof(val));
}

template<typename T> void WritePipe(const T& val)
{
	WritePipeBuffer(&val, sizeof(val));
}

void WritePipeString(const char* str)
{
	int len = strlen(str);
	WritePipe(len);
	WritePipeBuffer(str,len);
}

std::string ReadPipeString()
{
	int len = ReadPipe<int>();
	std::string ret;
	ret.resize(len);
	if(len!=0)
		ReadPipeBuffer(&ret[0],len);
	return ret;
}

typedef std::vector<char> Blob;

void WritePipeBlob(void* buf, int len)
{
	WritePipe(len);
	WritePipeBuffer(buf,len);
}

Blob ReadPipeBlob()
{
	int len = ReadPipe<int>();
	Blob ret;
	ret.resize(len);
	if(len!=0)
		ReadPipeBuffer(&ret[0],len);
	return ret;
}

void snes_video_refresh(const uint32_t *data, unsigned width, unsigned height)
{
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_video_refresh;
	s_EmulationControl.cb_video_refresh_params.data = data;
	s_EmulationControl.cb_video_refresh_params.width = width;
	s_EmulationControl.cb_video_refresh_params.height = height;

	SETCONTROL;
}

bool audio_en = false;
static const int AUDIOBUFFER_SIZE = 44100*2;
uint16_t audiobuffer[AUDIOBUFFER_SIZE];
int audiobuffer_idx = 0;

void SIG_FlushAudio()
{
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_audio_flush;
	SETCONTROL;
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
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_input_poll;
	SETCONTROL;
}
int16_t snes_input_state(unsigned port, unsigned device, unsigned index, unsigned id)
{
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_input_state;
	s_EmulationControl.cb_input_state_params.port = port;
	s_EmulationControl.cb_input_state_params.device = device;
	s_EmulationControl.cb_input_state_params.index = index;
	s_EmulationControl.cb_input_state_params.id = id;
	SETCONTROL;
	return s_EmulationControl.cb_input_state_params.result;
}
void snes_input_notify(int index)
{
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_input_notify;
	s_EmulationControl.cb_input_notify_params.index = index;
	SETCONTROL;
}

void snes_trace(const char *msg)
{
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_trace;
	s_EmulationControl.cb_trace_params.msg = msg;
	SETCONTROL;
}

const char* snes_path_request(int slot, const char* hint)
{
	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_path_request;
	s_EmulationControl.cb_path_request_params.slot = slot;
	s_EmulationControl.cb_path_request_params.hint = hint;
	SETCONTROL;
	
	return (const char*)s_EmulationControl.cb_path_request_params.result;
}

void RunControlMessageLoop();
void snes_scanlineStart(int line)
{
	s_EmulationControl.exitReason = eEmulationExitReason_BRK;
	s_EmulationControl.hookExitType = eMessage_BRK_scanlineStart;
	s_EmulationControl.cb_scanline_params.scanline = line;
	SETCONTROL;
}

class SharedMemoryBlock
{
public:
	std::string memtype;
	HANDLE handle;
};

static std::map<void*,SharedMemoryBlock*> memHandleTable;

void* implementation_snes_allocSharedMemory()
{
	const char* memtype = s_EmulationControl.cb_allocSharedMemory_params.memtype;
	size_t amt = s_EmulationControl.cb_allocSharedMemory_params.amt;

	if(!running)
	{
		s_EmulationControl.cb_allocSharedMemory_params.result = NULL;
		return NULL;
	}

	//printf("WritePipe(eMessage_SIG_allocSharedMemory)\n");
	WritePipe(eMessage_SIG_allocSharedMemory);
	WritePipeString(memtype);
	WritePipe(amt);
	
	std::string blockname = ReadPipeString();

	auto mapfile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, blockname.c_str());
	
	if(mapfile == INVALID_HANDLE_VALUE)
		return NULL;

	auto ptr = MapViewOfFile(mapfile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

	auto smb = new SharedMemoryBlock();
	smb->memtype = memtype;
	smb->handle = mapfile;

	memHandleTable[ptr] = smb;
	
	s_EmulationControl.cb_allocSharedMemory_params.result = ptr;

	return ptr;
}

void* snes_allocSharedMemory(const char* memtype, size_t amt)
{
	//its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
	if(!running) return NULL;

	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_allocSharedMemory;
	s_EmulationControl.cb_allocSharedMemory_params.memtype = memtype;
	s_EmulationControl.cb_allocSharedMemory_params.amt = amt;
	SETCONTROL;
	
	return s_EmulationControl.cb_allocSharedMemory_params.result;
}

void implementation_snes_freeSharedMemory()
{
	void* ptr = s_EmulationControl.cb_freeSharedMemory_params.ptr;
	if(!ptr) return;
	auto smb = memHandleTable.find(ptr)->second;
	UnmapViewOfFile(ptr);
	CloseHandle(smb->handle);
	//printf("WritePipe(eMessage_SIG_freeSharedMemory);\n");
	WritePipe(eMessage_SIG_freeSharedMemory);
	WritePipeString(smb->memtype.c_str());
}

void snes_freeSharedMemory(void* ptr)
{
	//its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
	if(!running) return;

	s_EmulationControl.exitReason = eEmulationExitReason_SIG;
	s_EmulationControl.exitCallbackType = eEmulationCallback_snes_freeSharedMemory;
	s_EmulationControl.cb_freeSharedMemory_params.ptr = ptr;
	SETCONTROL;
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
	s_EmulationControl.exitReason = eEmulationExitReason_BRK;
	s_EmulationControl.hookExitType = eMessage_BRK_hook_exec;
	s_EmulationControl.hookAddr = (uint32)addr;
	SETCONTROL;
	//WritePipe(eMessage_snes_cb_hook_exec);
	//WritePipe((uint32)addr);
}

static void debug_op_read(uint24 addr)
{
	s_EmulationControl.exitReason = eEmulationExitReason_BRK;
	s_EmulationControl.hookExitType = eMessage_BRK_hook_read;
	s_EmulationControl.hookAddr = (uint32)addr;
	SETCONTROL;
	//WritePipe(eMessage_snes_cb_hook_read);
	//WritePipe((uint32)addr);
}

static void debug_op_write(uint24 addr, uint8 value)
{
	s_EmulationControl.exitReason = eEmulationExitReason_BRK;
	s_EmulationControl.hookExitType = eMessage_BRK_hook_write;
	s_EmulationControl.hookAddr = (uint32)addr;
	s_EmulationControl.hookValue = value;
	SETCONTROL;
	//WritePipe(eMessage_snes_cb_hook_write);
	//WritePipe((uint32)addr);
	//WritePipe(value);
}

static void debug_op_nmi()
{
	WritePipe(eMessage_BRK_hook_nmi);
}

static void debug_op_irq()
{
	WritePipe(eMessage_BRK_hook_irq);
}

void HandleMessage_QUERY(eMessage msg)
{
}

bool Handle_QUERY(eMessage msg)
{
	switch(msg)
	{
	default:
		return false;

	case eMessage_QUERY_library_id:
		WritePipeString(snes_library_id());
		break;
	case eMessage_QUERY_library_revision_major:
		WritePipe(snes_library_revision_major()); 
		break;
	case eMessage_QUERY_library_revision_minor:
		WritePipe(snes_library_revision_minor()); 
		break;

	case eMessage_QUERY_get_region:
		WritePipe((char)snes_get_region());
		break;

	case eMessage_QUERY_get_mapper:
		WritePipe(snes_get_mapper());
		break;

	case eMessage_QUERY_get_memory_size:
		WritePipe((u32)snes_get_memory_size(ReadPipe<u32>()));
		break;

	case eMessage_QUERY_get_memory_data:
		{
			unsigned int id = ReadPipe<u32>();
			char* dstbuf = ReadPipeSharedPtr();
			uint8_t* srcbuf = snes_get_memory_data(id);
			memcpy(dstbuf,srcbuf,snes_get_memory_size(id));
			WritePipe(eMessage_BRK_Complete);
			break;
		}

	case eMessage_QUERY_peek:
		{
			int id = ReadPipe<s32>();
			unsigned int addr = ReadPipe<u32>();
			uint8_t ret;
			if(id == SNES_MEMORY_SYSBUS)
				ret = bus_read(addr);
			else ret = snes_get_memory_data(id)[addr];
			WritePipe(ret);
		}
		break;

	case eMessage_QUERY_poke:
		{
			int id = ReadPipe<s32>();
			unsigned int addr = ReadPipe<u32>();
			uint8_t val = ReadPipe<uint8_t>();
			if(id == SNES_MEMORY_SYSBUS)
				bus_write(addr,val);
			else snes_get_memory_data(id)[addr] = val;
			break;
		}
		break;

	case eMessage_QUERY_serialize_size:
		WritePipe((u32)snes_serialize_size());
		break;
	case eMessage_QUERY_poll_message:
		//TBD
		WritePipe(-1);
		break;
	case eMessage_QUERY_dequeue_message:
		//TBD
		break;

	case eMessage_QUERY_set_color_lut:
		{
			auto blob = ReadPipeBlob();
			snes_set_color_lut((uint32_t*)&blob[0]);
			break;
		}
		break;

	case eMessage_QUERY_enable_trace:
		if(!!ReadPipe<char>())
			snes_set_trace_callback(snes_trace);
		else snes_set_trace_callback(NULL);
		break;

	case eMessage_QUERY_enable_scanline:
		if(ReadPipe<bool>())
			snes_set_scanlineStart(snes_scanlineStart);
		else snes_set_scanlineStart(NULL);
		break;

	case eMessage_QUERY_enable_audio:
		audio_en = ReadPipe<bool>();
		break;

	case eMessage_QUERY_set_layer_enable:
		{
			int layer = ReadPipe<s32>();
			int priority = ReadPipe<s32>();
			bool enable = ReadPipe<bool>();
			snes_set_layer_enable(layer,priority,enable);
			break;
		}

	case eMessage_QUERY_set_backdropColor:
		snes_set_backdropColor(ReadPipe<s32>());
		break;

	case eMessage_QUERY_peek_logical_register:
		WritePipe(snes_peek_logical_register(ReadPipe<s32>()));
		break;

	case eMessage_QUERY_peek_cpu_regs:
		{
			//watch it! the size of this struct is important!
			#ifdef _MSC_VER
			#pragma pack(push,1)
			#endif
			struct {
				u32 pc;
				u16 a,x,y,z,s,d,vector; //7x
						u8 p, nothing;
				u32 aa,rd;
				u8 sp, dp, db, mdr;
			} 
			#ifndef _MSC_VER
			__attribute__((__packed__))
			#endif
			cpuregs;
			#ifdef _MSC_VER
			#pragma pack(pop)
			#endif

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

			WritePipeBuffer(&cpuregs,32); //watch it! the size of this struct is important!
		}
		break;

	case eMessage_QUERY_GetMemoryIdName:
		{
			uint32 id = ReadPipe<uint32>();
			const char* ret = snes_get_memory_id_name(id);
			if(!ret) ret = "";
			WritePipeString(ret);
			break;
		}

	case eMessage_QUERY_state_hook_exec:
		SNES::cpu.debugger.op_exec = ReadPipe<bool>() ? debug_op_exec : hook<void (uint24)>(); 
		break;

	case eMessage_QUERY_state_hook_read:
		SNES::cpu.debugger.op_read = ReadPipe<bool>() ? debug_op_read : hook<void (uint24)>(); 
		break;

	case eMessage_QUERY_state_hook_write:
		SNES::cpu.debugger.op_write = ReadPipe<bool>() ? debug_op_write : hook<void (uint24, uint8)>(); 
		break;

	case eMessage_QUERY_state_hook_nmi:
		SNES::cpu.debugger.op_nmi = ReadPipe<bool>() ? debug_op_nmi : hook<void ()>(); 
		break;

	case eMessage_QUERY_state_hook_irq:
		SNES::cpu.debugger.op_irq = ReadPipe<bool>() ? debug_op_irq : hook<void ()>(); 
		break;

	case eMessage_QUERY_set_cdl:
		for (int i = 0; i<eCDLog_AddrType_NUM; i++)
		{
			cdlInfo.blocks[i] = ReadPipe<uint8_t*>();
			cdlInfo.blockSizes[i] = ReadPipe<uint32_t>();
		}
		break;

	}
	return true;
}

bool Handle_CMD(eMessage msg)
{
	if(msg == eMessage_ResumeAfterBRK)
	{
		//careful! dont switch back to co_emu, we were in another cothread probably when the BRK happened.
		//i'm not sure its completely safe to be returning to co_emu below in the normal CMD handler, either...
		co_switch(co_emu_suspended);
		return true;		
	}
	
	if(msg<=eMessage_CMD_FIRST || msg>=eMessage_CMD_LAST) return false;

	s_EmulationControl.command = msg;
	s_EmulationControl.exitReason = eEmulationExitReason_NotSet;
	co_switch(co_emu);
	return true;
}

void Handle_SIG_audio_flush()
{
	WritePipe(eMessage_SIG_audio_flush);

	int nsamples = audiobuffer_idx;
	WritePipe(nsamples);
	char* buf = ReadPipeSharedPtr();
	memcpy(buf,audiobuffer,nsamples*2);
	//extra just in case we had to unexpectedly flush audio and then carry on with some other process... yeah, its rickety.
	WritePipe(0); //dummy synchronization
	
	//wait for frontend to consume data

	ReadPipe<int>(); //dummy synchronization
	WritePipe(0); //dummy synchronization
	audiobuffer_idx = 0;
}

void MessageLoop()
{
	for(;;)
	{
TOP:
		switch(s_EmulationControl.exitReason)
		{
		case eEmulationExitReason_NotSet:
			goto HANDLEMESSAGES;
		
		case eEmulationExitReason_CMD_Complete:
			//printf("eEmulationExitReason_CMD_Complete (command:%d)\n",s_EmulationControl.command);
			//MessageBox(0,"ZING","ZING",MB_OK);
			//printf("WRITING COMPLETE\n");
			WritePipe(eMessage_BRK_Complete);
			
			//special post-completion messages (return values)
			switch(s_EmulationControl.command)
			{
			case eMessage_CMD_load_cartridge_normal:
			case eMessage_CMD_load_cartridge_super_game_boy:
			case eMessage_CMD_serialize:
			case eMessage_CMD_unserialize:
				WritePipe(s_EmulationControl.cmd_result);
				break;
			}
			
			s_EmulationControl.exitReason = eEmulationExitReason_NotSet;
			s_EmulationControl.command = eMessage_NotSet;
			goto TOP;
		
		case eEmulationExitReason_SIG:
			s_EmulationControl.exitReason = eEmulationExitReason_NotSet;
			switch(s_EmulationControl.exitCallbackType)
			{
			case eEmulationCallback_snes_video_refresh:
				{
					WritePipe(eMessage_SIG_video_refresh);
					WritePipe(s_EmulationControl.cb_video_refresh_params.width);
					WritePipe(s_EmulationControl.cb_video_refresh_params.height);
					int destOfs = ReadPipe<int>();
					char* buf = (char*)hMapFilePtr + destOfs;
					int bufsize = 512 * 480 * 4;
					memcpy(buf,s_EmulationControl.cb_video_refresh_params.data,bufsize);
					WritePipe((char)0); //dummy synchronization (alert frontend we're done with buffer)
					break;
				}

			case eEmulationCallback_snes_audio_flush:
				Handle_SIG_audio_flush();
				break;
			case eEmulationCallback_snes_input_poll:
				WritePipe(eMessage_SIG_input_poll);
				break;
			case eEmulationCallback_snes_input_state:
				WritePipe(eMessage_SIG_input_state);
				WritePipe(s_EmulationControl.cb_input_state_params.port);
				WritePipe(s_EmulationControl.cb_input_state_params.device);
				WritePipe(s_EmulationControl.cb_input_state_params.index);
				WritePipe(s_EmulationControl.cb_input_state_params.id);
				s_EmulationControl.cb_input_state_params.result = ReadPipe<int16_t>();
				break;
			case eEmulationCallback_snes_input_notify:
				WritePipe(eMessage_SIG_input_notify);
				WritePipe(s_EmulationControl.cb_input_notify_params.index);
				break;
			case eEmulationCallback_snes_path_request:
				{
					WritePipe(eMessage_SIG_path_request);
					WritePipe(s_EmulationControl.cb_path_request_params.slot);
					WritePipeString(s_EmulationControl.cb_path_request_params.hint);
					std::string temp = ReadPipeString();
					//yucky! use strncpy and ARRAY_SIZE or something!
					strcpy(s_EmulationControl.cb_path_request_params.result,temp.c_str());
				}
				break;
			case eEmulationCallback_snes_allocSharedMemory:
				implementation_snes_allocSharedMemory();
				break;
			case eEmulationCallback_snes_freeSharedMemory:
				implementation_snes_freeSharedMemory();
				break;
			case eEmulationCallback_snes_trace:
				WritePipe(eMessage_SIG_trace_callback);
				WritePipeString(s_EmulationControl.cb_trace_params.msg);				
				break;
			}
			//when callbacks finish, we automatically resume emulation. be careful to go back to top!!!!!!!!
			SETEMU;
			goto TOP;

		case eEmulationExitReason_BRK:
			s_EmulationControl.exitReason = eEmulationExitReason_NotSet;
			switch(s_EmulationControl.hookExitType)
			{
			case eMessage_BRK_scanlineStart:
				WritePipe(eMessage_BRK_scanlineStart);
				WritePipe((uint32)s_EmulationControl.hookAddr);
				break;
			case eMessage_BRK_hook_exec:
				WritePipe(eMessage_BRK_hook_exec);
				WritePipe((uint32)s_EmulationControl.hookAddr);
				break;
			case eMessage_BRK_hook_read:
				WritePipe(eMessage_BRK_hook_read);
				WritePipe((uint32)s_EmulationControl.hookAddr);
				break;
			case eMessage_BRK_hook_write:
				WritePipe(eMessage_BRK_hook_write);
				WritePipe((uint32)s_EmulationControl.hookAddr);
				WritePipe((uint8)s_EmulationControl.hookValue);
				break;
			}
			goto TOP;
		}

HANDLEMESSAGES:

		//printf("Reading message from pipe...\n");
		eMessage msg = ReadPipe<eMessage>();
		//printf("...slam: %08X\n",msg);

		if(Handle_QUERY(msg))
			goto TOP;
		if(Handle_CMD(msg))
			goto TOP;

		switch(msg)
		{
		case eMessage_BRK_Complete:
			return;

		case eMessage_Shutdown:
			//terminate this dll process
			return;
		
		case eMessage_SetBuffer:
			{
				printf("eMessage_SetBuffer\n");
				int which = ReadPipe<s32>();
				std::string name = ReadPipeString();
				IPCRingBuffer* ipcrb = new IPCRingBuffer();
				ipcrb->Open(name);
				if(which==0) rbuf = ipcrb;
				else wbuf = ipcrb;
				break;
			}

		case eMessage_BeginBufferIO:
			bufio = true;
			break;

		case eMessage_EndBufferIO:
			bufio = false;
			break;

		} //switch(msg)
	}
}

static DWORD WINAPI ThreadProc(_In_ LPVOID lpParameter)
{
	MessageLoop();
	//send a message to the other thread to synchronize the shutdown of this thread
	//after that message is received, this thread (and the whole dll instance) is dead.
	WritePipe(eMessage_BRK_Complete);
	return 0;
}


void OpenConsole() 
{
	AllocConsole();
	freopen("CONOUT$", "w", stdout);
	freopen("CONOUT$", "w", stderr);
	freopen("CONIN$", "r", stdin);
}

void EMUTHREAD_handleCommand_LoadCartridgeNormal()
{
	Blob xml = ReadPipeBlob();
	xml.push_back(0); //make sure the xml is null terminated
	const char* xmlptr = NULL;
	if(xml.size() != 1) xmlptr = &xml[0];

	Blob rom_data = ReadPipeBlob();
	const unsigned char* rom_ptr = NULL;
	if(rom_data.size() != 0) rom_ptr = (unsigned char*)&rom_data[0];

	bool ret = snes_load_cartridge_normal(xmlptr,rom_ptr,rom_data.size());
	s_EmulationControl.cmd_result = ret?1:0;
}

void EMUTHREAD_handleCommand_LoadCartridgeSuperGameBoy()
{
	std::string rom_xml = ReadPipeString();
	const char* rom_xmlptr = NULL;
	if(rom_xml != "") rom_xmlptr = rom_xml.c_str();
	Blob rom_data = ReadPipeBlob();
	uint32 rom_length = rom_data.size();

	std::string dmg_xml = ReadPipeString();
	const char* dmg_xmlptr = NULL;
	if(dmg_xml != "") dmg_xmlptr = dmg_xml.c_str();
	Blob dmg_data = ReadPipeBlob();
	uint32 dmg_length = dmg_data.size();

	bool ret = snes_load_cartridge_super_game_boy(rom_xmlptr,(uint8*)&rom_data[0],rom_length, dmg_xmlptr,(uint8*)&dmg_data[0], dmg_length);
	s_EmulationControl.cmd_result = ret?1:0;
}

void EMUTHREAD_handle_CMD_serialize()
{
	int size = ReadPipe<s32>();
	int destOfs = ReadPipe<s32>();
	char* buf = (char*)hMapFilePtr + destOfs;
	bool ret = snes_serialize((uint8_t*)buf,size);
	s_EmulationControl.cmd_result = ret?1:0;
}

void EMUTHREAD_handle_CMD_unserialize()
{
	int size = ReadPipe<s32>();
	int destOfs = ReadPipe<s32>();
	char* buf = (char*)hMapFilePtr + destOfs;
	bool ret = snes_unserialize((uint8_t*)buf	,size);
	s_EmulationControl.cmd_result = ret?1:0;
}

void emuthread()
{
	for(;;)
	{
		switch(s_EmulationControl.command)
		{
		case eMessage_CMD_init:
			snes_init();
			break;
		case eMessage_CMD_power:
			snes_power();
			break;
		case eMessage_CMD_reset:
			snes_reset();
			break;
		case eMessage_CMD_term:
			snes_term();
			break;
		case eMessage_CMD_unload_cartridge:
			snes_unload_cartridge();
			break;
		case eMessage_CMD_load_cartridge_normal:
			EMUTHREAD_handleCommand_LoadCartridgeNormal();
			break;
		case eMessage_CMD_load_cartridge_super_game_boy:
			EMUTHREAD_handleCommand_LoadCartridgeSuperGameBoy();
			break;

		case eMessage_CMD_serialize:
			EMUTHREAD_handle_CMD_serialize();
			break;
		case eMessage_CMD_unserialize:
			EMUTHREAD_handle_CMD_unserialize();
			break;

		case eMessage_CMD_run:
			SIG_FlushAudio();

			//we could avoid this if we saved the current thread before jumping back to co_control, instead of always jumping back to co_emu
			//in effect, we're scrambling the scheduler
			//EDIT - well, we changed that, but.. we still want this probably, for debugging and stuff
			for(;;)
			{
				SNES::scheduler.sync = SNES::Scheduler::SynchronizeMode::None;
				SNES::scheduler.clearExitReason();
				SNES::scheduler.enter();
				if(SNES::scheduler.exit_reason() == SNES::Scheduler::ExitReason::FrameEvent)
				{
					SNES::video.update();
					break;
				}
				//not used yet
				if(SNES::scheduler.exit_reason() == SNES::Scheduler::ExitReason::DebuggerEvent)
					break;
			}

			SIG_FlushAudio();
			break;
		}

		s_EmulationControl.exitReason = eEmulationExitReason_CMD_Complete;
		SETCONTROL;
	}
}

BOOL WINAPI DllMain(_In_ HINSTANCE hinstDLL, _In_ DWORD     fdwReason, _In_ LPVOID    lpvReserved)
{
	return TRUE;
}

extern "C" dllexport bool __cdecl DllInit(const char* ipcname)
{
	printf("NEW INSTANCE: %08X\n", &s_EmulationControl);

	char pipename[256];
	char eventname[256];
	sprintf(pipename, "\\\\.\\Pipe\\%s",ipcname);
	sprintf(eventname, "%s-event", ipcname);

	printf("pipe: %s\n",pipename);
	printf("event: %s\n",eventname);

	hPipe = CreateFile(pipename, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

	if(hPipe == INVALID_HANDLE_VALUE)
		return false;

	hMapFile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, ipcname);
	if(hMapFile == INVALID_HANDLE_VALUE)
		return false;

	hMapFilePtr = MapViewOfFile(hMapFile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

	//make a coroutine thread to run the emulation in. we'll switch back to this thread when communicating with the frontend
	co_control = co_active();
	co_emu = co_create(65536*sizeof(void*),emuthread);

	running = true;
	printf("running\n");

	DWORD tid;
	CreateThread(nullptr, 0, &ThreadProc, nullptr, 0, &tid);

	return true;
}


void pwrap_init()
{
	//bsnes's interface initialization calls into this after initializing itself, so we can get a chance to mod it for pwrap functionalities
	InitBsnes();
}