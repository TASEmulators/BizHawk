//derived from libsnes
//types of messages:
//cmd: frontend->core: "command to core" a command from the frontend which causes emulation to proceed. when sending a command, the frontend should wait for an eMessage::BRK_Complete before proceeding, although a debugger might proceed after any BRK
//query: frontend->core: "query to core" a query from the frontend which can (and should) be satisfied immediately by the core but which does not result in emulation processes (notably, nothing resembling a CMD and nothing which can trigger a BRK)
//sig: core->frontend: "core signal" a synchronous operation called from the emulation process which the frontend should handle immediately without issuing any calls into the core
//brk: core->frontend: "core break" the emulation process has suspended. the frontend is free to do whatever it wishes.

#define _CRT_NONSTDC_NO_DEPRECATE

#include <Windows.h>

#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdio.h>

#include <string>

#define bool unsigned char
#include "libretro.h"
#undef bool

extern "C" uint64_t cpu_features_get();

#include "libco/libco.h"

//can't use retroarch's dynamic.h, it's too full of weird stuff. don't need it anyway

typedef uint8_t u8;
typedef uint16_t u16;
typedef uint64_t u64;
typedef uint32_t u32;

typedef u8 u8bool;

typedef int16_t s16;
typedef int32_t s32;
typedef int64_t s64;

typedef void(*Action)();

struct retro_core_t
{
	void(*retro_init)(void);
	void(*retro_deinit)(void);
	unsigned(*retro_api_version)(void);
	void(*retro_get_system_info)(struct retro_system_info*);
	void(*retro_get_system_av_info)(struct retro_system_av_info*);
	void(*retro_set_environment)(retro_environment_t);
	void(*retro_set_video_refresh)(retro_video_refresh_t);
	void(*retro_set_audio_sample)(retro_audio_sample_t);
	void(*retro_set_audio_sample_batch)(retro_audio_sample_batch_t);
	void(*retro_set_input_poll)(retro_input_poll_t);
	void(*retro_set_input_state)(retro_input_state_t);
	void(*retro_set_controller_port_device)(unsigned, unsigned);
	void(*retro_reset)(void);
	void(*retro_run)(void);
	size_t(*retro_serialize_size)(void);
	u8bool(*retro_serialize)(void*, size_t);
	u8bool(*retro_unserialize)(const void*, size_t);
	void(*retro_cheat_reset)(void);
	void(*retro_cheat_set)(unsigned, u8bool, const char*);
	u8bool(*retro_load_game)(const struct retro_game_info*);
	u8bool(*retro_load_game_special)(unsigned,
		const struct retro_game_info*, size_t);
	void(*retro_unload_game)(void);
	unsigned(*retro_get_region)(void);
	void *(*retro_get_memory_data)(unsigned);
	size_t(*retro_get_memory_size)(unsigned);
};

enum eMessage : s32
{
	NotSet,

	Resume,

	QUERY_FIRST,
	QUERY_GetMemory,
	QUERY_LAST,

	CMD_FIRST,
	CMD_SetEnvironment,
	CMD_LoadNoGame,
	CMD_LoadData,
	CMD_LoadPath,
	CMD_Deinit,
	CMD_Reset,
	CMD_Run,
	CMD_UpdateSerializeSize,
	CMD_Serialize,
	CMD_Unserialize,
	CMD_LAST,

	SIG_InputState,
	SIG_VideoUpdate,
	SIG_Sample,
	SIG_SampleBatch,
};

enum eStatus : s32
{
	eStatus_Idle,
	eStatus_CMD,
	eStatus_BRK
};

enum BufId : s32 {
	Param0 = 0,
	Param1 = 1,
	SystemDirectory = 2,
	SaveDirectory = 3,
	CoreDirectory = 4,
	CoreAssetsDirectory = 5,
	BufId_Num //excess sized by 1.. no big deal
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

	//flexible in/out parameters
	//these are all "overloaded" a little so it isn't clear what's used for what in for any particular message..
	//but I think it will beat having to have some kind of extremely verbose custom layouts for every message
	u32 id, addr, value, size;
	u32 port, device, index, slot; //for input state

