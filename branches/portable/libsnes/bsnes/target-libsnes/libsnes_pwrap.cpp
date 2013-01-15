#include <Windows.h>

#define LIBSNES_IMPORT
#include "snes/snes.hpp"
#include "libsnes.hpp"

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <map>
#include <string>
#include <vector>

typedef uint8 u8;
typedef int32 s32;
typedef uint32 u32;

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
		}
	}

	int Size()
	{
		int h = *head;
		int t = *tail;
		int size = h - t;
		if (size < 0) size += bufsize;
		else if (size >= bufsize)
		{
			//shouldnt be possible for size to be anything but bufsize here
			size = 0;
		}
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

HANDLE hPipe, hMapFile;
void* hMapFilePtr;
static bool running = false;

enum eMessage : int
{
	eMessage_Complete,

	eMessage_snes_library_id,
	eMessage_snes_library_revision_major,
	eMessage_snes_library_revision_minor,

	eMessage_snes_init,
	eMessage_snes_power,
	eMessage_snes_reset,
	eMessage_snes_run,
	eMessage_snes_term,
	eMessage_snes_unload_cartridge,

	//snes_set_cartridge_basename, //not used

	eMessage_snes_load_cartridge_normal,
	eMessage_snes_load_cartridge_super_game_boy,

	eMessage_snes_cb_video_refresh,
	eMessage_snes_cb_input_poll,
	eMessage_snes_cb_input_state,
	eMessage_snes_cb_input_notify,
	eMessage_snes_cb_audio_sample,
	eMessage_snes_cb_scanlineStart,
	eMessage_snes_cb_path_request,
	eMessage_snes_cb_trace_callback,

	eMessage_snes_get_region,

	eMessage_snes_get_memory_size,
	eMessage_snes_get_memory_data,
	eMessage_peek,
	eMessage_poke,

	eMessage_snes_serialize_size,

	eMessage_snes_serialize,
	eMessage_snes_unserialize,

	eMessage_snes_poll_message,
	eMessage_snes_dequeue_message,

	eMessage_snes_set_color_lut,
	
	eMessage_snes_enable_trace,
	eMessage_snes_enable_scanline,
	eMessage_snes_enable_audio,
	eMessage_snes_set_layer_enable,
	eMessage_snes_set_backdropColor,
	eMessage_snes_peek_logical_register,

	eMessage_snes_allocSharedMemory,
	eMessage_snes_freeSharedMemory,
	eMessage_GetMemoryIdName,

	eMessage_SetBuffer,
	eMessage_BeginBufferIO,
	eMessage_EndBufferIO
};

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
	WritePipe(eMessage_snes_cb_video_refresh);
	WritePipe(width);
	WritePipe(height);
	int destOfs = ReadPipe<int>();
	char* buf = (char*)hMapFilePtr + destOfs;
	int bufsize = 512 * 480 * 4;
	memcpy(buf,data,bufsize);
	WritePipe((char)0); //dummy synchronization
}

bool audio_en = false;
static const int AUDIOBUFFER_SIZE = 44100*2;
uint16_t audiobuffer[AUDIOBUFFER_SIZE];
int audiobuffer_idx = 0;

