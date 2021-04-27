#pragma once

namespace nall {

template<typename T, typename... P> struct variant_size {
  static constexpr uint size = max(sizeof(T), variant_size<P...>::size);
};

template<typename T> struct variant_size<T> {
  static constexpr uint size = sizeof(T);
};

template<uint Index, typename F, typename T, typename... P> struct variant_index {
  static constexpr uint index = is_same_v<F, T> ? Index : variant_index<Index + 1, F, P...>::index;
};

template<uint Index, typename F, typename T> struct variant_index<Index, F, T> {
  static constexpr uint index = is_same_v<F, T> ? Index : 0;
};

template<typename T, typename... P> struct variant_copy {
  constexpr variant_copy(uint index, uint assigned, void* target, void* source) {
    if(index == assigned) new(target) T(*((T*)source));
    else variant_copy<P...>(index + 1, assigned, target, source);
  }
};

template<typename T> struct variant_copy<T> {
  constexpr variant_copy(uint index, uint assigned, void* target, void* source) {
    if(index == assigned) new(target) T(*((T*)source));
  }
};

template<typename T, typename... P> struct variant_move {
  constexpr variant_move(uint index, uint assigned, void* target, void* source) {
    if(index == assigned) new(target) T(move(*((T*)source)));
    else variant_move<P...>(index + 1, assigned, target, source);
  }
};

template<typename T> struct variant_move<T> {
  constexpr variant_move(uint index, uint assigned, void* target, void* source) {
    if(index == assigned) new(target) T(move(*((T*)source)));
  }
};

template<typename T, typename... P> struct variant_destruct {
  constexpr variant_destruct(uint index, uint assigned, void* data) {
    if(index == assigned) ((T*)data)->~T();
    else variant_destruct<P...>(index + 1, assigned, data);
  }
};

template<typename T> struct variant_destruct<T> {
  constexpr variant_destruct(uint index, uint assigned, void* data) {
    if(index == assigned) ((T*)data)->~T();
  }
};

template<typename F, typename T, typename... P> struct variant_equals {
  constexpr auto operator()(uint index, uint assigned) const -> bool {
    if(index == assigned) return is_same_v<F, T>;
    return variant_equals<F, P...>()(index + 1, assigned);
  }
};

template<typename F, typename T> struct variant_equals<F, T> {
  constexpr auto operator()(uint index, uint assigned) const -> bool {
    if(index == assigned) return is_same_v<F, T>;
    return false;
  }
};

template<typename... P> struct variant final {  //final as destructor is not virtual
  variant() : assigned(0) {}
  variant(const variant& source) { operator=(source); }
  variant(variant&& source) { operator=(move(source)); }
  template<typename T> variant(const T& value) { operator=(value); }
  template<typename T> variant(T&& value) { operator=(move(value)); }
  ~variant() { reset(); }

  explicit operator bool() const { return assigned; }
  template<typename T> explicit constexpr operator T&() { return get<T>(); }
  template<typename T> explicit constexpr operator const T&() const { return get<T>(); }

  template<typename T> constexpr auto is() const -> bool {
    return variant_equals<T, P...>()(1, assigned);
  }

  template<typename T> constexpr auto get() -> T& {
    static_assert(variant_index<1, T, P...>::index, "type not in variant");
    struct variant_bad_cast{};
    if(!is<T>()) throw variant_bad_cast{};
    return *((T*)data);
  }

  template<typename T> constexpr auto get() const -> const T& {
    static_assert(variant_index<1, T, P...>::index, "type not in variant");
    struct variant_bad_cast{};
    if(!is<T>()) throw variant_bad_cast{};
    return *((const T*)data);
  }

  template<typename T> constexpr auto get(const T& fallback) const -> const T& {
    if(!is<T>()) return fallback;
    return *((const T*)data);
  }

  auto reset() -> void {
    if(assigned) variant_destruct<P...>(1, assigned, (void*)data);
    assigned = 0;
  }

  auto& operator=(const variant& source) {
    reset();
    if(assigned = source.assigned) variant_copy<P...>(1, source.assigned, (void*)data, (void*)source.data);
    return *this;
  }

  auto& operator=(variant&& source) {
    reset();
    if(assigned = source.assigned) variant_move<P...>(1, source.assigned, (void*)data, (void*)source.data);
    source.assigned = 0;
    return *this;
  }

  template<typename T> auto& operator=(const T& value) {
    static_assert(variant_index<1, T, P...>::index, "type not in variant");
    reset();
    new((void*)&data) T(value);
    assigned = variant_index<1, T, P...>::index;
    return *this;
  }

  template<typename T> auto& operator=(T&& value) {
    static_assert(variant_index<1, T, P...>::index, "type not in variant");
    reset();
    new((void*)&data) T(move(value));
    assigned = variant_index<1, T, P...>::index;
    return *this;
  }

private:
  alignas(P...) char data[variant_size<P...>::size];
  uint assigned;
};

}