	//variables meant for stateful communication (not parameters)
	//may be in, out, or inout. it's pretty sloppy.
	struct {
		//set by the core
		retro_system_info retro_system_info;
		retro_system_av_info retro_system_av_info;
		size_t retro_serialize_size_initial;
		size_t retro_serialize_size;
		u32 retro_region;
		u32 retro_api_version;
		retro_pixel_format pixel_format; //default is 0 -- RETRO_PIXEL_FORMAT_0RGB1555
		s32 rotation_ccw;
		bool support_no_game;
		retro_get_proc_address_t core_get_proc_address;

		retro_game_geometry retro_game_geometry;
		u8bool retro_game_geometry_dirty; //c# can clear this when it's acknowledged (but I think we might handle it from here? not sure)

		//defined by the core. values arent put here, this is just the variables defined by the core
		//todo: shutdown tidy
		s32 variable_count;
		const char** variable_keys;
		const char** variable_comments;

		//c# sets these with thunked callbacks
		retro_perf_callback retro_perf_callback;

		//various stashed stuff solely for c# convenience
		u64 processor_features;

		s32 fb_width, fb_height; //core sets these; c# picks up, and..
		s32* fb_bufptr; //..sets this for the core to spill its data nito

	} env;

	//always used in pairs
	void* buf[BufId_Num];
	size_t buf_size[BufId_Num];

	//===========================================================
	//private stuff

	std::string *variables;
	bool variables_dirty;


	void* privbuf[BufId_Num]; //TODO remember to tidy this.. (needs to be done in snes too)
	void SetString(int id, const char* str)
	{
		size_t len = strlen(str);
		CopyBuffer(id, (void*)str, len+1);
	}
	void CopyBuffer(int id, void* ptr, size_t size)
	{
		if (privbuf[id]) free(privbuf[id]);
		buf[id] = privbuf[id] = malloc(size);
		memcpy(buf[id], ptr, size);
		buf_size[id] = size;
	}

	void SetBuffer(int id, void* ptr, size_t size)
	{
		buf[id] = ptr;
		buf_size[id] = size;
	}

	struct {
	} strings;

	HMODULE dllModule;
	retro_core_t funs;

	void LoadSymbols()
	{
		//retroarch would throw an error here if the FP ws null. maybe better than throwing an error later, but are all the functions required?
#		define SYMBOL(x) { \
			FARPROC func = GetProcAddress(dllModule, #x); \
			memcpy(&funs.x, &func, sizeof(func)); \
		}

		SYMBOL(retro_init);
		SYMBOL(retro_deinit);

		SYMBOL(retro_api_version);
		SYMBOL(retro_get_system_info);
		SYMBOL(retro_get_system_av_info);

		SYMBOL(retro_set_environment);
		SYMBOL(retro_set_video_refresh);
		SYMBOL(retro_set_audio_sample);
		SYMBOL(retro_set_audio_sample_batch);
		SYMBOL(retro_set_input_poll);
		SYMBOL(retro_set_input_state);

		SYMBOL(retro_set_controller_port_device);

		SYMBOL(retro_reset);
		SYMBOL(retro_run);

		SYMBOL(retro_serialize_size);
		SYMBOL(retro_serialize);
		SYMBOL(retro_unserialize);

		SYMBOL(retro_cheat_reset);
		SYMBOL(retro_cheat_set);

		SYMBOL(retro_load_game);
		SYMBOL(retro_load_game_special);

		SYMBOL(retro_unload_game);
		SYMBOL(retro_get_region);
		SYMBOL(retro_get_memory_data);
		SYMBOL(retro_get_memory_size);
	}

	retro_core_t fn;

} comm;

//coroutines
cothread_t co_control, co_emu, co_emu_suspended;

//internal state
Action CMD_cb;

