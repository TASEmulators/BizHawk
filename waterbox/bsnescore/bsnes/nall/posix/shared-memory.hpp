#pragma once

#include <semaphore.h>
#include <sys/mman.h>

namespace nall {

struct shared_memory {
  shared_memory() = default;
  shared_memory(const shared_memory&) = delete;
  auto operator=(const shared_memory&) -> shared_memory& = delete;

  ~shared_memory() {
    reset();
  }

  explicit operator bool() const {
    return _mode != mode::inactive;
  }

  auto size() const -> unsigned {
    return _size;
  }

  auto acquired() const -> bool {
    return _acquired;
  }

  auto acquire() -> uint8_t* {
    if(!acquired()) {
      sem_wait(_semaphore);
      _acquired = true;
    }
    return _data;
  }

  auto release() -> void {
    if(acquired()) {
      sem_post(_semaphore);
      _acquired = false;
    }
  }

  auto reset() -> void {
    release();
    if(_mode == mode::server) return remove();
    if(_mode == mode::client) return close();
  }

  auto create(const string& name, unsigned size) -> bool {
    reset();

    _name = {"/nall-", string{name}.transform("/:", "--")};
    _size = size;

    //O_CREAT | O_EXCL seems to throw ENOENT even when semaphore does not exist ...
    _semaphore = sem_open(_name, O_CREAT, 0644, 1);
    if(_semaphore == SEM_FAILED) return remove(), false;

    _descriptor = shm_open(_name, O_CREAT | O_TRUNC | O_RDWR, 0644);
    if(_descriptor < 0) return remove(), false;

    if(ftruncate(_descriptor, _size) != 0) return remove(), false;

    _data = (uint8_t*)mmap(nullptr, _size, PROT_READ | PROT_WRITE, MAP_SHARED, _descriptor, 0);
    if(_data == MAP_FAILED) return remove(), false;

    memory::fill(_data, _size);

    _mode = mode::server;
    return true;
  }

  auto remove() -> void {
    if(_data) {
      munmap(_data, _size);
      _data = nullptr;
    }

    if(_descriptor) {
      ::close(_descriptor);
      shm_unlink(_name);
      _descriptor = -1;
    }

    if(_semaphore) {
      sem_close(_semaphore);
      sem_unlink(_name);
      _semaphore = nullptr;
    }

    _mode = mode::inactive;
    _name = "";
    _size = 0;
  }

  auto open(const string& name, unsigned size) -> bool {
    reset();

    _name = {"/nall-", string{name}.transform("/:", "--")};
    _size = size;

    _semaphore = sem_open(_name, 0, 0644);
    if(_semaphore == SEM_FAILED) return close(), false;

    _descriptor = shm_open(_name, O_RDWR, 0644);
    if(_descriptor < 0) return close(), false;

    _data = (uint8_t*)mmap(nullptr, _size, PROT_READ | PROT_WRITE, MAP_SHARED, _descriptor, 0);
    if(_data == MAP_FAILED) return close(), false;

    _mode = mode::client;
    return true;
  }

  auto close() -> void {
    if(_data) {
      munmap(_data, _size);
      _data = nullptr;
    }

    if(_descriptor) {
      ::close(_descriptor);
      _descriptor = -1;
    }

    if(_semaphore) {
      sem_close(_semaphore);
      _semaphore = nullptr;
    }

    _mode = mode::inactive;
    _name = "";
    _size = 0;
  }

private:
  enum class mode : unsigned { server, client, inactive } _mode = mode::inactive;
  string _name;
  sem_t* _semaphore = nullptr;
  signed _descriptor = -1;
  uint8_t* _data = nullptr;
  unsigned _size = 0;
  bool _acquired = false;
};

}
