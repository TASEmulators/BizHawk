#pragma once

//hashset
//
//search: O(1) average; O(n) worst
//insert: O(1) average; O(n) worst
//remove: O(1) average; O(n) worst
//
//requirements:
//  auto T::hash() const -> uint;
//  auto T::operator==(const T&) const -> bool;

namespace nall {

template<typename T>
struct hashset {
  hashset() = default;
  hashset(uint length) : length(bit::round(length)) {}
  hashset(const hashset& source) { operator=(source); }
  hashset(hashset&& source) { operator=(move(source)); }
  ~hashset() { reset(); }

  auto operator=(const hashset& source) -> hashset& {
    reset();
    if(source.pool) {
      for(uint n : range(source.count)) {
        insert(*source.pool[n]);
      }
    }
    return *this;
  }

  auto operator=(hashset&& source) -> hashset& {
    reset();
    pool = source.pool;
    length = source.length;
    count = source.count;
    source.pool = nullptr;
    source.length = 8;
    source.count = 0;
    return *this;
  }

  explicit operator bool() const { return count; }
  auto capacity() const -> uint { return length; }
  auto size() const -> uint { return count; }

  auto reset() -> void {
    if(pool) {
      for(uint n : range(length)) {
        if(pool[n]) {
          delete pool[n];
          pool[n] = nullptr;
        }
      }
      delete pool;
      pool = nullptr;
    }
    length = 8;
    count = 0;
  }

  auto reserve(uint size) -> void {
    //ensure all items will fit into pool (with <= 50% load) and amortize growth
    size = bit::round(max(size, count << 1));
    T** copy = new T*[size]();

    if(pool) {
      for(uint n : range(length)) {
        if(pool[n]) {
          uint hash = (*pool[n]).hash() & (size - 1);
          while(copy[hash]) if(++hash >= size) hash = 0;
          copy[hash] = pool[n];
          pool[n] = nullptr;
        }
      }
    }

    delete pool;
    pool = copy;
    length = size;
  }

  auto find(const T& value) -> maybe<T&> {
    if(!pool) return nothing;

    uint hash = value.hash() & (length - 1);
    while(pool[hash]) {
      if(value == *pool[hash]) return *pool[hash];
      if(++hash >= length) hash = 0;
    }

    return nothing;
  }

  auto insert(const T& value) -> maybe<T&> {
    if(!pool) pool = new T*[length]();

    //double pool size when load is >= 50%
    if(count >= (length >> 1)) reserve(length << 1);
    count++;

    uint hash = value.hash() & (length - 1);
    while(pool[hash]) if(++hash >= length) hash = 0;
    pool[hash] = new T(value);

    return *pool[hash];
  }

  auto remove(const T& value) -> bool {
    if(!pool) return false;

    uint hash = value.hash() & (length - 1);
    while(pool[hash]) {
      if(value == *pool[hash]) {
        delete pool[hash];
        pool[hash] = nullptr;
        count--;
        return true;
      }
      if(++hash >= length) hash = 0;
    }

    return false;
  }

protected:
  T** pool = nullptr;
  uint length = 8;  //length of pool
  uint count = 0;   //number of objects inside of the pool
};

}