void BREAK(eMessage msg) {
	comm.status = eStatus_BRK;
	comm.reason = msg;
	co_emu_suspended = co_active();
	co_switch(co_control);
	comm.status = eStatus_CMD;
}

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

void retro_log_printf(enum retro_log_level level, const char *fmt, ...)
{
	va_list args;
	va_start(args, fmt);
	vprintf(fmt,args);
	va_end(args);
}

u8bool retro_environment(unsigned cmd, void *data)
{
	switch (cmd)
	{
		case RETRO_ENVIRONMENT_SET_ROTATION:
			comm.env.rotation_ccw = (int)*(const unsigned*)data * 90;
			return true;
		case RETRO_ENVIRONMENT_GET_OVERSCAN:
			return false; //could return true to crop overscan
		case RETRO_ENVIRONMENT_GET_CAN_DUPE:
			return true;
		case RETRO_ENVIRONMENT_SET_MESSAGE:
		{
			//TODO: try to respect design principle by forwarding to frontend with the timer
			auto &msg = *(retro_message*)data;
			printf("%s\n",msg.msg);
			return true;
		}
		case RETRO_ENVIRONMENT_SHUTDOWN:
			//TODO low priority
			return false;
		case RETRO_ENVIRONMENT_SET_PERFORMANCE_LEVEL:
			//unneeded
			return false;
		case RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY:
			*(const char**)data = (const char*)comm.buf[SystemDirectory];
			return true;
		case RETRO_ENVIRONMENT_SET_PIXEL_FORMAT:
			comm.env.pixel_format = *(const enum retro_pixel_format*)data;
			return true;
		case RETRO_ENVIRONMENT_SET_INPUT_DESCRIPTORS:
			//TODO medium priority
			return false;
		case RETRO_ENVIRONMENT_SET_KEYBOARD_CALLBACK:
			//TODO high priority (to support keyboard consoles, probably high value for us. but that may take a lot of infrastructure work)
			return false;
		case RETRO_ENVIRONMENT_SET_DISK_CONTROL_INTERFACE:
			//TODO high priority (to support disc systems)
			return false;
		case RETRO_ENVIRONMENT_SET_HW_RENDER:
			//TODO high priority (to support 3d renderers
			return false;

		case RETRO_ENVIRONMENT_GET_VARIABLE:
		{
			//according to retroarch's `core_option_manager_get` this is what we should do
			comm.variables_dirty = false;

			auto req = (retro_variable *)data;
			req->value = nullptr;

			for(int i=0;i<comm.env.variable_count;i++)
			{
				if(!strcmp(comm.env.variable_keys[i],req->key))
				{
					req->value = comm.variables[i].c_str();
					return true;
				}
			}

			return true;
		}

		case RETRO_ENVIRONMENT_SET_VARIABLES:
		{
			auto var = (retro_variable *)data;
			int nVars = 0;
			while(var->key)
				nVars++, var++;
			comm.variables = new std::string[nVars];
			comm.env.variable_count = nVars;
			comm.env.variable_keys = new const char*[nVars];
			comm.env.variable_comments = new const char*[nVars];
			var = (retro_variable *)data;
			for(int i=0;i<nVars;i++)
			{
				comm.env.variable_keys[i] = var[i].key;
				comm.env.variable_comments[i] = var[i].value;
				
				//analyze to find default and save it
				std::string comment = var[i].value;
				auto ofs = comment.find_first_of(';')+2;
				auto pipe = comment.find('|',ofs);
				if(pipe == std::string::npos)
					comm.variables[i] = comment.substr(ofs);
				else 
					comm.variables[i] = comment.substr(ofs,pipe-ofs);
			}
			return true;
		}
		
		case RETRO_ENVIRONMENT_GET_VARIABLE_UPDATE:
			*(u8bool*)data = comm.variables_dirty;
			break;
		case RETRO_ENVIRONMENT_SET_SUPPORT_NO_GAME:
			comm.env.support_no_game = !!*(u8bool*)data;
			break;
		case RETRO_ENVIRONMENT_GET_LIBRETRO_PATH:
			*(const char**)data = (const char*)comm.buf[CoreDirectory];
			return true;
		case RETRO_ENVIRONMENT_SET_AUDIO_CALLBACK:
			//dont know what to do with this yet
			return false;
		case RETRO_ENVIRONMENT_SET_FRAME_TIME_CALLBACK:
			//dont know what to do with this yet
			return false;
		case RETRO_ENVIRONMENT_GET_RUMBLE_INTERFACE:
			//TODO low priority
			return false;
		case RETRO_ENVIRONMENT_GET_INPUT_DEVICE_CAPABILITIES:
			//TODO medium priority - other input methods
			*(u64*)data = (1<<RETRO_DEVICE_JOYPAD);
			return true;
		
		case RETRO_ENVIRONMENT_GET_LOG_INTERFACE:
			((retro_log_callback*)data)->log = retro_log_printf;
			return true;
		case RETRO_ENVIRONMENT_GET_PERF_INTERFACE:
			*((retro_perf_callback *)data) = comm.env.retro_perf_callback;
			return true;
		case RETRO_ENVIRONMENT_GET_LOCATION_INTERFACE:
			//TODO low priority
			return false;

		case RETRO_ENVIRONMENT_GET_CORE_ASSETS_DIRECTORY:
			*(const char**)data = (const char*)comm.buf[CoreAssetsDirectory];
			return true;
		case RETRO_ENVIRONMENT_GET_SAVE_DIRECTORY:
			*(const char**)data = (const char*)comm.buf[SaveDirectory];
			return true;
		case RETRO_ENVIRONMENT_SET_SYSTEM_AV_INFO:
			printf("NEED RETRO_ENVIRONMENT_SET_SYSTEM_AV_INFO\n");
			return false;
		case RETRO_ENVIRONMENT_SET_PROC_ADDRESS_CALLBACK:
			comm.env.core_get_proc_address = ((retro_get_proc_address_interface*)data)->get_proc_address;
			return true;
		
		case RETRO_ENVIRONMENT_SET_SUBSYSTEM_INFO:
			//needs retro_load_game_special to be useful; not supported yet
			return false;

		case RETRO_ENVIRONMENT_SET_CONTROLLER_INFO:
			//TODO medium priority probably
			return false;

		case RETRO_ENVIRONMENT_SET_GEOMETRY:
			comm.env.retro_game_geometry = *((const retro_game_geometry *)data);
			comm.env.retro_game_geometry_dirty = true;
			return true;

		case RETRO_ENVIRONMENT_GET_USERNAME:
			//we definitely want to return false here so the core will do something deterministic
			return false;

		case RETRO_ENVIRONMENT_GET_LANGUAGE:
			*((unsigned *)data) = RETRO_LANGUAGE_ENGLISH;
				return true;
	}
	
	return false;
}

