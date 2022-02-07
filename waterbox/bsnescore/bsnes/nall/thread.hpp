#pragma once

//simple thread library
//primary rationale is that std::thread does not support custom stack sizes
//this is highly critical in certain applications such as threaded web servers
//an added bonus is that it avoids licensing issues on Windows
//win32-pthreads (needed for std::thread) is licensed under the GPL only

#include <nall/platform.hpp>
#include <nall/function.hpp>
#include <nall/intrinsics.hpp>

#if defined(API_POSIX)

#include <pthread.h>

namespace nall {

struct thread {
  inline auto join() -> void;

  static inline auto create(const function<void (uintptr)>& callback, uintptr parameter = 0, uint stacksize = 0) -> thread;
  static inline auto detach() -> void;
  static inline auto exit() -> void;

  struct context {
    function<auto (uintptr) -> void> callback;
    uintptr parameter = 0;
  };

private:
  pthread_t handle;
};

inline auto _threadCallback(void* parameter) -> void* {
  auto context = (thread::context*)parameter;
  context->callback(context->parameter);
  delete context;
  return nullptr;
}

auto thread::join() -> void {
  pthread_join(handle, nullptr);
}

auto thread::create(const function<void (uintptr)>& callback, uintptr parameter, uint stacksize) -> thread {
  thread instance;

  auto context = new thread::context;
  context->callback = callback;
  context->parameter = parameter;

  pthread_attr_t attr;
  pthread_attr_init(&attr);
  if(stacksize) pthread_attr_setstacksize(&attr, max(PTHREAD_STACK_MIN, stacksize));

  pthread_create(&instance.handle, &attr, _threadCallback, (void*)context);
  return instance;
}

auto thread::detach() -> void {
  pthread_detach(pthread_self());
}

auto thread::exit() -> void {
  pthread_exit(nullptr);
}

}

#elif defined(API_WINDOWS)

namespace nall {

struct thread {
  inline ~thread();
  inline auto join() -> void;

  static inline auto create(const function<void (uintptr)>& callback, uintptr parameter = 0, uint stacksize = 0) -> thread;
  static inline auto detach() -> void;
  static inline auto exit() -> void;

  struct context {
    function<auto (uintptr) -> void> callback;
    uintptr parameter = 0;
  };

private:
  HANDLE handle = 0;
};

inline auto WINAPI _threadCallback(void* parameter) -> DWORD {
  auto context = (thread::context*)parameter;
  context->callback(context->parameter);
  delete context;
  return 0;
}

thread::~thread() {
  if(handle) {
    CloseHandle(handle);
    handle = 0;
  }
}

auto thread::join() -> void {
  if(handle) {
    WaitForSingleObject(handle, INFINITE);
    CloseHandle(handle);
    handle = 0;
  }
}

auto thread::create(const function<void (uintptr)>& callback, uintptr parameter, uint stacksize) -> thread {
  thread instance;

  auto context = new thread::context;
  context->callback = callback;
  context->parameter = parameter;

  instance.handle = CreateThread(nullptr, stacksize, _threadCallback, (void*)context, 0, nullptr);
  return instance;
}

auto thread::detach() -> void {
  //Windows threads do not use this concept:
  //~thread() frees resources via CloseHandle()
  //thread continues to run even after handle is closed
}

auto thread::exit() -> void {
  ExitThread(0);
}

}

#endif
