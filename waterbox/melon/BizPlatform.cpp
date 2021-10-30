#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <semaphore.h>
#include <thread>
#include <mutex>
#include "Platform.h"

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

    /*switch (entry)
    {
    case DLDI_ImageSize: return imgsizes[Config::DLDISize];

    case DSiSD_ImageSize: return imgsizes[Config::DSiSDSize];
    }*/

    return 0;
}

bool GetConfigBool(ConfigEntry entry)
{
    /*switch (entry)
    {
    case DLDI_Enable: return Config::DLDIEnable != 0;
    case DLDI_ReadOnly: return Config::DLDIReadOnly != 0;
    case DLDI_FolderSync: return Config::DLDIFolderSync != 0;

    case DSiSD_Enable: return Config::DSiSDEnable != 0;
    case DSiSD_ReadOnly: return Config::DSiSDReadOnly != 0;
    case DSiSD_FolderSync: return Config::DSiSDFolderSync != 0;
    }*/

    return false;
}

std::string GetConfigString(ConfigEntry entry)
{
    /*switch (entry)
    {
    case DLDI_ImagePath: return Config::DLDISDPath;
    case DLDI_FolderPath: return Config::DLDIFolderPath;

    case DSiSD_ImagePath: return Config::DSiSDPath;
    case DSiSD_FolderPath: return Config::DSiSDFolderPath;
    }*/

    return "";
}

FILE* OpenFile(const char* path, const char* mode, bool mustexist)
{
    return fopen(path, mode);
}

FILE* OpenLocalFile(const char* path, const char* mode)
{
    return fopen(path, mode);
}

Thread* Thread_Create(std::function<void()> func)
{
    std::thread* t = new std::thread(func);
    return (Thread*) t;
}

void Thread_Free(Thread* thread)
{
    delete (std::thread*) thread;
}

void Thread_Wait(Thread* thread)
{
	((std::thread*) thread)->join();
}

Semaphore* Semaphore_Create()
{
    sem_t* s = new sem_t;
    sem_init(s, 0, 1);
    return (Semaphore*) s;
}

void Semaphore_Free(Semaphore* sema)
{
    sem_destroy((sem_t*) sema);
    delete (sem_t*) sema;
}

void Semaphore_Reset(Semaphore* sema)
{
    while (!sem_trywait((sem_t*) sema)) {};
}

void Semaphore_Wait(Semaphore* sema)
{
    sem_wait((sem_t*) sema);
}

void Semaphore_Post(Semaphore* sema, int count)
{
    while (count--) sem_post((sem_t*) sema);
}

Mutex* Mutex_Create()
{
    std::mutex* m = new std::mutex();
    return (Mutex*) m;
}

void Mutex_Free(Mutex* mutex)
{
    delete (std::mutex*) mutex;
}

void Mutex_Lock(Mutex* mutex)
{
    ((std::mutex*) mutex)->lock();
}

void Mutex_Unlock(Mutex* mutex)
{
    ((std::mutex*) mutex)->unlock();
}

bool Mutex_TryLock(Mutex* mutex)
{
    return ((std::mutex*) mutex)->try_lock();
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
