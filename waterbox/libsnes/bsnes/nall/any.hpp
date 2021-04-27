#pragma once

#include <typeinfo>
#include <nall/traits.hpp>

namespace nall {

struct any {
  any() = default;
  any(const any& source) { operator=(source); }
  any(any&& source) { operator=(move(source)); }
  template<typename T> any(const T& value) { operator=(value); }
  ~any() { reset(); }

  explicit operator bool() const { return container; }
  auto reset() -> void { if(container) { delete container; container = nullptr; } }

  auto type() const -> const std::type_info& {
    return container ? container->type() : typeid(void);
  }

  template<typename T> auto is() const -> bool {
    return type() == typeid(typename remove_reference<T>::type);
  }

  template<typename T> auto get() -> T& {
    if(!is<T>()) throw;
    return static_cast<holder<typename remove_reference<T>::type>*>(container)->value;
  }

  template<typename T> auto get() const -> const T& {
    if(!is<T>()) throw;
    return static_cast<holder<typename remove_reference<T>::type>*>(container)->value;
  }

  template<typename T> auto get(const T& fallback) const -> const T& {
    if(!is<T>()) return fallback;
    return static_cast<holder<typename remove_reference<T>::type>*>(container)->value;
  }

  template<typename T> auto operator=(const T& value) -> any& {
    using auto_t = typename conditional<is_array<T>::value, typename remove_extent<typename add_const<T>::type>::type*, T>::type;

    if(type() == typeid(auto_t)) {
      static_cast<holder<auto_t>*>(container)->value = (auto_t)value;
    } else {
      if(container) delete container;
      container = new holder<auto_t>((auto_t)value);
    }

    return *this;
  }

  auto operator=(const any& source) -> any& {
    if(container) { delete container; container = nullptr; }
    if(source.container) container = source.container->copy();
    return *this;
  }

  auto operator=(any&& source) -> any& {
    if(container) delete container;
    container = source.container;
    source.container = nullptr;
    return *this;
  }

private:
  struct placeholder {
    virtual ~placeholder() = default;
    virtual auto type() const -> const std::type_info& = 0;
    virtual auto copy() const -> placeholder* = 0;
  };
  placeholder* container = nullptr;

  template<typename T> struct holder : placeholder {
    holder(const T& value) : value(value) {}
    auto type() const -> const std::type_info& { return typeid(T); }
    auto copy() const -> placeholder* { return new holder(value); }
    T value;
  };
};

}
