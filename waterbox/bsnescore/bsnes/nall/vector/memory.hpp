#pragma once

namespace nall {

//nall::vector acts internally as a deque (double-ended queue)
//it does this because it's essentially free to do so, only costing an extra integer in sizeof(vector)

template<typename T> auto vector<T>::reset() -> void {
  if(!_pool) return;

  for(uint64_t n : range(_size)) _pool[n].~T();
  memory::free(_pool - _left);

  _pool = nullptr;
  _size = 0;
  _left = 0;
  _right = 0;
}

//acquire ownership of allocated memory

template<typename T> auto vector<T>::acquire(T* data, uint64_t size, uint64_t capacity) -> void {
  reset();
  _pool = data;
  _size = size;
  _left = 0;
  _right = capacity ? capacity : size;
}

//release ownership of allocated memory

template<typename T> auto vector<T>::release() -> T* {
  auto pool = _pool;
  _pool = nullptr;
  _size = 0;
  _left = 0;
  _right = 0;
  return pool;
}

//reserve allocates memory for objects, but does not initialize them
//when the vector desired size is known, this can be used to avoid growing the capacity dynamically
//reserve will not actually shrink the capacity, only expand it
//shrinking the capacity would destroy objects, and break amortized growth with reallocate and resize

template<typename T> auto vector<T>::reserveLeft(uint64_t capacity) -> bool {
  if(_size + _left >= capacity) return false;

  uint64_t left = bit::round(capacity);
  auto pool = memory::allocate<T>(left + _right) + (left - _size);
  for(uint64_t n : range(_size)) new(pool + n) T(move(_pool[n]));
  memory::free(_pool - _left);

  _pool = pool;
  _left = left - _size;

  return true;
}

template<typename T> auto vector<T>::reserveRight(uint64_t capacity) -> bool {
  if(_size + _right >= capacity) return false;

  uint64_t right = bit::round(capacity);
  auto pool = memory::allocate<T>(_left + right) + _left;
  for(uint64_t n : range(_size)) new(pool + n) T(move(_pool[n]));
  memory::free(_pool - _left);

  _pool = pool;
  _right = right - _size;

  return true;
}

//reallocation is meant for POD types, to avoid the overhead of initialization
//do not use with non-POD types, or they will not be properly constructed or destructed

template<typename T> auto vector<T>::reallocateLeft(uint64_t size) -> bool {
  if(size < _size) {  //shrink
    _pool += _size - size;
    _left += _size - size;
    _size = size;
    return true;
  }
  if(size > _size) {  //grow
    reserveLeft(size);
    _pool -= size - _size;
    _left -= size - _size;
    _size = size;
    return true;
  }
  return false;
}

template<typename T> auto vector<T>::reallocateRight(uint64_t size) -> bool {
  if(size < _size) {  //shrink
    _right += _size - size;
    _size = size;
    return true;
  }
  if(size > _size) {  //grow
    reserveRight(size);
    _right -= size - _size;
    _size = size;
    return true;
  }
  return false;
}

//resize is meant for non-POD types, and will properly construct objects

template<typename T> auto vector<T>::resizeLeft(uint64_t size, const T& value) -> bool {
  if(size < _size) {  //shrink
    for(uint64_t n : range(_size - size)) _pool[n].~T();
    _pool += _size - size;
    _left += _size - size;
    _size = size;
    return true;
  }
  if(size > _size) {  //grow
    reserveLeft(size);
    _pool -= size - _size;
    for(uint64_t n : nall::reverse(range(size - _size))) new(_pool + n) T(value);
    _left -= size - _size;
    _size = size;
    return true;
  }
  return false;
}

template<typename T> auto vector<T>::resizeRight(uint64_t size, const T& value) -> bool {
  if(size < _size) {  //shrink
    for(uint64_t n : range(size, _size)) _pool[n].~T();
    _right += _size - size;
    _size = size;
    return true;
  }
  if(size > _size) {  //grow
    reserveRight(size);
    for(uint64_t n : range(_size, size)) new(_pool + n) T(value);
    _right -= size - _size;
    _size = size;
    return true;
  }
  return false;
}

}
