#include "Platform.h"

#include <emulibc.h>

namespace melonDS::Platform
{

static uintptr_t FrameThreadProc = 0;
static std::function<void()> ThreadEntryFunc = nullptr;
static void (*ThreadStartCallback)() = nullptr;

ECL_EXPORT uintptr_t GetFrameThreadProc()
{
	return FrameThreadProc;
}

ECL_EXPORT void SetThreadStartCallback(void (*callback)())
{
	ThreadStartCallback = callback;
}

static void ThreadEntry()
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

// hijacked to act as a thread start, consider this "wait for start of thread"
void Thread_Wait(Thread* thread)
{
	ThreadStartCallback();
}

}
