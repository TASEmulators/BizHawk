#pragma once

namespace nall {

template<typename T>
struct unique_pointer {
  template<typename... P> static auto create(P&&... p) {
    return unique_pointer<T>{new T{forward<P>(p)...}};
  }

  using type = T;
  T* pointer = nullptr;
  function<void (T*)> deleter;

  unique_pointer(const unique_pointer&) = delete;
  auto operator=(const unique_pointer&) -> unique_pointer& = delete;

  unique_pointer(T* pointer = nullptr, const function<void (T*)>& deleter = {}) : pointer(pointer), deleter(deleter) {}
  ~unique_pointer() { reset(); }

  auto operator=(T* source) -> unique_pointer& {
    reset();
    pointer = source;
    return *this;
  }

  explicit operator bool() const { return pointer; }

  auto operator->() -> T* { return pointer; }
  auto operator->() const -> const T* { return pointer; }

  auto operator*() -> T& { return *pointer; }
  auto operator*() const -> const T& { return *pointer; }

  auto operator()() -> T& { return *pointer; }
  auto operator()() const -> const T& { return *pointer; }

  auto data() -> T* { return pointer; }
  auto data() const -> const T* { return pointer; }

  auto release() -> T* {
    auto result = pointer;
    pointer = nullptr;
    return result;
  }

  auto reset() -> void {
    if(pointer) {
      if(deleter) {
        deleter(pointer);
      } else {
        delete pointer;
      }
      pointer = nullptr;
    }
  }
};

template<typename T>
struct unique_pointer<T[]> {
  using type = T;
  T* pointer = nullptr;
  function<auto (T*) -> void> deleter;

  unique_pointer(const unique_pointer&) = delete;
  auto operator=(const unique_pointer&) -> unique_pointer& = delete;

  unique_pointer(T* pointer = nullptr, const function<void (T*)>& deleter = {}) : pointer(pointer), deleter(deleter) {}
  ~unique_pointer() { reset(); }

  auto operator=(T* source) -> unique_pointer& {
    reset();
    pointer = source;
    return *this;
  }

  explicit operator bool() const { return pointer; }

  auto operator()() -> T* { return pointer; }
  auto operator()() const -> T* { return pointer; }

  alwaysinline auto operator[](uint offset) -> T& { return pointer[offset]; }
  alwaysinline auto operator[](uint offset) const -> const T& { return pointer[offset]; }

  auto data() -> T* { return pointer; }
  auto data() const -> const T* { return pointer; }

  auto release() -> T* {
    auto result = pointer;
    pointer = nullptr;
    return result;
  }

  auto reset() -> void {
    if(pointer) {
      if(deleter) {
        deleter(pointer);
      } else {
        delete[] pointer;
      }
      pointer = nullptr;
    }
  }
};

}
