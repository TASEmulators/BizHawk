#ifndef NALL_ARRAY_HPP
#define NALL_ARRAY_HPP

#include <stdlib.h>
#include <algorithm>
#include <initializer_list>
#include <utility>
#include <nall/algorithm.hpp>
#include <nall/bit.hpp>
#include <nall/sort.hpp>
#include <nall/type_traits.hpp>
#include <nall/utility.hpp>

namespace nall {

template<typename T, typename Enable = void> struct array;

//non-reference array
//===================

template<typename T> struct array<T, typename std::enable_if<!std::is_reference<T>::value>::type> {
  struct exception_out_of_bounds{};

protected:
  T *pool;
  unsigned poolsize, objectsize;

public:
  unsigned size() const { return objectsize; }
  unsigned capacity() const { return poolsize; }

  void reset() {
    if(pool) free(pool);
    pool = nullptr;
    poolsize = 0;
    objectsize = 0;
  }

  void reserve(unsigned newsize) {
    if(newsize == poolsize) return;

    pool = (T*)realloc(pool, newsize * sizeof(T));
    poolsize = newsize;
    objectsize = min(objectsize, newsize);
  }

  void resize(unsigned newsize) {
    if(newsize > poolsize) reserve(bit::round(newsize));  //round reserve size up to power of 2
    objectsize = newsize;
  }

  T* get(unsigned minsize = 0) {
    if(minsize > objectsize) resize(minsize);
    return pool;
  }

  void append(const T data) {
    operator()(objectsize) = data;
  }

  void append(const T data[], unsigned length) {
    for(unsigned n = 0; n < length; n++) operator()(objectsize) = data[n];
  }

  void remove() {
    if(size > 0) resize(size - 1);  //remove last element only
  }

  void remove(unsigned index, unsigned count = 1) {
    for(unsigned i = index; count + i < objectsize; i++) {
      pool[i] = pool[count + i];
    }
    if(count + index >= objectsize) resize(index);  //every element >= index was removed
    else resize(objectsize - count);
  }

  void sort() {
    nall::sort(pool, objectsize);
  }

  template<typename Comparator> void sort(const Comparator &lessthan) {
    nall::sort(pool, objectsize, lessthan);
  }

  optional<unsigned> find(const T data) {
    for(unsigned n = 0; n < size(); n++) if(pool[n] == data) return { true, n };
    return { false, 0u };
  }

  void clear() {
    memset(pool, 0, objectsize * sizeof(T));
  }

  array() : pool(nullptr), poolsize(0), objectsize(0) {
  }

  array(std::initializer_list<T> list) : pool(nullptr), poolsize(0), objectsize(0) {
    for(auto &data : list) append(data);
  }

  ~array() {
    reset();
  }

  //copy
  array& operator=(const array &source) {
    if(pool) free(pool);
    objectsize = source.objectsize;
    poolsize = source.poolsize;
    pool = (T*)malloc(sizeof(T) * poolsize);            //allocate entire pool size,
    memcpy(pool, source.pool, sizeof(T) * objectsize);  //... but only copy used pool objects
    return *this;
  }

  array(const array &source) : pool(nullptr), poolsize(0), objectsize(0) {
    operator=(source);
  }

  //move
  array& operator=(array &&source) {
    if(pool) free(pool);
    pool = source.pool;
    poolsize = source.poolsize;
    objectsize = source.objectsize;
    source.pool = nullptr;
    source.reset();
    return *this;
  }

  array(array &&source) : pool(nullptr), poolsize(0), objectsize(0) {
    operator=(std::move(source));
  }

  //access
  inline T& operator[](unsigned position) {
    if(position >= objectsize) throw exception_out_of_bounds();
    return pool[position];
  }

  inline const T& operator[](unsigned position) const {
    if(position >= objectsize) throw exception_out_of_bounds();
    return pool[position];
  }

  inline T& operator()(unsigned position) {
    if(position >= objectsize) resize(position + 1);
    return pool[position];
  }

  inline const T& operator()(unsigned position, const T& data) {
    if(position >= objectsize) return data;
    return pool[position];
  }

  //iteration
  T* begin() { return &pool[0]; }
  T* end() { return &pool[objectsize]; }
  const T* begin() const { return &pool[0]; }
  const T* end() const { return &pool[objectsize]; }
};

//reference array
//===============

template<typename TR> struct array<TR, typename std::enable_if<std::is_reference<TR>::value>::type> {
  struct exception_out_of_bounds{};

protected:
  typedef typename std::remove_reference<TR>::type T;
  T **pool;
  unsigned poolsize, objectsize;

public:
  unsigned size() const { return objectsize; }
  unsigned capacity() const { return poolsize; }

  void reset() {
    if(pool) free(pool);
    pool = nullptr;
    poolsize = 0;
    objectsize = 0;
  }

  void reserve(unsigned newsize) {
    if(newsize == poolsize) return;

    pool = (T**)realloc(pool, sizeof(T*) * newsize);
    poolsize = newsize;
    objectsize = min(objectsize, newsize);
  }

  void resize(unsigned newsize) {
    if(newsize > poolsize) reserve(bit::round(newsize));
    objectsize = newsize;
  }

  template<typename... Args>
  bool append(T& data, Args&&... args) {
    bool result = append(data);
    append(std::forward<Args>(args)...);
    return result;
  }

  bool append(T& data) {
    if(find(data)) return false;
    unsigned offset = objectsize++;
    if(offset >= poolsize) resize(offset + 1);
    pool[offset] = &data;
    return true;
  }

  bool remove(T& data) {
    if(auto position = find(data)) {
      for(signed i = position(); i < objectsize - 1; i++) pool[i] = pool[i + 1];
      resize(objectsize - 1);
      return true;
    }
    return false;
  }

  optional<unsigned> find(const T& data) {
    for(unsigned n = 0; n < objectsize; n++) if(pool[n] == &data) return { true, n };
    return { false, 0u };
  }

  template<typename... Args> array(Args&&... args) : pool(nullptr), poolsize(0), objectsize(0) {
    construct(std::forward<Args>(args)...);
  }

  ~array() {
    reset();
  }

  array& operator=(const array &source) {
    if(pool) free(pool);
    objectsize = source.objectsize;
    poolsize = source.poolsize;
    pool = (T**)malloc(sizeof(T*) * poolsize);
    memcpy(pool, source.pool, sizeof(T*) * objectsize);
    return *this;
  }

  array& operator=(const array &&source) {
    if(pool) free(pool);
    pool = source.pool;
    poolsize = source.poolsize;
    objectsize = source.objectsize;
    source.pool = nullptr;
    source.reset();
    return *this;
  }

  T& operator[](unsigned position) const {
    if(position >= objectsize) throw exception_out_of_bounds();
    return *pool[position];
  }

  //iteration
  struct iterator {
    bool operator!=(const iterator &source) const { return position != source.position; }
    T& operator*() { return source.operator[](position); }
    iterator& operator++() { position++; return *this; }
    iterator(const array &source, unsigned position) : source(source), position(position) {}
  private:
    const array &source;
    unsigned position;
  };

  iterator begin() { return iterator(*this, 0); }
  iterator end() { return iterator(*this, objectsize); }
  const iterator begin() const { return iterator(*this, 0); }
  const iterator end() const { return iterator(*this, objectsize); }

private:
  void construct() {
  }

  void construct(const array& source) { operator=(source); }
  void construct(const array&& source) { operator=(std::move(source)); }

  template<typename... Args> void construct(T& data, Args&&... args) {
    append(data);
    construct(std::forward<Args>(args)...);
  }
};

}

#endif
