#ifndef GAMEBOY_HPP
#define GAMEBOY_HPP

#include <base/base.hpp>

namespace GameBoy {
  namespace Info {
    static const char Name[] = "bgameboy";
    static const unsigned SerializerVersion = 3;
  }
}

/*
  bgameboy - Game Boy, Super Game Boy, and Game Boy Color emulator
  author: byuu
  license: GPLv3
  project started: 2010-12-27
*/

#include <libco/libco.h>
#include <nall/gameboy/cartridge.hpp>

namespace GameBoy {
  struct Processor {
    cothread_t thread;
    unsigned frequency;
    int64 clock;

    inline void create(void (*entrypoint)(), unsigned frequency) {
      if(thread) co_delete(thread);
      thread = co_create(65536 * sizeof(void*), entrypoint);
      this->frequency = frequency;
      clock = 0;
    }

    inline void serialize(serializer &s) {
      s.integer(frequency);
      s.integer(clock);
    }

    inline Processor() : thread(nullptr) {
    }

    inline ~Processor() {
      if(thread) co_delete(thread);
    }
  };

  #include <gameboy/memory/memory.hpp>
  #include <gameboy/system/system.hpp>
  #include <gameboy/scheduler/scheduler.hpp>
  #include <gameboy/cartridge/cartridge.hpp>
  #include <gameboy/cpu/cpu.hpp>
  #include <gameboy/apu/apu.hpp>
  #include <gameboy/lcd/lcd.hpp>
  #include <gameboy/cheat/cheat.hpp>
  #include <gameboy/video/video.hpp>
};

#endif
