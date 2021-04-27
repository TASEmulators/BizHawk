#pragma once

#include <nall/function.hpp>
#include <nall/maybe.hpp>
#include <nall/traits.hpp>
#include <nall/vector.hpp>

namespace nall {

template<typename T> struct shared_pointer;

struct shared_pointer_manager {
  void* pointer = nullptr;
  function<void (void*)> deleter;
  uint strong = 0;
  uint weak = 0;

  shared_pointer_manager(void* pointer) : pointer(pointer) {
  }
};

template<typename T> struct shared_pointer;
template<typename T> struct shared_pointer_weak;
template<typename T> struct shared_pointer_this;
struct shared_pointer_this_base{};

template<typename T>
struct shared_pointer {
  template<typename... P> static auto create(P&&... p) {
    return shared_pointer<T>{new T{forward<P>(p)...}};
  }

  using type = T;
  shared_pointer_manager* manager = nullptr;

  template<typename U>
  struct is_compatible {
    static constexpr bool value = is_base_of<T, U>::value || is_base_of<U, T>::value;
  };

  shared_pointer() {
  }

  shared_pointer(T* source) {
    operator=(source);
  }

  shared_pointer(T* source, const function<void (T*)>& deleter) {
    operator=(source);
    manager->deleter = function<void (void*)>([=](void* p) {
      deleter((T*)p);
    });
  }

  shared_pointer(const shared_pointer& source) {
    operator=(source);
  }

  shared_pointer(shared_pointer&& source) {
    operator=(move(source));
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  shared_pointer(const shared_pointer<U>& source) {
    operator=<U>(source);
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  shared_pointer(shared_pointer<U>&& source) {
    operator=<U>(move(source));
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  shared_pointer(const shared_pointer_weak<U>& source) {
    operator=<U>(source);
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  shared_pointer(const shared_pointer<U>& source, T* pointer) {
    if((bool)source && (T*)source.manager->pointer == pointer) {
      manager = source.manager;
      manager->strong++;
    }
  }

  ~shared_pointer() {
    reset();
  }

  auto operator=(T* source) -> shared_pointer& {
    reset();
    if(source) {
      manager = new shared_pointer_manager((void*)source);
      manager->strong++;
      if constexpr(is_base_of_v<shared_pointer_this_base, T>) {
        source->weak = *this;
      }
    }
    return *this;
  }

  auto operator=(const shared_pointer& source) -> shared_pointer& {
    if(this != &source) {
      reset();
      if((bool)source) {
        manager = source.manager;
        manager->strong++;
      }
    }
    return *this;
  }

  auto operator=(shared_pointer&& source) -> shared_pointer& {
    if(this != &source) {
      reset();
      manager = source.manager;
      source.manager = nullptr;
    }
    return *this;
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  auto operator=(const shared_pointer<U>& source) -> shared_pointer& {
    if((uintptr)this != (uintptr)&source) {
      reset();
      if((bool)source) {
        manager = source.manager;
        manager->strong++;
      }
    }
    return *this;
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  auto operator=(shared_pointer&& source) -> shared_pointer& {
    if((uintptr)this != (uintptr)&source) {
      reset();
      manager = source.manager;
      source.manager = nullptr;
    }
    return *this;
  }

  template<typename U, typename = enable_if_t<is_compatible<U>::value>>
  auto operator=(const shared_pointer_weak<U>& source) -> shared_pointer& {
    reset();
    if((bool)source) {
      manager = source.manager;
      manager->strong++;
    }
    return *this;
  }

  auto data() -> T* {
    if(manager) return (T*)manager->pointer;
    return nullptr;
  }

  auto data() const -> const T* {
    if(manager) return (T*)manager->pointer;
    return nullptr;
  }

  auto operator->() -> T* { return data(); }
  auto operator->() const -> const T* { return data(); }

  auto operator*() -> T& { return *data(); }
  auto operator*() const -> const T& { return *data(); }

  auto operator()() -> T& { return *data(); }
  auto operator()() const -> const T& { return *data(); }

  template<typename U>
  auto operator==(const shared_pointer<U>& source) const -> bool {
    return manager == source.manager;
  }

  template<typename U>
  auto operator!=(const shared_pointer<U>& source) const -> bool {
    return manager != source.manager;
  }

  explicit operator bool() const {
    return manager && manager->strong;
  }

  auto unique() const -> bool {
    return manager && manager->strong == 1;
  }

  auto references() const -> uint {
    return manager ? manager->strong : 0;
  }

  auto reset() -> void {
    if(manager && manager->strong) {
      //pointer may contain weak references; if strong==0 it may destroy manager
      //as such, we must destroy strong before decrementing it to zero
      if(manager->strong == 1) {
        if(manager->deleter) {
          manager->deleter(manager->pointer);
        } else {
          delete (T*)manager->pointer;
        }
        manager->pointer = nullptr;
      }
      if(--manager->strong == 0) {
        if(manager->weak == 0) {
          delete manager;
        }
      }
    }
    manager = nullptr;
  }

  template<typename U>
  auto cast() -> shared_pointer<U> {
    if(auto pointer = dynamic_cast<U*>(data())) {
      return {*this, pointer};
    }
    return {};
  }
};

template<typename T>
struct shared_pointer_weak {
  using type = T;
  shared_pointer_manager* manager = nullptr;

  shared_pointer_weak() {
  }

  shared_pointer_weak(const shared_pointer<T>& source) {
    operator=(source);
  }

  auto operator=(const shared_pointer<T>& source) -> shared_pointer_weak& {
    reset();
    if(manager = source.manager) manager->weak++;
    return *this;
  }

  ~shared_pointer_weak() {
    reset();
  }

  auto operator==(const shared_pointer_weak& source) const -> bool {
    return manager == source.manager;
  }

  auto operator!=(const shared_pointer_weak& source) const -> bool {
    return manager != source.manager;
  }

  explicit operator bool() const {
    return manager && manager->strong;
  }

  auto acquire() const -> shared_pointer<T> {
    return shared_pointer<T>(*this);
  }

  auto reset() -> void {
    if(manager && --manager->weak == 0) {
      if(manager->strong == 0) {
        delete manager;
      }
    }
    manager = nullptr;
  }
};

template<typename T>
struct shared_pointer_this : shared_pointer_this_base {
  shared_pointer_weak<T> weak;
  auto shared() -> shared_pointer<T> { return weak; }
  auto shared() const -> shared_pointer<T const> { return weak; }
};

template<typename T, typename... P>
auto shared_pointer_make(P&&... p) -> shared_pointer<T> {
  return shared_pointer<T>{new T{forward<P>(p)...}};
}

template<typename T>
struct shared_pointer_new : shared_pointer<T> {
  shared_pointer_new(const shared_pointer<T>& source) : shared_pointer<T>(source) {}
  template<typename... P> shared_pointer_new(P&&... p) : shared_pointer<T>(new T(forward<P>(p)...)) {}
};

}
