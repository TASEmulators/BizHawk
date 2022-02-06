#pragma once

#include <nall/traits.hpp>

namespace nall {

struct nothing_t {};
static nothing_t nothing;
struct else_t {};

template<typename T>
struct maybe {
  maybe() {}
  maybe(nothing_t) {}
  maybe(const T& source) { operator=(source); }
  maybe(T&& source) { operator=(move(source)); }
  maybe(const maybe& source) { operator=(source); }
  maybe(maybe&& source) { operator=(move(source)); }
  ~maybe() { reset(); }

  auto operator=(nothing_t) -> maybe& { reset(); return *this; }
  auto operator=(const T& source) -> maybe& { reset(); _valid = true; new(&_value.t) T(source); return *this; }
  auto operator=(T&& source) -> maybe& { reset(); _valid = true; new(&_value.t) T(move(source)); return *this; }

  auto operator=(const maybe& source) -> maybe& {
    if(this == &source) return *this;
    reset();
    if(_valid = source._valid) new(&_value.t) T(source.get());
    return *this;
  }

  auto operator=(maybe&& source) -> maybe& {
    if(this == &source) return *this;
    reset();
    if(_valid = source._valid) new(&_value.t) T(move(source.get()));
    return *this;
  }

  explicit operator bool() const { return _valid; }
  auto reset() -> void { if(_valid) { _value.t.~T(); _valid = false; } }
  auto data() -> T* { return _valid ? &_value.t : nullptr; }
  auto get() -> T& { assert(_valid); return _value.t; }

  auto data() const -> const T* { return ((maybe*)this)->data(); }
  auto get() const -> const T& { return ((maybe*)this)->get(); }
  auto operator->() -> T* { return data(); }
  auto operator->() const -> const T* { return data(); }
  auto operator*() -> T& { return get(); }
  auto operator*() const -> const T& { return get(); }
  auto operator()() -> T& { return get(); }
  auto operator()() const -> const T& { return get(); }
  auto operator()(const T& invalid) const -> const T& { return _valid ? get() : invalid; }

private:
  union U {
    T t;
    U() {}
    ~U() {}
  } _value;
  bool _valid = false;
};

template<typename T>
struct maybe<T&> {
  maybe() : _value(nullptr) {}
  maybe(nothing_t) : _value(nullptr) {}
  maybe(const T& source) : _value((T*)&source) {}
  maybe(const maybe& source) : _value(source._value) {}

  auto operator=(nothing_t) -> maybe& { _value = nullptr; return *this; }
  auto operator=(const T& source) -> maybe& { _value = (T*)&source; return *this; }
  auto operator=(const maybe& source) -> maybe& { _value = source._value; return *this; }

  explicit operator bool() const { return _value; }
  auto reset() -> void { _value = nullptr; }
  auto data() -> T* { return _value; }
  auto get() -> T& { assert(_value); return *_value; }

  auto data() const -> const T* { return ((maybe*)this)->data(); }
  auto get() const -> const T& { return ((maybe*)this)->get(); }
  auto operator->() -> T* { return data(); }
  auto operator->() const -> const T* { return data(); }
  auto operator*() -> T& { return get(); }
  auto operator*() const -> const T& { return get(); }
  auto operator()() -> T& { return get(); }
  auto operator()() const -> const T& { return get(); }
  auto operator()(const T& invalid) const -> const T& { return _value ? get() : invalid; }

private:
  T* _value;
};

}