template<int ROT> static inline int* address(int width, int height, int pitch, int x, int y, int* dstbuf, int* optimize0dst)
{
	switch (ROT)
	{
	case 0:
		return optimize0dst;

	case 90:
		//TODO:
		return optimize0dst;

	case 180:
		//TODO:
		return optimize0dst;

	case 270:
		{
			int dx = width - y - 1;
			int dy = x;
			return dstbuf + dy * width + dx;
		}

	default:
		//impossible
		return 0;
	}

}

template<int ROT> void Blit555(short* srcbuf, s32* dstbuf, int width, int height, int pitch)
{
	s32* dst = dstbuf;
	for (int y = 0; y < height; y++)
	{
		short* row = srcbuf;
		for (int x = 0; x < width; x++)
		{
			short ci = *row;
			int r = ci & 0x001f;
			int g = ci & 0x03e0;
			int b = ci & 0x7c00;

			r = (r << 3) | (r >> 2);
			g = (g >> 2) | (g >> 7);
			b = (b >> 7) | (b >> 12);
			int co = r | g | b | 0xff000000;

			*address<ROT>(width, height, pitch, x, y, dstbuf, dst) = co;
			dst++;
			row++;
		}
		srcbuf += pitch/2;
	}
}

template<int ROT> void Blit565(short* srcbuf, s32* dstbuf, int width, int height, int pitch)
{
	s32* dst = dstbuf;
	for (int y = 0; y < height; y++)
	{
		short* row = srcbuf;
		for (int x = 0; x < width; x++)
		{
			short ci = *row;
			int r = ci & 0x001f;
			int g = (ci & 0x07e0) >> 5;
			int b = (ci & 0xf800) >> 11;

			r = (r << 3) | (r >> 2);
			g = (g << 2) | (g >> 4);
			b = (b << 3) | (b >> 2);
			int co = (b << 16) | (g << 8) | r;

			*address<ROT>(width, height, pitch, x, y, dstbuf, dst) = co;
			dst++;
			row++;
		}
		srcbuf += pitch/2;
	}
}

