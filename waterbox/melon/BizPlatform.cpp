#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <mutex>
#include "Platform.h"
#include "BizConfig.h"

bool NdsSaveRamIsDirty = false;
std::stringstream* NANDFilePtr = NULL;

namespace Platform
{

void Init(int argc, char** argv)
{
}

void DeInit()
{
}

void StopEmu()
{
}

int GetConfigInt(ConfigEntry entry)
{
	const int imgsizes[] = {0, 256, 512, 1024, 2048, 4096};

	switch (entry)
	{
#ifdef JIT_ENABLED
		case JIT_MaxBlockSize: return Config::JIT_MaxBlockSize;
#endif

		case DLDI_ImageSize: return imgsizes[Config::DLDISize];

		case DSiSD_ImageSize: return imgsizes[Config::DSiSDSize];

		case Firm_Language: return Config::FirmwareLanguage;
		case Firm_BirthdayMonth: return Config::FirmwareBirthdayMonth;
		case Firm_BirthdayDay: return Config::FirmwareBirthdayDay;
		case Firm_Color: return Config::FirmwareFavouriteColour;

		case AudioBitrate: return Config::AudioBitrate;
		
		case TimeAtBoot: return Config::TimeAtBoot;

		default: return 0;
	}
}

bool GetConfigBool(ConfigEntry entry)
{
	switch (entry)
	{
#ifdef JIT_ENABLED
		case JIT_Enable: return Config::JIT_Enable != 0;
		case JIT_LiteralOptimizations: return Config::JIT_LiteralOptimisations != 0;
		case JIT_BranchOptimizations: return Config::JIT_BranchOptimisations != 0;
		case JIT_FastMemory: return Config::JIT_FastMemory != 0;
#endif

		case ExternalBIOSEnable: return Config::ExternalBIOSEnable != 0;

		case DLDI_Enable: return Config::DLDIEnable != 0;
		case DLDI_ReadOnly: return Config::DLDIReadOnly != 0;
		case DLDI_FolderSync: return Config::DLDIFolderSync != 0;

		case DSiSD_Enable: return Config::DSiSDEnable != 0;
		case DSiSD_ReadOnly: return Config::DSiSDReadOnly != 0;
		case DSiSD_FolderSync: return Config::DSiSDFolderSync != 0;

		case Firm_RandomizeMAC: return Config::RandomizeMAC != 0;
		case Firm_OverrideSettings: return Config::FirmwareOverrideSettings != 0;

		case UseRealTime: return Config::UseRealTime != 0;
		case FixedBootTime: return Config::FixedBootTime != 0;

		default: return false;
	}
}

std::string GetConfigString(ConfigEntry entry)
{
	switch (entry)
	{
		case BIOS9Path: return Config::BIOS9Path;
		case BIOS7Path: return Config::BIOS7Path;
		case FirmwarePath: return Config::FirmwarePath;

		case DSi_BIOS9Path: return Config::DSiBIOS9Path;
		case DSi_BIOS7Path: return Config::DSiBIOS7Path;
		case DSi_FirmwarePath: return Config::DSiFirmwarePath;
		case DSi_NANDPath: return Config::DSiNANDPath;

		case DLDI_ImagePath: return Config::DLDISDPath;
		case DLDI_FolderPath: return Config::DLDIFolderPath;

		case DSiSD_ImagePath: return Config::DSiSDPath;
		case DSiSD_FolderPath: return Config::DSiSDFolderPath;

		case Firm_Username: return Config::FirmwareUsername;
		case Firm_Message: return Config::FirmwareMessage;

		default: return "";
	}
}

bool GetConfigArray(ConfigEntry entry, void* data)
{
	switch (entry)
	{
		case Firm_MAC:
			{
				std::string& mac_in = Config::FirmwareMAC;
				u8* mac_out = (u8*)data;

				int o = 0;
				u8 tmp = 0;
				for (int i = 0; i < 18; i++)
				{
					char c = mac_in[i];
					if (c == '\0') break;

					int n;
					if      (c >= '0' && c <= '9') n = c - '0';
					else if (c >= 'a' && c <= 'f') n = c - 'a' + 10;
					else if (c >= 'A' && c <= 'F') n = c - 'A' + 10;
					else continue;

					if (!(o & 1))
						tmp = n;
					else
						mac_out[o >> 1] = n | (tmp << 4);

					o++;
					if (o >= 12) return true;
				}
				return false;
			}

		default: return false;
	}
}


FILE* OpenFile(std::string path, std::string mode, bool mustexist)
{
	if (path == Config::DSiNANDPath) return reinterpret_cast<FILE*>(NANDFilePtr);

	return fopen(path.c_str(), mode.c_str());
}

FILE* OpenLocalFile(std::string path, std::string mode)
{
	if (path == Config::DSiNANDPath) return reinterpret_cast<FILE*>(NANDFilePtr);

	return fopen(path.c_str(), mode.c_str());
}

uintptr_t FrameThreadProc = 0;
std::function<void()> ThreadEntryFunc = nullptr;
void (*ThreadStartCallback)() = nullptr;

void ThreadEntry()
{
	ThreadEntryFunc();
}

Thread* Thread_Create(std::function<void()> func)
{
	ThreadEntryFunc = func;
	FrameThreadProc = reinterpret_cast<uintptr_t>(ThreadEntry);
	return nullptr;
}

void Thread_Free(Thread* thread)
{
}

void Thread_Wait(Thread* thread) // hijacked to act as a thread start, consider this "wait for start of thread"
{
	ThreadStartCallback();
}

Semaphore* Semaphore_Create()
{
	return nullptr;
}

void Semaphore_Free(Semaphore* sema)
{
}

void Semaphore_Reset(Semaphore* sema)
{
}

void Semaphore_Wait(Semaphore* sema)
{
}

void Semaphore_Post(Semaphore* sema, int count)
{
}

Mutex* Mutex_Create()
{
	return nullptr;
}

void Mutex_Free(Mutex* mutex)
{
}

void Mutex_Lock(Mutex* mutex)
{
}

void Mutex_Unlock(Mutex* mutex)
{
}

bool Mutex_TryLock(Mutex* mutex)
{
	return false;
}


void WriteNDSSave(const u8* savedata, u32 savelen, u32 writeoffset, u32 writelen)
{
	NdsSaveRamIsDirty = true;
}

void WriteGBASave(const u8* savedata, u32 savelen, u32 writeoffset, u32 writelen)
{
}


bool MP_Init()
{
	return false;
}

void MP_DeInit()
{
}

int MP_SendPacket(u8* data, int len)
{
	return 0;
}

int MP_RecvPacket(u8* data, bool block)
{
	return 0;
}

bool LAN_Init()
{
	return false;
}

void LAN_DeInit()
{
}

int LAN_SendPacket(u8* data, int len)
{
	return 0;
}

int LAN_RecvPacket(u8* data)
{
	return 0;
}

void Sleep(u64 usecs)
{
}

}