void FlushAudio()
{
	if(audiobuffer_idx == 0) return;

	WritePipe(eMessage_snes_cb_audio_sample);

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

void snes_audio_sample(uint16_t left, uint16_t right)
{
	if(!audio_en) return;

	if(audiobuffer_idx == AUDIOBUFFER_SIZE)
		FlushAudio();
	audiobuffer[audiobuffer_idx++] = left;
	audiobuffer[audiobuffer_idx++] = right;
}

void snes_input_poll(void)
{
	WritePipe(eMessage_snes_cb_input_poll);
}
int16_t snes_input_state(unsigned port, unsigned device, unsigned index, unsigned id)
{
	WritePipe(eMessage_snes_cb_input_state);
	WritePipe(port);
	WritePipe(device);
	WritePipe(index);
	WritePipe(id);
	return ReadPipe<int16_t>();
}
void snes_input_notify(int index)
{
	WritePipe(eMessage_snes_cb_input_notify);
	WritePipe(index);
}

void snes_trace(const char *msg)
{
	WritePipe(eMessage_snes_cb_trace_callback);
	WritePipeString(msg);
}

const char* snes_path_request(int slot, const char* hint)
{
	//yuck
	static char ret[MAX_PATH];
	WritePipe(eMessage_snes_cb_path_request);
	WritePipe(slot);
	WritePipeString(hint);
	std::string str = ReadPipeString();
	strcpy(ret,str.c_str());
	return ret;
}

void RunMessageLoop();
void snes_scanlineStart(int line)
{
	WritePipe(eMessage_snes_cb_scanlineStart);
	WritePipe(line);

	//we've got to wait for the frontend to finish processing.
	//in theory we could let emulation proceed after snagging the vram and registers, and do decoding and stuff on another thread...
	//but its too hard for now.
	RunMessageLoop();
}

class SharedMemoryBlock
{
public:
	std::string memtype;
	HANDLE handle;
};

static std::map<void*,SharedMemoryBlock*> memHandleTable;

void* snes_allocSharedMemory(const char* memtype, size_t amt)
{
	if(!running) return NULL;
	WritePipe(eMessage_snes_allocSharedMemory);
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
	
	return ptr;
}

void snes_freeSharedMemory(void* ptr)
{
	if(!running) return;
	if(!ptr) return;
	auto smb = memHandleTable.find(ptr)->second;
	UnmapViewOfFile(ptr);
	CloseHandle(smb->handle);
	WritePipe(eMessage_snes_freeSharedMemory);
	WritePipeString(smb->memtype.c_str());
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

void RunMessageLoop()
{
	for(;;)
	{
		//printf("Reading message from pipe...\n");
		auto msg = ReadPipe<eMessage>();
		//printf("slam %08X\n",msg);
		switch(msg)
		{
		case eMessage_Complete:
			return;

		case eMessage_snes_library_id: WritePipeString(snes_library_id()); break;
		case eMessage_snes_library_revision_major: WritePipe(snes_library_revision_major()); break;
		case eMessage_snes_library_revision_minor: WritePipe(snes_library_revision_minor()); break;

		case eMessage_snes_init: 
			snes_init(); 
			WritePipe(eMessage_Complete);
			break;
		case eMessage_snes_power: snes_power(); break;
		case eMessage_snes_reset: snes_reset(); break;
		case eMessage_snes_run: 
			FlushAudio();
			snes_run();
			FlushAudio();
			WritePipe(eMessage_Complete);
			break;
		case eMessage_snes_term: snes_term(); break;
		case eMessage_snes_unload_cartridge: snes_unload_cartridge(); break;

		case eMessage_snes_load_cartridge_normal:
			{
				std::string xml = ReadPipeString();
				Blob rom_data = ReadPipeBlob();
				const char* xmlptr = NULL;
				if(xml != "") xmlptr = xml.c_str();
				bool ret = snes_load_cartridge_normal(xmlptr,(unsigned char*)&rom_data[0],rom_data.size());
				WritePipe(eMessage_Complete);
				WritePipe((char)(ret?1:0));
				break;
			}

		case eMessage_snes_load_cartridge_super_game_boy:
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
				
				WritePipe(eMessage_Complete);
				WritePipe((char)(ret?1:0));
				
				break;
			}

		case eMessage_snes_get_region:
			WritePipe((char)snes_get_region());
			break;

		case eMessage_snes_get_memory_size:
			WritePipe(snes_get_memory_size(ReadPipe<u32>()));
			break;

		case eMessage_snes_get_memory_data:
			{
				unsigned int id = ReadPipe<u32>();
				char* dstbuf = ReadPipeSharedPtr();
				uint8_t* srcbuf = snes_get_memory_data(id);
				memcpy(dstbuf,srcbuf,snes_get_memory_size(id));
				WritePipe(eMessage_Complete);
				break;
			}
			
		case eMessage_peek:
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

		case eMessage_poke:
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

		case eMessage_snes_serialize_size:
			WritePipe(snes_serialize_size());
			break;

		case eMessage_snes_serialize:
			{
				int size = ReadPipe<s32>();
				int destOfs = ReadPipe<s32>();
				char* buf = (char*)hMapFilePtr + destOfs;
				bool ret = snes_serialize((uint8_t*)buf,size);
				WritePipe(eMessage_Complete);
				WritePipe((char)(ret?1:0));
				break;
			}
		case eMessage_snes_unserialize:
			{
				//auto blob = ReadPipeBlob();
				int size = ReadPipe<s32>();
				int destOfs = ReadPipe<s32>();
				char* buf = (char*)hMapFilePtr + destOfs;
				bool ret = snes_unserialize((uint8_t*)buf	,size);
				WritePipe(eMessage_Complete);
				WritePipe((char)(ret?1:0));
				break;
			}

		case eMessage_snes_poll_message:
			//TBD
			WritePipe(-1);
			break;
		case eMessage_snes_dequeue_message:
			//TBD
			break;

		case eMessage_snes_set_color_lut:
			{
				auto blob = ReadPipeBlob();
				snes_set_color_lut((uint32_t*)&blob[0]);
				break;
			}
			break;

		case eMessage_snes_enable_trace:
			if(!!ReadPipe<char>())
				snes_set_trace_callback(snes_trace);
			else snes_set_trace_callback(NULL);
			break;

		case eMessage_snes_enable_scanline:
			if(ReadPipe<bool>())
				snes_set_scanlineStart(snes_scanlineStart);
			else snes_set_scanlineStart(NULL);
			break;

		case eMessage_snes_enable_audio:
			audio_en = ReadPipe<bool>();
			break;
	
		case eMessage_snes_set_layer_enable:
			{
				int layer = ReadPipe<s32>();
				int priority = ReadPipe<s32>();
				bool enable = ReadPipe<bool>();
				snes_set_layer_enable(layer,priority,enable);
				break;
			}

		case eMessage_snes_set_backdropColor:
			snes_set_backdropColor(ReadPipe<s32>());
			break;

		case eMessage_snes_peek_logical_register:
			WritePipe(snes_peek_logical_register(ReadPipe<s32>()));
			break;

		case eMessage_GetMemoryIdName:
			{
				uint32 id = ReadPipe<uint32>();
				const char* ret = snes_get_memory_id_name(id);
				if(!ret) ret = "";
				WritePipeString(ret);
				break;
			}

		case eMessage_SetBuffer:
			{
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


void OpenConsole() 
{
	AllocConsole();
	freopen("CONOUT$", "w", stdout);
	freopen("CONOUT$", "w", stderr);
	freopen("CONIN$", "r", stdin);
}

int xmain(int argc, char** argv)
{
	if(argc != 2)
	{
		printf("This program is run from the libsneshawk emulator core. It is useless to you directly.");
		exit(1);
	}

	if(!strcmp(argv[1],"Bongizong"))
	{
		fprintf(stderr,"Honga Wongkong");
		exit(0x16817);
	}

	char pipename[256];
	sprintf(pipename, "\\\\.\\Pipe\\%s",argv[1]);

	if(!strncmp(argv[1],"console",7))
	{
		OpenConsole();
	}
	
	printf("pipe: %s\n",pipename);

	hPipe = CreateFile(pipename, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

	if(hPipe == INVALID_HANDLE_VALUE)
		return 1;

	hMapFile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, argv[1]);
	if(hMapFile == INVALID_HANDLE_VALUE)
		return 1;

	hMapFilePtr = MapViewOfFile(hMapFile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

	running = true;
	printf("running\n");

	RunMessageLoop();
	
	return 0;
}

int CALLBACK WinMain(HINSTANCE hInstance,HINSTANCE hPrevInstance,LPSTR lpCmdLine,int nCmdShow)
{
	int argc = __argc;
	char** argv = __argv;

	if(argc != 2)
	{
		if(IDOK == MessageBox(0,"This program is run from the libsneshawk emulator core. It is useless to you directly. But if you're really, that curious, click OK.","Whatfor my daddy-o",MB_OKCANCEL))
		{
			ShellExecute(0,"open","http://www.youtube.com/watch?v=boanuwUMNNQ#t=98s",NULL,NULL,SW_SHOWNORMAL);
		}
		exit(1);
		
	}
	xmain(argc,argv);
}

void pwrap_init()
{
	InitBsnes();
}