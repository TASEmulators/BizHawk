#include <nall/thread.hpp>

namespace nall {

#if defined(API_WINDOWS)

NALL_HEADER_INLINE auto WINAPI _threadCallback(void* parameter) -> DWORD {
  auto context = (thread::context*)parameter;
  context->callback(context->parameter);
  delete context;
  return 0;
}

NALL_HEADER_INLINE auto thread::close() -> void {
  if(handle) {
    CloseHandle(handle);
    handle = 0;
  }
}

NALL_HEADER_INLINE auto thread::join() -> void {
  if(handle) {
    //wait until the thread has finished executing ...
    WaitForSingleObject(handle, INFINITE);
    CloseHandle(handle);
    handle = 0;
  }
}

NALL_HEADER_INLINE auto thread::create(const function<void (uintptr)>& callback, uintptr parameter, u32 stacksize) -> thread {
  thread instance;

  auto context = new thread::context;
  context->callback = callback;
  context->parameter = parameter;

  instance.handle = CreateThread(nullptr, stacksize, _threadCallback, (void*)context, 0, nullptr);
  return instance;
}

NALL_HEADER_INLINE auto thread::detach() -> void {
  //Windows threads do not use this concept:
  //~thread() frees resources via CloseHandle()
  //thread continues to run even after handle is closed
}

NALL_HEADER_INLINE auto thread::exit() -> void {
  ExitThread(0);
}

#endif

}
