#pragma once

//simple thread library
//primary rationale is that std::thread does not support custom stack sizes
//this is highly critical in certain applications such as threaded web servers
//an added bonus is that it avoids licensing issues on Windows
//win32-pthreads (needed for std::thread) is licensed under the GPL only

#include <nall/platform.hpp>
#include <nall/function.hpp>
#include <nall/intrinsics.hpp>

namespace nall {
  using mutex = std::mutex;
  using recursive_mutex = std::recursive_mutex;
  using condition_variable = std::condition_variable;
  template<typename T> using lock_guard = std::lock_guard<T>;
  template<typename T> using atomic = std::atomic<T>;
  template<typename T> using unique_lock = std::unique_lock<T>;
}

#if defined(API_POSIX)

#include <pthread.h>

namespace nall {

struct thread {
  thread(const thread&) = delete;
  auto operator=(const thread&) -> thread& = delete;

  thread() = default;
  thread(thread&& source) { operator=(std::move(source)); }

  auto operator=(thread&& source) -> thread& {
    if(this == &source) return *this;
    handle = source.handle;
    source.handle = 0;
    return *this;
  }

  auto join() -> void;

  static auto create(const function<void (uintptr)>& callback, uintptr parameter = 0, u32 stacksize = 0) -> thread;
  static auto detach() -> void;
  static auto exit() -> void;

  struct context {
    function<auto (uintptr) -> void> callback;
    uintptr parameter = 0;
  };

private:
  pthread_t handle = (pthread_t)nullptr;
};

inline auto _threadCallback(void* parameter) -> void* {
  auto context = (thread::context*)parameter;
  context->callback(context->parameter);
  delete context;
  return nullptr;
}

inline auto thread::join() -> void {
  if(handle) {
    pthread_join(handle, nullptr);
    handle = 0;
  }
}

inline auto thread::create(const function<void (uintptr)>& callback, uintptr parameter, u32 stacksize) -> thread {
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

inline auto thread::detach() -> void {
  pthread_detach(pthread_self());
}

inline auto thread::exit() -> void {
  pthread_exit(nullptr);
}

}

#elif defined(API_WINDOWS)

namespace nall {

struct thread {
  thread(const thread&) = delete;
  auto operator=(const thread&) -> thread& = delete;

  thread() = default;
  thread(thread&& source) { operator=(std::move(source)); }

  ~thread() { close(); }

  auto operator=(thread&& source) -> thread& {
    if(this == &source) return *this;
    close();
    handle = source.handle;
    source.handle = 0;
    return *this;
  }

  auto close() -> void;
  auto join() -> void;

  static auto create(const function<void (uintptr)>& callback, uintptr parameter = 0, u32 stacksize = 0) -> thread;
  static auto detach() -> void;
  static auto exit() -> void;

  struct context {
    function<auto (uintptr) -> void> callback;
    uintptr parameter = 0;
  };

private:
  HANDLE handle = 0;
};

}

#endif

#if defined(NALL_HEADER_ONLY)
  #include <nall/thread.cpp>
#endif