template<int ROT> void Blit888(int* srcbuf, s32* dstbuf, int width, int height, int pitch)
{
	s32* dst = dstbuf;
	for (int y = 0; y < height; y++)
	{
		int* row = srcbuf;
		for (int x = 0; x < width; x++)
		{
			int ci = *row;
			int co = ci | 0xff000000;
			*address<ROT>(width,height,pitch,x,y,dstbuf,dst) = co;
			dst++;
			row++;
		}
		srcbuf += pitch/4;
	}
}

void retro_video_refresh(const void *data, unsigned width, unsigned height, size_t pitch)
{
	//handle a "dup frame" -- same as previous frame. so there isn't anything to be done here
	if (!data) 
		return;

	comm.env.fb_width = (s32)width;
	comm.env.fb_height = (s32)height;
	//stash pitch if needed

	//notify c# of these new settings and let it allocate a buffer suitable for receiving the output (so we can work directly into c#'s int[])
	//c# can read the settings right out of the comm env
	BREAK(eMessage::SIG_VideoUpdate);


	////if (BufferWidth != width) BufferWidth = (int)width;
	////if (BufferHeight != height) BufferHeight = (int)height;
	////if (BufferWidth * BufferHeight != rawvidbuff.Length)
	////  rawvidbuff = new int[BufferWidth * BufferHeight];

	////if we have rotation, we might have a geometry mismatch and in any event we need a temp buffer to do the rotation from
	////but that's a general problem, isnt it?
	//if (comm.env.fb.raw == nullptr || comm.env.fb.raw_length != width * height)
	//{
	//	if(comm.env.fb.raw)
	//		delete[] comm.env.fb.raw;
	//	comm.env.fb.raw = new u32[width * height];
	//	comm.env.fb.width = width;
	//	comm.env.fb.height = height;
	//}

	int w = (int)width;
	int h = (int)height;
	int p = (int)pitch;

	switch(comm.env.pixel_format)
	{

	case RETRO_PIXEL_FORMAT_0RGB1555:
		switch (comm.env.rotation_ccw)
		{
		case 0: Blit555<0>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		case 90: Blit555<90>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		case 180: Blit555<180>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		case 270: Blit555<270>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		}
		break;

	case RETRO_PIXEL_FORMAT_XRGB8888:
		switch(comm.env.rotation_ccw)
		{
		case 0: Blit888<0>((int*)data, comm.env.fb_bufptr, w, h, p); break;
		case 90: Blit888<90>((int*)data, comm.env.fb_bufptr, w, h, p); break;
		case 180: Blit888<180>((int*)data, comm.env.fb_bufptr, w, h, p); break;
		case 270: Blit888<270>((int*)data, comm.env.fb_bufptr, w, h, p); break;
		}
		break;
		
	case RETRO_PIXEL_FORMAT_RGB565:
		switch (comm.env.rotation_ccw)
		{
		case 0: Blit565<0>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		case 90: Blit565<90>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		case 180: Blit565<180>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		case 270: Blit565<270>((short*)data, comm.env.fb_bufptr, w, h, p); break;
		}
		break;
	}
	
}

