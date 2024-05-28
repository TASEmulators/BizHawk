#pragma once

namespace nall {

template<typename T> struct iterator {
  iterator(T* self, u64 offset) : _self(self), _offset(offset) {}
  auto operator*() -> T& { return _self[_offset]; }
  auto operator!=(const iterator& source) const -> bool { return _offset != source._offset; }
  auto operator++() -> iterator& { return _offset++, *this; }
  auto offset() const -> u64 { return _offset; }

private:
  T* _self;
  u64 _offset;
};

template<typename T> struct iterator_const {
  iterator_const(const T* self, u64 offset) : _self(self), _offset(offset) {}
  auto operator*() -> const T& { return _self[_offset]; }
  auto operator!=(const iterator_const& source) const -> bool { return _offset != source._offset; }
  auto operator++() -> iterator_const& { return _offset++, *this; }
  auto offset() const -> u64 { return _offset; }

private:
  const T* _self;
  u64 _offset;
};

template<typename T> struct reverse_iterator {
  reverse_iterator(T* self, u64 offset) : _self(self), _offset(offset) {}
  auto operator*() -> T& { return _self[_offset]; }
  auto operator!=(const reverse_iterator& source) const -> bool { return _offset != source._offset; }
  auto operator++() -> reverse_iterator& { return _offset--, *this; }
  auto offset() const -> u64 { return _offset; }

private:
  T* _self;
  u64 _offset;
};

template<typename T> struct reverse_iterator_const {
  reverse_iterator_const(const T* self, u64 offset) : _self(self), _offset(offset) {}
  auto operator*() -> const T& { return _self[_offset]; }
  auto operator!=(const reverse_iterator_const& source) const -> bool { return _offset != source._offset; }
  auto operator++() -> reverse_iterator_const& { return _offset--, *this; }
  auto offset() const -> u64 { return _offset; }

private:
  const T* _self;
  u64 _offset;
};

//std::rbegin(), std::rend() is missing from GCC 4.9; which I still target

template<typename T, u64 Size> auto rbegin(T (&array)[Size]) { return reverse_iterator<T>{array, Size - 1}; }
template<typename T, u64 Size> auto rend(T (&array)[Size]) { return reverse_iterator<T>{array, (u64)-1}; }

template<typename T> auto rbegin(T& self) { return self.rbegin(); }
template<typename T> auto rend(T& self) { return self.rend(); }

template<typename T> struct reverse_wrapper {
  auto begin() { return rbegin(_self); }
  auto end() { return rend(_self); }

  auto begin() const { return rbegin(_self); }
  auto end() const { return rend(_self); }

  T _self;
};

template<typename T> auto reverse(T& object) -> reverse_wrapper<T&> {
  return {object};
}

template<typename T> auto reverse(T&& object) -> reverse_wrapper<T> {
  return {object};
}

}
