#include <Windows.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <string>
#include <vector>

#define LIBSNES_IMPORT
#include "../bsnes/target-libsnes/libsnes.hpp"

HANDLE hPipe, hMapFile;
void* hMapFilePtr;

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

	eMessage_snes_load_cartridge_normal, //10

	eMessage_snes_cb_video_refresh,
	eMessage_snes_cb_input_poll,
	eMessage_snes_cb_input_state,
	eMessage_snes_cb_input_notify,
	eMessage_snes_cb_audio_sample,
	eMessage_snes_cb_scanlineStart, //16
	eMessage_snes_cb_path_request,
	eMessage_snes_cb_trace_callback,

	eMessage_snes_get_region,

	eMessage_snes_get_memory_size, //20
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
	eMessage_snes_peek_logical_register
};

void ReadPipeBuffer(void* buf, int len)
{
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

FILE* outf = NULL;

void WritePipeBuffer(const void* buf, int len)
{
	//if(!outf) outf = fopen("c:\\trace.bin","wb"); fwrite(buf,1,len,outf); fflush(outf);
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
	//TODO - write string length
	int len = strlen(str);
	WritePipeBuffer(str,len+1);
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
void snes_scanlineStart(int line)
{
	WritePipe(eMessage_snes_cb_scanlineStart);
	WritePipe(line);
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
}

int main(int argc, char** argv)
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

	InitBsnes();

	char pipename[256];
	sprintf(pipename, "\\\\.\\Pipe\\%s",argv[1]);

	hPipe = CreateFile(pipename, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

	if(hPipe == INVALID_HANDLE_VALUE)
		return 1;

	hMapFile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, argv[1]);
	if(hMapFile == INVALID_HANDLE_VALUE)
		return 1;

	hMapFilePtr = MapViewOfFile(hMapFile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);

	for(;;)
	{
		auto msg = ReadPipe<eMessage>();
		switch(msg)
		{
		case eMessage_snes_library_id: WritePipeString(snes_library_id()); break;
		case eMessage_snes_library_revision_major: WritePipe(snes_library_revision_major()); break;
		case eMessage_snes_library_revision_minor: WritePipe(snes_library_revision_minor()); break;

		case eMessage_snes_init: 
			snes_init(); 
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

		case eMessage_snes_get_region:
			WritePipe((char)snes_get_region());
			break;

		case eMessage_snes_get_memory_size:
			WritePipe(snes_get_memory_size(ReadPipe<unsigned int>()));
			break;

		case eMessage_snes_get_memory_data:
			{
				unsigned int id = ReadPipe<unsigned int>();
				char* dstbuf = ReadPipeSharedPtr();
				uint8_t* srcbuf = snes_get_memory_data(id);
				memcpy(dstbuf,srcbuf,snes_get_memory_size(id));
				WritePipe(eMessage_Complete);
				break;
			}
			
		case eMessage_peek:
			{
				int id = ReadPipe<int>();
				unsigned int addr = ReadPipe<unsigned int>();
				uint8_t ret;
				if(id == SNES_MEMORY_SYSBUS)
					ret = bus_read(addr);
				else ret = snes_get_memory_data(id)[addr];
				WritePipe(ret);
			}
			break;

		case eMessage_poke:
			{
				int id = ReadPipe<int>();
				unsigned int addr = ReadPipe<unsigned int>();
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
				int size = ReadPipe<int>();
				int destOfs = ReadPipe<int>();
				char* buf = (char*)hMapFilePtr + destOfs;
				bool ret = snes_serialize((uint8_t*)buf,size);
				WritePipe(eMessage_Complete);
				WritePipe((char)(ret?1:0));
				break;
			}
		case eMessage_snes_unserialize:
			{
				//auto blob = ReadPipeBlob();
				int size = ReadPipe<int>();
				int destOfs = ReadPipe<int>();
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
				int layer = ReadPipe<int>();
				int priority = ReadPipe<int>();
				bool enable = ReadPipe<bool>();
				snes_set_layer_enable(layer,priority,enable);
				break;
			}

		case eMessage_snes_set_backdropColor:
			snes_set_backdropColor(ReadPipe<int>());
			break;

		case eMessage_snes_peek_logical_register:
			WritePipe(snes_peek_logical_register(ReadPipe<int>()));
			break;

		} //switch(msg)
	}

	return 0;
}

int CALLBACK WinMain(HINSTANCE hInstance,HINSTANCE hPrevInstance,LPSTR lpCmdLine,int nCmdShow)
{
	int argc = __argc;
	char** argv = __argv;

	if(argc != 2)
	{
		if(IDCANCEL == MessageBox(0,"This program is run from the libsneshawk emulator core. It is useless to you directly. But if you're really, that curious, click cancel.","Whatfor my daddy-o",MB_OKCANCEL))
		{
			ShellExecute(0,"open","http://www.youtube.com/watch?v=boanuwUMNNQ#t=98s",NULL,NULL,SW_SHOWNORMAL);
		}
		exit(1);
		
	}
	main(argc,argv);
}