void retro_audio_sample(s16 left, s16 right)
{
	s16 samples[] = {left,right};
	comm.SetBuffer(BufId::Param0,(void*)&samples,4); 
	BREAK(SIG_Sample);
}
size_t retro_audio_sample_batch(const s16 *data, size_t frames)
{
	comm.SetBuffer(BufId::Param0, (void*)data, frames*4);
	BREAK(SIG_SampleBatch);
	return frames;
}
void retro_input_poll()
{
}
s16 retro_input_state(unsigned port, unsigned device, unsigned index, unsigned id)
{
	//we have to bail to c# for this, it's too complex.
	comm.port = port;
	comm.device = device;
	comm.index = index;
	comm.id = id;
	
	BREAK(eMessage::SIG_InputState);

	return (s16)comm.value;
}

//loads the game, too
//REQUIREMENTS:
//set SystemDirectory, SaveDirectory, CoreDirectory, CoreAssetsDirectory are set
//retro_perf_callback is set
static void LoadHandler(eMessage msg)
{
	//retro_set_environment() is guaranteed to be called before retro_init().
	
	comm.funs.retro_init();

	retro_game_info rgi;
	retro_game_info* rgiptr = &rgi;
	memset(&rgi,0,sizeof(rgi));
	
	if (msg == eMessage::CMD_LoadNoGame)
	{
		rgiptr = nullptr;
	}
	else
	{
		rgi.path = (const char*)comm.buf[BufId::Param0];
		if (msg == eMessage::CMD_LoadData)
		{
			rgi.data = comm.buf[BufId::Param1];
			rgi.size = comm.buf_size[BufId::Param1];
		}
	}

	comm.funs.retro_load_game(rgiptr);

	//Can be called only after retro_load_game() has successfully completed.
	comm.funs.retro_get_system_av_info(&comm.env.retro_system_av_info);

	//guaranteed to have been called before the first call to retro_run() is made.
	//(I've put this after the retro_system_av_info runs, in case that's important
	comm.funs.retro_set_video_refresh(retro_video_refresh);

	comm.funs.retro_set_audio_sample(retro_audio_sample);
	comm.funs.retro_set_audio_sample_batch(retro_audio_sample_batch);
	comm.funs.retro_set_input_poll(retro_input_poll);
	comm.funs.retro_set_input_state(retro_input_state);

	//Between calls to retro_load_game() and retro_unload_game(), the returned size is never allowed to be larger than a previous returned
	//value, to ensure that the frontend can allocate a save state buffer once.
	comm.env.retro_serialize_size_initial = comm.env.retro_serialize_size = comm.funs.retro_serialize_size();

	//not sure when this can be called, but it's surely safe here
	comm.env.retro_region = comm.funs.retro_get_region();
}

void cmd_LoadNoGame() { LoadHandler(eMessage::CMD_LoadNoGame); }
void cmd_LoadData() { LoadHandler(eMessage::CMD_LoadData); }
void cmd_LoadPath() { LoadHandler(eMessage::CMD_LoadPath); }

void cmd_Deinit()
{
	//not sure if we need this
	comm.funs.retro_unload_game();
	comm.funs.retro_deinit();
	//TODO: tidy
}

void cmd_Reset()
{
	comm.funs.retro_reset();
}

void cmd_Run()
{
	comm.funs.retro_run();
}

void cmd_UpdateSerializeSize()
{
	comm.env.retro_serialize_size = comm.funs.retro_serialize_size();
}

void cmd_Serialize()
{
	comm.value = !!comm.funs.retro_serialize(comm.buf[BufId::Param0], comm.buf_size[BufId::Param0]);
}

void cmd_Unserialize()
{
	comm.value = !!comm.funs.retro_unserialize(comm.buf[BufId::Param0], comm.buf_size[BufId::Param0]);
}

//TODO
//void(*retro_set_controller_port_device)(unsigned, unsigned);
//void *(*retro_get_memory_data)(unsigned);
//size_t(*retro_get_memory_size)(unsigned);

