#pragma once

#include <target-bsnescore/callbacks.h>
#include <nall/vfs.hpp>

namespace nall::vfs {

struct biz_file : file {
  auto seek(intmax offset, index mode) -> void override {
    return snesCallbacks.snes_msu_seek(offset, mode == index::relative);
  }

  auto read() -> uint8_t override {
    return snesCallbacks.snes_msu_read();
  }

  auto end() const -> bool override {
    return snesCallbacks.snes_msu_end();
  }

  // not implemented and not necessary, but must be overridden
  auto size() const -> uintmax override { return -1; }
  auto offset() const -> uintmax override { return -1; }
  auto write(uint8_t data) -> void override {}
};

}
