#include <n64/n64.hpp>

namespace ares::Nintendo64 {

VI vi;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto VI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("VI");

  #if defined(VULKAN)
  screen = node->append<Node::Video::Screen>("Screen", vulkan.outputUpscale * 640, vulkan.outputUpscale * 576);
  #else
  screen = node->append<Node::Video::Screen>("Screen", 640, 576);
  #endif
  screen->setRefresh({&VI::refresh, this});
  screen->colors((1 << 24) + (1 << 15), [&](n32 color) -> n64 {
    if(color < (1 << 24)) {
      u64 a = 65535;
      u64 r = image::normalize(color >> 16 & 255, 8, 16);
      u64 g = image::normalize(color >>  8 & 255, 8, 16);
      u64 b = image::normalize(color >>  0 & 255, 8, 16);
      return a << 48 | r << 32 | g << 16 | b << 0;
    } else {
      u64 a = 65535;
      u64 r = image::normalize(color >> 10 & 31, 5, 16);
      u64 g = image::normalize(color >>  5 & 31, 5, 16);
      u64 b = image::normalize(color >>  0 & 31, 5, 16);
      return a << 48 | r << 32 | g << 16 | b << 0;
    }
  });
  #if defined(VULKAN)
  screen->setSize(vulkan.outputUpscale * 640, vulkan.outputUpscale * 480);
  if(!vulkan.supersampleScanout) {
    screen->setScale(1.0 / vulkan.outputUpscale, 1.0 / vulkan.outputUpscale);
  }
  #else
  screen->setSize(640, 480);
  #endif

  debugger.load(node);
}

auto VI::unload() -> void {
  debugger = {};
  screen->quit();
  node->remove(screen);
  screen.reset();
  node.reset();
}

auto VI::main() -> void {
  //field is not compared
  if(io.vcounter << 1 == io.coincidence) {
    mi.raise(MI::IRQ::VI);
  }

  if(++io.vcounter >= (Region::NTSC() ? 262 : 312) + io.field) {
    io.vcounter = 0;
    io.field = io.field + 1 & io.serrate;
    if(!io.field) {
      #if defined(VULKAN)
      gpuOutputValid = vulkan.scanoutAsync(io.field);
      vulkan.frame();
      #endif

      refreshed = true;
      screen->frame();
    }
  }

  if(Region::NTSC()) step(system.frequency() / 60 / 262);
  if(Region::PAL ()) step(system.frequency() / 50 / 312);
}

auto VI::step(u32 clocks) -> void {
  Thread::clock += clocks;
}

auto VI::refresh() -> void {
  #if defined(VULKAN)
  if(gpuOutputValid) {
    const u8* rgba = nullptr;
    u32 width = 0, height = 0;
    vulkan.mapScanoutRead(rgba, width, height);
    if(rgba) {
      screen->setViewport(0, 0, width, height);
      for(u32 y : range(height)) {
        auto source = rgba + width * y * sizeof(u32);
        auto target = screen->pixels(1).data() + y * vulkan.outputUpscale * 640;
        for(u32 x : range(width)) {
          target[x] = source[x * 4 + 0] << 16 | source[x * 4 + 1] << 8 | source[x * 4 + 2] << 0;
        }
      }
    } else {
      screen->setViewport(0, 0, 1, 1);
      screen->pixels(1).data()[0] = 0;
    }
    vulkan.unmapScanoutRead();
    vulkan.endScanout();
    return;
  }
  #endif

  u32 pitch  = vi.io.width;
  u32 width  = vi.io.width;  //vi.io.xscale <= 0x300 ? 320 : 640;
  u32 height = vi.io.yscale <= 0x400 ? 239 : 478;
  screen->setViewport(0, 0, width, height);

  if(vi.io.colorDepth == 2) {
    //15bpp
    for(u32 y : range(height)) {
      u32 address = vi.io.dramAddress + y * pitch * 2;
      auto line = screen->pixels(1).data() + y * 640;
      for(u32 x : range(min(width, pitch))) {
        u16 data = bus.read<Half>(address + x * 2);
        *line++ = 1 << 24 | data >> 1;
      }
    }
  }

  if(vi.io.colorDepth == 3) {
    //24bpp
    for(u32 y : range(height)) {
      u32 address = vi.io.dramAddress + y * pitch * 4;
      auto line = screen->pixels(1).data() + y * 640;
      for(u32 x : range(min(width, pitch))) {
        u32 data = bus.read<Word>(address + x * 4);
        *line++ = data >> 8;
      }
    }
  }
}

auto VI::power(bool reset) -> void {
  Thread::reset();
  screen->power();
  io = {};
  refreshed = false;

  #if defined(VULKAN)
  gpuOutputValid = false;
  #endif
}

}
