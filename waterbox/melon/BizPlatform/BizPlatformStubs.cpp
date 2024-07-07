#include "Platform.h"

namespace melonDS::Platform
{

void Init(int argc, char** argv)
{
}

void DeInit()
{
}

void SignalStop(StopReason reason)
{
}

int InstanceID()
{
	return 0;
}

std::string InstanceFileSuffix()
{
	return "";
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

bool MP_Init()
{
	return false;
}

void MP_DeInit()
{
}

void MP_Begin()
{
}

void MP_End()
{
}

int MP_SendPacket(u8* data, int len, u64 timestamp)
{
	return 0;
}

int MP_RecvPacket(u8* data, u64* timestamp)
{
	return 0;
}

int MP_SendCmd(u8* data, int len, u64 timestamp)
{
	return 0;
}

int MP_SendReply(u8* data, int len, u64 timestamp, u16 aid)
{
	return 0;
}

int MP_SendAck(u8* data, int len, u64 timestamp)
{
	return 0;
}

int MP_RecvHostPacket(u8* data, u64* timestamp)
{
	return 0;
}

u16 MP_RecvReplies(u8* data, u64 timestamp, u16 aidmask)
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

void Camera_Start(int num)
{
}

void Camera_Stop(int num)
{
}

void Camera_CaptureFrame(int num, u32* frame, int width, int height, bool yuv)
{
	// TODO
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
