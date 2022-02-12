#pragma once

//priority queue implementation using binary min-heap array:
//O(1)     find
//O(log n) insert
//O(log n) remove(first)
//O(n)     remove(event)

#include <nall/function.hpp>
#include <nall/serializer.hpp>

namespace nall {

template<typename T> struct priority_queue;

template<typename T, u32 Size>
struct priority_queue<T[Size]> {
  explicit operator bool() const {
    return size != 0;
  }

  auto reset() -> void {
    clock = 0;
    size = 0;
  }

  template<typename F>
  auto step(u32 clocks, const F& callback) -> void {
    clock += clocks;
    while(size && ge(clock, heap[0].clock)) {
      if(auto event = remove()) callback(*event);
    }
  }

  auto insert(const T& event, u32 clock) -> bool {
    if(size >= Size) return false;

    u32 child = size++;
    clock += this->clock;

    while(child) {
      u32 parent = (child - 1) >> 1;
      if(ge(clock, heap[parent].clock)) break;

      heap[child].clock = heap[parent].clock;
      heap[child].event = heap[parent].event;
      heap[child].valid = heap[parent].valid;
      child = parent;
    }

    heap[child].clock = clock;
    heap[child].event = event;
    heap[child].valid = true;
    return true;
  }

  auto remove() -> maybe<T> {
    T event = heap[0].event;
    bool valid = heap[0].valid;

    u32 parent = 0;
    u32 clock = heap[--size].clock;

    while(true) {
      u32 child = (parent << 1) + 1;
      if(child >= size) break;

      if(child + 1 < size && ge(heap[child].clock, heap[child + 1].clock)) child++;
      if(ge(heap[child].clock, clock)) break;

      heap[parent].clock = heap[child].clock;
      heap[parent].event = heap[child].event;
      heap[parent].valid = heap[child].valid;
      parent = child;
    }

    heap[parent].clock = clock;
    heap[parent].event = heap[size].event;
    heap[parent].valid = heap[size].valid;

    if(valid) return event;
    return nothing;
  }

  auto remove(const T& event) -> void {
    for(auto& entry : heap) {
      if(entry.event == event) entry.valid = false;
    }
  }

  auto serialize(serializer& s) -> void {
    s(clock);
    s(size);
    for(auto& entry : heap) {
      s(entry.clock);
      s(entry.event);
      s(entry.valid);
    }
  }

private:
  //returns true if x is greater than or equal to y
  auto ge(u32 x, u32 y) -> bool {
    return x - y < 0x7fffffff;
  }

  u32 clock = 0;
  u32 size = 0;
  struct Entry {
    u32  clock;
    T    event;
    bool valid;
  } heap[Size];
};

}
