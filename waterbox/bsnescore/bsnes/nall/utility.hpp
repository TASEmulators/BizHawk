#pragma once

#include <utility>

namespace nall {

using std::tuple;

template<typename T> struct base_from_member {
  base_from_member(T value) : value(value) {}
  T value;
};

template<typename To, typename With> struct castable {
  operator To&() { return (To&)value; }
  operator const To&() const { return (const To&)value; }
  operator With&() { return value; }
  operator const With&() const { return value; }
  auto& operator=(const With& value) { return this->value = value; }
  With value;
};

template<typename T> inline auto allocate(uint size, const T& value) -> T* {
  T* array = new T[size];
  for(uint i = 0; i < size; i++) array[i] = value;
  return array;
}

}
