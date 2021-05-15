#pragma once

#include <nall/traits.hpp>

namespace nall {

template<typename T> struct function;

template<typename R, typename... P> struct function<auto (P...) -> R> {
  using cast = auto (*)(P...) -> R;

  //value = true if auto L::operator()(P...) -> R exists
  template<typename L> struct is_compatible {
    template<typename T> static auto exists(T*) -> const typename is_same<R, decltype(declval<T>().operator()(declval<P>()...))>::type;
    template<typename T> static auto exists(...) -> const false_type;
    static constexpr bool value = decltype(exists<L>(0))::value;
  };

  function() {}
  function(const function& source) { operator=(source); }
  function(auto (*function)(P...) -> R) { callback = new global(function); }
  template<typename C> function(auto (C::*function)(P...) -> R, C* object) { callback = new member<C>(function, object); }
  template<typename C> function(auto (C::*function)(P...) const -> R, C* object) { callback = new member<C>((auto (C::*)(P...) -> R)function, object); }
  template<typename L, typename = enable_if_t<is_compatible<L>::value>> function(const L& object) { callback = new lambda<L>(object); }
  explicit function(void* function) { if(function) callback = new global((auto (*)(P...) -> R)function); }
  ~function() { if(callback) delete callback; }

  explicit operator bool() const { return callback; }
  auto operator()(P... p) const -> R { return (*callback)(forward<P>(p)...); }
  auto reset() -> void { if(callback) { delete callback; callback = nullptr; } }

  auto operator=(const function& source) -> function& {
    if(this != &source) {
      if(callback) { delete callback; callback = nullptr; }
      if(source.callback) callback = source.callback->copy();
    }
    return *this;
  }

  auto operator=(void* source) -> function& {
    if(callback) { delete callback; callback = nullptr; }
    callback = new global((auto (*)(P...) -> R)source);
    return *this;
  }

private:
  struct container {
    virtual auto operator()(P... p) const -> R = 0;
    virtual auto copy() const -> container* = 0;
    virtual ~container() = default;
  };

  container* callback = nullptr;

  struct global : container {
    auto (*function)(P...) -> R;
    auto operator()(P... p) const -> R { return function(forward<P>(p)...); }
    auto copy() const -> container* { return new global(function); }
    global(auto (*function)(P...) -> R) : function(function) {}
  };

  template<typename C> struct member : container {
    auto (C::*function)(P...) -> R;
    C* object;
    auto operator()(P... p) const -> R { return (object->*function)(forward<P>(p)...); }
    auto copy() const -> container* { return new member(function, object); }
    member(auto (C::*function)(P...) -> R, C* object) : function(function), object(object) {}
  };

  template<typename L> struct lambda : container {
    mutable L object;
    auto operator()(P... p) const -> R { return object(forward<P>(p)...); }
    auto copy() const -> container* { return new lambda(object); }
    lambda(const L& object) : object(object) {}
  };
};

}