//TODO low priority
//void(*retro_cheat_reset)(void);
//void(*retro_cheat_set)(unsigned, bool, const char*);
//bool(*retro_load_game_special)(unsigned,

//TODO maybe not sensible though
//void(*retro_unload_game)(void);

void cmd_SetEnvironment() 
{
	//stuff that can't be done until our environment is setup (the core will immediately query the environment)
	comm.funs.retro_set_environment(retro_environment);
}

void query_GetMemory()
{
	comm.buf_size[BufId::Param0] = comm.funs.retro_get_memory_size(comm.value);
	comm.buf[BufId::Param0] = comm.funs.retro_get_memory_data(comm.value);
}

const Action kHandlers_CMD[] = {
	cmd_SetEnvironment,
	cmd_LoadNoGame,
	cmd_LoadData,
	cmd_LoadPath,
	cmd_Deinit,
	cmd_Reset,
	cmd_Run,
	cmd_UpdateSerializeSize,
	cmd_Serialize,
	cmd_Unserialize,
};

const Action kHandlers_QUERY[] = {
	query_GetMemory,
};

//------------------------------------------------
//DLL INTERFACE

BOOL WINAPI DllMain(_In_ HINSTANCE hinstDLL, _In_ DWORD     fdwReason, _In_ LPVOID    lpvReserved)
{
	return TRUE;
}


extern "C" __declspec(dllexport) void* __cdecl DllInit(HMODULE dllModule)
{
	memset(&comm,0,sizeof(comm));

	//make a coroutine thread to run the emulation in. we'll switch back to this cothread when communicating with the frontend
	co_control = co_active(); 
	co_emu = co_create(128*1024 * sizeof(void*), new_emuthread);

	//grab all the function pointers we need.
	comm.dllModule = dllModule;
	comm.LoadSymbols();

	//libretro startup steps
	//"Can be called at any time, even before retro_init()."
	comm.funs.retro_get_system_info(&comm.env.retro_system_info);
	comm.env.retro_api_version = (u32)comm.funs.retro_api_version();

	//now after this we return to the c# side to let some more setup happen

	return &comm;
}


extern "C" __declspec(dllexport) void __cdecl Message(eMessage msg)
{
	if (msg == eMessage::Resume)
	{
		cothread_t temp = co_emu_suspended;
		co_emu_suspended = NULL;
		co_switch(temp);
	}

	if (msg >= eMessage::CMD_FIRST && msg <= eMessage::CMD_LAST)
	{
		//CMD is only valid if status is idle
		if (comm.status != eStatus_Idle)
		{
			printf("ERROR: cmd during non-idle\n");
			return;
		}

		comm.status = eStatus_CMD;
		comm.cmd = msg;

		CMD_cb = kHandlers_CMD[msg - eMessage::CMD_FIRST - 1];
		co_switch(co_emu);

		//we could be in ANY STATE when we return from here
	}

	//QUERY can run any time
	//but... some of them might not be safe for re-entrancy.
	//later, we should have metadata for messages that indicates that
	if (msg >= eMessage::QUERY_FIRST && msg <= eMessage::QUERY_LAST)
	{
		Action cb = kHandlers_QUERY[msg - eMessage::QUERY_FIRST - 1];
		if (cb) cb();
	}
}


//receives the given buffer and COPIES it. use this for returning values from SIGs
extern "C" __declspec(dllexport) void __cdecl CopyBuffer(int id, void* ptr, s32 size)
{
	comm.CopyBuffer(id, ptr, size);
}

//receives the given buffer and STASHES IT. use this (carefully) for sending params for CMDs
extern "C" __declspec(dllexport) void __cdecl SetBuffer(int id, void* ptr, s32 size)
{
	comm.SetBuffer(id, ptr, size);
}

extern "C" __declspec(dllexport) void __cdecl SetVariable(const char* key, const char* val)
{
	for(int i=0;i<comm.env.variable_count;i++)
		if(!strcmp(key,comm.env.variable_keys[i]))
		{
			comm.variables[i] = val;
			comm.variables_dirty = true;
		}
}