#pragma once

namespace nall {

template<typename T>
struct pointer {
  explicit operator bool() const { return value; }

  pointer() = default;
  pointer(T* source) { value = source; }
  pointer(const pointer& source) { value = source.value; }

  auto& operator=(T* source) { value = source; return *this; }
  auto& operator=(const pointer& source) { value = source.value; return *this; }

  auto operator()() -> T* { return value; }
  auto operator()() const -> const T* { return value; }

  auto operator->() -> T* { return value; }
  auto operator->() const -> const T* { return value; }

  auto operator*() -> T& { return *value; }
  auto operator*() const -> const T& { return *value; }

  auto reset() -> void { value = nullptr; }

  auto data() -> T* { return value; }
  auto data() const -> const T* { return value; }

private:
  T* value = nullptr;
};

}
