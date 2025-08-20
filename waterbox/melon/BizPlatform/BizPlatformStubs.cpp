#include "Platform.h"

namespace melonDS::Platform
{

void SignalStop(StopReason reason, void* userdata)
{
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

bool Semaphore_TryWait(Semaphore* sema, int timeout_ms)
{
	return false;
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

void Sleep(u64 usecs)
{
}

u64 GetMSCount()
{
	return 0;
}

u64 GetUSCount()
{
	return 0;
}

void MP_Begin(void* userdata)
{
}

void MP_End(void* userdata)
{
}

int MP_SendPacket(u8* data, int len, u64 timestamp, void* userdata)
{
	return 0;
}

int MP_RecvPacket(u8* data, u64* timestamp, void* userdata)
{
	return 0;
}

int MP_SendCmd(u8* data, int len, u64 timestamp, void* userdata)
{
	return 0;
}

int MP_SendReply(u8* data, int len, u64 timestamp, u16 aid, void* userdata)
{
	return 0;
}

int MP_SendAck(u8* data, int len, u64 timestamp, void* userdata)
{
	return 0;
}

int MP_RecvHostPacket(u8* data, u64* timestamp, void* userdata)
{
	return 0;
}

u16 MP_RecvReplies(u8* data, u64 timestamp, u16 aidmask, void* userdata)
{
	return 0;
}

int Net_SendPacket(u8* data, int len, void* userdata)
{
	return 0;
}

int Net_RecvPacket(u8* data, void* userdata)
{
	return 0;
}

void Camera_Start(int num, void* userdata)
{
}

void Camera_Stop(int num, void* userdata)
{
}

void Camera_CaptureFrame(int num, u32* frame, int width, int height, bool yuv, void* userdata)
{
	// TODO

	u32 length = width * height;
	if (yuv)
	{
		length /= 2;
	}

	u32 black = yuv ? 0x80008000 : 0x00000000;
	for (u32 i = 0; i < length; i++)
	{
		frame[i] = black;
	}
}

bool Addon_KeyDown(KeyType type, void* userdata)
{
	return false;
}

void Addon_RumbleStart(u32 len, void* userdata)
{
}

void Addon_RumbleStop(void* userdata)
{
}

float Addon_MotionQuery(MotionQueryType type, void* userdata)
{
	return 0;
}

DynamicLibrary* DynamicLibrary_Load(const char* lib)
{
	return nullptr;
}

void DynamicLibrary_Unload(DynamicLibrary* lib)
{
}

void* DynamicLibrary_LoadFunction(DynamicLibrary* lib, const char* name)
{
	return nullptr;
}

}
